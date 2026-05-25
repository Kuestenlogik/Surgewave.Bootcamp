// Surgewave Bootcamp - Unit 3, Lesson 5: Exactly-Once Semantics
//
// In this lesson, you'll learn to:
// - Enable idempotent production
// - Implement exactly-once consume-transform-produce
// - Verify no duplicates
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 3.5 - Exactly-Once Semantics ===");
        Console.WriteLine();

        // Start embedded broker
        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine($"Broker started on port {runtime.Port}");

        // TODO: Create idempotent transactional producer
        await using var producer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            // TODO: Enable idempotence
            // TODO: Set TransactionalId
        });

        // TODO: Create consumer with ReadCommitted isolation
        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            opt.GroupId = "exactly-once-processor";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
            // TODO: Disable auto-commit
            // TODO: Set IsolationLevel to ReadCommitted
        });

        // Seed input topic with messages
        Console.WriteLine("Seeding input topic...");
        for (int i = 0; i < 100; i++)
        {
            await producer.ProduceAsync("input", $"key-{i}", $"value-{i}");
        }

        // TODO: Initialize transactions
        // await producer.InitTransactionsAsync();

        consumer.Subscribe("input");

        // TODO: Implement exactly-once processing loop
        // 1. Consume batch
        // 2. BeginTransactionAsync
        // 3. Transform and produce each message
        // 4. SendOffsetsToTransactionAsync
        // 5. CommitTransactionAsync

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }

    private static string Transform(string input)
    {
        return input.ToUpperInvariant();
    }
}
