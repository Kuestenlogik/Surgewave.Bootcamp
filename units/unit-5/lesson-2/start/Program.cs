// Surgewave Bootcamp - Unit 7, Lesson 2: Transformations
//
// In this lesson, you'll learn to:
// - Use Map and MapValues for transformations
// - Use FlatMapValues to expand records
// - Use Branch to split streams
// - Use Merge to combine streams
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Streams;
using Microsoft.Extensions.Logging;

// Log entry record
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

        var builder = new StreamsBuilder { ApplicationId = "log-processor" };

        // Read raw log lines
        var rawLogs = builder.Stream<string, string>("raw-logs");

        // TODO: Parse log lines into LogEntry records
        // Format: "LEVEL|message"
        // Hint: .MapValues(line => ParseLogLine(line))

        // TODO: Filter out null (invalid) entries
        // Hint: .Filter((key, value) => value != null)

        // TODO: Branch by log level (ERROR, WARN, INFO)
        // Hint: var branches = stream.Branch(
        //     (key, log) => log!.Level == "ERROR",
        //     (key, log) => log!.Level == "WARN",
        //     (key, log) => log!.Level == "INFO"
        // );

        // TODO: Route ERROR logs to "error-logs" topic
        // TODO: Route WARN logs to "warn-logs" topic
        // TODO: Route INFO logs to "info-logs" topic

        // TODO: Merge all branches and write to "all-logs"

        // Build topology and create application
        var topology = builder.Build();
        var config = new StreamsConfig { ApplicationId = "log-processor", BootstrapServers = $"localhost:{runtime.Port}" };
        await using var app = new StreamsApplication(config, topology, loggerFactory);
        app.Start();

        // Test with sample logs
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // TODO: Process test records
        // app.ProcessRecord("raw-logs", "1", "ERROR|Database connection failed", timestamp);
        // app.ProcessRecord("raw-logs", "2", "WARN|High memory usage", timestamp);
        // app.ProcessRecord("raw-logs", "3", "INFO|User logged in", timestamp);

        await app.CloseAsync();

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
