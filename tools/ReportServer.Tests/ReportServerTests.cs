using System.Text.Json;
using ReportServer;

namespace ReportServer.Tests;

[TestClass]
public sealed class ReportServerTests
{
    [TestMethod]
    public async Task ParseAsync_DevelopmentWithoutReportPath_CopiesFixture()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var fixtureDirectory = Directory.CreateDirectory(Path.Combine(root, "Fixtures"));
            var fixturePath = Path.Combine(fixtureDirectory.FullName, "improvement-report-data.json");
            await File.WriteAllTextAsync(fixturePath, CreateReportJson());

            var options = await ServerOptions.ParseAsync([], true, root);

            Assert.IsTrue(options.UsesDevelopmentFixture);
            Assert.AreNotEqual(fixturePath, options.ReportPath);
            Assert.IsTrue(File.Exists(options.ReportPath));
            Assert.AreEqual(await File.ReadAllTextAsync(fixturePath), await File.ReadAllTextAsync(options.ReportPath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task ParseAsync_ProductionWithoutReportPath_Throws()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            await Assert.ThrowsExactlyAsync<ServerOptionsException>(
                () => ServerOptions.ParseAsync([], false, root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void Compress_StripsMarkdownAndCollapsesWhitespace()
    {
        var result = PromptCompressor.Compress("# Heading\n\n**Bold**  `code`\n\n---\n\nText");

        Assert.AreEqual("Heading\n\nBold code\n\nText", result);
    }

    [TestMethod]
    public async Task ReportStore_DismissAndShipPrompt_PersistsDecisions()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var reportPath = Path.Combine(root, "improvement-report-data.json");
            var historyPath = Path.Combine(root, "history", "dismissed-keys.json");
            await File.WriteAllTextAsync(reportPath, CreateReportJson());
            var store = new ReportStore(reportPath, historyPath);

            var dismissed = await store.DismissAsync(new DismissalRequest { Id = "f-001", DismissedReason = "not applicable" }, CancellationToken.None);
            var duplicate = await store.DismissAsync(new DismissalRequest { Id = "f-001" }, CancellationToken.None);
            await store.ShipPromptAsync(
                new ShipPromptRequest { Prompt = "# Prompt\n\nText", QueuedIds = ["f-001", "f-002"] },
                "Prompt\n\nText",
                CancellationToken.None);

            using var document = JsonDocument.Parse(await store.ReadRawAsync(CancellationToken.None));
            var decisions = document.RootElement.GetProperty("decisions");
            Assert.IsFalse(dismissed.IsDuplicate);
            Assert.IsTrue(duplicate.IsDuplicate);
            Assert.AreEqual(dismissed.DecidedAt, duplicate.DecidedAt);
            Assert.AreEqual("dismissed", decisions.GetProperty("f-001").GetProperty("action").GetString());
            Assert.AreEqual("queued", decisions.GetProperty("f-002").GetProperty("action").GetString());
            Assert.AreEqual("Prompt\n\nText", document.RootElement.GetProperty("shipped_prompt").GetProperty("transformed").GetString());
            Assert.IsTrue(File.Exists(historyPath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "report-server-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CreateReportJson() =>
        """
        {
          "schema_version": "1.1",
          "generated_at": "2026-08-01T00:15:00Z",
          "origin": {
            "skill_id": "test",
            "skill_scope": "user",
            "skill_path": "C:/test",
            "repo_root": "C:/repo",
            "run_id": "run-test"
          },
          "findings": [
            { "id": "f-001", "suggestion_key": "test:first" },
            { "id": "f-002", "suggestion_key": "test:second" }
          ],
          "decisions": {},
          "shipped_prompt": null
        }
        """;
}
