// Surgewave Bootcamp - Unit 8, Lesson 2: File Connectors
//
// In this lesson, you'll learn to:
// - Build CSV and JSON source connectors
// - Create file sink connectors for export
// - Transform data between formats
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

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

// TODO: Create a CsvSourceConnector
// Hint: var csvSource = new CsvSourceConnector();

// TODO: Configure the CSV source with data and topic
// Hint: csvSource.Configure(new Dictionary<string, string>
//       {
//           ["topic"] = "users",
//           ["data"] = csvData,
//           ["has.header"] = "true"
//       });

// TODO: Create a producer and wire the source to it
// Hint: await using var producer = new SurgewaveProducer<byte[], byte[]>(options =>
//       {
//           options.BootstrapServers = $"localhost:{runtime.Port}";
//       });
// Hint: csvSource.OnRecord += (topic, key, value) => { ... };

// TODO: Start the source connector
// Hint: await csvSource.StartAsync(cts.Token);

Console.WriteLine();

// ============== Demo 2: JSON Source Connector ==============
Console.WriteLine("--- Demo 2: JSON Source Connector ---");

// TODO: Create a JsonSourceConnector
// Hint: var jsonSource = new JsonSourceConnector();

// TODO: Configure the JSON source with data and topic
// Hint: jsonSource.Configure(new Dictionary<string, string>
//       {
//           ["topic"] = "products",
//           ["data"] = jsonData,
//           ["key.field"] = "productId"
//       });

// TODO: Wire source to producer and start it
// Hint: Follow the same pattern as CSV source

Console.WriteLine();

// ============== Demo 3: CSV Sink Connector ==============
Console.WriteLine("--- Demo 3: CSV Sink Connector ---");

// TODO: Create a CsvSinkConnector
// Hint: var csvSink = new CsvSinkConnector();

// TODO: Configure the sink with headers
// Hint: csvSink.Configure(new Dictionary<string, string>
//       {
//           ["headers"] = "id,name,email,department"
//       });

// TODO: Consume from topic and write to sink using SurgewaveConsumer
// Hint: await using var consumer = new SurgewaveConsumer<byte[], byte[]>(options =>
//       {
//           options.BootstrapServers = $"localhost:{runtime.Port}";
//           options.GroupId = "csv-sink-group";
//           options.AutoOffsetReset = AutoOffsetReset.Earliest;
//       });
// Hint: await csvSink.ProcessAsync(new[] { record });

// TODO: Print the resulting CSV
// Hint: Console.WriteLine(csvSink.ToCsv());

Console.WriteLine();

// ============== Demo 4: JSON Sink Connector ==============
Console.WriteLine("--- Demo 4: JSON Sink Connector ---");

// TODO: Create a JsonSinkConnector
// Hint: var jsonSink = new JsonSinkConnector();

// TODO: Consume from products topic and write to sink
// Hint: Follow the same pattern as CSV sink

// TODO: Print the resulting JSON
// Hint: Console.WriteLine(jsonSink.ToJson());

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

// TODO: Implement CsvSourceConnector
// Hint: Parse CSV data line by line, skip header if configured
// Hint: Convert each row to JSON for the record value
// Hint: Use the first column as the key

// TODO: Implement JsonSourceConnector
// Hint: Parse JSON array, iterate elements
// Hint: Use configured key.field for record key
// Hint: Serialize each element as the record value

// TODO: Implement CsvSinkConnector
// Hint: Parse incoming JSON records
// Hint: Extract fields based on headers
// Hint: Build CSV output with ToCsv() method

// TODO: Implement JsonSinkConnector
// Hint: Collect incoming records
// Hint: Build JSON array with ToJson() method
