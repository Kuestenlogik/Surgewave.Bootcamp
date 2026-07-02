// Surgewave Bootcamp - Unit 13, Lesson 2: Dependency Injection
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Configure Surgewave options from configuration
builder.Services.AddSurgewaveConfiguration(builder.Configuration);

// Register Surgewave broker service
builder.Services.AddSingleton<SurgewaveBrokerService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SurgewaveBrokerService>());

// Register Surgewave producer (uses broker port)
builder.Services.AddSingleton(sp =>
{
    var broker = sp.GetRequiredService<SurgewaveBrokerService>();
    var logger = sp.GetRequiredService<ILogger<SurgewaveProducer<string, string>>>();

    logger.LogInformation("Creating Surgewave producer for port {Port}", broker.Port);

    return new SurgewaveProducer<string, string>(options =>
    {
        options.BootstrapServers = $"localhost:{broker.Port}";
    });
});

var app = builder.Build();

app.MapGet("/", () => "Surgewave DI Demo - POST to /messages to send a message");

app.MapPost("/messages", async (
    MessageRequest request,
    SurgewaveProducer<string, string> producer,
    ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Producing message with key: {Key}", request.Key);

        await producer.ProduceAsync("demo-messages", request.Key, request.Value);

        logger.LogInformation("Message produced successfully");
        return Results.Ok(new MessageResponse(true, "Message sent successfully"));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to produce message");
        return Results.BadRequest(new MessageResponse(false, ex.Message));
    }
});

app.Run();

// Configuration class
public class SurgewaveConfiguration
{
    public const string SectionName = "Surgewave";
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string? DefaultGroupId { get; set; }
}

// DTOs
public record MessageRequest(string Key, string Value);
public record MessageResponse(bool Success, string Message);

// Extension methods for clean registration
public static class SurgewaveServiceCollectionExtensions
{
    public static IServiceCollection AddSurgewaveConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SurgewaveConfiguration>(
            configuration.GetSection(SurgewaveConfiguration.SectionName));
        return services;
    }

    public static IServiceCollection AddSurgewaveProducer<TKey, TValue>(
        this IServiceCollection services,
        Action<SurgewaveProducerOptions<TKey, TValue>>? configure = null)
    {
        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IOptions<SurgewaveConfiguration>>().Value;

            return new SurgewaveProducer<TKey, TValue>(options =>
            {
                options.BootstrapServers = config.BootstrapServers;
                configure?.Invoke(options);
            });
        });

        return services;
    }
}

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
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        _logger.LogInformation("Surgewave broker started on port {Port}", _runtime.Port);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
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
