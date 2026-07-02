// Surgewave Bootcamp - Unit 21, Lesson 2: In-Memory Deduplication
// COMPLETED SOLUTION

using System.Collections;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 21.2 - In-Memory Deduplication ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var topic = "orders-topic";

// Step 1 - LRU Cache Implementation
Console.WriteLine("\n--- LRU Cache Demo ---");
var lruCache = new LruDeduplicationCache(10);

Console.WriteLine("Adding 15 items to cache with capacity 10...");
for (int i = 1; i <= 15; i++)
{
    lruCache.TryAdd($"key-{i}");
}
Console.WriteLine($"Cache count: {lruCache.Count} (evicted {lruCache.EvictionCount} items)");

// Verify oldest items were evicted
Console.WriteLine($"Contains key-1 (oldest): {lruCache.Contains("key-1")}");
Console.WriteLine($"Contains key-15 (newest): {lruCache.Contains("key-15")}");

// Step 2 - Time-Based Expiry Demo
Console.WriteLine("\n--- Time-Based Expiry Demo ---");
var timedCache = new TimedDeduplicationCache(TimeSpan.FromSeconds(1));

Console.WriteLine("Adding 5 items with 1-second TTL...");
timedCache.MarkProcessed("key-1");
timedCache.MarkProcessed("key-2");
await Task.Delay(500);
timedCache.MarkProcessed("key-3");
timedCache.MarkProcessed("key-4");
timedCache.MarkProcessed("key-5");
Console.WriteLine($"Before delay: {timedCache.Count} items");

await Task.Delay(600); // Wait for first 2 to expire

// Check each key to trigger expiry detection
Console.WriteLine($"key-1 is duplicate: {timedCache.IsDuplicate("key-1")} (should be false - expired)");
Console.WriteLine($"key-2 is duplicate: {timedCache.IsDuplicate("key-2")} (should be false - expired)");
Console.WriteLine($"key-3 is duplicate: {timedCache.IsDuplicate("key-3")} (should be true - still valid)");
Console.WriteLine($"Expired count: {timedCache.ExpiredCount}");

// Step 3 - Bloom Filter Demo
Console.WriteLine("\n--- Bloom Filter Demo ---");
var bloom = new SimpleBloomFilter(1000, 0.01);

for (int i = 0; i < 1000; i++)
{
    bloom.Add($"item-{i}");
}
Console.WriteLine($"Added {bloom.ItemCount} items to bloom filter");

int knownFound = 0;
for (int i = 0; i < 100; i++)
{
    if (bloom.MayContain($"item-{i}"))
        knownFound++;
}
Console.WriteLine($"Testing 100 known items: {knownFound} found (should be 100%)");

int falsePositives = 0;
for (int i = 1000; i < 1100; i++)
{
    if (bloom.MayContain($"item-{i}"))
        falsePositives++;
}
Console.WriteLine($"Testing 100 unknown items: {falsePositives} false positives (~1% expected)");

// Step 4 - Multi-Layer Deduplication with Message Processing
Console.WriteLine("\n--- Multi-Layer Deduplication ---");

var deduplicator = new MultiLayerDeduplicator(
    expectedItems: 10000,
    lruCapacity: 1000,
    ttl: TimeSpan.FromMinutes(5));

// Create producer
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

// Produce messages with duplicates
Console.WriteLine("Producing 20 messages (10 unique, 10 duplicates)...");

var uniqueKeys = new[] { "order:001", "order:002", "order:003", "order:004", "order:005",
                         "order:006", "order:007", "order:008", "order:009", "order:010" };

// Send unique messages
foreach (var key in uniqueKeys)
{
    var message = JsonSerializer.Serialize(new { IdempotencyKey = key, Data = $"Data for {key}" });
    await producer.ProduceAsync(topic, key, message);
}

// Send duplicates
foreach (var key in uniqueKeys)
{
    var message = JsonSerializer.Serialize(new { IdempotencyKey = key, Data = $"Data for {key}" });
    await producer.ProduceAsync(topic, key, message);
}

// Create consumer
await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "dedup-demo-group";
    options.EnableAutoCommit = false;
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await consumer.SubscribeAsync(default, topic);

Console.WriteLine("\nProcessing messages with multi-layer deduplication...");

var processedCount = 0;
var duplicateCount = 0;
var totalMessages = 20;

for (int i = 0; i < totalMessages; i++)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    var message = JsonSerializer.Deserialize<JsonElement>(result.Value);
    var idempotencyKey = message.GetProperty("IdempotencyKey").GetString()!;

    if (deduplicator.IsDuplicate(idempotencyKey))
    {
        duplicateCount++;
        Console.WriteLine($"  Duplicate: {idempotencyKey}");
        await consumer.CommitAsync(result);
        continue;
    }

    // Process message
    processedCount++;
    Console.WriteLine($"  Processed: {idempotencyKey}");

    deduplicator.MarkProcessed(idempotencyKey);
    await consumer.CommitAsync(result);
}

// Statistics
Console.WriteLine("\n--- Deduplication Statistics ---");
Console.WriteLine($"Total received: {processedCount + duplicateCount}");
Console.WriteLine($"Unique processed: {processedCount}");
Console.WriteLine($"Duplicates detected: {duplicateCount}");
Console.WriteLine($"Bloom filter hits: {deduplicator.BloomFilterHits}");
Console.WriteLine($"LRU cache hits: {deduplicator.LruCacheHits}");
Console.WriteLine($"True positives: {deduplicator.TruePositives}");
Console.WriteLine($"False positives: {deduplicator.FalsePositives}");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. LRU cache bounds memory by evicting least recently used items");
Console.WriteLine("2. Time-based expiry removes stale entries automatically");
Console.WriteLine("3. Bloom filters provide fast 'definitely not seen' checks");
Console.WriteLine("4. Combine strategies: Bloom -> LRU -> Time for best results");
Console.WriteLine("5. In-memory state is lost on restart - plan accordingly");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ==================== Helper Classes ====================

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

            // Evict LRU item if at capacity
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

public class TimedDeduplicationCache
{
    private readonly TimeSpan _ttl;
    private readonly Dictionary<string, DateTime> _cache = new();
    private readonly object _lock = new();

    public int ExpiredCount { get; private set; }

    public TimedDeduplicationCache(TimeSpan ttl)
    {
        _ttl = ttl;
    }

    public bool IsDuplicate(string key)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var timestamp))
            {
                if (DateTime.UtcNow - timestamp <= _ttl)
                    return true;

                // Entry expired
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

    public int Count => _cache.Count;
}

public class SimpleBloomFilter
{
    private readonly BitArray _bits;
    private readonly int _hashCount;
    private readonly int _size;

    public int ItemCount { get; private set; }

    public SimpleBloomFilter(int expectedItems, double falsePositiveRate = 0.01)
    {
        // Calculate optimal size: m = -n * ln(p) / (ln(2)^2)
        _size = (int)Math.Ceiling(-expectedItems * Math.Log(falsePositiveRate) / Math.Pow(Math.Log(2), 2));
        // Calculate optimal hash count: k = (m/n) * ln(2)
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
                return false; // Definitely not in set
        }
        return true; // Probably in set
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
            return false; // Definitely not a duplicate
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
                return true; // Confirmed duplicate
            }
        }

        // Bloom filter false positive or expired entry
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
