# Lesson 15.2: Instructions

Follow these step-by-step instructions to implement cross-datacenter replication.

## Step 1: Define Replication Configuration

Create configuration for the replication bridge:

```csharp
public class ReplicationConfig
{
    public string SourceBootstrapServers { get; set; } = "";
    public string TargetBootstrapServers { get; set; } = "";
    public List<string> Topics { get; set; } = new();
    public string TopicMappingMode { get; set; } = "identity"; // identity, prefixed, custom
    public string TopicPrefix { get; set; } = "";
    public bool EnableCompression { get; set; } = true;
    public TimeSpan LagAlertThreshold { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan CheckpointInterval { get; set; } = TimeSpan.FromSeconds(30);
}
```

## Step 2: Create the Replication Bridge

Implement the core replication logic:

```csharp
public class ReplicationBridge : IAsyncDisposable
{
    private readonly ReplicationConfig _config;
    private readonly SurgewaveConsumer<byte[], byte[]> _sourceConsumer;
    private readonly SurgewaveProducer<byte[], byte[]> _targetProducer;
    private readonly Dictionary<string, ReplicationStats> _stats = new();
    private CancellationTokenSource? _cts;

    public ReplicationBridge(ReplicationConfig config)
    {
        _config = config;

        _sourceConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
        {
            options.BootstrapServers = config.SourceBootstrapServers;
            options.GroupId = "replication-bridge";
            options.AutoOffsetReset = AutoOffsetReset.Earliest;
            options.EnableAutoCommit = false;
        });

        _targetProducer = new SurgewaveProducer<byte[], byte[]>(options =>
        {
            options.BootstrapServers = config.TargetBootstrapServers;
            options.CompressionType = config.EnableCompression
                ? CompressionType.Snappy
                : CompressionType.None;
        });
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // Subscribe to source topics
        _sourceConsumer.Subscribe(_config.Topics);

        Console.WriteLine($"Replication bridge started");
        Console.WriteLine($"  Source: {_config.SourceBootstrapServers}");
        Console.WriteLine($"  Target: {_config.TargetBootstrapServers}");
        Console.WriteLine($"  Topics: {string.Join(", ", _config.Topics)}");

        // Start replication loop
        _ = Task.Run(() => ReplicationLoopAsync(_cts.Token), _cts.Token);

        // Start lag monitoring
        _ = Task.Run(() => LagMonitorLoopAsync(_cts.Token), _cts.Token);
    }
}
```

## Step 3: Implement the Replication Loop

Process messages from source and produce to target:

```csharp
private async Task ReplicationLoopAsync(CancellationToken ct)
{
    var lastCheckpoint = DateTime.UtcNow;

    while (!ct.IsCancellationRequested)
    {
        try
        {
            var result = _sourceConsumer.Consume(TimeSpan.FromMilliseconds(100));
            if (result == null) continue;

            // Map topic name based on configuration
            var targetTopic = MapTopicName(result.Topic);

            // Produce to target cluster
            await _targetProducer.ProduceAsync(
                targetTopic,
                result.Key,
                result.Value);

            // Update stats
            UpdateStats(result.Topic, result.Offset, result.Timestamp);

            // Periodic checkpoint
            if (DateTime.UtcNow - lastCheckpoint > _config.CheckpointInterval)
            {
                await _sourceConsumer.CommitAsync();
                lastCheckpoint = DateTime.UtcNow;
                Console.WriteLine("  Checkpoint committed");
            }
        }
        catch (OperationCanceledException)
        {
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Replication error: {ex.Message}");
            await Task.Delay(1000, ct);
        }
    }
}

private string MapTopicName(string sourceTopic)
{
    return _config.TopicMappingMode switch
    {
        "prefixed" => $"{_config.TopicPrefix}.{sourceTopic}",
        "custom" => ApplyCustomMapping(sourceTopic),
        _ => sourceTopic // identity mapping
    };
}
```

## Step 4: Implement Lag Monitoring

Track and alert on replication lag:

```csharp
public class ReplicationStats
{
    public string Topic { get; set; } = "";
    public long LastSourceOffset { get; set; }
    public long LastReplicatedOffset { get; set; }
    public DateTime LastMessageTimestamp { get; set; }
    public DateTime LastReplicationTimestamp { get; set; }
    public long MessageCount { get; set; }

    public long OffsetLag => LastSourceOffset - LastReplicatedOffset;
    public TimeSpan TimeLag => DateTime.UtcNow - LastMessageTimestamp;
}

private async Task LagMonitorLoopAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), ct);

        foreach (var (topic, stats) in _stats)
        {
            Console.WriteLine($"  [{topic}] Replicated: {stats.MessageCount}, " +
                              $"Offset lag: {stats.OffsetLag}, " +
                              $"Time lag: {stats.TimeLag.TotalSeconds:F1}s");

            if (stats.TimeLag > _config.LagAlertThreshold)
            {
                await RaiseLagAlertAsync(topic, stats);
            }
        }
    }
}

private async Task RaiseLagAlertAsync(string topic, ReplicationStats stats)
{
    Console.WriteLine($"  ALERT: Replication lag for {topic} exceeds threshold!");
    Console.WriteLine($"    Current lag: {stats.TimeLag}");
    Console.WriteLine($"    Threshold: {_config.LagAlertThreshold}");

    // In production, send to alerting system
    await Task.CompletedTask;
}
```

