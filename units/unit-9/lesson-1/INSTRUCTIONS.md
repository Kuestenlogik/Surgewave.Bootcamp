# Lesson 8.1: Connect Concepts - Instructions

## Step 1: Understand Connector Interfaces

Connectors follow a simple pattern:

```csharp
// Source: External System → Surgewave
public interface ISourceConnector
{
    void Configure(Dictionary<string, string> config);
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
    event Action<string, string, byte[]> OnRecord; // topic, key, value
}

// Sink: Surgewave → External System
public interface ISinkConnector
{
    void Configure(Dictionary<string, string> config);
    Task ProcessAsync(IEnumerable<ConnectorRecord> records);
}
```

## Step 2: Create a Simple Source Connector

Build a source that generates sample data:

```csharp
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
            var key = $"key-{counter}";
            var value = Encoding.UTF8.GetBytes($"{{\"id\":{counter},\"timestamp\":\"{DateTime.UtcNow:O}\"}}");

            OnRecord?.Invoke(_topic, key, value);
            counter++;

            await Task.Delay(_intervalMs, _cts.Token);
        }
    }

    public Task StopAsync()
    {
        _cts?.Cancel();
        return Task.CompletedTask;
    }
}
```

## Step 3: Create a Simple Sink Connector

Build a sink that logs received records:

```csharp
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
            Console.WriteLine($"{_prefix} {record.Topic}/{record.Key}: {value}");
        }
        return Task.CompletedTask;
    }
}
```

## Step 4: Wire Connectors Together

Connect source to Surgewave, Surgewave to sink:

```csharp
// Source → Surgewave
source.OnRecord += (topic, key, value) =>
{
    producer.Produce(topic, key, value);
};

// Surgewave → Sink
consumer.OnMessage += async (record) =>
{
    await sink.ProcessAsync(new[] { record });
};
```

## Verification

Run the completed solution:
```bash
cd completed
dotnet run
```

You should see:
- Source generating records every second
- Records flowing through Surgewave
- Sink logging received records
