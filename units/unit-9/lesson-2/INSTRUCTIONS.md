# Lesson 8.2: File Connectors - Instructions

## Step 1: Understand CSV Source Connector

A CSV source connector reads CSV data and produces records:

```csharp
public class CsvSourceConnector : ISourceConnector
{
    private string _topic = "csv-data";
    private bool _hasHeader = true;
    private char _delimiter = ',';
    private List<string[]> _data = new();

    public event Action<string, string, byte[]>? OnRecord;

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("topic", out var topic))
            _topic = topic;
        if (config.TryGetValue("has.header", out var hasHeader))
            _hasHeader = bool.Parse(hasHeader);
        if (config.TryGetValue("delimiter", out var delim))
            _delimiter = delim[0];
        if (config.TryGetValue("data", out var csvData))
            ParseCsv(csvData);
    }

    private void ParseCsv(string csvData)
    {
        var lines = csvData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var startLine = _hasHeader ? 1 : 0;

        for (int i = startLine; i < lines.Length; i++)
        {
            var fields = lines[i].Split(_delimiter);
            _data.Add(fields);
        }
    }
}
```

## Step 2: Build JSON Source Connector

JSON sources parse JSON arrays or JSON Lines:

```csharp
public class JsonSourceConnector : ISourceConnector
{
    private string _topic = "json-data";
    private List<JsonElement> _records = new();

    public event Action<string, string, byte[]>? OnRecord;

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("topic", out var topic))
            _topic = topic;
        if (config.TryGetValue("data", out var jsonData))
            ParseJson(jsonData);
    }

    private void ParseJson(string jsonData)
    {
        var doc = JsonDocument.Parse(jsonData);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                _records.Add(element.Clone());
            }
        }
    }
}
```

## Step 3: Create File Sink Connectors

Sink connectors write records to file formats:

```csharp
public class CsvSinkConnector : ISinkConnector
{
    private List<string[]> _rows = new();
    private string[] _headers = Array.Empty<string>();

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("headers", out var headers))
            _headers = headers.Split(',');
    }

    public Task ProcessAsync(IEnumerable<ConnectorRecord> records)
    {
        foreach (var record in records)
        {
            var json = JsonDocument.Parse(record.Value);
            var row = ExtractFields(json.RootElement);
            _rows.Add(row);
        }
        return Task.CompletedTask;
    }

    public string ToCsv()
    {
        var sb = new StringBuilder();
        if (_headers.Length > 0)
            sb.AppendLine(string.Join(",", _headers));
        foreach (var row in _rows)
            sb.AppendLine(string.Join(",", row));
        return sb.ToString();
    }
}
```

## Step 4: Implement Data Transformation

Transform data as it flows through the connector:

```csharp
// CSV row to JSON
var csvRow = new[] { "1", "John", "john@example.com" };
var headers = new[] { "id", "name", "email" };
var json = new Dictionary<string, string>();
for (int i = 0; i < headers.Length; i++)
    json[headers[i]] = csvRow[i];
var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(json);

// JSON to CSV row
var jsonDoc = JsonDocument.Parse(jsonBytes);
var values = headers.Select(h =>
    jsonDoc.RootElement.GetProperty(h).GetString() ?? ""
).ToArray();
```

## Step 5: Build Complete Pipeline

Wire source -> Surgewave -> sink:

```csharp
// Configure source
var csvSource = new CsvSourceConnector();
csvSource.Configure(new Dictionary<string, string>
{
    ["topic"] = "users",
    ["data"] = csvData,
    ["has.header"] = "true"
});

// Wire to producer
csvSource.OnRecord += (topic, key, value) =>
{
    producer.Produce(topic, Encoding.UTF8.GetBytes(key), value);
};

// Run source
await csvSource.StartAsync(cts.Token);

// Configure sink
var jsonSink = new JsonSinkConnector();
jsonSink.Configure(new Dictionary<string, string>());

// Wire consumer to sink
while (consuming)
{
    var result = consumer.Consume(timeout);
    if (result != null)
    {
        var record = new ConnectorRecord(...);
        await jsonSink.ProcessAsync(new[] { record });
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
- CSV data parsed and converted to JSON
- JSON data parsed and sent to Surgewave
- Records consumed and written to CSV/JSON sinks
- Data transformation demonstrated
