// Surgewave Bootcamp - Unit 11, Lesson 5: Microservices Communication
// COMPLETED SOLUTION

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 11.5 - Microservices Communication ===");
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

// ============== State ==============
var sagaStates = new Dictionary<string, SagaState>();
var inventory = new Dictionary<string, int>
{
    ["PROD-001"] = 100,
    ["PROD-002"] = 50,
    ["PROD-003"] = 5 // Low stock - will cause some failures
};
var orders = new Dictionary<string, Order>();
var random = new Random(42);

// ============== Step 1: Setup Producers ==============
Console.WriteLine("--- Step 1: Setting Up Services ---");

await using var sagaEventProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

await using var inventoryCommandProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

await using var paymentCommandProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

await using var orderCommandProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

Console.WriteLine("  Producers created for all service topics");
Console.WriteLine();

// ============== Helper Functions ==============

async Task EmitSagaEvent<T>(T evt, string sagaId)
{
    var wrapper = new { Type = typeof(T).Name, Payload = JsonSerializer.Serialize(evt) };
    await sagaEventProducer.ProduceAsync("saga-events",
        Encoding.UTF8.GetBytes(sagaId),
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(wrapper)));
}

async Task SendInventoryCommand<T>(T cmd, string sagaId)
{
    var wrapper = new { Type = typeof(T).Name, Payload = JsonSerializer.Serialize(cmd) };
    await inventoryCommandProducer.ProduceAsync("inventory-commands",
        Encoding.UTF8.GetBytes(sagaId),
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(wrapper)));
}

async Task SendPaymentCommand<T>(T cmd, string sagaId)
{
    var wrapper = new { Type = typeof(T).Name, Payload = JsonSerializer.Serialize(cmd) };
    await paymentCommandProducer.ProduceAsync("payment-commands",
        Encoding.UTF8.GetBytes(sagaId),
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(wrapper)));
}

// ============== Step 2: Saga Orchestrator ==============
Console.WriteLine("--- Step 2: Starting Sagas ---");

async Task StartSaga(Order order)
{
    var sagaId = $"SAGA-{Guid.NewGuid().ToString()[..8]}";
    var state = new SagaState(
        sagaId,
        order.OrderId,
        "STARTED",
        new List<string>(),
        null,
        DateTime.UtcNow,
        null);

    sagaStates[sagaId] = state;
    orders[order.OrderId] = order;

    Console.WriteLine($"  [SAGA] Started {sagaId} for order {order.OrderId}");

    // Emit saga started event
    await EmitSagaEvent(new SagaStarted(sagaId, order.OrderId, DateTime.UtcNow), sagaId);

    // Send first command: reserve inventory
    await SendInventoryCommand(new ReserveInventory(sagaId, order.OrderId, order.Items), sagaId);
}

// Create and start test orders
var testOrders = new[]
{
    new Order("ORD-001", "CUST-001",
        new List<OrderItem> { new("PROD-001", 2, 49.99m) }, 99.98m),
    new Order("ORD-002", "CUST-002",
        new List<OrderItem> { new("PROD-002", 1, 29.99m), new("PROD-001", 1, 49.99m) }, 79.98m),
    new Order("ORD-003", "CUST-003",
        new List<OrderItem> { new("PROD-003", 10, 9.99m) }, 99.90m), // Will fail - not enough stock
    new Order("ORD-004", "CUST-004",
        new List<OrderItem> { new("PROD-001", 5, 49.99m) }, 249.95m),
};

foreach (var order in testOrders)
{
    await StartSaga(order);
}

Console.WriteLine($"  Started {testOrders.Length} sagas");
Console.WriteLine();

// ============== Step 3: Inventory Service ==============
Console.WriteLine("--- Step 3: Processing Inventory Commands ---");

await using var inventoryConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "inventory-service";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

inventoryConsumer.Subscribe("inventory-commands");

var timeout = TimeSpan.FromMilliseconds(500);
var deadline = DateTime.UtcNow.AddSeconds(3);
var inventoryCommandsProcessed = 0;

while (DateTime.UtcNow < deadline)
{
    var result = await inventoryConsumer.ConsumeAsync(timeout);
    if (result?.Value != null)
    {
        var wrapper = JsonSerializer.Deserialize<JsonElement>(result.Value);
        var type = wrapper.GetProperty("Type").GetString();
        var payload = wrapper.GetProperty("Payload").GetString()!;

        switch (type)
        {
            case "ReserveInventory":
                var reserveCmd = JsonSerializer.Deserialize<ReserveInventory>(payload)!;
                var canReserve = reserveCmd.Items.All(item =>
                    inventory.GetValueOrDefault(item.ProductId, 0) >= item.Quantity);

                if (canReserve)
                {
                    foreach (var item in reserveCmd.Items)
                        inventory[item.ProductId] -= item.Quantity;
                    Console.WriteLine($"  [INVENTORY] Reserved for {reserveCmd.OrderId}");
                    await EmitSagaEvent(new InventoryReserved(reserveCmd.SagaId, reserveCmd.OrderId), reserveCmd.SagaId);
                }
                else
                {
                    Console.WriteLine($"  [INVENTORY] Failed for {reserveCmd.OrderId} - insufficient stock");
                    await EmitSagaEvent(new InventoryReservationFailed(reserveCmd.SagaId, reserveCmd.OrderId,
                        "Insufficient stock"), reserveCmd.SagaId);
                }
                break;

            case "ReleaseInventory":
                var releaseCmd = JsonSerializer.Deserialize<ReleaseInventory>(payload)!;
                foreach (var item in releaseCmd.Items)
                    inventory[item.ProductId] = inventory.GetValueOrDefault(item.ProductId, 0) + item.Quantity;
                Console.WriteLine($"  [INVENTORY] Released for {releaseCmd.OrderId}");
                break;
        }
        inventoryCommandsProcessed++;
    }
}

