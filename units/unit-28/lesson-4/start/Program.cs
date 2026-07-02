// Surgewave Bootcamp - Unit 3, Lesson 4: Transactions
//
// In this lesson, you'll learn to:
// - Create transactional producers
// - Implement atomic multi-topic operations
// - Handle transaction failures
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

public record Order(string Id, string CustomerId, string ProductId, int Quantity);

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 3.4 - Transactions ===");
        Console.WriteLine();

        // Start embedded broker
        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine($"Broker started on port {runtime.Port}");

        // TODO: Create transactional producer
        // Hint: Set opt.TransactionalId = "order-processor"
        await using var producer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            // TODO: Set TransactionalId
        });

        // TODO: Initialize transactions
        // await producer.InitTransactionsAsync();

        // Process orders transactionally
        var orders = new[]
        {
            new Order("ORD-001", "CUST-1", "PROD-A", 2),
            new Order("ORD-002", "CUST-2", "PROD-B", 1),
            new Order("ORD-003", "CUST-1", "PROD-C", 5)
        };

        foreach (var order in orders)
        {
            // TODO: Implement transactional processing
            // 1. BeginTransactionAsync
            // 2. Produce to "orders" topic
            // 3. Produce to "inventory" topic
            // 4. Produce to "notifications" topic
            // 5. CommitTransactionAsync (or AbortTransactionAsync on error)

            Console.WriteLine($"Processing order {order.Id}...");
        }

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
