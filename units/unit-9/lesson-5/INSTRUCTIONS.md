# Lesson 8.5: Custom Connectors - Instructions

## Step 1: Define Enhanced Connector Interfaces

Custom connectors need additional capabilities:

```csharp
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
```

## Step 2: Build HTTP Polling Source Connector

A source that polls an HTTP endpoint:

```csharp
public class HttpPollingSourceConnector : ICustomSourceConnector
{
    private string _name = "http-source";
    private string _topic = "http-data";
    private string _endpoint = "";
    private int _pollIntervalMs = 5000;
    private DateTime _startTime;

    public string Name => _name;
    public event Action<string, string, byte[]>? OnRecord;

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("name", out var name)) _name = name;
        if (config.TryGetValue("topic", out var topic)) _topic = topic;
        if (config.TryGetValue("endpoint", out var endpoint)) _endpoint = endpoint;
        if (config.TryGetValue("poll.interval.ms", out var interval))
            _pollIntervalMs = int.Parse(interval);
    }

    public Task ValidateAsync()
    {
        if (string.IsNullOrEmpty(_endpoint))
            throw new InvalidOperationException("Endpoint is required");
        return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _startTime = DateTime.UtcNow;

        while (!ct.IsCancellationRequested)
        {
            // Simulate HTTP poll
            var data = SimulateHttpGet(_endpoint);
            var key = DateTime.UtcNow.Ticks.ToString();
            OnRecord?.Invoke(_topic, key, data);

            await Task.Delay(_pollIntervalMs, ct);
        }
    }
}
```

## Step 3: Build Webhook Sink Connector

A sink that sends data to webhooks:

```csharp
public class WebhookSinkConnector : ICustomSinkConnector
{
    private string _name = "webhook-sink";
    private string _webhookUrl = "";
    private int _batchSize = 100;
    private List<byte[]> _batch = new();

    public async Task ProcessAsync(IEnumerable<ConnectorRecord> records)
    {
        foreach (var record in records)
        {
            _batch.Add(record.Value);

            if (_batch.Count >= _batchSize)
            {
                await FlushAsync();
            }
        }
    }

    public async Task FlushAsync()
    {
        if (_batch.Count == 0) return;

        // Simulate webhook POST
        await SimulateWebhookPost(_webhookUrl, _batch);
        _batch.Clear();
    }
}
```

## Step 4: Implement Error Handling

Add robust error handling to connectors:

```csharp
public class RetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public bool ExponentialBackoff { get; set; } = true;
}

public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, RetryPolicy policy)
{
    var attempts = 0;
    var delay = policy.RetryDelayMs;

    while (true)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            attempts++;
            if (attempts >= policy.MaxRetries)
                throw;

            Console.WriteLine($"Retry {attempts}/{policy.MaxRetries}: {ex.Message}");
            await Task.Delay(delay);

            if (policy.ExponentialBackoff)
                delay *= 2;
        }
    }
}
```

## Step 5: Build Connector Factory

Create connectors dynamically from configuration:

```csharp
public class ConnectorFactory
{
    private readonly Dictionary<string, Func<ICustomSourceConnector>> _sourceFactories = new()
    {
        ["http"] = () => new HttpPollingSourceConnector(),
        ["timer"] = () => new TimerSourceConnector(),
        ["random"] = () => new RandomDataSourceConnector()
    };

    private readonly Dictionary<string, Func<ICustomSinkConnector>> _sinkFactories = new()
    {
        ["webhook"] = () => new WebhookSinkConnector(),
        ["console"] = () => new ConsoleSinkConnector(),
        ["null"] = () => new NullSinkConnector()
    };

    public ICustomSourceConnector CreateSource(string type, Dictionary<string, string> config)
    {
        if (!_sourceFactories.TryGetValue(type, out var factory))
            throw new ArgumentException($"Unknown source type: {type}");

        var connector = factory();
        connector.Configure(config);
        return connector;
    }

    public void RegisterSource(string type, Func<ICustomSourceConnector> factory)
    {
        _sourceFactories[type] = factory;
    }
}
```

## Step 6: Implement Health Monitoring

Monitor connector health continuously:

```csharp
public class ConnectorHealthMonitor
{
    private readonly List<ICustomSourceConnector> _sources = new();
    private readonly List<ICustomSinkConnector> _sinks = new();

    public void Register(ICustomSourceConnector connector)
    {
        _sources.Add(connector);
    }

    public async Task MonitorAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            foreach (var source in _sources)
            {
                var health = source.GetHealth();
                if (!health.IsHealthy)
                {
                    Console.WriteLine($"UNHEALTHY: {source.Name} - {health.ErrorMessage}");
                }
            }

            await Task.Delay(5000, ct);
        }
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
- Custom HTTP source polling and producing records
- Webhook sink batching and sending
- Health checks reporting status
- Factory creating connectors from configuration
