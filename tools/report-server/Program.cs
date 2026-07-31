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
    var suggestionKey = "";
    var isDuplicate = false;
    string? priorDecidedAt = null;  // set when already dismissed; used as the echoed timestamp

    await reportLock.WaitAsync(ctx.RequestAborted);
    try
    {
        var raw  = await File.ReadAllTextAsync(reportPath, ctx.RequestAborted);
        var doc  = JsonNode.Parse(raw)?.AsObject()
                   ?? throw new InvalidOperationException("report root is not an object");

        // Validate finding exists and capture suggestion_key inside the lock
        var findings = doc["findings"]?.AsArray();
        if (findings is null || !findings.Any(f => f?["id"]?.GetValue<string>() == req.Id))
            return Results.NotFound(new { ok = false, error = "finding not found" });

        suggestionKey = findings
            .FirstOrDefault(f => f?["id"]?.GetValue<string>() == req.Id)
            ?["suggestion_key"]?.GetValue<string>() ?? "";

        // Ensure decisions map exists
        if (doc["decisions"] is not JsonObject decisions)
        {
            decisions = new JsonObject();
            doc["decisions"] = decisions;
        }

        // Idempotent: if already dismissed, skip report write but fall through to refresh history
        var existing = decisions[req.Id]?.AsObject();
        if (existing?["action"]?.GetValue<string>() == "dismissed")
        {
            isDuplicate = true;
            priorDecidedAt = existing["decided_at"]?.GetValue<string>();
            // No report write needed; history refresh still runs below
        }
        else
        {
            var decisionNode = new JsonObject
            {
                ["action"]     = "dismissed",
                ["decided_at"] = decidedAt
            };
            // Only set dismissed_reason when provided — omitting it avoids conflating "not provided" with "empty"
            if (!string.IsNullOrEmpty(req.DismissedReason))
                decisionNode["dismissed_reason"] = req.DismissedReason;

            decisions[req.Id] = decisionNode;

            // Atomic write
            var tmp = Path.Combine(Path.GetDirectoryName(reportPath)!, Path.GetRandomFileName());
            await File.WriteAllTextAsync(tmp, doc.ToJsonString(jsonWriteOptions), ctx.RequestAborted);
            File.Move(tmp, reportPath, overwrite: true);
        }
    }
    finally { reportLock.Release(); }

    // Cross-run history write — always refresh dismissed_at so the 30-day cooldown anchors to the most recent dismissal
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

            var existingEntry = history.FirstOrDefault(e => e?["suggestion_key"]?.GetValue<string>() == suggestionKey);
            // Use the stored timestamp for duplicates (to honour what's in the report) or current time for first dismissal.
            var historyTimestamp = isDuplicate ? (priorDecidedAt ?? decidedAt) : decidedAt;
            if (existingEntry is JsonObject existingObj)
            {
                // Refresh dismissed_at so the 30-day cooldown window is anchored to the most recent dismissal
                existingObj["dismissed_at"] = historyTimestamp;
            }
            else
            {
                history.Add(new JsonObject
                {
                    ["suggestion_key"] = suggestionKey,
                    ["dismissed_at"]   = historyTimestamp
                });
            }

            var tmp2 = Path.Combine(historyDir, Path.GetRandomFileName());
            await File.WriteAllTextAsync(tmp2, history.ToJsonString(jsonWriteOptions));
            File.Move(tmp2, historyFile, overwrite: true);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[report-server] history write failed: {ex.Message}");
        }
        finally { historyLock.Release(); }
    }

    return isDuplicate
        ? Results.Ok(new { ok = true, id = req.Id, decided_at = priorDecidedAt ?? decidedAt })
        : Results.Ok(new { ok = true, id = req.Id, decided_at = decidedAt });
});

