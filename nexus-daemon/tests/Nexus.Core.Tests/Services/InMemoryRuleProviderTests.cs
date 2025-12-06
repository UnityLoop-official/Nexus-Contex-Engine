using Nexus.Core.Services;
using Xunit;

namespace Nexus.Core.Tests.Services;

public class InMemoryRuleProviderTests
{
    [Fact]
    public void GetAll_ShouldReturnNonEmptyList()
    {
        // Arrange
        var provider = new InMemoryRuleProvider();

        // Act
        var rules = provider.GetAll();

        // Assert
        Assert.NotNull(rules);
        Assert.NotEmpty(rules);
    }

    [Fact]
    public void GetAll_ShouldContainR01AndR02()
    {
        // Arrange
        var provider = new InMemoryRuleProvider();

        // Act
        var rules = provider.GetAll();

        // Assert
        Assert.Contains(rules, r => r.RuleId == "R01");
        Assert.Contains(rules, r => r.RuleId == "R02");
    }

    [Fact]
    public void GetAll_ShouldReturnReadOnlyList()
    {
        // Arrange
        var provider = new InMemoryRuleProvider();

        // Act
        var rules = provider.GetAll();

        // Assert - verify it's read-only by checking interface type
        Assert.IsAssignableFrom<IReadOnlyList<Nexus.Core.Models.Rule>>(rules);
    }
}
