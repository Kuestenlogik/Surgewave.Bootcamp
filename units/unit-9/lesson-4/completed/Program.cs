// Surgewave Bootcamp - Unit 8, Lesson 4: Change Data Capture
// COMPLETED SOLUTION

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

var insertEvent = new CdcEvent
{
    Operation = "INSERT",
    Table = "users",
    Before = null,
    After = new Dictionary<string, object> { ["id"] = 1L, ["name"] = "Alice", ["email"] = "alice@example.com" },
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    Position = 1
};
Console.WriteLine($"  INSERT event: {JsonSerializer.Serialize(insertEvent)}");

var updateEvent = new CdcEvent
{
    Operation = "UPDATE",
    Table = "users",
    Before = new Dictionary<string, object> { ["id"] = 1L, ["name"] = "Alice", ["email"] = "alice@example.com" },
    After = new Dictionary<string, object> { ["id"] = 1L, ["name"] = "Alice Smith", ["email"] = "alice.smith@example.com" },
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    Position = 2
};
Console.WriteLine($"  UPDATE event: {JsonSerializer.Serialize(updateEvent)}");

var deleteEvent = new CdcEvent
{
    Operation = "DELETE",
    Table = "users",
    Before = new Dictionary<string, object> { ["id"] = 1L, ["name"] = "Alice Smith", ["email"] = "alice.smith@example.com" },
    After = null,
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    Position = 3
};
Console.WriteLine($"  DELETE event: {JsonSerializer.Serialize(deleteEvent)}");
Console.WriteLine();

// ============== Demo 2: CDC Event Generator ==============
Console.WriteLine("--- Demo 2: CDC Event Generator ---");

var generator = new CdcEventGenerator();

// Simulate a series of database changes
generator.Insert("products", new Dictionary<string, object>
{
    ["id"] = 101L, ["name"] = "Widget", ["price"] = 29.99, ["stock"] = 100L
});

generator.Insert("products", new Dictionary<string, object>
{
    ["id"] = 102L, ["name"] = "Gadget", ["price"] = 49.99, ["stock"] = 50L
});

generator.Update("products",
    before: new Dictionary<string, object> { ["id"] = 101L, ["name"] = "Widget", ["price"] = 29.99, ["stock"] = 100L },
    after: new Dictionary<string, object> { ["id"] = 101L, ["name"] = "Widget Pro", ["price"] = 39.99, ["stock"] = 100L }
);

generator.Update("products",
    before: new Dictionary<string, object> { ["id"] = 102L, ["name"] = "Gadget", ["price"] = 49.99, ["stock"] = 50L },
    after: new Dictionary<string, object> { ["id"] = 102L, ["name"] = "Gadget", ["price"] = 49.99, ["stock"] = 45L }
);

generator.Insert("products", new Dictionary<string, object>
{
    ["id"] = 103L, ["name"] = "Gizmo", ["price"] = 19.99, ["stock"] = 200L
});

generator.Delete("products", new Dictionary<string, object>
{
    ["id"] = 103L, ["name"] = "Gizmo", ["price"] = 19.99, ["stock"] = 200L
});

Console.WriteLine("  Generated change log:");
foreach (var evt in generator.GetChangeLog())
{
    var detail = evt.Operation switch
    {
        "INSERT" => $"After: {FormatRow(evt.After)}",
        "UPDATE" => $"Before: {FormatRow(evt.Before)} -> After: {FormatRow(evt.After)}",
        "DELETE" => $"Before: {FormatRow(evt.Before)}",
        _ => ""
    };
    Console.WriteLine($"    [{evt.Position}] {evt.Operation} {evt.Table}: {detail}");
}
Console.WriteLine();

// ============== Demo 3: CDC Source Connector ==============
Console.WriteLine("--- Demo 3: CDC Source Connector ---");

var cdcSource = new CdcSourceConnector(generator);
cdcSource.Configure(new Dictionary<string, string>
{
    ["topic"] = "cdc-products",
    ["start.position"] = "0"
});

