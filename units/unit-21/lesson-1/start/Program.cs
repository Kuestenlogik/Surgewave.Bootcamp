// Surgewave Bootcamp - Unit 4, Lesson 1: Clustering
//
// In this lesson, you'll learn to:
// - Set up a multi-broker cluster
// - Connect clients to the cluster
// - Observe partition distribution
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
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 4.1 - Clustering ===");
        Console.WriteLine();

        // TODO: Start 3 brokers to form a cluster
        // Broker 1: Port 9092
        // Broker 2: Port 9093, seed: localhost:9092
        // Broker 3: Port 9094, seed: localhost:9092

        Console.WriteLine("Starting broker cluster...");

        // For simplicity in this exercise, we use in-memory mode
        await using var broker1 = await SurgewaveRuntime.CreateBuilder()
            .WithBrokerId(1)
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        // TODO: Start broker 2
        // await using var broker2 = ...

        // TODO: Start broker 3
        // await using var broker3 = ...

        Console.WriteLine("Cluster started!");
        Console.WriteLine();

        // TODO: Create producer connected to all brokers
        // opt.BootstrapServers = "localhost:9092,localhost:9093,localhost:9094"
        await using var producer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = "localhost:9092";
        });

        // TODO: Get and display cluster metadata
        // var admin = new SurgewaveAdminClient(opt => ...);
        // var metadata = await admin.GetMetadataAsync();

        // TODO: Send messages to verify cluster is working
        Console.WriteLine("Sending messages...");
        for (int i = 0; i < 10; i++)
        {
            await producer.ProduceAsync("cluster-test", $"key-{i}", $"value-{i}");
        }

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
