using System.Text;
using Nexus.Core.Models;

namespace Nexus.Linker.Services;

public class ContextCompiler : IContextCompiler
{
    public string Compile(List<Rule> rules, List<Node> nodes, List<Decision> decisions)
    {
        var sb = new StringBuilder();

        // === RULES ===
        sb.AppendLine("=== RULES ===");
        foreach (var rule in rules.OrderByDescending(r => r.Priority))
        {
            sb.AppendLine($"@RULE {rule.RuleId} PRI:{rule.Priority:F1}");
            sb.AppendLine($"    \"{rule.Description}\"");
        }
        sb.AppendLine();

        // === NODES ===
        sb.AppendLine("=== NODES ===");
        foreach (var node in nodes)
        {
            sb.AppendLine($"@NODE {node.NodeId} T:{node.Type} P:{node.Path} L:{node.LineStart}-{node.LineEnd}");
            
            if (node.Tags.Any())
            {
                sb.AppendLine($"    TAGS: {string.Join(", ", node.Tags)}");
            }
            
            if (node.Deps.Any())
            {
                sb.AppendLine($"    DEP: {string.Join(", ", node.Deps)}");
            }

            if (node.RuleAmplitudes.Any())
            {
                var decs = node.RuleAmplitudes.Select(kv => $"{kv.Key}({kv.Value:F1})");
                sb.AppendLine($"    DEC: {string.Join(", ", decs)}");
            }

            sb.AppendLine($"    SUM: \"{node.Summary}\"");
            sb.AppendLine();
        }

        // === DECISIONS ===
        sb.AppendLine("=== DECISIONS ===");
        foreach (var decision in decisions)
        {
            sb.AppendLine($"@DECISION {decision.DecisionId}");
            sb.AppendLine($"    RULE: {decision.RuleId ?? "NONE"}");
            sb.AppendLine($"    CONTEXT: \"{decision.Context}\"");
            
            if (decision.ScopeNodes.Any())
            {
                sb.AppendLine($"    SCOPE: {string.Join(", ", decision.ScopeNodes)}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
