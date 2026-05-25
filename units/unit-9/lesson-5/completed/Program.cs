// Surgewave Bootcamp - Unit 8, Lesson 5: Custom Connectors
// COMPLETED SOLUTION

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

var httpSource = new HttpPollingSourceConnector();
httpSource.Configure(new Dictionary<string, string>
{
    ["name"] = "api-poller",
    ["topic"] = "api-data",
    ["endpoint"] = "https://api.example.com/data",
    ["poll.interval.ms"] = "300"
});

Console.WriteLine("  Validating configuration...");
await httpSource.ValidateAsync();
Console.WriteLine("  Configuration valid!");

await using var producer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var httpRecordsProduced = 0;
httpSource.OnRecord += (topic, key, value) =>
{
    producer.ProduceAsync(topic, Encoding.UTF8.GetBytes(key), value).GetAwaiter().GetResult();
    httpRecordsProduced++;
    var json = Encoding.UTF8.GetString(value);
    Console.WriteLine($"  HTTP Poll -> Surgewave: key={key}, value={json}");
};

Console.WriteLine("  Starting HTTP source...");
using var httpCts = new CancellationTokenSource(1500);
try
{
    await httpSource.StartAsync(httpCts.Token);
}
catch (OperationCanceledException) { }

Console.WriteLine($"  Produced {httpRecordsProduced} records from HTTP source");
Console.WriteLine();

// ============== Demo 2: Custom Sink Connector ==============
Console.WriteLine("--- Demo 2: Custom Webhook Sink ---");

var webhookSink = new WebhookSinkConnector();
webhookSink.Configure(new Dictionary<string, string>
{
    ["name"] = "webhook-sender",
    ["webhook.url"] = "https://webhook.example.com/receive",
    ["batch.size"] = "3"
});

await webhookSink.ValidateAsync();

await using var consumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "webhook-sink-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe("api-data");

var sinkRecordsConsumed = 0;
var consumeTimeout = DateTime.UtcNow.AddSeconds(2);

while (DateTime.UtcNow < consumeTimeout && sinkRecordsConsumed < httpRecordsProduced)
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

        await webhookSink.ProcessAsync(new[] { record });
        sinkRecordsConsumed++;
    }
}

// Flush remaining batch
await webhookSink.FlushAsync();

Console.WriteLine($"  Processed {sinkRecordsConsumed} records through webhook sink");
Console.WriteLine();

// ============== Demo 3: Connector Health Monitoring ==============
Console.WriteLine("--- Demo 3: Health Monitoring ---");

Console.WriteLine("  HTTP Source Health:");
var httpHealth = httpSource.GetHealth();
Console.WriteLine($"    IsHealthy: {httpHealth.IsHealthy}");
Console.WriteLine($"    Status: {httpHealth.Status}");
Console.WriteLine($"    LastCheck: {httpHealth.LastCheck:HH:mm:ss}");

Console.WriteLine();
Console.WriteLine("  HTTP Source Metrics:");
var httpMetrics = httpSource.GetMetrics();
Console.WriteLine($"    RecordsProcessed: {httpMetrics.RecordsProcessed}");
Console.WriteLine($"    BytesProcessed: {httpMetrics.BytesProcessed}");
Console.WriteLine($"    ErrorCount: {httpMetrics.ErrorCount}");
Console.WriteLine($"    Uptime: {httpMetrics.Uptime.TotalSeconds:F1}s");

Console.WriteLine();
Console.WriteLine("  Webhook Sink Health:");
var sinkHealth = webhookSink.GetHealth();
Console.WriteLine($"    IsHealthy: {sinkHealth.IsHealthy}");
Console.WriteLine($"    Status: {sinkHealth.Status}");

Console.WriteLine();
Console.WriteLine("  Webhook Sink Metrics:");
var sinkMetrics = webhookSink.GetMetrics();
Console.WriteLine($"    RecordsProcessed: {sinkMetrics.RecordsProcessed}");
Console.WriteLine($"    BytesProcessed: {sinkMetrics.BytesProcessed}");
Console.WriteLine($"    BatchesSent: (estimated from records/batch)");
Console.WriteLine();

