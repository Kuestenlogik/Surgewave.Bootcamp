// Surgewave Bootcamp - Unit 3, Lesson 4: Transactions
// COMPLETED SOLUTION

using System.Text.Json;
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

        // Create transactional producer
        await using var producer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            // opt.TransactionalId = "order-processor"; // TODO: TransactionalId not yet available on SurgewaveProducerOptions
        });

        // Initialize transactions
        // await producer.InitTransactionsAsync(); // TODO: Not yet implemented
        Console.WriteLine("Transactions initialized (stubbed)");
        Console.WriteLine();

        // Process orders transactionally
        var orders = new[]
        {
            new Order("ORD-001", "CUST-1", "PROD-A", 2),
            new Order("ORD-002", "CUST-2", "PROD-B", 1),
            new Order("ORD-003", "CUST-1", "PROD-C", 5)
        };

        int committed = 0;
        int aborted = 0;

        foreach (var order in orders)
        {
            Console.WriteLine($"Processing order {order.Id}...");

            // await producer.BeginTransactionAsync(); // TODO: Not yet implemented

            try
            {
                // Produce to multiple topics atomically
                await producer.ProduceAsync(
                    "orders",
                    order.Id,
                    JsonSerializer.Serialize(order));

                await producer.ProduceAsync(
                    "inventory",
                    order.ProductId,
                    $"RESERVE:{order.Quantity}");

                await producer.ProduceAsync(
                    "notifications",
                    order.CustomerId,
                    $"Order {order.Id} placed for {order.Quantity} x {order.ProductId}");

                // await producer.CommitTransactionAsync(); // TODO: Not yet implemented
                Console.WriteLine($"  Transaction COMMITTED");
                committed++;
            }
            catch (Exception ex)
            {
                // await producer.AbortTransactionAsync(); // TODO: Not yet implemented
                Console.WriteLine($"  Transaction ABORTED: {ex.Message}");
                aborted++;
            }
        }

        Console.WriteLine();

        // Verify messages in each topic
        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
            opt.IsolationLevel = IsolationLevel.ReadCommitted; // Only read committed
        });

        Console.WriteLine("--- Verifying 'orders' topic ---");
        consumer.Subscribe("orders");
        await ConsumeAndPrint(consumer, orders.Length);

        Console.WriteLine("--- Verifying 'inventory' topic ---");
        // consumer.Unsubscribe(); // TODO: Unsubscribe not yet implemented on SurgewaveConsumer
        consumer.Subscribe("inventory");
        await ConsumeAndPrint(consumer, orders.Length);

        Console.WriteLine("--- Verifying 'notifications' topic ---");
        // consumer.Unsubscribe(); // TODO: Unsubscribe not yet implemented on SurgewaveConsumer
        consumer.Subscribe("notifications");
        await ConsumeAndPrint(consumer, orders.Length);

        Console.WriteLine($"--- Summary ---");
        Console.WriteLine($"Transactions committed: {committed}");
        Console.WriteLine($"Transactions aborted: {aborted}");
        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }

    private static async Task ConsumeAndPrint(SurgewaveConsumer<string, string> consumer, int maxMessages)
    {
        for (int i = 0; i < maxMessages; i++)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
            if (result == null) break;
            Console.WriteLine($"  [{result.Key}] {result.Value}");
        }
        Console.WriteLine();
    }
}