await using var producer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var eventsProduced = 0;
cdcSource.OnRecord += (topic, key, value) =>
{
    producer.ProduceAsync(topic, Encoding.UTF8.GetBytes(key), value).GetAwaiter().GetResult();
    eventsProduced++;
    Console.WriteLine($"  CDC -> Surgewave: key={key}");
};

using var cdcCts = new CancellationTokenSource(2000);
try
{
    await cdcSource.StartAsync(cdcCts.Token);
}
catch (OperationCanceledException) { }

Console.WriteLine($"  Produced {eventsProduced} CDC events");
Console.WriteLine();

// ============== Demo 4: CDC Consumer ==============
Console.WriteLine("--- Demo 4: CDC Consumer ---");

await using var consumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "cdc-consumer-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe("cdc-products");

var cdcProcessor = new CdcProcessor();
var eventsConsumed = 0;
var consumeTimeout = DateTime.UtcNow.AddSeconds(2);

Console.WriteLine("  Processing CDC events:");
while (DateTime.UtcNow < consumeTimeout && eventsConsumed < eventsProduced)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
    if (result != null)
    {
        var evt = JsonSerializer.Deserialize<CdcEvent>(result.Value);
        if (evt != null)
        {
            cdcProcessor.ProcessEvent(evt);
            eventsConsumed++;
        }
    }
}
Console.WriteLine($"  Processed {eventsConsumed} CDC events");
Console.WriteLine();

// ============== Demo 5: Materialized View ==============
Console.WriteLine("--- Demo 5: Materialized View ---");

// Replay events to build materialized view
var view = new MaterializedView();

Console.WriteLine("  Building materialized view from CDC events...");
foreach (var evt in generator.GetChangeLog())
{
    view.ApplyEvent(evt);
    Console.WriteLine($"    Applied {evt.Operation} for {evt.Table}:{GetKey(evt)}");
}

Console.WriteLine();
Console.WriteLine("  Current state of materialized view:");
foreach (var row in view.GetAll())
{
    Console.WriteLine($"    {FormatRow(row)}");
}

Console.WriteLine();
Console.WriteLine("  Query specific row (id=101):");
var product101 = view.Get("products:101");
if (product101 != null)
{
    Console.WriteLine($"    Found: {FormatRow(product101)}");
}

Console.WriteLine();
Console.WriteLine("  Query deleted row (id=103):");
var product103 = view.Get("products:103");
if (product103 == null)
{
    Console.WriteLine("    Not found (was deleted)");
}
Console.WriteLine();

// ============== Demo 6: Incremental CDC ==============
Console.WriteLine("--- Demo 6: Incremental CDC (Resume from Position) ---");

// Add more changes
generator.Insert("products", new Dictionary<string, object>
{
    ["id"] = 104L, ["name"] = "NewProduct", ["price"] = 99.99, ["stock"] = 25L
});

// Create new source starting from last position
var incrementalSource = new CdcSourceConnector(generator);
incrementalSource.Configure(new Dictionary<string, string>
{
    ["topic"] = "cdc-products-incremental",
    ["start.position"] = eventsProduced.ToString() // Start after previous events
});

var newEventsProduced = 0;
incrementalSource.OnRecord += (topic, key, value) =>
{
    newEventsProduced++;
    var evt = JsonSerializer.Deserialize<CdcEvent>(value);
    Console.WriteLine($"  New CDC event: {evt?.Operation} key={key}");
};

using var incCts = new CancellationTokenSource(1000);
try
{
    await incrementalSource.StartAsync(incCts.Token);
}
catch (OperationCanceledException) { }

Console.WriteLine($"  Produced {newEventsProduced} incremental CDC events");
Console.WriteLine();

// ============== Demo 7: PostgreSQL CDC Configuration ==============
Console.WriteLine("--- Demo 7: PostgreSQL CDC Source Connector Configuration ---");
Console.WriteLine();
Console.WriteLine("  Production PostgreSQL CDC connector configuration:");
Console.WriteLine();

