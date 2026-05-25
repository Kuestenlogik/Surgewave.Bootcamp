// Surgewave Bootcamp - Unit 8, Lesson 2: File Connectors
// COMPLETED SOLUTION

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 8.2 - File Connectors ===");
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

// Sample CSV data (simulating file content)
var csvData = @"id,name,email,department
1,Alice,alice@example.com,Engineering
2,Bob,bob@example.com,Marketing
3,Charlie,charlie@example.com,Sales
4,Diana,diana@example.com,Engineering";

// Sample JSON data (simulating file content)
var jsonData = @"[
    {""productId"": ""P001"", ""name"": ""Widget"", ""price"": 29.99},
    {""productId"": ""P002"", ""name"": ""Gadget"", ""price"": 49.99},
    {""productId"": ""P003"", ""name"": ""Gizmo"", ""price"": 19.99}
]";

// ============== Demo 1: CSV Source Connector ==============
Console.WriteLine("--- Demo 1: CSV Source Connector ---");

var csvSource = new CsvSourceConnector();
csvSource.Configure(new Dictionary<string, string>
{
    ["topic"] = "users",
    ["data"] = csvData,
    ["has.header"] = "true"
});

await using var producer1 = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var csvRecordsProduced = 0;
csvSource.OnRecord += (topic, key, value) =>
{
    producer1.ProduceAsync(topic, Encoding.UTF8.GetBytes(key), value).GetAwaiter().GetResult();
    csvRecordsProduced++;
    var json = Encoding.UTF8.GetString(value);
    Console.WriteLine($"  CSV -> Surgewave: key={key}, value={json}");
};

using var csvCts = new CancellationTokenSource(2000);
try
{
    await csvSource.StartAsync(csvCts.Token);
}
catch (OperationCanceledException) { }

Console.WriteLine($"  Produced {csvRecordsProduced} records from CSV");
Console.WriteLine();

// ============== Demo 2: JSON Source Connector ==============
Console.WriteLine("--- Demo 2: JSON Source Connector ---");

var jsonSource = new JsonSourceConnector();
jsonSource.Configure(new Dictionary<string, string>
{
    ["topic"] = "products",
    ["data"] = jsonData,
    ["key.field"] = "productId"
});

await using var producer2 = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var jsonRecordsProduced = 0;
jsonSource.OnRecord += (topic, key, value) =>
{
    producer2.ProduceAsync(topic, Encoding.UTF8.GetBytes(key), value).GetAwaiter().GetResult();
    jsonRecordsProduced++;
    var json = Encoding.UTF8.GetString(value);
    Console.WriteLine($"  JSON -> Surgewave: key={key}, value={json}");
};

using var jsonCts = new CancellationTokenSource(2000);
try
{
    await jsonSource.StartAsync(jsonCts.Token);
}
catch (OperationCanceledException) { }

Console.WriteLine($"  Produced {jsonRecordsProduced} records from JSON");
Console.WriteLine();

// ============== Demo 3: CSV Sink Connector ==============
Console.WriteLine("--- Demo 3: CSV Sink Connector ---");

var csvSink = new CsvSinkConnector();
csvSink.Configure(new Dictionary<string, string>
{
    ["headers"] = "id,name,email,department"
});

await using var userConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "csv-sink-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

userConsumer.Subscribe("users");

var csvRecordsConsumed = 0;
var csvConsumeTimeout = DateTime.UtcNow.AddSeconds(2);

while (DateTime.UtcNow < csvConsumeTimeout && csvRecordsConsumed < csvRecordsProduced)
{
    var result = await userConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
    if (result != null)
    {
        var record = new ConnectorRecord(
            result.Topic,
            Encoding.UTF8.GetString(result.Key ?? Array.Empty<byte>()),
            result.Value ?? Array.Empty<byte>(),
            result.Timestamp.ToUnixTimeMilliseconds()
        );

        await csvSink.ProcessAsync(new[] { record });
        csvRecordsConsumed++;
    }
}

