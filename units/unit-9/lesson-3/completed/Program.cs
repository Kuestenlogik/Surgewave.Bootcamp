// Surgewave Bootcamp - Unit 8, Lesson 3: Database Connectors
// COMPLETED SOLUTION

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
    new Dictionary<string, object> { ["id"] = 1L, ["customer"] = "Alice", ["amount"] = 150.00, ["status"] = "completed" },
    new Dictionary<string, object> { ["id"] = 2L, ["customer"] = "Bob", ["amount"] = 250.00, ["status"] = "pending" },
    new Dictionary<string, object> { ["id"] = 3L, ["customer"] = "Charlie", ["amount"] = 75.50, ["status"] = "completed" },
    new Dictionary<string, object> { ["id"] = 4L, ["customer"] = "Diana", ["amount"] = 320.00, ["status"] = "shipped" },
});

Console.WriteLine("Simulated database created with orders table:");
simulatedDb.PrintTable("orders");
Console.WriteLine();

// ============== Demo 1: Bulk Load Pattern ==============
Console.WriteLine("--- Demo 1: Bulk Load Pattern ---");

var bulkSource = new DatabaseSourceConnector(simulatedDb);
bulkSource.Configure(new Dictionary<string, string>
{
    ["topic"] = "orders-bulk",
    ["table"] = "orders",
    ["mode"] = "bulk"
});

await using var bulkProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var bulkRecordsProduced = 0;
bulkSource.OnRecord += (topic, key, value) =>
{
    bulkProducer.ProduceAsync(topic, Encoding.UTF8.GetBytes(key), value).GetAwaiter().GetResult();
    bulkRecordsProduced++;
    var json = Encoding.UTF8.GetString(value);
    Console.WriteLine($"  Bulk Load: key={key}, value={json}");
};

using var bulkCts = new CancellationTokenSource(2000);
try
{
    await bulkSource.StartAsync(bulkCts.Token);
}
catch (OperationCanceledException) { }

Console.WriteLine($"  Produced {bulkRecordsProduced} records via bulk load");
Console.WriteLine();

// ============== Demo 2: Incremental Load Pattern ==============
Console.WriteLine("--- Demo 2: Incremental Load Pattern ---");

// Add new rows to simulate new data arriving
Console.WriteLine("  Adding new orders to database...");
simulatedDb.InsertRow("orders", new Dictionary<string, object>
{
    ["id"] = 5L, ["customer"] = "Eve", ["amount"] = 180.00, ["status"] = "pending"
});
simulatedDb.InsertRow("orders", new Dictionary<string, object>
{
    ["id"] = 6L, ["customer"] = "Frank", ["amount"] = 95.00, ["status"] = "completed"
});

var incrementalSource = new DatabaseSourceConnector(simulatedDb);
incrementalSource.Configure(new Dictionary<string, string>
{
    ["topic"] = "orders-incremental",
    ["table"] = "orders",
    ["mode"] = "incremental",
    ["incrementing.column"] = "id",
    ["start.offset"] = "4" // Start after existing records
});

await using var incProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var incRecordsProduced = 0;
incrementalSource.OnRecord += (topic, key, value) =>
{
    incProducer.ProduceAsync(topic, Encoding.UTF8.GetBytes(key), value).GetAwaiter().GetResult();
    incRecordsProduced++;
    var json = Encoding.UTF8.GetString(value);
    Console.WriteLine($"  Incremental Load: key={key}, value={json}");
};

using var incCts = new CancellationTokenSource(2000);
try
{
    await incrementalSource.StartAsync(incCts.Token);
}
catch (OperationCanceledException) { }

Console.WriteLine($"  Produced {incRecordsProduced} new records via incremental load");
Console.WriteLine($"  Current offset: {incrementalSource.CurrentOffset}");
Console.WriteLine();

// ============== Demo 3: Database Sink Connector ==============
Console.WriteLine("--- Demo 3: Database Sink Connector ---");

simulatedDb.CreateTable("processed_orders", Array.Empty<Dictionary<string, object>>());

var dbSink = new DatabaseSinkConnector(simulatedDb);
dbSink.Configure(new Dictionary<string, string>
{
    ["table"] = "processed_orders",
    ["batch.size"] = "2",
    ["mode"] = "insert"
});

