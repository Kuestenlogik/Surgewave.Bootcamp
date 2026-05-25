# Lesson 13.2: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Understand the Starting Code

Open `start/Program.cs`. You'll see:

```csharp
// TODO: Add using statements

var builder = WebApplication.CreateBuilder(args);

// TODO: Configure Surgewave options from configuration

// TODO: Register Surgewave broker service

// TODO: Register Surgewave producer using extension method

var app = builder.Build();

// TODO: Add endpoint to produce messages

app.Run();

// TODO: Create SurgewaveConfiguration class

// TODO: Create SurgewaveServiceCollectionExtensions class
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
using Microsoft.Extensions.Options;
```

---

## Step 3: Create the Configuration Class

Create a class to hold Surgewave configuration:

```csharp
public class SurgewaveConfiguration
{
    public const string SectionName = "Surgewave";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string? DefaultGroupId { get; set; }
}
```

This allows configuration via appsettings.json:
```json
{
  "Surgewave": {
    "BootstrapServers": "localhost:9092",
    "DefaultGroupId": "my-app-group"
  }
}
```

---

## Step 4: Create Extension Methods

Create extension methods for clean service registration:

```csharp
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
        this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IOptions<SurgewaveConfiguration>>().Value;
            var logger = sp.GetRequiredService<ILogger<SurgewaveProducer<TKey, TValue>>>();

            logger.LogInformation(
                "Creating Surgewave producer for {Bootstrap}",
                config.BootstrapServers);

            return new SurgewaveProducer<TKey, TValue>(options =>
            {
                options.BootstrapServers = config.BootstrapServers;
            });
        });

        return services;
    }

    public static IServiceCollection AddSurgewaveConsumer<TKey, TValue>(
        this IServiceCollection services,
        string groupId)
    {
        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IOptions<SurgewaveConfiguration>>().Value;
            var logger = sp.GetRequiredService<ILogger<SurgewaveConsumer<TKey, TValue>>>();

            logger.LogInformation(
                "Creating Surgewave consumer for {Bootstrap} with group {GroupId}",
                config.BootstrapServers, groupId);

            return new SurgewaveConsumer<TKey, TValue>(options =>
            {
                options.BootstrapServers = config.BootstrapServers;
                options.GroupId = groupId;
                options.AutoOffsetReset = AutoOffsetReset.Earliest;
            });
        });

        return services;
    }
}
```

**What this does:**
- `AddSurgewaveConfiguration` binds the "Surgewave" section from configuration
- `AddSurgewaveProducer` registers a singleton producer
- `AddSurgewaveConsumer` registers a singleton consumer with a group ID
- Both use IOptions to get configuration values

---

## Step 5: Create the Broker Service

Reuse the broker service from Lesson 13.1:

```csharp
public class SurgewaveBrokerService : IHostedService, IAsyncDisposable
{
    private SurgewaveRuntime? _runtime;
    private readonly ILogger<SurgewaveBrokerService> _logger;
    private readonly SurgewaveConfiguration _config;

    public int Port => _runtime?.Port ?? 0;

    public SurgewaveBrokerService(
        IOptions<SurgewaveConfiguration> config,
        ILogger<SurgewaveBrokerService> logger)
    {
        _config = config.Value;
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

## Step 6: Create a Message DTO

Create a simple DTO for the API:

```csharp
public record MessageRequest(string Key, string Value);
public record MessageResponse(bool Success, string Message);
```

---

## Step 7: Configure Services in Program.cs

Update the main Program.cs:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure Surgewave options from configuration
builder.Services.AddSurgewaveConfiguration(builder.Configuration);

// Register Surgewave broker service
builder.Services.AddSingleton<SurgewaveBrokerService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SurgewaveBrokerService>());

// We need to update configuration with the dynamic port after broker starts
// So we register a producer factory instead
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
```

---

## Step 8: Add API Endpoints

Add endpoints to produce messages:

```csharp
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
```

---

## Complete Solution

Your completed `Program.cs` should look like this:

```csharp
using Kuestenlogik.Surgewave.Broker;
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
        Action<SurgewaveProducerOptions>? configure = null)
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

In another terminal, test the API:

```bash
# Send a message
curl -X POST http://localhost:5000/messages \
  -H "Content-Type: application/json" \
  -d '{"key":"test-key","value":"Hello from DI!"}'

# Expected response
{"success":true,"message":"Message sent successfully"}
```

---

## Knowledge Check

Before moving on, make sure you can answer:

1. Why should Surgewave producers typically be registered as singletons?
2. When might you use scoped lifetime for a Surgewave consumer?
3. How does the IOptions pattern help with configuration?
4. What happens if you try to inject a service before it's registered?

---

**Ready for more?** -> [Lesson 13.3: Health Checks](../lesson-3/README.md)
