// Surgewave Bootcamp - Unit 8, Lesson 1: Connect Concepts
// COMPLETED SOLUTION

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 8.1 - Connect Concepts ===");
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

// ============== Demo 1: Source Connector Pattern ==============
Console.WriteLine("--- Demo 1: Source Connector Pattern ---");

var sourceConnector = new SampleSourceConnector();
sourceConnector.Configure(new Dictionary<string, string>
{
    ["topic"] = "sensor-data",
    ["interval.ms"] = "500"
});

// Wire source to producer
await using var producer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var recordsProduced = 0;
sourceConnector.OnRecord += (topic, key, value) =>
{
    producer.ProduceAsync(topic, Encoding.UTF8.GetBytes(key), value).GetAwaiter().GetResult();
    recordsProduced++;
    Console.WriteLine($"  Source -> Surgewave: {key}");
};

// Run source for a few records
using var cts = new CancellationTokenSource(2500);
try
{
    await sourceConnector.StartAsync(cts.Token);
}
catch (OperationCanceledException) { }

Console.WriteLine($"  Produced {recordsProduced} records");
Console.WriteLine();

// ============== Demo 2: Sink Connector Pattern ==============
Console.WriteLine("--- Demo 2: Sink Connector Pattern ---");

var sinkConnector = new ConsoleSinkConnector();
sinkConnector.Configure(new Dictionary<string, string>
{
    ["prefix"] = "[SENSOR]"
});

// Wire consumer to sink
await using var consumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "sink-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe("sensor-data");

var recordsConsumed = 0;
var consumeTimeout = DateTime.UtcNow.AddSeconds(2);

while (DateTime.UtcNow < consumeTimeout && recordsConsumed < recordsProduced)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
    if (result != null)
    {
        var record = new ConnectorRecord(
            result.Topic,
            Encoding.UTF8.GetString(result.Key ?? Array.Empty<byte>()),
            result.Value ?? Array.Empty<byte>(),
            result.Timestamp.ToUnixTimeMilliseconds()
        );

        await sinkConnector.ProcessAsync(new[] { record });
        recordsConsumed++;
    }
}

Console.WriteLine($"  Consumed {recordsConsumed} records");
Console.WriteLine();

// ============== Demo 3: Connector Configuration ==============
Console.WriteLine("--- Demo 3: Connector Configuration ---");

var configuredSource = new SampleSourceConnector();
configuredSource.Configure(new Dictionary<string, string>
{
    ["topic"] = "custom-topic",
    ["interval.ms"] = "100",
    ["data.format"] = "json"
});

Console.WriteLine("  Source configured with:");
Console.WriteLine("    topic = custom-topic");
Console.WriteLine("    interval.ms = 100");
Console.WriteLine("    data.format = json");
Console.WriteLine();

// ============== Demo 4: Connector Lifecycle ==============
Console.WriteLine("--- Demo 4: Connector Lifecycle ---");

var lifecycleConnector = new LifecycleAwareConnector();

Console.WriteLine("  1. Configure");
lifecycleConnector.Configure(new Dictionary<string, string> { ["name"] = "demo" });

Console.WriteLine("  2. Start");
using var lifeCts = new CancellationTokenSource(500);
var startTask = lifecycleConnector.StartAsync(lifeCts.Token);

await Task.Delay(600);

Console.WriteLine("  3. Stop");
await lifecycleConnector.StopAsync();

try { await startTask; } catch (OperationCanceledException) { }

Console.WriteLine("  4. Cleanup complete");
Console.WriteLine();

// Summary
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Source connectors: External -> Surgewave (produce)");
Console.WriteLine("2. Sink connectors: Surgewave -> External (consume)");
Console.WriteLine("3. Configuration via Dictionary<string, string>");
Console.WriteLine("4. Lifecycle: Configure -> Start -> Stop");
Console.WriteLine("5. Events/callbacks for record handling");

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

public class SampleSourceConnector : ISourceConnector
{
    private string _topic = "sample-data";
    private int _intervalMs = 1000;
    private CancellationTokenSource? _cts;

    public event Action<string, string, byte[]>? OnRecord;

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("topic", out var topic))
            _topic = topic;
        if (config.TryGetValue("interval.ms", out var interval))
            _intervalMs = int.Parse(interval);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var counter = 0;

        while (!_cts.Token.IsCancellationRequested)
        {
            var key = $"sensor-{counter % 3}";
            var data = new
            {
                id = counter,
                sensor = key,
                value = counter * 10 % 100,
                timestamp = DateTime.UtcNow
            };
            var value = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data));

            OnRecord?.Invoke(_topic, key, value);
            counter++;

            try
            {
                await Task.Delay(_intervalMs, _cts.Token);
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

public class ConsoleSinkConnector : ISinkConnector
{
    private string _prefix = "[SINK]";

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("prefix", out var prefix))
            _prefix = prefix;
    }

    public Task ProcessAsync(IEnumerable<ConnectorRecord> records)
    {
        foreach (var record in records)
        {
            var value = Encoding.UTF8.GetString(record.Value);
            Console.WriteLine($"  {_prefix} {record.Key}: {value}");
        }
        return Task.CompletedTask;
    }
}

public class LifecycleAwareConnector : ISourceConnector
{
    private string _name = "unnamed";
    private CancellationTokenSource? _cts;

#pragma warning disable CS0067
    public event Action<string, string, byte[]>? OnRecord;
#pragma warning restore CS0067

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("name", out var name))
            _name = name;
        Console.WriteLine($"    [{_name}] Configured");
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        Console.WriteLine($"    [{_name}] Started");

        try
        {
            await Task.Delay(Timeout.Infinite, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"    [{_name}] Cancelled");
        }
    }

    public Task StopAsync()
    {
        Console.WriteLine($"    [{_name}] Stopping");
        _cts?.Cancel();
        return Task.CompletedTask;
    }
}
