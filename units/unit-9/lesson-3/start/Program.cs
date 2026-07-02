// Surgewave Bootcamp - Unit 8, Lesson 3: Database Connectors
//
// In this lesson, you'll learn to:
// - Build database source connectors for data ingestion
// - Implement bulk and incremental load patterns
// - Create database sink connectors with batching
// - Handle upsert (insert/update) semantics
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 8.3 - Database Connectors ===");
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

// Create a simulated database with sample data
var simulatedDb = new SimulatedDatabase();
simulatedDb.CreateTable("orders", new[]
{
    new Dictionary<string, object> { ["id"] = 1, ["customer"] = "Alice", ["amount"] = 150.00, ["status"] = "completed" },
    new Dictionary<string, object> { ["id"] = 2, ["customer"] = "Bob", ["amount"] = 250.00, ["status"] = "pending" },
    new Dictionary<string, object> { ["id"] = 3, ["customer"] = "Charlie", ["amount"] = 75.50, ["status"] = "completed" },
    new Dictionary<string, object> { ["id"] = 4, ["customer"] = "Diana", ["amount"] = 320.00, ["status"] = "shipped" },
});

Console.WriteLine("Simulated database created with orders table:");
simulatedDb.PrintTable("orders");
Console.WriteLine();

// ============== Demo 1: Bulk Load Pattern ==============
Console.WriteLine("--- Demo 1: Bulk Load Pattern ---");

// TODO: Create a DatabaseSourceConnector
// Hint: var dbSource = new DatabaseSourceConnector(simulatedDb);

// TODO: Configure for bulk load mode
// Hint: dbSource.Configure(new Dictionary<string, string>
//       {
//           ["topic"] = "orders",
//           ["table"] = "orders",
//           ["mode"] = "bulk"
//       });

// TODO: Wire source to producer
// Hint: dbSource.OnRecord += (topic, key, value) => { ... };

// TODO: Start the source to produce all records
// Hint: await dbSource.StartAsync(cts.Token);

Console.WriteLine();

// ============== Demo 2: Incremental Load Pattern ==============
Console.WriteLine("--- Demo 2: Incremental Load Pattern ---");

// Add new rows to simulate new data arriving
// TODO: Add new orders to the database
// Hint: simulatedDb.InsertRow("orders", new Dictionary<string, object> { ... });

// TODO: Create a new source connector for incremental mode
// Hint: var incrementalSource = new DatabaseSourceConnector(simulatedDb);

// TODO: Configure for incremental mode with offset tracking
// Hint: ["mode"] = "incremental",
//       ["incrementing.column"] = "id",
//       ["start.offset"] = "4"

// TODO: Start the source - should only produce new records
// Hint: await incrementalSource.StartAsync(cts.Token);

Console.WriteLine();

// ============== Demo 3: Database Sink Connector ==============
Console.WriteLine("--- Demo 3: Database Sink Connector ---");

// TODO: Create a DatabaseSinkConnector
// Hint: var dbSink = new DatabaseSinkConnector(simulatedDb);

// TODO: Configure the sink with target table and batch size
// Hint: dbSink.Configure(new Dictionary<string, string>
//       {
//           ["table"] = "processed_orders",
//           ["batch.size"] = "2"
//       });

// TODO: Consume from orders topic and write to sink
// Hint: await dbSink.ProcessAsync(new[] { record });

// TODO: Flush remaining batch
// Hint: await dbSink.FlushAsync();

// TODO: Print the resulting table
// Hint: simulatedDb.PrintTable("processed_orders");

Console.WriteLine();

// ============== Demo 4: Upsert Semantics ==============
Console.WriteLine("--- Demo 4: Upsert Semantics ---");

// TODO: Create a sink with upsert mode enabled
// Hint: var upsertSink = new DatabaseSinkConnector(simulatedDb);

// TODO: Configure with upsert mode and key column
// Hint: ["mode"] = "upsert",
//       ["key.column"] = "id"

// TODO: Send records with both new and existing keys
// Hint: Show insert for new keys, update for existing keys

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Connector Interfaces ==============

public record ConnectorRecord(string Topic, string Key, byte[] Value, long Timestamp);

public interface ISourceConnector
{
    void Configure(Dictionary<string, string> config);
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
    event Action<string, string, byte[]>? OnRecord;
}

public interface ISinkConnector
{
    void Configure(Dictionary<string, string> config);
    Task ProcessAsync(IEnumerable<ConnectorRecord> records);
}

// ============== Simulated Database ==============

public class SimulatedDatabase
{
    private Dictionary<string, List<Dictionary<string, object>>> _tables = new();

    public void CreateTable(string name, IEnumerable<Dictionary<string, object>> rows)
    {
        _tables[name] = rows.ToList();
    }

    public void InsertRow(string table, Dictionary<string, object> row)
    {
        if (!_tables.ContainsKey(table))
            _tables[table] = new List<Dictionary<string, object>>();
        _tables[table].Add(row);
    }

    public List<Dictionary<string, object>> GetAllRows(string table)
    {
        return _tables.TryGetValue(table, out var rows) ? rows : new List<Dictionary<string, object>>();
    }

    public void PrintTable(string table)
    {
        if (!_tables.TryGetValue(table, out var rows))
        {
            Console.WriteLine($"  Table '{table}' not found");
            return;
        }

        Console.WriteLine($"  Table: {table} ({rows.Count} rows)");
        foreach (var row in rows)
        {
            var fields = row.Select(kv => $"{kv.Key}={kv.Value}");
            Console.WriteLine($"    {string.Join(", ", fields)}");
        }
    }
}

// TODO: Implement DatabaseSourceConnector
// Hint: Support bulk and incremental modes
// Hint: Track offset for incremental loads
// Hint: Convert rows to JSON records

// TODO: Implement DatabaseSinkConnector
// Hint: Batch records before writing
// Hint: Support insert and upsert modes
// Hint: Implement FlushAsync for remaining batch