// ============== Demo 4: Connector Factory ==============
Console.WriteLine("--- Demo 4: Connector Factory ---");

var factory = new ConnectorFactory();

Console.WriteLine("  Creating connectors via factory...");

// Create HTTP source
var factoryHttpSource = factory.CreateSource("http", new Dictionary<string, string>
{
    ["name"] = "factory-http",
    ["topic"] = "factory-data",
    ["endpoint"] = "https://api.example.com/v2/data"
});
Console.WriteLine($"  Created source: {factoryHttpSource.Name} (type: http)");

// Create timer source
var factoryTimerSource = factory.CreateSource("timer", new Dictionary<string, string>
{
    ["name"] = "factory-timer",
    ["topic"] = "timer-data",
    ["interval.ms"] = "1000"
});
Console.WriteLine($"  Created source: {factoryTimerSource.Name} (type: timer)");

// Create webhook sink
var factoryWebhookSink = factory.CreateSink("webhook", new Dictionary<string, string>
{
    ["name"] = "factory-webhook",
    ["webhook.url"] = "https://webhook.example.com/v2"
});
Console.WriteLine($"  Created sink: {factoryWebhookSink.Name} (type: webhook)");

// Create console sink
var factoryConsoleSink = factory.CreateSink("console", new Dictionary<string, string>
{
    ["name"] = "factory-console",
    ["prefix"] = "[FACTORY]"
});
Console.WriteLine($"  Created sink: {factoryConsoleSink.Name} (type: console)");

// Register custom connector
Console.WriteLine();
Console.WriteLine("  Registering custom connector type...");
factory.RegisterSource("random", () => new RandomDataSourceConnector());

var randomSource = factory.CreateSource("random", new Dictionary<string, string>
{
    ["name"] = "random-generator",
    ["topic"] = "random-data"
});
Console.WriteLine($"  Created custom source: {randomSource.Name} (type: random)");
Console.WriteLine();

// ============== Demo 5: Error Handling with Retry ==============
Console.WriteLine("--- Demo 5: Error Handling with Retry ---");

var retryPolicy = new RetryPolicy
{
    MaxRetries = 3,
    RetryDelayMs = 100,
    ExponentialBackoff = true
};

Console.WriteLine($"  Retry Policy: MaxRetries={retryPolicy.MaxRetries}, " +
                  $"InitialDelay={retryPolicy.RetryDelayMs}ms, ExponentialBackoff={retryPolicy.ExponentialBackoff}");
Console.WriteLine();

// Demonstrate retry with simulated failures
var attemptCount = 0;
try
{
    var result = await ExecuteWithRetryAsync(async () =>
    {
        attemptCount++;
        Console.WriteLine($"  Attempt {attemptCount}...");

        // Simulate failures for first 2 attempts
        if (attemptCount < 3)
        {
            await Task.Delay(10);
            throw new InvalidOperationException($"Simulated failure on attempt {attemptCount}");
        }

        return "Success!";
    }, retryPolicy);

    Console.WriteLine($"  Result: {result}");
}
catch (Exception ex)
{
    Console.WriteLine($"  Failed after all retries: {ex.Message}");
}

Console.WriteLine();

// ============== Demo 6: Complete Pipeline with Custom Connectors ==============
Console.WriteLine("--- Demo 6: Complete Pipeline with Custom Connectors ---");

var pipelineSource = factory.CreateSource("random", new Dictionary<string, string>
{
    ["name"] = "pipeline-source",
    ["topic"] = "pipeline-data",
    ["records.per.poll"] = "3"
});

await using var pipelineProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var pipelineConsoleSink = factory.CreateSink("console", new Dictionary<string, string>
{
    ["name"] = "pipeline-sink",
    ["prefix"] = "[PIPELINE]"
});

// Wire source to Surgewave
var pipelineRecords = 0;
pipelineSource.OnRecord += (topic, key, value) =>
{
    pipelineProducer.ProduceAsync(topic, Encoding.UTF8.GetBytes(key), value).GetAwaiter().GetResult();
    pipelineRecords++;
};

