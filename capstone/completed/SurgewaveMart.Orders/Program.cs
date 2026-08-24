// SurgewaveMart Order Service - COMPLETED
// Handles order creation, validation, and lifecycle management

using System.Collections.Concurrent;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using SurgewaveMart.Shared;
using SurgewaveMart.Shared.Domain;
using SurgewaveMart.Shared.Events;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== SurgewaveMart Order Service ===\n");

// State stores
var orderStates = new ConcurrentDictionary<string, OrderState>();
var inventory = new ConcurrentDictionary<string, InventoryItem>();
var reservations = new ConcurrentDictionary<string, StockReservation>();

// Initialize sample inventory
InitializeInventory();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;

Console.WriteLine("[Setup] Topics will be auto-created");
Console.WriteLine("[Inventory] Sample products initialized\n");

// Create producer and consumer
await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "order-service";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe(Topics.Orders);

Console.WriteLine("[Consumer] Starting order processor...");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var processedCount = 0;
var confirmedCount = 0;
var rejectedCount = 0;

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        var result = await consumer.ConsumeAsync(cts.Token);
        if (result is null) continue;

        try
        {
            var orderEvent = JsonSerializer.Deserialize<OrderCreated>(result.Value);
            if (orderEvent is null)
            {
                Console.WriteLine($"[Error] Invalid order message, sending to DLQ");
                await SendToDlq(producer, result.Value, "Invalid order format");
                continue;
            }

            Console.WriteLine($"[Processing] Order {orderEvent.OrderId} from customer {orderEvent.CustomerId}");

            // Step 1: Validate order
            var validationResult = ValidateOrder(orderEvent);
            if (!validationResult.IsValid)
            {
                Console.WriteLine($"[Rejected] Order {orderEvent.OrderId}: {validationResult.Error}");
                await ProduceOrderRejected(producer, orderEvent.OrderId, validationResult.Error);
                rejectedCount++;
                continue;
            }

            // Step 2: Check inventory availability
            var availabilityResult = CheckInventoryAvailability(orderEvent.Items);
            if (!availabilityResult.IsAvailable)
            {
                Console.WriteLine($"[Rejected] Order {orderEvent.OrderId}: {availabilityResult.Error}");
                await ProduceOrderRejected(producer, orderEvent.OrderId, availabilityResult.Error);
                rejectedCount++;
                continue;
            }

            // Step 3: Reserve inventory
            var reservationId = ReserveInventory(orderEvent.OrderId, orderEvent.Items);
            Console.WriteLine($"[Reserved] Inventory for order {orderEvent.OrderId}, reservation: {reservationId}");

            // Produce InventoryReserved event
            var reservedEvent = new InventoryReserved(
                reservationId,
                orderEvent.OrderId,
                orderEvent.Items.Select(i => new ReservationItem(i.ProductId, i.Quantity)).ToList(),
                DateTime.UtcNow.AddMinutes(15)
            );
            await producer.ProduceAsync(Topics.InventoryUpdates, orderEvent.OrderId, JsonSerializer.Serialize(reservedEvent));

            // Step 4: Produce to orders-confirmed topic
            var confirmed = new OrderConfirmed(orderEvent.OrderId, reservationId, DateTime.UtcNow);
            await producer.ProduceAsync(Topics.OrdersConfirmed, orderEvent.OrderId, JsonSerializer.Serialize(confirmed));

            // Update order state
            orderStates[orderEvent.OrderId] = new OrderState(
                orderEvent.OrderId,
                orderEvent.CustomerId,
                orderEvent.Total,
                OrderStatus.Confirmed,
                reservationId,
                DateTime.UtcNow
            );

            Console.WriteLine($"[Confirmed] Order {orderEvent.OrderId} -> orders-confirmed topic");
            processedCount++;
            confirmedCount++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to process order: {ex.Message}");
            await SendToDlq(producer, result.Value, ex.Message);
        }
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("\n[Shutdown] Order service stopping...");
}

// Print summary
Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("[Stats] Order Processing Summary:");
Console.WriteLine($"  Processed: {processedCount}");
Console.WriteLine($"  Confirmed: {confirmedCount}");
Console.WriteLine($"  Rejected:  {rejectedCount}");
Console.WriteLine(new string('=', 50));

Console.WriteLine("=== Order Service Stopped ===");

// Helper methods
void InitializeInventory()
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
        inventory[p.ProductId] = p;
    }
}

(bool IsValid, string Error) ValidateOrder(OrderCreated order)
{
    if (order.Items == null || order.Items.Count == 0)
        return (false, "Order must have at least one item");

    foreach (var item in order.Items)
    {
        if (item.Quantity <= 0)
            return (false, $"Invalid quantity for product {item.ProductId}");
        if (item.UnitPrice <= 0)
            return (false, $"Invalid price for product {item.ProductId}");
    }

    var calculatedTotal = order.Items.Sum(i => i.Quantity * i.UnitPrice);
    if (Math.Abs(calculatedTotal - order.Total) > 0.01m)
        return (false, $"Total mismatch: expected {calculatedTotal:F2}, got {order.Total:F2}");

    return (true, string.Empty);
}

(bool IsAvailable, string Error) CheckInventoryAvailability(List<OrderItemEvent> items)
{
    foreach (var item in items)
    {
        if (!inventory.TryGetValue(item.ProductId, out var inv))
            return (false, $"Product {item.ProductId} not found");

        var available = inv.QuantityAvailable - inv.QuantityReserved;
        if (available < item.Quantity)
            return (false, $"Insufficient stock for {item.ProductId}: need {item.Quantity}, have {available}");
    }
    return (true, string.Empty);
}

string ReserveInventory(string orderId, List<OrderItemEvent> items)
{
    var reservationId = $"RES-{Guid.NewGuid():N}"[..12];

    foreach (var item in items)
    {
        if (inventory.TryGetValue(item.ProductId, out var inv))
        {
            inventory[item.ProductId] = inv with
            {
                QuantityReserved = inv.QuantityReserved + item.Quantity
            };

            reservations[$"{reservationId}-{item.ProductId}"] = new StockReservation(
                reservationId, orderId, item.ProductId, item.Quantity,
                DateTime.UtcNow.AddMinutes(15)
            );
        }
    }

    return reservationId;
}

async Task ProduceOrderRejected(SurgewaveProducer<string, string> producer, string orderId, string reason)
{
    var rejected = new OrderRejected(orderId, reason, DateTime.UtcNow);
    await producer.ProduceAsync(Topics.Orders, orderId, JsonSerializer.Serialize(rejected));
}

async Task SendToDlq(SurgewaveProducer<string, string> producer, string message, string error)
{
    var dlqMessage = JsonSerializer.Serialize(new
    {
        OriginalTopic = Topics.Orders,
        Message = message,
        Error = error,
        FailedAt = DateTime.UtcNow
    });
    await producer.ProduceAsync(Topics.DeadLetterQueue, Topics.Orders, dlqMessage);
}

record OrderState(string OrderId, string CustomerId, decimal Total, OrderStatus Status, string? ReservationId, DateTime UpdatedAt);