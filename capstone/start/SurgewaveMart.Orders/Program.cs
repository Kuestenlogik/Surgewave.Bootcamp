// SurgewaveMart Order Service
// Handles order creation, validation, and lifecycle management

using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using SurgewaveMart.Shared;
using SurgewaveMart.Shared.Domain;
using SurgewaveMart.Shared.Events;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== SurgewaveMart Order Service ===\n");

// Start embedded broker (in production, connect to cluster)
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;
Console.WriteLine($"[Setup] Broker started at {bootstrapServers}");

// TODO: Create producer and consumer
await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "orders-service";
    opt.AutoOffsetReset = Kuestenlogik.Surgewave.Client.Consumer.AutoOffsetReset.Earliest;
});

// TODO: Implement order state store
// - Track order status through lifecycle
// - Store order details for querying

// TODO: Implement order validation
// - Validate non-empty items
// - Validate quantities > 0
// - Validate prices > 0

// TODO: Implement inventory check
// - Query inventory service/state store
// - Check availability for all items

// TODO: Implement order consumer
// - Consume from Topics.Orders
// - Validate order
// - Check inventory availability
// - Reserve stock (produce InventoryReserved event)
// - Produce to Topics.OrdersConfirmed or reject

Console.WriteLine("[Consumer] Starting order processor...");

consumer.Subscribe(Topics.Orders);

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
            // TODO: Deserialize OrderCreated event
            var orderEvent = JsonSerializer.Deserialize<OrderCreated>(result.Value ?? "");
            if (orderEvent is null)
            {
                Console.WriteLine($"[Error] Invalid order message, skipping");
                continue;
            }

            Console.WriteLine($"[Processing] Order {orderEvent.OrderId} from customer {orderEvent.CustomerId}");

            // TODO: Validate order
            // - Check items not empty
            // - Check quantities valid
            // - Check total matches items

            // TODO: Check inventory availability
            // - For each item, verify stock available
            // - If insufficient, reject order

            // TODO: Reserve inventory
            // - Create reservation with expiration
            // - Produce InventoryReserved event

            // TODO: Produce to orders-confirmed topic
            // var confirmed = new OrderConfirmed(orderEvent.OrderId, reservationId, DateTime.UtcNow);
            // await producer.ProduceAsync(Topics.OrdersConfirmed, orderEvent.OrderId, JsonSerializer.Serialize(confirmed));

            Console.WriteLine($"[Processed] Order {orderEvent.OrderId} confirmed");
        }
        catch (Exception ex)
        {
            // TODO: Send to DLQ
            Console.WriteLine($"[Error] Failed to process order: {ex.Message}");
        }
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("\n[Shutdown] Order service stopping...");
}

Console.WriteLine("=== Order Service Stopped ===");