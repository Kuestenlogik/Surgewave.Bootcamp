# Lesson 8.4: Change Data Capture - Instructions

## Step 1: Understand CDC Event Structure

CDC events capture the full change context:

```csharp
public class CdcEvent
{
    public string Operation { get; set; } = "";  // INSERT, UPDATE, DELETE
    public string Table { get; set; } = "";
    public Dictionary<string, object>? Before { get; set; }
    public Dictionary<string, object>? After { get; set; }
    public long Timestamp { get; set; }
    public long Position { get; set; }
}
```

## Step 2: Build CDC Event Generator

Simulate database change log:

```csharp
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
}
```

## Step 3: Create CDC Source Connector

Stream changes from the log:

```csharp
public class CdcSourceConnector : ISourceConnector
{
    private string _topic = "cdc-events";
    private long _startPosition = 0;

    public event Action<string, string, byte[]>? OnRecord;

    public async Task StartAsync(CancellationToken ct)
    {
        var changes = _generator.GetChangesAfter(_startPosition);

        foreach (var change in changes)
        {
            if (ct.IsCancellationRequested) break;

            var key = GetEventKey(change);
            var value = JsonSerializer.SerializeToUtf8Bytes(change);

            OnRecord?.Invoke(_topic, key, value);
            _startPosition = change.Position;
        }
    }

    private string GetEventKey(CdcEvent evt)
    {
        // Use table name + row key as event key
        var row = evt.After ?? evt.Before;
        var id = row?.GetValueOrDefault("id", "unknown");
        return $"{evt.Table}:{id}";
    }
}
```

## Step 4: Build CDC Consumer

React to different change types:

```csharp
public class CdcConsumer
{
    public void ProcessEvent(CdcEvent evt)
    {
        switch (evt.Operation)
        {
            case "INSERT":
                Console.WriteLine($"New row in {evt.Table}: {FormatRow(evt.After)}");
                break;

            case "UPDATE":
                Console.WriteLine($"Updated {evt.Table}:");
                Console.WriteLine($"  Before: {FormatRow(evt.Before)}");
                Console.WriteLine($"  After: {FormatRow(evt.After)}");
                break;

            case "DELETE":
                Console.WriteLine($"Deleted from {evt.Table}: {FormatRow(evt.Before)}");
                break;
        }
    }
}
```

## Step 5: Build Materialized View

Maintain current state from CDC events:

```csharp
public class MaterializedView
{
    private Dictionary<string, Dictionary<string, object>> _state = new();

    public void ApplyEvent(CdcEvent evt)
    {
        var key = GetKey(evt);

        switch (evt.Operation)
        {
            case "INSERT":
            case "UPDATE":
                _state[key] = evt.After!;
                break;

            case "DELETE":
                _state.Remove(key);
                break;
        }
    }

    public Dictionary<string, object>? Get(string key)
    {
        return _state.TryGetValue(key, out var row) ? row : null;
    }

    public IEnumerable<Dictionary<string, object>> GetAll()
    {
        return _state.Values;
    }
}
```

## Step 6: Wire Complete CDC Pipeline

```csharp
// Generate changes
generator.Insert("users", new Dictionary<string, object> { ["id"] = 1, ["name"] = "Alice" });
generator.Update("users",
    new Dictionary<string, object> { ["id"] = 1, ["name"] = "Alice" },
    new Dictionary<string, object> { ["id"] = 1, ["name"] = "Alice Smith" });
generator.Delete("users", new Dictionary<string, object> { ["id"] = 1, ["name"] = "Alice Smith" });

// Stream to Surgewave
cdcSource.OnRecord += (topic, key, value) => producer.Produce(topic, key, value);
await cdcSource.StartAsync(cts.Token);

// Consume and apply to view
while (consuming)
{
    var result = consumer.Consume(timeout);
    var evt = JsonSerializer.Deserialize<CdcEvent>(result.Value);
    materializedView.ApplyEvent(evt);
}
```

## Verification

Run the completed solution:
```bash
cd completed
dotnet run
```

Expected output:
- INSERT events for new rows
- UPDATE events with before/after state
- DELETE events for removed rows
- Materialized view reflects current state
