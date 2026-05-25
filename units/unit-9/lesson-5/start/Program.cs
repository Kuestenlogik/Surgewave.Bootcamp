// Surgewave Bootcamp - Unit 8, Lesson 5: Custom Connectors
//
// In this lesson, you'll learn to:
// - Design custom connector interfaces
// - Build source connectors with lifecycle management
// - Create sink connectors with error handling
// - Implement connector factories for dynamic instantiation
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 8.5 - Custom Connectors ===");
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

// ============== Demo 1: Custom Source Connector ==============
Console.WriteLine("--- Demo 1: Custom HTTP Polling Source ---");

// TODO: Create an HttpPollingSourceConnector
// Hint: var httpSource = new HttpPollingSourceConnector();

// TODO: Configure the connector
// Hint: httpSource.Configure(new Dictionary<string, string>
//       {
//           ["name"] = "api-poller",
//           ["topic"] = "api-data",
//           ["endpoint"] = "https://api.example.com/data",
//           ["poll.interval.ms"] = "500"
//       });

// TODO: Validate the configuration
// Hint: await httpSource.ValidateAsync();

// TODO: Wire to producer and start
// Hint: httpSource.OnRecord += (topic, key, value) => { ... };
//       await httpSource.StartAsync(cts.Token);

Console.WriteLine();

// ============== Demo 2: Custom Sink Connector ==============
Console.WriteLine("--- Demo 2: Custom Webhook Sink ---");

// TODO: Create a WebhookSinkConnector
// Hint: var webhookSink = new WebhookSinkConnector();

// TODO: Configure with webhook URL and batch settings
// Hint: webhookSink.Configure(new Dictionary<string, string>
//       {
//           ["name"] = "webhook-sender",
//           ["webhook.url"] = "https://webhook.example.com/receive",
//           ["batch.size"] = "5"
//       });

// TODO: Process records through the sink
// Hint: await webhookSink.ProcessAsync(new[] { record });

// TODO: Flush remaining records
// Hint: await webhookSink.FlushAsync();

Console.WriteLine();

// ============== Demo 3: Connector Health Monitoring ==============
Console.WriteLine("--- Demo 3: Health Monitoring ---");

// TODO: Get health status from connectors
// Hint: var health = httpSource.GetHealth();
//       Console.WriteLine($"Health: {health.Status}");

// TODO: Get metrics from connectors
// Hint: var metrics = httpSource.GetMetrics();
//       Console.WriteLine($"Records: {metrics.RecordsProcessed}");

Console.WriteLine();

// ============== Demo 4: Connector Factory ==============
Console.WriteLine("--- Demo 4: Connector Factory ---");

// TODO: Create a ConnectorFactory
// Hint: var factory = new ConnectorFactory();

// TODO: Create connectors using the factory
// Hint: var source = factory.CreateSource("http", config);
//       var sink = factory.CreateSink("webhook", config);

// TODO: Register a custom connector type
// Hint: factory.RegisterSource("custom", () => new CustomSourceConnector());

Console.WriteLine();

// ============== Demo 5: Error Handling with Retry ==============
Console.WriteLine("--- Demo 5: Error Handling ---");

// TODO: Create a RetryPolicy
// Hint: var policy = new RetryPolicy
//       {
//           MaxRetries = 3,
//           RetryDelayMs = 100,
//           ExponentialBackoff = true
//       };

// TODO: Execute operation with retry
// Hint: await ExecuteWithRetryAsync(async () => { ... }, policy);

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Connector Interfaces ==============

public record ConnectorRecord(string Topic, string Key, byte[] Value, long Timestamp);

public class ConnectorHealth
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = "";
    public DateTime LastCheck { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ConnectorMetrics
{
    public long RecordsProcessed { get; set; }
    public long BytesProcessed { get; set; }
    public long ErrorCount { get; set; }
    public TimeSpan Uptime { get; set; }
}

public interface ICustomSourceConnector
{
    string Name { get; }
    void Configure(Dictionary<string, string> config);
    Task ValidateAsync();
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
    ConnectorHealth GetHealth();
    ConnectorMetrics GetMetrics();
    event Action<string, string, byte[]>? OnRecord;
}

public interface ICustomSinkConnector
{
    string Name { get; }
    void Configure(Dictionary<string, string> config);
    Task ValidateAsync();
    Task ProcessAsync(IEnumerable<ConnectorRecord> records);
    Task FlushAsync();
    ConnectorHealth GetHealth();
    ConnectorMetrics GetMetrics();
}

public class RetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public bool ExponentialBackoff { get; set; } = true;
}

// TODO: Implement HttpPollingSourceConnector
// Hint: Simulate HTTP polling with random data
// Hint: Track records processed and errors
// Hint: Implement health check and metrics

// TODO: Implement WebhookSinkConnector
// Hint: Batch records before "sending"
// Hint: Simulate webhook POST with console output
// Hint: Track metrics and provide health status

// TODO: Implement ConnectorFactory
// Hint: Use Dictionary to map type names to factory functions
// Hint: Support both source and sink connectors
// Hint: Allow registration of custom types
