namespace ReportServer;

public sealed record ServerOptions(
    string ReportPath,
    int Port,
    string BindAddress,
    int IdleMinutes,
    bool UsesDevelopmentFixture)
{
    public static async Task<ServerOptions> ParseAsync(
        string[] args,
        bool isDevelopment,
        string contentRootPath,
        CancellationToken cancellationToken = default)
    {
        var port = int.TryParse(GetArgument(args, "--port"), out var parsedPort) ? parsedPort : 5173;
        var idleMinutes = int.TryParse(GetArgument(args, "--idle-minutes"), out var parsedIdleMinutes) ? parsedIdleMinutes : 10;
        var bindAddress = GetArgument(args, "--bind") is { Length: > 0 } configuredAddress
            ? configuredAddress
            : "127.0.0.1";
        var reportPath = GetArgument(args, "--report-path");

        if (port is < 1 or > 65535)
            throw new ServerOptionsException("--port must be between 1 and 65535.");
        if (idleMinutes < 1)
            throw new ServerOptionsException("--idle-minutes must be at least 1.");

        if (string.IsNullOrWhiteSpace(reportPath) && isDevelopment)
        {
            var fixturePath = Path.Combine(contentRootPath, "Fixtures", "improvement-report-data.json");
            if (!File.Exists(fixturePath))
                throw new ServerOptionsException($"Development fixture not found: {fixturePath}");

            var temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                "report-server",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);
            reportPath = Path.Combine(temporaryDirectory, "improvement-report-data.json");
            await using var source = File.OpenRead(fixturePath);
            await using var destination = File.Create(reportPath);
            await source.CopyToAsync(destination, cancellationToken);

            return new ServerOptions(reportPath, port, bindAddress, idleMinutes, true);
        }

        if (string.IsNullOrWhiteSpace(reportPath))
            throw new ServerOptionsException("--report-path is required outside Development.");
        if (!File.Exists(reportPath))
            throw new ServerOptionsException($"Report file not found: {reportPath}");

        return new ServerOptions(Path.GetFullPath(reportPath), port, bindAddress, idleMinutes, false);
    }

    private static string? GetArgument(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}

public sealed class ServerOptionsException(string message) : Exception(message);