app.MapPost("/api/ship-prompt", async (HttpContext ctx) =>
{
    // Read prompt + queued_ids from JSON body
    string? prompt = null;
    string[] queuedIds = Array.Empty<string>();
    var ct = ctx.RequestAborted;
    try
    {
        var node = await JsonNode.ParseAsync(ctx.Request.Body, cancellationToken: ct);
        prompt = node?["prompt"]?.GetValue<string>();
        var idsNode = node?["queued_ids"]?.AsArray();
        if (idsNode is not null)
            queuedIds = idsNode
                .Select(n => n?.GetValue<string>())
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s!)
                .ToArray();
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
        s = Regex.Replace(s, @"[ \t]+", " ");
        s = string.Join("\n", s.Split('\n').Select(l => l.Trim()));
        s = Regex.Replace(s, @"\n{3,}", "\n\n");
        s = s.Trim();
        compressed = s;
    }
    catch (Exception ex)
    {
        warnings.Add($"whitespace-collapse failed: {ex.Message}");
    }

    // Persist shipped_prompt and per-finding queued decisions to report JSON
    var shippedAt = DateTime.UtcNow.ToString("O");
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
                ["shipped_at"]  = shippedAt
            };

            // Write a decisions entry for each queued finding so the audit trail is complete
            if (queuedIds.Length > 0)
            {
                if (doc["decisions"] is not JsonObject decisionsObj)
                {
                    decisionsObj = new JsonObject();
                    doc["decisions"] = decisionsObj;
                }
                foreach (var id in queuedIds)
                {
                    // Only write if not already decided (dismissed takes precedence)
                    if (decisionsObj[id] is null)
                        decisionsObj[id] = new JsonObject { ["action"] = "queued", ["decided_at"] = shippedAt };
                }
            }

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

// ── QR code for mobile access (all-interfaces binding only) ────────────────
if (bindAddr == "0.0.0.0")
{
    var localIp = GetLocalIpAddress();
    var url = $"http://{localIp}:{port}";
    Console.WriteLine($"[report-server] mobile URL: {url}");
    PrintQrCode(url);
}

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
        // When binding to all-interfaces (0.0.0.0), probe loopback (127.0.0.1) — a locally-running
        // server will accept a loopback connection, whereas TcpClient.Connect(IPAddress.Any, n)
        // resolves differently per OS and is unreliable on Windows.
        var probeAddress = (address == "0.0.0.0") ? "127.0.0.1" : address;
        var ip = IPAddress.Parse(probeAddress);
        using var sock = new TcpClient();
        sock.Connect(ip, port);
        return true;   // connection succeeded → port is already listening
    }
    catch
    {
        return false;
    }
}

static string GetLocalIpAddress()
{
    try
    {
        foreach (var addr in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (addr.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addr))
                return addr.ToString();
        }
    }
    catch { }
    return "127.0.0.1";
}

// ── Minimal QR code generator (byte mode, version 3, ECC-M) ────────────────
// Based on the ISO/IEC 18004 specification.
static void PrintQrCode(string url)
{
    try
    {
        var modules = GenerateQr(url);
        if (modules == null)
        {
            PrintUrlBox(url);
            return;
        }
        int size = modules.GetLength(0);
        // Add 4-module quiet zone
        int qz = 4;
        int total = size + qz * 2;

        Console.WriteLine();
        Console.WriteLine("  ┌─ Scan with your phone ─────────┐");

        // Render 2 rows per terminal line using half-block chars
        for (int row = 0; row < total; row += 2)
        {
            var sb = new StringBuilder("  │ ");
            for (int col = 0; col < total; col++)
            {
                bool top    = GetModule(modules, row,     col, qz, size);
                bool bottom = GetModule(modules, row + 1, col, qz, size);
                // dark = black QR square, light = white background
                // Terminal: dark square → filled, light → space
                // ▀ top dark, bottom light; ▄ top light, bottom dark; █ both dark; ' ' both light
                sb.Append((top, bottom) switch
                {
                    (true,  true)  => '█',
                    (true,  false) => '▀',
                    (false, true)  => '▄',
                    _              => ' ',
                });
            }
            sb.Append(" │");
            Console.WriteLine(sb);
        }

        Console.WriteLine("  └────────────────────────────────┘");
        Console.WriteLine($"  {url}");
        Console.WriteLine();
    }
    catch
    {
        PrintUrlBox(url);
    }
}

static bool GetModule(bool[,] modules, int row, int col, int qz, int size)
{
    int r = row - qz;
    int c = col - qz;
    if (r < 0 || r >= size || c < 0 || c >= size) return false; // quiet zone = light
    return modules[r, c];
}

static void PrintUrlBox(string url)
{
    Console.WriteLine();
    Console.WriteLine($"  ╔══ Mobile Access ══╗");
    Console.WriteLine($"  ║  {url}  ║");
    Console.WriteLine($"  ╚═══════════════════╝");
    Console.WriteLine("  (Open URL on phone or use a QR generator)");
    Console.WriteLine();
}

