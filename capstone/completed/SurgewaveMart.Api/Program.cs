// SurgewaveMart API Gateway - COMPLETED
// REST endpoints for order management and queries

using System.Collections.Concurrent;
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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

// Create topics
await runtime.LogManager.CreateTopicAsync(Topics.Orders, partitionCount: 6);
await runtime.LogManager.CreateTopicAsync(Topics.OrdersConfirmed, partitionCount: 3);
await runtime.LogManager.CreateTopicAsync(Topics.Payments, partitionCount: 3);
await runtime.LogManager.CreateTopicAsync(Topics.Shipments, partitionCount: 3);

// Create a producer for sending order events
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = bootstrapServers;
});

Console.WriteLine($"[Setup] Surgewave broker started on {bootstrapServers}, topics created");

// State stores (simulating state store queries)
var orders = new ConcurrentDictionary<string, Order>();
var inventory = new ConcurrentDictionary<string, InventoryItem>();
var shipments = new ConcurrentDictionary<string, Shipment>();
var analytics = new AnalyticsState();

// Initialize inventory
InitializeInventory(inventory);

// ===== ORDER ENDPOINTS =====

app.MapPost("/api/orders", async (CreateOrderRequest request) =>
{
    // Validate request
    if (string.IsNullOrEmpty(request.CustomerId))
        return Results.BadRequest(new { error = "Customer ID is required" });

    if (request.Items == null || request.Items.Count == 0)
        return Results.BadRequest(new { error = "Order must have at least one item" });

    foreach (var item in request.Items)
    {
        if (item.Quantity <= 0)
            return Results.BadRequest(new { error = $"Invalid quantity for product {item.ProductId}" });

        if (!inventory.ContainsKey(item.ProductId))
            return Results.BadRequest(new { error = $"Product {item.ProductId} not found" });
    }

    // Generate order ID
    var orderId = $"ORD-{Guid.NewGuid():N}"[..12];

    // Calculate total
    var total = request.Items.Sum(i =>
    {
        var price = inventory.TryGetValue(i.ProductId, out var inv) ? inv.Price : 0;
        return i.Quantity * price;
    });

    // Create OrderCreated event
    var orderEvent = new OrderCreated(
        orderId,
        request.CustomerId,
        request.Items.Select(i => new OrderItemEvent(
            i.ProductId,
            i.Quantity,
            inventory.TryGetValue(i.ProductId, out var inv) ? inv.Price : 0
        )).ToList(),
        total,
        DateTime.UtcNow
    );

    // Produce to orders topic
    await producer.ProduceAsync(Topics.Orders, orderId, JsonSerializer.Serialize(orderEvent));

    // Store order in local state
    var order = new Order(
        orderId,
        request.CustomerId,
        request.Items.Select(i => new OrderItem(
            i.ProductId,
            i.Quantity,
            inventory.TryGetValue(i.ProductId, out var inv) ? inv.Price : 0
        )).ToList(),
        total,
        OrderStatus.Pending,
        DateTime.UtcNow
    );
    orders[orderId] = order;

    // Update analytics
    analytics.TotalOrders++;
    analytics.TotalRevenue += total;

    Console.WriteLine($"[API] Order {orderId} created, total: ${total:F2}");

    return Results.Created($"/api/orders/{orderId}", new
    {
        orderId,
        status = "Pending",
        total,
        message = "Order submitted for processing"
    });
})
.WithName("CreateOrder");

app.MapGet("/api/orders/{id}", (string id) =>
{
    if (!orders.TryGetValue(id, out var order))
        return Results.NotFound(new { error = $"Order {id} not found" });

    return Results.Ok(new
    {
        order.OrderId,
        order.CustomerId,
        Items = order.Items.Select(i => new
        {
            i.ProductId,
            ProductName = inventory.TryGetValue(i.ProductId, out var inv) ? inv.ProductName : "Unknown",
            i.Quantity,
            i.UnitPrice,
            Subtotal = i.Quantity * i.UnitPrice
        }),
        order.Total,
        Status = order.Status.ToString(),
        order.CreatedAt
    });
})
.WithName("GetOrder");

app.MapPost("/api/orders/{id}/cancel", async (string id) =>
{
    if (!orders.TryGetValue(id, out var order))
        return Results.NotFound(new { error = $"Order {id} not found" });

    if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
        return Results.BadRequest(new { error = "Cannot cancel shipped or delivered orders" });

    if (order.Status == OrderStatus.Cancelled)
        return Results.BadRequest(new { error = "Order is already cancelled" });

    // Produce OrderCancelled event
    var cancelled = new OrderCancelled(id, "Customer requested cancellation", DateTime.UtcNow);
    await producer.ProduceAsync(Topics.Orders, id, JsonSerializer.Serialize(cancelled));

    // Update local state
    orders[id] = order with { Status = OrderStatus.Cancelled };

    Console.WriteLine($"[API] Order {id} cancelled");

    return Results.Ok(new { orderId = id, status = "Cancelled" });
})
.WithName("CancelOrder");

