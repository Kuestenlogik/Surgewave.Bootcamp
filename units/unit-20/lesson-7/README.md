# Lesson 20.7: Tool Result Caching

### Why Cache Tool Results?

AI agents often call external tools (APIs, databases, web searches) that are expensive, slow, or rate-limited. Caching successful results avoids redundant calls for identical inputs.

### CachedAgentTool

A decorator that wraps any `IAgentTool` with transparent caching:

```csharp
var cache = new InMemoryToolResultCache(new ToolCacheOptions());
var cachedTool = new CachedAgentTool(
    inner: originalTool,
    cache: cache,
    ttl: TimeSpan.FromMinutes(10)
);

// First call: executes the tool, caches the result
var result1 = await cachedTool.InvokeAsync(args);

// Second call with same args: returns cached result instantly
var result2 = await cachedTool.InvokeAsync(args);
```

**Key behavior:**
- Cache keys are SHA-256 hashes of the JSON-serialized, sorted argument dictionary
- Error results are **never** cached (only successful results)
- The decorator preserves the tool's Name, Description, and InputSchema

### InMemoryToolResultCache

Backed by a `ConcurrentDictionary` with TTL-based expiration and LRU eviction:

```csharp
var cache = new InMemoryToolResultCache(new ToolCacheOptions
{
    DefaultTtl = TimeSpan.FromMinutes(5),
    MaxCachedEntries = 1000
});

// Manual cache operations
await cache.SetAsync("toolName", "cacheKey", result, TimeSpan.FromMinutes(10));
var cached = await cache.GetAsync("toolName", "cacheKey");
await cache.InvalidateAsync("toolName");           // Invalidate all entries for a tool
await cache.InvalidateAsync("toolName", "cacheKey"); // Invalidate specific entry
```

### CachingToolProvider

Wraps an entire `IAgentToolProvider` to automatically cache all tools:

```csharp
var cachingProvider = new CachingToolProvider(
    inner: myToolProvider,
    cache: cache,
    options: new ToolCacheOptions
    {
        DefaultTtl = TimeSpan.FromMinutes(5),
        MaxCachedEntries = 1000,
        ExcludedTools = ["real-time-weather"],  // Never cache these
        ToolOverrides = new()
        {
            ["expensive-api"] = new ToolCachePolicyOverride
            {
                Ttl = TimeSpan.FromHours(1)  // Cache longer
            },
            ["volatile-data"] = new ToolCachePolicyOverride
            {
                Disabled = true  // Disable caching for this tool
            }
        }
    }
);
```

### ToolCacheOptions

```csharp
public sealed class ToolCacheOptions
{
    public bool Enabled { get; set; } = true;
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxCachedEntries { get; set; } = 1000;
    public HashSet<string> ExcludedTools { get; set; } = [];
    public Dictionary<string, ToolCachePolicyOverride> ToolOverrides { get; set; } = [];
}
```

### ToolCacheStats

Monitor cache performance:

```csharp
var stats = await cache.GetStatsAsync();
Console.WriteLine($"Entries: {stats.TotalEntries}");
Console.WriteLine($"Hits: {stats.Hits}");
Console.WriteLine($"Misses: {stats.Misses}");
Console.WriteLine($"Hit Rate: {stats.HitRate:P1}");  // e.g., "85.3%"
```

---

[Back to Unit 20](../README.md)
