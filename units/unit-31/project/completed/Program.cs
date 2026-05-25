// Unit 21 Project: Idempotent Processor with Bloom Filter - COMPLETED
// A deduplication system using Bloom filter and LRU cache

using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Idempotent Order Processor ===\n");

// Configuration
var expectedItems = 10000;
var falsePositiveRate = 0.01; // 1%
var lruCacheSize = 1000;

// Step 1: Create deduplication components
var bloomFilter = new BloomFilter(expectedItems, falsePositiveRate);
var lruCache = new LruCache<DateTime>(lruCacheSize);

Console.WriteLine($"[Setup] Bloom filter: {expectedItems} expected items, {falsePositiveRate * 100}% false positive rate");
Console.WriteLine($"[Setup] LRU cache size: {lruCacheSize} items");
Console.WriteLine($"[Setup] Bit array size: {bloomFilter.BitCount} bits ({bloomFilter.BitCount / 8} bytes)");
Console.WriteLine($"[Setup] Hash functions: {bloomFilter.HashCount}");

// Statistics
var totalReceived = 0;
var uniqueProcessed = 0;
var duplicatesDetected = 0;
var bloomFilterHits = 0;
var processedAmount = 0m;

// Start embedded Surgewave broker
Console.WriteLine("\n[Surgewave] Starting embedded broker...");
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;
Console.WriteLine($"[Surgewave] Broker started at {bootstrapServers}");

// Order model with duplicates
var orders = new[]
{
    new Order("order-001", "Alice", 99.95m),
    new Order("order-002", "Bob", 149.50m),
    new Order("order-001", "Alice", 99.95m),   // Duplicate!
    new Order("order-003", "Carol", 75.00m),
    new Order("order-002", "Bob", 149.50m),    // Duplicate!
    new Order("order-004", "Dave", 200.00m),
    new Order("order-001", "Alice", 99.95m),   // Duplicate!
    new Order("order-005", "Eve", 50.00m),
    new Order("order-003", "Carol", 75.00m),   // Duplicate!
    new Order("order-006", "Frank", 125.00m),
};

// Step 2: Produce orders (including duplicates)
Console.WriteLine("\n--- Producing test orders (with duplicates) ---");

await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);

var seenOrderIds = new HashSet<string>();
foreach (var order in orders)
{
    var json = JsonSerializer.Serialize(order);
    await producer.ProduceAsync("orders", order.OrderId, json);

    var isDupe = !seenOrderIds.Add(order.OrderId);
    Console.WriteLine($"[Producer] Sent {order.OrderId}{(isDupe ? " (duplicate!)" : " (first time)")}");
}

Console.WriteLine($"\n[Producer] Total sent: {orders.Length} ({seenOrderIds.Count} unique, {orders.Length - seenOrderIds.Count} duplicates)");

// Step 3: Process orders with deduplication
Console.WriteLine("\n--- Processing orders ---");

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "dedup-processor";
    opt.AutoOffsetReset = Kuestenlogik.Surgewave.Client.Consumer.AutoOffsetReset.Earliest;
});
consumer.Subscribe("orders");

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        var result = await consumer.ConsumeAsync(cts.Token);
        if (result is null) continue;

        totalReceived++;

        var order = JsonSerializer.Deserialize<Order>(result.Value ?? "");
        if (order is null) continue;

        // Generate idempotency key from order content
        var idempotencyKey = GenerateIdempotencyKey(order);

        // Deduplication pipeline: Bloom filter first, then LRU cache
        if (bloomFilter.MayContain(idempotencyKey))
        {
            bloomFilterHits++;

            // Bloom filter says "maybe" - check LRU cache for confirmation
            if (lruCache.Contains(idempotencyKey))
            {
                // Confirmed duplicate - skip processing
                duplicatesDetected++;
                Console.WriteLine($"[Dedup] Skipping {order.OrderId} (duplicate detected by bloom+LRU)");

                if (totalReceived >= orders.Length) break;
                continue;
            }

            // False positive from Bloom filter - item not in LRU
            Console.WriteLine($"[Dedup] Bloom filter false positive for {order.OrderId} (not in LRU)");
        }

        // Process the unique order
        Console.WriteLine($"[Processor] Processing {order.OrderId}: ${order.Amount} for {order.Customer}");
        uniqueProcessed++;
        processedAmount += order.Amount;

        // Simulate processing work
        await Task.Delay(10);

        // Add to deduplication stores
        bloomFilter.Add(idempotencyKey);
        lruCache.Add(idempotencyKey, DateTime.UtcNow);

        if (totalReceived >= orders.Length) break;
    }
}
catch (OperationCanceledException)
{
    // Expected timeout
}

// Print statistics
Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("--- Deduplication Statistics ---");
Console.WriteLine($"Total messages received: {totalReceived}");
Console.WriteLine($"Unique messages processed: {uniqueProcessed}");
Console.WriteLine($"Duplicates detected: {duplicatesDetected}");
Console.WriteLine($"Total order value processed: ${processedAmount:F2}");
Console.WriteLine();
Console.WriteLine("--- Bloom Filter Stats ---");
Console.WriteLine($"Bloom filter hits (maybe): {bloomFilterHits}");
Console.WriteLine($"Bloom filter utilization: {bloomFilter.Utilization:P4}");
Console.WriteLine($"Items in Bloom filter: {bloomFilter.ItemCount}");
Console.WriteLine();
Console.WriteLine("--- LRU Cache Stats ---");
Console.WriteLine($"Items in LRU cache: {lruCache.Count}");
Console.WriteLine(new string('=', 50));

