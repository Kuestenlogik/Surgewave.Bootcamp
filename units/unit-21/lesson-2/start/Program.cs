// Surgewave Bootcamp - Unit 4, Lesson 2: Replication
//
// In this lesson, you'll learn to:
// - Configure topic replication
// - Monitor ISR status
// - Handle replica failures
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
// using Kuestenlogik.Surgewave.Client.Native.Operations.Admin; // TODO: Admin client not yet implemented

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
            .WithClusterSeeds("localhost:9092")
            .Build()
            .StartAsync();

        await using var broker3 = await SurgewaveRuntime.CreateBuilder()
            .WithBrokerId(3)
            .WithPort(9094)
            .WithStorageEngine(StorageEngines.Memory)
            .WithClusterSeeds("localhost:9092")
            .Build()
            .StartAsync();

        Console.WriteLine("Cluster started!");

        // TODO: Admin client not yet implemented
        // await using var admin = new SurgewaveAdminClient(opt =>
        //     opt.BootstrapServers = "localhost:9092,localhost:9093,localhost:9094");

        // TODO: Create topic with replication factor 3 and min.insync.replicas=2
        // await admin.CreateTopicAsync(new TopicSpecification { ... });

        // TODO: Create producer with Acks.All for durability
        await using var producer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = "localhost:9092,localhost:9093,localhost:9094";
            // TODO: Set Acks = Acks.All
        });

        // TODO: Check replication status using admin.GetMetadataAsync()

        // TODO: Send messages and verify replication

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