Console.WriteLine($"  Processed {inventoryCommandsProcessed} inventory commands");
Console.WriteLine();

// ============== Step 4: Payment Service ==============
Console.WriteLine("--- Step 4: Processing Payment Commands ---");

await using var paymentConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "payment-service";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

paymentConsumer.Subscribe("payment-commands");

deadline = DateTime.UtcNow.AddSeconds(3);
var paymentCommandsProcessed = 0;

while (DateTime.UtcNow < deadline)
{
    var result = await paymentConsumer.ConsumeAsync(timeout);
    if (result?.Value != null)
    {
        var wrapper = JsonSerializer.Deserialize<JsonElement>(result.Value);
        var type = wrapper.GetProperty("Type").GetString();
        var payload = wrapper.GetProperty("Payload").GetString()!;

        switch (type)
        {
            case "ProcessPayment":
                var payCmd = JsonSerializer.Deserialize<ProcessPayment>(payload)!;
                // Simulate payment processing (20% failure rate for demo)
                var success = random.NextDouble() > 0.2;

                if (success)
                {
                    Console.WriteLine($"  [PAYMENT] Processed ${payCmd.Amount} for {payCmd.OrderId}");
                    await EmitSagaEvent(new PaymentProcessed(payCmd.SagaId, payCmd.OrderId), payCmd.SagaId);
                }
                else
                {
                    Console.WriteLine($"  [PAYMENT] Failed for {payCmd.OrderId} - declined");
                    await EmitSagaEvent(new PaymentFailed(payCmd.SagaId, payCmd.OrderId, "Payment declined"), payCmd.SagaId);
                }
                break;

            case "RefundPayment":
                var refundCmd = JsonSerializer.Deserialize<RefundPayment>(payload)!;
                Console.WriteLine($"  [PAYMENT] Refunded ${refundCmd.Amount} for {refundCmd.OrderId}");
                break;
        }
        paymentCommandsProcessed++;
    }
}

Console.WriteLine($"  Processed {paymentCommandsProcessed} payment commands");
Console.WriteLine();

// ============== Step 5: Saga Event Handler ==============
Console.WriteLine("--- Step 5: Processing Saga Events ---");

await using var sagaEventConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "saga-orchestrator";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

sagaEventConsumer.Subscribe("saga-events");

deadline = DateTime.UtcNow.AddSeconds(5);
var sagaEventsProcessed = 0;

while (DateTime.UtcNow < deadline)
{
    var result = await sagaEventConsumer.ConsumeAsync(timeout);
    if (result?.Value != null)
    {
        var wrapper = JsonSerializer.Deserialize<JsonElement>(result.Value);
        var type = wrapper.GetProperty("Type").GetString();
        var payload = wrapper.GetProperty("Payload").GetString()!;

        switch (type)
        {
            case "SagaStarted":
                // Already handled in StartSaga
                break;

            case "InventoryReserved":
                var invReserved = JsonSerializer.Deserialize<InventoryReserved>(payload)!;
                if (sagaStates.TryGetValue(invReserved.SagaId, out var state1))
                {
                    state1.CompletedSteps.Add("ReserveInventory");
                    sagaStates[invReserved.SagaId] = state1 with { Status = "INVENTORY_RESERVED" };

                    // Next step: process payment
                    if (orders.TryGetValue(invReserved.OrderId, out var order))
                    {
                        await SendPaymentCommand(
                            new ProcessPayment(invReserved.SagaId, order.OrderId, order.CustomerId, order.Total),
                            invReserved.SagaId);
                    }
                }
                break;

            case "InventoryReservationFailed":
                var invFailed = JsonSerializer.Deserialize<InventoryReservationFailed>(payload)!;
                if (sagaStates.TryGetValue(invFailed.SagaId, out var state2))
                {
                    sagaStates[invFailed.SagaId] = state2 with
                    {
                        Status = "FAILED",
                        FailureReason = invFailed.Reason,
                        CompletedAt = DateTime.UtcNow
                    };
                    Console.WriteLine($"  [SAGA] {invFailed.SagaId} failed: {invFailed.Reason}");
                }
                break;

            case "PaymentProcessed":
                var payProcessed = JsonSerializer.Deserialize<PaymentProcessed>(payload)!;
                if (sagaStates.TryGetValue(payProcessed.SagaId, out var state3))
                {
                    state3.CompletedSteps.Add("ProcessPayment");
                    sagaStates[payProcessed.SagaId] = state3 with
                    {
                        Status = "COMPLETED",
                        CompletedAt = DateTime.UtcNow
                    };
                    Console.WriteLine($"  [SAGA] {payProcessed.SagaId} completed successfully");
                }
                break;

            case "PaymentFailed":
                var payFailed = JsonSerializer.Deserialize<PaymentFailed>(payload)!;
                if (sagaStates.TryGetValue(payFailed.SagaId, out var state4) && orders.TryGetValue(payFailed.OrderId, out var failedOrder))
                {
                    // Compensate: release inventory
                    Console.WriteLine($"  [SAGA] {payFailed.SagaId} compensating - payment failed");
                    await SendInventoryCommand(
                        new ReleaseInventory(payFailed.SagaId, payFailed.OrderId, failedOrder.Items),
                        payFailed.SagaId);

                    sagaStates[payFailed.SagaId] = state4 with
                    {
                        Status = "COMPENSATED",
                        FailureReason = payFailed.Reason,
                        CompletedAt = DateTime.UtcNow
                    };
                }
                break;
        }
        sagaEventsProcessed++;
    }
}

