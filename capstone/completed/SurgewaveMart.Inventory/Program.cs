// SurgewaveMart Inventory Service - COMPLETED
// Manages stock levels and reservations

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

Console.WriteLine("=== SurgewaveMart Inventory Service ===\n");

const int LowStockThreshold = 10;

// State stores
var inventory = new ConcurrentDictionary<string, InventoryItem>();
var reservations = new ConcurrentDictionary<string, ReservationRecord>();

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
PrintInventoryState("Initial");

// Create producer and consumer
await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "inventory-service";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe(Topics.InventoryUpdates);

Console.WriteLine("\n[Consumer] Starting inventory processor...");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// Background task for reservation cleanup
var cleanupTask = Task.Run(async () =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), cts.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        await CleanupExpiredReservations(producer);
    }
});

var reservedCount = 0;
var releasedCount = 0;
var committedCount = 0;
var alertCount = 0;

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        var result = await consumer.ConsumeAsync(cts.Token);
        if (result is null) continue;

        try
        {
            // Parse event type
            using var doc = JsonDocument.Parse(result.Value);
            var eventType = doc.RootElement.GetProperty("EventType").GetString();

            switch (eventType)
            {
                case "InventoryReserved":
                    var reserved = JsonSerializer.Deserialize<InventoryReserved>(result.Value)!;
                    HandleInventoryReserved(reserved);
                    reservedCount++;
                    break;

                case "InventoryReleased":
                    var released = JsonSerializer.Deserialize<InventoryReleased>(result.Value)!;
                    HandleInventoryReleased(released);
                    releasedCount++;
                    break;

                case "InventoryCommitted":
                    var committed = JsonSerializer.Deserialize<InventoryCommitted>(result.Value)!;
                    HandleInventoryCommitted(committed);
                    committedCount++;
                    break;

                default:
                    Console.WriteLine($"[Warning] Unknown event type: {eventType}");
                    break;
            }

            // Check for low stock alerts
            foreach (var (productId, item) in inventory)
            {
                if (item.QuantityAvailable - item.QuantityReserved < LowStockThreshold)
                {
                    await EmitLowStockAlert(producer, item);
                    alertCount++;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to process inventory event: {ex.Message}");
        }
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("\n[Shutdown] Inventory service stopping...");
}

// Print summary
Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("[Stats] Inventory Processing Summary:");
Console.WriteLine($"  Reserved:  {reservedCount}");
Console.WriteLine($"  Released:  {releasedCount}");
Console.WriteLine($"  Committed: {committedCount}");
Console.WriteLine($"  Alerts:    {alertCount}");
Console.WriteLine(new string('=', 50));

PrintInventoryState("Final");

Console.WriteLine("=== Inventory Service Stopped ===");

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
        Console.WriteLine($"[Init] {p.ProductName}: {p.QuantityAvailable} units @ ${p.Price}");
    }
}

void HandleInventoryReserved(InventoryReserved evt)
{
    Console.WriteLine($"[Reserved] Order {evt.OrderId}, Reservation {evt.ReservationId}");

    foreach (var item in evt.Items)
    {
        reservations[$"{evt.ReservationId}-{item.ProductId}"] = new ReservationRecord(
            evt.ReservationId,
            evt.OrderId,
            item.ProductId,
            item.Quantity,
            evt.ExpiresAt
        );

        // Note: Actual reservation is done by Order Service
        // This handler just tracks the reservation
        Console.WriteLine($"  - {item.ProductId}: {item.Quantity} units");
    }
}

void HandleInventoryReleased(InventoryReleased evt)
{
    Console.WriteLine($"[Released] Order {evt.OrderId}, Reservation {evt.ReservationId}: {evt.Reason}");

    var toRemove = reservations
        .Where(kv => kv.Value.ReservationId == evt.ReservationId)
        .ToList();

    foreach (var (key, reservation) in toRemove)
    {
        if (inventory.TryGetValue(reservation.ProductId, out var item))
        {
            inventory[reservation.ProductId] = item with
            {
                QuantityReserved = Math.Max(0, item.QuantityReserved - reservation.Quantity)
            };
        }
        reservations.TryRemove(key, out _);
        Console.WriteLine($"  - {reservation.ProductId}: {reservation.Quantity} units released");
    }
}

void HandleInventoryCommitted(InventoryCommitted evt)
{
    Console.WriteLine($"[Committed] Order {evt.OrderId}, Reservation {evt.ReservationId}");

    var toRemove = reservations
        .Where(kv => kv.Value.ReservationId == evt.ReservationId)
        .ToList();

    foreach (var (key, reservation) in toRemove)
    {
        if (inventory.TryGetValue(reservation.ProductId, out var item))
        {
            // Deduct from available and reserved
            inventory[reservation.ProductId] = item with
            {
                QuantityAvailable = item.QuantityAvailable - reservation.Quantity,
                QuantityReserved = Math.Max(0, item.QuantityReserved - reservation.Quantity)
            };
        }
        reservations.TryRemove(key, out _);
        Console.WriteLine($"  - {reservation.ProductId}: {reservation.Quantity} units sold");
    }
}

async Task CleanupExpiredReservations(SurgewaveProducer<string, string> producer)
{
    var now = DateTime.UtcNow;
    var expired = reservations
        .Where(kv => kv.Value.ExpiresAt < now)
        .ToList();

    foreach (var (key, reservation) in expired)
    {
        var released = new InventoryReleased(
            reservation.ReservationId,
            reservation.OrderId,
            "Reservation expired",
            DateTime.UtcNow
        );

        await producer.ProduceAsync(Topics.InventoryUpdates, reservation.OrderId, JsonSerializer.Serialize(released));

        Console.WriteLine($"[Cleanup] Expired reservation {reservation.ReservationId} for order {reservation.OrderId}");
    }
}

async Task EmitLowStockAlert(SurgewaveProducer<string, string> producer, InventoryItem item)
{
    var alert = new LowStockAlert(
        item.ProductId,
        item.ProductName,
        item.QuantityAvailable - item.QuantityReserved,
        LowStockThreshold,
        DateTime.UtcNow
    );

    await producer.ProduceAsync(Topics.Analytics, item.ProductId, JsonSerializer.Serialize(alert));

    Console.WriteLine($"[ALERT] Low stock: {item.ProductName} ({item.QuantityAvailable - item.QuantityReserved} available)");
}

void PrintInventoryState(string label)
{
    Console.WriteLine($"\n[{label}] Inventory State:");
    foreach (var (productId, item) in inventory.OrderBy(x => x.Key))
    {
        var effective = item.QuantityAvailable - item.QuantityReserved;
        Console.WriteLine($"  {item.ProductName}: {effective} available ({item.QuantityReserved} reserved)");
    }
}

record ReservationRecord(string ReservationId, string OrderId, string ProductId, int Quantity, DateTime ExpiresAt);