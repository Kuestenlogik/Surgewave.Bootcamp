// Surgewave Bootcamp - Unit 21, Lesson 2: In-Memory Deduplication
//
// In this lesson, you'll learn:
// - Implement LRU cache for bounded deduplication
// - Use time-based expiry to manage memory
// - Create Bloom filters for fast pre-checking
// - Combine strategies for optimal performance
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

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

// TODO: Step 1 - Implement an LRU Cache
// public class LruDeduplicationCache
// {
//     private readonly int _capacity;
//     private readonly Dictionary<string, LinkedListNode<string>> _cache;
//     private readonly LinkedList<string> _lruList;
//     private readonly object _lock = new();
//
//     public int EvictionCount { get; private set; }
//
//     public LruDeduplicationCache(int capacity)
//     {
//         _capacity = capacity;
//         _cache = new Dictionary<string, LinkedListNode<string>>(capacity);
//         _lruList = new LinkedList<string>();
//     }
//
//     public bool Contains(string key)
//     {
//         lock (_lock)
//         {
//             if (_cache.TryGetValue(key, out var node))
//             {
//                 _lruList.Remove(node);
//                 _lruList.AddFirst(node);
//                 return true;
//             }
//             return false;
//         }
//     }
//
//     public bool TryAdd(string key)
//     {
//         lock (_lock)
//         {
//             if (_cache.ContainsKey(key))
//                 return false;
//
//             if (_cache.Count >= _capacity)
//             {
//                 var lruKey = _lruList.Last!.Value;
//                 _lruList.RemoveLast();
//                 _cache.Remove(lruKey);
//                 EvictionCount++;
//             }
//
//             var node = _lruList.AddFirst(key);
//             _cache[key] = node;
//             return true;
//         }
//     }
//
//     public int Count => _cache.Count;
// }

// TODO: Step 2 - Test the LRU cache
// Console.WriteLine("--- LRU Cache Demo ---");
// var lruCache = new LruDeduplicationCache(10);
//
// Console.WriteLine("Adding 15 items to cache with capacity 10...");
// for (int i = 1; i <= 15; i++)
// {
//     lruCache.TryAdd($"key-{i}");
// }
// Console.WriteLine($"Cache count: {lruCache.Count} (evicted {lruCache.EvictionCount} items)");

// TODO: Step 3 - Implement time-based expiry cache
// public class TimedDeduplicationCache
// {
//     private readonly TimeSpan _ttl;
//     private readonly Dictionary<string, DateTime> _cache = new();
//     private readonly object _lock = new();
//
//     public int ExpiredCount { get; private set; }
//
//     public TimedDeduplicationCache(TimeSpan ttl)
//     {
//         _ttl = ttl;
//     }
//
//     public bool IsDuplicate(string key)
//     {
//         lock (_lock)
//         {
//             if (_cache.TryGetValue(key, out var timestamp))
//             {
//                 if (DateTime.UtcNow - timestamp <= _ttl)
//                     return true;
//                 _cache.Remove(key);
//                 ExpiredCount++;
//             }
//             return false;
//         }
//     }
//
//     public void MarkProcessed(string key)
//     {
//         lock (_lock)
//         {
//             _cache[key] = DateTime.UtcNow;
//         }
//     }
//
//     public int Count => _cache.Count;
// }

// TODO: Step 4 - Test time-based expiry
// Console.WriteLine("\n--- Time-Based Expiry Demo ---");
// var timedCache = new TimedDeduplicationCache(TimeSpan.FromSeconds(2));
//
// Console.WriteLine("Adding 5 items with 2-second TTL...");
// for (int i = 1; i <= 5; i++)
// {
//     timedCache.MarkProcessed($"key-{i}");
// }
// Console.WriteLine($"Before delay: {timedCache.Count} items");
//
// await Task.Delay(2500); // Wait for some to expire
// timedCache.IsDuplicate("key-1"); // Trigger cleanup
// timedCache.IsDuplicate("key-2");
// Console.WriteLine($"After delay: {timedCache.Count} items ({timedCache.ExpiredCount} expired)");

// TODO: Step 5 - Implement a simple Bloom filter
// public class SimpleBloomFilter
// {
//     private readonly BitArray _bits;
//     private readonly int _hashCount;
//     private readonly int _size;
//
//     public int ItemCount { get; private set; }
//
//     public SimpleBloomFilter(int expectedItems, double falsePositiveRate = 0.01)
//     {
//         _size = (int)Math.Ceiling(-expectedItems * Math.Log(falsePositiveRate) / Math.Pow(Math.Log(2), 2));
//         _hashCount = (int)Math.Ceiling(_size / (double)expectedItems * Math.Log(2));
//         _bits = new BitArray(_size);
//     }
//
//     public void Add(string item)
//     {
//         foreach (var position in GetPositions(item))
//         {
//             _bits[position] = true;
//         }
//         ItemCount++;
//     }
//
//     public bool MayContain(string item)
//     {
//         foreach (var position in GetPositions(item))
//         {
//             if (!_bits[position])
//                 return false;
//         }
//         return true;
//     }
//
//     private IEnumerable<int> GetPositions(string item)
//     {
//         var hash1 = item.GetHashCode();
//         var hash2 = item.GetHashCode() ^ 0x7FFFFFFF;
//
//         for (int i = 0; i < _hashCount; i++)
//         {
//             var combinedHash = Math.Abs(hash1 + i * hash2);
//             yield return combinedHash % _size;
//         }
//     }
// }

// TODO: Step 6 - Test the Bloom filter
// Console.WriteLine("\n--- Bloom Filter Demo ---");
// var bloom = new SimpleBloomFilter(1000, 0.01);
//
// for (int i = 0; i < 1000; i++)
// {
//     bloom.Add($"item-{i}");
// }
// Console.WriteLine($"Added {bloom.ItemCount} items to bloom filter");
//
// int knownFound = 0;
// for (int i = 0; i < 100; i++)
// {
//     if (bloom.MayContain($"item-{i}"))
//         knownFound++;
// }
// Console.WriteLine($"Testing 100 known items: {knownFound} found ({knownFound}% hit rate)");
//
// int falsePositives = 0;
// for (int i = 1000; i < 1100; i++)
// {
//     if (bloom.MayContain($"item-{i}"))
//         falsePositives++;
// }
// Console.WriteLine($"Testing 100 unknown items: {falsePositives} false positives ({falsePositives}% FP rate)");

// TODO: Step 7 - Create and test a multi-layer deduplicator with message processing
// (See completed solution for full implementation)

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. LRU cache bounds memory by evicting least recently used items");
Console.WriteLine("2. Time-based expiry removes stale entries automatically");
Console.WriteLine("3. Bloom filters provide fast 'definitely not seen' checks");
Console.WriteLine("4. Combine strategies: Bloom -> LRU -> Time for best results");
Console.WriteLine("5. In-memory state is lost on restart - plan accordingly");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
