# Lesson 4.6: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Start an Embedded Broker

Create an embedded Surgewave broker for admin operations:

```csharp
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)                           // Auto-assign port
    .WithStorageMode(StorageMode.Memory)   // In-memory for demo
    .WithAutoCreateTopics(false)           // We'll create topics manually
    .WithPartitions(3)                     // Default partitions
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
```

## Step 2: Create Topics Programmatically

Use the LogManager to create topics with specific configurations:

```csharp
// Create a topic with custom partition count
var ordersMetadata = await runtime.LogManager.CreateTopicAsync(
    topicName: "orders",
    partitionCount: 6,
    replicationFactor: 1,
    config: new Dictionary<string, string>
    {
        ["retention.ms"] = "604800000",     // 7 days
        ["segment.bytes"] = "536870912"     // 512 MB
    });

Console.WriteLine($"Created topic '{ordersMetadata.Name}' with {ordersMetadata.PartitionCount} partitions");

// Create another topic with different settings
var eventsMetadata = await runtime.LogManager.CreateTopicAsync(
    topicName: "events",
    partitionCount: 12,
    replicationFactor: 1,
    config: new Dictionary<string, string>
    {
        ["retention.bytes"] = "1073741824", // 1 GB per partition
        ["cleanup.policy"] = "delete"
    });
```

## Step 3: List All Topics

Enumerate topics and their metadata:

```csharp
Console.WriteLine("\n--- All Topics ---");
foreach (var topic in runtime.LogManager.ListTopics())
{
    Console.WriteLine($"Topic: {topic.Name}");
    Console.WriteLine($"  TopicId: {topic.TopicId}");
    Console.WriteLine($"  Partitions: {topic.PartitionCount}");
    Console.WriteLine($"  Replication Factor: {topic.ReplicationFactor}");
    Console.WriteLine($"  Created: {topic.CreatedAt:u}");
    Console.WriteLine($"  Config entries: {topic.Config.Count}");
}
```

## Step 4: Describe Topic Details

Get detailed information about a specific topic:

```csharp
var metadata = runtime.LogManager.GetTopicMetadata("orders");
if (metadata != null)
{
    Console.WriteLine($"\n--- Topic Details: {metadata.Name} ---");
    Console.WriteLine($"TopicId: {metadata.TopicId}");
    Console.WriteLine($"Partitions: {metadata.PartitionCount}");

    // Show configuration
    Console.WriteLine("Configuration:");
    foreach (var (key, value) in metadata.Config)
    {
        Console.WriteLine($"  {key} = {value}");
    }
}
```

## Step 5: Modify Topic Configuration

Update topic settings at runtime:

```csharp
// Update retention to 14 days
var configUpdates = new Dictionary<string, string>
{
    ["retention.ms"] = "1209600000"  // 14 days
};

var success = runtime.LogManager.UpdateTopicConfig("orders", configUpdates);
Console.WriteLine($"\nConfig update success: {success}");

// Verify the change
var updatedConfig = runtime.LogManager.GetTopicConfig("orders");
Console.WriteLine($"New retention.ms: {updatedConfig?["retention.ms"]}");
```

## Step 6: Add Partitions to Existing Topic

Scale a topic by adding more partitions:

```csharp
// Increase partitions from 6 to 12
var addResult = runtime.LogManager.AddPartitions("orders", 12);
Console.WriteLine($"\nAdd partitions success: {addResult}");

// Verify
var updated = runtime.LogManager.GetTopicMetadata("orders");
Console.WriteLine($"New partition count: {updated?.PartitionCount}");
```

**Note:** Partition count can only be increased, never decreased.

## Step 7: Delete Topics

Remove topics when no longer needed:

```csharp
// Create a temporary topic
await runtime.LogManager.CreateTopicAsync("temp-topic", 1);
Console.WriteLine("\nCreated temp-topic");

// Delete it
await runtime.LogManager.DeleteTopicAsync("temp-topic");
Console.WriteLine("Deleted temp-topic");

// Verify deletion
var deletedTopic = runtime.LogManager.GetTopicMetadata("temp-topic");
Console.WriteLine($"Topic exists after delete: {deletedTopic != null}");
```

## Step 8: ACL Concepts (Demonstration)

Understand how ACLs work in Surgewave:

```csharp
// ACL entries define who can do what on which resources
// In production, you would configure the AclAuthorizer

Console.WriteLine("\n--- ACL Concepts ---");

// Example ACL structure (for reference)
// AclEntry {
//     Principal = "User:alice",
//     Host = "*",
//     ResourceType = AclResourceType.Topic,
//     ResourceName = "orders",
//     Operation = AclOperation.Read,
//     Permission = AclPermission.Allow
// }

Console.WriteLine("ACL Components:");
Console.WriteLine("  Principal: Who (User:alice, User:*)");
Console.WriteLine("  Host: From where (* for any)");
Console.WriteLine("  ResourceType: Topic, Group, Cluster");
Console.WriteLine("  ResourceName: Specific or wildcard (*)");
Console.WriteLine("  Operation: Read, Write, Create, Delete, etc.");
Console.WriteLine("  Permission: Allow or Deny");
```

## Step 9: Quota Concepts (Demonstration)

Understand producer/consumer quota management:

```csharp
Console.WriteLine("\n--- Quota Management ---");

// Quotas control resource usage per client
// QuotaConfig {
//     Enabled = true,
//     ProducerBytesPerSecond = 10_000_000,  // 10 MB/sec
//     ConsumerBytesPerSecond = 20_000_000,  // 20 MB/sec
//     MaxThrottleTimeMs = 30_000            // Max 30s throttle
// }

Console.WriteLine("Quota Settings:");
Console.WriteLine("  Producer: Limits produce throughput per client");
Console.WriteLine("  Consumer: Limits fetch throughput per client");
Console.WriteLine("  Burst: Token bucket capacity for traffic spikes");
Console.WriteLine("  Throttle: Time delay when quota exceeded");
```

## Step 10: Cluster Metadata Inspection

Examine broker and cluster state:

```csharp
Console.WriteLine("\n--- Cluster Metadata ---");
Console.WriteLine($"Broker ID: {runtime.BrokerId}");
Console.WriteLine($"Host: {runtime.Host}");
Console.WriteLine($"Port: {runtime.Port}");
Console.WriteLine($"Bootstrap Servers: {runtime.BootstrapServers}");
Console.WriteLine($"Data Directory: {runtime.DataDirectory}");
Console.WriteLine($"Cluster Mode: {runtime.IsClusterEnabled}");
Console.WriteLine($"Is Controller: {runtime.IsController}");
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. What happens if you try to create a topic that already exists?
2. Can you decrease the number of partitions in a topic?
3. What is the difference between `retention.ms` and `retention.bytes`?
4. How do ACL Deny rules interact with Allow rules?

## Best Practices

1. **Topic Naming**: Use consistent naming conventions (e.g., `domain.entity.version`)
2. **Partition Planning**: Choose partition count based on expected throughput
3. **Configuration**: Set retention and segment sizes appropriate for your data volume
4. **ACLs**: Follow the principle of least privilege
5. **Quotas**: Protect shared clusters from noisy neighbors

---

**Congratulations!** You've completed Lesson 4.6: Admin API.

**Next Unit:** -> [Unit 11: Kafka Migration](../../unit-11/README.md)
