// Surgewave Bootcamp - Unit 27, Lesson 3: Cross-Cloud Replication
// COMPLETED SOLUTION

using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 27.3 - Cross-Cloud Replication ===");
Console.WriteLine();

// Step 1 - Set up multi-cloud clusters
var cloudClusters = new Dictionary<string, (SurgewaveRuntime Runtime, string Provider)>();

Console.WriteLine("--- Starting Multi-Cloud Clusters ---");

// AWS Cluster (Primary)
var awsCluster = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
cloudClusters["aws"] = (awsCluster, "AWS");
Console.WriteLine($"[AWS] Surgewave cluster started on port {awsCluster.Port}");

// Azure Cluster (DR)
var azureCluster = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
cloudClusters["azure"] = (azureCluster, "Azure");
Console.WriteLine($"[Azure] Surgewave cluster started on port {azureCluster.Port}");

// GCP Cluster (Analytics)
var gcpCluster = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
cloudClusters["gcp"] = (gcpCluster, "GCP");
Console.WriteLine($"[GCP] Surgewave cluster started on port {gcpCluster.Port}");

// Step 2 - Display replication configuration
Console.WriteLine("\n--- Replication Configuration ---");
var replConfigs = new[]
{
    new ReplicationConfig("aws", "azure", ReplicationMode.Bidirectional,
        new[] { "orders.*", "events.*" }, new[] { "*.internal" }, ConflictResolution.LastWriteWins),
    new ReplicationConfig("aws", "gcp", ReplicationMode.Unidirectional,
        new[] { "analytics.*" }, Array.Empty<string>(), ConflictResolution.SourceWins)
};

foreach (var config in replConfigs)
{
    Console.WriteLine($"{config.SourceCluster.ToUpper()} -> {config.TargetCluster.ToUpper()}: " +
        $"{config.Mode} ({string.Join(", ", config.IncludeTopics)})");
}
Console.WriteLine($"Conflict Resolution: {replConfigs[0].ConflictStrategy}");

// Step 3 - Demonstrate topic filtering
Console.WriteLine("\n--- Topic Filtering Demo ---");
var filter = new TopicFilter(
    includePatterns: new[] { "orders.*", "events.*" },
    excludePatterns: new[] { "*.internal", "*.debug" }
);

var testTopics = new[] { "orders.created", "orders.internal", "events.user", "metrics.cpu", "logs.debug" };
foreach (var topic in testTopics)
{
    Console.WriteLine($"{topic}: Should replicate: {filter.ShouldReplicate(topic)}");
}

// Step 4 - Cross-cloud replication demo
Console.WriteLine("\n--- Cross-Cloud Replication Demo ---");

// Produce to AWS
await using var awsProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{awsCluster.Port}";
    opt.ClientId = "aws-producer";
});

var orders = new[]
{
    new { OrderId = "US-001", Amount = 99.99, Region = "us-east", Timestamp = DateTime.UtcNow },
    new { OrderId = "US-002", Amount = 149.50, Region = "us-east", Timestamp = DateTime.UtcNow },
    new { OrderId = "US-003", Amount = 75.00, Region = "us-west", Timestamp = DateTime.UtcNow },
    new { OrderId = "US-004", Amount = 200.00, Region = "us-east", Timestamp = DateTime.UtcNow },
    new { OrderId = "US-005", Amount = 50.00, Region = "us-west", Timestamp = DateTime.UtcNow }
};

var ordersTopic = "orders.created";
foreach (var order in orders)
{
    await awsProducer.ProduceAsync(ordersTopic, order.OrderId, JsonSerializer.Serialize(order));
}
Console.WriteLine($"[AWS] Produced {orders.Length} orders to {ordersTopic}");

// Create replicator AWS -> Azure
var awsToAzureReplicator = new CrossCloudReplicator(
    sourceBootstrap: $"localhost:{awsCluster.Port}",
    targetBootstrap: $"localhost:{azureCluster.Port}",
    topics: new[] { ordersTopic },
    topicTransform: topic => $"replicated.{topic}"
);

// Run replication
using var replCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
var replicationTask = Task.Run(() => awsToAzureReplicator.ReplicateAsync(replCts.Token));

// Wait a bit for replication
await Task.Delay(1000);
Console.WriteLine($"[AWS -> Azure] Replicated {awsToAzureReplicator.MessagesReplicated} messages");

// Consume from Azure to verify
await using var azureConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{azureCluster.Port}";
    opt.GroupId = "azure-consumer";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
