// Surgewave Bootcamp - Unit 7, Lesson 3: Aggregations & Windowing
//
// In this lesson, you'll learn to:
// - Group streams by key
// - Use Count, Reduce, and Aggregate
// - Apply tumbling, hopping, and session windows
// - Handle late-arriving records with grace periods
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
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 7.3 - Aggregations & Windowing ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

        var builder = new StreamsBuilder { ApplicationId = "metrics-app" };

        // Event stream: key=eventType, value=1 (count increment)
        var events = builder.Stream<string, int>("events");

        // TODO: Count events per type (non-windowed)
        // Hint: events.GroupByKey().Count().ToStream().Peek(...)

        // TODO: Apply 1-minute tumbling window and count
        // Hint: .GroupByKey().WindowedBy(TumblingWindows.Of(TimeSpan.FromMinutes(1))).Count()

        // TODO: Apply hopping window (5 min size, 1 min advance)
        // Hint: HoppingWindows.Of(TimeSpan.FromMinutes(5)).AdvanceBy(TimeSpan.FromMinutes(1))

        // TODO: Sum values using Reduce
        // Hint: .GroupByKey().Reduce((a, b) => a + b)

        // TODO: Add grace period to accept late records
        // Hint: TumblingWindows.Of(TimeSpan.FromMinutes(1)).Grace(TimeSpan.FromSeconds(30))

        // TODO: Check late record metrics
        // Hint: Console.WriteLine($"Late records: {app.Metrics.LateRecords}");

        // Build topology and create application
        var topology = builder.Build();
        var config = new StreamsConfig { ApplicationId = "metrics-app", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app = new StreamsApplication(config, topology, loggerFactory);
        app.Start();

        // Test with events
        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // TODO: Process test events
        // app.ProcessRecord("events", "click", 1, baseTime);
        // app.ProcessRecord("events", "view", 1, baseTime);
        // app.ProcessRecord("events", "click", 1, baseTime + 1000);

        await app.CloseAsync();

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
