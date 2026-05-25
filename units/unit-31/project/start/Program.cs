// Unit 21 Project: Idempotent Processor with Bloom Filter
// Build a deduplication system using Bloom filter and LRU cache

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

// TODO: Step 1 - Calculate Bloom filter parameters
// Formula: m = -n * ln(p) / (ln(2)^2)
// Formula: k = (m/n) * ln(2)

// double ln2 = Math.Log(2);
// int bitArraySize = (int)Math.Ceiling(-expectedItems * Math.Log(falsePositiveRate) / (ln2 * ln2));
// int hashCount = (int)Math.Round((bitArraySize / (double)expectedItems) * ln2);

Console.WriteLine($"[Setup] Bloom filter: {expectedItems} expected items, {falsePositiveRate * 100}% false positive rate");
Console.WriteLine($"[Setup] LRU cache size: {lruCacheSize} items");
// Console.WriteLine($"[Setup] Bit array size: {bitArraySize} bits ({bitArraySize / 8} bytes)");
// Console.WriteLine($"[Setup] Hash functions: {hashCount}");

// TODO: Step 2 - Implement BloomFilter class
// See BloomFilter class template below

// TODO: Step 3 - Implement LruCache class
// See LruCache class template below

// TODO: Step 4 - Create deduplication pipeline
// var bloomFilter = new BloomFilter(expectedItems, falsePositiveRate);
// var lruCache = new LruCache<DateTime>(lruCacheSize);

// Statistics
var totalReceived = 0;
var uniqueProcessed = 0;
var duplicatesDetected = 0;
var falsePositives = 0;

// Start embedded Surgewave broker
Console.WriteLine("\n[Surgewave] Starting embedded broker...");
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;

// TODO: Create topic explicitly if needed
// await runtime.LogManager.CreateTopicAsync("orders", partitions: 1);
Console.WriteLine($"[Surgewave] Broker started at {bootstrapServers}");

// Order model
var orders = new[]
{
    new Order("order-001", "Alice", 99.95m),
    new Order("order-002", "Bob", 149.50m),
    new Order("order-001", "Alice", 99.95m),  // Duplicate!
    new Order("order-003", "Carol", 75.00m),
    new Order("order-002", "Bob", 149.50m),   // Duplicate!
    new Order("order-004", "Dave", 200.00m),
    new Order("order-001", "Alice", 99.95m),  // Duplicate!
    new Order("order-005", "Eve", 50.00m),
};

// Produce orders (including duplicates)
Console.WriteLine("\n--- Producing test orders (with duplicates) ---");

// TODO: Create producer
await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);

var seenOrderIds = new HashSet<string>();
foreach (var order in orders)
{
    var json = JsonSerializer.Serialize(order);
    // TODO: Send message using producer.ProduceAsync("orders", key, value)
    await producer.ProduceAsync("orders", order.OrderId, json);

    var isDupe = !seenOrderIds.Add(order.OrderId);
    Console.WriteLine($"[Producer] Sent {order.OrderId}{(isDupe ? " (duplicate!)" : " (first time)")}");
}

// TODO: Step 5 - Process orders with deduplication
Console.WriteLine("\n--- Processing orders ---");

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

// TODO: Create consumer
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

        // TODO: Generate idempotency key
        // var idempotencyKey = GenerateIdempotencyKey(order);

        // TODO: Check Bloom filter first (fast path)
        // if (bloomFilter.MayContain(idempotencyKey))
        // {
        //     // Maybe duplicate - check LRU cache for confirmation
        //     if (lruCache.Contains(idempotencyKey))
        //     {
        //         duplicatesDetected++;
        //         Console.WriteLine($"[Dedup] Skipping {order.OrderId} (duplicate detected)");
        //         continue;
        //     }
        //     else
        //     {
        //         // False positive from Bloom filter
        //         falsePositives++;
        //     }
        // }

        // TODO: Process the order
        Console.WriteLine($"[Processor] Processing {order.OrderId}: ${order.Amount} for {order.Customer}");
        uniqueProcessed++;

        // TODO: Add to deduplication stores
        // bloomFilter.Add(idempotencyKey);
        // lruCache.Add(idempotencyKey, DateTime.UtcNow);

        if (totalReceived >= orders.Length) break;
    }
}
catch (OperationCanceledException)
{
    // Expected timeout
}

// Print statistics
Console.WriteLine("\n--- Statistics ---");
Console.WriteLine($"Total messages received: {totalReceived}");
Console.WriteLine($"Unique messages processed: {uniqueProcessed}");
Console.WriteLine($"Duplicates detected: {duplicatesDetected}");
Console.WriteLine($"False positives: {falsePositives}");
// Console.WriteLine($"Bloom filter utilization: {bloomFilter.Utilization:P2}");

Console.WriteLine("\n=== Processor Complete ===");

// Helper: Generate idempotency key from order
static string GenerateIdempotencyKey(Order order)
{
    // Combine order ID and content hash for idempotency
    var content = $"{order.OrderId}:{order.Customer}:{order.Amount}";
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
    return Convert.ToHexString(hash)[..16]; // First 16 chars
}

// Order model
public record Order(string OrderId, string Customer, decimal Amount);

// TODO: Implement BloomFilter class
public class BloomFilter
{
    private readonly BitArray _bits;
    private readonly int _hashCount;
    private readonly int _size;
    private int _itemCount;

    public BloomFilter(int expectedItems, double falsePositiveRate)
    {
        // TODO: Calculate optimal parameters
        double ln2 = Math.Log(2);
        _size = (int)Math.Ceiling(-expectedItems * Math.Log(falsePositiveRate) / (ln2 * ln2));
        _hashCount = (int)Math.Round((_size / (double)expectedItems) * ln2);

        _bits = new BitArray(_size);
        _itemCount = 0;
    }

    public double Utilization => _itemCount / (double)_size;

    public void Add(string item)
    {
        // TODO: Set bits at hash positions
        // foreach (var pos in GetHashPositions(item))
        // {
        //     _bits[pos] = true;
        // }
        // _itemCount++;
        throw new NotImplementedException("Implement Add method");
    }

    public bool MayContain(string item)
    {
        // TODO: Check all hash positions
        // foreach (var pos in GetHashPositions(item))
        // {
        //     if (!_bits[pos]) return false;
        // }
        // return true;
        throw new NotImplementedException("Implement MayContain method");
    }

    private int[] GetHashPositions(string item)
    {
        // TODO: Use double hashing to generate k positions
        // hash_i = (hash1 + i * hash2) % size
        throw new NotImplementedException("Implement GetHashPositions method");
    }
}

// TODO: Implement LruCache class
public class LruCache<T>
{
    private readonly int _capacity;
    private readonly LinkedList<(string key, T value)> _list;
    private readonly Dictionary<string, LinkedListNode<(string key, T value)>> _map;

    public LruCache(int capacity)
    {
        _capacity = capacity;
        _list = new LinkedList<(string, T)>();
        _map = new Dictionary<string, LinkedListNode<(string, T)>>(capacity);
    }

    public bool Contains(string key)
    {
        // TODO: Check if key exists and move to front (most recently used)
        throw new NotImplementedException("Implement Contains method");
    }

    public void Add(string key, T value)
    {
        // TODO: Add item, evict oldest if at capacity
        throw new NotImplementedException("Implement Add method");
    }
}