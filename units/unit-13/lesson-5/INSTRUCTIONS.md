# Lesson 13.5: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Understand the Starting Code

Open `start/Program.cs`. You'll see:

```csharp
// TODO: Add using statements

var builder = WebApplication.CreateBuilder(args);

// TODO: Configure shutdown timeout

// TODO: Register services

var app = builder.Build();

// TODO: Add endpoints

app.Run();

// TODO: Implement ProducerPool

// TODO: Implement ConsumerWorkerService

// TODO: Implement MessageGeneratorService
```

Your job is to replace the `// TODO` comments with working code.

---

## Step 2: Add Using Statements

At the top of the file, add:

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using System.Collections.Concurrent;
using System.Threading.Channels;
```

---

## Step 3: Create the Producer Pool

A pool of producers for high-throughput scenarios:

```csharp
public class ProducerPool<TKey, TValue> : IAsyncDisposable
{
    private readonly SurgewaveProducer<TKey, TValue>[] _producers;
    private readonly ILogger<ProducerPool<TKey, TValue>> _logger;
    private int _currentIndex = 0;

    public ProducerPool(
        int poolSize,
        string bootstrapServers,
        ILogger<ProducerPool<TKey, TValue>> logger)
    {
        _logger = logger;
        _producers = new SurgewaveProducer<TKey, TValue>[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            _producers[i] = new SurgewaveProducer<TKey, TValue>(options =>
            {
                options.BootstrapServers = bootstrapServers;
            });
        }

        _logger.LogInformation("Producer pool initialized with {PoolSize} producers", poolSize);
    }

    public async Task ProduceAsync(string topic, TKey key, TValue value)
    {
        // Round-robin selection
        var index = Interlocked.Increment(ref _currentIndex) % _producers.Length;
        var producer = _producers[index];

        await producer.ProduceAsync(topic, key, value);
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing producer pool...");

        foreach (var producer in _producers)
        {
            await producer.DisposeAsync();
        }
    }
}
```

**What this does:**
- Creates multiple producer instances
- Distributes work using round-robin selection
- Disposes all producers on shutdown

---

## Step 4: Create the Producer Pool Service

A hosted service that manages the producer pool:

```csharp
public class ProducerPoolService : IHostedService
{
    private ProducerPool<string, string>? _pool;
    private readonly SurgewaveBrokerService _broker;
    private readonly ILogger<ProducerPoolService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ProducerPool<string, string>? Pool => _pool;

    public ProducerPoolService(
        SurgewaveBrokerService broker,
        IServiceProvider serviceProvider,
        ILogger<ProducerPoolService> logger)
    {
        _broker = broker;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var poolLogger = _serviceProvider
            .GetRequiredService<ILogger<ProducerPool<string, string>>>();

        _pool = new ProducerPool<string, string>(
            poolSize: 3,
            bootstrapServers: $"localhost:{_broker.Port}",
            logger: poolLogger);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping producer pool service...");

        if (_pool != null)
        {
            await _pool.DisposeAsync();
        }
    }
}
```

---

## Step 5: Create the Consumer Worker Service

A service that runs multiple consumer workers:

```csharp
public class ConsumerWorkerService : BackgroundService
{
    private readonly SurgewaveBrokerService _broker;
    private readonly ILogger<ConsumerWorkerService> _logger;
    private readonly int _workerCount;
    private readonly string _topic;
    private readonly string _groupId;

    public ConsumerWorkerService(
        SurgewaveBrokerService broker,
        ILogger<ConsumerWorkerService> logger,
        int workerCount = 3,
        string topic = "demo-topic",
        string groupId = "demo-group")
    {
        _broker = broker;
        _logger = logger;
        _workerCount = workerCount;
        _topic = topic;
        _groupId = groupId;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for broker to be ready
        await Task.Delay(500, stoppingToken);

        // Start multiple worker tasks
        var workers = new Task[_workerCount];

        for (int i = 0; i < _workerCount; i++)
        {
            var workerId = i + 1;
            workers[i] = RunWorkerAsync(workerId, stoppingToken);
        }

        // Wait for all workers to complete
        await Task.WhenAll(workers);
    }

