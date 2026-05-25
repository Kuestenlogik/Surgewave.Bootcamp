// Surgewave Bootcamp - Unit 5, Lesson 2: API Wrapper Migration
//
// In this lesson, you'll learn:
// - How to migrate using the Confluent.Kafka-compatible wrapper
// - The package replacement approach
// - Testing migrated code
//
// This demo simulates the migration pattern using Surgewave's native client.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 5.2 - API Wrapper Migration ===");
        Console.WriteLine();

        // Start Surgewave broker
        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine("Surgewave broker started");
        Console.WriteLine();

        // This demonstrates the migration pattern:
        // Your existing Confluent.Kafka code would look similar
        // After switching to Kuestenlogik.Surgewave.Kafka.Compat, it continues working

        Console.WriteLine("--- Migration Steps ---");
        Console.WriteLine();
        Console.WriteLine("1. In your .csproj, replace:");
        Console.WriteLine("   <PackageReference Include=\"Confluent.Kafka\" Version=\"2.x\" />");
        Console.WriteLine("   with:");
        Console.WriteLine("   <PackageReference Include=\"Kuestenlogik.Surgewave.Kafka.Compat\" Version=\"1.x\" />");
        Console.WriteLine();
        Console.WriteLine("2. Your code stays the same - 'using Confluent.Kafka;' still works");
        Console.WriteLine();
        Console.WriteLine("3. Update BootstrapServers to point to Surgewave");
        Console.WriteLine();

        // Demo with Surgewave native client (same patterns as Confluent.Kafka)
        await using var producer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = "localhost:9092");

        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = "localhost:9092";
            opt.GroupId = "migration-test";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        // TODO: Send test messages
        Console.WriteLine("Sending test messages...");
        for (int i = 0; i < 5; i++)
        {
            await producer.ProduceAsync("migration-topic", $"key-{i}", $"value-{i}");
        }

        // TODO: Consume messages
        Console.WriteLine("Consuming messages...");
        consumer.Subscribe("migration-topic");

        for (int i = 0; i < 5; i++)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
            if (result != null)
            {
                Console.WriteLine($"  Received: {result.Key} = {result.Value}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Migration pattern verified!");
        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
