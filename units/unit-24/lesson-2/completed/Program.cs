// Surgewave Bootcamp - Unit 15, Lesson 2: Cross-Datacenter Replication
// COMPLETED SOLUTION

using System.Collections.Concurrent;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 15.2 - Cross-Datacenter Replication ===");
Console.WriteLine();

// Start two Surgewave runtimes simulating different datacenters
Console.WriteLine("Starting source cluster (DC-A)...");
await using var sourceRuntime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
Console.WriteLine($"  Source cluster started on port {sourceRuntime.Port}");

Console.WriteLine("Starting target cluster (DC-B)...");
await using var targetRuntime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
Console.WriteLine($"  Target cluster started on port {targetRuntime.Port}");
Console.WriteLine();

// Step 1: Create replication configuration
var config = new ReplicationConfig
{
    SourceBootstrapServers = $"localhost:{sourceRuntime.Port}",
    TargetBootstrapServers = $"localhost:{targetRuntime.Port}",
    Topics = new List<string> { "orders", "payments" },
    TopicMappingMode = "identity",
    LagAlertThreshold = TimeSpan.FromMinutes(1),
    CheckpointInterval = TimeSpan.FromSeconds(5)
};

// Step 2: Create and start replication bridge
await using var bridge = new ReplicationBridge(config);
await bridge.StartAsync();
Console.WriteLine("Replication bridge started");
Console.WriteLine($"  Source: {config.SourceBootstrapServers}");
Console.WriteLine($"  Target: {config.TargetBootstrapServers}");
Console.WriteLine($"  Topics: {string.Join(", ", config.Topics)}");
Console.WriteLine();

// Step 3: Create test data on source cluster
Console.WriteLine("Creating test data on source cluster...");
await using var sourceProducer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{sourceRuntime.Port}";
});

for (int i = 0; i < 100; i++)
{
    await sourceProducer.ProduceAsync("orders", $"order-{i}", $"Order {i} data");
}
Console.WriteLine("  Produced 100 orders");

for (int i = 0; i < 50; i++)
{
    await sourceProducer.ProduceAsync("payments", $"payment-{i}", $"Payment {i} data");
}
Console.WriteLine("  Produced 50 payments");
Console.WriteLine();

// Step 4: Monitor replication progress
Console.WriteLine("Monitoring replication...");
for (int i = 0; i < 5; i++)
{
    await Task.Delay(1000);
    var health = bridge.GetHealth();
    foreach (var (topic, stats) in health.TopicStats)
    {
        Console.WriteLine($"  [{topic}] Replicated: {stats.MessageCount}, " +
                          $"Lag: {stats.TimeLag.TotalSeconds:F1}s");
    }
}
Console.WriteLine();

// Step 5: Verify replication on target cluster
Console.WriteLine("Verifying replication on target cluster...");
await using var targetConsumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{targetRuntime.Port}";
    options.GroupId = "verify-replication";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

targetConsumer.Subscribe(new[] { "orders", "payments" });

var messagesByTopic = new Dictionary<string, int>();
var timeout = DateTime.UtcNow.AddSeconds(10);
while (DateTime.UtcNow < timeout)
{
    var msg = await targetConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
    if (msg != null)
    {
        if (!messagesByTopic.ContainsKey(msg.Topic))
            messagesByTopic[msg.Topic] = 0;
        messagesByTopic[msg.Topic]++;
    }
    if (messagesByTopic.Values.Sum() >= 150) break;
}

foreach (var (topic, count) in messagesByTopic.OrderBy(x => x.Key))
{
    var expected = topic == "orders" ? 100 : 50;
    Console.WriteLine($"  {topic}: {count}/{expected} messages replicated");
}
Console.WriteLine();

// Step 6: Check replication health
var finalHealth = bridge.GetHealth();
Console.WriteLine($"Replication bridge healthy: {finalHealth.IsHealthy}");
if (!finalHealth.IsHealthy && finalHealth.Errors.Count > 0)
{
    Console.WriteLine($"  Errors: {string.Join(", ", finalHealth.Errors)}");
}
Console.WriteLine();

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Async replication provides geographic redundancy");
Console.WriteLine("2. Monitor replication lag for RPO compliance");
Console.WriteLine("3. Use offset translation for consumer failover");
Console.WriteLine("4. Topic mapping enables flexible naming");
Console.WriteLine("5. Health checks ensure replication availability");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Configuration classes
public class ReplicationConfig
{
    public string SourceBootstrapServers { get; set; } = "";
    public string TargetBootstrapServers { get; set; } = "";
    public List<string> Topics { get; set; } = new();
    public string TopicMappingMode { get; set; } = "identity";
    public string TopicPrefix { get; set; } = "";
    public bool EnableCompression { get; set; } = true;
    public TimeSpan LagAlertThreshold { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan CheckpointInterval { get; set; } = TimeSpan.FromSeconds(30);
}

public class ReplicationStats
{
    public string Topic { get; set; } = "";
    public long LastSourceOffset { get; set; }
    public long LastReplicatedOffset { get; set; }
    public DateTime LastMessageTimestamp { get; set; } = DateTime.UtcNow;
    public DateTime LastReplicationTimestamp { get; set; } = DateTime.UtcNow;
    public long MessageCount { get; set; }

