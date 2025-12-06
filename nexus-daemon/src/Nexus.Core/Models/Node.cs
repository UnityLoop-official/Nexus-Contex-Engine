using System.Text.Json.Serialization;

namespace Nexus.Core.Models;

public class Node
{
    [JsonPropertyName("node_id")]
    public string NodeId { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NodeType Type { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("line_start")]
    public int LineStart { get; set; }

    [JsonPropertyName("line_end")]
    public int LineEnd { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("deps")]
    public List<string> Deps { get; set; } = new();

    // Map rule_id -> amplitude (0.0-1.0)
    [JsonPropertyName("rule_amplitudes")]
    public Dictionary<string, double> RuleAmplitudes { get; set; } = new();

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}