// Returns bool[size,size] where true = dark module, or null on failure
static bool[,]? GenerateQr(string url)
{
    // Version 3, ECC M: 29×29, 22 data codewords, 22 ECC codewords (1 block)
    const int Version = 3;
    const int Size = 29;
    const int DataCodewords = 22;
    const int EccCodewords = 22;

    // ── 1. Encode data (byte mode) ──────────────────────────────────────────
    var dataBytes = Encoding.UTF8.GetBytes(url);
    if (dataBytes.Length > DataCodewords - 3) return null; // too long for v3-M byte mode
    // byte mode indicator: 0100, char count (8 bits), data, terminator
    var bits = new List<bool>();
    AddBits(bits, 0b0100, 4);
    AddBits(bits, dataBytes.Length, 8);
    foreach (var b in dataBytes) AddBits(bits, b, 8);
    // terminator
    for (int i = 0; i < 4 && bits.Count < DataCodewords * 8; i++) bits.Add(false);
    // pad to byte boundary
    while (bits.Count % 8 != 0) bits.Add(false);
    // pad codewords
    bool toggle = true;
    while (bits.Count < DataCodewords * 8)
    {
        AddBits(bits, toggle ? 0b11101100 : 0b00010001, 8);
        toggle = !toggle;
    }

    var dataCodewords = BitsToBytes(bits, DataCodewords);

    // ── 2. ECC (Reed-Solomon) ───────────────────────────────────────────────
    // Generator polynomial for 22 ECC codewords (GF(256), prim poly x^8+x^4+x^3+x^2+1 = 285)
    int[] genPoly = GetGeneratorPoly(EccCodewords);
    var eccBytes = ReedSolomon(dataCodewords, genPoly, EccCodewords);

    // Combine data + ECC
    var codewords = new byte[DataCodewords + EccCodewords];
    Array.Copy(dataCodewords, 0, codewords, 0, DataCodewords);
    Array.Copy(eccBytes, 0, codewords, DataCodewords, EccCodewords);

    // ── 3. Module placement ─────────────────────────────────────────────────
    var modules = new bool[Size, Size];
    var reserved = new bool[Size, Size]; // true = not data

    PlaceFinderPattern(modules, reserved, 0, 0);
    PlaceFinderPattern(modules, reserved, 0, Size - 7);
    PlaceFinderPattern(modules, reserved, Size - 7, 0);
    PlaceSeparators(reserved, Size);
    PlaceTimingPatterns(modules, reserved, Size);
    PlaceFormatReserve(reserved, Size);
    PlaceDarkModule(modules, reserved, Version);

    // Data placement (zigzag)
    var dataBits = new List<bool>();
    foreach (var cw in codewords)
        for (int i = 7; i >= 0; i--) dataBits.Add((cw >> i & 1) == 1);

    PlaceData(modules, reserved, dataBits, Size);

    // ── 4. Best mask ────────────────────────────────────────────────────────
    int bestMask = 0;
    int bestPenalty = int.MaxValue;
    bool[,]? bestModules = null;
    for (int m = 0; m < 8; m++)
    {
        var masked = ApplyMask(modules, reserved, m, Size);
        var penalty = CalcPenalty(masked, Size);
        if (penalty < bestPenalty) { bestPenalty = penalty; bestMask = m; bestModules = masked; }
    }

    // ── 5. Format info ──────────────────────────────────────────────────────
    PlaceFormatInfo(bestModules!, reserved, Size, 2 /* ECC-M */, bestMask);

    return bestModules;
}

static void AddBits(List<bool> bits, int value, int count)
{
    for (int i = count - 1; i >= 0; i--)
        bits.Add((value >> i & 1) == 1);
}

static byte[] BitsToBytes(List<bool> bits, int count)
{
    var result = new byte[count];
    for (int i = 0; i < count; i++)
    {
        byte b = 0;
        for (int j = 0; j < 8; j++)
            if (bits[i * 8 + j]) b |= (byte)(1 << (7 - j));
        result[i] = b;
    }
    return result;
}

