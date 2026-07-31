using System.Net;
using System.Net.Sockets;
using System.Text.Json;

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

// ── State ───────────────────────────────────────────────────────────────────
var browserConnected = false;
var lastActivity     = DateTime.UtcNow;
var cts              = new CancellationTokenSource();

// ── Middleware: track idle activity after first browser connection ───────────
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path != "/favicon.ico")
    {
        lastActivity = DateTime.UtcNow;
        if (!browserConnected && ctx.Request.Path == "/api/report")
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
