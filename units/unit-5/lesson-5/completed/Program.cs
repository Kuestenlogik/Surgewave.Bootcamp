// Surgewave Bootcamp - Unit 7, Lesson 5: State Stores
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
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 7.5 - State Stores ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // ============== Demo 1: KeyValue Store (Direct Usage) ==============
        Console.WriteLine("--- Demo 1: InMemoryKeyValueStore ---");

        // Directly demonstrate state store usage
        using var countStore = new InMemoryKeyValueStore<string, long>("event-counts");

        // Simulate counting events (Get returns default(long)=0 if key not found)
        countStore.Put("click", countStore.Get("click") + 1);
        countStore.Put("click", countStore.Get("click") + 1);
        countStore.Put("click", countStore.Get("click") + 1);
        countStore.Put("view", countStore.Get("view") + 1);
        countStore.Put("view", countStore.Get("view") + 1);
        countStore.Put("purchase", countStore.Get("purchase") + 1);

        Console.WriteLine("  Querying KeyValueStore:");
        Console.WriteLine($"    click: {countStore.Get("click")}");
        Console.WriteLine($"    view: {countStore.Get("view")}");
        Console.WriteLine($"    purchase: {countStore.Get("purchase")}");

        // Iterate all entries
        Console.WriteLine();
        Console.WriteLine("  All counts via All():");
        foreach (var kv in countStore.All())
        {
            Console.WriteLine($"    {kv.Key}: {kv.Value}");
        }

        Console.WriteLine($"  ApproximateNumEntries: {countStore.ApproximateNumEntries}");
        Console.WriteLine();

        // ============== Demo 2: Window Store ==============
        Console.WriteLine("--- Demo 2: InMemoryWindowStore ---");

        using var windowStore = new InMemoryWindowStore<string, long>(
            "minute-counts",
            windowSize: TimeSpan.FromMinutes(1),
            retentionPeriod: TimeSpan.FromHours(1));

        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var minute0 = ts - (ts % 60_000);  // Align to minute boundary
        var minute1 = minute0 + 60_000;

        // Store windowed counts
        windowStore.Put("cpu", 2, minute0);
        windowStore.Put("cpu", 1, minute1);
        windowStore.Put("memory", 1, minute0);
        windowStore.Put("disk", 1, minute1);

        Console.WriteLine("  Querying WindowStore for 'cpu':");
        var cpuCount0 = windowStore.Fetch("cpu", minute0);
        var cpuCount1 = windowStore.Fetch("cpu", minute1);
        Console.WriteLine($"    Window {DateTimeOffset.FromUnixTimeMilliseconds(minute0):HH:mm}: {cpuCount0} events");
        Console.WriteLine($"    Window {DateTimeOffset.FromUnixTimeMilliseconds(minute1):HH:mm}: {cpuCount1} events");

        // Fetch range
        Console.WriteLine();
        Console.WriteLine("  Fetching 'cpu' range:");
        foreach (var kv in windowStore.Fetch("cpu", minute0, minute1 + 60_000))
        {
            var windowTime = DateTimeOffset.FromUnixTimeMilliseconds(kv.Key.Window.StartMs);
            Console.WriteLine($"    Window {windowTime:HH:mm}: {kv.Value} events");
        }
        Console.WriteLine();

        // ============== Demo 3: Session Store ==============
        Console.WriteLine("--- Demo 3: InMemorySessionStore ---");

        using var sessionStore = new InMemorySessionStore<string, int>(
            "user-sessions",
            retentionPeriod: TimeSpan.FromMinutes(5));

        // Store sessions with start/end times
        var sessionStart = ts;
        var sessionEnd = ts + 20_000;  // 20 second session

        sessionStore.Put(new Windowed<string>("alice", new Window(sessionStart, sessionEnd)), 5);
        sessionStore.Put(new Windowed<string>("bob", new Window(sessionStart, sessionEnd + 10_000)), 3);

        Console.WriteLine("  Finding sessions for 'alice':");
        var aliceSessions = sessionStore.FindSessions("alice", sessionStart - 1000, sessionEnd + 1000);
        foreach (var session in aliceSessions)
        {
            var duration = session.Key.Window.EndMs - session.Key.Window.StartMs;
            Console.WriteLine($"    Session: {session.Value} clicks, {duration}ms duration");
        }
        Console.WriteLine();

        // ============== Demo 4: State Stores with Streams ==============
        Console.WriteLine("--- Demo 4: State Stores via Streams Aggregation ---");

        var builder = new StreamsBuilder { ApplicationId = "state-demo" };

        // Count creates a Table backed by a state store
        var countTable = builder.Stream<string, int>("events")
            .GroupByKey()
            .Count();

        // The table has a queryable store name
        Console.WriteLine($"  Count table store name: {countTable.QueryableStoreName}");

        // Aggregate also creates a Table with state
        var sumTable = builder.Stream<string, int>("sales")
            .GroupByKey()
            .Reduce((a, b) => a + b);

        Console.WriteLine($"  Sum table store name: {sumTable.QueryableStoreName}");

        // Note: GetStateStore<T>() can retrieve these stores at runtime
        var topology = builder.Build();
        var config = new StreamsConfig { ApplicationId = "state-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app = new StreamsApplication(config, topology, loggerFactory);
        app.Start();

        // Process some records
        app.ProcessRecord("events", "click", 1, ts);
        app.ProcessRecord("events", "click", 1, ts);
        app.ProcessRecord("events", "view", 1, ts);

        await app.CloseAsync();
        Console.WriteLine();

        // ============== Demo 5: Interactive Queries Pattern ==============
        Console.WriteLine("--- Demo 5: Interactive Queries Pattern ---");
        Console.WriteLine(@"
  // In a real application, expose state via REST API:

  app.MapGet(""/api/counts/{key}"", (string key) =>
  {
      var store = streamsApp.GetStateStore<IReadOnlyKeyValueStore<string, long>>(
          countTable.QueryableStoreName);
      var count = store?.Get(key);
      return count.HasValue ? Results.Ok(count.Value) : Results.NotFound();
  });

  // Time-range queries with window stores:
  app.MapGet(""/api/metrics/{key}"", (string key, long from, long to) =>
  {
      var store = streamsApp.GetStateStore<IWindowStore<string, long>>(""windowed-metrics"");
      return store?.Fetch(key, from, to)
          .Select(kv => new { WindowStart = kv.Key.Window.StartMs, Count = kv.Value });
  });
");

        // Summary
        Console.WriteLine("--- Key Takeaways ---");
        Console.WriteLine("1. InMemoryKeyValueStore for point lookups and scans");
        Console.WriteLine("2. InMemoryWindowStore for time-windowed data");
        Console.WriteLine("3. InMemorySessionStore for session-based aggregations");
        Console.WriteLine("4. Table.QueryableStoreName identifies the backing store");
        Console.WriteLine("5. GetStateStore<T>() retrieves stores for interactive queries");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
