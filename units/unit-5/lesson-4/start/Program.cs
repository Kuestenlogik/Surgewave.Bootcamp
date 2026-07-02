// Surgewave Bootcamp - Unit 7, Lesson 4: Joins & Suppression
//
// In this lesson, you'll learn to:
// - Join streams with windows (stream-stream)
// - Enrich streams with tables (stream-table)
// - Join tables (table-table)
// - Use suppression to emit only final aggregation results
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Streams;
using Kuestenlogik.Surgewave.Streams.Windows;
using Microsoft.Extensions.Logging;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 7.4 - Joins & Suppression ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

        var builder = new StreamsBuilder { ApplicationId = "join-demo" };

        // Streams: key=orderId
        var orders = builder.Stream<string, string>("orders");
        var payments = builder.Stream<string, string>("payments");

        // Table: key=customerId
        var customers = builder.Table<string, string>("customers");

        // TODO: Stream-Table join - enrich orders with customer name
        // Hint: orders.Join(customers, (order, customer) => $"{order} [Customer: {customer}]")
        //       .Peek((key, value) => Console.WriteLine($"  Enriched: {value}"))

        // TODO: Stream-Stream join - correlate orders with payments
        // Hint: orders.Join(payments, (order, payment) => $"Matched: {order} + {payment}",
        //                   JoinWindows.Of(TimeSpan.FromMinutes(5)))

        // Build topology and create application
        var topology = builder.Build();
        var config = new StreamsConfig { ApplicationId = "join-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app = new StreamsApplication(config, topology, loggerFactory);
        app.Start();

        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // TODO: Populate customer table
        // app.ProcessRecord("customers", "c1", "Alice", ts);

        // TODO: Process orders
        // app.ProcessRecord("orders", "o1", "Order for laptop", ts);

        // TODO: Process payments
        // app.ProcessRecord("payments", "o1", "Payment $1200", ts);

        // TODO: Add suppression to emit only final window results
        // Hint: .Suppress(Suppressed<Windowed<string>>.UntilWindowClose(TimeSpan.FromSeconds(30)))

        // TODO: Add rate-limited suppression
        // Hint: .Suppress(Suppressed<string>.UntilTimeLimit(TimeSpan.FromSeconds(5), maxBytes: 1024 * 1024))

        await app.CloseAsync();

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
