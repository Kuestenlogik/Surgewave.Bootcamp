// Surgewave Bootcamp - Unit 11, Lesson 1: E-Commerce Pipeline
// COMPLETED SOLUTION

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 11.1 - E-Commerce Pipeline ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// ============== Shared State ==============
var inventory = new Dictionary<string, int>
{
    ["PROD-001"] = 100,
    ["PROD-002"] = 50,
    ["PROD-003"] = 25
};

// ============== Step 1: Order Service (Producer) ==============
Console.WriteLine("--- Step 1: Placing Orders ---");

await using var orderProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

// Create sample orders
var order1 = new Order("ORD-001", "CUST-123",
    new List<OrderItem>
    {
        new("PROD-001", "Widget", 2, 29.99m),
        new("PROD-002", "Gadget", 1, 49.99m)
    }, 109.97m);

var order2 = new Order("ORD-002", "CUST-456",
    new List<OrderItem>
    {
        new("PROD-003", "Gizmo", 3, 19.99m)
    }, 59.97m);

var order3 = new Order("ORD-003", "CUST-789",
    new List<OrderItem>
    {
        new("PROD-001", "Widget", 5, 29.99m),
        new("PROD-002", "Gadget", 2, 49.99m)
    }, 249.93m);

// Produce orders
foreach (var order in new[] { order1, order2, order3 })
{
    var orderJson = JsonSerializer.Serialize(order);
    await orderProducer.ProduceAsync("orders",
        Encoding.UTF8.GetBytes(order.OrderId),
        Encoding.UTF8.GetBytes(orderJson));
    Console.WriteLine($"  Placed order: {order.OrderId} for {order.CustomerId} - ${order.Total}");
}

Console.WriteLine();

// ============== Step 2: Order Processor ==============
Console.WriteLine("--- Step 2: Processing Orders ---");

// Create producers for downstream topics
await using var eventProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

await using var inventoryProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

await using var notificationProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

// Create consumer for orders
await using var orderConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "order-processor";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

orderConsumer.Subscribe("orders");

// Process orders
var ordersProcessed = 0;
var timeout = TimeSpan.FromMilliseconds(500);
var deadline = DateTime.UtcNow.AddSeconds(3);

while (DateTime.UtcNow < deadline && ordersProcessed < 3)
{
    var result = await orderConsumer.ConsumeAsync(timeout);
    if (result?.Value != null)
    {
        var order = JsonSerializer.Deserialize<Order>(result.Value)!;
        Console.WriteLine($"  Processing order: {order.OrderId}");

        // Emit OrderPlaced event
        var placedEvent = new OrderEvent(order.OrderId, "OrderPlaced",
            $"Order placed with {order.Items.Count} items", DateTime.UtcNow);
        await eventProducer.ProduceAsync("order-events",
            Encoding.UTF8.GetBytes(order.OrderId),
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(placedEvent)));

        // Emit OrderValidated event
        var validatedEvent = new OrderEvent(order.OrderId, "OrderValidated",
            "Order passed validation", DateTime.UtcNow);
        await eventProducer.ProduceAsync("order-events",
            Encoding.UTF8.GetBytes(order.OrderId),
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(validatedEvent)));

        // Emit inventory updates for each item
        foreach (var item in order.Items)
        {
            var inventoryUpdate = new InventoryUpdate(item.ProductId, -item.Quantity,
                $"Reserved for order {order.OrderId}");
            await inventoryProducer.ProduceAsync("inventory-updates",
                Encoding.UTF8.GetBytes(item.ProductId),
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(inventoryUpdate)));
        }

        // Emit OrderConfirmed event
        var confirmedEvent = new OrderEvent(order.OrderId, "OrderConfirmed",
            "Order confirmed and ready for fulfillment", DateTime.UtcNow);
        await eventProducer.ProduceAsync("order-events",
            Encoding.UTF8.GetBytes(order.OrderId),
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(confirmedEvent)));

        // Emit notification
        var notification = new Notification(order.CustomerId, "OrderConfirmation",
            $"Your order {order.OrderId} has been confirmed! Total: ${order.Total}");
        await notificationProducer.ProduceAsync("notifications",
            Encoding.UTF8.GetBytes(order.CustomerId),
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notification)));

        ordersProcessed++;
    }
}

