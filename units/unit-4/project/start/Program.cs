// Unit 6 Project: E-Commerce Event Store Design
// Design domain events with proper schema evolution and projections

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== E-Commerce Event Store ===");
Console.WriteLine();

#region Event Definitions

// Base event envelope for all domain events
public record EventEnvelope(
    string EventId,
    string EventType,
    string AggregateId,
    int SchemaVersion,
    DateTime Timestamp,
    string Payload
);

// ============ Order Events V1 ============
public record OrderCreatedV1(
    string OrderId,
    string CustomerId,
    DateTime CreatedAt
);

public record OrderItemAddedV1(
    string OrderId,
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

public record OrderSubmittedV1(
    string OrderId,
    DateTime SubmittedAt
);

// ============ Order Events V2 (with additional fields) ============
public record OrderCreatedV2(
    string OrderId,
    string CustomerId,
    DateTime CreatedAt,
    string LoyaltyTier = "Standard",  // New in V2
    string? CouponCode = null          // New in V2
);

public record OrderItemAddedV2(
    string OrderId,
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountPercent = 0       // New in V2
);

// ============ Order Projection (Read Model) ============
public class OrderProjection
{
    public string OrderId { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public string LoyaltyTier { get; set; } = "Standard";
    public List<OrderLineItem> Items { get; set; } = new();
    public decimal TotalAmount => Items.Sum(i => i.LineTotal);
    public string Status { get; set; } = "Created";
    public int Version { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
}

public record OrderLineItem(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountPercent
)
{
    public decimal LineTotal => Quantity * UnitPrice * (1 - DiscountPercent / 100);
}

#endregion

// JSON serialization options
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true
};

// Topic configuration
const string OrdersTopic = "ecommerce.orders";

// TODO: Step 1 - Start embedded Surgewave broker
Console.WriteLine("[Setup] Creating topic structure...");

// await using var runtime = await SurgewaveRuntime.CreateBuilder()
//     .WithPort(9092)
//     .WithStorageEngine(StorageEngines.Memory)
//     .WithAutoCreateTopics(true)
//     .Build()
//     .StartAsync();

// TODO: Create producer and consumer
// await using var producer = new SurgewaveProducer<string, string>(opt =>
//     opt.BootstrapServers = "localhost:9092");

Console.WriteLine($"  Created: {OrdersTopic} (partitions: 6)");
Console.WriteLine($"  Created: ecommerce.customers (partitions: 3)");
Console.WriteLine($"  Created: ecommerce.products (partitions: 3)");
Console.WriteLine($"  Created: ecommerce.inventory (partitions: 6)");
Console.WriteLine();

// TODO: Step 2 - Publish domain events
Console.WriteLine("[Events] Publishing domain events...");
Console.WriteLine();

// Helper to create and publish events
async Task PublishEvent<T>(string topic, string aggregateId, T eventData, int schemaVersion) where T : class
{
    var envelope = new EventEnvelope(
        EventId: Guid.NewGuid().ToString(),
        EventType: typeof(T).Name,
        AggregateId: aggregateId,
        SchemaVersion: schemaVersion,
        Timestamp: DateTime.UtcNow,
        Payload: JsonSerializer.Serialize(eventData, jsonOptions)
    );

    var json = JsonSerializer.Serialize(envelope, jsonOptions);
    var headers = new Dictionary<string, byte[]>
    {
        { "event-type", Encoding.UTF8.GetBytes(envelope.EventType) },
        { "schema-version", Encoding.UTF8.GetBytes(schemaVersion.ToString()) }
    };

    // TODO: Produce the event
    // await producer.ProduceAsync(topic, aggregateId, json, headers);

    Console.WriteLine($"    -> {envelope.EventType} (v{schemaVersion})");
}

// Order 1: Using V1 events
Console.WriteLine("  Order ORD-001:");

// TODO: Publish V1 events for ORD-001
// await PublishEvent(OrdersTopic, "ORD-001", new OrderCreatedV1("ORD-001", "CUST-123", DateTime.UtcNow), 1);
// await PublishEvent(OrdersTopic, "ORD-001", new OrderItemAddedV1("ORD-001", "PROD-001", "Widget", 2, 50.00m), 1);
// await PublishEvent(OrdersTopic, "ORD-001", new OrderItemAddedV1("ORD-001", "PROD-002", "Gadget", 1, 75.00m), 1);
// await PublishEvent(OrdersTopic, "ORD-001", new OrderSubmittedV1("ORD-001", DateTime.UtcNow), 1);

Console.WriteLine();

// Order 2: Using V2 events (with loyalty tier and discounts)
Console.WriteLine("  Order ORD-002:");

// TODO: Publish V2 events for ORD-002
// await PublishEvent(OrdersTopic, "ORD-002", new OrderCreatedV2("ORD-002", "CUST-456", DateTime.UtcNow, "Gold", "SAVE10"), 2);
// await PublishEvent(OrdersTopic, "ORD-002", new OrderItemAddedV2("ORD-002", "PROD-003", "Premium Item", 1, 95.00m, 10), 2);
// await PublishEvent(OrdersTopic, "ORD-002", new OrderSubmittedV1("ORD-002", DateTime.UtcNow), 1);

Console.WriteLine();

// TODO: Step 3 - Build projections from events
Console.WriteLine("[Projection] Building Order Summary...");
Console.WriteLine();

var projections = new Dictionary<string, OrderProjection>();

// TODO: Implement event upcasters
OrderCreatedV2 UpcastOrderCreatedV1(OrderCreatedV1 v1) => new(
    v1.OrderId,
    v1.CustomerId,
    v1.CreatedAt,
    "Standard",  // Default for V1 events
    null
);

OrderItemAddedV2 UpcastOrderItemAddedV1(OrderItemAddedV1 v1) => new(
    v1.OrderId,
    v1.ProductId,
    v1.ProductName,
    v1.Quantity,
    v1.UnitPrice,
    0  // No discount in V1
);

// TODO: Consume events and build projections
// await using var consumer = new SurgewaveConsumer<string, string>(opt =>
// {
//     opt.BootstrapServers = "localhost:9092";
//     opt.GroupId = "projection-builder";
//     opt.AutoOffsetReset = AutoOffsetReset.Earliest;
// });
// consumer.Subscribe(OrdersTopic);

// TODO: Process events
// while (true)
// {
//     var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
//     if (result == null) break;
//
//     var envelope = JsonSerializer.Deserialize<EventEnvelope>(result.Value, jsonOptions);
//     if (envelope == null) continue;
//
//     // Get or create projection
//     if (!projections.TryGetValue(envelope.AggregateId, out var projection))
//     {
//         projection = new OrderProjection { OrderId = envelope.AggregateId };
//         projections[envelope.AggregateId] = projection;
//     }
//
//     // Apply event based on type
//     switch (envelope.EventType)
//     {
//         case nameof(OrderCreatedV1):
//         case nameof(OrderCreatedV2):
//             // TODO: Handle OrderCreated (upcast V1 to V2 if needed)
//             break;
//
//         case nameof(OrderItemAddedV1):
//         case nameof(OrderItemAddedV2):
//             // TODO: Handle OrderItemAdded (upcast V1 to V2 if needed)
//             break;
//
//         case nameof(OrderSubmittedV1):
//             // TODO: Handle OrderSubmitted
//             break;
//     }
//
//     projection.Version++;
// }

// TODO: Step 4 - Display projections
Console.WriteLine("[Summary] Order Projections:");
Console.WriteLine();

foreach (var (orderId, projection) in projections.OrderBy(p => p.Key))
{
    Console.WriteLine($"  Order {orderId}:");
    Console.WriteLine($"    Customer:    {projection.CustomerId}");
    if (projection.LoyaltyTier != "Standard")
    {
        Console.WriteLine($"    Loyalty:     {projection.LoyaltyTier}");
    }
    Console.WriteLine($"    Items:       {projection.Items.Count}");
    Console.WriteLine($"    Total:       ${projection.TotalAmount:F2}");
    Console.WriteLine($"    Status:      {projection.Status}");
    Console.WriteLine($"    Version:     {projection.Version}");
    Console.WriteLine();
}

Console.WriteLine("=== Event Store Design Complete ===");
