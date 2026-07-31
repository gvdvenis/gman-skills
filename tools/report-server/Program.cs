using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

// ── CLI argument parsing ────────────────────────────────────────────────────
var reportPath = GetArg(args, "--report-path");
var port       = int.TryParse(GetArg(args, "--port"), out var p) ? p : 5173;
var bindAddr   = GetArg(args, "--bind") is { Length: > 0 } b ? b : "127.0.0.1";
var idleMinutes= int.TryParse(GetArg(args, "--idle-minutes"), out var im) ? im : 10;

if (string.IsNullOrEmpty(reportPath))
{
    Console.Error.WriteLine("[report-server] --report-path is required.");
    return 1;
}

if (!File.Exists(reportPath))
{
    Console.Error.WriteLine($"[report-server] report file not found: {reportPath}");
    return 1;
}

// ── Port-conflict check ─────────────────────────────────────────────────────
// Exit code 2 signals "port already bound — assume server already running".
if (IsPortBound(bindAddr, port))
{
    Console.WriteLine($"[report-server] WARNING: port {port} on {bindAddr} is already bound. " +
                      "Assuming the server is already running. No new instance will be started.");
    return 2;
}

// ── Build the host ──────────────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();   // keep stdout clean; CLI owns the console
builder.WebHost.UseUrls($"http://{bindAddr}:{port}");

var app = builder.Build();

// ── Static files (wwwroot/index.html) ──────────────────────────────────────
app.UseDefaultFiles();   // maps "/" → "/index.html"
app.UseStaticFiles();

// ── State ───────────────────────────────────────────────────────────────────
var browserConnected  = false;
var lastActivity      = DateTime.UtcNow;
var cts               = new CancellationTokenSource();
var reportLock        = new SemaphoreSlim(1, 1);
var historyLock       = new SemaphoreSlim(1, 1);
var historyDir        = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".copilot", "blazor-orchestration", "history");
var historyFile       = Path.Combine(historyDir, "dismissed-keys.json");
var jsonWriteOptions  = new JsonSerializerOptions { WriteIndented = true };

// ── Middleware: track idle activity after first browser connection ───────────
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path != "/favicon.ico")
    {
        lastActivity = DateTime.UtcNow;
        if (!browserConnected &&
            (ctx.Request.Path == "/api/report" || ctx.Request.Path == "/" || ctx.Request.Path == "/index.html"))
            browserConnected = true;
    }
    await next(ctx);
});

// ── Endpoints ───────────────────────────────────────────────────────────────
app.MapGet("/ping", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/report", async (HttpContext ctx) =>
{
    var json = await File.ReadAllTextAsync(reportPath, ctx.RequestAborted);
    return Results.Content(json, "application/json");
});

