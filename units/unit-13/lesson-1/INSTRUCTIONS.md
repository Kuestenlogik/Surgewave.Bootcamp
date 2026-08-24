# Lesson 13.1: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Understand the Starting Code

Open `start/Program.cs`. You'll see:

```csharp
// TODO: Add using statements for Surgewave and hosting

var builder = WebApplication.CreateBuilder(args);

// TODO: Register the embedded Surgewave broker as a hosted service

// TODO: Register the message producer service

// TODO: Register the message consumer service

var app = builder.Build();

app.MapGet("/", () => "Surgewave Consumer Service Running");

app.Run();

// TODO: Implement MessageConsumerService as a BackgroundService

// TODO: Implement MessageProducerService to generate test messages
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
```

These namespaces provide:
- `Kuestenlogik.Surgewave.Broker` - StorageMode enum
- `Kuestenlogik.Surgewave.Runtime` - Embedded SurgewaveRuntime
- `Kuestenlogik.Surgewave.Client` - SurgewaveProducer and SurgewaveConsumer
- `Kuestenlogik.Surgewave.Client.Consumer` - AutoOffsetReset enum

---

## Step 3: Create the SurgewaveBrokerService

This hosted service starts the embedded Surgewave broker:

```csharp
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
            .WithPort(0)  // Auto-assign port
            .WithStorageEngine(StorageEngines.Memory)
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
```

**What this does:**
- Implements `IHostedService` for lifecycle control
- Starts an embedded Surgewave broker on an available port
- Exposes the `Port` property for other services
- Cleans up properly on shutdown

---

## Step 4: Create the MessageConsumerService

This is the main consumer background service:

```csharp
public class MessageConsumerService : BackgroundService
{
    private readonly SurgewaveBrokerService _broker;
    private readonly ILogger<MessageConsumerService> _logger;

    public MessageConsumerService(
        SurgewaveBrokerService broker,
        ILogger<MessageConsumerService> logger)
    {
        _broker = broker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a moment for the broker to be ready
        await Task.Delay(500, stoppingToken);

        _logger.LogInformation("Starting message consumer...");

        await using var consumer = new SurgewaveConsumer<string, string>(options =>
        {
            options.BootstrapServers = $"localhost:{_broker.Port}";
            options.GroupId = "demo-consumer-group";
            options.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        consumer.Subscribe("demo-messages");
        _logger.LogInformation("Subscribed to topic: demo-messages");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(1));

                if (result != null)
                {
                    _logger.LogInformation("Received: {Value}", result.Value);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming message");
                await Task.Delay(1000, stoppingToken);  // Back off on error
            }
        }

        _logger.LogInformation("Message consumer stopped gracefully");
    }
}
```

**What this does:**
- Extends `BackgroundService` for simplified long-running work
- Creates a Surgewave consumer with the broker's port
- Subscribes to a topic and processes messages in a loop
- Handles cancellation gracefully

---

## Step 5: Create the MessageProducerService

This service produces test messages for the demo:

```csharp
public class MessageProducerService : BackgroundService
{
    private readonly SurgewaveBrokerService _broker;
    private readonly ILogger<MessageProducerService> _logger;

    public MessageProducerService(
        SurgewaveBrokerService broker,
        ILogger<MessageProducerService> logger)
    {
        _broker = broker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for broker and consumer to be ready
        await Task.Delay(1000, stoppingToken);

        _logger.LogInformation("Starting message producer...");

        await using var producer = new SurgewaveProducer<string, string>(options =>
        {
            options.BootstrapServers = $"localhost:{_broker.Port}";
        });

        int messageCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                messageCount++;
                var message = $"Message {messageCount} at {DateTime.UtcNow:u}";
                var key = $"key-{messageCount}";

                await producer.ProduceAsync("demo-messages", key, message);
                _logger.LogInformation("Produced: {Message}", message);

                await Task.Delay(2000, stoppingToken);  // Send a message every 2 seconds
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error producing message");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("Message producer stopped");
    }
}
```

---

## Step 6: Register Services in Program.cs