Console.WriteLine($"  Processed {ordersProcessed} orders");
Console.WriteLine();

// ============== Step 3: Inventory Service ==============
Console.WriteLine("--- Step 3: Updating Inventory ---");

await using var inventoryConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "inventory-service";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

inventoryConsumer.Subscribe("inventory-updates");

var updatesProcessed = 0;
deadline = DateTime.UtcNow.AddSeconds(2);

while (DateTime.UtcNow < deadline)
{
    var result = await inventoryConsumer.ConsumeAsync(timeout);
    if (result?.Value != null)
    {
        var update = JsonSerializer.Deserialize<InventoryUpdate>(result.Value)!;
        var oldStock = inventory.GetValueOrDefault(update.ProductId, 0);
        var newStock = oldStock + update.QuantityChange;
        inventory[update.ProductId] = newStock;

        Console.WriteLine($"  {update.ProductId}: {oldStock} -> {newStock} ({update.Reason})");

        // Check for low inventory
        if (newStock < 10)
        {
            Console.WriteLine($"    [ALERT] Low inventory for {update.ProductId}: {newStock} remaining");
        }

        updatesProcessed++;
    }
}

Console.WriteLine($"  Processed {updatesProcessed} inventory updates");
Console.WriteLine();

// ============== Step 4: Notification Service ==============
Console.WriteLine("--- Step 4: Sending Notifications ---");

await using var notificationConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "notification-service";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

notificationConsumer.Subscribe("notifications");

var notificationsSent = 0;
deadline = DateTime.UtcNow.AddSeconds(2);

while (DateTime.UtcNow < deadline)
{
    var result = await notificationConsumer.ConsumeAsync(timeout);
    if (result?.Value != null)
    {
        var notification = JsonSerializer.Deserialize<Notification>(result.Value)!;
        Console.WriteLine($"  [EMAIL] To: {notification.CustomerId}");
        Console.WriteLine($"          Type: {notification.Type}");
        Console.WriteLine($"          Message: {notification.Message}");
        notificationsSent++;
    }
}

Console.WriteLine($"  Sent {notificationsSent} notifications");
Console.WriteLine();

// ============== Step 5: Order History ==============
Console.WriteLine("--- Step 5: Order Event History ---");

await using var historyConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "order-history";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

historyConsumer.Subscribe("order-events");

var events = new List<OrderEvent>();
deadline = DateTime.UtcNow.AddSeconds(2);

while (DateTime.UtcNow < deadline)
{
    var result = await historyConsumer.ConsumeAsync(timeout);
    if (result?.Value != null)
    {
        var orderEvent = JsonSerializer.Deserialize<OrderEvent>(result.Value)!;
        events.Add(orderEvent);
    }
}

// Group and display events by order
var eventsByOrder = events.GroupBy(e => e.OrderId).OrderBy(g => g.Key);
foreach (var orderGroup in eventsByOrder)
{
    Console.WriteLine($"  Order {orderGroup.Key}:");
    foreach (var evt in orderGroup.OrderBy(e => e.Timestamp))
    {
        Console.WriteLine($"    [{evt.EventType}] {evt.Details}");
    }
}

Console.WriteLine();

// ============== Final Inventory State ==============
Console.WriteLine("--- Final Inventory State ---");
foreach (var (productId, stock) in inventory.OrderBy(kvp => kvp.Key))
{
    var status = stock < 10 ? "[LOW]" : "[OK]";
    Console.WriteLine($"  {productId}: {stock} units {status}");
}

Console.WriteLine();
Console.WriteLine("E-Commerce Pipeline complete!");

// ============== Domain Models ==============

public record Order(string OrderId, string CustomerId, List<OrderItem> Items, decimal Total);
public record OrderItem(string ProductId, string Name, int Quantity, decimal Price);
public record OrderEvent(string OrderId, string EventType, string Details, DateTime Timestamp);
public record InventoryUpdate(string ProductId, int QuantityChange, string Reason);
public record Notification(string CustomerId, string Type, string Message);
