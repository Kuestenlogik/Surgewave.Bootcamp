// Surgewave Bootcamp - Unit 7, Lesson 5: State Stores
//
// In this lesson, you'll learn to:
// - Use InMemoryKeyValueStore for key-value state
// - Use InMemoryWindowStore for time-windowed state
// - Use InMemorySessionStore for session-based state
// - Access state stores from streams applications
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
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 7.5 - State Stores ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // TODO: Create an InMemoryKeyValueStore<string, long> for counting events
        // Hint: using var countStore = new InMemoryKeyValueStore<string, long>("event-counts");

        // TODO: Put values and query them
        // Hint: countStore.Put("click", 3);
        //       var count = countStore.Get("click");
        //       foreach (var kv in countStore.All()) { ... }

        // TODO: Create an InMemoryWindowStore for time-windowed data
        // Hint: using var windowStore = new InMemoryWindowStore<string, long>(
        //           "minute-counts",
        //           windowSize: TimeSpan.FromMinutes(1),
        //           retentionPeriod: TimeSpan.FromHours(1));

        // TODO: Put windowed values and fetch them
        // Hint: var windowStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        //       windowStore.Put("cpu", 5, windowStart);
        //       var value = windowStore.Fetch("cpu", windowStart);

        // TODO: Create an InMemorySessionStore for session-based state
        // Hint: using var sessionStore = new InMemorySessionStore<string, int>(
        //           "user-sessions", retentionPeriod: TimeSpan.FromMinutes(5));

        // TODO: Create a streams app with aggregations
        // Hint: var builder = new StreamsBuilder { ApplicationId = "state-demo" };
        //       var countTable = builder.Stream<string, int>("events").GroupByKey().Count();
        //       Console.WriteLine($"Store name: {countTable.QueryableStoreName}");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
