// Surgewave Bootcamp - Unit 8, Lesson 4: Change Data Capture
//
// In this lesson, you'll learn to:
// - Understand Change Data Capture (CDC) concepts
// - Build CDC event generators and connectors
// - Handle INSERT, UPDATE, DELETE operations
// - Build materialized views from CDC events
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 8.4 - Change Data Capture ===");
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

// ============== Demo 1: CDC Event Structure ==============
Console.WriteLine("--- Demo 1: CDC Event Structure ---");

// TODO: Create a CdcEvent for an INSERT operation
// Hint: var insertEvent = new CdcEvent
//       {
//           Operation = "INSERT",
//           Table = "users",
//           Before = null,
//           After = new Dictionary<string, object> { ["id"] = 1, ["name"] = "Alice" },
//           Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
//           Position = 1
//       };

// TODO: Create a CdcEvent for an UPDATE operation
// Hint: Include both Before and After states

// TODO: Create a CdcEvent for a DELETE operation
// Hint: Before has the row, After is null

Console.WriteLine();

// ============== Demo 2: CDC Event Generator ==============
Console.WriteLine("--- Demo 2: CDC Event Generator ---");

// TODO: Create a CdcEventGenerator
// Hint: var generator = new CdcEventGenerator();

// TODO: Generate a series of changes
// Hint: generator.Insert("products", new Dictionary<string, object> { ... });
//       generator.Update("products", oldRow, newRow);
//       generator.Delete("products", row);

// TODO: Print the change log
// Hint: foreach (var evt in generator.GetChangeLog()) { ... }

Console.WriteLine();

// ============== Demo 3: CDC Source Connector ==============
Console.WriteLine("--- Demo 3: CDC Source Connector ---");

// TODO: Create a CdcSourceConnector
// Hint: var cdcSource = new CdcSourceConnector(generator);

// TODO: Configure the connector
// Hint: cdcSource.Configure(new Dictionary<string, string>
//       {
//           ["topic"] = "cdc-products",
//           ["start.position"] = "0"
//       });

// TODO: Wire to producer and stream events
// Hint: cdcSource.OnRecord += (topic, key, value) => { ... };

Console.WriteLine();

// ============== Demo 4: CDC Consumer ==============
Console.WriteLine("--- Demo 4: CDC Consumer ---");

// TODO: Create a consumer for CDC events
// Hint: Use SurgewaveConsumer with consumer group

// TODO: Process CDC events and print them
// Hint: Deserialize to CdcEvent, handle each operation type

Console.WriteLine();

// ============== Demo 5: Materialized View ==============
Console.WriteLine("--- Demo 5: Materialized View ---");

// TODO: Create a MaterializedView
// Hint: var view = new MaterializedView();

// TODO: Apply CDC events to build current state
// Hint: view.ApplyEvent(evt);

// TODO: Query the materialized view
// Hint: view.GetAll() returns current state of all rows

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== CDC Event Structure ==============

public class CdcEvent
{
    [JsonPropertyName("operation")]
    public string Operation { get; set; } = "";

    [JsonPropertyName("table")]
    public string Table { get; set; } = "";

    [JsonPropertyName("before")]
    public Dictionary<string, object>? Before { get; set; }

    [JsonPropertyName("after")]
    public Dictionary<string, object>? After { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("position")]
    public long Position { get; set; }
}

// ============== Connector Interfaces ==============

public record ConnectorRecord(string Topic, string Key, byte[] Value, long Timestamp);

public interface ISourceConnector
{
    void Configure(Dictionary<string, string> config);
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
    event Action<string, string, byte[]>? OnRecord;
}

// TODO: Implement CdcEventGenerator
// Hint: Track position, generate INSERT/UPDATE/DELETE events
// Hint: Maintain a change log that can be queried by position

// TODO: Implement CdcSourceConnector
// Hint: Read from generator's change log
// Hint: Support start position for resumability
// Hint: Use table:id as event key

// TODO: Implement MaterializedView
// Hint: Maintain Dictionary<string, Dictionary<string, object>>
// Hint: Apply INSERT/UPDATE to add/update rows
// Hint: Apply DELETE to remove rows
