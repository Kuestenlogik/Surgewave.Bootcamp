// Surgewave Bootcamp - Unit 9, Lesson 1: Logging & Diagnostics
// COMPLETED SOLUTION

using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 9.1 - Logging & Diagnostics ===");
Console.WriteLine();

// ============== Demo 1: Configure Logging ==============
Console.WriteLine("--- Demo 1: Configure Logging ---");

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Debug)
        .AddFilter("Surgewave", LogLevel.Debug)
        .AddFilter("Microsoft", LogLevel.Warning)
        .AddSimpleConsole(options =>
        {
            options.TimestampFormat = "[HH:mm:ss] ";
            options.SingleLine = true;
            options.IncludeScopes = true;
        });
});

var logger = loggerFactory.CreateLogger("SurgewaveApp");
var producerLogger = loggerFactory.CreateLogger("Surgewave.Producer");
var consumerLogger = loggerFactory.CreateLogger("Surgewave.Consumer");

Console.WriteLine("  Logging configured with:");
Console.WriteLine("    - Minimum level: Debug");
Console.WriteLine("    - Timestamp format: [HH:mm:ss]");
Console.WriteLine("    - Single line output");
Console.WriteLine();

// ============== Demo 2: Log Levels ==============
Console.WriteLine("--- Demo 2: Log Levels ---");

logger.LogTrace("This is a TRACE message (very detailed)");
logger.LogDebug("This is a DEBUG message (development info)");
logger.LogInformation("This is an INFO message (normal operations)");
logger.LogWarning("This is a WARNING message (unexpected but handled)");
logger.LogError("This is an ERROR message (operation failure)");
// logger.LogCritical("This is a CRITICAL message (app crash)"); // Not shown to avoid alarm

Console.WriteLine();

// ============== Demo 3: Structured Logging ==============
Console.WriteLine("--- Demo 3: Structured Logging ---");

var topic = "orders";
var partition = 0;
var offset = 42L;

// Good: structured logging with named parameters
logger.LogInformation("Produced message to {Topic} at partition {Partition}, offset {Offset}",
    topic, partition, offset);

// The parameters are captured as structured data, not just string substitution
logger.LogInformation("Processing {MessageCount} messages from {ConsumerGroup}",
    100, "order-processors");

Console.WriteLine();

// ============== Demo 4: Diagnostic Events ==============
Console.WriteLine("--- Demo 4: Diagnostic Events ---");

// Subscribe to diagnostic events
var diagnosticObserver = new SurgewaveDiagnosticObserver(logger);
using var subscription = DiagnosticListener.AllListeners.Subscribe(diagnosticObserver);

// Simulate diagnostic events
var diagnosticSource = new DiagnosticListener("Surgewave.Demo");
diagnosticSource.Write("ProduceStart", new { Topic = "orders", Key = "order-123" });
diagnosticSource.Write("ProduceComplete", new { Topic = "orders", Offset = 42, Duration = TimeSpan.FromMilliseconds(5) });

Console.WriteLine();

// ============== Demo 5: Logging Scopes ==============
Console.WriteLine("--- Demo 5: Logging Scopes ---");

var requestId = Guid.NewGuid().ToString("N")[..8];

using (logger.BeginScope(new Dictionary<string, object>
{
    ["RequestId"] = requestId,
    ["UserId"] = "user-456"
}))
{
    logger.LogInformation("Request started");
    logger.LogDebug("Validating input parameters");
    logger.LogInformation("Processing order");
    logger.LogInformation("Request completed successfully");
}

Console.WriteLine();

// ============== Demo 6: Full Example with Surgewave ==============
Console.WriteLine("--- Demo 6: Full Example with Surgewave ---");

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

logger.LogInformation("Broker started on {Host}:{Port}", "localhost", runtime.Port);

// Create producer with logging
await using var producer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

producerLogger.LogDebug("Producer created and connected");

// Produce messages with detailed logging
for (var i = 0; i < 3; i++)
{
    var msgKey = $"key-{i}";
    var msgValue = $"{{\"orderId\": {i}, \"amount\": {100 + i * 10}}}";

    using (producerLogger.BeginScope(new Dictionary<string, object>
    {
        ["MessageKey"] = msgKey,
        ["MessageIndex"] = i
    }))
    {
        producerLogger.LogDebug("Preparing to produce message");
        await producer.ProduceAsync("orders",
            Encoding.UTF8.GetBytes(msgKey),
            Encoding.UTF8.GetBytes(msgValue));
        producerLogger.LogInformation("Produced message {Index} to {Topic}", i, "orders");
    }
}

// Create consumer with logging
await using var consumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "logging-demo";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe("orders");
consumerLogger.LogDebug("Consumer subscribed to topic {Topic}", "orders");

// Consume messages with detailed logging
var consumeTimeout = DateTime.UtcNow.AddSeconds(2);
var consumed = 0;

while (DateTime.UtcNow < consumeTimeout && consumed < 3)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
    if (result != null)
    {
        consumed++;
        var key = Encoding.UTF8.GetString(result.Key ?? Array.Empty<byte>());
        var value = Encoding.UTF8.GetString(result.Value ?? Array.Empty<byte>());

        using (consumerLogger.BeginScope(new Dictionary<string, object>
        {
            ["Partition"] = result.Partition,
            ["Offset"] = result.Offset
        }))
        {
            consumerLogger.LogInformation("Consumed message {Key} from {Topic}", key, result.Topic);
            consumerLogger.LogDebug("Message value: {Value}", value);
        }
    }
}

consumerLogger.LogInformation("Consumed {Count} messages total", consumed);

Console.WriteLine();

// Summary
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use structured logging: LogInformation(\"{Topic}\", topic)");
Console.WriteLine("2. Choose appropriate log levels for each message");
Console.WriteLine("3. Use scopes to group related log entries");
Console.WriteLine("4. Subscribe to DiagnosticListener for component events");
Console.WriteLine("5. Create category-specific loggers for filtering");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Diagnostic Observer Classes ==============

public class SurgewaveDiagnosticObserver : IObserver<DiagnosticListener>
{
    private readonly ILogger _logger;

    public SurgewaveDiagnosticObserver(ILogger logger)
    {
        _logger = logger;
    }

    public void OnNext(DiagnosticListener listener)
    {
        if (listener.Name.StartsWith("Surgewave"))
        {
            _logger.LogDebug("Subscribing to diagnostic source: {Source}", listener.Name);
            listener.Subscribe(new EventObserver(_logger));
        }
    }

    public void OnError(Exception error)
    {
        _logger.LogError(error, "Error in diagnostic listener");
    }

    public void OnCompleted()
    {
        _logger.LogDebug("Diagnostic listener completed");
    }
}

public class EventObserver : IObserver<KeyValuePair<string, object?>>
{
    private readonly ILogger _logger;

    public EventObserver(ILogger logger)
    {
        _logger = logger;
    }

    public void OnNext(KeyValuePair<string, object?> pair)
    {
        _logger.LogDebug("[DIAG] {EventName}: {EventData}", pair.Key, pair.Value);
    }

    public void OnError(Exception error)
    {
        _logger.LogError(error, "Error in event observer");
    }

    public void OnCompleted()
    {
        // Observer completed
    }
}
