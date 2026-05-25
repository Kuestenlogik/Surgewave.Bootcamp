# Lesson 6.4: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Implement CQRS - Command Side

```csharp
// Command handler - writes to event stream
public class OrderCommandHandler
{
    private readonly IProducer<string, string> _producer;

    public async Task Handle(CreateOrderCommand cmd)
    {
        // Validate command
        if (cmd.Amount <= 0)
            throw new ValidationException("Amount must be positive");

        // Emit event
        var evt = new OrderCreatedEvent
        {
            OrderId = Guid.NewGuid().ToString(),
            CustomerId = cmd.CustomerId,
            Amount = cmd.Amount,
            CreatedAt = DateTime.UtcNow
        };

        await _producer.ProduceAsync("orders.events", evt.OrderId, Serialize(evt));
    }
}
```

## Step 2: Implement CQRS - Query Side

```csharp
// Event handler - builds read model
public class OrderReadModelBuilder
{
    private readonly Dictionary<string, OrderReadModel> _orders = new();

    public async Task HandleEvent(OrderCreatedEvent evt)
    {
        _orders[evt.OrderId] = new OrderReadModel
        {
            OrderId = evt.OrderId,
            CustomerId = evt.CustomerId,
            Amount = evt.Amount,
            Status = "Created",
            CreatedAt = evt.CreatedAt
        };
    }

    public async Task HandleEvent(OrderShippedEvent evt)
    {
        if (_orders.TryGetValue(evt.OrderId, out var order))
        {
            order.Status = "Shipped";
            order.ShippedAt = evt.ShippedAt;
        }
    }

    // Query method
    public OrderReadModel? GetOrder(string orderId) =>
        _orders.GetValueOrDefault(orderId);
}
```

## Step 3: Implement Event Sourcing

```csharp
// Event-sourced aggregate
public class BankAccount
{
    public string AccountId { get; private set; } = "";
    public decimal Balance { get; private set; }
    public List<object> PendingEvents { get; } = new();

    // Apply event to rebuild state
    public void Apply(object evt)
    {
        switch (evt)
        {
            case AccountCreated e:
                AccountId = e.AccountId;
                Balance = 0;
                break;
            case MoneyDeposited e:
                Balance += e.Amount;
                break;
            case MoneyWithdrawn e:
                Balance -= e.Amount;
                break;
        }
    }

    // Command handlers emit events
    public void Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Amount must be positive");

        var evt = new MoneyDeposited { AccountId = AccountId, Amount = amount };
        Apply(evt);
        PendingEvents.Add(evt);
    }

    public void Withdraw(decimal amount)
    {
        if (amount > Balance)
            throw new InvalidOperationException("Insufficient funds");

        var evt = new MoneyWithdrawn { AccountId = AccountId, Amount = amount };
        Apply(evt);
        PendingEvents.Add(evt);
    }

    // Rebuild from event stream
    public static BankAccount FromEvents(IEnumerable<object> events)
    {
        var account = new BankAccount();
        foreach (var evt in events)
            account.Apply(evt);
        return account;
    }
}
```

## Step 4: Implement Saga Pattern

```csharp
public class OrderSaga
{
    private readonly IProducer<string, string> _producer;
    private readonly Dictionary<string, SagaState> _states = new();

    public async Task Handle(OrderCreatedEvent evt)
    {
        _states[evt.OrderId] = new SagaState { Status = "AwaitingInventory" };

        // Step 1: Reserve inventory
        await _producer.ProduceAsync("inventory.commands",
            evt.OrderId,
            Serialize(new ReserveInventoryCommand { OrderId = evt.OrderId }));
    }

    public async Task Handle(InventoryReservedEvent evt)
    {
        _states[evt.OrderId].Status = "AwaitingPayment";

        // Step 2: Process payment
        await _producer.ProduceAsync("payment.commands",
            evt.OrderId,
            Serialize(new ProcessPaymentCommand { OrderId = evt.OrderId }));
    }

    public async Task Handle(PaymentCompletedEvent evt)
    {
        _states[evt.OrderId].Status = "Completed";

        // Step 3: Confirm order
        await _producer.ProduceAsync("orders.events",
            evt.OrderId,
            Serialize(new OrderConfirmedEvent { OrderId = evt.OrderId }));
    }

    // Compensation handlers
    public async Task Handle(PaymentFailedEvent evt)
    {
        _states[evt.OrderId].Status = "Compensating";

        // Rollback: Release inventory
        await _producer.ProduceAsync("inventory.commands",
            evt.OrderId,
            Serialize(new ReleaseInventoryCommand { OrderId = evt.OrderId }));

        // Cancel order
        await _producer.ProduceAsync("orders.events",
            evt.OrderId,
            Serialize(new OrderCancelledEvent { OrderId = evt.OrderId, Reason = "Payment failed" }));
    }
}
```

## Pattern Selection Guide

| Pattern | Use When | Complexity |
|---------|----------|------------|
| **CQRS** | Different read/write needs | Medium |
| **Event Sourcing** | Need full audit trail | High |
| **Saga** | Distributed transactions | High |
| **Simple Events** | Async communication | Low |

## Anti-Patterns

### Event Sourcing Anti-Patterns

```
❌ Storing derived data in events
❌ Large events (store IDs, not full objects)
❌ Business logic in event handlers
❌ Not versioning events

✅ Small, immutable events
✅ Domain logic in aggregates
✅ Event versioning from day 1
```

### Saga Anti-Patterns

```
❌ Synchronous sagas (blocking calls)
❌ No compensation logic
❌ Shared mutable state

✅ Async, event-driven
✅ Always handle failures
✅ Idempotent handlers
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

---

**Next:** → [Lesson 6.5: Capacity Planning](../lesson-5/README.md)
