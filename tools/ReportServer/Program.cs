using ElBruno.QRCodeGenerator.CLI;
using ReportServer;
using System.Net;
using System.Net.Sockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

ServerOptions options;
try
{
    options = await ServerOptions.ParseAsync(
        args,
        builder.Environment.IsDevelopment(),
        builder.Environment.ContentRootPath);
}
catch (ServerOptionsException exception)
{
    Console.Error.WriteLine($"[report-server] {exception.Message}");
    return 1;
}

if (IsPortBound(options.BindAddress, options.Port))
{
    Console.WriteLine($"[report-server] WARNING: port {options.Port} on {options.BindAddress} is already bound. Assuming the server is already running.");
    return 2;
}

builder.WebHost.UseUrls($"http://{options.BindAddress}:{options.Port}");
var historyPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".copilot",
    "blazor-orchestration",
    "history",
    "dismissed-keys.json");
builder.Services.AddSingleton(new ReportStore(options.ReportPath, historyPath));

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

var browserConnected = false;
var lastActivity = DateTime.UtcNow;
var shutdown = new CancellationTokenSource();

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

app.MapGet("/ping", () => Results.Json(
    new ApiStatusResponse("ok"),
    ReportJsonContext.Default.ApiStatusResponse));

app.MapGet("/api/report", async (ReportStore reportStore, HttpContext context) =>
{
    var json = await reportStore.ReadRawAsync(context.RequestAborted);
    return Results.Content(json, "application/json");
});

app.MapPost("/api/dismissals", async (ReportStore reportStore, HttpContext context) =>
{
    DismissalRequest? request;
    try
    {
        request = await System.Text.Json.JsonSerializer.DeserializeAsync(
            context.Request.Body,
            ReportJsonContext.Default.DismissalRequest,
            context.RequestAborted);
    }
    catch (System.Text.Json.JsonException)
    {
        return Results.Json(new ApiErrorResponse(false, "invalid JSON body"), ReportJsonContext.Default.ApiErrorResponse, statusCode: StatusCodes.Status400BadRequest);
    }

    if (request is null || string.IsNullOrWhiteSpace(request.Id))
        return Results.Json(new ApiErrorResponse(false, "id is required"), ReportJsonContext.Default.ApiErrorResponse, statusCode: StatusCodes.Status400BadRequest);

    var result = await reportStore.DismissAsync(request, context.RequestAborted);
    if (result.DecidedAt is null)
        return Results.Json(new ApiErrorResponse(false, "finding not found"), ReportJsonContext.Default.ApiErrorResponse, statusCode: StatusCodes.Status404NotFound);

    return Results.Json(
        new DismissalResponse(true, request.Id, result.DecidedAt),
        ReportJsonContext.Default.DismissalResponse);
});

app.MapPost("/api/ship-prompt", async (ReportStore reportStore, HttpContext context) =>
{
    ShipPromptRequest? request;
    try
    {
        request = await System.Text.Json.JsonSerializer.DeserializeAsync(
            context.Request.Body,
            ReportJsonContext.Default.ShipPromptRequest,
            context.RequestAborted);
    }
    catch (System.Text.Json.JsonException)
    {
        return Results.Json(new ApiErrorResponse(false, "invalid JSON body"), ReportJsonContext.Default.ApiErrorResponse, statusCode: StatusCodes.Status400BadRequest);
    }

    if (request is null || string.IsNullOrWhiteSpace(request.Prompt))
        return Results.Json(new ApiErrorResponse(false, "prompt is required"), ReportJsonContext.Default.ApiErrorResponse, statusCode: StatusCodes.Status400BadRequest);

    var transformed = PromptCompressor.Compress(request.Prompt);
    await reportStore.ShipPromptAsync(request, transformed, context.RequestAborted);
    var clipboard = Convert.ToBase64String(Encoding.UTF8.GetBytes(transformed));
    Console.Write($"\x1b]52;c;{clipboard}\x07");

    return Results.Json(
        new ShipPromptResponse(true, transformed, []),
        ReportJsonContext.Default.ShipPromptResponse);
});

app.Map("/shutdown", context => ShutdownAsync(context, shutdown));

_ = Task.Run(async () =>
{
    while (!shutdown.Token.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), CancellationToken.None);
        if (browserConnected && DateTime.UtcNow - lastActivity > TimeSpan.FromMinutes(options.IdleMinutes))
        {
            Console.WriteLine($"[report-server] idle timeout ({options.IdleMinutes} min) reached. Shutting down.");
            shutdown.Cancel();
            break;
        }
    }
});

Console.WriteLine($"[report-server] listening on http://{options.BindAddress}:{options.Port}");
Console.WriteLine($"[report-server] serving: {options.ReportPath}");
if (options.UsesDevelopmentFixture)
    Console.WriteLine("[report-server] using an isolated Development fixture copy.");

if (options.BindAddress == "0.0.0.0")
{
    var localIp = GetLocalIpAddress();
    var url = $"http://{localIp}:{options.Port}";
    Console.WriteLine($"[report-server] mobile URL: {url}");
    QRCode.Print(url);
}

try
{
    await app.RunAsync(shutdown.Token);
}
catch (OperationCanceledException)
{
}

Console.WriteLine("[report-server] stopped.");
return 0;

static async Task ShutdownAsync(HttpContext context, CancellationTokenSource shutdown)
{
    context.Response.ContentType = "application/json";
    await System.Text.Json.JsonSerializer.SerializeAsync(
        context.Response.Body,
        new ShutdownResponse("shutting down"),
        ReportJsonContext.Default.ShutdownResponse,
        context.RequestAborted);
    await context.Response.CompleteAsync();
    shutdown.Cancel();
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
