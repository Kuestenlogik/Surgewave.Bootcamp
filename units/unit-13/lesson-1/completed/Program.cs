// Surgewave Bootcamp - Unit 13, Lesson 1: Hosted Services
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
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
