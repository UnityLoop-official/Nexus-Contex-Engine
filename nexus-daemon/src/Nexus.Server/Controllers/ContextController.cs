using Microsoft.AspNetCore.Mvc;
using Nexus.Core.Models;
using Nexus.Core.Services;
using Nexus.Linker.Services;

namespace Nexus.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class ContextController : ControllerBase
{
    private readonly ICodeIndexer _indexer;
    private readonly IContextCompiler _compiler;
    private readonly IRuleProvider _ruleProvider;

    public ContextController(ICodeIndexer indexer, IContextCompiler compiler, IRuleProvider ruleProvider)
    {
        _indexer = indexer;
        _compiler = compiler;
        _ruleProvider = ruleProvider;
    }

    [HttpPost("compile")]
    public async Task<IActionResult> CompileContext([FromBody] CompileContextRequest request)
    {
        // 1. Index the solution (or load from cache - MVP re-indexes)
        // Assuming SolutionId maps to a local path or we just take a path for MVP
        // For MVP, let's assume request.SolutionId IS the path or we have a map.
        // Let's rely on a header or body field for the actual path for now to be simple.
        
        string solutionPath = request.SolutionPath; // Temporary extension to request model
        if (string.IsNullOrEmpty(solutionPath))
        {
             return BadRequest("SolutionPath is required for MVP.");
        }

        var allNodes = await _indexer.IndexAsync(solutionPath);

        // 2. Filter Nodes based on Targets (Improved Logic)
        // Distinguish between exact NodeId matches and path/summary searches
        var selectedNodes = FilterNodesByTargets(allNodes, request.Targets);

        // If no specific targets provided, return a representative subset (top 20 for safety)
        if (!selectedNodes.Any() && request.Targets.Count == 0)
        {
            selectedNodes = allNodes.Take(20).ToList(); // Cap for safety in MVP
        }

        // 3. Fetch Rules from provider (extensible architecture for future DB/JSON sources)
        var rules = _ruleProvider.GetAll();

        // 4. Compile
        var dsl = _compiler.Compile(rules, selectedNodes, new List<Decision>());

        return Ok(new CompileContextResponse
        {
            Bytecode = dsl,
            Summary = $"Compiled context for {selectedNodes.Count} nodes.",
            Targets = selectedNodes.Select(n => n.NodeId).ToList()
        });
    }

    /// <summary>
    /// Filters nodes based on target identifiers.
    /// Supports both exact NodeId matching and fuzzy path/summary matching.
    /// </summary>
    private static List<Node> FilterNodesByTargets(List<Node> allNodes, List<string> targets)
    {
        if (targets == null || targets.Count == 0)
        {
            return new List<Node>();
        }

        var selectedNodes = new HashSet<Node>();

        foreach (var target in targets)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                continue;
            }

            // Check if target looks like a NodeId (e.g., "FN:123", "TST:456")
            if (LooksLikeNodeId(target))
            {
                // Exact NodeId match (case-insensitive)
                var exactMatches = allNodes.Where(n =>
                    string.Equals(n.NodeId, target, StringComparison.OrdinalIgnoreCase));

                foreach (var match in exactMatches)
                {
                    selectedNodes.Add(match);
                }
            }
            else
            {
                // Fuzzy matching: path or summary contains the target string
                var fuzzyMatches = allNodes.Where(n =>
                    n.Path.Contains(target, StringComparison.OrdinalIgnoreCase) ||
                    n.Summary.Contains(target, StringComparison.OrdinalIgnoreCase));

                foreach (var match in fuzzyMatches)
                {
                    selectedNodes.Add(match);
                }
            }
        }

        return selectedNodes.ToList();
    }

    /// <summary>
    /// Determines if a candidate string looks like a NodeId.
    /// NodeId format: {PREFIX}:{NUMBER} (e.g., FN:123, TST:456, SRV:789)
    /// </summary>
    private static bool LooksLikeNodeId(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        // Simple heuristic: contains colon and starts with known prefixes
        return candidate.StartsWith("FN:", StringComparison.OrdinalIgnoreCase) ||
               candidate.StartsWith("TST:", StringComparison.OrdinalIgnoreCase) ||
               candidate.StartsWith("SRV:", StringComparison.OrdinalIgnoreCase) ||
               candidate.StartsWith("REPO:", StringComparison.OrdinalIgnoreCase) ||
               candidate.StartsWith("CTRL:", StringComparison.OrdinalIgnoreCase) ||
               candidate.StartsWith("API:", StringComparison.OrdinalIgnoreCase) ||
               candidate.StartsWith("DOM:", StringComparison.OrdinalIgnoreCase) ||
               candidate.StartsWith("JOB:", StringComparison.OrdinalIgnoreCase) ||
               candidate.StartsWith("CFG:", StringComparison.OrdinalIgnoreCase) ||
               candidate.StartsWith("UTIL:", StringComparison.OrdinalIgnoreCase);
    }
}

public class CompileContextRequest
{
    public string TaskType { get; set; } = string.Empty;
    public string SolutionId { get; set; } = string.Empty;
    public string SolutionPath { get; set; } = string.Empty; // Extra for MVP
    public List<string> Targets { get; set; } = new();
}

public class CompileContextResponse
{
    public string Bytecode { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> Targets { get; set; } = new();
}
