// Surgewave Bootcamp - Unit 11, Lesson 4: Event Sourcing
// COMPLETED SOLUTION

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 11.4 - Event Sourcing ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// ============== State ==============
var aggregateVersions = new Dictionary<string, int>();
var eventStore = new Dictionary<string, List<Event>>();
var eventTimestamps = new List<DateTime>(); // Track timestamps for replay demo

// ============== Step 1: Command Handler ==============
Console.WriteLine("--- Step 1: Processing Commands ---");

await using var eventProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

// Helper to create events
Event CreateEvent(string aggregateId, string eventType, object payload)
{
    var version = aggregateVersions.GetValueOrDefault(aggregateId, 0) + 1;
    aggregateVersions[aggregateId] = version;
    var timestamp = DateTime.UtcNow;
    eventTimestamps.Add(timestamp);
    return new Event(
        Guid.NewGuid().ToString()[..8],
        aggregateId,
        eventType,
        JsonSerializer.Serialize(payload),
        version,
        timestamp);
}

// Process commands
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
        Console.WriteLine($"  Event: {evt.EventType} for {evt.AggregateId} (v{evt.Version})");
    }
}

// Execute commands
var commands = new object[]
{
    new CreateAccount("ACC-001", "Alice", 1000m),
    new CreateAccount("ACC-002", "Bob", 500m),
    new Deposit("ACC-001", 250m, "Paycheck"),
    new Withdraw("ACC-001", 100m, "Groceries"),
    new Deposit("ACC-002", 300m, "Gift"),
    new Withdraw("ACC-002", 50m, "Coffee"),
    new Deposit("ACC-001", 500m, "Bonus"),
    new Withdraw("ACC-001", 200m, "Rent"),
    new CloseAccount("ACC-002", "Account consolidation")
};

foreach (var command in commands)
{
    await HandleCommand(command);
    await Task.Delay(50); // Small delay for distinct timestamps
}

Console.WriteLine($"  Processed {commands.Length} commands");
Console.WriteLine();

// ============== Step 2: Event Store ==============
Console.WriteLine("--- Step 2: Building Event Store ---");

await using var storeConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "event-store";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

storeConsumer.Subscribe("events");

var timeout = TimeSpan.FromMilliseconds(500);
var deadline = DateTime.UtcNow.AddSeconds(3);

while (DateTime.UtcNow < deadline)
{
    var result = await storeConsumer.ConsumeAsync(timeout);
    if (result?.Value != null)
    {
        var evt = JsonSerializer.Deserialize<Event>(result.Value)!;
        if (!eventStore.ContainsKey(evt.AggregateId))
            eventStore[evt.AggregateId] = new List<Event>();
        eventStore[evt.AggregateId].Add(evt);
    }
}

var totalEvents = eventStore.Values.Sum(list => list.Count);
Console.WriteLine($"  Stored {totalEvents} events for {eventStore.Count} aggregates");
Console.WriteLine();

// ============== Step 3: Balance Projection ==============
Console.WriteLine("--- Step 3: Balance Projection ---");

var balances = new Dictionary<string, (decimal Balance, string Owner, bool IsOpen)>();

foreach (var (accountId, events) in eventStore)
{
    decimal balance = 0;
    string owner = "";
    bool isOpen = true;

    foreach (var evt in events.OrderBy(e => e.Version))
    {
        switch (evt.EventType)
        {
            case "AccountCreated":
                var created = JsonSerializer.Deserialize<AccountCreated>(evt.Payload)!;
                balance = created.InitialBalance;
                owner = created.Owner;
                break;
            case "MoneyDeposited":
                var deposit = JsonSerializer.Deserialize<MoneyDeposited>(evt.Payload)!;
                balance += deposit.Amount;
                break;
            case "MoneyWithdrawn":
                var withdraw = JsonSerializer.Deserialize<MoneyWithdrawn>(evt.Payload)!;
                balance -= withdraw.Amount;
                break;
            case "AccountClosed":
                balance = 0;
                isOpen = false;
                break;
        }
    }

    balances[accountId] = (balance, owner, isOpen);
}

