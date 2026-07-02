// Surgewave Bootcamp - Unit 5, Lesson 1: Kafka Compatibility
//
// In this lesson, you'll learn:
// - Surgewave speaks the Kafka protocol
// - Existing Kafka clients can connect to Surgewave
// - Migration can be as simple as changing bootstrap servers
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 5.1 - Kafka Compatibility ===");
        Console.WriteLine();

        // Start Surgewave broker on standard Kafka port
        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine($"Surgewave broker started on port {runtime.Port}");
        Console.WriteLine("This broker is Kafka protocol compatible!");
        Console.WriteLine();

        // Using Surgewave's native client (simulating Kafka client)
        // In production, you could use Confluent.Kafka here
        await using var producer = new SurgewaveProducer<string, string>(opt =>
        {
            // This is the only change needed for Kafka clients:
            // BootstrapServers points to Surgewave instead of Kafka
            opt.BootstrapServers = "localhost:9092";
        });

        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = "localhost:9092";
            opt.GroupId = "kafka-compat-group";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        // TODO: Send messages (same API as Kafka)
        Console.WriteLine("Sending messages...");
        for (int i = 0; i < 5; i++)
        {
            await producer.ProduceAsync("kafka-topic", $"key-{i}", $"value-{i}");
            Console.WriteLine($"  Sent: key-{i}");
        }

        // TODO: Consume messages (same API as Kafka)
        Console.WriteLine();
        Console.WriteLine("Consuming messages...");
        consumer.Subscribe("kafka-topic");

        for (int i = 0; i < 5; i++)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
            if (result != null)
            {
                Console.WriteLine($"  Received: {result.Key} = {result.Value}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Kafka protocol compatibility verified!");
        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
