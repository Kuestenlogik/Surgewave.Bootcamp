# Lesson 21.2: Instructions

Follow these step-by-step instructions to implement in-memory deduplication strategies.

## Step 1: Implement an LRU Cache

Create a bounded cache that evicts least recently used entries:

```csharp
public class LruDeduplicationCache
{
    private readonly int _capacity;
    private readonly Dictionary<string, LinkedListNode<string>> _cache;
    private readonly LinkedList<string> _lruList;
    private readonly object _lock = new();

    public int EvictionCount { get; private set; }

    public LruDeduplicationCache(int capacity)
    {
        _capacity = capacity;
        _cache = new Dictionary<string, LinkedListNode<string>>(capacity);
        _lruList = new LinkedList<string>();
    }

    public bool Contains(string key)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                // Move to front (most recently used)
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                return true;
            }
            return false;
        }
    }

    public bool TryAdd(string key)
    {
        lock (_lock)
        {
            if (_cache.ContainsKey(key))
                return false;

            // Evict if at capacity
            if (_cache.Count >= _capacity)
            {
                var lruKey = _lruList.Last!.Value;
                _lruList.RemoveLast();
                _cache.Remove(lruKey);
                EvictionCount++;
            }

            var node = _lruList.AddFirst(key);
            _cache[key] = node;
            return true;
        }
    }

    public int Count => _cache.Count;
}
```

## Step 2: Add Time-Based Expiry

Extend the cache with timestamp tracking:

```csharp
public class TimedDeduplicationCache
{
    private readonly TimeSpan _ttl;
    private readonly Dictionary<string, DateTime> _cache = new();
    private readonly object _lock = new();
    private DateTime _lastCleanup = DateTime.UtcNow;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(1);

    public int ExpiredCount { get; private set; }

    public TimedDeduplicationCache(TimeSpan ttl)
    {
        _ttl = ttl;
    }

    public bool IsDuplicate(string key)
    {
        lock (_lock)
        {
            CleanupIfNeeded();

            if (_cache.TryGetValue(key, out var timestamp))
            {
                if (DateTime.UtcNow - timestamp <= _ttl)
                    return true;

                // Entry expired, remove it
                _cache.Remove(key);
                ExpiredCount++;
            }
            return false;
        }
    }

    public void MarkProcessed(string key)
    {
        lock (_lock)
        {
            _cache[key] = DateTime.UtcNow;
        }
    }

    private void CleanupIfNeeded()
    {
        if (DateTime.UtcNow - _lastCleanup < _cleanupInterval)
            return;

        var expiredKeys = _cache
            .Where(kvp => DateTime.UtcNow - kvp.Value > _ttl)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.Remove(key);
            ExpiredCount++;
        }

        _lastCleanup = DateTime.UtcNow;
    }

    public int Count => _cache.Count;
}
```

## Step 3: Implement a Simple Bloom Filter

Create a probabilistic data structure for fast pre-checking:

```csharp
public class SimpleBloomFilter
{
    private readonly BitArray _bits;
    private readonly int _hashCount;
    private readonly int _size;

    public int ItemCount { get; private set; }

    public SimpleBloomFilter(int expectedItems, double falsePositiveRate = 0.01)
    {
        // Calculate optimal size and hash count
        _size = (int)Math.Ceiling(-expectedItems * Math.Log(falsePositiveRate) / Math.Pow(Math.Log(2), 2));
        _hashCount = (int)Math.Ceiling(_size / (double)expectedItems * Math.Log(2));
        _bits = new BitArray(_size);
    }

    public void Add(string item)
    {
        foreach (var position in GetPositions(item))
        {
            _bits[position] = true;
        }
        ItemCount++;
    }

    public bool MayContain(string item)
    {
        foreach (var position in GetPositions(item))
        {
            if (!_bits[position])
                return false;  // Definitely not in set
        }
        return true;  // Probably in set (may be false positive)
    }

    private IEnumerable<int> GetPositions(string item)
    {
        var hash1 = item.GetHashCode();
        var hash2 = item.GetHashCode() ^ 0x7FFFFFFF;

        for (int i = 0; i < _hashCount; i++)
        {
            var combinedHash = Math.Abs(hash1 + i * hash2);
            yield return combinedHash % _size;
        }
    }
}
```

## Step 4: Create a Multi-Layer Deduplication Pipeline

Combine strategies for optimal performance:

