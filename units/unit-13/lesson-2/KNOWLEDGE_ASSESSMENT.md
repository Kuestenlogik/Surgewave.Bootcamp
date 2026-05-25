# Knowledge Assessment: Lesson 13.2 - Dependency Injection

## Multiple Choice

### 1. What is the recommended service lifetime for a Surgewave producer in ASP.NET Core?
- [ ] A) Transient
- [ ] B) Scoped
- [ ] C) Singleton
- [ ] D) It depends on the message size

<details>
<summary>Answer</summary>
**C)** Singleton is recommended for Surgewave producers because they are thread-safe and expensive to create. Sharing a single instance across all requests is efficient and avoids the overhead of creating new connections for each request.
</details>

### 2. What is the purpose of the IOptions<T> pattern in Surgewave configuration?
- [ ] A) To encrypt configuration values
- [ ] B) To bind configuration from appsettings.json to strongly-typed objects
- [ ] C) To increase producer performance
- [ ] D) To validate message schemas

<details>
<summary>Answer</summary>
**B)** The IOptions<T> pattern binds configuration values from sources like appsettings.json to strongly-typed configuration classes. This provides type safety, IntelliSense support, and centralized configuration management.
</details>

### 3. Why should you NOT use `using var` when injecting a singleton producer into a controller?
- [ ] A) It causes compilation errors
- [ ] B) The DI container manages the lifetime, and disposing early breaks other requests
- [ ] C) Producers cannot be disposed
- [ ] D) It reduces performance

<details>
<summary>Answer</summary>
**B)** When the DI container manages an object's lifetime (singleton), you should not dispose it manually with `using`. Disposing early would break the producer for all subsequent requests since the singleton is shared.
</details>

## True/False

### 4. Scoped services create a new instance for each HTTP request.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Scoped services create one instance per request (or per scope). This is useful for services that need to maintain state during a single request, like DbContext, but should not share state across requests.
</details>

### 5. Extension methods for service registration should always register services as transient.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Extension methods should register services with the appropriate lifetime for their use case. Surgewave producers are typically singleton, while message handlers that need DbContext might be scoped. Choose the lifetime based on the service's characteristics.
</details>

## Code Challenge

### 6. What is wrong with this dependency injection setup?

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

var producer = new SurgewaveProducer<string, string>(opt =>
    opt.BootstrapServers = "localhost:9092");
builder.Services.AddSingleton(producer);

var app = builder.Build();

// Controller
public class MessagesController : ControllerBase
{
    private readonly SurgewaveProducer<string, string> _producer;

    public MessagesController(SurgewaveProducer<string, string> producer)
    {
        using (_producer = producer)  // Problem here!
        {
        }
    }
}
```

<details>
<summary>Answer</summary>
Issues:

1. **Producer disposal in constructor**: Using `using` disposes the producer immediately after constructor completes, breaking all subsequent operations
2. **Singleton disposed per request**: Each controller instance tries to dispose the shared singleton
3. **Producer created before DI container**: Producer is created outside DI, so configuration binding and logging won't work properly

Fixed version:
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IOptions<SurgewaveConfiguration>>().Value;
    return new SurgewaveProducer<string, string>(opt =>
        opt.BootstrapServers = config.BootstrapServers);
});

var app = builder.Build();

// Controller
public class MessagesController : ControllerBase
{
    private readonly SurgewaveProducer<string, string> _producer;

    public MessagesController(SurgewaveProducer<string, string> producer)
    {
        _producer = producer;  // Just store reference, don't dispose
    }

    [HttpPost]
    public async Task<IActionResult> Post(MessageDto dto)
    {
        await _producer.ProduceAsync("topic", dto.Key, dto.Value);
        return Ok();
    }
}
```
</details>

### 7. Write an extension method that registers a Surgewave producer with configuration from IOptions:

```csharp
public static class SurgewaveServiceCollectionExtensions
{
    public static IServiceCollection AddSurgewaveProducer<TKey, TValue>(
        this IServiceCollection services)
    {
        // Implement this
    }
}
```

<details>
<summary>Answer</summary>
```csharp
public static class SurgewaveServiceCollectionExtensions
{
    public static IServiceCollection AddSurgewaveProducer<TKey, TValue>(
        this IServiceCollection services)
    {
        services.AddSingleton<SurgewaveProducer<TKey, TValue>>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<SurgewaveConfiguration>>().Value;
            var logger = sp.GetRequiredService<ILogger<SurgewaveProducer<TKey, TValue>>>();

            return new SurgewaveProducer<TKey, TValue>(opt =>
            {
                opt.BootstrapServers = options.BootstrapServers;
                opt.ClientId = options.ClientId ?? Environment.MachineName;
                // Add other configuration options as needed
            });
        });

        return services;
    }

    // Overload with configuration action
    public static IServiceCollection AddSurgewaveProducer<TKey, TValue>(
        this IServiceCollection services,
        Action<SurgewaveProducerOptions> configure)
    {
        services.AddSingleton(sp =>
        {
            return new SurgewaveProducer<TKey, TValue>(configure);
        });

        return services;
    }
}
```

Usage:
```csharp
builder.Services.Configure<SurgewaveConfiguration>(
    builder.Configuration.GetSection("Surgewave"));
builder.Services.AddSurgewaveProducer<string, string>();
```
</details>

## Score
- 7/7: Excellent! You have mastered dependency injection for Surgewave
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding
