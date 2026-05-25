# Lesson 11.1: E-Commerce Pipeline - Instructions

## Step 1: Define the Domain Models

Create the core domain types for your e-commerce system:

```csharp
public record Order(string OrderId, string CustomerId, List<OrderItem> Items, decimal Total);
public record OrderItem(string ProductId, string Name, int Quantity, decimal Price);
public record OrderEvent(string OrderId, string EventType, string Details, DateTime Timestamp);
public record InventoryUpdate(string ProductId, int QuantityChange, string Reason);
public record Notification(string CustomerId, string Type, string Message);
```

## Step 2: Create the Order Service (Producer)

The order service generates new orders:

```csharp
await using var orderProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var order = new Order("ORD-001", "CUST-123", items, 99.99m);
var orderJson = JsonSerializer.Serialize(order);
await orderProducer.ProduceAsync("orders",
    Encoding.UTF8.GetBytes(order.OrderId),
    Encoding.UTF8.GetBytes(orderJson));
```

## Step 3: Create the Order Processor

The order processor consumes orders and emits lifecycle events:

```csharp
await using var orderConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "order-processor";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

orderConsumer.Subscribe("orders");

// Process and emit events
var result = await orderConsumer.ConsumeAsync(timeout);
if (result != null)
{
    var order = JsonSerializer.Deserialize<Order>(result.Value);

    // Emit order events
    await EmitOrderEvent(order.OrderId, "OrderValidated", "Order passed validation");
    await EmitInventoryUpdate(order.Items);
    await EmitNotification(order.CustomerId, "Order confirmed!");
}
```

## Step 4: Create the Inventory Service

Consume inventory updates and track stock levels:

```csharp
await using var inventoryConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "inventory-service";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

inventoryConsumer.Subscribe("inventory-updates");

// Track inventory in-memory (in production, use a database)
var inventory = new Dictionary<string, int>();

var update = JsonSerializer.Deserialize<InventoryUpdate>(result.Value);
inventory[update.ProductId] = inventory.GetValueOrDefault(update.ProductId, 100) + update.QuantityChange;
```

## Step 5: Create the Notification Service

Consume notification requests and send to customers:

```csharp
await using var notificationConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "notification-service";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

notificationConsumer.Subscribe("notifications");

var notification = JsonSerializer.Deserialize<Notification>(result.Value);
Console.WriteLine($"[EMAIL] To: {notification.CustomerId} - {notification.Message}");
```

## Step 6: Run the Complete Pipeline

Orchestrate all services and process orders end-to-end:

```csharp
// 1. Place orders
await PlaceOrder(new Order("ORD-001", "CUST-123", items1, 149.99m));
await PlaceOrder(new Order("ORD-002", "CUST-456", items2, 79.99m));

// 2. Process orders (generates events)
await ProcessOrders();

// 3. Update inventory
await ProcessInventoryUpdates();

// 4. Send notifications
await ProcessNotifications();

// 5. View order history
await DisplayOrderHistory();
```

## Exercises

1. **Basic Pipeline**: Complete the TODO items in `start/Program.cs`
2. **Add Order Status**: Track order status (pending, confirmed, shipped, delivered)
3. **Low Inventory Alert**: Emit alerts when inventory falls below threshold
4. **Retry Logic**: Add retry handling for failed notifications
5. **Order Cancellation**: Implement order cancellation with inventory rollback

## Testing Your Implementation

Run the completed solution:

```bash
cd completed
dotnet run
```

Expected output:
- Orders placed and events emitted
- Inventory updated for each order item
- Notifications sent for each order
- Complete audit trail of events
