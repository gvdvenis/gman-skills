using System.Text.Json;

namespace ReportServer;

public sealed class ReportStore(string reportPath, string historyPath)
{
    private readonly SemaphoreSlim reportLock = new(1, 1);
    private readonly SemaphoreSlim historyLock = new(1, 1);

    public async Task<string> ReadRawAsync(CancellationToken cancellationToken)
    {
        return await File.ReadAllTextAsync(reportPath, cancellationToken);
    }

    public async Task<DismissalResult> DismissAsync(DismissalRequest request, CancellationToken cancellationToken)
    {
        await reportLock.WaitAsync(cancellationToken);
        try
        {
            var report = await ReadReportAsync(cancellationToken);
            var finding = report.Findings.SingleOrDefault(f => f.Id == request.Id);
            if (finding is null)
                return DismissalResult.NotFound;

            if (report.Decisions.TryGetValue(request.Id, out var existing) && existing.Action == "dismissed")
            {
                await UpdateDismissalHistoryAsync(finding.SuggestionKey, existing.DecidedAt, cancellationToken);
                return new DismissalResult(true, existing.DecidedAt);
            }

            var decidedAt = DateTime.UtcNow.ToString("O");
            report.Decisions[request.Id] = new ReportDecision
            {
                Action = "dismissed",
                DecidedAt = decidedAt,
                DismissedReason = request.DismissedReason
            };

            await WriteReportAsync(report, cancellationToken);
            await UpdateDismissalHistoryAsync(finding.SuggestionKey, decidedAt, cancellationToken);
            return new DismissalResult(false, decidedAt);
        }
        finally
        {
            reportLock.Release();
        }
    }

    public async Task<string> ShipPromptAsync(ShipPromptRequest request, string transformed, CancellationToken cancellationToken)
    {
        await reportLock.WaitAsync(cancellationToken);
        try
        {
            var report = await ReadReportAsync(cancellationToken);
            var shippedAt = DateTime.UtcNow.ToString("O");
            report.ShippedPrompt = new ShippedPrompt
            {
                Readable = request.Prompt,
                Transformed = transformed,
                ShippedAt = shippedAt
            };

            foreach (var id in request.QueuedIds.Distinct(StringComparer.Ordinal))
            {
                if (!report.Decisions.ContainsKey(id))
                {
                    report.Decisions[id] = new ReportDecision
                    {
                        Action = "queued",
                        DecidedAt = shippedAt
                    };
                }
            }

            await WriteReportAsync(report, cancellationToken);
            return shippedAt;
        }
        finally
        {
            reportLock.Release();
        }
    }

    private async Task<ReportDocument> ReadReportAsync(CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(reportPath);
        return await JsonSerializer.DeserializeAsync(
            stream,
            ReportJsonContext.Default.ReportDocument,
            cancellationToken)
            ?? throw new InvalidOperationException("Report root is invalid.");
    }

    private async Task WriteReportAsync(ReportDocument report, CancellationToken cancellationToken)
    {
        var temporaryPath = Path.Combine(Path.GetDirectoryName(reportPath)!, Path.GetRandomFileName());
        try
        {
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    report,
                    ReportJsonContext.Default.ReportDocument,
                    cancellationToken);
            }

            File.Move(temporaryPath, reportPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private async Task UpdateDismissalHistoryAsync(string? suggestionKey, string dismissedAt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(suggestionKey))
            return;

        await historyLock.WaitAsync(cancellationToken);
        try
        {
            var historyDirectory = Path.GetDirectoryName(historyPath)!;
            Directory.CreateDirectory(historyDirectory);

            List<DismissalHistoryEntry> history;
            if (File.Exists(historyPath))
            {
                await using var historyStream = File.OpenRead(historyPath);
                history = await JsonSerializer.DeserializeAsync(
                    historyStream,
                    ReportJsonContext.Default.ListDismissalHistoryEntry,
                    cancellationToken)
                    ?? [];
            }
            else
            {
                history = [];
            }

            var existingIndex = history.FindIndex(entry => entry.SuggestionKey == suggestionKey);
            var entry = new DismissalHistoryEntry
            {
                SuggestionKey = suggestionKey,
                DismissedAt = dismissedAt
            };

            if (existingIndex >= 0)
                history[existingIndex] = entry;
            else
                history.Add(entry);

            var temporaryPath = Path.Combine(historyDirectory, Path.GetRandomFileName());
            try
            {
                await using (var stream = File.Create(temporaryPath))
                {
                    await JsonSerializer.SerializeAsync(
                        stream,
                        history,
                        ReportJsonContext.Default.ListDismissalHistoryEntry,
                        cancellationToken);
                }

                File.Move(temporaryPath, historyPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }
        finally
        {
            historyLock.Release();
        }
    }
}

public sealed record DismissalResult(bool IsDuplicate, string? DecidedAt)
{
    public static DismissalResult NotFound { get; } = new(false, null);
}
