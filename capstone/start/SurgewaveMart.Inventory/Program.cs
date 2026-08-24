// SurgewaveMart Inventory Service
// Manages stock levels and reservations

using System.Collections.Concurrent;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client.Producer;
using Kuestenlogik.Surgewave.Client.Consumer;
using SurgewaveMart.Shared;
using SurgewaveMart.Shared.Domain;
using SurgewaveMart.Shared.Events;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== SurgewaveMart Inventory Service ===\n");

// In-memory state stores (in production, use persistent stores)
var inventory = new ConcurrentDictionary<string, InventoryItem>();
var reservations = new ConcurrentDictionary<string, StockReservation>();

// Initialize sample inventory
var sampleProducts = new[]
{
    new InventoryItem("P-001", "Widget Pro", 100, 0, 29.99m),
    new InventoryItem("P-002", "Gadget X", 50, 0, 149.99m),
    new InventoryItem("P-003", "Tool Set", 200, 0, 49.99m),
    new InventoryItem("P-004", "Premium Device", 25, 0, 299.99m),
    new InventoryItem("P-005", "Basic Kit", 500, 0, 19.99m),
};

foreach (var product in sampleProducts)
{
    inventory[product.ProductId] = product;
    Console.WriteLine($"[Init] {product.ProductName}: {product.QuantityAvailable} units @ ${product.Price}");
}

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;

Console.WriteLine("\n[Setup] Topics will be auto-created");

// Create producer and consumer
await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "inventory-service";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe(Topics.InventoryUpdates);

// TODO: Implement stock reservation
// - Check availability for all items
// - Reduce available, increase reserved
// - Create reservation record with expiration
// - Return reservation ID

// TODO: Implement reservation release
// - Find reservation by ID or order ID
// - Increase available, decrease reserved
// - Remove reservation record

// TODO: Implement reservation commit
// - Find reservation by ID
// - Remove from reserved (already deducted from available)
// - Remove reservation record

// TODO: Implement low-stock alerts
// - After any stock change, check threshold
// - Emit alert if below threshold (e.g., 10 units)

Console.WriteLine("[Consumer] Starting inventory processor...");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        var result = await consumer.ConsumeAsync(cts.Token);
        if (result is null) continue;

        try
        {
            // TODO: Parse event type from message
            // Events: InventoryReserved, InventoryReleased, InventoryCommitted

            Console.WriteLine($"[Processing] Inventory event from partition {result.Partition}");

            // TODO: Handle InventoryReserved
            // - Already processed by order service, just log

            // TODO: Handle InventoryReleased
            // - Release reservation back to available stock

            // TODO: Handle InventoryCommitted
            // - Finalize the sale, remove reservation

            // TODO: Check for low stock alerts
            // foreach (var (productId, item) in inventory)
            // {
            //     if (item.QuantityAvailable < 10)
            //     {
            //         var alert = new LowStockAlert(productId, item.ProductName,
            //             item.QuantityAvailable, 10, DateTime.UtcNow);
            //         Console.WriteLine($"[ALERT] Low stock: {item.ProductName}");
            //     }
            // }
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

// Print final inventory state
Console.WriteLine("\n[Final] Inventory State:");
foreach (var (productId, item) in inventory.OrderBy(x => x.Key))
{
    Console.WriteLine($"  {item.ProductName}: {item.QuantityAvailable} available, {item.QuantityReserved} reserved");
}

Console.WriteLine("=== Inventory Service Stopped ===");