    public long OffsetLag => LastSourceOffset - LastReplicatedOffset;
    public TimeSpan TimeLag => DateTime.UtcNow - LastMessageTimestamp;
}

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

// Replication Bridge
public class ReplicationBridge : IAsyncDisposable
{
    private readonly ReplicationConfig _config;
    private readonly ConcurrentDictionary<string, ReplicationStats> _stats = new();
    private readonly ConcurrentBag<string> _errors = new();
    private readonly OffsetTranslator _offsetTranslator = new();
    private CancellationTokenSource? _cts;
    private Task? _replicationTask;
    private bool _isRunning;
    private DateTime _lastActivity = DateTime.UtcNow;

    public ReplicationBridge(ReplicationConfig config)
    {
        _config = config;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _isRunning = true;

        // Initialize stats for each topic
        foreach (var topic in _config.Topics)
        {
            _stats[topic] = new ReplicationStats { Topic = topic };
        }

        // Start replication task
        _replicationTask = Task.Run(() => ReplicationLoopAsync(_cts.Token), _cts.Token);

        return Task.CompletedTask;
    }

    private async Task ReplicationLoopAsync(CancellationToken ct)
    {
        await using var sourceConsumer = new SurgewaveConsumer<string, string>(options =>
        {
            options.BootstrapServers = _config.SourceBootstrapServers;
            options.GroupId = $"replication-bridge-{Guid.NewGuid():N}";
            options.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        await using var targetProducer = new SurgewaveProducer<string, string>(options =>
        {
            options.BootstrapServers = _config.TargetBootstrapServers;
        });

        foreach (var topic in _config.Topics)
        {
            sourceConsumer.Subscribe(topic);
        }
        var lastCheckpoint = DateTime.UtcNow;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await sourceConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
                if (result == null) continue;

                // Map topic name
                var targetTopic = MapTopicName(result.Topic);

                // Produce to target cluster
                await targetProducer.ProduceAsync(targetTopic, result.Key, result.Value);

                // Update stats
                UpdateStats(result.Topic, result.Offset);

                // Record offset mapping
                _offsetTranslator.RecordMapping(result.Topic, 0, result.Offset, result.Offset);

                _lastActivity = DateTime.UtcNow;

                // Periodic checkpoint
                if (DateTime.UtcNow - lastCheckpoint > _config.CheckpointInterval)
                {
                    lastCheckpoint = DateTime.UtcNow;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _errors.Add(ex.Message);
                await Task.Delay(1000, ct);
            }
        }

        _isRunning = false;
    }

    private string MapTopicName(string sourceTopic)
    {
        return _config.TopicMappingMode switch
        {
            "prefixed" => $"{_config.TopicPrefix}.{sourceTopic}",
            _ => sourceTopic
        };
    }

    private void UpdateStats(string topic, long offset)
    {
        if (_stats.TryGetValue(topic, out var stats))
        {
            stats.LastReplicatedOffset = offset;
            stats.LastReplicationTimestamp = DateTime.UtcNow;
            stats.LastMessageTimestamp = DateTime.UtcNow;
            stats.MessageCount++;
        }
    }

    public ReplicationHealth GetHealth()
    {
        return new ReplicationHealth
        {
            IsRunning = _isRunning,
            LastActivityTime = _lastActivity,
            TopicStats = _stats.ToDictionary(x => x.Key, x => x.Value),
            Errors = _errors.ToList()
        };
    }

    public long? TranslateOffset(string topic, int partition, long sourceOffset)
    {
        return _offsetTranslator.TranslateOffset(topic, partition, sourceOffset);
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        if (_replicationTask != null)
        {
            try
            {
                await _replicationTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
        _cts?.Dispose();
    }
}

// Offset Translator
public class OffsetTranslator
{
    private readonly ConcurrentDictionary<string, List<OffsetMapping>> _mappings = new();

    public void RecordMapping(string topic, int partition, long sourceOffset, long targetOffset)
    {
        var key = $"{topic}-{partition}";
        var mappings = _mappings.GetOrAdd(key, _ => new List<OffsetMapping>());

        lock (mappings)
        {
            mappings.Add(new OffsetMapping
            {
                SourceOffset = sourceOffset,
                TargetOffset = targetOffset,
                Timestamp = DateTime.UtcNow
            });

            // Keep only recent mappings
            if (mappings.Count > 10000)
                mappings.RemoveRange(0, 5000);
        }
    }

    public long? TranslateOffset(string topic, int partition, long sourceOffset)
    {
        var key = $"{topic}-{partition}";
        if (!_mappings.TryGetValue(key, out var mappings))
            return null;

        lock (mappings)
        {
            var mapping = mappings
                .OrderBy(m => Math.Abs(m.SourceOffset - sourceOffset))
                .FirstOrDefault();

            if (mapping == null)
                return null;

            var delta = sourceOffset - mapping.SourceOffset;
            return mapping.TargetOffset + delta;
        }
    }
}

public class OffsetMapping
{
    public long SourceOffset { get; set; }
    public long TargetOffset { get; set; }
    public DateTime Timestamp { get; set; }
}