## Step 5: Implement Offset Translation

Map offsets between source and target for failover:

```csharp
public class OffsetTranslator
{
    private readonly Dictionary<string, List<OffsetMapping>> _mappings = new();

    public void RecordMapping(string topic, int partition,
        long sourceOffset, long targetOffset)
    {
        var key = $"{topic}-{partition}";
        if (!_mappings.ContainsKey(key))
            _mappings[key] = new List<OffsetMapping>();

        _mappings[key].Add(new OffsetMapping
        {
            SourceOffset = sourceOffset,
            TargetOffset = targetOffset,
            Timestamp = DateTime.UtcNow
        });

        // Keep only recent mappings
        if (_mappings[key].Count > 10000)
            _mappings[key].RemoveRange(0, 5000);
    }

    public long? TranslateOffset(string topic, int partition, long sourceOffset)
    {
        var key = $"{topic}-{partition}";
        if (!_mappings.TryGetValue(key, out var mappings))
            return null;

        // Find closest mapping
        var mapping = mappings
            .OrderBy(m => Math.Abs(m.SourceOffset - sourceOffset))
            .FirstOrDefault();

        if (mapping == null)
            return null;

        // Calculate offset delta
        var delta = sourceOffset - mapping.SourceOffset;
        return mapping.TargetOffset + delta;
    }
}

public class OffsetMapping
{
    public long SourceOffset { get; set; }
    public long TargetOffset { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## Step 6: Add Health Checks

Monitor replication bridge health:

```csharp
public class ReplicationHealth
{
    public bool IsRunning { get; set; }
    public DateTime LastActivityTime { get; set; }
    public Dictionary<string, ReplicationStats> TopicStats { get; set; } = new();
    public List<string> Errors { get; set; } = new();

    public bool IsHealthy =>
        IsRunning &&
        (DateTime.UtcNow - LastActivityTime) < TimeSpan.FromMinutes(1) &&
        Errors.Count == 0;
}

public ReplicationHealth GetHealth()
{
    return new ReplicationHealth
    {
        IsRunning = _cts != null && !_cts.IsCancellationRequested,
        LastActivityTime = _stats.Values
            .Select(s => s.LastReplicationTimestamp)
            .DefaultIfEmpty(DateTime.MinValue)
            .Max(),
        TopicStats = new Dictionary<string, ReplicationStats>(_stats),
        Errors = new List<string>(_recentErrors)
    };
}
```

## Step 7: Test the Replication

Verify replication works correctly:

```csharp
// Create test data on source
Console.WriteLine("Creating test data on source cluster...");
for (int i = 0; i < 100; i++)
{
    await sourceProducer.ProduceAsync("orders", $"key-{i}", $"Order {i}");
}

// Wait for replication
await Task.Delay(5000);

// Verify on target
Console.WriteLine("Verifying replication on target cluster...");
var targetConsumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = targetBootstrapServers;
    options.GroupId = "verify-replication";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

targetConsumer.Subscribe(new[] { "orders" });

var replicated = 0;
var timeout = DateTime.UtcNow.AddSeconds(10);
while (DateTime.UtcNow < timeout)
{
    var msg = targetConsumer.Consume(TimeSpan.FromMilliseconds(100));
    if (msg != null) replicated++;
    if (replicated >= 100) break;
}

Console.WriteLine($"Replicated messages: {replicated}/100");
```

## Complete Solution

See `completed/Program.cs` for the full implementation including:

- Full replication bridge with all features
- Lag monitoring and alerting
- Offset translation for failover
- Health check endpoints
- Interactive demo with two clusters

## Knowledge Check

1. What is the difference between synchronous and asynchronous replication?
2. How does MirrorMaker track replication progress?
3. Why is offset translation important for failover?
4. How do you prevent replication loops in bidirectional setups?
5. What metrics should you monitor for replication health?

## Expected Output

```
=== Surgewave Bootcamp: Lesson 15.2 - Cross-Datacenter Replication ===

Starting source cluster on port 9092...
Starting target cluster on port 9093...

Configuring replication bridge...
Replication bridge started
  Source: localhost:9092
  Target: localhost:9093
  Topics: orders, payments

Creating test data on source cluster...
  Produced 100 orders
  Produced 50 payments

Monitoring replication...
  [orders] Replicated: 45, Offset lag: 55, Time lag: 1.2s
  [payments] Replicated: 20, Offset lag: 30, Time lag: 0.8s
  [orders] Replicated: 100, Offset lag: 0, Time lag: 0.1s
  [payments] Replicated: 50, Offset lag: 0, Time lag: 0.1s

Verifying replication on target cluster...
  orders: 100/100 messages replicated
  payments: 50/50 messages replicated

Replication bridge healthy: True

Lesson complete!
```

---

**Next:** [Lesson 15.3: Failover Procedures](../lesson-3/README.md)
