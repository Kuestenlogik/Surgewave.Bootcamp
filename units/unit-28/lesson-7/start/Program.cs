// Surgewave Bootcamp - Unit 3, Lesson 7: Ephemeral Topics
//
// In this lesson, you'll learn to:
// - Create ephemeral topics with ring-buffer storage
// - Observe real-time message delivery to active consumers
// - Demonstrate message loss without active consumers
// - Trigger ring-buffer wrap-around and observe LogStartOffset advancing
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Client.Native;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 3.7 - Ephemeral Topics ===");
        Console.WriteLine();

        // Start embedded broker
        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(false)
            .Build()
            .StartAsync();

        Console.WriteLine($"Broker started on port {runtime.Port}");

        // Create native client for topic management
        await using var nativeClient = new SurgewaveNativeClient("localhost", runtime.Port);
        await nativeClient.ConnectAsync();

        // TODO: Create ephemeral topic with 1 MB ring buffer
        // await nativeClient.Topics.Create("sensor-telemetry")
        //     .WithPartitions(1)
        //     .WithConfig("cleanup.policy", "ephemeral")
        //     .WithConfig("ephemeral.buffer.bytes", "1048576")
        //     .ExecuteAsync();

        // TODO: Create persistent topic for comparison
        // await nativeClient.Topics.Create("sensor-telemetry-persistent")
        //     .WithPartitions(1)
        //     .ExecuteAsync();

        Console.WriteLine();

        // TODO: Start a consumer for the ephemeral topic in a background task
        // Hint: Use Task.Run with a SurgewaveConsumer, AutoOffsetReset.Latest
        // Store received messages in a List<string>
        Console.WriteLine("--- Real-Time Consumption ---");

        // Create producer
        await using var producer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
        });

        // TODO: Produce 5 sensor readings while consumer is active
        // Hint: Use a loop with short delays between messages
        Console.WriteLine("Producing sensor readings...");

        Console.WriteLine();

        // TODO: Stop the consumer, then produce 5 more messages
        // Verify that a new consumer with Latest offset doesn't receive them
        Console.WriteLine("--- Messages Without Active Consumer ---");

        Console.WriteLine();

        // TODO: Produce ~2.5 MB of data (50 messages * 50 KB) to trigger wrap-around
        // Then check LogStartOffset via nativeClient.Topics.DescribeAsync()
        Console.WriteLine("--- Buffer Wrap-Around ---");

        Console.WriteLine();

        // TODO: Produce 10 messages to both topics, then read persistent topic
        // from Earliest to show full history is preserved
        Console.WriteLine("--- Persistent vs Ephemeral ---");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
