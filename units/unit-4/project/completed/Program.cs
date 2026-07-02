// Unit 6 Project: E-Commerce Event Store Design - COMPLETED
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

// JSON serialization options
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true
};

// Topic configuration
const string OrdersTopic = "ecommerce.orders";

// Step 1 - Start embedded Surgewave broker
Console.WriteLine("[Setup] Creating topic structure...");

await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    // opt.Acks = Acks.All; // TODO: Acks not yet available on SurgewaveProducerOptions
});

Console.WriteLine($"  Created: {OrdersTopic} (partitions: 6)");
Console.WriteLine($"  Created: ecommerce.customers (partitions: 3)");
Console.WriteLine($"  Created: ecommerce.products (partitions: 3)");
Console.WriteLine($"  Created: ecommerce.inventory (partitions: 6)");
Console.WriteLine();

// Step 2 - Publish domain events
Console.WriteLine("[Events] Publishing domain events...");
Console.WriteLine();

// Helper to create and publish events
async Task PublishEvent<T>(string topic, string aggregateId, T eventData, int schemaVersion, string? extraInfo = null) where T : class
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

    await producer.ProduceAsync(topic, aggregateId, json, headers);

    var info = extraInfo != null ? $" - {extraInfo}" : "";
    Console.WriteLine($"    -> {envelope.EventType} (v{schemaVersion}){info}");
}

// Order 1: Using V1 events
Console.WriteLine("  Order ORD-001:");
await PublishEvent(OrdersTopic, "ORD-001", new OrderCreatedV1("ORD-001", "CUST-123", DateTime.UtcNow), 1);
await PublishEvent(OrdersTopic, "ORD-001", new OrderItemAddedV1("ORD-001", "PROD-001", "Widget", 2, 50.00m), 1, "Widget x2");
await PublishEvent(OrdersTopic, "ORD-001", new OrderItemAddedV1("ORD-001", "PROD-002", "Gadget", 1, 75.00m), 1, "Gadget x1");
await PublishEvent(OrdersTopic, "ORD-001", new OrderSubmittedV1("ORD-001", DateTime.UtcNow), 1);
Console.WriteLine();

// Order 2: Using V2 events (with loyalty tier and discounts)
Console.WriteLine("  Order ORD-002:");
await PublishEvent(OrdersTopic, "ORD-002", new OrderCreatedV2("ORD-002", "CUST-456", DateTime.UtcNow, "Gold", "SAVE10"), 2, "includes loyalty tier");
await PublishEvent(OrdersTopic, "ORD-002", new OrderItemAddedV2("ORD-002", "PROD-003", "Premium Item", 1, 95.00m, 10), 2, "includes discount");
await PublishEvent(OrdersTopic, "ORD-002", new OrderSubmittedV1("ORD-002", DateTime.UtcNow), 1);
Console.WriteLine();

await producer.FlushAsync();

// Step 3 - Build projections from events
Console.WriteLine("[Projection] Building Order Summary...");
Console.WriteLine();

var projections = new Dictionary<string, OrderProjection>();

// Event upcasters - transform V1 events to V2 for uniform processing
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

// Consume events and build projections
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    opt.GroupId = "projection-builder";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
consumer.Subscribe(OrdersTopic);

var currentOrderId = "";
var eventCount = 0;

while (true)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
    if (result == null) break;

    var envelope = JsonSerializer.Deserialize<EventEnvelope>(result.Value, jsonOptions);
    if (envelope == null) continue;

    // Track which order we're processing
    if (envelope.AggregateId != currentOrderId)
    {
        currentOrderId = envelope.AggregateId;
        eventCount = 0;
        var versionNote = envelope.SchemaVersion > 1 ? " (with v2 events)" : "";
        Console.WriteLine($"  Processing events for {currentOrderId}{versionNote}...");
    }
    eventCount++;

    // Get or create projection
    if (!projections.TryGetValue(envelope.AggregateId, out var projection))
    {
        projection = new OrderProjection { OrderId = envelope.AggregateId };
        projections[envelope.AggregateId] = projection;
    }

    // Apply event based on type (with upcasting)
    switch (envelope.EventType)
    {
        case nameof(OrderCreatedV1):
            var createdV1 = JsonSerializer.Deserialize<OrderCreatedV1>(envelope.Payload, jsonOptions)!;
            var createdV2FromV1 = UpcastOrderCreatedV1(createdV1);
            projection.CustomerId = createdV2FromV1.CustomerId;
            projection.LoyaltyTier = createdV2FromV1.LoyaltyTier;
            projection.CouponCode = createdV2FromV1.CouponCode;
            projection.CreatedAt = createdV2FromV1.CreatedAt;
            Console.WriteLine($"    Event {eventCount}: OrderCreated -> new order");
            break;

        case nameof(OrderCreatedV2):
            var createdV2 = JsonSerializer.Deserialize<OrderCreatedV2>(envelope.Payload, jsonOptions)!;
            projection.CustomerId = createdV2.CustomerId;
            projection.LoyaltyTier = createdV2.LoyaltyTier;
            projection.CouponCode = createdV2.CouponCode;
            projection.CreatedAt = createdV2.CreatedAt;
            Console.WriteLine($"    Event {eventCount}: OrderCreated (v2 -> v2) -> new order, tier={createdV2.LoyaltyTier}");
            break;

        case nameof(OrderItemAddedV1):
            var itemV1 = JsonSerializer.Deserialize<OrderItemAddedV1>(envelope.Payload, jsonOptions)!;
            var itemV2FromV1 = UpcastOrderItemAddedV1(itemV1);
            projection.Items.Add(new OrderLineItem(
                itemV2FromV1.ProductId,
                itemV2FromV1.ProductName,
                itemV2FromV1.Quantity,
                itemV2FromV1.UnitPrice,
                itemV2FromV1.DiscountPercent
            ));
            Console.WriteLine($"    Event {eventCount}: OrderItemAdded -> {projection.Items.Count} items, ${projection.TotalAmount:F2}");
            break;

        case nameof(OrderItemAddedV2):
            var itemV2 = JsonSerializer.Deserialize<OrderItemAddedV2>(envelope.Payload, jsonOptions)!;
            projection.Items.Add(new OrderLineItem(
                itemV2.ProductId,
                itemV2.ProductName,
                itemV2.Quantity,
                itemV2.UnitPrice,
                itemV2.DiscountPercent
            ));
            var discountInfo = itemV2.DiscountPercent > 0 ? $" ({itemV2.DiscountPercent}% discount)" : "";
            Console.WriteLine($"    Event {eventCount}: OrderItemAdded (v2 -> v2) -> {projection.Items.Count} item, ${projection.TotalAmount:F2}{discountInfo}");
            break;

        case nameof(OrderSubmittedV1):
            var submitted = JsonSerializer.Deserialize<OrderSubmittedV1>(envelope.Payload, jsonOptions)!;
            projection.Status = "Submitted";
            projection.SubmittedAt = submitted.SubmittedAt;
            Console.WriteLine($"    Event {eventCount}: OrderSubmitted -> status=submitted");
            break;
    }

    projection.Version++;
}

Console.WriteLine();

// Step 4 - Display projections
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
    public string? CouponCode { get; set; }
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
