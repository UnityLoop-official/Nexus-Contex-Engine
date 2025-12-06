using Nexus.Core.Models;

namespace Nexus.Core.Services;

/// <summary>
/// In-memory rule provider with hardcoded architectural rules.
/// MVP implementation - future versions can load from JSON, database, etc.
/// </summary>
public sealed class InMemoryRuleProvider : IRuleProvider
{
    private static readonly IReadOnlyList<Rule> _rules = new List<Rule>
    {
        new Rule
        {
            RuleId = "R01",
            Priority = 0.9,
            Description = "Services must not access DB directly. Use repositories."
        },
        new Rule
        {
            RuleId = "R02",
            Priority = 0.6,
            Description = "Validation logic belongs to the Domain layer."
        }
        // Future: Load from external source (appsettings.json, database, etc.)
    }.AsReadOnly();

    public IReadOnlyList<Rule> GetAll() => _rules;
}