```csharp
public class MultiLayerDeduplicator
{
    private readonly SimpleBloomFilter _bloomFilter;
    private readonly LruDeduplicationCache _lruCache;
    private readonly TimedDeduplicationCache _timedCache;

    public int BloomFilterHits { get; private set; }
    public int LruCacheHits { get; private set; }
    public int TruePositives { get; private set; }
    public int FalsePositives { get; private set; }

    public MultiLayerDeduplicator(int expectedItems, int lruCapacity, TimeSpan ttl)
    {
        _bloomFilter = new SimpleBloomFilter(expectedItems);
        _lruCache = new LruDeduplicationCache(lruCapacity);
        _timedCache = new TimedDeduplicationCache(ttl);
    }

    public bool IsDuplicate(string key)
    {
        // Layer 1: Bloom filter (fast, may have false positives)
        if (!_bloomFilter.MayContain(key))
        {
            return false;  // Definitely not a duplicate
        }
        BloomFilterHits++;

        // Layer 2: LRU cache (accurate for recent items)
        if (_lruCache.Contains(key))
        {
            LruCacheHits++;

            // Layer 3: Time check
            if (_timedCache.IsDuplicate(key))
            {
                TruePositives++;
                return true;  // Confirmed duplicate
            }
        }

        // Bloom filter false positive
        FalsePositives++;
        return false;
    }

    public void MarkProcessed(string key)
    {
        _bloomFilter.Add(key);
        _lruCache.TryAdd(key);
        _timedCache.MarkProcessed(key);
    }
}
```

## Step 5: Process Messages with Multi-Layer Deduplication

Apply the combined strategy during consumption:

```csharp
var deduplicator = new MultiLayerDeduplicator(
    expectedItems: 100000,
    lruCapacity: 10000,
    ttl: TimeSpan.FromMinutes(30));

while (!cancellationToken.IsCancellationRequested)
{
    var result = await consumer.ConsumeAsync(cancellationToken);
    if (result == null) continue;

    var idempotencyKey = ExtractIdempotencyKey(result.Value);

    if (deduplicator.IsDuplicate(idempotencyKey))
    {
        Console.WriteLine($"Duplicate: {idempotencyKey}");
        await consumer.CommitAsync(result);
        continue;
    }

    // Process the message
    await ProcessMessageAsync(result.Value);

    // Mark as processed in all layers
    deduplicator.MarkProcessed(idempotencyKey);
    await consumer.CommitAsync(result);
}
```

## Step 6: Monitor Deduplication Statistics

Track the effectiveness of each layer:

```csharp
Console.WriteLine("\n--- Multi-Layer Statistics ---");
Console.WriteLine($"Bloom filter hits: {deduplicator.BloomFilterHits}");
Console.WriteLine($"LRU cache hits: {deduplicator.LruCacheHits}");
Console.WriteLine($"True positives: {deduplicator.TruePositives}");
Console.WriteLine($"False positives: {deduplicator.FalsePositives}");
Console.WriteLine($"Bloom filter accuracy: {1 - (double)deduplicator.FalsePositives / deduplicator.BloomFilterHits:P2}");
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- LRU cache with eviction
- Time-based expiry cache
- Simple Bloom filter
- Multi-layer deduplication
- Statistics and monitoring

## Sample Output

```
=== Surgewave Bootcamp: Lesson 21.2 - In-Memory Deduplication ===

Surgewave broker started on port 12345

--- LRU Cache Demo ---
Adding 15 items to cache with capacity 10...
Cache count: 10 (evicted 5 items)

--- Time-Based Expiry Demo ---
Added 5 items with 2-second TTL
Before delay: 5 items
After delay: 3 items (2 expired)

--- Bloom Filter Demo ---
Added 1000 items to bloom filter
Testing 100 known items: 100 found (100% hit rate)
Testing 100 unknown items: 1 false positives (1% false positive rate)

--- Multi-Layer Deduplication ---
Processing 20 messages (10 unique, 10 duplicates)...
Processed: order:001
Duplicate detected: order:001
Processed: order:002
...

--- Deduplication Statistics ---
Total received: 20
Unique processed: 10
Duplicates detected: 10
Bloom filter hits: 10
LRU cache hits: 10
True positives: 10
False positives: 0

Lesson complete!
```

## Knowledge Check

1. Why does an LRU cache provide better memory bounds than a simple HashSet?
2. What is the trade-off of using a Bloom filter for deduplication?
3. How do you choose the appropriate TTL for time-based expiry?
4. Why might you see false positives in the Bloom filter layer?
5. What happens when the consumer restarts and the in-memory cache is empty?

---

**Next:** [Lesson 21.3: Persistent Deduplication](../lesson-3/README.md)