// ===== INVENTORY ENDPOINTS =====

app.MapGet("/api/inventory", () =>
{
    return Results.Ok(inventory.Values.OrderBy(i => i.ProductId).Select(i => new
    {
        i.ProductId,
        i.ProductName,
        i.QuantityAvailable,
        i.QuantityReserved,
        EffectiveStock = i.QuantityAvailable - i.QuantityReserved,
        i.Price
    }));
})
.WithName("ListInventory");

app.MapGet("/api/inventory/{productId}", (string productId) =>
{
    if (!inventory.TryGetValue(productId, out var item))
        return Results.NotFound(new { error = $"Product {productId} not found" });

    return Results.Ok(new
    {
        item.ProductId,
        item.ProductName,
        item.QuantityAvailable,
        item.QuantityReserved,
        EffectiveStock = item.QuantityAvailable - item.QuantityReserved,
        item.Price
    });
})
.WithName("GetInventory");

// ===== SHIPMENT ENDPOINTS =====

app.MapGet("/api/shipments/{orderId}", (string orderId) =>
{
    if (!shipments.TryGetValue(orderId, out var shipment))
    {
        // Check if order exists but hasn't shipped yet
        if (orders.TryGetValue(orderId, out var order))
        {
            return Results.Ok(new
            {
                orderId,
                status = order.Status < OrderStatus.Shipped ? "Not yet shipped" : "Unknown",
                orderStatus = order.Status.ToString()
            });
        }
        return Results.NotFound(new { error = $"Order {orderId} not found" });
    }

    return Results.Ok(new
    {
        shipment.ShipmentId,
        shipment.OrderId,
        shipment.TrackingNumber,
        Status = shipment.Status.ToString(),
        shipment.DeliveryAddress,
        shipment.EstimatedDelivery
    });
})
.WithName("GetShipment");

// ===== ANALYTICS ENDPOINTS =====

app.MapGet("/api/analytics/summary", () =>
{
    var ordersList = orders.Values.ToList();

    return Results.Ok(new
    {
        analytics.TotalOrders,
        analytics.TotalRevenue,
        AverageOrderValue = analytics.TotalOrders > 0
            ? analytics.TotalRevenue / analytics.TotalOrders
            : 0,
        OrdersByStatus = ordersList
            .GroupBy(o => o.Status)
            .ToDictionary(g => g.Key.ToString(), g => g.Count()),
        TopProducts = ordersList
            .SelectMany(o => o.Items)
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                ProductName = inventory.TryGetValue(g.Key, out var inv) ? inv.ProductName : "Unknown",
                TotalQuantity = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.Quantity * i.UnitPrice)
            })
            .OrderByDescending(p => p.TotalRevenue)
            .Take(5),
        Timestamp = DateTime.UtcNow
    });
})
.WithName("GetAnalyticsSummary");

// ===== HEALTH ENDPOINTS =====

app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        status = "healthy",
        components = new
        {
            broker = "connected",
            stateStores = "available",
            api = "running"
        },
        stats = new
        {
            ordersInMemory = orders.Count,
            productsInMemory = inventory.Count
        },
        timestamp = DateTime.UtcNow
    });
})
.WithName("HealthCheck");

app.MapGet("/api/health/ready", () =>
{
    return Results.Ok(new { status = "ready" });
})
.WithName("ReadinessCheck");

app.MapGet("/api/health/live", () =>
{
    return Results.Ok(new { status = "alive" });
})
.WithName("LivenessCheck");

// Cleanup on shutdown
app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("[Shutdown] API stopping, cleaning up...");
    // Runtime and producer are disposed via await using
});

Console.WriteLine("[API] SurgewaveMart API started on http://localhost:5000");
Console.WriteLine("[API] Swagger UI available at http://localhost:5000/swagger\n");

app.Run();

// Helper methods
void InitializeInventory(ConcurrentDictionary<string, InventoryItem> inv)
{
    var products = new[]
    {
        new InventoryItem("P-001", "Widget Pro", 100, 0, 29.99m),
        new InventoryItem("P-002", "Gadget X", 50, 0, 149.99m),
        new InventoryItem("P-003", "Tool Set", 200, 0, 49.99m),
        new InventoryItem("P-004", "Premium Device", 25, 0, 299.99m),
        new InventoryItem("P-005", "Basic Kit", 500, 0, 19.99m),
    };
    foreach (var p in products)
    {
        inv[p.ProductId] = p;
    }
}

// DTOs
record CreateOrderRequest(string CustomerId, List<CreateOrderItem> Items);
record CreateOrderItem(string ProductId, int Quantity);

class AnalyticsState
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
}
