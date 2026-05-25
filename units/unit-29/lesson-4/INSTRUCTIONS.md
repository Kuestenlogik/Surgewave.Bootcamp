# Lesson 11.4: Event Sourcing - Instructions

## Step 1: Define the Domain Models

Create event and command types for a banking domain:

```csharp
// Base event wrapper
public record Event(
    string EventId,
    string AggregateId,
    string EventType,
    string Payload,
    int Version,
    DateTime Timestamp);

// Domain events
public record AccountCreated(string AccountId, string Owner, decimal InitialBalance);
public record MoneyDeposited(string AccountId, decimal Amount, string Description);
public record MoneyWithdrawn(string AccountId, decimal Amount, string Description);
public record AccountClosed(string AccountId, string Reason);

// Commands
public record CreateAccount(string AccountId, string Owner, decimal InitialBalance);
public record Deposit(string AccountId, decimal Amount, string Description);
public record Withdraw(string AccountId, decimal Amount, string Description);
public record CloseAccount(string AccountId, string Reason);
```

## Step 2: Create the Command Handler

Process commands and emit events:

```csharp
// Track aggregate versions for optimistic concurrency
var aggregateVersions = new Dictionary<string, int>();

async Task HandleCommand(object command)
{
    Event? evt = command switch
    {
        CreateAccount cmd => CreateEvent(cmd.AccountId, "AccountCreated",
            new AccountCreated(cmd.AccountId, cmd.Owner, cmd.InitialBalance)),
        Deposit cmd => CreateEvent(cmd.AccountId, "MoneyDeposited",
            new MoneyDeposited(cmd.AccountId, cmd.Amount, cmd.Description)),
        Withdraw cmd => CreateEvent(cmd.AccountId, "MoneyWithdrawn",
            new MoneyWithdrawn(cmd.AccountId, cmd.Amount, cmd.Description)),
        CloseAccount cmd => CreateEvent(cmd.AccountId, "AccountClosed",
            new AccountClosed(cmd.AccountId, cmd.Reason)),
        _ => null
    };

    if (evt != null)
    {
        await eventProducer.ProduceAsync("events",
            Encoding.UTF8.GetBytes(evt.AggregateId),
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt)));
    }
}
```

## Step 3: Create the Event Store Consumer

Build the event store by consuming and indexing events:

```csharp
await using var eventConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "event-store";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

eventConsumer.Subscribe("events");

// Store events by aggregate ID
var eventStore = new Dictionary<string, List<Event>>();

var result = await eventConsumer.ConsumeAsync(timeout);
if (result != null)
{
    var evt = JsonSerializer.Deserialize<Event>(result.Value);
    if (!eventStore.ContainsKey(evt.AggregateId))
        eventStore[evt.AggregateId] = new List<Event>();
    eventStore[evt.AggregateId].Add(evt);
}
```

## Step 4: Create Balance Projection

Project current account balances:

```csharp
// Balance projection - maintains current balance per account
var balances = new Dictionary<string, decimal>();

foreach (var (accountId, events) in eventStore)
{
    decimal balance = 0;
    foreach (var evt in events.OrderBy(e => e.Version))
    {
        balance = evt.EventType switch
        {
            "AccountCreated" =>
                JsonSerializer.Deserialize<AccountCreated>(evt.Payload)!.InitialBalance,
            "MoneyDeposited" =>
                balance + JsonSerializer.Deserialize<MoneyDeposited>(evt.Payload)!.Amount,
            "MoneyWithdrawn" =>
                balance - JsonSerializer.Deserialize<MoneyWithdrawn>(evt.Payload)!.Amount,
            "AccountClosed" => 0,
            _ => balance
        };
    }
    balances[accountId] = balance;
}
```

## Step 5: Create History Projection

Project transaction history:

```csharp
// History projection - maintains transaction history per account
var histories = new Dictionary<string, List<string>>();

foreach (var (accountId, events) in eventStore)
{
    var history = new List<string>();
    foreach (var evt in events.OrderBy(e => e.Version))
    {
        var entry = evt.EventType switch
        {
            "AccountCreated" =>
                $"Account opened with ${JsonSerializer.Deserialize<AccountCreated>(evt.Payload)!.InitialBalance}",
            "MoneyDeposited" =>
                $"+${JsonSerializer.Deserialize<MoneyDeposited>(evt.Payload)!.Amount}",
            "MoneyWithdrawn" =>
                $"-${JsonSerializer.Deserialize<MoneyWithdrawn>(evt.Payload)!.Amount}",
            "AccountClosed" =>
                $"Account closed: {JsonSerializer.Deserialize<AccountClosed>(evt.Payload)!.Reason}",
            _ => null
        };
        if (entry != null) history.Add(entry);
    }
    histories[accountId] = history;
}
```

## Step 6: Implement Event Replay

Reconstruct state at a specific point in time:

```csharp
decimal GetBalanceAtTime(string accountId, DateTime pointInTime)
{
    if (!eventStore.TryGetValue(accountId, out var events))
        return 0;

    decimal balance = 0;
    foreach (var evt in events.Where(e => e.Timestamp <= pointInTime).OrderBy(e => e.Version))
    {
        balance = evt.EventType switch
        {
            "AccountCreated" =>
                JsonSerializer.Deserialize<AccountCreated>(evt.Payload)!.InitialBalance,
            "MoneyDeposited" =>
                balance + JsonSerializer.Deserialize<MoneyDeposited>(evt.Payload)!.Amount,
            "MoneyWithdrawn" =>
                balance - JsonSerializer.Deserialize<MoneyWithdrawn>(evt.Payload)!.Amount,
            _ => balance
        };
    }
    return balance;
}
```

## Exercises

1. **Basic Event Sourcing**: Complete the TODO items in `start/Program.cs`
2. **Optimistic Concurrency**: Add version checking to prevent conflicts
3. **Snapshots**: Implement periodic snapshots for faster replay
4. **Event Upcasting**: Handle event schema evolution
5. **CQRS**: Separate command and query responsibilities

## Testing Your Implementation

Run the completed solution:

```bash
cd completed
dotnet run
```

Expected output:
- Commands processed and events emitted
- Events stored in event store
- Balance projection showing current balances
- History projection showing transaction history
- Replay showing state at different points in time