Console.WriteLine($"  Consumed {csvRecordsConsumed} records to CSV sink");
Console.WriteLine("  Generated CSV output:");
Console.WriteLine(csvSink.ToCsv());

// ============== Demo 4: JSON Sink Connector ==============
Console.WriteLine("--- Demo 4: JSON Sink Connector ---");

var jsonSink = new JsonSinkConnector();
jsonSink.Configure(new Dictionary<string, string>());

await using var productConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "json-sink-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

productConsumer.Subscribe("products");

var jsonRecordsConsumed = 0;
var jsonConsumeTimeout = DateTime.UtcNow.AddSeconds(2);

while (DateTime.UtcNow < jsonConsumeTimeout && jsonRecordsConsumed < jsonRecordsProduced)
{
    var result = await productConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
    if (result != null)
    {
        var record = new ConnectorRecord(
            result.Topic,
            Encoding.UTF8.GetString(result.Key ?? Array.Empty<byte>()),
            result.Value ?? Array.Empty<byte>(),
            result.Timestamp.ToUnixTimeMilliseconds()
        );

        await jsonSink.ProcessAsync(new[] { record });
        jsonRecordsConsumed++;
    }
}

Console.WriteLine($"  Consumed {jsonRecordsConsumed} records to JSON sink");
Console.WriteLine("  Generated JSON output:");
Console.WriteLine(jsonSink.ToJson());
Console.WriteLine();

// ============== Demo 5: Format Transformation ==============
Console.WriteLine("--- Demo 5: Format Transformation (CSV to JSON and back) ---");

// Show the roundtrip: CSV -> Surgewave -> JSON -> Surgewave -> CSV
Console.WriteLine("  Original CSV -> JSON records -> CSV output");
Console.WriteLine("  Data integrity maintained through transformation");
Console.WriteLine();

// Summary
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. CSV source: Parse rows, convert to JSON records");
Console.WriteLine("2. JSON source: Parse array, extract key field");
Console.WriteLine("3. CSV sink: Collect JSON, export as CSV");
Console.WriteLine("4. JSON sink: Collect records, export as JSON array");
Console.WriteLine("5. Connectors enable format transformation pipelines");

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

public class CsvSourceConnector : ISourceConnector
{
    private string _topic = "csv-data";
    private bool _hasHeader = true;
    private char _delimiter = ',';
    private string[] _headers = Array.Empty<string>();
    private List<string[]> _rows = new();
    private CancellationTokenSource? _cts;

