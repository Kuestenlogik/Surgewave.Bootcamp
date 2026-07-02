// Surgewave Bootcamp - Unit 8, Lesson 1: Connect Concepts
//
// In this lesson, you'll learn to:
// - Understand source and sink connector patterns
// - Configure connectors with settings
// - Implement connector lifecycle
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 8.1 - Connect Concepts ===");
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

// TODO: Create a SampleSourceConnector
// Hint: var sourceConnector = new SampleSourceConnector();

// TODO: Configure the source connector
// Hint: sourceConnector.Configure(new Dictionary<string, string>
//       {
//           ["topic"] = "sensor-data",
//           ["interval.ms"] = "500"
//       });

// TODO: Wire source to producer
// Hint: sourceConnector.OnRecord += (topic, key, value) =>
//       {
//           producer.Produce(topic, Encoding.UTF8.GetBytes(key), value);
//       };

// TODO: Create a ConsoleSinkConnector
// Hint: var sinkConnector = new ConsoleSinkConnector();

// TODO: Wire consumer to sink
// Hint: await sinkConnector.ProcessAsync(new[] { record });

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

// TODO: Implement SampleSourceConnector
// Hint: Generate records periodically with sensor data

// TODO: Implement ConsoleSinkConnector
// Hint: Log received records to console
