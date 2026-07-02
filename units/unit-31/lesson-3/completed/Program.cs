// Surgewave Bootcamp - Unit 21, Lesson 3: Persistent Deduplication
// COMPLETED SOLUTION

using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 21.3 - Persistent Deduplication ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

// Create temp directory for persistent storage
var tempDir = Path.Combine(Path.GetTempPath(), $"surgewave-dedup-{Guid.NewGuid():N}");
Directory.CreateDirectory(tempDir);
Console.WriteLine($"Temp directory: {tempDir}");

var bootstrapServers = $"localhost:{runtime.Port}";

// Step 1 - File-Based Persistent Store Demo
Console.WriteLine("\n--- File-Based Persistent Store ---");
var filePath = Path.Combine(tempDir, "dedup-keys.txt");

using (var store = new FileBasedDeduplicationStore(filePath))
{
    for (int i = 0; i < 10; i++)
    {
        store.MarkProcessed($"order:{i:D3}");
    }
    Console.WriteLine($"Processed 10 messages, store contains {store.Count} keys");
}

Console.WriteLine("Simulating restart...");
using (var recoveredStore = new FileBasedDeduplicationStore(filePath))
{
    Console.WriteLine($"Recovered {recoveredStore.Count} keys from file");
    Console.WriteLine($"order:005 is duplicate: {recoveredStore.IsDuplicate("order:005")} (recovered successfully)");
    Console.WriteLine($"order:999 is duplicate: {recoveredStore.IsDuplicate("order:999")} (never processed)");
}

// Step 2 - Topic-Based Distributed Store Demo
Console.WriteLine("\n--- Topic-Based Distributed Store ---");
var dedupTopic = "_dedup-state";

await using (var topicStore = new TopicBasedDeduplicationStore(bootstrapServers, dedupTopic))
{
    Console.WriteLine("Initializing from compacted topic...");
    await topicStore.InitializeAsync();

    for (int i = 0; i < 10; i++)
    {
        await topicStore.MarkProcessedAsync($"payment:{i:D3}");
    }
    Console.WriteLine($"Processed 10 messages via topic store, contains {topicStore.Count} keys");
}

Console.WriteLine("Simulating restart...");
await using (var recoveredTopicStore = new TopicBasedDeduplicationStore(bootstrapServers, dedupTopic))
{
    await recoveredTopicStore.InitializeAsync();
    Console.WriteLine($"Re-initialized store from topic with {recoveredTopicStore.Count} keys");
    Console.WriteLine($"payment:003 is duplicate: {recoveredTopicStore.IsDuplicate("payment:003")} (state recovered from topic)");
}

// Step 3 - Checkpointed Store Demo
Console.WriteLine("\n--- Checkpointed Store ---");
var checkpointPath = Path.Combine(tempDir, "checkpoint.json");

var checkpointStore = new CheckpointedDeduplicationStore(checkpointPath, checkpointInterval: 10);
Console.WriteLine("Processing 50 messages with checkpoint interval of 10...");

for (int i = 0; i < 50; i++)
{
    checkpointStore.MarkProcessed($"order:{i:D3}", i);
}

Console.WriteLine("\nSimulating crash before final checkpoint...");
var recoveredCheckpointStore = new CheckpointedDeduplicationStore(checkpointPath, checkpointInterval: 10);
Console.WriteLine($"Recovered store with {recoveredCheckpointStore.Count} keys");
Console.WriteLine($"Resume processing from offset: {recoveredCheckpointStore.LastCheckpointOffset}");

// Step 4 - Hybrid Store Demo
Console.WriteLine("\n--- Hybrid Store Performance ---");
var hybridPath = Path.Combine(tempDir, "hybrid-keys.txt");

