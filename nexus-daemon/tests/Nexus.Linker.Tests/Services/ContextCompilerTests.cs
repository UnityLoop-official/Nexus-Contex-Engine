using Nexus.Core.Models;
using Nexus.Linker.Services;
using Xunit;

namespace Nexus.Linker.Tests.Services;

public class ContextCompilerTests
{
    [Fact]
    public void Compile_ShouldGenerateValidDslFormat()
    {
        // Arrange
        var compiler = new ContextCompiler();

        var rules = new List<Rule>
        {
            new Rule { RuleId = "R01", Priority = 0.9, Description = "Test rule 1" },
            new Rule { RuleId = "R02", Priority = 0.6, Description = "Test rule 2" }
        };

        var nodes = new List<Node>
        {
            new Node
            {
                NodeId = "FN:123",
                Type = NodeType.SRV,
                Path = "Services/TestService.cs",
                LineStart = 10,
                LineEnd = 20,
                Summary = "Test method"
            },
            new Node
            {
                NodeId = "FN:456",
                Type = NodeType.REPO,
                Path = "Repositories/TestRepo.cs",
                LineStart = 5,
                LineEnd = 15,
                Summary = "Repository method"
            }
        };

        var decisions = new List<Decision>
        {
            new Decision
            {
                DecisionId = "D001",
                RuleId = "R01",
                Context = "Test decision context",
                ScopeNodes = new List<string> { "FN:123" }
            }
        };

        // Act
        var result = compiler.Compile(rules, nodes, decisions);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Verify sections exist
        Assert.Contains("=== RULES ===", result);
        Assert.Contains("=== NODES ===", result);
        Assert.Contains("=== DECISIONS ===", result);

        // Verify rule entries
        Assert.Contains("@RULE R01", result);
        Assert.Contains("@RULE R02", result);
        Assert.Contains("Test rule 1", result);

        // Verify node entries
        Assert.Contains("@NODE FN:123", result);
        Assert.Contains("@NODE FN:456", result);
        Assert.Contains("T:SRV", result);
        Assert.Contains("T:REPO", result);

        // Verify decision entry
        Assert.Contains("@DECISION D001", result);
        Assert.Contains("RULE: R01", result);
        Assert.Contains("Test decision context", result);
    }

    [Fact]
    public void Compile_WithEmptyLists_ShouldStillGenerateSections()
    {
        // Arrange
        var compiler = new ContextCompiler();

        // Act
        var result = compiler.Compile(
            new List<Rule>(),
            new List<Node>(),
            new List<Decision>());

        // Assert
        Assert.Contains("=== RULES ===", result);
        Assert.Contains("=== NODES ===", result);
        Assert.Contains("=== DECISIONS ===", result);
    }

    [Fact]
    public void Compile_ShouldOrderRulesByPriorityDescending()
    {
        // Arrange
        var compiler = new ContextCompiler();

        var rules = new List<Rule>
        {
            new Rule { RuleId = "R02", Priority = 0.5, Description = "Low priority" },
            new Rule { RuleId = "R01", Priority = 0.9, Description = "High priority" },
            new Rule { RuleId = "R03", Priority = 0.7, Description = "Med priority" }
        };

        // Act
        var result = compiler.Compile(rules, new List<Node>(), new List<Decision>());

        // Assert
        var r01Index = result.IndexOf("@RULE R01");
        var r03Index = result.IndexOf("@RULE R03");
        var r02Index = result.IndexOf("@RULE R02");

        Assert.True(r01Index < r03Index, "R01 (0.9) should appear before R03 (0.7)");
        Assert.True(r03Index < r02Index, "R03 (0.7) should appear before R02 (0.5)");
    }
}