Console.WriteLine("  Running pipeline: RandomSource -> Surgewave -> ConsoleSink");

// Run source briefly
using var pipelineCts = new CancellationTokenSource(500);
try
{
    await pipelineSource.StartAsync(pipelineCts.Token);
}
catch (OperationCanceledException) { }

Console.WriteLine($"  Pipeline produced {pipelineRecords} records");
Console.WriteLine();

// Summary
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Custom connectors extend Surgewave's integration capabilities");
Console.WriteLine("2. Lifecycle: Configure -> Validate -> Start -> Stop");
Console.WriteLine("3. Health checks enable monitoring and alerting");
Console.WriteLine("4. Metrics track throughput, errors, and uptime");
Console.WriteLine("5. Factory pattern enables configuration-driven instantiation");
Console.WriteLine("6. Retry policies handle transient failures gracefully");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Helper: Retry Execution ==============

static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, RetryPolicy policy)
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

            Console.WriteLine($"    Retry {attempts}/{policy.MaxRetries} after {delay}ms: {ex.Message}");
            await Task.Delay(delay);

            if (policy.ExponentialBackoff)
                delay *= 2;
        }
    }
}

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

// ============== HTTP Polling Source Connector ==============

public class HttpPollingSourceConnector : ICustomSourceConnector
{
    private string _name = "http-source";
    private string _topic = "http-data";
    private string _endpoint = "";
    private int _pollIntervalMs = 5000;
    private DateTime _startTime;
    private long _recordsProcessed = 0;
    private long _bytesProcessed = 0;
    private long _errorCount = 0;
    private CancellationTokenSource? _cts;
    private bool _isRunning = false;

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
            throw new InvalidOperationException("Endpoint configuration is required");
        if (_pollIntervalMs < 100)
            throw new InvalidOperationException("Poll interval must be at least 100ms");
        return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _startTime = DateTime.UtcNow;
        _isRunning = true;

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                // Simulate HTTP GET response
                var data = SimulateHttpGet();
                var key = DateTime.UtcNow.Ticks.ToString();

                OnRecord?.Invoke(_topic, key, data);
                _recordsProcessed++;
                _bytesProcessed += data.Length;
            }
            catch (Exception)
            {
                _errorCount++;
            }

            try
            {
                await Task.Delay(_pollIntervalMs, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _isRunning = false;
    }

    private byte[] SimulateHttpGet()
    {
        var response = new
        {
            endpoint = _endpoint,
            timestamp = DateTime.UtcNow,
            data = new
            {
                value = Random.Shared.Next(1, 100),
                status = "ok"
            }
        };
        return JsonSerializer.SerializeToUtf8Bytes(response);
    }

    public Task StopAsync()
    {
        _cts?.Cancel();
        _isRunning = false;
        return Task.CompletedTask;
    }

    public ConnectorHealth GetHealth()
    {
        return new ConnectorHealth
        {
            IsHealthy = _errorCount == 0 || (_recordsProcessed > 0 && _errorCount < _recordsProcessed / 10),
            Status = _isRunning ? "Running" : "Stopped",
            LastCheck = DateTime.UtcNow,
            ErrorMessage = _errorCount > 0 ? $"{_errorCount} errors occurred" : null
        };
    }

    public ConnectorMetrics GetMetrics()
    {
        return new ConnectorMetrics
        {
            RecordsProcessed = _recordsProcessed,
            BytesProcessed = _bytesProcessed,
            ErrorCount = _errorCount,
            Uptime = DateTime.UtcNow - _startTime
        };
    }
}

// ============== Webhook Sink Connector ==============

public class WebhookSinkConnector : ICustomSinkConnector
{
    private string _name = "webhook-sink";
    private string _webhookUrl = "";
    private int _batchSize = 100;
    private List<ConnectorRecord> _batch = new();
    private DateTime _startTime = DateTime.UtcNow;
    private long _recordsProcessed = 0;
    private long _bytesProcessed = 0;
    private long _errorCount = 0;

