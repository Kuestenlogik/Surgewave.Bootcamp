# Lesson 8.3: Database Connectors - Instructions

## Step 1: Understand Database Source Architecture

A database source connector queries tables and produces records:

```csharp
public class DatabaseSourceConnector : ISourceConnector
{
    private string _topic = "db-data";
    private string _table = "records";
    private string _mode = "bulk"; // bulk or incremental
    private string _incrementingColumn = "id";
    private long _lastOffset = 0;

    public event Action<string, string, byte[]>? OnRecord;

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
    }
}
```

## Step 2: Implement Bulk Load Pattern

Bulk load reads all rows from a table:

```csharp
// Simulated bulk load
var allRows = simulatedDatabase.GetAllRows(tableName);
foreach (var row in allRows)
{
    var key = row["id"].ToString();
    var value = JsonSerializer.SerializeToUtf8Bytes(row);
    OnRecord?.Invoke(_topic, key, value);
}
```

## Step 3: Implement Incremental Load Pattern

Incremental load tracks position and queries only new rows:

```csharp
// Query only rows with ID > last offset
var newRows = simulatedDatabase.GetRowsWhere(
    tableName,
    $"{_incrementingColumn} > {_lastOffset}"
);

foreach (var row in newRows)
{
    var id = Convert.ToInt64(row[_incrementingColumn]);
    if (id > _lastOffset)
        _lastOffset = id;

    var key = row["id"].ToString();
    var value = JsonSerializer.SerializeToUtf8Bytes(row);
    OnRecord?.Invoke(_topic, key, value);
}
```

## Step 4: Build Database Sink Connector

The sink connector writes records to database tables:

```csharp
public class DatabaseSinkConnector : ISinkConnector
{
    private string _table = "output";
    private List<Dictionary<string, object>> _batch = new();
    private int _batchSize = 100;

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("table", out var table))
            _table = table;
        if (config.TryGetValue("batch.size", out var size))
            _batchSize = int.Parse(size);
    }

    public async Task ProcessAsync(IEnumerable<ConnectorRecord> records)
    {
        foreach (var record in records)
        {
            var row = JsonSerializer.Deserialize<Dictionary<string, object>>(record.Value);
            _batch.Add(row);

            if (_batch.Count >= _batchSize)
            {
                await FlushBatchAsync();
            }
        }
    }

    private async Task FlushBatchAsync()
    {
        // Execute batch insert/upsert
        foreach (var row in _batch)
        {
            simulatedDatabase.Upsert(_table, row);
        }
        _batch.Clear();
    }
}
```

## Step 5: Implement Upsert Semantics

Handle both insert and update operations:

```csharp
public void Upsert(Dictionary<string, object> row)
{
    var key = row["id"];

    if (_data.ContainsKey(key))
    {
        // Update existing row
        _data[key] = row;
        Console.WriteLine($"  Updated row: {key}");
    }
    else
    {
        // Insert new row
        _data[key] = row;
        Console.WriteLine($"  Inserted row: {key}");
    }
}
```

## Step 6: Wire Complete Pipeline

Connect database source -> Surgewave -> database sink:

```csharp
// Configure source
var dbSource = new DatabaseSourceConnector();
dbSource.Configure(new Dictionary<string, string>
{
    ["topic"] = "orders",
    ["table"] = "source_orders",
    ["mode"] = "incremental"
});

// Wire to producer
dbSource.OnRecord += (topic, key, value) =>
{
    producer.Produce(topic, Encoding.UTF8.GetBytes(key), value);
};

// Configure sink
var dbSink = new DatabaseSinkConnector();
dbSink.Configure(new Dictionary<string, string>
{
    ["table"] = "target_orders",
    ["batch.size"] = "10"
});

// Wire consumer to sink
while (consuming)
{
    var result = consumer.Consume(timeout);
    if (result != null)
    {
        await dbSink.ProcessAsync(new[] { record });
    }
}
```

## Verification

Run the completed solution:
```bash
cd completed
dotnet run
```

Expected output:
- Bulk load: All rows queried and produced
- Incremental load: Only new rows queried
- Sink batching: Records written in batches
- Upsert: Updates for existing keys, inserts for new
