using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReportServer;

public sealed class ReportDocument
{
    [JsonPropertyName("schema_version")]
    public required string SchemaVersion { get; set; }

    [JsonPropertyName("generated_at")]
    public required string GeneratedAt { get; set; }

    [JsonPropertyName("origin")]
    public required ReportOrigin Origin { get; set; }

    [JsonPropertyName("findings")]
    public required List<ReportFinding> Findings { get; set; }

    [JsonPropertyName("decisions")]
    public Dictionary<string, ReportDecision> Decisions { get; set; } = [];

    [JsonPropertyName("shipped_prompt")]
    public ShippedPrompt? ShippedPrompt { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

public sealed class ReportOrigin
{
    [JsonPropertyName("skill_id")]
    public required string SkillId { get; set; }

    [JsonPropertyName("skill_scope")]
    public required string SkillScope { get; set; }

    [JsonPropertyName("skill_path")]
    public required string SkillPath { get; set; }

    [JsonPropertyName("repo_root")]
    public required string RepoRoot { get; set; }

    [JsonPropertyName("run_id")]
    public required string RunId { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

public sealed class ReportFinding
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("suggestion_key")]
    public string? SuggestionKey { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

public sealed class ReportDecision
{
    [JsonPropertyName("action")]
    public required string Action { get; set; }

    [JsonPropertyName("decided_at")]
    public required string DecidedAt { get; set; }

    [JsonPropertyName("dismissed_reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DismissedReason { get; set; }
}

public sealed class ShippedPrompt
{
    [JsonPropertyName("readable")]
    public required string Readable { get; set; }

    [JsonPropertyName("transformed")]
    public required string Transformed { get; set; }

    [JsonPropertyName("shipped_at")]
    public required string ShippedAt { get; set; }
}

public sealed class DismissalHistoryEntry
{
    [JsonPropertyName("suggestion_key")]
    public required string SuggestionKey { get; set; }

    [JsonPropertyName("dismissed_at")]
    public required string DismissedAt { get; set; }
}

/// <summary>Server health-check response.</summary>
public sealed record ApiStatusResponse(string Status);

/// <summary>Shutdown acknowledgement response.</summary>
public sealed record ShutdownResponse(string Message);

/// <summary>Request to dismiss a single finding by id.</summary>
public sealed record DismissalRequest
{
    [Required]
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("dismissed_reason")]
    public string? DismissedReason { get; init; }
}

/// <summary>Request to ship (compress and persist) the assembled prompt.</summary>
public sealed record ShipPromptRequest
{
    [Required]
    [JsonPropertyName("prompt")]
    public required string Prompt { get; init; }

    [JsonPropertyName("queued_ids")]
    public List<string> QueuedIds { get; init; } = [];
}

/// <summary>Response confirming a finding dismissal decision was recorded.</summary>
public sealed record DismissalResponse(string Id, DateTimeOffset DecidedAt);

/// <summary>Response containing the compressed prompt and any warnings.</summary>
public sealed record ShipPromptResponse(string Transformed, List<string> Warnings);