using (var hybridStore = new HybridDeduplicationStore(hybridPath, memoryCacheLimit: 50))
{
    // Process messages
    for (int i = 0; i < 100; i++)
    {
        hybridStore.MarkProcessed($"hybrid:{i:D3}");
    }

    // Check some duplicates (should hit cache)
    for (int i = 0; i < 50; i++)
    {
        hybridStore.IsDuplicate($"hybrid:{i:D3}");
    }

    Console.WriteLine($"Processed 100 messages");
    Console.WriteLine($"Cache hits: {hybridStore.CacheHits}");
    Console.WriteLine($"Cache misses: {hybridStore.CacheMisses}");
    Console.WriteLine($"Memory cache: {hybridStore.MemoryCacheCount} keys");
    Console.WriteLine($"Persistent store: {hybridStore.PersistentCount} keys");
}

// Cleanup
try
{
    Directory.Delete(tempDir, recursive: true);
    Console.WriteLine("\nTemp directory cleaned up");
}
catch
{
    // Ignore cleanup errors
}

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Persistent stores survive application restarts");
Console.WriteLine("2. File-based stores are simple but don't scale horizontally");
Console.WriteLine("3. Compacted topics provide distributed deduplication state");
Console.WriteLine("4. Checkpointing reduces recovery time and reprocessing");
Console.WriteLine("5. Hybrid stores combine fast lookups with durability");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ==================== Helper Classes ====================

public class FileBasedDeduplicationStore : IDisposable
{
    private readonly string _filePath;
    private readonly HashSet<string> _processedKeys;
    private readonly StreamWriter _appendWriter;
    private readonly object _lock = new();

    public FileBasedDeduplicationStore(string filePath)
    {
        _filePath = filePath;
        _processedKeys = new HashSet<string>();

        // Load existing keys from file
        if (File.Exists(filePath))
        {
            foreach (var line in File.ReadLines(filePath))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    _processedKeys.Add(line.Trim());
            }
        }

        // Open for appending
        _appendWriter = new StreamWriter(filePath, append: true);
    }

    public bool IsDuplicate(string key)
    {
        lock (_lock)
        {
            return _processedKeys.Contains(key);
        }
    }

    public void MarkProcessed(string key)
    {
        lock (_lock)
        {
            if (_processedKeys.Add(key))
            {
                _appendWriter.WriteLine(key);
                _appendWriter.Flush();
            }
        }
    }

    public int Count => _processedKeys.Count;

    public void Dispose()
    {
        _appendWriter.Dispose();
    }
}

public class TopicBasedDeduplicationStore : IAsyncDisposable
{
    private readonly SurgewaveProducer<string, string> _producer;
    private readonly SurgewaveConsumer<string, string> _consumer;
    private readonly string _dedupTopic;
    private readonly HashSet<string> _processedKeys = new();
    private readonly object _lock = new();

    public TopicBasedDeduplicationStore(string bootstrapServers, string dedupTopic)
    {
        _dedupTopic = dedupTopic;

        _producer = new SurgewaveProducer<string, string>(options =>
        {
            options.BootstrapServers = bootstrapServers;
        });

        _consumer = new SurgewaveConsumer<string, string>(options =>
        {
            options.BootstrapServers = bootstrapServers;
            options.GroupId = $"dedup-store-{Guid.NewGuid():N}";
            options.AutoOffsetReset = AutoOffsetReset.Earliest;
            options.EnableAutoCommit = false;
        });
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _consumer.SubscribeAsync(ct, _dedupTopic);

        // Read to end of topic to build local state
        while (true)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

            try
            {
                var result = await _consumer.ConsumeAsync(linkedCts.Token);
                if (result == null) break;

                lock (_lock)
                {
                    if (result.Value == null)
                        _processedKeys.Remove(result.Key!); // Tombstone
                    else
                        _processedKeys.Add(result.Key!);
                }
            }
            catch (OperationCanceledException)
            {
                break; // Reached end of topic
            }
        }
    }

    public bool IsDuplicate(string key)
    {
        lock (_lock)
        {
            return _processedKeys.Contains(key);
        }
    }

    public async Task MarkProcessedAsync(string key, CancellationToken ct = default)
    {
        await _producer.ProduceAsync(_dedupTopic, key, DateTime.UtcNow.ToString("O"), ct);

        lock (_lock)
        {
            _processedKeys.Add(key);
        }
    }

    public int Count => _processedKeys.Count;

    public async ValueTask DisposeAsync()
    {
        await _producer.DisposeAsync();
        await _consumer.DisposeAsync();
    }
}

