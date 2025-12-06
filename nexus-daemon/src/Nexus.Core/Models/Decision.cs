using System.Text.Json.Serialization;

namespace Nexus.Core.Models;

public class Decision
{
    [JsonPropertyName("decision_id")]
    public string DecisionId { get; set; } = string.Empty;

    [JsonPropertyName("rule_id")]
    public string? RuleId { get; set; }

    [JsonPropertyName("context")]
    public string Context { get; set; } = string.Empty;

    [JsonPropertyName("scope_nodes")]
    public List<string> ScopeNodes { get; set; } = new();
}
