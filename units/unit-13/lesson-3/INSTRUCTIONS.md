# Lesson 13.3: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Understand the Starting Code

Open `start/Program.cs`. You'll see:

```csharp
// TODO: Add using statements

var builder = WebApplication.CreateBuilder(args);

// TODO: Register Surgewave broker service

// TODO: Register Surgewave producer

// TODO: Add health checks

var app = builder.Build();

// TODO: Map health check endpoints

app.Run();

// TODO: Implement SurgewaveBrokerHealthCheck

// TODO: Implement SurgewaveProducerHealthCheck
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
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
```

---

## Step 3: Create the Broker Health Check

This health check verifies the broker is reachable:

```csharp
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
            // Check if broker port is available (broker is running)
            if (_broker.Port <= 0)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Surgewave broker is not running"));
            }

            // For a real implementation, you would try to connect
            // Here we just check the port is assigned
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
```

**What this does:**
- Injects the broker service to check its status
- Returns Healthy if the broker has a valid port
- Returns Unhealthy if the broker isn't running

---

## Step 4: Create the Producer Health Check

This health check verifies the producer can send messages:

```csharp
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
            // Try to send a health check message
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
```

**What this does:**
- Attempts to send a test message to a health check topic
- Returns Healthy if the message sends successfully
- Returns Unhealthy if sending fails

---

## Step 5: Create a Consumer Lag Health Check (Optional)

For more advanced monitoring:

```csharp
public class SurgewaveConsumerLagHealthCheck : IHealthCheck
{
    private readonly ILogger<SurgewaveConsumerLagHealthCheck> _logger;
    private readonly int _warningThreshold;
    private readonly int _criticalThreshold;

    // In a real app, you'd track actual lag from consumers
    private static long _simulatedLag = 0;

    public SurgewaveConsumerLagHealthCheck(
        ILogger<SurgewaveConsumerLagHealthCheck> logger,
        int warningThreshold = 100,
        int criticalThreshold = 1000)
    {
        _logger = logger;
        _warningThreshold = warningThreshold;
        _criticalThreshold = criticalThreshold;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // In production, you'd query actual consumer group offsets
        var lag = _simulatedLag;

        if (lag >= _criticalThreshold)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Consumer lag is critical: {lag} messages behind"));
        }

        if (lag >= _warningThreshold)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Consumer lag is high: {lag} messages behind"));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Consumer lag is acceptable: {lag} messages"));
    }

    // For testing purposes
    public static void SetSimulatedLag(long lag) => _simulatedLag = lag;
}
```

---

## Step 6: Configure Services and Health Checks

Update Program.cs to register everything:

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

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<SurgewaveBrokerHealthCheck>("surgewave-broker", tags: new[] { "surgewave", "broker" })
    .AddCheck<SurgewaveProducerHealthCheck>("surgewave-producer", tags: new[] { "surgewave", "producer" });

var app = builder.Build();
```

---

## Step 7: Map Health Check Endpoints

Add endpoints to expose health status:

```csharp
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
```

---

## Step 8: Create Custom Response Writer

Add a method to format health check responses as JSON:

```csharp
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
```

---

## Complete Solution

Your completed `Program.cs` should look like this:

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
```

---

## Run and Verify

```bash
cd start
dotnet run
```

Test the health endpoints:

```bash
# Basic health check
curl http://localhost:5000/health
# Response: Healthy

# Detailed health check
curl http://localhost:5000/health/detail

# Surgewave-specific checks
curl http://localhost:5000/health/surgewave
```

---

## Knowledge Check

Before moving on, make sure you can answer:

1. What are the three possible HealthCheckResult states?
2. When would you use Degraded vs Unhealthy?
3. How do tags help organize health checks?
4. Why is it important to have health checks in production?

---

**Ready for more?** -> [Lesson 13.4: Middleware & Endpoints](../lesson-4/README.md)