await using var sinkConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "db-sink-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

sinkConsumer.Subscribe("orders-bulk");

var sinkRecordsConsumed = 0;
var sinkConsumeTimeout = DateTime.UtcNow.AddSeconds(2);

while (DateTime.UtcNow < sinkConsumeTimeout && sinkRecordsConsumed < bulkRecordsProduced)
{
    var result = await sinkConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
    if (result != null)
    {
        var record = new ConnectorRecord(
            result.Topic,
            Encoding.UTF8.GetString(result.Key ?? Array.Empty<byte>()),
            result.Value ?? Array.Empty<byte>(),
            result.Timestamp.ToUnixTimeMilliseconds()
        );

        await dbSink.ProcessAsync(new[] { record });
        sinkRecordsConsumed++;
    }
}

// Flush remaining batch
await dbSink.FlushAsync();

Console.WriteLine($"  Consumed {sinkRecordsConsumed} records to database sink");
Console.WriteLine("  Resulting table:");
simulatedDb.PrintTable("processed_orders");
Console.WriteLine();

// ============== Demo 4: Upsert Semantics ==============
Console.WriteLine("--- Demo 4: Upsert Semantics ---");

simulatedDb.CreateTable("upsert_orders", new[]
{
    new Dictionary<string, object> { ["id"] = 1L, ["customer"] = "Alice", ["amount"] = 150.00, ["status"] = "completed" },
    new Dictionary<string, object> { ["id"] = 2L, ["customer"] = "Bob", ["amount"] = 250.00, ["status"] = "pending" },
});

Console.WriteLine("  Initial upsert_orders table:");
simulatedDb.PrintTable("upsert_orders");

var upsertSink = new DatabaseSinkConnector(simulatedDb);
upsertSink.Configure(new Dictionary<string, string>
{
    ["table"] = "upsert_orders",
    ["batch.size"] = "10",
    ["mode"] = "upsert",
    ["key.column"] = "id"
});

// Simulate records: update existing (id=1), insert new (id=10)
var upsertRecords = new[]
{
    new ConnectorRecord("test", "1", JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
    {
        ["id"] = 1L, ["customer"] = "Alice Updated", ["amount"] = 175.00, ["status"] = "shipped"
    }), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
    new ConnectorRecord("test", "10", JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
    {
        ["id"] = 10L, ["customer"] = "New Customer", ["amount"] = 500.00, ["status"] = "pending"
    }), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
};

Console.WriteLine();
Console.WriteLine("  Processing upsert records...");
await upsertSink.ProcessAsync(upsertRecords);
await upsertSink.FlushAsync();

Console.WriteLine();
Console.WriteLine("  Table after upsert:");
simulatedDb.PrintTable("upsert_orders");
Console.WriteLine();

// ============== Demo 5: MongoDB Source Connector Configuration ==============
Console.WriteLine("--- Demo 5: MongoDB Source Connector Configuration ---");
Console.WriteLine();
Console.WriteLine("  Production MongoDB connector configuration:");
Console.WriteLine();

// Show how to configure a real MongoDB source connector
// using Kuestenlogik.Surgewave.Connector.MongoDB
var mongoSourceConfig = new Dictionary<string, string>
{
    // Connection settings
    ["mongodb.connection"] = "mongodb://localhost:27017",
    ["mongodb.database"] = "shop",
    ["mongodb.collection"] = "products",

    // Source mode: change_stream (CDC) or poll (periodic queries)
    ["source.mode"] = "change_stream",

    // Topic configuration
    ["topic.prefix"] = "mongo",
    ["topic.pattern"] = "${database}.${collection}",

    // Change stream options (for CDC mode)
    ["change.stream.full.document"] = "updateLookup",

    // Polling options (for poll mode, if not using change streams)
    ["poll.field"] = "_id",
    ["poll.interval.ms"] = "1000",

    // Batch configuration
    ["batch.max.records"] = "500"
};

Console.WriteLine("  MongoDbConnectorConfig (Source):");
foreach (var kvp in mongoSourceConfig)
{
    Console.WriteLine($"    {kvp.Key} = {kvp.Value}");
}

