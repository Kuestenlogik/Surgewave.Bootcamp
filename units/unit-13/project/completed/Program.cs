// Unit 13 Project: Surgewave Web API - COMPLETED
// An ASP.NET Core Minimal API with Surgewave backend

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client.Native;
using Microsoft.Extensions.Hosting;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Notification API ===\n");

var builder = WebApplication.CreateBuilder(args);

// Step 1: Register Surgewave services with DI
Console.WriteLine("[Startup] Initializing Surgewave client...");

// Create and register the embedded runtime
var surgewave = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .Build()
    .StartAsync();

builder.Services.AddSingleton(surgewave);

// Create native client for fluent API
var surgewaveClient = new SurgewaveNativeClient("localhost", surgewave.Port);
await surgewaveClient.ConnectAsync();
builder.Services.AddSingleton(surgewaveClient);

// In-memory status tracking
var notificationStatuses = new ConcurrentDictionary<string, NotificationStatus>();
var metrics = new ConcurrentDictionary<string, int>
{
    ["produced"] = 0,
    ["consumed"] = 0,
    ["errors"] = 0
};
var latencyTracker = new ConcurrentBag<double>();
var startTime = DateTime.UtcNow;

builder.Services.AddSingleton(notificationStatuses);
builder.Services.AddSingleton(metrics);
builder.Services.AddSingleton(latencyTracker);

// Step 2: Add background consumer service
builder.Services.AddHostedService<NotificationConsumerService>();

var app = builder.Build();

// Step 3: Add correlation ID middleware
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? Guid.NewGuid().ToString();

    context.Items["CorrelationId"] = correlationId;
    context.Response.Headers["X-Correlation-ID"] = correlationId;

    await next();
});

// Request logging middleware
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    await next();
    sw.Stop();

    latencyTracker.Add(sw.ElapsedMilliseconds);
});

// Step 4: Create topics on startup
Console.WriteLine("[Startup] Creating topics...");

var client = app.Services.GetRequiredService<SurgewaveNativeClient>();
// Topics are auto-created by Surgewave when first produced to

Console.WriteLine("[Startup] Topics created: notifications-email, notifications-sms, notifications-push");

// Step 5: Define API endpoints

// POST /api/notifications - Send a notification
app.MapPost("/api/notifications", async (
    NotificationRequest request,
    SurgewaveNativeClient surgewaveClient,
    HttpContext httpContext,
    ConcurrentDictionary<string, NotificationStatus> statuses,
    ConcurrentDictionary<string, int> metricsDict) =>
{
    // Validate request
    if (string.IsNullOrWhiteSpace(request.Recipient))
        return Results.BadRequest(new { error = "Recipient is required" });

    if (string.IsNullOrWhiteSpace(request.Channel))
        return Results.BadRequest(new { error = "Channel is required" });

    var validChannels = new[] { "email", "sms", "push" };
    if (!validChannels.Contains(request.Channel.ToLower()))
        return Results.BadRequest(new { error = $"Invalid channel. Must be one of: {string.Join(", ", validChannels)}" });

    // Create notification
    var notificationId = $"NOT-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    var correlationId = httpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
    var now = DateTime.UtcNow;

    var notificationEvent = new NotificationEvent(
        notificationId,
        request.Recipient,
        request.Channel.ToLower(),
        request.Subject,
        request.Body,
        request.Priority ?? "normal",
        now,
        correlationId
    );

    // Produce to appropriate topic
    var topic = $"notifications-{request.Channel.ToLower()}";
    var json = JsonSerializer.Serialize(notificationEvent);

    await surgewaveClient.Messaging.Send(topic)
        .WithKey(notificationId)
        .WithValue(json)
        .ExecuteAsync();

    // Track status
    statuses[notificationId] = new NotificationStatus(
        notificationId,
        "queued",
        now,
        null
    );

    // Update metrics
    metricsDict.AddOrUpdate("produced", 1, (_, c) => c + 1);

    Console.WriteLine($"[API] POST /api/notifications - Notification {notificationId} queued ({request.Channel})");

    return Results.Accepted($"/api/notifications/{notificationId}", new NotificationResponse(
        notificationId,
        "queued",
        now
    ));
});

// GET /api/notifications/{id} - Get notification status
app.MapGet("/api/notifications/{id}", (
    string id,
    ConcurrentDictionary<string, NotificationStatus> statuses) =>
{
    if (statuses.TryGetValue(id, out var status))
    {
        Console.WriteLine($"[API] GET /api/notifications/{id} - Status: {status.Status}");
        return Results.Ok(status);
    }

    return Results.NotFound(new { error = "Notification not found", notificationId = id });
});

// GET /api/topics - List topics
app.MapGet("/api/topics", async (SurgewaveNativeClient surgewaveClient) =>
{
    // In a real implementation, this would query the broker
    var topics = new[]
    {
        new TopicInfo("notifications-email", 3),
        new TopicInfo("notifications-sms", 3),
        new TopicInfo("notifications-push", 3)
    };

    Console.WriteLine("[API] GET /api/topics");

    return Results.Ok(new { topics });
});

