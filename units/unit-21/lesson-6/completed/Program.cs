// Surgewave Bootcamp - Unit 4, Lesson 6: Admin API
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 4.6 - Admin API ===");
        Console.WriteLine();

        // Start an embedded broker
        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(false)  // We'll create topics manually
            .WithPartitions(3)
            .Build()
            .StartAsync();

        Console.WriteLine($"Broker started on port {runtime.Port}");
        Console.WriteLine();

        // ===========================================
        // 1. Create topics with custom configurations
        // ===========================================
        Console.WriteLine("--- Creating Topics ---");

        // Create "orders" topic with custom configuration
        var ordersMetadata = await runtime.LogManager.CreateTopicAsync(
            topicName: "orders",
            partitionCount: 6,
            replicationFactor: 1,
            config: new Dictionary<string, string>
            {
                ["retention.ms"] = "604800000",     // 7 days
                ["segment.bytes"] = "536870912"    // 512 MB
            });
        Console.WriteLine($"Created topic '{ordersMetadata.Name}' with {ordersMetadata.PartitionCount} partitions");

        // Create "events" topic
        var eventsMetadata = await runtime.LogManager.CreateTopicAsync(
            topicName: "events",
            partitionCount: 12,
            replicationFactor: 1,
            config: new Dictionary<string, string>
            {
                ["retention.bytes"] = "1073741824", // 1 GB per partition
                ["cleanup.policy"] = "delete"
            });
        Console.WriteLine($"Created topic '{eventsMetadata.Name}' with {eventsMetadata.PartitionCount} partitions");

        // Create "logs" topic with compact cleanup policy
        var logsMetadata = await runtime.LogManager.CreateTopicAsync(
            topicName: "logs",
            partitionCount: 4,
            replicationFactor: 1,
            config: new Dictionary<string, string>
            {
                ["cleanup.policy"] = "compact",
                ["min.cleanable.dirty.ratio"] = "0.5"
            });
        Console.WriteLine($"Created topic '{logsMetadata.Name}' with {logsMetadata.PartitionCount} partitions");

        // ===========================================
        // 2. List all topics
        // ===========================================
        Console.WriteLine("\n--- All Topics ---");
        Console.WriteLine($"{"Topic",-15} {"Partitions",12} {"Replication",12} {"Created",22}");
        Console.WriteLine(new string('-', 65));

        foreach (var topic in runtime.LogManager.ListTopics())
        {
            Console.WriteLine($"{topic.Name,-15} {topic.PartitionCount,12} {topic.ReplicationFactor,12} {topic.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        // ===========================================
        // 3. Describe a specific topic
        // ===========================================
        Console.WriteLine("\n--- Topic Details: orders ---");
        var metadata = runtime.LogManager.GetTopicMetadata("orders");
        if (metadata != null)
        {
            Console.WriteLine($"Name: {metadata.Name}");
            Console.WriteLine($"TopicId: {metadata.TopicId}");
            Console.WriteLine($"Partitions: {metadata.PartitionCount}");
            Console.WriteLine($"Replication Factor: {metadata.ReplicationFactor}");
            Console.WriteLine($"Created: {metadata.CreatedAt:u}");
            Console.WriteLine($"Cleanup Policy: {metadata.CleanupPolicy}");
            Console.WriteLine("Configuration:");
            foreach (var (key, value) in metadata.Config)
            {
                Console.WriteLine($"  {key} = {value}");
            }
        }

        // ===========================================
        // 4. Update topic configuration
        // ===========================================
        Console.WriteLine("\n--- Update Configuration ---");

        // Change retention to 14 days
        var configUpdates = new Dictionary<string, string>
        {
            ["retention.ms"] = "1209600000"  // 14 days
        };

        var updateSuccess = runtime.LogManager.UpdateTopicConfig("orders", configUpdates);
        Console.WriteLine($"Config update success: {updateSuccess}");

        // Verify the change
        var updatedConfig = runtime.LogManager.GetTopicConfig("orders");
        if (updatedConfig != null && updatedConfig.TryGetValue("retention.ms", out var retentionMs))
        {
            var retentionDays = long.Parse(retentionMs) / (1000 * 60 * 60 * 24);
            Console.WriteLine($"New retention.ms: {retentionMs} ({retentionDays} days)");
        }

        // ===========================================
        // 5. Add partitions to a topic
        // ===========================================
        Console.WriteLine("\n--- Add Partitions ---");

        var currentPartitions = runtime.LogManager.GetTopicMetadata("orders")?.PartitionCount ?? 0;
        Console.WriteLine($"Current partition count: {currentPartitions}");

        // Increase from 6 to 12 partitions
        var addResult = runtime.LogManager.AddPartitions("orders", 12);
        Console.WriteLine($"Add partitions success: {addResult}");

        var newPartitions = runtime.LogManager.GetTopicMetadata("orders")?.PartitionCount ?? 0;
        Console.WriteLine($"New partition count: {newPartitions}");

        // Try to decrease partitions (should fail)
        var decreaseResult = runtime.LogManager.AddPartitions("orders", 6);
        Console.WriteLine($"Decrease partitions success: {decreaseResult} (expected: False)");

        // ===========================================
        // 6. Delete a topic
        // ===========================================
        Console.WriteLine("\n--- Delete Topic ---");

        // Create a temporary topic
        await runtime.LogManager.CreateTopicAsync("temp-topic", 1);
        Console.WriteLine("Created temp-topic");

        // Verify it exists
        var tempExists = runtime.LogManager.GetTopicMetadata("temp-topic") != null;
        Console.WriteLine($"temp-topic exists: {tempExists}");

        // Delete it
        await runtime.LogManager.DeleteTopicAsync("temp-topic");
        Console.WriteLine("Deleted temp-topic");

        // Verify deletion
        var tempExistsAfter = runtime.LogManager.GetTopicMetadata("temp-topic") != null;
        Console.WriteLine($"temp-topic exists after delete: {tempExistsAfter}");

        // ===========================================
        // 7. ACL Concepts (informational)
        // ===========================================
        Console.WriteLine("\n--- ACL Concepts ---");
        Console.WriteLine("ACL Entry Structure:");
        Console.WriteLine("  Principal: User identity (e.g., User:alice, User:*)");
        Console.WriteLine("  Host: Source host filter (* for any)");
        Console.WriteLine("  ResourceType: Topic, Group, Cluster, TransactionalId");
        Console.WriteLine("  ResourceName: Specific name or wildcard (*)");
        Console.WriteLine("  PatternType: Literal, Prefixed, Suffix, Regex");
        Console.WriteLine("  Operation: Read, Write, Create, Delete, Alter, Describe, etc.");
        Console.WriteLine("  Permission: Allow or Deny (Deny takes precedence)");

        // ===========================================
        // 8. Quota Concepts (informational)
        // ===========================================
        Console.WriteLine("\n--- Quota Management ---");
        Console.WriteLine("Quota Types:");
        Console.WriteLine("  ProducerBytesPerSecond: Max produce throughput per client");
        Console.WriteLine("  ConsumerBytesPerSecond: Max fetch throughput per client");
        Console.WriteLine("  ProducerBurstBytes: Token bucket capacity for burst traffic");
        Console.WriteLine("  ConsumerBurstBytes: Token bucket capacity for burst traffic");
        Console.WriteLine("  MaxThrottleTimeMs: Maximum delay when quota exceeded (default 30s)");

        // ===========================================
        // 9. Cluster Metadata
        // ===========================================
        Console.WriteLine("\n--- Cluster Metadata ---");
        Console.WriteLine($"Broker ID: {runtime.BrokerId}");
        Console.WriteLine($"Host: {runtime.Host}");
        Console.WriteLine($"Port: {runtime.Port}");
        Console.WriteLine($"Bootstrap Servers: {runtime.BootstrapServers}");
        Console.WriteLine($"Data Directory: {runtime.DataDirectory}");
        Console.WriteLine($"Cluster Mode Enabled: {runtime.IsClusterEnabled}");
        Console.WriteLine($"Is Controller: {runtime.IsController}");

        // ===========================================
        // 10. Summary Statistics
        // ===========================================
        Console.WriteLine("\n--- Summary ---");
        var allTopics = runtime.LogManager.ListTopics().ToList();
        var totalPartitions = allTopics.Sum(t => t.PartitionCount);
        Console.WriteLine($"Total Topics: {allTopics.Count}");
        Console.WriteLine($"Total Partitions: {totalPartitions}");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
