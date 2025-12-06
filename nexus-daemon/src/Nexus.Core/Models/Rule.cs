using System.Text.Json.Serialization;

namespace Nexus.Core.Models;

public class Rule
{
    [JsonPropertyName("rule_id")]
    public string RuleId { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public double Priority { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