    public string Name => _name;

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("name", out var name)) _name = name;
        if (config.TryGetValue("webhook.url", out var url)) _webhookUrl = url;
        if (config.TryGetValue("batch.size", out var size)) _batchSize = int.Parse(size);
        _startTime = DateTime.UtcNow;
    }

    public Task ValidateAsync()
    {
        if (string.IsNullOrEmpty(_webhookUrl))
            throw new InvalidOperationException("Webhook URL is required");
        return Task.CompletedTask;
    }

    public async Task ProcessAsync(IEnumerable<ConnectorRecord> records)
    {
        foreach (var record in records)
        {
            _batch.Add(record);
            _recordsProcessed++;
            _bytesProcessed += record.Value.Length;

            if (_batch.Count >= _batchSize)
            {
                await FlushAsync();
            }
        }
    }

    public Task FlushAsync()
    {
        if (_batch.Count == 0)
            return Task.CompletedTask;

        // Simulate webhook POST
        Console.WriteLine($"  [WEBHOOK] POST to {_webhookUrl}");
        Console.WriteLine($"    Batch size: {_batch.Count} records");

        foreach (var record in _batch)
        {
            var json = Encoding.UTF8.GetString(record.Value);
            Console.WriteLine($"    -> {record.Key}: {json}");
        }

        _batch.Clear();
        return Task.CompletedTask;
    }

    public ConnectorHealth GetHealth()
    {
        return new ConnectorHealth
        {
            IsHealthy = true,
            Status = "Ready",
            LastCheck = DateTime.UtcNow,
            ErrorMessage = _errorCount > 0 ? $"{_errorCount} errors" : null
        };
    }

    public ConnectorMetrics GetMetrics()
    {
        return new ConnectorMetrics
        {
            RecordsProcessed = _recordsProcessed,
            BytesProcessed = _bytesProcessed,
            ErrorCount = _errorCount,
            Uptime = DateTime.UtcNow - _startTime
        };
    }
}

// ============== Timer Source Connector ==============

public class TimerSourceConnector : ICustomSourceConnector
{
    private string _name = "timer-source";
    private string _topic = "timer-data";
    private int _intervalMs = 1000;
    private DateTime _startTime;
    private long _recordsProcessed = 0;
    private CancellationTokenSource? _cts;

    public string Name => _name;
    public event Action<string, string, byte[]>? OnRecord;

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("name", out var name)) _name = name;
        if (config.TryGetValue("topic", out var topic)) _topic = topic;
        if (config.TryGetValue("interval.ms", out var interval))
            _intervalMs = int.Parse(interval);
    }

    public Task ValidateAsync() => Task.CompletedTask;

    public async Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _startTime = DateTime.UtcNow;
        var counter = 0;

        while (!_cts.Token.IsCancellationRequested)
        {
            var data = JsonSerializer.SerializeToUtf8Bytes(new
            {
                tick = counter++,
                timestamp = DateTime.UtcNow
            });

            OnRecord?.Invoke(_topic, counter.ToString(), data);
            _recordsProcessed++;

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

    public ConnectorHealth GetHealth() => new()
    {
        IsHealthy = true,
        Status = "Running",
        LastCheck = DateTime.UtcNow
    };

    public ConnectorMetrics GetMetrics() => new()
    {
        RecordsProcessed = _recordsProcessed,
        Uptime = DateTime.UtcNow - _startTime
    };
}

// ============== Random Data Source Connector ==============

public class RandomDataSourceConnector : ICustomSourceConnector
{
    private string _name = "random-source";
    private string _topic = "random-data";
    private int _recordsPerPoll = 1;
    private int _pollIntervalMs = 500;
    private DateTime _startTime;
    private long _recordsProcessed = 0;
    private CancellationTokenSource? _cts;