Console.WriteLine("  Current Account Balances:");
Console.WriteLine("  +----------+-----------+------------+--------+");
Console.WriteLine("  | Account  | Owner     | Balance    | Status |");
Console.WriteLine("  +----------+-----------+------------+--------+");
foreach (var (accountId, (balance, owner, isOpen)) in balances.OrderBy(kvp => kvp.Key))
{
    var status = isOpen ? "Open" : "Closed";
    Console.WriteLine($"  | {accountId,-8} | {owner,-9} | ${balance,9:F2} | {status,-6} |");
}
Console.WriteLine("  +----------+-----------+------------+--------+");
Console.WriteLine();

// ============== Step 4: History Projection ==============
Console.WriteLine("--- Step 4: History Projection ---");

foreach (var (accountId, events) in eventStore.OrderBy(kvp => kvp.Key))
{
    Console.WriteLine($"  Transaction History for {accountId}:");
    Console.WriteLine("  +-----+-------------------+------------------+------------+");
    Console.WriteLine("  | Ver | Event             | Description      | Amount     |");
    Console.WriteLine("  +-----+-------------------+------------------+------------+");

    foreach (var evt in events.OrderBy(e => e.Version))
    {
        var (description, amount) = evt.EventType switch
        {
            "AccountCreated" => (
                "Account opened",
                $"+${JsonSerializer.Deserialize<AccountCreated>(evt.Payload)!.InitialBalance:F2}"),
            "MoneyDeposited" => (
                JsonSerializer.Deserialize<MoneyDeposited>(evt.Payload)!.Description,
                $"+${JsonSerializer.Deserialize<MoneyDeposited>(evt.Payload)!.Amount:F2}"),
            "MoneyWithdrawn" => (
                JsonSerializer.Deserialize<MoneyWithdrawn>(evt.Payload)!.Description,
                $"-${JsonSerializer.Deserialize<MoneyWithdrawn>(evt.Payload)!.Amount:F2}"),
            "AccountClosed" => (
                JsonSerializer.Deserialize<AccountClosed>(evt.Payload)!.Reason,
                "N/A"),
            _ => ("Unknown", "N/A")
        };

        Console.WriteLine($"  | {evt.Version,3} | {evt.EventType,-17} | {description,-16} | {amount,10} |");
    }
    Console.WriteLine("  +-----+-------------------+------------------+------------+");
    Console.WriteLine();
}

// ============== Step 5: Event Replay ==============
Console.WriteLine("--- Step 5: Event Replay ---");

// Function to get balance at a specific point in time
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
            "AccountClosed" => 0,
            _ => balance
        };
    }
    return balance;
}

// Demo: Show balance evolution for ACC-001
Console.WriteLine("  Balance Evolution for ACC-001:");
if (eventStore.TryGetValue("ACC-001", out var acc001Events))
{
    Console.WriteLine("  +----------------------------+------------+");
    Console.WriteLine("  | Point in Time              | Balance    |");
    Console.WriteLine("  +----------------------------+------------+");

    foreach (var evt in acc001Events.OrderBy(e => e.Version))
    {
        var balance = GetBalanceAtTime("ACC-001", evt.Timestamp);
        Console.WriteLine($"  | After v{evt.Version}: {evt.EventType,-16} | ${balance,9:F2} |");
    }
    Console.WriteLine("  +----------------------------+------------+");
}

Console.WriteLine();

// Demo: Replay to reconstruct state after event 3
Console.WriteLine("  Replay Demo: State after first 3 events per account:");
foreach (var (accountId, events) in eventStore.OrderBy(kvp => kvp.Key))
{
    var eventsToReplay = events.OrderBy(e => e.Version).Take(3).ToList();
    if (eventsToReplay.Count > 0)
    {
        var replayTime = eventsToReplay.Last().Timestamp;
        var balance = GetBalanceAtTime(accountId, replayTime);
        Console.WriteLine($"    {accountId}: ${balance:F2} (after {eventsToReplay.Count} events)");
    }
}

Console.WriteLine();
Console.WriteLine("Event Sourcing complete!");

// ============== Domain Models ==============

// Event wrapper
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
