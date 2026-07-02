// Surgewave Bootcamp - Unit 6, Lesson 3: Event Schema Design
//
// In this lesson, you'll learn to:
// - Design backward and forward compatible schemas
// - Implement schema versioning
// - Handle schema evolution
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

// TODO: Define V1 schema
public record OrderCreatedV1
{
    public required string OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required decimal Amount { get; init; }
}

// TODO: Define V2 schema (backward compatible)
public record OrderCreatedV2
{
    public required string OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required decimal Amount { get; init; }
    // TODO: Add optional fields
}

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 6.3 - Event Schema Design ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        await using var producer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = "localhost:9092");

        // TODO: Produce V1 event
        // TODO: Produce V2 event
        // TODO: Consume and handle both versions

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