// Show how to configure a real PostgreSQL CDC source connector
// using Kuestenlogik.Surgewave.Connector.PostgreSql
var postgresCdcConfig = new Dictionary<string, string>
{
    // Connection settings
    ["postgresql.connection"] = "Host=localhost;Database=orders;Username=cdc_user;Password=secret",

    // Replication slot configuration
    ["postgresql.slot.name"] = "orders_slot",
    ["postgresql.publication.name"] = "orders_pub",

    // Tables to capture (comma-separated)
    ["postgresql.tables"] = "public.orders,public.order_items",

    // Auto-create slot and publication if they don't exist
    ["postgresql.create.slot"] = "true",
    ["postgresql.create.publication"] = "true",

    // Topic configuration
    ["topic.prefix"] = "cdc.",
    ["topic.pattern"] = "${schema}.${table}",

    // CDC options
    ["snapshot.mode"] = "initial",    // initial, never, always
    ["include.schema"] = "true",
    ["include.before.values"] = "true", // Requires REPLICA IDENTITY FULL

    // Performance tuning
    ["poll.interval.ms"] = "100",
    ["batch.max.records"] = "500"
};

Console.WriteLine("  PostgreSqlConnectorConfig:");
foreach (var kvp in postgresCdcConfig)
{
    Console.WriteLine($"    {kvp.Key} = {kvp.Value}");
}

Console.WriteLine();
Console.WriteLine("  PostgreSQL prerequisites:");
Console.WriteLine("    1. wal_level = logical in postgresql.conf");
Console.WriteLine("    2. max_replication_slots >= 1");
Console.WriteLine("    3. CREATE USER cdc_user WITH REPLICATION PASSWORD 'secret'");
Console.WriteLine("    4. GRANT SELECT ON ALL TABLES IN SCHEMA public TO cdc_user");
Console.WriteLine("    5. ALTER TABLE orders REPLICA IDENTITY FULL (for before values)");
Console.WriteLine();

