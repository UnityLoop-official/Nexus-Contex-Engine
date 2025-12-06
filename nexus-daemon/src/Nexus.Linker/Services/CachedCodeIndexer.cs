using Microsoft.Extensions.Caching.Memory;
using Nexus.Core.Models;
using Nexus.Linker.Services;

namespace Nexus.Linker.Services;

/// <summary>
/// Decorator for ICodeIndexer that provides in-memory caching using IMemoryCache.
/// </summary>
public class CachedCodeIndexer : ICodeIndexer
{
    private readonly ICodeIndexer _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public CachedCodeIndexer(ICodeIndexer inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<List<Node>> IndexAsync(string path)
    {
        // Simple cache key based on path
        string key = $"CodeIndex_{path}";

        return await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            // Allow larger items but maybe track size? MVP: simple time expiration.
            return await _inner.IndexAsync(path);
        }) ?? new List<Node>();
    }
}