    public string Name => _name;
    public event Action<string, string, byte[]>? OnRecord;

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("name", out var name)) _name = name;
        if (config.TryGetValue("topic", out var topic)) _topic = topic;
        if (config.TryGetValue("records.per.poll", out var rpp))
            _recordsPerPoll = int.Parse(rpp);
        if (config.TryGetValue("poll.interval.ms", out var interval))
            _pollIntervalMs = int.Parse(interval);
    }

    public Task ValidateAsync() => Task.CompletedTask;

    public async Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _startTime = DateTime.UtcNow;

        while (!_cts.Token.IsCancellationRequested)
        {
            for (int i = 0; i < _recordsPerPoll; i++)
            {
                var data = JsonSerializer.SerializeToUtf8Bytes(new
                {
                    id = Guid.NewGuid().ToString("N")[..8],
                    value = Random.Shared.NextDouble() * 100,
                    timestamp = DateTime.UtcNow
                });

                var key = $"random-{_recordsProcessed}";
                OnRecord?.Invoke(_topic, key, data);
                _recordsProcessed++;
            }

            try
            {
                await Task.Delay(_pollIntervalMs, _cts.Token);
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

    public ConnectorHealth GetHealth() => new()
    {
        IsHealthy = true,
        Status = "Running",
        LastCheck = DateTime.UtcNow
    };

    public ConnectorMetrics GetMetrics() => new()
    {
        RecordsProcessed = _recordsProcessed,
        Uptime = DateTime.UtcNow - _startTime
    };
}

// ============== Console Sink Connector ==============

public class ConsoleSinkConnector : ICustomSinkConnector
{
    private string _name = "console-sink";
    private string _prefix = "[SINK]";
    private DateTime _startTime = DateTime.UtcNow;
    private long _recordsProcessed = 0;
    private long _bytesProcessed = 0;

    public string Name => _name;

    public void Configure(Dictionary<string, string> config)
    {
        if (config.TryGetValue("name", out var name)) _name = name;
        if (config.TryGetValue("prefix", out var prefix)) _prefix = prefix;
        _startTime = DateTime.UtcNow;
    }

    public Task ValidateAsync() => Task.CompletedTask;

    public Task ProcessAsync(IEnumerable<ConnectorRecord> records)
    {
        foreach (var record in records)
        {
            var json = Encoding.UTF8.GetString(record.Value);
            Console.WriteLine($"  {_prefix} {record.Key}: {json}");
            _recordsProcessed++;
            _bytesProcessed += record.Value.Length;
        }
        return Task.CompletedTask;
    }

    public Task FlushAsync() => Task.CompletedTask;

    public ConnectorHealth GetHealth() => new()
    {
        IsHealthy = true,
        Status = "Ready",
        LastCheck = DateTime.UtcNow
    };

    public ConnectorMetrics GetMetrics() => new()
    {
        RecordsProcessed = _recordsProcessed,
        BytesProcessed = _bytesProcessed,
        Uptime = DateTime.UtcNow - _startTime
    };
}

// ============== Connector Factory ==============

public class ConnectorFactory
{
    private readonly Dictionary<string, Func<ICustomSourceConnector>> _sourceFactories = new()
    {
        ["http"] = () => new HttpPollingSourceConnector(),
        ["timer"] = () => new TimerSourceConnector()
    };

    private readonly Dictionary<string, Func<ICustomSinkConnector>> _sinkFactories = new()
    {
        ["webhook"] = () => new WebhookSinkConnector(),
        ["console"] = () => new ConsoleSinkConnector()
    };

    public ICustomSourceConnector CreateSource(string type, Dictionary<string, string> config)
    {
        if (!_sourceFactories.TryGetValue(type, out var factory))
            throw new ArgumentException($"Unknown source connector type: {type}");

        var connector = factory();
        connector.Configure(config);
        return connector;
    }

    public ICustomSinkConnector CreateSink(string type, Dictionary<string, string> config)
    {
        if (!_sinkFactories.TryGetValue(type, out var factory))
            throw new ArgumentException($"Unknown sink connector type: {type}");

        var connector = factory();
        connector.Configure(config);
        return connector;
    }

    public void RegisterSource(string type, Func<ICustomSourceConnector> factory)
    {
        _sourceFactories[type] = factory;
    }

    public void RegisterSink(string type, Func<ICustomSinkConnector> factory)
    {
        _sinkFactories[type] = factory;
    }

    public IEnumerable<string> GetAvailableSourceTypes() => _sourceFactories.Keys;
    public IEnumerable<string> GetAvailableSinkTypes() => _sinkFactories.Keys;
}
