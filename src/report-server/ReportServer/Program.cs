using ElBruno.QRCodeGenerator.CLI;
using ReportServer;
using ReportServer.Endpoints;
using ReportServer.Middleware;
using System.Net;
using System.Net.Sockets;

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
    "self-improve-reports",
    "history",
    "dismissed-keys.json");
builder.Services.AddSingleton(new ReportStore(options.ReportPath, historyPath));

builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolver = ReportJsonContext.Default);

var app = builder.Build();

app.UseExceptionHandler();
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

app.MapReportEndpoints();
app.MapSystemEndpoints(shutdown);

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
