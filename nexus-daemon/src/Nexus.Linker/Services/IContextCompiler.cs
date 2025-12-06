using Nexus.Core.Models;

namespace Nexus.Linker.Services;

public interface IContextCompiler
{
    string Compile(List<Rule> rules, List<Node> nodes, List<Decision> decisions);
}