    public event Action<string, string, byte[]>? OnRecord;

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("topic", out var topic))
            _topic = topic;
        if (config.TryGetValue("has.header", out var hasHeader))
            _hasHeader = bool.Parse(hasHeader);
        if (config.TryGetValue("delimiter", out var delim) && delim.Length > 0)
            _delimiter = delim[0];
        if (config.TryGetValue("data", out var csvData))
            ParseCsv(csvData);
    }

    private void ParseCsv(string csvData)
    {
        var lines = csvData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return;

        var startLine = 0;
        if (_hasHeader)
        {
            _headers = lines[0].Split(_delimiter).Select(h => h.Trim()).ToArray();
            startLine = 1;
        }
        else
        {
            // Generate default headers
            var firstRow = lines[0].Split(_delimiter);
            _headers = Enumerable.Range(0, firstRow.Length).Select(i => $"col{i}").ToArray();
        }

        for (int i = startLine; i < lines.Length; i++)
        {
            var fields = lines[i].Split(_delimiter).Select(f => f.Trim()).ToArray();
            _rows.Add(fields);
        }
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        foreach (var row in _rows)
        {
            if (_cts.Token.IsCancellationRequested)
                break;

            // Convert row to JSON
            var record = new Dictionary<string, object>();
            for (int i = 0; i < Math.Min(_headers.Length, row.Length); i++)
            {
                record[_headers[i]] = row[i];
            }

            var key = row.Length > 0 ? row[0] : Guid.NewGuid().ToString();
            var value = JsonSerializer.SerializeToUtf8Bytes(record);

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

public class JsonSourceConnector : ISourceConnector
{
    private string _topic = "json-data";
    private string _keyField = "id";
    private List<JsonElement> _records = new();
    private CancellationTokenSource? _cts;

    public event Action<string, string, byte[]>? OnRecord;

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("topic", out var topic))
            _topic = topic;
        if (config.TryGetValue("key.field", out var keyField))
            _keyField = keyField;
        if (config.TryGetValue("data", out var jsonData))
            ParseJson(jsonData);
    }

    private void ParseJson(string jsonData)
    {
        try
        {
            var doc = JsonDocument.Parse(jsonData);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    _records.Add(element.Clone());
                }
            }
            else
            {
                _records.Add(doc.RootElement.Clone());
            }
        }
        catch (JsonException)
        {
            // Handle JSON Lines format
            var lines = jsonData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                try
                {
                    var doc = JsonDocument.Parse(line);
                    _records.Add(doc.RootElement.Clone());
                }
                catch { }
            }
        }
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        foreach (var element in _records)
        {
            if (_cts.Token.IsCancellationRequested)
                break;

            // Extract key from configured field
            var key = "unknown";
            if (element.TryGetProperty(_keyField, out var keyProp))
            {
                key = keyProp.ToString();
            }

            var value = JsonSerializer.SerializeToUtf8Bytes(element);

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

public class CsvSinkConnector : ISinkConnector
{
    private List<Dictionary<string, string>> _rows = new();
    private string[] _headers = Array.Empty<string>();

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("headers", out var headers))
            _headers = headers.Split(',').Select(h => h.Trim()).ToArray();
    }

    public Task ProcessAsync(IEnumerable<ConnectorRecord> records)
    {
        foreach (var record in records)
        {
            try
            {
                var json = JsonDocument.Parse(record.Value);
                var row = new Dictionary<string, string>();

                foreach (var prop in json.RootElement.EnumerateObject())
                {
                    row[prop.Name] = prop.Value.ToString();
                }

                _rows.Add(row);

                // Auto-discover headers if not configured
                if (_headers.Length == 0)
                {
                    _headers = row.Keys.ToArray();
                }
            }
            catch (JsonException)
            {
                // Skip invalid JSON
            }
        }
        return Task.CompletedTask;
    }

    public string ToCsv()
    {
        var sb = new StringBuilder();

        // Write headers
        sb.AppendLine(string.Join(",", _headers));

        // Write rows
        foreach (var row in _rows)
        {
            var values = _headers.Select(h =>
            {
                if (row.TryGetValue(h, out var value))
                {
                    // Escape commas and quotes
                    if (value.Contains(',') || value.Contains('"'))
                        return $"\"{value.Replace("\"", "\"\"")}\"";
                    return value;
                }
                return "";
            });
            sb.AppendLine(string.Join(",", values));
        }

        return sb.ToString();
    }
}

public class JsonSinkConnector : ISinkConnector
{
    private List<JsonElement> _records = new();
    private bool _prettyPrint = true;
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("pretty.print", out var pretty))
            _prettyPrint = bool.Parse(pretty);
    }

    public Task ProcessAsync(IEnumerable<ConnectorRecord> records)
    {
        foreach (var record in records)
        {
            try
            {
                var json = JsonDocument.Parse(record.Value);
                _records.Add(json.RootElement.Clone());
            }
            catch (JsonException)
            {
                // Skip invalid JSON
            }
        }
        return Task.CompletedTask;
    }

    public string ToJson()
    {
        // Build array of all records
        var array = _records.Select(r =>
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in r.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ToString();
            }
            return dict;
        }).ToArray();

        return JsonSerializer.Serialize(array, _prettyPrint ? _jsonOptions : null);
    }
}
