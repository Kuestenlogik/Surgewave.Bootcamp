// Surgewave Bootcamp - Unit 7, Lesson 2: Transformations
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Streams;
using Microsoft.Extensions.Logging;

public record LogEntry(string Level, string Message, DateTime Timestamp);

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 7.2 - Transformations ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // ============== Demo 1: Map and MapValues ==============
        Console.WriteLine("--- Demo 1: Map and MapValues ---");

        var builder1 = new StreamsBuilder { ApplicationId = "map-demo" };

        builder1.Stream<string, string>("users")
            .MapValues(name => name.ToUpperInvariant())
            .Peek((key, value) => Console.WriteLine($"  MapValues: {key} -> {value}"))
            .To("users-upper");

        var topology1 = builder1.Build();
        var config1 = new StreamsConfig { ApplicationId = "map-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app1 = new StreamsApplication(config1, topology1, loggerFactory);
        app1.Start();

        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        app1.ProcessRecord("users", "u1", "alice", ts);
        app1.ProcessRecord("users", "u2", "bob", ts);
        await app1.CloseAsync();

        Console.WriteLine();

        // ============== Demo 2: FlatMapValues ==============
        Console.WriteLine("--- Demo 2: FlatMapValues (Word Split) ---");

        var builder2 = new StreamsBuilder { ApplicationId = "flatmap-demo" };

        builder2.Stream<string, string>("sentences")
            .FlatMapValues(sentence => sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Peek((key, word) => Console.WriteLine($"  Word: {word}"))
            .To("words");

        var topology2 = builder2.Build();
        var config2 = new StreamsConfig { ApplicationId = "flatmap-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app2 = new StreamsApplication(config2, topology2, loggerFactory);
        app2.Start();
        app2.ProcessRecord("sentences", "s1", "hello world from surgewave", ts);
        app2.ProcessRecord("sentences", "s2", "stream processing rocks", ts);
        await app2.CloseAsync();

        Console.WriteLine();

        // ============== Demo 3: Branch and Merge ==============
        Console.WriteLine("--- Demo 3: Branch by Log Level ---");

        var builder3 = new StreamsBuilder { ApplicationId = "log-processor" };

        var rawLogs = builder3.Stream<string, string>("raw-logs");

        // Parse log lines
        var parsed = rawLogs
            .MapValues(line => ParseLogLine(line))
            .Filter((key, value) => value != null);

        // Branch by severity
        var branches = parsed.Branch(
            (key, log) => log!.Level == "ERROR",
            (key, log) => log!.Level == "WARN",
            (key, log) => log!.Level == "INFO"
        );

        var errorStream = branches[0];
        var warnStream = branches[1];
        var infoStream = branches[2];

        // Route to separate topics
        errorStream
            .Peek((key, log) => Console.WriteLine($"  [ERROR] {log!.Message}"))
            .MapValues(log => log!.Message)
            .To("error-logs");

        warnStream
            .Peek((key, log) => Console.WriteLine($"  [WARN]  {log!.Message}"))
            .MapValues(log => log!.Message)
            .To("warn-logs");

        infoStream
            .Peek((key, log) => Console.WriteLine($"  [INFO]  {log!.Message}"))
            .MapValues(log => log!.Message)
            .To("info-logs");

        // Merge all branches for unified logging
        var allLogs = errorStream.Merge(warnStream).Merge(infoStream);
        allLogs
            .MapValues(log => $"[{log!.Level}] {log.Message}")
            .To("all-logs");

        var topology3 = builder3.Build();
        var config3 = new StreamsConfig { ApplicationId = "log-processor", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app3 = new StreamsApplication(config3, topology3, loggerFactory);
        app3.Start();

        // Process test logs
        app3.ProcessRecord("raw-logs", "1", "ERROR|Database connection failed", ts);
        app3.ProcessRecord("raw-logs", "2", "WARN|High memory usage detected", ts);
        app3.ProcessRecord("raw-logs", "3", "INFO|User alice logged in", ts);
        app3.ProcessRecord("raw-logs", "4", "ERROR|Disk space critical", ts);
        app3.ProcessRecord("raw-logs", "5", "INFO|Backup completed", ts);
        app3.ProcessRecord("raw-logs", "6", "INVALID LINE", ts); // Will be filtered

        await app3.CloseAsync();

        Console.WriteLine();

        // ============== Demo 4: SelectKey ==============
        Console.WriteLine("--- Demo 4: SelectKey (Rekey by User) ---");

        var builder4 = new StreamsBuilder { ApplicationId = "rekey-demo" };

        builder4.Stream<string, string>("events")
            .SelectKey((key, value) =>
            {
                // Extract user from "user:action" format
                var parts = value.Split(':');
                return parts.Length > 0 ? parts[0] : key;
            })
            .Peek((key, value) => Console.WriteLine($"  Rekeyed: {key} -> {value}"))
            .To("events-by-user");

        var topology4 = builder4.Build();
        var config4 = new StreamsConfig { ApplicationId = "rekey-demo", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app4 = new StreamsApplication(config4, topology4, loggerFactory);
        app4.Start();
        app4.ProcessRecord("events", "e1", "alice:login", ts);
        app4.ProcessRecord("events", "e2", "bob:purchase", ts);
        app4.ProcessRecord("events", "e3", "alice:logout", ts);
        await app4.CloseAsync();

        Console.WriteLine();

        // Summary
        Console.WriteLine("--- Key Takeaways ---");
        Console.WriteLine("1. MapValues - Transform value, keep key (no repartition)");
        Console.WriteLine("2. Map - Transform key and value (may repartition)");
        Console.WriteLine("3. FlatMapValues - Expand one record to many");
        Console.WriteLine("4. Branch - Split stream by predicates");
        Console.WriteLine("5. Merge - Combine multiple streams");
        Console.WriteLine("6. SelectKey - Change message key");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }

    private static LogEntry? ParseLogLine(string line)
    {
        var parts = line.Split('|', 2);
        if (parts.Length != 2) return null;
        return new LogEntry(parts[0], parts[1], DateTime.UtcNow);
    }
}