static int[] GetGeneratorPoly(int degree)
{
    // GF(256) with primitive polynomial 285 (x^8+x^4+x^3+x^2+1)
    int[] gf_exp = new int[512];
    int[] gf_log = new int[256];
    int x = 1;
    for (int i = 0; i < 255; i++)
    {
        gf_exp[i] = x;
        gf_log[x] = i;
        x <<= 1;
        if (x >= 256) x ^= 285;
    }
    for (int i = 255; i < 512; i++) gf_exp[i] = gf_exp[i - 255];

    int GfMul(int a, int b) => (a == 0 || b == 0) ? 0 : gf_exp[(gf_log[a] + gf_log[b]) % 255];

    // Start with polynomial [1]
    int[] g = new int[1] { 1 };
    for (int i = 0; i < degree; i++)
    {
        var term = new int[] { 1, gf_exp[i] };
        var result = new int[g.Length + 1];
        for (int j = 0; j < g.Length; j++)
        {
            result[j] ^= GfMul(g[j], term[0]);
            result[j + 1] ^= GfMul(g[j], term[1]);
        }
        g = result;
    }
    return g;
}

static byte[] ReedSolomon(byte[] data, int[] genPoly, int eccCount)
{
    int[] gf_exp = new int[512];
    int[] gf_log = new int[256];
    int x = 1;
    for (int i = 0; i < 255; i++)
    {
        gf_exp[i] = x; gf_log[x] = i;
        x <<= 1; if (x >= 256) x ^= 285;
    }
    for (int i = 255; i < 512; i++) gf_exp[i] = gf_exp[i - 255];

    int GfMul(int a, int b) => (a == 0 || b == 0) ? 0 : gf_exp[(gf_log[a] + gf_log[b]) % 255];

    var msg = new int[data.Length + eccCount];
    for (int i = 0; i < data.Length; i++) msg[i] = data[i];

    for (int i = 0; i < data.Length; i++)
    {
        int coef = msg[i];
        if (coef == 0) continue;
        for (int j = 1; j < genPoly.Length; j++)
            msg[i + j] ^= GfMul(genPoly[j], coef);
    }

    var ecc = new byte[eccCount];
    for (int i = 0; i < eccCount; i++) ecc[i] = (byte)msg[data.Length + i];
    return ecc;
}

static void PlaceFinderPattern(bool[,] m, bool[,] r, int row, int col)
{
    for (int dr = -1; dr <= 7; dr++)
    for (int dc = -1; dc <= 7; dc++)
    {
        int rr = row + dr, cc = col + dc;
        if (rr < 0 || rr >= m.GetLength(0) || cc < 0 || cc >= m.GetLength(1)) continue;
        bool dark = (dr >= 0 && dr <= 6 && dc >= 0 && dc <= 6) &&
                    (dr == 0 || dr == 6 || dc == 0 || dc == 6 || (dr >= 2 && dr <= 4 && dc >= 2 && dc <= 4));
        m[rr, cc] = dark;
        r[rr, cc] = true;
    }
}

static void PlaceSeparators(bool[,] r, int size)
{
    // Horizontal/vertical separator rows/cols around finders
    for (int i = 0; i < 8; i++)
    {
        if (i < size) { r[7, i] = true; r[i, 7] = true; }          // top-left
        if (i < size) { r[7, size - 8 + i] = true; r[i, size - 8] = true; } // top-right
        if (i < size) { r[size - 8, i] = true; r[size - 8 + i, 7] = true; } // bottom-left
    }
}

static void PlaceTimingPatterns(bool[,] m, bool[,] r, int size)
{
    for (int i = 8; i < size - 8; i++)
    {
        bool dark = i % 2 == 0;
        m[6, i] = dark; r[6, i] = true;
        m[i, 6] = dark; r[i, 6] = true;
    }
}

static void PlaceFormatReserve(bool[,] r, int size)
{
    // Reserve format information areas
    for (int i = 0; i < 9; i++)
    {
        r[8, i] = true; r[i, 8] = true;
    }
    for (int i = size - 8; i < size; i++)
    {
        r[8, i] = true; r[i, 8] = true;
    }
}

static void PlaceDarkModule(bool[,] m, bool[,] r, int version)
{
    int row = 4 * version + 9;
    m[row, 8] = true;
    r[row, 8] = true;
}

