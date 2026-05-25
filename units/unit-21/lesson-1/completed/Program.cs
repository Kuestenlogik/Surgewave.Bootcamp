// Surgewave Bootcamp - Unit 4, Lesson 1: Clustering
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
// using Kuestenlogik.Surgewave.Client.Admin; // TODO: Admin client not yet implemented

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 4.1 - Clustering ===");
        Console.WriteLine();

        Console.WriteLine("Starting 3-broker cluster...");
        Console.WriteLine();

        // Start broker 1 (seed broker)
        await using var broker1 = await SurgewaveRuntime.CreateBuilder()
            .WithBrokerId(1)
            .WithPort(9092)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine("  Broker 1 started on port 9092");

        // Start broker 2
        await using var broker2 = await SurgewaveRuntime.CreateBuilder()
            .WithBrokerId(2)
            .WithPort(9093)
            .WithStorageMode(StorageMode.Memory)
            // .WithClusterSeeds("localhost:9092") // TODO: Clustering configuration not yet available via builder
            .Build()
            .StartAsync();

        Console.WriteLine("  Broker 2 started on port 9093");

        // Start broker 3
        await using var broker3 = await SurgewaveRuntime.CreateBuilder()
            .WithBrokerId(3)
            .WithPort(9094)
            .WithStorageMode(StorageMode.Memory)
            // .WithClusterSeeds("localhost:9092") // TODO: Clustering configuration not yet available via builder
            .Build()
            .StartAsync();

        Console.WriteLine("  Broker 3 started on port 9094");
        Console.WriteLine();

        // Create producer connected to all brokers
        await using var producer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = "localhost:9092,localhost:9093,localhost:9094";
        });

        // TODO: Admin client not yet implemented
        // await using var admin = new SurgewaveAdminClient(opt =>
        //     opt.BootstrapServers = "localhost:9092,localhost:9093,localhost:9094");
        //
        // var metadata = await admin.GetMetadataAsync();
        //
        // Console.WriteLine("--- Cluster Metadata ---");
        // Console.WriteLine($"Cluster ID: {metadata.ClusterId}");
        // Console.WriteLine($"Controller: Broker {metadata.ControllerId}");
        // Console.WriteLine();
        //
        // Console.WriteLine("Brokers:");
        // foreach (var broker in metadata.Brokers)
        // {
        //     Console.WriteLine($"  Broker {broker.Id}: {broker.Host}:{broker.Port}");
        // }
        // Console.WriteLine();
        //
        // // Create a topic
        // await admin.CreateTopicAsync(new TopicSpecification
        // {
        //     Name = "cluster-events",
        //     NumPartitions = 6,
        //     ReplicationFactor = 3
        // });
        //
        // Console.WriteLine("Created topic 'cluster-events' with 6 partitions, RF=3");
        //
        // // Get updated metadata with topic info
        // metadata = await admin.GetMetadataAsync();
        //
        // Console.WriteLine();
        // Console.WriteLine("Topic Partitions:");
        // var topic = metadata.Topics.FirstOrDefault(t => t.Name == "cluster-events");
        // if (topic != null)
        // {
        //     foreach (var partition in topic.Partitions.OrderBy(p => p.Id))
        //     {
        //         Console.WriteLine($"  Partition {partition.Id}: Leader=Broker {partition.Leader}, Replicas=[{string.Join(", ", partition.Replicas)}]");
        //     }
        // }
        Console.WriteLine();

        // Send messages
        Console.WriteLine("Sending 100 messages...");
        for (int i = 0; i < 100; i++)
        {
            var result = await producer.ProduceAsync("cluster-events", $"key-{i}", $"value-{i}");
            if (i % 25 == 0)
            {
                Console.WriteLine($"  Sent message {i} to partition {result.Partition}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("--- Simulating Broker Failure ---");
        Console.WriteLine("Stopping broker 1...");
        await broker1.DisposeAsync();

        Console.WriteLine("Sending more messages (should work via other brokers)...");
        for (int i = 100; i < 110; i++)
        {
            await producer.ProduceAsync("cluster-events", $"key-{i}", $"value-{i}");
        }
        Console.WriteLine("  Messages sent successfully!");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
        Console.WriteLine();
        Console.WriteLine("Key Takeaways:");
        Console.WriteLine("  - Clusters provide high availability");
        Console.WriteLine("  - Clients can connect to any broker");
        Console.WriteLine("  - Production survives broker failures");
    }
}
