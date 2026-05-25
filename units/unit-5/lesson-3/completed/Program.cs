// Surgewave Bootcamp - Unit 7, Lesson 3: Aggregations & Windowing
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Streams;
using Kuestenlogik.Surgewave.Streams.Windows;
using Microsoft.Extensions.Logging;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 7.3 - Aggregations & Windowing ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // ============== Demo 1: Count (Non-windowed) ==============
        Console.WriteLine("--- Demo 1: Count Events by Type ---");

        var builder1 = new StreamsBuilder { ApplicationId = "count-demo" };

        builder1.Stream<string, int>("events")
            .GroupByKey()
            .Count()
            .ToStream()
            .Peek((key, count) => Console.WriteLine($"  {key}: {count} total"));

        var topology1 = builder1.Build();
        var config1 = new StreamsConfig { ApplicationId = "count-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app1 = new StreamsApplication(config1, topology1, loggerFactory);
        app1.Start();

        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        app1.ProcessRecord("events", "click", 1, ts);
        app1.ProcessRecord("events", "view", 1, ts);
        app1.ProcessRecord("events", "click", 1, ts);
        app1.ProcessRecord("events", "click", 1, ts);
        app1.ProcessRecord("events", "purchase", 1, ts);
        app1.ProcessRecord("events", "view", 1, ts);

        await app1.CloseAsync();
        Console.WriteLine();

        // ============== Demo 2: Reduce (Sum) ==============
        Console.WriteLine("--- Demo 2: Reduce (Sum Values) ---");

        var builder2 = new StreamsBuilder { ApplicationId = "reduce-demo" };

        builder2.Stream<string, int>("sales")
            .GroupByKey()
            .Reduce((a, b) => a + b)
            .ToStream()
            .Peek((product, total) => Console.WriteLine($"  {product} sales: ${total}"));

        var topology2 = builder2.Build();
        var config2 = new StreamsConfig { ApplicationId = "reduce-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app2 = new StreamsApplication(config2, topology2, loggerFactory);
        app2.Start();

        app2.ProcessRecord("sales", "laptop", 1200, ts);
        app2.ProcessRecord("sales", "phone", 800, ts);
        app2.ProcessRecord("sales", "laptop", 1200, ts);
        app2.ProcessRecord("sales", "tablet", 500, ts);
        app2.ProcessRecord("sales", "phone", 900, ts);

        await app2.CloseAsync();
        Console.WriteLine();

        // ============== Demo 3: Aggregate (Custom) ==============
        Console.WriteLine("--- Demo 3: Aggregate (Stats) ---");

        var builder3 = new StreamsBuilder { ApplicationId = "aggregate-demo" };

        builder3.Stream<string, int>("measurements")
            .GroupByKey()
            .Aggregate(
                () => new Stats(0, 0, int.MinValue, int.MaxValue),
                (key, value, stats) => new Stats(
                    stats.Count + 1,
                    stats.Sum + value,
                    Math.Max(stats.Max, value),
                    Math.Min(stats.Min, value)
                )
            )
            .ToStream()
            .Peek((sensor, stats) =>
                Console.WriteLine($"  {sensor}: count={stats.Count}, avg={stats.Sum / Math.Max(1, stats.Count)}, max={stats.Max}, min={stats.Min}"));

        var topology3 = builder3.Build();
        var config3 = new StreamsConfig { ApplicationId = "aggregate-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app3 = new StreamsApplication(config3, topology3, loggerFactory);
        app3.Start();

        app3.ProcessRecord("measurements", "temp-1", 72, ts);
        app3.ProcessRecord("measurements", "temp-1", 75, ts);
        app3.ProcessRecord("measurements", "temp-1", 68, ts);
        app3.ProcessRecord("measurements", "temp-2", 80, ts);
        app3.ProcessRecord("measurements", "temp-2", 82, ts);

        await app3.CloseAsync();
        Console.WriteLine();

        // ============== Demo 4: Tumbling Windows ==============
        Console.WriteLine("--- Demo 4: Tumbling Windows (1 min) ---");

        var builder4 = new StreamsBuilder { ApplicationId = "tumbling-demo" };

        builder4.Stream<string, int>("page-views")
            .GroupByKey()
            .WindowedBy(TumblingWindows.Of(TimeSpan.FromMinutes(1)))
            .Count()
            .ToStream()
            .Peek((windowed, count) =>
            {
                var windowStart = DateTimeOffset.FromUnixTimeMilliseconds(windowed.Window.StartMs);
                Console.WriteLine($"  {windowed.Key} @ {windowStart:HH:mm}: {count} views");
            });

        var topology4 = builder4.Build();
        var config4 = new StreamsConfig { ApplicationId = "tumbling-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app4 = new StreamsApplication(config4, topology4, loggerFactory);
        app4.Start();

        var minute0 = ts;
        var minute1 = ts + 60_000;
        var minute2 = ts + 120_000;

        app4.ProcessRecord("page-views", "home", 1, minute0);
        app4.ProcessRecord("page-views", "home", 1, minute0 + 10_000);
        app4.ProcessRecord("page-views", "products", 1, minute0);
        app4.ProcessRecord("page-views", "home", 1, minute1);  // Next window
        app4.ProcessRecord("page-views", "home", 1, minute1 + 5_000);
        app4.ProcessRecord("page-views", "checkout", 1, minute2);  // Third window

        await app4.CloseAsync();
        Console.WriteLine();

        // ============== Demo 5: Hopping Windows ==============
        Console.WriteLine("--- Demo 5: Hopping Windows (5 min, advance 1 min) ---");

        var builder5 = new StreamsBuilder { ApplicationId = "hopping-demo" };

        builder5.Stream<string, int>("metrics")
            .GroupByKey()
            .WindowedBy(
                HoppingWindows
                    .Of(TimeSpan.FromMinutes(5))
                    .AdvanceBy(TimeSpan.FromMinutes(1))
            )
            .Reduce((a, b) => a + b)
            .ToStream()
            .Peek((windowed, sum) =>
            {
                var start = DateTimeOffset.FromUnixTimeMilliseconds(windowed.Window.StartMs);
                var end = DateTimeOffset.FromUnixTimeMilliseconds(windowed.Window.EndMs);
                Console.WriteLine($"  {windowed.Key} [{start:mm}-{end:mm}]: sum={sum}");
            });

        var topology5 = builder5.Build();
        var config5 = new StreamsConfig { ApplicationId = "hopping-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app5 = new StreamsApplication(config5, topology5, loggerFactory);
        app5.Start();

        // Events at different minutes
        app5.ProcessRecord("metrics", "cpu", 50, minute0);
        app5.ProcessRecord("metrics", "cpu", 60, minute1);
        app5.ProcessRecord("metrics", "cpu", 55, minute2);

        await app5.CloseAsync();
        Console.WriteLine();

        // ============== Demo 6: Session Windows ==============
        Console.WriteLine("--- Demo 6: Session Windows (30 sec gap) ---");

        var builder6 = new StreamsBuilder { ApplicationId = "session-demo" };

        builder6.Stream<string, int>("clicks")
            .GroupByKey()
            .WindowedBy(SessionWindows.With(TimeSpan.FromSeconds(30)))
            .Count()
            .ToStream()
            .Peek((windowed, count) =>
            {
                var duration = windowed.Window.EndMs - windowed.Window.StartMs;
                Console.WriteLine($"  User {windowed.Key} session: {count} clicks, {duration}ms duration");
            });

        var topology6 = builder6.Build();
        var config6 = new StreamsConfig { ApplicationId = "session-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app6 = new StreamsApplication(config6, topology6, loggerFactory);
        app6.Start();

        // User alice: continuous activity (within 30 sec gaps)
        app6.ProcessRecord("clicks", "alice", 1, ts);
        app6.ProcessRecord("clicks", "alice", 1, ts + 10_000);  // 10 sec later
        app6.ProcessRecord("clicks", "alice", 1, ts + 20_000);  // 20 sec later

        // User alice: new session (>30 sec gap)
        app6.ProcessRecord("clicks", "alice", 1, ts + 60_000);  // 60 sec later - new session

        // User bob: single session
        app6.ProcessRecord("clicks", "bob", 1, ts + 5_000);
        app6.ProcessRecord("clicks", "bob", 1, ts + 15_000);

        await app6.CloseAsync();
        Console.WriteLine();

        // ============== Demo 7: Grace Period for Late Records ==============
        Console.WriteLine("--- Demo 7: Grace Period for Late Arrivals ---");

        var builder7 = new StreamsBuilder { ApplicationId = "grace-demo" };

        // Use grace period to accept late-arriving records
        builder7.Stream<string, int>("sensor-readings")
            .GroupByKey()
            .WindowedBy(
                TumblingWindows
                    .Of(TimeSpan.FromMinutes(1))
                    .Grace(TimeSpan.FromSeconds(30))  // Accept records up to 30 sec late
            )
            .Count()
            .ToStream()
            .Peek((windowed, count) =>
            {
                var windowStart = DateTimeOffset.FromUnixTimeMilliseconds(windowed.Window.StartMs);
                Console.WriteLine($"  {windowed.Key} @ {windowStart:HH:mm}: {count} readings");
            });

        var topology7 = builder7.Build();
        var config7 = new StreamsConfig { ApplicationId = "grace-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app7 = new StreamsApplication(config7, topology7, loggerFactory);
        app7.Start();

        var windowStart = ts;
        var windowEnd = ts + 60_000;  // 1 minute later

        // Records within the window
        app7.ProcessRecord("sensor-readings", "temp-sensor", 1, windowStart);
        app7.ProcessRecord("sensor-readings", "temp-sensor", 1, windowStart + 30_000);

        // Late record (arrives after window ends, but within grace period)
        // In real scenarios, this would have an event time within the window
        // but arrive at the processor after the window end time
        app7.ProcessRecord("sensor-readings", "temp-sensor", 1, windowStart + 45_000);

        Console.WriteLine($"  Metrics - Processed: {app7.Metrics.ProcessedRecords}, Late: {app7.Metrics.LateRecords}");

        await app7.CloseAsync();
        Console.WriteLine();

        // ============== Demo 8: Hopping Windows with Grace ==============
        Console.WriteLine("--- Demo 8: Hopping Windows with Grace Period ---");

        var builder8 = new StreamsBuilder { ApplicationId = "hopping-grace-demo" };

        builder8.Stream<string, int>("transactions")
            .GroupByKey()
            .WindowedBy(
                HoppingWindows
                    .Of(TimeSpan.FromMinutes(5))
                    .AdvanceBy(TimeSpan.FromMinutes(1))
                    .Grace(TimeSpan.FromMinutes(2))  // 2 minutes grace for late transactions
            )
            .Reduce((a, b) => a + b)
            .ToStream()
            .Peek((windowed, sum) =>
            {
                var start = DateTimeOffset.FromUnixTimeMilliseconds(windowed.Window.StartMs);
                Console.WriteLine($"  {windowed.Key} 5-min window starting {start:mm}: total=${sum}");
            });

        var topology8 = builder8.Build();
        var config8 = new StreamsConfig { ApplicationId = "hopping-grace-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app8 = new StreamsApplication(config8, topology8, loggerFactory);
        app8.Start();

        // Process transactions across multiple overlapping windows
        app8.ProcessRecord("transactions", "store-1", 100, minute0);
        app8.ProcessRecord("transactions", "store-1", 200, minute1);
        app8.ProcessRecord("transactions", "store-1", 150, minute2);

        await app8.CloseAsync();
        Console.WriteLine();

        // ============== Demo 9: Session Windows with Grace ==============
        Console.WriteLine("--- Demo 9: Session Windows with Grace Period ---");

        var builder9 = new StreamsBuilder { ApplicationId = "session-grace-demo" };

        builder9.Stream<string, int>("user-activity")
            .GroupByKey()
            .WindowedBy(
                SessionWindows
                    .With(TimeSpan.FromSeconds(30))
                    .Grace(TimeSpan.FromSeconds(10))  // 10 sec grace for delayed events
            )
            .Count()
            .ToStream()
            .Peek((windowed, count) =>
            {
                var duration = windowed.Window.EndMs - windowed.Window.StartMs;
                Console.WriteLine($"  User {windowed.Key} session: {count} events, {duration}ms duration");
            });

        var topology9 = builder9.Build();
        var config9 = new StreamsConfig { ApplicationId = "session-grace-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app9 = new StreamsApplication(config9, topology9, loggerFactory);
        app9.Start();

        // User session with some potentially delayed events
        app9.ProcessRecord("user-activity", "user-123", 1, ts);
        app9.ProcessRecord("user-activity", "user-123", 1, ts + 5_000);
        app9.ProcessRecord("user-activity", "user-123", 1, ts + 15_000);

        // Late event that arrives within grace period
        app9.ProcessRecord("user-activity", "user-123", 1, ts + 10_000);

        await app9.CloseAsync();
        Console.WriteLine();

        // Summary
        Console.WriteLine("--- Key Takeaways ---");
        Console.WriteLine("1. GroupByKey() - Group by existing key");
        Console.WriteLine("2. Count() - Count records per key");
        Console.WriteLine("3. Reduce() - Combine values of same type");
        Console.WriteLine("4. Aggregate() - Custom aggregation with initializer");
        Console.WriteLine("5. TumblingWindows - Fixed non-overlapping");
        Console.WriteLine("6. HoppingWindows - Fixed overlapping");
        Console.WriteLine("7. SessionWindows - Gap-based dynamic");
        Console.WriteLine("8. Grace Period - Accept late records within a time buffer");
        Console.WriteLine("9. StreamsMetrics.LateRecords - Monitor dropped late records");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}

public record Stats(int Count, int Sum, int Max, int Min);
