# Lesson 27.3: Instructions

Follow these step-by-step instructions to implement cross-cloud replication.

## Step 1: Define Replication Configuration

Create data structures for cross-cloud replication:

```csharp
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
```

## Step 2: Set Up Multi-Cloud Clusters

Start clusters representing different cloud providers:

```csharp
var cloudClusters = new Dictionary<string, (SurgewaveRuntime Runtime, string Provider)>();

Console.WriteLine("--- Starting Multi-Cloud Clusters ---");

// AWS Cluster
var awsCluster = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
cloudClusters["aws"] = (awsCluster, "AWS");
Console.WriteLine($"[AWS] Surgewave cluster started on port {awsCluster.Port}");

// Azure Cluster
var azureCluster = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
cloudClusters["azure"] = (azureCluster, "Azure");
Console.WriteLine($"[Azure] Surgewave cluster started on port {azureCluster.Port}");

// GCP Cluster
var gcpCluster = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
cloudClusters["gcp"] = (gcpCluster, "GCP");
Console.WriteLine($"[GCP] Surgewave cluster started on port {gcpCluster.Port}");
```

## Step 3: Implement Cross-Cloud Replicator

Build a replicator that moves data between clouds:

```csharp
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

        while (!ct.IsCancellationRequested)
        {
            var result = await consumer.ConsumeAsync(ct);
            if (result == null) continue;

            var targetTopic = _topicTransform?.Invoke(result.Topic) ?? result.Topic;

            await producer.ProduceAsync(targetTopic, result.Key ?? "", result.Value ?? "");

            _messagesReplicated++;
            _bytesReplicated += (result.Value?.Length ?? 0);
        }
    }

    public int MessagesReplicated => _messagesReplicated;
    public long BytesReplicated => _bytesReplicated;
}
```

## Step 4: Add Conflict Detection

Implement conflict detection for bidirectional replication:

```csharp
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
```

## Step 5: Implement Topic Filtering

Add filtering to control what gets replicated:

```csharp
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
        // Simple wildcard matching (* = any)
        if (pattern == "*") return true;
        if (pattern.EndsWith("*"))
            return topic.StartsWith(pattern[..^1]);
        if (pattern.StartsWith("*"))
            return topic.EndsWith(pattern[1..]);
        return topic == pattern;
    }
}

// Usage
var filter = new TopicFilter(
    includePatterns: new[] { "orders.*", "events.*" },
    excludePatterns: new[] { "*.internal", "*.debug" }
);

Console.WriteLine($"orders.created: {filter.ShouldReplicate("orders.created")}");     // true
Console.WriteLine($"orders.internal: {filter.ShouldReplicate("orders.internal")}");   // false
Console.WriteLine($"metrics.cpu: {filter.ShouldReplicate("metrics.cpu")}");          // false
```

## Step 6: Monitor Replication Lag

Track how far behind replication is:

```csharp
public class ReplicationLagMonitor
{
    private readonly Dictionary<string, long> _sourceOffsets = new();
    private readonly Dictionary<string, long> _targetOffsets = new();

    public void UpdateSourceOffset(string topic, int partition, long offset)
    {
        var key = $"{topic}-{partition}";
        _sourceOffsets[key] = offset;
    }

    public void UpdateTargetOffset(string topic, int partition, long offset)
    {
        var key = $"{topic}-{partition}";
        _targetOffsets[key] = offset;
    }

    public long GetLag(string topic, int partition)
    {
        var key = $"{topic}-{partition}";
        var sourceOffset = _sourceOffsets.GetValueOrDefault(key, 0);
        var targetOffset = _targetOffsets.GetValueOrDefault(key, 0);
        return sourceOffset - targetOffset;
    }

    public long GetTotalLag()
    {
        return _sourceOffsets.Keys
            .Sum(key => _sourceOffsets[key] - _targetOffsets.GetValueOrDefault(key, 0));
    }
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Multi-cloud cluster setup (AWS, Azure, GCP)
- Cross-cloud replicator implementation
- Bidirectional replication with conflict detection
- Topic filtering and transformation
- Replication lag monitoring

## Sample Output

```
=== Surgewave Bootcamp: Lesson 27.3 - Cross-Cloud Replication ===

--- Starting Multi-Cloud Clusters ---
[AWS] Surgewave cluster started on port 12345
[Azure] Surgewave cluster started on port 12346
[GCP] Surgewave cluster started on port 12347

--- Replication Configuration ---
AWS -> Azure: Bidirectional (orders.*, events.*)
AWS -> GCP: Unidirectional (analytics.*)
Conflict Resolution: LastWriteWins

--- Topic Filtering Demo ---
orders.created: Should replicate: true
orders.internal: Should replicate: false
metrics.cpu: Should replicate: false

--- Cross-Cloud Replication Demo ---
[AWS] Produced 5 orders
[AWS -> Azure] Replicated 5 messages
[Azure] Consumed 5 replicated orders

--- Bidirectional Replication ---
[Azure] Produced 3 orders
[Azure -> AWS] Replicated 3 messages
[AWS] Consumed 3 replicated orders

--- Conflict Detection ---
[AWS] Updated order-001 at 2024-01-15T10:00:00Z
[Azure] Updated order-001 at 2024-01-15T10:00:00.500Z
CONFLICT detected for order-001 (AWS vs Azure)
Resolved using LastWriteWins: Azure version accepted

--- Replication Statistics ---
AWS -> Azure: 8 messages, 2.4 KB, lag: 0
Azure -> AWS: 3 messages, 0.9 KB, lag: 0
AWS -> GCP: 5 messages, 1.5 KB, lag: 0

Lesson complete!
```

## Knowledge Check

1. What is the main advantage of unidirectional vs bidirectional replication?
2. How does Last-Write-Wins conflict resolution work?
3. Why should cross-cloud replication be asynchronous?
4. What is replication lag and why is it important to monitor?
5. How can topic partitioning eliminate conflicts entirely?

---

**Next:** [Lesson 27.4: Geo-Routing and Latency Optimization](../lesson-4/README.md)