Console.WriteLine();
Console.WriteLine("  MongoDB Change Stream modes:");
Console.WriteLine("    - change_stream: Real-time CDC using MongoDB Change Streams (requires replica set)");
Console.WriteLine("    - poll: Periodic queries for new/updated documents");
Console.WriteLine();

// ============== Demo 6: MongoDB Sink Connector Configuration ==============
Console.WriteLine("--- Demo 6: MongoDB Sink Connector Configuration ---");
Console.WriteLine();

var mongoSinkConfig = new Dictionary<string, string>
{
    // Connection settings
    ["mongodb.connection"] = "mongodb://localhost:27017",
    ["mongodb.database"] = "analytics",
    ["mongodb.collection"] = "processed_events",
    ["topics"] = "events,metrics",

    // Write mode: insert, upsert, or replace
    ["write.mode"] = "upsert",

    // Document ID strategy: auto, key, or field
    ["document.id.strategy"] = "key",
    ["document.id.field"] = "_id",

    // Write concern: w1, majority, or unacknowledged
    ["write.concern"] = "majority",

    // Batching
    ["batch.size"] = "100",

    // Retry configuration
    ["retry.max"] = "3",
    ["retry.backoff.ms"] = "1000"
};

Console.WriteLine("  MongoDbConnectorConfig (Sink):");
foreach (var kvp in mongoSinkConfig)
{
    Console.WriteLine($"    {kvp.Key} = {kvp.Value}");
}

Console.WriteLine();
Console.WriteLine("  MongoDB CDC event format:");