static void PlaceData(bool[,] m, bool[,] r, List<bool> dataBits, int size)
{
    int idx = 0;
    bool upward = true;
    for (int col = size - 1; col >= 1; col -= 2)
    {
        if (col == 6) col--; // skip timing column
        for (int row2 = 0; row2 < size; row2++)
        {
            int row = upward ? size - 1 - row2 : row2;
            for (int dc = 0; dc < 2; dc++)
            {
                int c = col - dc;
                if (r[row, c]) continue;
                m[row, c] = idx < dataBits.Count && dataBits[idx++];
            }
        }
        upward = !upward;
    }
}

static bool[,] ApplyMask(bool[,] m, bool[,] r, int mask, int size)
{
    var result = (bool[,])m.Clone();
    for (int row = 0; row < size; row++)
    for (int col = 0; col < size; col++)
    {
        if (r[row, col]) continue;
        bool flip = mask switch
        {
            0 => (row + col) % 2 == 0,
            1 => row % 2 == 0,
            2 => col % 3 == 0,
            3 => (row + col) % 3 == 0,
            4 => (row / 2 + col / 3) % 2 == 0,
            5 => (row * col) % 2 + (row * col) % 3 == 0,
            6 => ((row * col) % 2 + (row * col) % 3) % 2 == 0,
            7 => ((row + col) % 2 + (row * col) % 3) % 2 == 0,
            _ => false
        };
        if (flip) result[row, col] = !result[row, col];
    }
    return result;
}

static int CalcPenalty(bool[,] m, int size)
{
    int penalty = 0;
    // Rule 1: 5+ consecutive same-color in row/col
    for (int r = 0; r < size; r++)
    {
        int run = 1;
        for (int c = 1; c < size; c++)
        {
            if (m[r, c] == m[r, c - 1]) { run++; if (run == 5) penalty += 3; else if (run > 5) penalty++; }
            else run = 1;
        }
        run = 1;
        for (int c = 1; c < size; c++)
        {
            if (m[c, r] == m[c - 1, r]) { run++; if (run == 5) penalty += 3; else if (run > 5) penalty++; }
            else run = 1;
        }
    }
    // Rule 2: 2×2 blocks
    for (int r = 0; r < size - 1; r++)
    for (int c = 0; c < size - 1; c++)
        if (m[r, c] == m[r + 1, c] && m[r, c] == m[r, c + 1] && m[r, c] == m[r + 1, c + 1])
            penalty += 3;
    return penalty;
}

static void PlaceFormatInfo(bool[,] m, bool[,] r, int size, int eccLevel, int mask)
{
    // Format info: 5 bits data (eccLevel<<3|mask), 10 bits ECC, XOR with 101010000010010
    int data = (eccLevel << 3) | mask;
    int gen = 0b10100110111;
    int fmt = data << 10;
    for (int i = 14; i >= 10; i--)
        if ((fmt >> i & 1) == 1) fmt ^= gen << (i - 10);
    fmt = (data << 10 | fmt) ^ 0b101010000010010;

    // Place around top-left finder — ISO 18004 §7.9.1
    // Horizontal strip: bits 0-5 → row 8, cols 0-5; bit 6 → row 8, col 7 (skip timing col 6); bit 7 → row 8, col 8
    int[] fmtCols = { 0, 1, 2, 3, 4, 5, 7, 8 };
    for (int i = 0; i < 8; i++)
        m[8, fmtCols[i]] = (fmt >> i & 1) == 1;

    // Vertical strip: bits 8-14 → col 8, rows 7, 5, 4, 3, 2, 1, 0 (skip timing row 6)
    int[] fmtRows = { 7, 5, 4, 3, 2, 1, 0 };
    for (int i = 0; i < 7; i++)
        m[fmtRows[i], 8] = (fmt >> (8 + i) & 1) == 1;

    // Place bottom-left and top-right copies
    // Bottom-left: bits 0-7 (8 modules) → col 8, rows size-1 down to size-8
    // Bit 7 lands on the dark-module position (size-8, 8) which is then overridden to always-dark
    for (int i = 0; i < 8; i++)
        m[size - 1 - i, 8] = (fmt >> i & 1) == 1;
    // Top-right: bits 8-14 (7 modules) → row 8, cols size-7 to size-1
    for (int i = 0; i < 7; i++)
        m[8, size - 7 + i] = (fmt >> (8 + i) & 1) == 1;
    m[size - 8, 8] = true; // dark module — always dark per spec (overrides bit 7 above)
}

// ── Request models ───────────────────────────────────────────────────────────
record DismissalRequest(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] string Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("dismissed_reason")] string? DismissedReason);
