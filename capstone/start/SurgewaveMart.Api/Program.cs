// SurgewaveMart API Gateway
// REST endpoints for order management and queries

using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Serialization;
using SurgewaveMart.Shared;
using SurgewaveMart.Shared.Domain;
using SurgewaveMart.Shared.Events;
using Kuestenlogik.Surgewave.Broker;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// TODO: Add Surgewave client as singleton service
// builder.Services.AddSingleton<ISurgewaveClient>(sp => ...);

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Start embedded Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;

// Create topics explicitly (optional with AutoCreateTopics enabled)
await runtime.LogManager.CreateTopicAsync(Topics.Orders, partitions: 6);
await runtime.LogManager.CreateTopicAsync(Topics.OrdersConfirmed, partitions: 3);

// Create a producer for sending order events
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = bootstrapServers;
});

Console.WriteLine($"[Setup] Surgewave broker started on {bootstrapServers}, topics created");

// In-memory state (in production, query state stores)
var orders = new Dictionary<string, Order>();
var inventory = new Dictionary<string, InventoryItem>
{
    ["P-001"] = new("P-001", "Widget Pro", 100, 0, 29.99m),
    ["P-002"] = new("P-002", "Gadget X", 50, 0, 149.99m),
    ["P-003"] = new("P-003", "Tool Set", 200, 0, 49.99m),
};

// ===== ORDER ENDPOINTS =====

// POST /api/orders - Create a new order
app.MapPost("/api/orders", async (CreateOrderRequest request) =>
{
    // TODO: Validate request
    if (request.Items == null || request.Items.Count == 0)
    {
        return Results.BadRequest(new { error = "Order must have at least one item" });
    }

    // Generate order ID
    var orderId = $"ORD-{Guid.NewGuid():N}"[..12];

    // TODO: Calculate total from items
    var total = request.Items.Sum(i => i.Quantity * i.UnitPrice);

    // TODO: Create OrderCreated event
    var orderEvent = new OrderCreated(
        orderId,
        request.CustomerId,
        request.Items.Select(i => new OrderItemEvent(i.ProductId, i.Quantity, i.UnitPrice)).ToList(),
        total,
        DateTime.UtcNow
    );

    // TODO: Produce to orders topic
    // await producer.ProduceAsync(Topics.Orders, orderId, JsonSerializer.Serialize(orderEvent));

    // TODO: Store order in state (temporary, until consumed)
    var order = new Order(orderId, request.CustomerId,
        request.Items.Select(i => new OrderItem(i.ProductId, i.Quantity, i.UnitPrice)).ToList(),
        total, OrderStatus.Pending, DateTime.UtcNow);
    orders[orderId] = order;

    Console.WriteLine($"[API] Order {orderId} created, total: ${total:F2}");

    return Results.Created($"/api/orders/{orderId}", new { orderId, status = "Pending", total });
})
.WithName("CreateOrder");

// GET /api/orders/{id} - Get order status
app.MapGet("/api/orders/{id}", (string id) =>
{
    // TODO: Query order state store
    if (!orders.TryGetValue(id, out var order))
    {
        return Results.NotFound(new { error = $"Order {id} not found" });
    }

    return Results.Ok(new
    {
        order.OrderId,
        order.CustomerId,
        order.Items,
        order.Total,
        Status = order.Status.ToString(),
        order.CreatedAt
    });
})
.WithName("GetOrder");

// POST /api/orders/{id}/cancel - Cancel an order
app.MapPost("/api/orders/{id}/cancel", async (string id) =>
{
    // TODO: Check if order can be cancelled
    if (!orders.TryGetValue(id, out var order))
    {
        return Results.NotFound(new { error = $"Order {id} not found" });
    }

    if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
    {
        return Results.BadRequest(new { error = "Cannot cancel shipped or delivered orders" });
    }

    // TODO: Produce OrderCancelled event
    // var cancelled = new OrderCancelled(id, "Customer requested", DateTime.UtcNow);
    // await producer.ProduceAsync(Topics.Orders, id, JsonSerializer.Serialize(cancelled));

    // Update local state
    orders[id] = order with { Status = OrderStatus.Cancelled };

    Console.WriteLine($"[API] Order {id} cancelled");

    return Results.Ok(new { orderId = id, status = "Cancelled" });
})
.WithName("CancelOrder");

// ===== INVENTORY ENDPOINTS =====

// GET /api/inventory - List all inventory
app.MapGet("/api/inventory", () =>
{
    // TODO: Query inventory state store
    return Results.Ok(inventory.Values.Select(i => new
    {
        i.ProductId,
        i.ProductName,
        i.QuantityAvailable,
        i.QuantityReserved,
        i.Price
    }));
})
.WithName("ListInventory");

// GET /api/inventory/{productId} - Get product stock
app.MapGet("/api/inventory/{productId}", (string productId) =>
{
    if (!inventory.TryGetValue(productId, out var item))
    {
        return Results.NotFound(new { error = $"Product {productId} not found" });
    }

    return Results.Ok(new
    {
        item.ProductId,
        item.ProductName,
        item.QuantityAvailable,
        item.QuantityReserved,
        item.Price
    });
})
.WithName("GetInventory");

// ===== ANALYTICS ENDPOINTS =====

// GET /api/analytics/summary - Get current metrics
app.MapGet("/api/analytics/summary", () =>
{
    // TODO: Query analytics state store
    return Results.Ok(new
    {
        ordersToday = orders.Count,
        revenueToday = orders.Values.Sum(o => o.Total),
        averageOrderValue = orders.Count > 0 ? orders.Values.Average(o => o.Total) : 0,
        topProducts = new[] { "P-001", "P-002", "P-003" }
    });
})
.WithName("GetAnalyticsSummary");

// ===== HEALTH ENDPOINTS =====

// GET /api/health - Health check
app.MapGet("/api/health", () =>
{
    // TODO: Check broker connection, state stores, etc.
    return Results.Ok(new
    {
        status = "healthy",
        broker = "connected",
        timestamp = DateTime.UtcNow
    });
})
.WithName("HealthCheck");

// Cleanup on shutdown
app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("[Shutdown] API stopping, cleaning up...");
    // Runtime is disposed via await using
});

Console.WriteLine("[API] SurgewaveMart API started on http://localhost:5000");

app.Run();

// Request/Response DTOs
record CreateOrderRequest(string CustomerId, List<CreateOrderItem> Items);
record CreateOrderItem(string ProductId, int Quantity, decimal UnitPrice);
