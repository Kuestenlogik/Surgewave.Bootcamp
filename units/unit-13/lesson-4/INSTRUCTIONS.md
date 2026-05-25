# Lesson 13.4: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Understand the Starting Code

Open `start/Program.cs`. You'll see:

```csharp
// TODO: Add using statements

var builder = WebApplication.CreateBuilder(args);

// TODO: Register services

var app = builder.Build();

// TODO: Add correlation middleware

// TODO: Add API endpoints

app.Run();

// TODO: Implement CorrelationMiddleware

// TODO: Implement DTOs
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
using System.Text.Json;
```

---

## Step 3: Create the Correlation Middleware

This middleware extracts or generates correlation IDs:

```csharp
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
        // Try to get correlation ID from request header
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();

        // Generate new ID if not provided
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        // Store in HttpContext for use by endpoints
        context.Items[CorrelationIdKey] = correlationId;

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // Create a logging scope with correlation ID
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
```

**What this does:**
- Extracts correlation ID from incoming request header
- Generates a new GUID if no correlation ID provided
- Stores the ID in HttpContext.Items for endpoint access
- Adds the correlation ID to the response header
- Creates a logging scope so all logs include the correlation ID

---

## Step 4: Create Extension Method for Middleware

```csharp
public static class CorrelationMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationMiddleware>();
    }
}
```

---

## Step 5: Create DTOs

Define request and response models:

```csharp
public record ProduceRequest(string Key, string Value);

public record ProduceResponse(
    bool Success,
    string CorrelationId,
    string Topic,
    string Key,
    string? Error = null);

public record TopicInfo(string Name, int PartitionCount);
```

---

## Step 6: Configure Services

Set up the service container:

```csharp
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
```

---

## Step 7: Add Middleware to Pipeline

Configure the middleware pipeline:

```csharp
// Add correlation middleware early in the pipeline
app.UseCorrelation();

// Add routing
app.UseRouting();
```

---

## Step 8: Create API Endpoints

Add Minimal API endpoints:

```csharp
// Root endpoint
app.MapGet("/", () => "Surgewave Middleware & Endpoints Demo");

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
```

---

## Complete Solution

Your completed `Program.cs` should look like this:

```csharp
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
```

---

## Run and Verify

```bash
cd start
dotnet run
```

Test the endpoints:

```bash
# Produce a single message
curl -X POST http://localhost:5000/api/topics/orders/messages \
  -H "Content-Type: application/json" \
  -d '{"key":"order-1","value":"New order"}'

# Produce with custom correlation ID
curl -X POST http://localhost:5000/api/topics/orders/messages \
  -H "Content-Type: application/json" \
  -H "X-Correlation-ID: my-custom-id" \
  -d '{"key":"order-2","value":"Another order"}'

# Batch produce
curl -X POST http://localhost:5000/api/topics/orders/messages/batch \
  -H "Content-Type: application/json" \
  -d '[{"key":"order-3","value":"Order 3"},{"key":"order-4","value":"Order 4"}]'
```

---

## Knowledge Check

Before moving on, make sure you can answer:

1. Why is correlation ID useful for distributed tracing?
2. At what point in the pipeline should correlation middleware run?
3. How do you access HttpContext.Items in a Minimal API endpoint?
4. What's the benefit of using Results.Created vs Results.Ok?

---

**Ready for more?** -> [Lesson 13.5: Background Processing](../lesson-5/README.md)
