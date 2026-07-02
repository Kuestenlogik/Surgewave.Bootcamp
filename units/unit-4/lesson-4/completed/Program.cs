// Surgewave Bootcamp - Unit 6, Lesson 4: Event-Driven Patterns
// COMPLETED SOLUTION

using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

// ============== Events ==============
public record OrderCreated(string OrderId, string CustomerId, decimal Amount, DateTime CreatedAt);
public record OrderShipped(string OrderId, DateTime ShippedAt);
public record AccountCreated(string AccountId);
public record MoneyDeposited(string AccountId, decimal Amount);
public record MoneyWithdrawn(string AccountId, decimal Amount);

// ============== CQRS Read Model ==============
public class OrderReadModel
{
    public string OrderId { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
}

public class OrderReadModelProjection
{
    private readonly Dictionary<string, OrderReadModel> _orders = new();

    public void Apply(OrderCreated evt)
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

    public void Apply(OrderShipped evt)
    {
        if (_orders.TryGetValue(evt.OrderId, out var order))
        {
            order.Status = "Shipped";
            order.ShippedAt = evt.ShippedAt;
        }
    }

    public OrderReadModel? GetOrder(string orderId) =>
        _orders.GetValueOrDefault(orderId);

    public IEnumerable<OrderReadModel> GetOrdersByCustomer(string customerId) =>
        _orders.Values.Where(o => o.CustomerId == customerId);
}

// ============== Event Sourced Aggregate ==============
public class BankAccount
{
    public string AccountId { get; private set; } = "";
    public decimal Balance { get; private set; }
    private readonly List<object> _pendingEvents = new();

    public IReadOnlyList<object> PendingEvents => _pendingEvents;

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

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Amount must be positive");

        var evt = new MoneyDeposited(AccountId, amount);
        Apply(evt);
        _pendingEvents.Add(evt);
    }

    public void Withdraw(decimal amount)
    {
        if (amount > Balance)
            throw new InvalidOperationException("Insufficient funds");

        var evt = new MoneyWithdrawn(AccountId, amount);
        Apply(evt);
        _pendingEvents.Add(evt);
    }

    public static BankAccount Create(string accountId)
    {
        var account = new BankAccount();
        var evt = new AccountCreated(accountId);
        account.Apply(evt);
        account._pendingEvents.Add(evt);
        return account;
    }

    public static BankAccount FromEvents(IEnumerable<object> events)
    {
        var account = new BankAccount();
        foreach (var evt in events)
            account.Apply(evt);
        return account;
    }
}

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 6.4 - Event-Driven Patterns ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        // ============== CQRS Demo ==============
        Console.WriteLine("--- CQRS Pattern Demo ---");
        Console.WriteLine();

        var projection = new OrderReadModelProjection();

        // Simulate events arriving
        var events = new object[]
        {
            new OrderCreated("ORD-1", "CUST-A", 100m, DateTime.UtcNow.AddMinutes(-30)),
            new OrderCreated("ORD-2", "CUST-A", 250m, DateTime.UtcNow.AddMinutes(-20)),
            new OrderCreated("ORD-3", "CUST-B", 75m, DateTime.UtcNow.AddMinutes(-10)),
            new OrderShipped("ORD-1", DateTime.UtcNow.AddMinutes(-5)),
        };

        foreach (var evt in events)
        {
            switch (evt)
            {
                case OrderCreated oc: projection.Apply(oc); break;
                case OrderShipped os: projection.Apply(os); break;
            }
        }

        Console.WriteLine("Read Model Query: Orders for CUST-A");
        foreach (var order in projection.GetOrdersByCustomer("CUST-A"))
        {
            Console.WriteLine($"  {order.OrderId}: ${order.Amount} - {order.Status}");
        }
        Console.WriteLine();

        // ============== Event Sourcing Demo ==============
        Console.WriteLine("--- Event Sourcing Pattern Demo ---");
        Console.WriteLine();

        // Create account and perform operations
        var account = BankAccount.Create("ACC-001");
        account.Deposit(100);
        account.Deposit(50);
        account.Withdraw(30);

        Console.WriteLine("Event Stream:");
        foreach (var evt in account.PendingEvents)
        {
            Console.WriteLine($"  {evt}");
        }
        Console.WriteLine($"Current Balance: ${account.Balance}");
        Console.WriteLine();

        // Rebuild from events
        Console.WriteLine("Rebuilding state from events...");
        var rebuilt = BankAccount.FromEvents(account.PendingEvents);
        Console.WriteLine($"Rebuilt Balance: ${rebuilt.Balance}");
        Console.WriteLine();

        // ============== Saga Demo ==============
        Console.WriteLine("--- Saga Pattern Demo ---");
        Console.WriteLine();

        Console.WriteLine(@"
Order Saga Flow:

  ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
  │  Order Service  │     │Inventory Service│     │ Payment Service │
  └────────┬────────┘     └────────┬────────┘     └────────┬────────┘
           │                       │                       │
     CreateOrder                   │                       │
           │───OrderCreated───────►│                       │
           │                       │                       │
           │                  ReserveStock                 │
           │◄──InventoryReserved───│                       │
           │                       │                       │
           │───ProcessPayment─────────────────────────────►│
           │                       │                       │
           │◄──PaymentCompleted────────────────────────────│
           │                       │                       │
     ConfirmOrder                  │                       │
           │                       │                       │
           ▼                       ▼                       ▼

Compensation (if payment fails):
  → ReleaseInventory
  → CancelOrder
");

        // ============== Pattern Selection Guide ==============
        Console.WriteLine("--- When to Use Each Pattern ---");
        Console.WriteLine(@"
| Pattern         | Use When                              | Complexity |
|-----------------|---------------------------------------|------------|
| Simple Events   | Async decoupling                      | Low        |
| CQRS            | Different read/write requirements     | Medium     |
| Event Sourcing  | Need audit trail, temporal queries    | High       |
| Saga            | Distributed transactions              | High       |

Decision Flow:
  1. Start simple (just events)
  2. Add CQRS if queries become complex
  3. Add Event Sourcing if audit/replay needed
  4. Add Saga for multi-service transactions
");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
