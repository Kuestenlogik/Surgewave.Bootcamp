# Lesson 7.1: Processor Lifecycle Hooks

Processor lifecycle hooks let you initialize resources when a processor starts and clean them up when it shuts down. This is essential for managing database connections, HTTP clients, file handles, and other external resources.

## Learning Objectives

- Implement `IProcessorLifecycle` with `OnInit` and `OnClose`
- Use `ProcessorLifecycleContext` for node info and shutdown tokens
- Understand reverse-topological shutdown ordering
- Configure graceful shutdown timeouts

## IProcessorLifecycle Interface

```csharp
public interface IProcessorLifecycle
{
    Task OnInit(ProcessorLifecycleContext context);
    Task OnClose(ProcessorLifecycleContext context);
}
```

## ProcessorLifecycleContext

The context provides:

- `NodeName` -- The name of this processor node in the topology
- `ProcessorContext` -- Access to state stores and forwarding
- `ShutdownToken` -- Cancelled when the processor is shutting down

## Example: HTTP Client Lifecycle

```csharp
public class EnrichmentProcessor : IProcessor<string, Order>, IProcessorLifecycle
{
    private HttpClient _httpClient = null!;
    private IMemoryCache _cache = null!;

    public async Task OnInit(ProcessorLifecycleContext context)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.enrichment.example.com"),
            Timeout = TimeSpan.FromSeconds(5)
        };

        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10_000 });
        await WarmCacheAsync(context.ShutdownToken);
    }

    public Task OnClose(ProcessorLifecycleContext context)
    {
        _httpClient.Dispose();
        _cache.Dispose();
        return Task.CompletedTask;
    }

    public async Task Process(string key, Order value, ProcessorContext context)
    {
        var enrichedOrder = await EnrichOrderAsync(value);
        context.Forward(key, enrichedOrder);
    }
}
```

## ShutdownOrchestrator

The `ShutdownOrchestrator` ensures processors shut down in reverse-topological order: sink nodes close first, then transforms, then sources -- preventing data loss during shutdown.

## Configuring Shutdown Timeout

```csharp
var config = new StreamsConfig
{
    ApplicationId = "order-enrichment",
    BootstrapServers = "localhost:9092",
    GracefulShutdownTimeout = TimeSpan.FromSeconds(60) // default: 30s
};
```

If a processor's `OnClose` exceeds the timeout, the orchestrator logs a warning and proceeds to the next node.

## Exercise

1. Create a processor that connects to an external resource in `OnInit`
2. Process records using the external resource
3. Clean up the resource in `OnClose`
4. Start and stop the topology, verifying correct `OnInit`/`OnClose` order
5. Test with a multi-node topology (3+ processors) and confirm reverse-topological shutdown

---

[Back to Unit 7](../README.md)