    private async Task RunWorkerAsync(int workerId, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting consumer worker {WorkerId}...", workerId);

        await using var consumer = new SurgewaveConsumer<string, string>(options =>
        {
            options.BootstrapServers = $"localhost:{_broker.Port}";
            options.GroupId = _groupId;
            options.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        consumer.Subscribe(_topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(1));

                if (result != null)
                {
                    // Simulate processing
                    await ProcessMessageAsync(workerId, result.Key, result.Value);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker {WorkerId}: Error processing message", workerId);
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("Worker {WorkerId}: Stopped", workerId);
    }

    private async Task ProcessMessageAsync(int workerId, string key, string value)
    {
        _logger.LogInformation(
            "Worker {WorkerId}: Processing message {Key} = {Value}",
            workerId, key, value);

        // Simulate some work
        await Task.Delay(100);
    }
}
```

---

## Step 6: Create the Message Generator Service

A service that generates test messages:

```csharp
public class MessageGeneratorService : BackgroundService
{
    private readonly ProducerPoolService _producerPoolService;
    private readonly ILogger<MessageGeneratorService> _logger;
    private readonly string _topic;

    public MessageGeneratorService(
        ProducerPoolService producerPoolService,
        ILogger<MessageGeneratorService> logger,
        string topic = "demo-topic")
    {
        _producerPoolService = producerPoolService;
        _logger = logger;
        _topic = topic;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for producer pool to be ready
        await Task.Delay(1000, stoppingToken);

        _logger.LogInformation("Starting message generator...");

        int messageCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pool = _producerPoolService.Pool;
                if (pool == null)
                {
                    await Task.Delay(100, stoppingToken);
                    continue;
                }

                messageCount++;
                var key = $"key-{messageCount}";
                var value = $"Message {messageCount} at {DateTime.UtcNow:HH:mm:ss}";

                await pool.ProduceAsync(_topic, key, value);
                _logger.LogDebug("Generated message {Key}", key);

                await Task.Delay(500, stoppingToken);  // Generate 2 messages per second
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating message");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("Message generator stopped");
    }
}
```

---

## Step 7: Configure Services in Program.cs

Set up all the services:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure shutdown timeout
builder.Host.ConfigureHostOptions(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

// Register Surgewave broker service
builder.Services.AddSingleton<SurgewaveBrokerService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SurgewaveBrokerService>());

// Register producer pool service
builder.Services.AddSingleton<ProducerPoolService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ProducerPoolService>());

// Register consumer worker service
builder.Services.AddSingleton(sp => new ConsumerWorkerService(
    sp.GetRequiredService<SurgewaveBrokerService>(),
    sp.GetRequiredService<ILogger<ConsumerWorkerService>>(),
    workerCount: 3,
    topic: "demo-topic",
    groupId: "demo-group"));
builder.Services.AddHostedService(sp => sp.GetRequiredService<ConsumerWorkerService>());

// Register message generator
builder.Services.AddHostedService<MessageGeneratorService>();

var app = builder.Build();

app.MapGet("/", () => "Surgewave Background Processing Demo - Messages are being processed in the background");

app.MapGet("/stats", (ProducerPoolService poolService) =>
{
    return new
    {
        ProducerPoolActive = poolService.Pool != null,
        Status = "Running"
    };
});

app.Run();
```

---

## Complete Solution

Your completed `Program.cs` should look like this:

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// Configure shutdown timeout
builder.Host.ConfigureHostOptions(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

// Register Surgewave broker service
builder.Services.AddSingleton<SurgewaveBrokerService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SurgewaveBrokerService>());

// Register producer pool service
builder.Services.AddSingleton<ProducerPoolService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ProducerPoolService>());

// Register consumer worker service
builder.Services.AddSingleton(sp => new ConsumerWorkerService(
    sp.GetRequiredService<SurgewaveBrokerService>(),
    sp.GetRequiredService<ILogger<ConsumerWorkerService>>(),
    workerCount: 3,
    topic: "demo-topic",
    groupId: "demo-group"));
builder.Services.AddHostedService(sp => sp.GetRequiredService<ConsumerWorkerService>());

// Register message generator
builder.Services.AddHostedService<MessageGeneratorService>();

var app = builder.Build();

app.MapGet("/", () => "Surgewave Background Processing Demo - Messages are being processed in the background");

app.MapGet("/stats", (ProducerPoolService poolService) =>
{
    return new
    {
        ProducerPoolActive = poolService.Pool != null,
        Status = "Running"
    };
});

app.Run();

// Broker service
public class SurgewaveBrokerService : IHostedService, IAsyncDisposable
{
    private SurgewaveRuntime? _runtime;
    private readonly ILogger<SurgewaveBrokerService> _logger;

    public int Port => _runtime?.Port ?? 0;

    public SurgewaveBrokerService(ILogger<SurgewaveBrokerService> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting embedded Surgewave broker...");

        _runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        _logger.LogInformation("Surgewave broker started on port {Port}", _runtime.Port);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Surgewave broker...");
        if (_runtime != null)
        {
            await _runtime.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_runtime != null)
        {
            await _runtime.DisposeAsync();
        }
    }
}

// Producer pool
public class ProducerPool<TKey, TValue> : IAsyncDisposable
{
    private readonly SurgewaveProducer<TKey, TValue>[] _producers;
    private readonly ILogger<ProducerPool<TKey, TValue>> _logger;
    private int _currentIndex = 0;

    public ProducerPool(
        int poolSize,
        string bootstrapServers,
        ILogger<ProducerPool<TKey, TValue>> logger)
    {
        _logger = logger;
        _producers = new SurgewaveProducer<TKey, TValue>[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            _producers[i] = new SurgewaveProducer<TKey, TValue>(options =>
            {
                options.BootstrapServers = bootstrapServers;
            });
        }

        _logger.LogInformation("Producer pool initialized with {PoolSize} producers", poolSize);
    }

    public async Task ProduceAsync(string topic, TKey key, TValue value)
    {
        var index = Interlocked.Increment(ref _currentIndex) % _producers.Length;
        var producer = _producers[index];
        await producer.ProduceAsync(topic, key, value);
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing producer pool...");
        foreach (var producer in _producers)
        {
            await producer.DisposeAsync();
        }
    }
}

// Producer pool service
public class ProducerPoolService : IHostedService
{
    private ProducerPool<string, string>? _pool;
    private readonly SurgewaveBrokerService _broker;
    private readonly ILogger<ProducerPoolService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ProducerPool<string, string>? Pool => _pool;

    public ProducerPoolService(
        SurgewaveBrokerService broker,
        IServiceProvider serviceProvider,
        ILogger<ProducerPoolService> logger)
    {
        _broker = broker;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var poolLogger = _serviceProvider
            .GetRequiredService<ILogger<ProducerPool<string, string>>>();

        _pool = new ProducerPool<string, string>(
            poolSize: 3,
            bootstrapServers: $"localhost:{_broker.Port}",
            logger: poolLogger);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping producer pool service...");
        if (_pool != null)
        {
            await _pool.DisposeAsync();
        }
    }
}

// Consumer worker service
public class ConsumerWorkerService : BackgroundService
{
    private readonly SurgewaveBrokerService _broker;
    private readonly ILogger<ConsumerWorkerService> _logger;
    private readonly int _workerCount;
    private readonly string _topic;
    private readonly string _groupId;

    public ConsumerWorkerService(
        SurgewaveBrokerService broker,
        ILogger<ConsumerWorkerService> logger,
        int workerCount = 3,
        string topic = "demo-topic",
        string groupId = "demo-group")
    {
        _broker = broker;
        _logger = logger;
        _workerCount = workerCount;
        _topic = topic;
        _groupId = groupId;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(500, stoppingToken);

        var workers = new Task[_workerCount];
        for (int i = 0; i < _workerCount; i++)
        {
            var workerId = i + 1;
            workers[i] = RunWorkerAsync(workerId, stoppingToken);
        }

        await Task.WhenAll(workers);
    }

    private async Task RunWorkerAsync(int workerId, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting consumer worker {WorkerId}...", workerId);

        await using var consumer = new SurgewaveConsumer<string, string>(options =>
        {
            options.BootstrapServers = $"localhost:{_broker.Port}";
            options.GroupId = _groupId;
            options.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        consumer.Subscribe(_topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(1));
                if (result != null)
                {
                    _logger.LogInformation(
                        "Worker {WorkerId}: Processed {Key} = {Value}",
                        workerId, result.Key, result.Value);
                    await Task.Delay(100, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker {WorkerId}: Error", workerId);
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("Worker {WorkerId}: Stopped", workerId);
    }
}

// Message generator service
public class MessageGeneratorService : BackgroundService
{
    private readonly ProducerPoolService _producerPoolService;
    private readonly ILogger<MessageGeneratorService> _logger;
    private readonly string _topic = "demo-topic";

    public MessageGeneratorService(
        ProducerPoolService producerPoolService,
        ILogger<MessageGeneratorService> logger)
    {
        _producerPoolService = producerPoolService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(1000, stoppingToken);
        _logger.LogInformation("Starting message generator...");

        int messageCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pool = _producerPoolService.Pool;
                if (pool == null)
                {
                    await Task.Delay(100, stoppingToken);
                    continue;
                }

                messageCount++;
                var key = $"key-{messageCount}";
                var value = $"Message {messageCount} at {DateTime.UtcNow:HH:mm:ss}";

                await pool.ProduceAsync(_topic, key, value);
                _logger.LogDebug("Generated message {Key}", key);

                await Task.Delay(500, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating message");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("Message generator stopped");
    }
}
```

---

## Run and Verify

```bash
cd start
dotnet run
```

Watch the logs show workers processing messages in parallel. Press Ctrl+C to see graceful shutdown.

---

## Knowledge Check

Before finishing, make sure you can answer:

1. Why use a producer pool instead of a single producer?
2. How does consumer group coordination work with multiple workers?
3. What happens if a worker crashes during message processing?
4. Why is shutdown timeout important?

---

**Congratulations!** You've completed Unit 13: ASP.NET Core Integration!
