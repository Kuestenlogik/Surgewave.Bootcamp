// Unit 13 Project: Surgewave Web API
// Build an ASP.NET Core Minimal API with Surgewave backend

using System.Collections.Concurrent;
using System.Text.Json;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Notification API ===\n");

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

var builder = WebApplication.CreateBuilder(args);

// TODO: Step 1 - Register Surgewave services with DI
// Hint: Create an embedded Surgewave broker and producers/consumers:
//       var runtime = await SurgewaveRuntime.CreateBuilder()
//           .WithPort(0)
//           .WithStorageMode(StorageMode.Memory)
//           .WithAutoCreateTopics(true)
//           .Build()
//           .StartAsync();
//       builder.Services.AddSingleton(runtime);
//
//       var bootstrapServers = runtime.BootstrapServers;
//       var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
//       builder.Services.AddSingleton(producer);

Console.WriteLine("[Startup] Initializing Surgewave client...");


// In-memory status tracking (in production, use a database)
var notificationStatuses = new ConcurrentDictionary<string, NotificationStatus>();
var metrics = new ConcurrentDictionary<string, int>
{
    ["produced"] = 0,
    ["consumed"] = 0,
    ["errors"] = 0
};
var startTime = DateTime.UtcNow;

builder.Services.AddSingleton(notificationStatuses);
builder.Services.AddSingleton(metrics);

// TODO: Step 2 - Add background consumer service
// Hint: builder.Services.AddHostedService<NotificationConsumerService>()
// The consumer service should:
//   - Create a SurgewaveConsumer<string, string> with appropriate settings
//   - Subscribe to notification topics
//   - Consume and process messages in a loop


var app = builder.Build();

// TODO: Step 3 - Add correlation ID middleware
// Hint: app.Use(async (context, next) =>
// {
//     var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
//         ?? Guid.NewGuid().ToString();
//     context.Items["CorrelationId"] = correlationId;
//     context.Response.Headers["X-Correlation-ID"] = correlationId;
//     await next();
// });


// TODO: Step 4 - Create topics on startup (or use WithAutoCreateTopics)
Console.WriteLine("[Startup] Creating topics...");

// Topics will be auto-created when first message is produced


// TODO: Step 5 - Define API endpoints

// POST /api/notifications - Send a notification
app.MapPost("/api/notifications", async (NotificationRequest request) =>
{
    // TODO: Validate request
    // TODO: Create notification event
    // TODO: Produce to appropriate topic based on channel
    //       var topic = $"notifications-{request.Channel}";
    //       await producer.ProduceAsync(topic, notificationId, JsonSerializer.Serialize(notificationEvent));
    // TODO: Track status
    // TODO: Return response

    var notificationId = $"NOT-{Guid.NewGuid().ToString()[..8].ToUpper()}";

    Console.WriteLine($"[API] POST /api/notifications - Notification {notificationId} queued ({request.Channel})");

    return Results.Accepted($"/api/notifications/{notificationId}", new NotificationResponse(
        notificationId,
        "queued",
        DateTime.UtcNow
    ));
});

// GET /api/notifications/{id} - Get notification status
app.MapGet("/api/notifications/{id}", (string id) =>
{
    // TODO: Look up status from notificationStatuses dictionary

    if (notificationStatuses.TryGetValue(id, out var status))
    {
        return Results.Ok(status);
    }

    return Results.NotFound(new { error = "Notification not found" });
});

// GET /api/topics - List topics
app.MapGet("/api/topics", () =>
{
    // TODO: Return actual topic list from Surgewave client

    var topics = new[]
    {
        new TopicInfo("notifications-email", 3),
        new TopicInfo("notifications-sms", 3),
        new TopicInfo("notifications-push", 3)
    };

    return Results.Ok(new { topics });
});

// GET /api/health - Health check
app.MapGet("/api/health", () =>
{
    // TODO: Check actual broker connectivity

    var uptime = DateTime.UtcNow - startTime;

    Console.WriteLine($"[API] GET /api/health - Status: healthy");

    return Results.Ok(new HealthStatus(
        "healthy",
        "connected",
        uptime.ToString(@"hh\:mm\:ss")
    ));
});

// GET /api/metrics - Metrics endpoint
app.MapGet("/api/metrics", () =>
{
    var produced = metrics.GetValueOrDefault("produced", 0);
    var consumed = metrics.GetValueOrDefault("consumed", 0);
    var errors = metrics.GetValueOrDefault("errors", 0);

    Console.WriteLine($"[API] GET /api/metrics - Produced: {produced}, Consumed: {consumed}, Errors: {errors}");

    return Results.Ok(new Metrics(
        produced,
        consumed,
        12.5, // TODO: Calculate actual latency
        errors
    ));
});

Console.WriteLine("[Startup] API ready on http://localhost:5000\n");

app.Run();

// TODO: Step 6 - Implement background consumer service
// public class NotificationConsumerService : BackgroundService
// {
//     private readonly SurgewaveConsumer<string, string> _consumer;
//     private readonly ConcurrentDictionary<string, NotificationStatus> _statuses;
//     private readonly ConcurrentDictionary<string, int> _metrics;
//
//     public NotificationConsumerService(
//         string bootstrapServers,
//         ConcurrentDictionary<string, NotificationStatus> statuses,
//         ConcurrentDictionary<string, int> metrics)
//     {
//         _consumer = new SurgewaveConsumer<string, string>(opt =>
//         {
//             opt.BootstrapServers = bootstrapServers;
//             opt.GroupId = "notification-processor";
//             opt.AutoOffsetReset = Kuestenlogik.Surgewave.Client.Consumer.AutoOffsetReset.Earliest;
//         });
//         _consumer.Subscribe(new[] { "notifications-email", "notifications-sms", "notifications-push" });
//         _statuses = statuses;
//         _metrics = metrics;
//     }
//
//     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//         while (!stoppingToken.IsCancellationRequested)
//         {
//             var result = await _consumer.ConsumeAsync(stoppingToken);
//             if (result is null) continue;
//
//             var notification = JsonSerializer.Deserialize<NotificationEvent>(result.Value);
//             if (notification is null) continue;
//
//             // Update status to "delivered"
//             _statuses[notification.NotificationId] = new NotificationStatus(
//                 notification.NotificationId,
//                 "delivered",
//                 notification.CreatedAt,
//                 DateTime.UtcNow
//             );
//
//             // Increment metrics
//             _metrics.AddOrUpdate("consumed", 1, (_, c) => c + 1);
//         }
//     }
//
//     public override async Task StopAsync(CancellationToken cancellationToken)
//     {
//         await _consumer.DisposeAsync();
//         await base.StopAsync(cancellationToken);
//     }
// }