Console.WriteLine($"  Processed {sagaEventsProcessed} saga events");
Console.WriteLine();

// ============== Step 6: Results ==============
Console.WriteLine("--- Step 6: Saga Results ---");
Console.WriteLine();
Console.WriteLine("=== SAGA EXECUTION SUMMARY ===");
Console.WriteLine("+------------+----------+--------------+----------------------------+");
Console.WriteLine("| Saga ID    | Order    | Status       | Details                    |");
Console.WriteLine("+------------+----------+--------------+----------------------------+");

foreach (var (sagaId, state) in sagaStates.OrderBy(kvp => kvp.Key))
{
    var details = state.Status switch
    {
        "COMPLETED" => $"Steps: {string.Join(" -> ", state.CompletedSteps)}",
        "FAILED" => state.FailureReason ?? "Unknown",
        "COMPENSATED" => $"Rolled back: {state.FailureReason}",
        _ => state.Status
    };

    if (details.Length > 26) details = details[..23] + "...";

    Console.WriteLine($"| {sagaId,-10} | {state.OrderId,-8} | {state.Status,-12} | {details,-26} |");
}
Console.WriteLine("+------------+----------+--------------+----------------------------+");

// Summary statistics
var completed = sagaStates.Values.Count(s => s.Status == "COMPLETED");
var failed = sagaStates.Values.Count(s => s.Status == "FAILED");
var compensated = sagaStates.Values.Count(s => s.Status == "COMPENSATED");

Console.WriteLine();
Console.WriteLine("Statistics:");
Console.WriteLine($"  Completed:   {completed}");
Console.WriteLine($"  Failed:      {failed}");
Console.WriteLine($"  Compensated: {compensated}");
Console.WriteLine($"  Total:       {sagaStates.Count}");

Console.WriteLine();
Console.WriteLine("Final Inventory State:");
foreach (var (productId, quantity) in inventory.OrderBy(kvp => kvp.Key))
{
    Console.WriteLine($"  {productId}: {quantity} units");
}

Console.WriteLine();
Console.WriteLine("Microservices Communication complete!");

// ============== Domain Models ==============

// Saga state
public record SagaState(
    string SagaId,
    string OrderId,
    string Status,
    List<string> CompletedSteps,
    string? FailureReason,
    DateTime StartedAt,
    DateTime? CompletedAt);

// Order types
public record OrderItem(string ProductId, int Quantity, decimal Price);
public record Order(string OrderId, string CustomerId, List<OrderItem> Items, decimal Total);

// Commands
public record CreateOrderSaga(string SagaId, Order Order);
public record ReserveInventory(string SagaId, string OrderId, List<OrderItem> Items);
public record ProcessPayment(string SagaId, string OrderId, string CustomerId, decimal Amount);
public record ConfirmOrder(string SagaId, string OrderId);
public record ReleaseInventory(string SagaId, string OrderId, List<OrderItem> Items);
public record RefundPayment(string SagaId, string OrderId, decimal Amount);
public record CancelOrder(string SagaId, string OrderId, string Reason);

// Events
public record SagaStarted(string SagaId, string OrderId, DateTime Timestamp);
public record InventoryReserved(string SagaId, string OrderId);
public record InventoryReservationFailed(string SagaId, string OrderId, string Reason);
public record PaymentProcessed(string SagaId, string OrderId);
public record PaymentFailed(string SagaId, string OrderId, string Reason);
public record OrderConfirmed(string SagaId, string OrderId);
public record SagaCompleted(string SagaId, string OrderId, bool Success, string? Reason);