azureConsumer.Subscribe("replicated.orders.created");

var azureConsumed = 0;
using var consumeCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
try
{
    while (!consumeCts.IsCancellationRequested && azureConsumed < orders.Length)
    {
        var result = await azureConsumer.ConsumeAsync(consumeCts.Token);
        if (result != null) azureConsumed++;
    }
}
catch (OperationCanceledException) { }
Console.WriteLine($"[Azure] Consumed {azureConsumed} replicated orders");

// Step 5 - Bidirectional replication demo
Console.WriteLine("\n--- Bidirectional Replication ---");

// Produce from Azure
await using var azureProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{azureCluster.Port}";
    opt.ClientId = "azure-producer";
});

var euOrders = new[]
{
    new { OrderId = "EU-001", Amount = 199.99, Region = "eu-west", Timestamp = DateTime.UtcNow },
    new { OrderId = "EU-002", Amount = 89.00, Region = "eu-central", Timestamp = DateTime.UtcNow },
    new { OrderId = "EU-003", Amount = 125.00, Region = "eu-west", Timestamp = DateTime.UtcNow }
};

foreach (var order in euOrders)
{
    await azureProducer.ProduceAsync(ordersTopic, order.OrderId, JsonSerializer.Serialize(order));
}
Console.WriteLine($"[Azure] Produced {euOrders.Length} orders");

// Replicate Azure -> AWS
var azureToAwsReplicator = new CrossCloudReplicator(
    sourceBootstrap: $"localhost:{azureCluster.Port}",
    targetBootstrap: $"localhost:{awsCluster.Port}",
    topics: new[] { ordersTopic },
    topicTransform: topic => $"replicated.{topic}"
);

using var repl2Cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
_ = Task.Run(() => azureToAwsReplicator.ReplicateAsync(repl2Cts.Token));

await Task.Delay(1000);
Console.WriteLine($"[Azure -> AWS] Replicated {azureToAwsReplicator.MessagesReplicated} messages");

// Consume from AWS to verify
await using var awsConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{awsCluster.Port}";
    opt.GroupId = "aws-consumer";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
awsConsumer.Subscribe("replicated.orders.created");

var awsConsumed = 0;
using var awsConsumeCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
try
{
    while (!awsConsumeCts.IsCancellationRequested && awsConsumed < euOrders.Length)
    {
        var result = await awsConsumer.ConsumeAsync(awsConsumeCts.Token);
        if (result != null) awsConsumed++;
    }
}
catch (OperationCanceledException) { }
Console.WriteLine($"[AWS] Consumed {awsConsumed} replicated orders from Azure");

// Step 6 - Conflict detection demo
Console.WriteLine("\n--- Conflict Detection ---");
var conflictDetector = new ConflictDetector();

// Simulate concurrent updates
var awsUpdate = DateTime.UtcNow;
var azureUpdate = awsUpdate.AddMilliseconds(500);

Console.WriteLine($"[AWS] Updated order-001 at {awsUpdate:O}");
var result1 = conflictDetector.CheckConflict("order-001", awsUpdate, "aws");
Console.WriteLine($"  -> Conflict: {result1.IsConflict}");

Console.WriteLine($"[Azure] Updated order-001 at {azureUpdate:O}");
var result2 = conflictDetector.CheckConflict("order-001", azureUpdate, "azure");
Console.WriteLine($"  -> Conflict: {result2.IsConflict}");

if (result2.IsConflict)
{
    Console.WriteLine($"CONFLICT detected for order-001 ({result2.ExistingSource} vs {result2.NewSource})");
    Console.WriteLine("Resolved using LastWriteWins: Azure version accepted (later timestamp)");
}

// Step 7 - Display statistics
Console.WriteLine("\n--- Replication Statistics ---");
Console.WriteLine($"AWS -> Azure: {awsToAzureReplicator.MessagesReplicated} messages, " +
    $"{awsToAzureReplicator.BytesReplicated} bytes");
Console.WriteLine($"Azure -> AWS: {azureToAwsReplicator.MessagesReplicated} messages, " +
    $"{azureToAwsReplicator.BytesReplicated} bytes");

// Cleanup
await awsCluster.DisposeAsync();
await azureCluster.DisposeAsync();
await gcpCluster.DisposeAsync();

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use async replication for cross-cloud (avoid latency in critical path)");
Console.WriteLine("2. Choose conflict resolution strategy based on use case");
Console.WriteLine("3. Filter topics to replicate only what's needed");
Console.WriteLine("4. Monitor replication lag continuously");
Console.WriteLine("5. Test cloud failover scenarios regularly");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Supporting types

