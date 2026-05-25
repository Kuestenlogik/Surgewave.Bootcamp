# Lesson 11.5: Microservices Communication - Instructions

## Step 1: Define the Domain Models

Create saga and service message types:

```csharp
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
```

## Step 2: Create the Saga Orchestrator

Manage saga state and coordinate services:

```csharp
var sagaStates = new Dictionary<string, SagaState>();

async Task StartSaga(Order order)
{
    var sagaId = $"SAGA-{Guid.NewGuid().ToString()[..8]}";
    var state = new SagaState(sagaId, order.OrderId, "STARTED",
        new List<string>(), null, DateTime.UtcNow, null);
    sagaStates[sagaId] = state;

    // Emit saga started event
    await EmitEvent(new SagaStarted(sagaId, order.OrderId, DateTime.UtcNow));

    // Send first command: reserve inventory
    await SendCommand("inventory-commands",
        new ReserveInventory(sagaId, order.OrderId, order.Items));
}

async Task HandleSagaEvent(object evt)
{
    switch (evt)
    {
        case InventoryReserved e:
            await UpdateSaga(e.SagaId, "INVENTORY_RESERVED", "ReserveInventory");
            await SendCommand("payment-commands", new ProcessPayment(...));
            break;
        case InventoryReservationFailed e:
            await CompensateSaga(e.SagaId, e.Reason);
            break;
        // ... handle other events
    }
}
```

## Step 3: Create the Inventory Service

Handle inventory commands:

```csharp
var inventory = new Dictionary<string, int>
{
    ["PROD-001"] = 100,
    ["PROD-002"] = 50,
    ["PROD-003"] = 5 // Low stock - will fail some orders
};

async Task HandleInventoryCommand(object command)
{
    switch (command)
    {
        case ReserveInventory cmd:
            // Check if all items are available
            var canReserve = cmd.Items.All(item =>
                inventory.GetValueOrDefault(item.ProductId, 0) >= item.Quantity);

            if (canReserve)
            {
                foreach (var item in cmd.Items)
                    inventory[item.ProductId] -= item.Quantity;
                await EmitEvent(new InventoryReserved(cmd.SagaId, cmd.OrderId));
            }
            else
            {
                await EmitEvent(new InventoryReservationFailed(cmd.SagaId, cmd.OrderId,
                    "Insufficient stock"));
            }
            break;

        case ReleaseInventory cmd:
            // Compensating action: release reserved inventory
            foreach (var item in cmd.Items)
                inventory[item.ProductId] += item.Quantity;
            Console.WriteLine($"  [INVENTORY] Released inventory for {cmd.OrderId}");
            break;
    }
}
```

## Step 4: Create the Payment Service

Handle payment commands:

```csharp
async Task HandlePaymentCommand(object command)
{
    switch (command)
    {
        case ProcessPayment cmd:
            // Simulate payment processing (10% failure rate)
            var success = new Random().NextDouble() > 0.1;

            if (success)
            {
                await EmitEvent(new PaymentProcessed(cmd.SagaId, cmd.OrderId));
            }
            else
            {
                await EmitEvent(new PaymentFailed(cmd.SagaId, cmd.OrderId,
                    "Payment declined"));
            }
            break;

        case RefundPayment cmd:
            // Compensating action: refund payment
            Console.WriteLine($"  [PAYMENT] Refunded ${cmd.Amount} for {cmd.OrderId}");
            break;
    }
}
```

## Step 5: Implement Compensation Logic

Handle saga failures with rollback:

```csharp
async Task CompensateSaga(string sagaId, string reason)
{
    if (!sagaStates.TryGetValue(sagaId, out var state))
        return;

    Console.WriteLine($"  [SAGA] Compensating {sagaId}: {reason}");

    // Update state to compensating
    sagaStates[sagaId] = state with { Status = "COMPENSATING", FailureReason = reason };

    // Execute compensations in reverse order
    foreach (var step in state.CompletedSteps.AsEnumerable().Reverse())
    {
        switch (step)
        {
            case "ProcessPayment":
                await SendCommand("payment-commands", new RefundPayment(sagaId, state.OrderId, 0));
                break;
            case "ReserveInventory":
                await SendCommand("inventory-commands", new ReleaseInventory(sagaId, state.OrderId, ...));
                break;
        }
    }

    // Mark saga as failed
    sagaStates[sagaId] = state with { Status = "FAILED", CompletedAt = DateTime.UtcNow };
    await EmitEvent(new SagaCompleted(sagaId, state.OrderId, false, reason));
}
```

## Step 6: Run the Complete System

Orchestrate services and process orders:

```csharp
// Create test orders
var orders = new[]
{
    new Order("ORD-001", "CUST-001", items1, 299.99m),
    new Order("ORD-002", "CUST-002", items2, 149.99m), // May fail payment
    new Order("ORD-003", "CUST-003", items3, 59.99m),  // May fail inventory
};

// Start sagas
foreach (var order in orders)
    await StartSaga(order);

// Process all services in parallel
await Task.WhenAll(
    ProcessInventoryCommands(),
    ProcessPaymentCommands(),
    ProcessSagaEvents()
);

// Display final results
DisplaySagaResults();
```

## Exercises

1. **Basic Saga**: Complete the TODO items in `start/Program.cs`
2. **Timeout Handling**: Add saga timeout with automatic compensation
3. **Retry Logic**: Implement retries for transient failures
4. **Parallel Steps**: Execute independent saga steps in parallel
5. **Saga History**: Track full saga execution history for debugging

## Testing Your Implementation

Run the completed solution:

```bash
cd completed
dotnet run
```

Expected output:
- Multiple sagas started
- Services processing commands
- Some sagas completing successfully
- Some sagas compensating due to failures
- Final saga state summary
