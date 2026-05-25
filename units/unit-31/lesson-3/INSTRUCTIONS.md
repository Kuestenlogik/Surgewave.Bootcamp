# Lesson 21.3: Instructions

Follow these step-by-step instructions to implement persistent deduplication stores.

## Step 1: Implement a File-Based Persistent Store

Create a simple file-based deduplication store:

```csharp
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
```

## Step 2: Use a Compacted Topic for Distributed Deduplication

Create a deduplication store backed by a Surgewave compacted topic:

```csharp
public class TopicBasedDeduplicationStore : IAsyncDisposable
{
    private readonly SurgewaveProducer<string, string> _producer;
    private readonly SurgewaveConsumer<string, string> _consumer;
    private readonly string _dedupTopic;
    private readonly HashSet<string> _processedKeys = new();
    private readonly object _lock = new();

    public TopicBasedDeduplicationStore(
        string bootstrapServers,
        string dedupTopic)
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
        // Subscribe and read all existing keys from compacted topic
        await _consumer.SubscribeAsync(ct, _dedupTopic);

        // Read to end of topic to build local state
        while (true)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            try
            {
                var result = await _consumer.ConsumeAsync(cts.Token);
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
        // Write to compacted topic
        await _producer.ProduceAsync(_dedupTopic, key, DateTime.UtcNow.ToString("O"), ct);

        lock (_lock)
        {
            _processedKeys.Add(key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        // Write tombstone (null value)
        await _producer.ProduceAsync(_dedupTopic, key, null!, ct);

        lock (_lock)
        {
            _processedKeys.Remove(key);
        }
    }

    public int Count => _processedKeys.Count;

    public async ValueTask DisposeAsync()
    {
        await _producer.DisposeAsync();
        await _consumer.DisposeAsync();
    }
}
```

## Step 3: Implement Checkpointing

Create a checkpoint-based deduplication system:

```csharp
public class CheckpointedDeduplicationStore
{
    private readonly string _checkpointPath;
    private readonly HashSet<string> _processedKeys = new();
    private readonly object _lock = new();
    private long _lastCheckpointOffset;
    private int _messagesSinceCheckpoint;
    private readonly int _checkpointInterval;

    public CheckpointedDeduplicationStore(string checkpointPath, int checkpointInterval = 100)
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
        lock (_lock)
        {
            return _processedKeys.Contains(key);
        }
    }

    public void MarkProcessed(string key, long offset)
    {
        lock (_lock)
        {
            _processedKeys.Add(key);
            _messagesSinceCheckpoint++;

            if (_messagesSinceCheckpoint >= _checkpointInterval)
            {
                SaveCheckpoint(offset);
                _messagesSinceCheckpoint = 0;
            }
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
```

## Step 4: Create a Hybrid Store

Combine in-memory cache with persistent backing:

```csharp
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

        // Pre-warm cache with recent keys from persistent store
        // (In real implementation, would use LRU and selective loading)
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
```

## Step 5: Test Recovery

Simulate a crash and verify recovery:

```csharp
// Process some messages
for (int i = 0; i < 50; i++)
{
    var key = $"order:{i:D3}";
    store.MarkProcessed(key, i);
}
Console.WriteLine($"Processed 50 messages, checkpoint at offset {store.LastCheckpointOffset}");

// Simulate crash (dispose without final checkpoint)
// store goes out of scope...

// Recover with new instance
var recoveredStore = new CheckpointedDeduplicationStore(checkpointPath, checkpointInterval: 10);
Console.WriteLine($"Recovered {recoveredStore.Count} keys from checkpoint");
Console.WriteLine($"Resume from offset: {recoveredStore.LastCheckpointOffset}");

// Verify duplicates are detected
var isDup = recoveredStore.IsDuplicate("order:005");
Console.WriteLine($"order:005 is duplicate: {isDup}"); // Should be true
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- File-based persistent store
- Compacted topic deduplication
- Checkpoint-based recovery
- Hybrid memory + persistent store
- Recovery testing

## Sample Output

```
=== Surgewave Bootcamp: Lesson 21.3 - Persistent Deduplication ===

Surgewave broker started on port 12345

--- File-Based Persistent Store ---
Processed 10 messages
Store contains 10 keys
Simulating restart...
Recovered 10 keys from file
order:005 is duplicate: True (recovered successfully)

--- Topic-Based Distributed Store ---
Initializing from compacted topic...
Processed 10 messages via topic store
Store contains 10 keys
Simulating restart...
Re-initialized store from topic
order:003 is duplicate: True (state recovered from topic)

--- Checkpointed Store ---
Processing 50 messages with checkpoint interval of 10...
Checkpoint saved at offset 9
Checkpoint saved at offset 19
Checkpoint saved at offset 29
Checkpoint saved at offset 39
Checkpoint saved at offset 49
Simulating crash before final checkpoint...
Recovered store with 50 keys
Resume processing from offset: 49

--- Hybrid Store Performance ---
Processed 100 messages
Cache hits: 45
Cache misses: 5
Memory cache: 100 keys
Persistent store: 100 keys

Lesson complete!
```

## Knowledge Check

1. What are the trade-offs between file-based and database-backed deduplication?
2. How does a compacted topic help with distributed deduplication?
3. What happens to messages between the last checkpoint and a crash?
4. Why is a hybrid approach often best for production systems?
5. How would you handle TTL/expiry in a persistent deduplication store?

---

**Next:** [Lesson 21.4: Idempotent Operations](../lesson-4/README.md)