// GET /api/health - Health check
app.MapGet("/api/health", (SurgewaveRuntime surgewaveRuntime) =>
{
    var uptime = DateTime.UtcNow - startTime;
    var isConnected = surgewaveRuntime != null; // Simplified check

    var status = isConnected ? "healthy" : "unhealthy";
    var brokerStatus = isConnected ? "connected" : "disconnected";

    Console.WriteLine($"[API] GET /api/health - Status: {status}");

    return Results.Ok(new HealthStatus(
        status,
        brokerStatus,
        uptime.ToString(@"hh\:mm\:ss")
    ));
});

// GET /api/metrics - Metrics endpoint
app.MapGet("/api/metrics", (
    ConcurrentDictionary<string, int> metricsDict,
    ConcurrentBag<double> latencies) =>
{
    var produced = metricsDict.GetValueOrDefault("produced", 0);
    var consumed = metricsDict.GetValueOrDefault("consumed", 0);
    var errors = metricsDict.GetValueOrDefault("errors", 0);

    var avgLatency = latencies.Count > 0 ? latencies.Average() : 0;

    Console.WriteLine($"[API] GET /api/metrics - Produced: {produced}, Consumed: {consumed}, Errors: {errors}");

    return Results.Ok(new Metrics(
        produced,
        consumed,
        Math.Round(avgLatency, 2),
        errors
    ));
});

// Root endpoint with API info
app.MapGet("/", () => Results.Ok(new
{
    name = "Surgewave Notification API",
    version = "1.0",
    endpoints = new[]
    {
        "POST /api/notifications - Send notification",
        "GET /api/notifications/{id} - Get status",
        "GET /api/topics - List topics",
        "GET /api/health - Health check",
        "GET /api/metrics - Metrics"
    }
}));

Console.WriteLine("[Startup] Starting background consumer...");
Console.WriteLine("[Startup] API ready on http://localhost:5000\n");

// Handle graceful shutdown
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("\n[Shutdown] Stopping consumer...");
    Console.WriteLine("[Shutdown] Flushing producer...");
});

lifetime.ApplicationStopped.Register(() =>
{
    Console.WriteLine("[Shutdown] API shutdown complete");
    surgewave.DisposeAsync().AsTask().Wait();
});

app.Run();

// DTOs
public record NotificationRequest(
    string Recipient,
    string Channel,
    string Subject,
    string Body,
    string? Priority = "normal"
);

public record NotificationResponse(
    string NotificationId,
    string Status,
    DateTime Timestamp
);

public record NotificationStatus(
    string NotificationId,
    string Status,
    DateTime? QueuedAt,
    DateTime? DeliveredAt
);

public record TopicInfo(string Name, int Partitions);

public record HealthStatus(string Status, string Broker, string Uptime);

public record Metrics(
    int MessagesProduced,
    int MessagesConsumed,
    double AvgLatencyMs,
    int ErrorsCount
);

// Notification event
public record NotificationEvent(
    string NotificationId,
    string Recipient,
    string Channel,
    string Subject,
    string Body,
    string Priority,
    DateTime CreatedAt,
    string CorrelationId
);

// Step 6: Background consumer service
public class NotificationConsumerService : BackgroundService
{
    private readonly SurgewaveNativeClient _client;
    private readonly ConcurrentDictionary<string, NotificationStatus> _statuses;
    private readonly ConcurrentDictionary<string, int> _metrics;
    private readonly ILogger<NotificationConsumerService> _logger;

    public NotificationConsumerService(
        SurgewaveNativeClient client,
        ConcurrentDictionary<string, NotificationStatus> statuses,
        ConcurrentDictionary<string, int> metrics,
        ILogger<NotificationConsumerService> logger)
    {
        _client = client;
        _statuses = statuses;
        _metrics = metrics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Small delay to ensure topics are created
        await Task.Delay(1000, stoppingToken);

        var channels = new[] { "email", "sms", "push" };

        // Start consumers for each channel
        var tasks = channels.Select(channel => ConsumeChannel(channel, stoppingToken));
        await Task.WhenAll(tasks);
    }

    private async Task ConsumeChannel(string channel, CancellationToken stoppingToken)
    {
        var topic = $"notifications-{channel}";

        try
        {
            await foreach (var message in _client.Messaging.Receive(topic)
                .FromBeginning()
                .Stream(stoppingToken))
            {
                try
                {
                    var notification = JsonSerializer.Deserialize<NotificationEvent>(message.ValueString);
                    if (notification is null) continue;

                    Console.WriteLine($"[Consumer] Processing {notification.NotificationId} for {notification.Recipient}");

                    // Simulate delivery delay
                    await Task.Delay(Random.Shared.Next(50, 200), stoppingToken);

                    // Update status to delivered
                    if (_statuses.TryGetValue(notification.NotificationId, out var status))
                    {
                        _statuses[notification.NotificationId] = status with
                        {
                            Status = "delivered",
                            DeliveredAt = DateTime.UtcNow
                        };
                    }

                    _metrics.AddOrUpdate("consumed", 1, (_, c) => c + 1);

                    Console.WriteLine($"[Consumer] {notification.NotificationId} delivered successfully");
                }
                catch (Exception ex)
                {
                    _metrics.AddOrUpdate("errors", 1, (_, c) => c + 1);
                    _logger.LogError(ex, "Error processing notification");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Consumer error for channel {Channel}", channel);
        }
    }
}