app.MapPost("/api/dismissals", async (HttpContext ctx) =>
{
    DismissalRequest? req;
    try { req = await ctx.Request.ReadFromJsonAsync<DismissalRequest>(); }
    catch { return Results.BadRequest(new { ok = false, error = "invalid JSON body" }); }

    if (req is null || string.IsNullOrWhiteSpace(req.Id))
        return Results.BadRequest(new { ok = false, error = "id is required" });

    var decidedAt = DateTime.UtcNow.ToString("O");

    await reportLock.WaitAsync(ctx.RequestAborted);
    try
    {
        var raw  = await File.ReadAllTextAsync(reportPath, ctx.RequestAborted);
        var doc  = JsonNode.Parse(raw)?.AsObject()
                   ?? throw new InvalidOperationException("report root is not an object");

        // Validate finding exists
        var findings = doc["findings"]?.AsArray();
        if (findings is null || !findings.Any(f => f?["id"]?.GetValue<string>() == req.Id))
            return Results.NotFound(new { ok = false, error = "finding not found" });

        // Ensure decisions map exists
        if (doc["decisions"] is not JsonObject decisions)
        {
            decisions = new JsonObject();
            doc["decisions"] = decisions;
        }

        // Idempotent: skip write if already dismissed
        var existing = decisions[req.Id]?.AsObject();
        if (existing?["status"]?.GetValue<string>() == "dismissed")
            return Results.Ok(new { ok = true, id = req.Id, decided_at = existing["decided_at"]?.GetValue<string>() });

        decisions[req.Id] = new JsonObject
        {
            ["status"]           = "dismissed",
            ["dismissed_reason"] = req.DismissedReason ?? "",
            ["decided_at"]       = decidedAt
        };

        // Atomic write
        var tmp = Path.Combine(Path.GetDirectoryName(reportPath)!, Path.GetRandomFileName());
        await File.WriteAllTextAsync(tmp, doc.ToJsonString(jsonWriteOptions), ctx.RequestAborted);
        File.Move(tmp, reportPath, overwrite: true);
    }
    finally { reportLock.Release(); }

    // Cross-run history write
    var suggestionKey = "";
    try
    {
        var raw2     = await File.ReadAllTextAsync(reportPath, ctx.RequestAborted);
        var doc2     = JsonNode.Parse(raw2)?.AsObject();
        var findings2 = doc2?["findings"]?.AsArray();
        suggestionKey = findings2?
            .FirstOrDefault(f => f?["id"]?.GetValue<string>() == req.Id)
            ?["suggestion_key"]?.GetValue<string>() ?? "";
    }
    catch { /* best-effort */ }

    if (!string.IsNullOrEmpty(suggestionKey))
    {
        await historyLock.WaitAsync(CancellationToken.None);
        try
        {
            Directory.CreateDirectory(historyDir);
            JsonArray history;
            if (File.Exists(historyFile))
            {
                var hRaw = await File.ReadAllTextAsync(historyFile);
                history  = JsonNode.Parse(hRaw)?.AsArray() ?? new JsonArray();
            }
            else history = new JsonArray();

            var alreadyPresent = history.Any(e => e?["suggestion_key"]?.GetValue<string>() == suggestionKey);
            if (!alreadyPresent)
            {
                history.Add(new JsonObject
                {
                    ["suggestion_key"] = suggestionKey,
                    ["dismissed_at"]   = decidedAt
                });
                var tmp2 = Path.Combine(historyDir, Path.GetRandomFileName());
                await File.WriteAllTextAsync(tmp2, history.ToJsonString(jsonWriteOptions));
                File.Move(tmp2, historyFile, overwrite: true);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[report-server] history write failed: {ex.Message}");
        }
        finally { historyLock.Release(); }
    }

    return Results.Ok(new { ok = true, id = req.Id, decided_at = decidedAt });
});

app.MapPost("/api/ship-prompt", async (HttpContext ctx) =>
{
    // Read prompt from body (plain text or JSON)
    string? prompt = null;
    var ct = ctx.RequestAborted;
    try
    {
        var contentType = ctx.Request.ContentType ?? "";
        if (contentType.Contains("application/json"))
        {
            var node = await JsonNode.ParseAsync(ctx.Request.Body, cancellationToken: ct);
            prompt = node?["prompt"]?.GetValue<string>();
        }
        else
        {
            using var reader = new System.IO.StreamReader(ctx.Request.Body, Encoding.UTF8);
            prompt = await reader.ReadToEndAsync(ct);
        }
    }
    catch { /* fall through — prompt stays null */ }

    if (string.IsNullOrWhiteSpace(prompt))
        return Results.BadRequest(new { ok = false, error = "prompt is required" });

    var warnings = new List<string>();
    var compressed = prompt;

    // Pass 1: strip markdown
    try
    {
        var s = compressed;
        s = Regex.Replace(s, @"^#{1,6}\s+", "", RegexOptions.Multiline);   // headers
        s = Regex.Replace(s, @"\*\*(.+?)\*\*", "$1");                       // bold
        s = Regex.Replace(s, @"\*(.+?)\*", "$1");                           // italic *
        s = Regex.Replace(s, @"_(.+?)_", "$1");                             // italic _
        s = Regex.Replace(s, @"`(.+?)`", "$1");                             // inline code
        s = Regex.Replace(s, @"^(-{3,}|\*{3,})\s*$", "", RegexOptions.Multiline); // hr
        compressed = s;
    }
    catch (Exception ex)
    {
        warnings.Add($"markdown-strip failed: {ex.Message}");
    }

    // Pass 2: collapse whitespace
    try
    {
        var s = compressed;
        // Trim each line
        s = Regex.Replace(s, @"[ \t]+", " ");
        s = string.Join("\n", s.Split('\n').Select(l => l.Trim()));
        // Collapse 3+ newlines to 2
        s = Regex.Replace(s, @"\n{3,}", "\n\n");
        s = s.Trim();
        compressed = s;
    }
    catch (Exception ex)
    {
        warnings.Add($"whitespace-collapse failed: {ex.Message}");
    }

    // Persist to report JSON
    try
    {
        await reportLock.WaitAsync(ct);
        try
        {
            var raw = await File.ReadAllTextAsync(reportPath, ct);
            var doc = JsonNode.Parse(raw)?.AsObject()
                      ?? throw new InvalidOperationException("report root is not an object");
            doc["shipped_prompt"] = new JsonObject
            {
                ["readable"]    = prompt,
                ["transformed"] = compressed,
                ["shipped_at"]  = DateTime.UtcNow.ToString("O")
            };
            var tmp = Path.Combine(Path.GetDirectoryName(reportPath)!, Path.GetRandomFileName());
            await File.WriteAllTextAsync(tmp, doc.ToJsonString(jsonWriteOptions), ct);
            File.Move(tmp, reportPath, overwrite: true);
        }
        finally { reportLock.Release(); }
    }
    catch (Exception ex)
    {
        warnings.Add($"persist failed: {ex.Message}");
    }

    // Clipboard via OSC-52
    try
    {
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(compressed));
        Console.Write($"\x1b]52;c;{b64}\x07");
    }
    catch (Exception ex)
    {
        warnings.Add($"clipboard failed: {ex.Message}");
    }

    return Results.Ok(new { ok = true, transformed = compressed, warnings });
});

