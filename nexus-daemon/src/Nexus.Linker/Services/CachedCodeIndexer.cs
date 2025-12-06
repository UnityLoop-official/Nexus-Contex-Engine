using System.Collections.Concurrent;
using Nexus.Core.Models;

namespace Nexus.Linker.Services;

/// <summary>
/// Decorator for ICodeIndexer that provides simple in-memory caching.
/// Cache strategy: TTL-based (time-to-live), no file watching for MVP.
/// </summary>
public sealed class CachedCodeIndexer : ICodeIndexer
{
    private readonly ICodeIndexer _inner;
    private readonly ConcurrentDictionary<string, CachedEntry> _cache;
    private readonly TimeSpan _cacheTtl;

    public CachedCodeIndexer(ICodeIndexer inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = new ConcurrentDictionary<string, CachedEntry>(StringComparer.OrdinalIgnoreCase);
        _cacheTtl = TimeSpan.FromSeconds(30); // Simple TTL: 30 seconds for MVP
    }

    public async Task<List<Node>> IndexAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return await _inner.IndexAsync(path);
        }

        // Normalize the cache key
        var cacheKey = Path.GetFullPath(path);

        // Check if we have a valid cached entry
        if (_cache.TryGetValue(cacheKey, out var entry))
        {
            var age = DateTime.UtcNow - entry.Timestamp;
            if (age < _cacheTtl)
            {
                // Cache hit: return cached nodes
                return entry.Nodes;
            }
        }

        // Cache miss or expired: index and update cache
        var nodes = await _inner.IndexAsync(path);

        var newEntry = new CachedEntry
        {
            Nodes = nodes,
            Timestamp = DateTime.UtcNow
        };

        _cache.AddOrUpdate(cacheKey, newEntry, (_, __) => newEntry);

        return nodes;
    }

    /// <summary>
    /// Clears all cached entries. Useful for testing or forced refresh.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }

    private sealed class CachedEntry
    {
        public required List<Node> Nodes { get; init; }
        public required DateTime Timestamp { get; init; }
    }
}