Console.WriteLine("\n=== Processor Complete ===");

// Helper: Generate idempotency key from order
static string GenerateIdempotencyKey(Order order)
{
    // Combine order ID and content hash for idempotency
    // This ensures same order content = same key
    var content = $"{order.OrderId}:{order.Customer}:{order.Amount:F2}";
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
    return Convert.ToHexString(hash)[..16]; // First 16 chars (64 bits)
}

// Order model
public record Order(string OrderId, string Customer, decimal Amount);

/// <summary>
/// A probabilistic data structure for set membership testing.
/// May return false positives but never false negatives.
/// </summary>
public class BloomFilter
{
    private readonly BitArray _bits;
    private readonly int _hashCount;
    private readonly int _size;
    private int _itemCount;
    private readonly object _lock = new();

    public BloomFilter(int expectedItems, double falsePositiveRate)
    {
        // Calculate optimal parameters using formulas:
        // m = -n * ln(p) / (ln(2)^2)
        // k = (m/n) * ln(2)
        double ln2 = Math.Log(2);
        _size = (int)Math.Ceiling(-expectedItems * Math.Log(falsePositiveRate) / (ln2 * ln2));
        _hashCount = Math.Max(1, (int)Math.Round((_size / (double)expectedItems) * ln2));

        _bits = new BitArray(_size);
        _itemCount = 0;
    }

    public int BitCount => _size;
    public int HashCount => _hashCount;
    public int ItemCount => _itemCount;

    public double Utilization
    {
        get
        {
            lock (_lock)
            {
                var setBits = 0;
                for (int i = 0; i < _bits.Length; i++)
                {
                    if (_bits[i]) setBits++;
                }
                return setBits / (double)_size;
            }
        }
    }

    public void Add(string item)
    {
        var positions = GetHashPositions(item);

        lock (_lock)
        {
            foreach (var pos in positions)
            {
                _bits[pos] = true;
            }
            _itemCount++;
        }
    }

    public bool MayContain(string item)
    {
        var positions = GetHashPositions(item);

        lock (_lock)
        {
            foreach (var pos in positions)
            {
                if (!_bits[pos]) return false;
            }
            return true;
        }
    }

    private int[] GetHashPositions(string item)
    {
        // Use double hashing technique: hash_i = (hash1 + i * hash2) % size
        // This gives us k independent hash positions from just 2 hash computations

        var bytes = Encoding.UTF8.GetBytes(item);

        // Get two 64-bit hashes using SHA256 (first 128 bits)
        var sha = SHA256.HashData(bytes);
        var hash1 = BitConverter.ToInt64(sha, 0);
        var hash2 = BitConverter.ToInt64(sha, 8);

        // Ensure positive values
        hash1 = Math.Abs(hash1);
        hash2 = Math.Abs(hash2);

        var positions = new int[_hashCount];
        for (int i = 0; i < _hashCount; i++)
        {
            // Combine hashes to get position
            var combinedHash = hash1 + (i * hash2);
            positions[i] = (int)(Math.Abs(combinedHash) % _size);
        }

        return positions;
    }
}

/// <summary>
/// A Least Recently Used (LRU) cache with O(1) operations.
/// </summary>
public class LruCache<T>
{
    private readonly int _capacity;
    private readonly LinkedList<(string key, T value)> _list;
    private readonly Dictionary<string, LinkedListNode<(string key, T value)>> _map;
    private readonly object _lock = new();

    public LruCache(int capacity)
    {
        _capacity = capacity;
        _list = new LinkedList<(string, T)>();
        _map = new Dictionary<string, LinkedListNode<(string, T)>>(capacity);
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _map.Count;
            }
        }
    }

    public bool Contains(string key)
    {
        lock (_lock)
        {
            if (_map.TryGetValue(key, out var node))
            {
                // Move to front (most recently used)
                _list.Remove(node);
                _list.AddFirst(node);
                return true;
            }
            return false;
        }
    }

    public bool TryGet(string key, out T? value)
    {
        lock (_lock)
        {
            if (_map.TryGetValue(key, out var node))
            {
                // Move to front (most recently used)
                _list.Remove(node);
                _list.AddFirst(node);
                value = node.Value.value;
                return true;
            }
            value = default;
            return false;
        }
    }

    public void Add(string key, T value)
    {
        lock (_lock)
        {
            // Check if key exists
            if (_map.TryGetValue(key, out var existingNode))
            {
                // Update value and move to front
                _list.Remove(existingNode);
                existingNode.Value = (key, value);
                _list.AddFirst(existingNode);
                return;
            }

            // Evict oldest if at capacity
            if (_map.Count >= _capacity)
            {
                var oldest = _list.Last;
                if (oldest != null)
                {
                    _map.Remove(oldest.Value.key);
                    _list.RemoveLast();
                }
            }

            // Add new item at front
            var newNode = _list.AddFirst((key, value));
            _map[key] = newNode;
        }
    }

    public void Remove(string key)
    {
        lock (_lock)
        {
            if (_map.TryGetValue(key, out var node))
            {
                _list.Remove(node);
                _map.Remove(key);
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _list.Clear();
            _map.Clear();
        }
    }
}