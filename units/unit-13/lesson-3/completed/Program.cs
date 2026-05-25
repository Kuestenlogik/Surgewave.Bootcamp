// Surgewave Bootcamp - Unit 13, Lesson 3: Health Checks
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Register Surgewave broker service
builder.Services.AddSingleton<SurgewaveBrokerService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SurgewaveBrokerService>());

// Register Surgewave producer
builder.Services.AddSingleton(sp =>
{
    var broker = sp.GetRequiredService<SurgewaveBrokerService>();
    return new SurgewaveProducer<string, string>(options =>
    {
        options.BootstrapServers = $"localhost:{broker.Port}";
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<SurgewaveBrokerHealthCheck>("surgewave-broker", tags: new[] { "surgewave", "broker" })
    .AddCheck<SurgewaveProducerHealthCheck>("surgewave-producer", tags: new[] { "surgewave", "producer" });

var app = builder.Build();

// Basic health endpoint
app.MapHealthChecks("/health");

// Detailed health endpoint with JSON response
app.MapHealthChecks("/health/detail", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponse
});

// Health check for specific tags
app.MapHealthChecks("/health/surgewave", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("surgewave"),
    ResponseWriter = WriteHealthCheckResponse
});

app.MapGet("/", () => "Surgewave Health Check Demo - GET /health or /health/detail");

app.Run();

static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var response = new
    {
        status = report.Status.ToString(),
        totalDuration = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description,
            duration = e.Value.Duration.TotalMilliseconds,
            exception = e.Value.Exception?.Message
        })
    };

    var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
    {
        WriteIndented = true
    });

    return context.Response.WriteAsync(json);
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
            .WithStorageMode(StorageMode.Memory)
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

// Broker health check
public class SurgewaveBrokerHealthCheck : IHealthCheck
{
    private readonly SurgewaveBrokerService _broker;
    private readonly ILogger<SurgewaveBrokerHealthCheck> _logger;

    public SurgewaveBrokerHealthCheck(
        SurgewaveBrokerService broker,
        ILogger<SurgewaveBrokerHealthCheck> logger)
    {
        _broker = broker;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_broker.Port <= 0)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Surgewave broker is not running"));
            }

            _logger.LogDebug("Broker health check passed on port {Port}", _broker.Port);
            return Task.FromResult(HealthCheckResult.Healthy(
                $"Surgewave broker is reachable on port {_broker.Port}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Broker health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Surgewave broker health check failed: {ex.Message}"));
        }
    }
}

// Producer health check
public class SurgewaveProducerHealthCheck : IHealthCheck
{
    private readonly SurgewaveProducer<string, string> _producer;
    private readonly ILogger<SurgewaveProducerHealthCheck> _logger;

    public SurgewaveProducerHealthCheck(
        SurgewaveProducer<string, string> producer,
        ILogger<SurgewaveProducerHealthCheck> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthCheckTopic = "__health_check";
            var testKey = $"health-{DateTime.UtcNow.Ticks}";
            var testValue = "ping";

            await _producer.ProduceAsync(healthCheckTopic, testKey, testValue);

            _logger.LogDebug("Producer health check passed");
            return HealthCheckResult.Healthy("Producer can send messages");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Producer health check failed");
            return HealthCheckResult.Unhealthy(
                $"Producer cannot send messages: {ex.Message}");
        }
    }
}
