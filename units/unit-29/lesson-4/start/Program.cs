// Surgewave Bootcamp - Unit 11, Lesson 4: Event Sourcing
//
// In this lesson, you'll implement event sourcing with:
// - An event store using Surgewave topics
// - Projections for read models
// - Event replay for state reconstruction
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

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

// ============== Step 1: Command Handler ==============
Console.WriteLine("--- Step 1: Processing Commands ---");

// TODO: Create a producer for the events topic
// Hint: await using var eventProducer = new SurgewaveProducer<byte[], byte[]>(options =>
// {
//     options.BootstrapServers = $"localhost:{runtime.Port}";
// });

// TODO: Implement CreateEvent helper function
// Hint: Event CreateEvent(string aggregateId, string eventType, object payload)
// {
//     var version = aggregateVersions.GetValueOrDefault(aggregateId, 0) + 1;
//     aggregateVersions[aggregateId] = version;
//     return new Event(
//         Guid.NewGuid().ToString()[..8],
//         aggregateId,
//         eventType,
//         JsonSerializer.Serialize(payload),
//         version,
//         DateTime.UtcNow);
// }

// TODO: Process commands and emit events
// Hint: Create accounts, deposit money, withdraw money, etc.
// var commands = new object[]
// {
//     new CreateAccount("ACC-001", "Alice", 1000m),
//     new CreateAccount("ACC-002", "Bob", 500m),
//     new Deposit("ACC-001", 250m, "Paycheck"),
//     new Withdraw("ACC-001", 100m, "Groceries"),
//     new Deposit("ACC-002", 300m, "Gift"),
//     new Withdraw("ACC-002", 50m, "Coffee"),
// };

Console.WriteLine("Commands processed!");
Console.WriteLine();

// ============== Step 2: Event Store ==============
Console.WriteLine("--- Step 2: Building Event Store ---");

// TODO: Create a consumer for the events topic
// TODO: Store events by aggregate ID

Console.WriteLine("Event store built!");
Console.WriteLine();

// ============== Step 3: Balance Projection ==============
Console.WriteLine("--- Step 3: Balance Projection ---");

// TODO: Create balance projection from events
// Hint: Apply each event to compute current balance per account

Console.WriteLine("Balance projection built!");
Console.WriteLine();

// ============== Step 4: History Projection ==============
Console.WriteLine("--- Step 4: History Projection ---");

// TODO: Create history projection from events
// Hint: Build transaction history per account

Console.WriteLine("History projection built!");
Console.WriteLine();

// ============== Step 5: Event Replay ==============
Console.WriteLine("--- Step 5: Event Replay ---");

// TODO: Implement GetBalanceAtTime function
// Hint: Replay events up to a specific timestamp

Console.WriteLine("Event replay complete!");
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