Update the service registration in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register the Surgewave broker as a singleton (shared instance)
builder.Services.AddSingleton<SurgewaveBrokerService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SurgewaveBrokerService>());

// Register producer and consumer as hosted services
builder.Services.AddHostedService<MessageProducerService>();
builder.Services.AddHostedService<MessageConsumerService>();

var app = builder.Build();

app.MapGet("/", () => "Surgewave Consumer Service Running");

app.Run();
```

**What this does:**
- Registers `SurgewaveBrokerService` as a singleton so it can be injected
- Also registers it as a hosted service so it starts/stops with the app
- Adds the producer and consumer as hosted services

---

## Complete Solution

Your completed `Program.cs` should look like this:

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

var builder = WebApplication.CreateBuilder(args);

// Register the Surgewave broker as a singleton (shared instance)
builder.Services.AddSingleton<SurgewaveBrokerService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SurgewaveBrokerService>());

// Register producer and consumer as hosted services
builder.Services.AddHostedService<MessageProducerService>();
builder.Services.AddHostedService<MessageConsumerService>();

var app = builder.Build();

app.MapGet("/", () => "Surgewave Consumer Service Running");

app.Run();

// Hosted service to manage the embedded Surgewave broker
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
            .WithStorageEngine(StorageEngines.Memory)
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

// Background service to consume messages
public class MessageConsumerService : BackgroundService
{
    private readonly SurgewaveBrokerService _broker;
    private readonly ILogger<MessageConsumerService> _logger;

    public MessageConsumerService(
        SurgewaveBrokerService broker,
        ILogger<MessageConsumerService> logger)
    {
        _broker = broker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(500, stoppingToken);

        _logger.LogInformation("Starting message consumer...");

        await using var consumer = new SurgewaveConsumer<string, string>(options =>
        {
            options.BootstrapServers = $"localhost:{_broker.Port}";
            options.GroupId = "demo-consumer-group";
            options.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        consumer.Subscribe("demo-messages");
        _logger.LogInformation("Subscribed to topic: demo-messages");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(1));

                if (result != null)
                {
                    _logger.LogInformation("Received: {Value}", result.Value);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming message");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("Message consumer stopped gracefully");
    }
}

// Background service to produce test messages
public class MessageProducerService : BackgroundService
{
    private readonly SurgewaveBrokerService _broker;
    private readonly ILogger<MessageProducerService> _logger;

    public MessageProducerService(
        SurgewaveBrokerService broker,
        ILogger<MessageProducerService> logger)
    {
        _broker = broker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(1000, stoppingToken);

        _logger.LogInformation("Starting message producer...");

        await using var producer = new SurgewaveProducer<string, string>(options =>
        {
            options.BootstrapServers = $"localhost:{_broker.Port}";
        });

        int messageCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                messageCount++;
                var message = $"Message {messageCount} at {DateTime.UtcNow:u}";
                var key = $"key-{messageCount}";

                await producer.ProduceAsync("demo-messages", key, message);
                _logger.LogInformation("Produced: {Message}", message);

                await Task.Delay(2000, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error producing message");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("Message producer stopped");
    }
}
```

---

## Run and Verify

```bash
cd start
dotnet run
```

Expected output:

```
info: SurgewaveBrokerService[0]
      Starting embedded Surgewave broker...
info: SurgewaveBrokerService[0]
      Surgewave broker started on port 54321
info: MessageConsumerService[0]
      Starting message consumer...
info: MessageConsumerService[0]
      Subscribed to topic: demo-messages
info: MessageProducerService[0]
      Starting message producer...
info: MessageProducerService[0]
      Produced: Message 1 at 2024-01-15T10:30:00Z
info: MessageConsumerService[0]
      Received: Message 1 at 2024-01-15T10:30:00Z
```

Press Ctrl+C to see graceful shutdown.

---

## Knowledge Check

Before moving on, make sure you can answer:

1. What's the difference between `IHostedService` and `BackgroundService`?
2. Why do we use `CancellationToken` in `ExecuteAsync`?
3. In what order do hosted services start and stop?
4. How do you handle exceptions in a background service?

---

**Ready for more?** -> [Lesson 13.2: Dependency Injection](../lesson-2/README.md)
