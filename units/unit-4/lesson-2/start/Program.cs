// Surgewave Bootcamp - Unit 6, Lesson 2: Partitioning Strategies
//
// In this lesson, you'll learn to:
// - Choose partition keys for ordering and scalability
// - Avoid hot partitions
// - Analyze partition distribution
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Native;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 6.2 - Partitioning Strategies ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        // TODO: Create a Surgewave client for topic management
        // Hint: await using var client = new SurgewaveNativeClient("localhost", runtime.Port);
        // Hint: await client.ConnectAsync();

        await using var producer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = $"localhost:{runtime.Port}");

        // TODO: Create topic with 8 partitions using client.Topics.Create(...)

        // TODO: Experiment with different key strategies

        // Strategy 1: Entity ID (good distribution)
        Console.WriteLine("--- Strategy 1: Entity ID ---");
        // for (int i = 0; i < 1000; i++)
        //     await producer.ProduceAsync("partitioning-demo", $"user-{i}", "data");

        // Strategy 2: Country code (potentially hot partition)
        Console.WriteLine("--- Strategy 2: Country Code ---");
        // Simulate 80% US, 10% UK, 10% other
        // This will create a hot partition!

        // TODO: Analyze partition distribution
        // await AnalyzeDistribution(client, "partitioning-demo");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }

    private static async Task AnalyzeDistribution(SurgewaveNativeClient client, string topic)
    {
        // TODO: Use client.Topics.DescribeAsync(topic) to get partition metadata
        // The PartitionDescription has HighWatermark and LogStartOffset
        throw new NotImplementedException();
    }
}
