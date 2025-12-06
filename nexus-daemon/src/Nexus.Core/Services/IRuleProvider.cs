using Nexus.Core.Models;

namespace Nexus.Core.Services;

/// <summary>
/// Provides architectural and design rules for the context engine.
/// Future: Can be backed by JSON files, database, or external configuration.
/// </summary>
public interface IRuleProvider
{
    /// <summary>
    /// Returns all available rules.
    /// </summary>
    IReadOnlyList<Rule> GetAll();
}
