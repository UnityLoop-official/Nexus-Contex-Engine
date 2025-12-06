using Nexus.Core.Models;

namespace Nexus.Linker.Services;

public interface IContextCompiler
{
    string Compile(IEnumerable<Rule> rules, IEnumerable<Node> nodes, IEnumerable<Decision> decisions);
}
