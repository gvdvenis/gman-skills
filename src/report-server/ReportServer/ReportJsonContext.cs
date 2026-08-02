using System.Text.Json.Serialization;

namespace ReportServer;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ReportDocument))]
[JsonSerializable(typeof(DismissalRequest))]
[JsonSerializable(typeof(ShipPromptRequest))]
[JsonSerializable(typeof(List<DismissalHistoryEntry>))]
[JsonSerializable(typeof(ApiStatusResponse))]
[JsonSerializable(typeof(ShutdownResponse))]
[JsonSerializable(typeof(DismissalResponse))]
[JsonSerializable(typeof(ShipPromptResponse))]
internal partial class ReportJsonContext : JsonSerializerContext;