// Simulated CDC event from PostgreSQL
var pgCdcEvent = new Dictionary<string, object?>
{
    ["op"] = "c",  // c=create, u=update, d=delete, r=read(snapshot)
    ["source"] = new Dictionary<string, object>
    {
        ["schema"] = "public",
        ["table"] = "orders",
        ["lsn"] = "0/16B3740"
    },
    ["before"] = null,
    ["after"] = new Dictionary<string, object>
    {
        ["id"] = 1001,
        ["customer_id"] = 42,
        ["amount"] = 299.99,
        ["status"] = "pending"
    },
    ["ts_ms"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};

Console.WriteLine("  Sample PostgreSQL CDC event:");
Console.WriteLine($"    {JsonSerializer.Serialize(pgCdcEvent, new JsonSerializerOptions { WriteIndented = true })}");
Console.WriteLine();

// Summary
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. CDC captures INSERT, UPDATE, DELETE operations");
Console.WriteLine("2. Events contain before/after state for full context");
Console.WriteLine("3. Position tracking enables resumable streaming");
Console.WriteLine("4. Materialized views can be built from CDC events");
Console.WriteLine("5. CDC enables real-time data synchronization");
Console.WriteLine("6. PostgreSQL CDC uses logical replication (wal_level=logical)");
Console.WriteLine("7. Use REPLICA IDENTITY FULL for UPDATE/DELETE before values");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Helper function
static string FormatRow(Dictionary<string, object>? row)
{
    if (row == null) return "null";
    return string.Join(", ", row.Select(kv => $"{kv.Key}={kv.Value}"));
}

static string GetKey(CdcEvent evt)
{
    var row = evt.After ?? evt.Before;
    if (row != null && row.TryGetValue("id", out var id))
        return id.ToString() ?? "unknown";
    return "unknown";
}

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

// ============== CDC Event Generator ==============

public class CdcEventGenerator
{
    private readonly List<CdcEvent> _changeLog = new();
    private long _position = 0;

    public void Insert(string table, Dictionary<string, object> row)
    {
        _changeLog.Add(new CdcEvent
        {
            Operation = "INSERT",
            Table = table,
            Before = null,
            After = row,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Position = ++_position
        });
    }

    public void Update(string table, Dictionary<string, object> before, Dictionary<string, object> after)
    {
        _changeLog.Add(new CdcEvent
        {
            Operation = "UPDATE",
            Table = table,
            Before = before,
            After = after,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Position = ++_position
        });
    }

    public void Delete(string table, Dictionary<string, object> row)
    {
        _changeLog.Add(new CdcEvent
        {
            Operation = "DELETE",
            Table = table,
            Before = row,
            After = null,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Position = ++_position
        });
    }

    public IEnumerable<CdcEvent> GetChangeLog()
    {
        return _changeLog;
    }

    public IEnumerable<CdcEvent> GetChangesAfter(long position)
    {
        return _changeLog.Where(e => e.Position > position);
    }

    public long CurrentPosition => _position;
}

// ============== CDC Source Connector ==============

public class CdcSourceConnector : ISourceConnector
{
    private readonly CdcEventGenerator _generator;
    private string _topic = "cdc-events";
    private long _startPosition = 0;
    private CancellationTokenSource? _cts;

    public event Action<string, string, byte[]>? OnRecord;

    public CdcSourceConnector(CdcEventGenerator generator)
    {
        _generator = generator;
    }

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("topic", out var topic))
            _topic = topic;
        if (config.TryGetValue("start.position", out var pos))
            _startPosition = long.Parse(pos);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var changes = _generator.GetChangesAfter(_startPosition);

        foreach (var change in changes)
        {
            if (_cts.Token.IsCancellationRequested)
                break;

            var key = GetEventKey(change);
            var value = JsonSerializer.SerializeToUtf8Bytes(change);

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

    private string GetEventKey(CdcEvent evt)
    {
        var row = evt.After ?? evt.Before;
        if (row != null && row.TryGetValue("id", out var id))
            return $"{evt.Table}:{id}";
        return $"{evt.Table}:unknown";
    }

    public Task StopAsync()
    {
        _cts?.Cancel();
        return Task.CompletedTask;
    }
}

// ============== CDC Processor ==============

public class CdcProcessor
{
    public void ProcessEvent(CdcEvent evt)
    {
        switch (evt.Operation)
        {
            case "INSERT":
                Console.WriteLine($"    INSERT into {evt.Table}: {FormatRow(evt.After)}");
                break;

            case "UPDATE":
                Console.WriteLine($"    UPDATE {evt.Table}:");
                Console.WriteLine($"      Before: {FormatRow(evt.Before)}");
                Console.WriteLine($"      After: {FormatRow(evt.After)}");
                break;

            case "DELETE":
                Console.WriteLine($"    DELETE from {evt.Table}: {FormatRow(evt.Before)}");
                break;
        }
    }

    private string FormatRow(Dictionary<string, object>? row)
    {
        if (row == null) return "null";
        return string.Join(", ", row.Select(kv => $"{kv.Key}={kv.Value}"));
    }
}

// ============== Materialized View ==============

public class MaterializedView
{
    private readonly Dictionary<string, Dictionary<string, object>> _state = new();

    public void ApplyEvent(CdcEvent evt)
    {
        var key = GetKey(evt);

        switch (evt.Operation)
        {
            case "INSERT":
            case "UPDATE":
                if (evt.After != null)
                    _state[key] = evt.After;
                break;

            case "DELETE":
                _state.Remove(key);
                break;
        }
    }

    private string GetKey(CdcEvent evt)
    {
        var row = evt.After ?? evt.Before;
        if (row != null && row.TryGetValue("id", out var id))
            return $"{evt.Table}:{id}";
        return $"{evt.Table}:unknown";
    }

    public Dictionary<string, object>? Get(string key)
    {
        return _state.TryGetValue(key, out var row) ? row : null;
    }

    public IEnumerable<Dictionary<string, object>> GetAll()
    {
        return _state.Values;
    }

    public int Count => _state.Count;
}
