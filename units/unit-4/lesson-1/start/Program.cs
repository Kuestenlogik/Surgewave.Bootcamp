// Surgewave Bootcamp - Unit 6, Lesson 1: Topic Design
//
// In this lesson, you'll learn to:
// - Apply topic naming conventions
// - Choose appropriate topic granularity
// - Design topics based on domain boundaries
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client.Native;

// TODO: Define topic names following naming conventions
public static class TopicNames
{
    // Pattern: <domain>.<aggregate>.<version>

    // Orders domain
    public const string OrderEvents = ""; // TODO: orders.order.v1

    // Inventory domain
    public const string InventoryEvents = ""; // TODO

    // Payments domain
    public const string PaymentEvents = ""; // TODO

    // Users domain
    public const string UserEvents = ""; // TODO
}

// TODO: Define topic configuration
public record TopicConfiguration(
    string Name,
    int Partitions = 6,
    short ReplicationFactor = 1,
    TimeSpan? Retention = null,
    bool Compacted = false);

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 6.1 - Topic Design ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageMode(StorageMode.Memory)
            .Build()
            .StartAsync();

        // TODO: Create a Surgewave client for topic management
        // Hint: await using var client = new SurgewaveNativeClient("localhost", runtime.Port);
        // Hint: await client.ConnectAsync();

        // TODO: Define topic configurations for your domain
        var topics = new List<TopicConfiguration>
        {
            // Example: new TopicConfiguration(TopicNames.OrderEvents, Partitions: 12)
        };

        // TODO: Create topics
        foreach (var topic in topics)
        {
            // Create topic with configuration
        }

        // TODO: Validate topic design
        Console.WriteLine("Validating topic design...");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
