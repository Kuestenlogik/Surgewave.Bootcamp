// Surgewave Bootcamp - Unit 4, Lesson 6: Admin API
//
// In this lesson, you'll learn to:
// - Create, list, and delete topics programmatically
// - Manage topic configurations
// - Understand ACL and quota management
// - Inspect cluster metadata
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
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
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(false)  // We'll create topics manually
            .WithPartitions(3)
            .Build()
            .StartAsync();

        Console.WriteLine($"Broker started on port {runtime.Port}");
        Console.WriteLine();

        // ===========================================
        // TODO 1: Create topics with custom configurations
        // ===========================================
        // Create a topic called "orders" with:
        //   - 6 partitions
        //   - Retention of 7 days (retention.ms = 604800000)
        //   - Segment size of 512 MB (segment.bytes = 536870912)

        // var ordersMetadata = await runtime.LogManager.CreateTopicAsync(...);
        // Console.WriteLine($"Created topic: {ordersMetadata.Name}");

        // Create another topic called "events" with:
        //   - 12 partitions
        //   - Retention bytes of 1 GB per partition

        // ===========================================
        // TODO 2: List all topics
        // ===========================================
        // Iterate through runtime.LogManager.ListTopics()
        // Print each topic's name, partition count, and creation time

        Console.WriteLine("--- All Topics ---");
        // foreach (var topic in runtime.LogManager.ListTopics()) { ... }

        // ===========================================
        // TODO 3: Describe a specific topic
        // ===========================================
        // Get metadata for the "orders" topic
        // Print its TopicId, partition count, and configuration

        Console.WriteLine("\n--- Topic Details ---");
        // var metadata = runtime.LogManager.GetTopicMetadata("orders");
        // if (metadata != null) { ... }

        // ===========================================
        // TODO 4: Update topic configuration
        // ===========================================
        // Change retention to 14 days (retention.ms = 1209600000)
        // Use runtime.LogManager.UpdateTopicConfig()

        Console.WriteLine("\n--- Update Configuration ---");
        // var configUpdates = new Dictionary<string, string> { ... };
        // var success = runtime.LogManager.UpdateTopicConfig("orders", configUpdates);

        // ===========================================
        // TODO 5: Add partitions to a topic
        // ===========================================
        // Increase "orders" from 6 to 12 partitions
        // Use runtime.LogManager.AddPartitions()

        Console.WriteLine("\n--- Add Partitions ---");
        // var addResult = runtime.LogManager.AddPartitions("orders", 12);

        // ===========================================
        // TODO 6: Delete a topic
        // ===========================================
        // Create a temporary topic, then delete it

        Console.WriteLine("\n--- Delete Topic ---");
        // await runtime.LogManager.CreateTopicAsync("temp-topic", 1);
        // await runtime.LogManager.DeleteTopicAsync("temp-topic");

        // ===========================================
        // TODO 7: Print cluster metadata
        // ===========================================
        // Print broker ID, host, port, and other cluster info

        Console.WriteLine("\n--- Cluster Metadata ---");
        // Console.WriteLine($"Broker ID: {runtime.BrokerId}");
        // Console.WriteLine($"Bootstrap Servers: {runtime.BootstrapServers}");
        // ...

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