public class CheckpointedDeduplicationStore
{
    private readonly string _checkpointPath;
    private readonly HashSet<string> _processedKeys = new();
    private long _lastCheckpointOffset;
    private int _messagesSinceCheckpoint;
    private readonly int _checkpointInterval;

    public CheckpointedDeduplicationStore(string checkpointPath, int checkpointInterval = 10)
    {
        _checkpointPath = checkpointPath;
        _checkpointInterval = checkpointInterval;
        LoadCheckpoint();
    }

    private void LoadCheckpoint()
    {
        if (!File.Exists(_checkpointPath))
            return;

        var json = File.ReadAllText(_checkpointPath);
        var checkpoint = JsonSerializer.Deserialize<Checkpoint>(json);

        if (checkpoint != null)
        {
            _lastCheckpointOffset = checkpoint.Offset;
            foreach (var key in checkpoint.ProcessedKeys)
            {
                _processedKeys.Add(key);
            }
        }
    }

    public bool IsDuplicate(string key)
    {
        return _processedKeys.Contains(key);
    }

    public void MarkProcessed(string key, long offset)
    {
        _processedKeys.Add(key);
        _messagesSinceCheckpoint++;

        if (_messagesSinceCheckpoint >= _checkpointInterval)
        {
            SaveCheckpoint(offset);
            _messagesSinceCheckpoint = 0;
        }
    }

    public void SaveCheckpoint(long offset)
    {
        var checkpoint = new Checkpoint
        {
            Offset = offset,
            Timestamp = DateTime.UtcNow,
            ProcessedKeys = _processedKeys.ToList()
        };

        var json = JsonSerializer.Serialize(checkpoint, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_checkpointPath, json);
        _lastCheckpointOffset = offset;
        Console.WriteLine($"  Checkpoint saved at offset {offset}");
    }

    public long LastCheckpointOffset => _lastCheckpointOffset;
    public int Count => _processedKeys.Count;

    private class Checkpoint
    {
        public long Offset { get; set; }
        public DateTime Timestamp { get; set; }
        public List<string> ProcessedKeys { get; set; } = new();
    }
}

public class HybridDeduplicationStore : IDisposable
{
    private readonly HashSet<string> _memoryCache;
    private readonly FileBasedDeduplicationStore _persistentStore;
    private readonly int _memoryCacheLimit;
    private readonly object _lock = new();

    public int CacheHits { get; private set; }
    public int CacheMisses { get; private set; }

    public HybridDeduplicationStore(string persistentPath, int memoryCacheLimit = 10000)
    {
        _memoryCacheLimit = memoryCacheLimit;
        _memoryCache = new HashSet<string>();
        _persistentStore = new FileBasedDeduplicationStore(persistentPath);
    }

    public bool IsDuplicate(string key)
    {
        lock (_lock)
        {
            // Check memory cache first (fast path)
            if (_memoryCache.Contains(key))
            {
                CacheHits++;
                return true;
            }

            // Check persistent store (slow path)
            if (_persistentStore.IsDuplicate(key))
            {
                CacheMisses++;
                // Add to cache for future lookups
                if (_memoryCache.Count < _memoryCacheLimit)
                {
                    _memoryCache.Add(key);
                }
                return true;
            }

            return false;
        }
    }

    public void MarkProcessed(string key)
    {
        lock (_lock)
        {
            // Add to memory cache
            if (_memoryCache.Count < _memoryCacheLimit)
            {
                _memoryCache.Add(key);
            }

            // Persist durably
            _persistentStore.MarkProcessed(key);
        }
    }

    public int MemoryCacheCount => _memoryCache.Count;
    public int PersistentCount => _persistentStore.Count;

    public void Dispose()
    {
        _persistentStore.Dispose();
    }
}
