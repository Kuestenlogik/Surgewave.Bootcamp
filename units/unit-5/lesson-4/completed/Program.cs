// Surgewave Bootcamp - Unit 7, Lesson 4: Joins & Suppression
// COMPLETED SOLUTION

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

        // ============== Demo 1: Stream-Table Join ==============
        Console.WriteLine("--- Demo 1: Stream-Table Join (Order Enrichment) ---");

        var builder1 = new StreamsBuilder { ApplicationId = "stream-table-join" };

        // Orders stream: key=customerId, value=order details
        var orders = builder1.Stream<string, string>("orders");

        // Customers table: key=customerId, value=customer name
        var customers = builder1.Table<string, string>("customers");

        // Join orders with customer data
        orders.Join(
            customers,
            (order, customerName) => $"{order} [Customer: {customerName}]"
        )
        .Peek((customerId, enriched) =>
            Console.WriteLine($"  Enriched: {enriched}"))
        .To("enriched-orders");

        var topology1 = builder1.Build();
        var config1 = new StreamsConfig { ApplicationId = "stream-table-join", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app1 = new StreamsApplication(config1, topology1, loggerFactory);
        app1.Start();

        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Populate customer table first
        app1.ProcessRecord("customers", "c1", "Alice Smith", ts);
        app1.ProcessRecord("customers", "c2", "Bob Jones", ts);

        // Process orders (will be enriched with customer names)
        app1.ProcessRecord("orders", "c1", "Order: Laptop $1200", ts);
        app1.ProcessRecord("orders", "c2", "Order: Phone $800", ts);
        app1.ProcessRecord("orders", "c1", "Order: Mouse $50", ts);
        app1.ProcessRecord("orders", "c3", "Order: Keyboard $100", ts);  // No customer - won't join

        await app1.CloseAsync();
        Console.WriteLine();

        // ============== Demo 2: Stream-Stream Join ==============
        Console.WriteLine("--- Demo 2: Stream-Stream Join (Order-Payment Correlation) ---");

        var builder2 = new StreamsBuilder { ApplicationId = "stream-stream-join" };

        // Both keyed by orderId
        var orderStream = builder2.Stream<string, string>("order-events");
        var paymentStream = builder2.Stream<string, string>("payment-events");

        // Join orders with payments within 5-minute window
        orderStream.Join(
            paymentStream,
            (order, payment) => $"MATCHED: {order} | {payment}",
            JoinWindows.Of(TimeSpan.FromMinutes(5))
        )
        .Peek((orderId, matched) =>
            Console.WriteLine($"  {orderId}: {matched}"))
        .To("matched-transactions");

        var topology2 = builder2.Build();
        var config2 = new StreamsConfig { ApplicationId = "stream-stream-join", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app2 = new StreamsApplication(config2, topology2, loggerFactory);
        app2.Start();

        // Order placed
        app2.ProcessRecord("order-events", "ord-1", "Order placed: $500", ts);
        app2.ProcessRecord("order-events", "ord-2", "Order placed: $300", ts);

        // Payments arrive (within window)
        app2.ProcessRecord("payment-events", "ord-1", "Payment received: $500", ts + 1000);
        app2.ProcessRecord("payment-events", "ord-2", "Payment received: $300", ts + 2000);

        // Late payment (ord-3 has no matching order)
        app2.ProcessRecord("payment-events", "ord-3", "Payment received: $100", ts);

        await app2.CloseAsync();
        Console.WriteLine();

        // ============== Demo 3: Left Join ==============
        Console.WriteLine("--- Demo 3: Left Join (Include Unmatched) ---");

        var builder3 = new StreamsBuilder { ApplicationId = "left-join-demo" };

        var events = builder3.Stream<string, string>("events");
        var metadata = builder3.Table<string, string>("metadata");

        // Left join - include events even without metadata
        events.LeftJoin(
            metadata,
            (evt, meta) => $"{evt} [meta: {meta ?? "UNKNOWN"}]"
        )
        .Peek((key, result) =>
            Console.WriteLine($"  {key}: {result}"))
        .To("events-with-metadata");

        var topology3 = builder3.Build();
        var config3 = new StreamsConfig { ApplicationId = "left-join-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app3 = new StreamsApplication(config3, topology3, loggerFactory);
        app3.Start();

        // Some metadata
        app3.ProcessRecord("metadata", "sensor-1", "Temperature Sensor", ts);

        // Events (some with metadata, some without)
        app3.ProcessRecord("events", "sensor-1", "Reading: 72F", ts);
        app3.ProcessRecord("events", "sensor-2", "Reading: 68F", ts);  // No metadata

        await app3.CloseAsync();
        Console.WriteLine();

        // ============== Demo 4: Table-Table Join ==============
        Console.WriteLine("--- Demo 4: Table-Table Join ---");

        var builder4 = new StreamsBuilder { ApplicationId = "table-table-join" };

        var users = builder4.Table<string, string>("users");
        var profiles = builder4.Table<string, string>("profiles");

        // Join two tables
        users.Join(
            profiles,
            (user, profile) => $"{user} | {profile}"
        )
        .ToStream()
        .Peek((userId, combined) =>
            Console.WriteLine($"  {userId}: {combined}"))
        .To("user-profiles");

        var topology4 = builder4.Build();
        var config4 = new StreamsConfig { ApplicationId = "table-table-join", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app4 = new StreamsApplication(config4, topology4, loggerFactory);
        app4.Start();

        // Populate both tables
        app4.ProcessRecord("users", "u1", "alice@example.com", ts);
        app4.ProcessRecord("profiles", "u1", "Software Engineer", ts);

        app4.ProcessRecord("users", "u2", "bob@example.com", ts);
        app4.ProcessRecord("profiles", "u2", "Data Scientist", ts);

        // Update triggers re-join
        app4.ProcessRecord("profiles", "u1", "Senior Engineer", ts + 1000);

        await app4.CloseAsync();
        Console.WriteLine();

        // ============== Demo 5: Suppression - Emit Only Final Results ==============
        Console.WriteLine("--- Demo 5: Suppression (UntilWindowClose) ---");

        var builder5 = new StreamsBuilder { ApplicationId = "suppression-demo" };

        // Without suppression - every update is emitted
        Console.WriteLine("  Without suppression (every update emitted):");
        builder5.Stream<string, int>("sales-unsuppressed")
            .GroupByKey()
            .WindowedBy(TumblingWindows.Of(TimeSpan.FromMinutes(1)))
            .Reduce((a, b) => a + b)
            .ToStream()
            .Peek((windowed, sum) =>
                Console.WriteLine($"    {windowed.Key}: ${sum}"));

        var topology5 = builder5.Build();
        var config5 = new StreamsConfig { ApplicationId = "suppression-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app5 = new StreamsApplication(config5, topology5, loggerFactory);
        app5.Start();

        // Multiple sales - each triggers an output
        app5.ProcessRecord("sales-unsuppressed", "electronics", 100, ts);
        app5.ProcessRecord("sales-unsuppressed", "electronics", 200, ts + 1000);
        app5.ProcessRecord("sales-unsuppressed", "electronics", 150, ts + 2000);

        await app5.CloseAsync();
        Console.WriteLine();

        // ============== Demo 6: Suppressed Aggregation ==============
        Console.WriteLine("--- Demo 6: Suppressed Aggregation (Final Only) ---");

        var builder6 = new StreamsBuilder { ApplicationId = "suppressed-agg-demo" };

        // With suppression - emit only when window closes
        builder6.Stream<string, int>("sales-suppressed")
            .GroupByKey()
            .WindowedBy(
                TumblingWindows
                    .Of(TimeSpan.FromMinutes(1))
                    .Grace(TimeSpan.FromSeconds(10))
            )
            .Reduce((a, b) => a + b)
            .Suppress(Suppressed<Windowed<string>>.UntilWindowClose(
                TimeSpan.FromSeconds(10)  // Match grace period
            ))
            .ToStream()
            .Peek((windowed, sum) =>
            {
                var windowStart = DateTimeOffset.FromUnixTimeMilliseconds(windowed.Window.StartMs);
                Console.WriteLine($"  FINAL {windowed.Key} @ {windowStart:HH:mm}: ${sum}");
            });

        var topology6 = builder6.Build();
        var config6 = new StreamsConfig { ApplicationId = "suppressed-agg-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app6 = new StreamsApplication(config6, topology6, loggerFactory);
        app6.Start();

        // Multiple sales - only final result is emitted after window closes
        app6.ProcessRecord("sales-suppressed", "clothing", 50, ts);
        app6.ProcessRecord("sales-suppressed", "clothing", 75, ts + 1000);
        app6.ProcessRecord("sales-suppressed", "clothing", 100, ts + 2000);

        // Note: In practice, the suppressed output appears when the window closes + grace expires
        Console.WriteLine("  (Suppressed output will appear when window closes)");

        await app6.CloseAsync();
        Console.WriteLine();

        // ============== Demo 7: Rate-Limited Suppression ==============
        Console.WriteLine("--- Demo 7: Time-Limited Suppression (Rate Limiting) ---");

        var builder7 = new StreamsBuilder { ApplicationId = "rate-limit-demo" };

        // Suppress updates, emit at most every 2 seconds
        builder7.Stream<string, int>("click-counts")
            .GroupByKey()
            .Count()
            .Suppress(Suppressed<string>.UntilTimeLimit(
                TimeSpan.FromSeconds(2),
                maxBytes: 1024 * 1024  // 1 MB buffer
            ))
            .ToStream()
            .Peek((key, count) =>
                Console.WriteLine($"  {key}: {count} clicks (rate-limited update)"));

        var topology7 = builder7.Build();
        var config7 = new StreamsConfig { ApplicationId = "rate-limit-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app7 = new StreamsApplication(config7, topology7, loggerFactory);
        app7.Start();

        // Rapid clicks - outputs are throttled
        for (int i = 0; i < 10; i++)
        {
            app7.ProcessRecord("click-counts", "button-1", 1, ts + i * 100);
        }

        Console.WriteLine("  (Rate-limited output appears periodically)");

        await app7.CloseAsync();
        Console.WriteLine();

        // ============== Demo 8: Combined Grace + Suppression ==============
        Console.WriteLine("--- Demo 8: Grace Period + Suppression (Best Practice) ---");

        var builder8 = new StreamsBuilder { ApplicationId = "grace-suppression-demo" };

        builder8.Stream<string, int>("hourly-revenue")
            .GroupByKey()
            .WindowedBy(
                TumblingWindows
                    .Of(TimeSpan.FromMinutes(1))  // 1-minute windows for demo
                    .Grace(TimeSpan.FromSeconds(30))  // 30 sec grace for late arrivals
            )
            .Aggregate(
                () => new RevenueStats(0, 0, 0),
                (key, value, stats) => new RevenueStats(
                    stats.Count + 1,
                    stats.Total + value,
                    Math.Max(stats.Max, value)
                )
            )
            .Suppress(Suppressed<Windowed<string>>.UntilWindowClose(
                TimeSpan.FromSeconds(30)  // Wait for grace period
            ))
            .ToStream()
            .Peek((windowed, stats) =>
            {
                var windowStart = DateTimeOffset.FromUnixTimeMilliseconds(windowed.Window.StartMs);
                Console.WriteLine($"  {windowed.Key} @ {windowStart:HH:mm}:");
                Console.WriteLine($"    Transactions: {stats.Count}");
                Console.WriteLine($"    Total: ${stats.Total}");
                Console.WriteLine($"    Max: ${stats.Max}");
            });

        var topology8 = builder8.Build();
        var config8 = new StreamsConfig { ApplicationId = "grace-suppression-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app8 = new StreamsApplication(config8, topology8, loggerFactory);
        app8.Start();

        // Process revenue data
        app8.ProcessRecord("hourly-revenue", "east", 500, ts);
        app8.ProcessRecord("hourly-revenue", "east", 750, ts + 5_000);
        app8.ProcessRecord("hourly-revenue", "west", 1200, ts);
        app8.ProcessRecord("hourly-revenue", "east", 300, ts + 10_000);

        Console.WriteLine("  (Final stats appear after window closes + grace expires)");

        await app8.CloseAsync();
        Console.WriteLine();

        // Summary
        Console.WriteLine("--- Key Takeaways ---");
        Console.WriteLine("1. Stream-Table: Enrich events with lookup data");
        Console.WriteLine("2. Stream-Stream: Correlate events within time window");
        Console.WriteLine("3. Table-Table: Join current state of two tables");
        Console.WriteLine("4. LeftJoin: Include records without matches (null)");
        Console.WriteLine("5. Co-partitioning required for joins to work");
        Console.WriteLine("6. UntilWindowClose: Emit only final aggregation result");
        Console.WriteLine("7. UntilTimeLimit: Rate-limit update frequency");
        Console.WriteLine("8. Combine Grace + Suppression for accurate final results");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}

public record RevenueStats(int Count, int Total, int Max);