var mongoCdcEvent = new Dictionary<string, object?>
{
    ["op"] = "c",  // c=create, u=update, r=replace, d=delete
    ["ns"] = new { db = "shop", coll = "products" },
    ["documentKey"] = "{\"_id\": {\"$oid\": \"507f1f77bcf86cd799439011\"}}",
    ["fullDocument"] = "{\"_id\": ..., \"name\": \"Widget\", \"price\": 29.99}",
    ["updateDescription"] = (object?)null,
    ["ts_ms"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};

Console.WriteLine($"    {JsonSerializer.Serialize(mongoCdcEvent)}");
Console.WriteLine();

// Summary
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Bulk load: Query all rows for initial migration");
Console.WriteLine("2. Incremental load: Track offset, query only new rows");
Console.WriteLine("3. Sink batching: Improve performance with batch writes");
Console.WriteLine("4. Upsert: Insert new rows, update existing by key");
Console.WriteLine("5. Offset tracking: Remember position for resumability");
Console.WriteLine("6. MongoDB Change Streams: Real-time CDC (requires replica set)");
Console.WriteLine("7. Write concerns: Use 'majority' for data consistency");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Connector Interfaces and Implementations ==============

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

    public void UpsertRow(string table, string keyColumn, Dictionary<string, object> row)
    {
        if (!_tables.ContainsKey(table))
            _tables[table] = new List<Dictionary<string, object>>();

        if (!row.TryGetValue(keyColumn, out var keyValue))
        {
            _tables[table].Add(row);
            Console.WriteLine($"    INSERT: {FormatRow(row)}");
            return;
        }

        var existingIndex = _tables[table].FindIndex(r =>
            r.TryGetValue(keyColumn, out var v) && v.ToString() == keyValue.ToString());

        if (existingIndex >= 0)
        {
            _tables[table][existingIndex] = row;
            Console.WriteLine($"    UPDATE: key={keyValue}, {FormatRow(row)}");
        }
        else
        {
            _tables[table].Add(row);
            Console.WriteLine($"    INSERT: key={keyValue}, {FormatRow(row)}");
        }
    }

    private string FormatRow(Dictionary<string, object> row)
    {
        return string.Join(", ", row.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    public List<Dictionary<string, object>> GetAllRows(string table)
    {
        return _tables.TryGetValue(table, out var rows) ? rows.ToList() : new List<Dictionary<string, object>>();
    }

    public List<Dictionary<string, object>> GetRowsWhere(string table, string column, long greaterThan)
    {
        if (!_tables.TryGetValue(table, out var rows))
            return new List<Dictionary<string, object>>();

        return rows.Where(r =>
        {
            if (r.TryGetValue(column, out var value))
            {
                var longVal = Convert.ToInt64(value);
                return longVal > greaterThan;
            }
            return false;
        }).ToList();
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

// ============== Database Source Connector ==============

public class DatabaseSourceConnector : ISourceConnector
{
    private readonly SimulatedDatabase _db;
    private string _topic = "db-data";
    private string _table = "records";
    private string _mode = "bulk";
    private string _incrementingColumn = "id";
    private long _offset = 0;
    private CancellationTokenSource? _cts;

    public event Action<string, string, byte[]>? OnRecord;
    public long CurrentOffset => _offset;

    public DatabaseSourceConnector(SimulatedDatabase db)
    {
        _db = db;
    }

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("topic", out var topic))
            _topic = topic;
        if (config.TryGetValue("table", out var table))
            _table = table;
        if (config.TryGetValue("mode", out var mode))
            _mode = mode;
        if (config.TryGetValue("incrementing.column", out var col))
            _incrementingColumn = col;
        if (config.TryGetValue("start.offset", out var offset))
            _offset = long.Parse(offset);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        List<Dictionary<string, object>> rows;

        if (_mode == "incremental")
        {
            rows = _db.GetRowsWhere(_table, _incrementingColumn, _offset);
        }
        else
        {
            rows = _db.GetAllRows(_table);
        }

        foreach (var row in rows)
        {
            if (_cts.Token.IsCancellationRequested)
                break;

            // Track offset for incremental mode
            if (_mode == "incremental" && row.TryGetValue(_incrementingColumn, out var idVal))
            {
                var id = Convert.ToInt64(idVal);
                if (id > _offset)
                    _offset = id;
            }

            // Use first column or id as key
            var key = row.TryGetValue("id", out var keyVal) ? keyVal.ToString()! : Guid.NewGuid().ToString();
            var value = JsonSerializer.SerializeToUtf8Bytes(row);

            OnRecord?.Invoke(_topic, key, value);

            try
            {
                await Task.Delay(50, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public Task StopAsync()
    {
        _cts?.Cancel();
        return Task.CompletedTask;
    }
}

// ============== Database Sink Connector ==============

public class DatabaseSinkConnector : ISinkConnector
{
    private readonly SimulatedDatabase _db;
    private string _table = "output";
    private string _mode = "insert";
    private string _keyColumn = "id";
    private int _batchSize = 100;
    private List<Dictionary<string, object>> _batch = new();

    public DatabaseSinkConnector(SimulatedDatabase db)
    {
        _db = db;
    }

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("table", out var table))
            _table = table;
        if (config.TryGetValue("mode", out var mode))
            _mode = mode;
        if (config.TryGetValue("key.column", out var keyCol))
            _keyColumn = keyCol;
        if (config.TryGetValue("batch.size", out var size))
            _batchSize = int.Parse(size);
    }

    public async Task ProcessAsync(IEnumerable<ConnectorRecord> records)
    {
        foreach (var record in records)
        {
            try
            {
                var row = JsonSerializer.Deserialize<Dictionary<string, object>>(record.Value);
                if (row != null)
                {
                    // Convert JsonElement values to proper types
                    var convertedRow = new Dictionary<string, object>();
                    foreach (var kv in row)
                    {
                        if (kv.Value is JsonElement je)
                        {
                            convertedRow[kv.Key] = je.ValueKind switch
                            {
                                JsonValueKind.Number => je.TryGetInt64(out var l) ? l : je.GetDouble(),
                                JsonValueKind.String => je.GetString() ?? "",
                                JsonValueKind.True => true,
                                JsonValueKind.False => false,
                                _ => je.ToString()
                            };
                        }
                        else
                        {
                            convertedRow[kv.Key] = kv.Value;
                        }
                    }

                    _batch.Add(convertedRow);

                    if (_batch.Count >= _batchSize)
                    {
                        await FlushAsync();
                    }
                }
            }
            catch (JsonException)
            {
                // Skip invalid JSON
            }
        }
    }

    public Task FlushAsync()
    {
        if (_batch.Count == 0)
            return Task.CompletedTask;

        Console.WriteLine($"  Flushing batch of {_batch.Count} records to {_table}...");

        foreach (var row in _batch)
        {
            if (_mode == "upsert")
            {
                _db.UpsertRow(_table, _keyColumn, row);
            }
            else
            {
                _db.InsertRow(_table, row);
            }
        }

        _batch.Clear();
        return Task.CompletedTask;
    }
}