public record ReplicationConfig(
    string SourceCluster,
    string TargetCluster,
    ReplicationMode Mode,
    string[] IncludeTopics,
    string[] ExcludeTopics,
    ConflictResolution ConflictStrategy
);

public enum ReplicationMode
{
    Unidirectional,  // Source -> Target only
    Bidirectional    // Both directions
}

public enum ConflictResolution
{
    LastWriteWins,   // Latest timestamp wins
    SourceWins,      // Source cluster always wins
    TargetWins,      // Target cluster always wins
    Manual           // Flag for manual resolution
}

public class TopicFilter
{
    private readonly string[] _includePatterns;
    private readonly string[] _excludePatterns;

    public TopicFilter(string[] includePatterns, string[] excludePatterns)
    {
        _includePatterns = includePatterns;
        _excludePatterns = excludePatterns;
    }

    public bool ShouldReplicate(string topic)
    {
        // Check exclusions first
        foreach (var pattern in _excludePatterns)
        {
            if (MatchesPattern(topic, pattern))
                return false;
        }

        // Check inclusions
        if (_includePatterns.Length == 0)
            return true;

        foreach (var pattern in _includePatterns)
        {
            if (MatchesPattern(topic, pattern))
                return true;
        }

        return false;
    }

    private bool MatchesPattern(string topic, string pattern)
    {
        if (pattern == "*") return true;
        if (pattern.EndsWith(".*"))
        {
            var prefix = pattern[..^2];
            return topic.StartsWith(prefix + ".");
        }
        if (pattern.EndsWith("*"))
            return topic.StartsWith(pattern[..^1]);
        if (pattern.StartsWith("*"))
            return topic.EndsWith(pattern[1..]);
        return topic == pattern;
    }
}

public class CrossCloudReplicator
{
    private readonly string _sourceBootstrap;
    private readonly string _targetBootstrap;
    private readonly string[] _topics;
    private readonly Func<string, string>? _topicTransform;
    private int _messagesReplicated;
    private long _bytesReplicated;

    public CrossCloudReplicator(
        string sourceBootstrap,
        string targetBootstrap,
        string[] topics,
        Func<string, string>? topicTransform = null)
    {
        _sourceBootstrap = sourceBootstrap;
        _targetBootstrap = targetBootstrap;
        _topics = topics;
        _topicTransform = topicTransform;
    }

    public async Task ReplicateAsync(CancellationToken ct)
    {
        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = _sourceBootstrap;
            opt.GroupId = $"replicator-{Guid.NewGuid():N}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        foreach (var topic in _topics)
        {
            consumer.Subscribe(topic);
        }

        await using var producer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = _targetBootstrap;
            opt.ClientId = "cross-cloud-replicator";
        });

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var result = await consumer.ConsumeAsync(ct);
                if (result == null) continue;

                var targetTopic = _topicTransform?.Invoke(result.Topic) ?? result.Topic;

                await producer.ProduceAsync(targetTopic, result.Key ?? "", result.Value ?? "");

                _messagesReplicated++;
                _bytesReplicated += result.Value?.Length ?? 0;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
    }

    public int MessagesReplicated => _messagesReplicated;
    public long BytesReplicated => _bytesReplicated;
}

public class ConflictDetector
{
    private readonly Dictionary<string, (DateTime Timestamp, string Source)> _lastSeen = new();

    public ConflictResult CheckConflict(string key, DateTime timestamp, string source)
    {
        if (!_lastSeen.TryGetValue(key, out var existing))
        {
            _lastSeen[key] = (timestamp, source);
            return new ConflictResult(false, null, null);
        }

        // Same source updating again - no conflict
        if (existing.Source == source)
        {
            _lastSeen[key] = (timestamp, source);
            return new ConflictResult(false, null, null);
        }

        // Different sources with close timestamps - potential conflict
        var timeDiff = Math.Abs((timestamp - existing.Timestamp).TotalMilliseconds);
        if (timeDiff < 1000) // Within 1 second
        {
            _lastSeen[key] = (timestamp, source); // Accept newer
            return new ConflictResult(true, existing.Source, source);
        }

        // Update with newer timestamp
        if (timestamp > existing.Timestamp)
        {
            _lastSeen[key] = (timestamp, source);
        }

        return new ConflictResult(false, null, null);
    }
}

public record ConflictResult(bool IsConflict, string? ExistingSource, string? NewSource);