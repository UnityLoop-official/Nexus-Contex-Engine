using Nexus.Core.Models;

namespace Nexus.Linker.Services;

public interface ICodeIndexer
{
    Task<List<Node>> IndexAsync(string path);
}