app.MapGet("/shutdown", async (HttpContext ctx) =>
{
    await ctx.Response.WriteAsJsonAsync(new { message = "shutting down" });
    await ctx.Response.CompleteAsync();
    cts.Cancel();
    return Results.Empty;
});

// ── Idle-timeout background monitor ─────────────────────────────────────────
_ = Task.Run(async () =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), CancellationToken.None);
        if (browserConnected && DateTime.UtcNow - lastActivity > TimeSpan.FromMinutes(idleMinutes))
        {
            Console.WriteLine($"[report-server] idle timeout ({idleMinutes} min) reached. Shutting down.");
            cts.Cancel();
            break;
        }
    }
});

// ── Start ───────────────────────────────────────────────────────────────────
Console.WriteLine($"[report-server] listening on http://{bindAddr}:{port}");
Console.WriteLine($"[report-server] serving: {reportPath}");

try
{
    await app.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
    // expected on shutdown / idle timeout
}

Console.WriteLine("[report-server] stopped.");
return 0;

// ── Helpers ─────────────────────────────────────────────────────────────────
static string? GetArg(string[] args, string name)
{
    var idx = Array.IndexOf(args, name);
    return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
}

static bool IsPortBound(string address, int port)
{
    try
    {
        var ip  = address == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(address);
        using var sock = new TcpClient();
        sock.Connect(ip, port);
        return true;   // connection succeeded → port is already listening
    }
    catch
    {
        return false;
    }
}

// ── Request models ───────────────────────────────────────────────────────────
record DismissalRequest(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] string Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("dismissed_reason")] string? DismissedReason);
