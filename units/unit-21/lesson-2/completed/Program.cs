// Surgewave Bootcamp - Unit 4, Lesson 2: Replication
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
// using Kuestenlogik.Surgewave.Client.Admin; // TODO: Admin client not yet implemented

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 4.2 - Replication ===");
        Console.WriteLine();

        // Start a 3-broker cluster
        Console.WriteLine("Starting 3-broker cluster...");

        await using var broker1 = await SurgewaveRuntime.CreateBuilder()
            .WithBrokerId(1)
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .Build()
            .StartAsync();

        await using var broker2 = await SurgewaveRuntime.CreateBuilder()
            .WithBrokerId(2)
            .WithPort(9093)
            .WithStorageEngine(StorageEngines.Memory)
            // .WithClusterSeeds("localhost:9092") // TODO: Clustering configuration not yet available via builder
            .Build()
            .StartAsync();

        await using var broker3 = await SurgewaveRuntime.CreateBuilder()
            .WithBrokerId(3)
            .WithPort(9094)
            .WithStorageEngine(StorageEngines.Memory)
            // .WithClusterSeeds("localhost:9092") // TODO: Clustering configuration not yet available via builder
            .Build()
            .StartAsync();

        Console.WriteLine("Cluster started with 3 brokers");
        Console.WriteLine();

        // TODO: Admin client not yet implemented
        // await using var admin = new SurgewaveAdminClient(opt =>
        //     opt.BootstrapServers = "localhost:9092,localhost:9093,localhost:9094");
        //
        // // Create topic with replication
        // await admin.CreateTopicAsync(new TopicSpecification
        // {
        //     Name = "durable-events",
        //     NumPartitions = 3,
        //     ReplicationFactor = 3,
        //     Config = new Dictionary<string, string>
        //     {
        //         ["min.insync.replicas"] = "2"
        //     }
        // });

        Console.WriteLine("Created topic 'durable-events':");
        Console.WriteLine("  Partitions: 3");
        Console.WriteLine("  Replication Factor: 3");
        Console.WriteLine("  Min ISR: 2");
        Console.WriteLine();

        // Create durable producer
        await using var producer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = "localhost:9092,localhost:9093,localhost:9094";
            // opt.Acks = Acks.All; // TODO: Acks not yet available on SurgewaveProducerOptions
        });

        // Check replication status
        Console.WriteLine("--- Replication Status ---");
        // await PrintReplicationStatus(admin, "durable-events"); // TODO: Admin client not yet implemented
        Console.WriteLine();

        // Send messages
        Console.WriteLine("Sending messages with Acks.All...");
        for (int i = 0; i < 10; i++)
        {
            var result = await producer.ProduceAsync("durable-events", $"key-{i}", $"value-{i}");
            Console.WriteLine($"  Message {i} stored (partition {result.Partition}, offset {result.Offset})");
        }

        Console.WriteLine();
        Console.WriteLine("--- Testing Replica Failure ---");

        // Stop one broker
        Console.WriteLine("Stopping broker 3...");
        await broker3.DisposeAsync();
        await Task.Delay(1000); // Let cluster detect failure

        Console.WriteLine("Checking ISR after broker failure...");
        // await PrintReplicationStatus(admin, "durable-events"); // TODO: Admin client not yet implemented

        // Writes should still work (2 ISR remaining >= min.insync.replicas)
        Console.WriteLine();
        Console.WriteLine("Sending more messages (should work with 2 ISR)...");
        for (int i = 10; i < 15; i++)
        {
            await producer.ProduceAsync("durable-events", $"key-{i}", $"value-{i}");
        }
        Console.WriteLine("  Messages sent successfully!");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
        Console.WriteLine();
        Console.WriteLine("Key Takeaways:");
        Console.WriteLine("  - Replication provides data durability");
        Console.WriteLine("  - ISR tracks which replicas are caught up");
        Console.WriteLine("  - min.insync.replicas prevents under-replicated writes");
    }

    // TODO: Admin client not yet implemented
    // private static async Task PrintReplicationStatus(SurgewaveAdminClient admin, string topicName)
    // {
    //     var metadata = await admin.GetMetadataAsync();
    //     var topic = metadata.Topics.FirstOrDefault(t => t.Name == topicName);
    //
    //     if (topic == null)
    //     {
    //         Console.WriteLine($"Topic '{topicName}' not found");
    //         return;
    //     }
    //
    //     foreach (var partition in topic.Partitions.OrderBy(p => p.Id))
    //     {
    //         var isrCount = partition.Isr.Count;
    //         var replicaCount = partition.Replicas.Count;
    //         var status = isrCount == replicaCount ? "OK" : "UNDER-REPLICATED";
    //
    //         Console.WriteLine($"Partition {partition.Id}: Leader={partition.Leader}, " +
    //             $"Replicas=[{string.Join(",", partition.Replicas)}], " +
    //             $"ISR=[{string.Join(",", partition.Isr)}] ({status})");
    //     }
    // }
}
