// Surgewave Bootcamp - Unit 13, Lesson 4: Middleware & Endpoints
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
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

var app = builder.Build();

// Add correlation middleware early in the pipeline
app.UseCorrelation();

// Root endpoint
app.MapGet("/", () => "Surgewave Middleware & Endpoints Demo - POST to /api/topics/{topic}/messages");

// Produce a message to a topic
app.MapPost("/api/topics/{topic}/messages", async (
    string topic,
    ProduceRequest request,
    SurgewaveProducer<string, string> producer,
    HttpContext context,
    ILogger<Program> logger) =>
{
    var correlationId = context.Items[CorrelationMiddleware.CorrelationIdKey]?.ToString()
        ?? Guid.NewGuid().ToString();

    try
    {
        logger.LogInformation(
            "Producing message to {Topic} with key {Key}",
            topic, request.Key);

        await producer.ProduceAsync(topic, request.Key, request.Value);

        var response = new ProduceResponse(
            Success: true,
            CorrelationId: correlationId,
            Topic: topic,
            Key: request.Key);

        return Results.Created($"/api/topics/{topic}/messages/{request.Key}", response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to produce message to {Topic}", topic);

        var response = new ProduceResponse(
            Success: false,
            CorrelationId: correlationId,
            Topic: topic,
            Key: request.Key,
            Error: ex.Message);

        return Results.BadRequest(response);
    }
});

// Batch produce multiple messages
app.MapPost("/api/topics/{topic}/messages/batch", async (
    string topic,
    List<ProduceRequest> requests,
    SurgewaveProducer<string, string> producer,
    HttpContext context,
    ILogger<Program> logger) =>
{
    var correlationId = context.Items[CorrelationMiddleware.CorrelationIdKey]?.ToString()
        ?? Guid.NewGuid().ToString();

    var results = new List<ProduceResponse>();

    foreach (var request in requests)
    {
        try
        {
            await producer.ProduceAsync(topic, request.Key, request.Value);
            results.Add(new ProduceResponse(true, correlationId, topic, request.Key));
        }
        catch (Exception ex)
        {
            results.Add(new ProduceResponse(false, correlationId, topic, request.Key, ex.Message));
        }
    }

    var allSuccess = results.All(r => r.Success);
    return allSuccess ? Results.Ok(results) : Results.BadRequest(results);
});

app.Run();

// DTOs
public record ProduceRequest(string Key, string Value);

public record ProduceResponse(
    bool Success,
    string CorrelationId,
    string Topic,
    string Key,
    string? Error = null);

// Correlation middleware
public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationMiddleware> _logger;
    public const string CorrelationIdHeader = "X-Correlation-ID";
    public const string CorrelationIdKey = "CorrelationId";

    public CorrelationMiddleware(
        RequestDelegate next,
        ILogger<CorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();

        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        context.Items[CorrelationIdKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            [CorrelationIdKey] = correlationId
        }))
        {
            _logger.LogDebug("Request started with correlation ID: {CorrelationId}", correlationId);
            await _next(context);
        }
    }
}

public static class CorrelationMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationMiddleware>();
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
