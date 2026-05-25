// Surgewave Bootcamp - Unit 6, Lesson 3: Event Schema Design
// COMPLETED SOLUTION

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

// V1 Schema
public record OrderCreatedV1
{
    public required string OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required decimal Amount { get; init; }
}

// V2 Schema (backward compatible - added optional fields)
public record OrderCreatedV2
{
    public required string OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required decimal Amount { get; init; }

    // New optional fields with defaults
    public string Currency { get; init; } = "USD";
    public string? PromoCode { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public List<string> Tags { get; init; } = new();
}

// Tolerant JSON options for schema evolution
public static class SchemaOptions
{
    public static readonly JsonSerializerOptions Tolerant = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };
}

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 6.3 - Event Schema Design ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        await using var producer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = "localhost:9092");

        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = "localhost:9092";
            opt.GroupId = "schema-demo";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        const string topic = "orders.created";

        // Produce V1 event (simulating old producer)
        Console.WriteLine("--- Producing V1 Event ---");
        var v1Event = new OrderCreatedV1
        {
            OrderId = "ORD-001",
            CustomerId = "CUST-123",
            Amount = 99.99m
        };

        var v1Json = JsonSerializer.Serialize(v1Event, SchemaOptions.Tolerant);
        var v1Headers = new Dictionary<string, byte[]> { { "schema-version", Encoding.UTF8.GetBytes("1") } };
        await producer.ProduceAsync(topic, v1Event.OrderId, v1Json, v1Headers);
        Console.WriteLine($"  Sent V1: {v1Json}");

        // Produce V2 event (new producer with additional fields)
        Console.WriteLine();
        Console.WriteLine("--- Producing V2 Event ---");
        var v2Event = new OrderCreatedV2
        {
            OrderId = "ORD-002",
            CustomerId = "CUST-456",
            Amount = 149.99m,
            Currency = "EUR",
            PromoCode = "SAVE10",
            Tags = new List<string> { "premium", "express" }
        };

        var v2Json = JsonSerializer.Serialize(v2Event, SchemaOptions.Tolerant);
        var v2Headers = new Dictionary<string, byte[]> { { "schema-version", Encoding.UTF8.GetBytes("2") } };
        await producer.ProduceAsync(topic, v2Event.OrderId, v2Json, v2Headers);
        Console.WriteLine($"  Sent V2: {v2Json}");

        // Consume and handle both versions
        Console.WriteLine();
        Console.WriteLine("--- Consuming Events (Handling Both Versions) ---");
        consumer.Subscribe(topic);

        for (int i = 0; i < 2; i++)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
            if (result == null) continue;

            // Get schema version from header
            var versionHeader = result.Headers?.GetValueOrDefault("schema-version");
            var version = versionHeader != null
                ? int.Parse(Encoding.UTF8.GetString(versionHeader))
                : 1; // Default to v1 for old messages

            Console.WriteLine($"  Received (schema v{version}): {result.Key}");

            // Parse as V2 (our canonical internal format)
            var order = version switch
            {
                1 => UpgradeV1ToV2(JsonSerializer.Deserialize<OrderCreatedV1>(result.Value, SchemaOptions.Tolerant)!),
                2 => JsonSerializer.Deserialize<OrderCreatedV2>(result.Value, SchemaOptions.Tolerant)!,
                _ => throw new InvalidOperationException($"Unknown schema version: {version}")
            };

            Console.WriteLine($"    OrderId: {order.OrderId}");
            Console.WriteLine($"    Customer: {order.CustomerId}");
            Console.WriteLine($"    Amount: {order.Amount} {order.Currency}");
            Console.WriteLine($"    Promo: {order.PromoCode ?? "(none)"}");
            Console.WriteLine($"    Tags: [{string.Join(", ", order.Tags)}]");
            Console.WriteLine();
        }

        // Schema evolution summary
        Console.WriteLine("--- Schema Evolution Rules ---");
        Console.WriteLine(@"
SAFE Changes (Backward + Forward Compatible):
  ✅ Add optional field with default value
  ✅ Remove optional field (tolerant reader ignores)
  ✅ Add new enum value (at end)

UNSAFE Changes:
  ❌ Add required field (breaks old messages)
  ❌ Remove required field (breaks old consumers)
  ❌ Change field type (string → int)
  ❌ Rename field without alias
");

        Console.WriteLine("--- Compatibility Matrix ---");
        Console.WriteLine(@"
| Change Type              | Backward | Forward | Full |
|--------------------------|----------|---------|------|
| Add optional field       | ✅       | ❌      | ❌   |
| Remove optional field    | ❌       | ✅      | ❌   |
| Add optional w/ default  | ✅       | ✅      | ✅   |
| Add required field       | ❌       | ❌      | ❌   |
");

        Console.WriteLine("Lesson complete!");
    }

    private static OrderCreatedV2 UpgradeV1ToV2(OrderCreatedV1 v1) => new()
    {
        OrderId = v1.OrderId,
        CustomerId = v1.CustomerId,
        Amount = v1.Amount,
        Currency = "USD",  // Default for old events
        PromoCode = null,
        CreatedAt = DateTime.UtcNow,  // Unknown, use current time
        Tags = new List<string>()
    };
}
