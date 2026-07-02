// Surgewave Bootcamp - Unit 5, Lesson 2: API Wrapper Migration
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
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
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine("Surgewave broker started on port 9092");
        Console.WriteLine();

        // Display migration instructions
        PrintMigrationGuide();

        // Demonstrate that code patterns work
        Console.WriteLine("--- Demonstrating Compatible Patterns ---");
        Console.WriteLine();

        await using var producer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = "localhost:9092";
            // opt.Acks = Acks.All; // TODO: Acks not yet available on SurgewaveProducerOptions
        });

        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = "localhost:9092";
            opt.GroupId = "api-wrapper-demo";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
            opt.EnableAutoCommit = false;
        });

        // Producer pattern (same as Confluent.Kafka)
        Console.WriteLine("Producer pattern:");
        var deliveryResult = await producer.ProduceAsync(
            "orders",
            "order-123",
            "{\"product\": \"Widget\", \"quantity\": 5}");

        Console.WriteLine($"  Delivered to: {deliveryResult.Topic}/{deliveryResult.Partition}@{deliveryResult.Offset}");
        Console.WriteLine();

        // Batch producer pattern
        Console.WriteLine("Batch producer pattern:");
        for (int i = 0; i < 10; i++)
        {
            await producer.ProduceAsync("orders", $"order-{i}", $"Order data {i}");
        }
        await producer.FlushAsync();
        Console.WriteLine("  Flushed 10 messages");
        Console.WriteLine();

        // Consumer pattern (same as Confluent.Kafka)
        Console.WriteLine("Consumer pattern:");
        consumer.Subscribe("orders");

        int count = 0;
        while (count < 11)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
            if (result == null) break;

            Console.WriteLine($"  Consumed: {result.Key}");
            count++;
        }

        await consumer.CommitAsync();
        Console.WriteLine($"  Committed {count} messages");
        Console.WriteLine();

        // Print migration checklist
        PrintMigrationChecklist();

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }

    private static void PrintMigrationGuide()
    {
        Console.WriteLine("=== API Wrapper Migration Guide ===");
        Console.WriteLine();
        Console.WriteLine("STEP 1: Update Package Reference");
        Console.WriteLine("─────────────────────────────────");
        Console.WriteLine("In your .csproj file, change:");
        Console.WriteLine();
        Console.WriteLine("  <!-- Before -->");
        Console.WriteLine("  <PackageReference Include=\"Confluent.Kafka\" Version=\"2.3.0\" />");
        Console.WriteLine();
        Console.WriteLine("  <!-- After -->");
        Console.WriteLine("  <PackageReference Include=\"Kuestenlogik.Surgewave.Kafka.Compat\" Version=\"1.0.0\" />");
        Console.WriteLine();
        Console.WriteLine("STEP 2: Keep Your Code");
        Console.WriteLine("──────────────────────");
        Console.WriteLine("Your existing code continues to work:");
        Console.WriteLine();
        Console.WriteLine("  using Confluent.Kafka;  // ← Still works!");
        Console.WriteLine();
        Console.WriteLine("  var config = new ProducerConfig");
        Console.WriteLine("  {");
        Console.WriteLine("      BootstrapServers = \"surgewave-broker:9092\"  // ← Update this");
        Console.WriteLine("  };");
        Console.WriteLine();
        Console.WriteLine("STEP 3: Test and Deploy");
        Console.WriteLine("───────────────────────");
        Console.WriteLine("Run your tests, then deploy to production.");
        Console.WriteLine();
    }

    private static void PrintMigrationChecklist()
    {
        Console.WriteLine("=== Migration Checklist ===");
        Console.WriteLine();
        Console.WriteLine("[ ] Update NuGet package reference");
        Console.WriteLine("[ ] Build project (should succeed)");
        Console.WriteLine("[ ] Run unit tests");
        Console.WriteLine("[ ] Run integration tests");
        Console.WriteLine("[ ] Update bootstrap servers config");
        Console.WriteLine("[ ] Test in staging environment");
        Console.WriteLine("[ ] Monitor for errors");
        Console.WriteLine("[ ] Deploy to production");
    }
}
