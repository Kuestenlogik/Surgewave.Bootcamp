# Knowledge Assessment: Lesson 13.1 - Hosted Services

## Multiple Choice

### 1. What is the main difference between IHostedService and BackgroundService?
- [ ] A) IHostedService is faster
- [ ] B) BackgroundService is an abstract class that simplifies implementing IHostedService with ExecuteAsync
- [ ] C) IHostedService cannot be cancelled
- [ ] D) BackgroundService does not support dependency injection

<details>
<summary>Answer</summary>
**B)** BackgroundService is an abstract class that inherits from IHostedService and simplifies the implementation by providing an ExecuteAsync method for long-running work. IHostedService gives more control over start/stop logic with its StartAsync and StopAsync methods.
</details>

### 2. When is StopAsync called on a hosted service?
- [ ] A) After every message is processed
- [ ] B) When the application receives a shutdown signal (SIGTERM, Ctrl+C)
- [ ] C) When StartAsync completes
- [ ] D) Only during unhandled exceptions

<details>
<summary>Answer</summary>
**B)** StopAsync is called when the application receives a shutdown signal (such as SIGTERM or Ctrl+C). This gives the service a chance to gracefully shut down, commit offsets, and release resources.
</details>

### 3. What is the correct way to handle graceful shutdown in a consumer BackgroundService?
- [ ] A) Use Thread.Sleep in the consume loop
- [ ] B) Check stoppingToken.IsCancellationRequested in the consume loop
- [ ] C) Call Environment.Exit(0)
- [ ] D) Ignore shutdown signals and let the OS kill the process

<details>
<summary>Answer</summary>
**B)** You should check stoppingToken.IsCancellationRequested in your consume loop to detect when shutdown is requested, then exit gracefully. This allows proper offset commits and resource cleanup.
</details>

## True/False

### 4. Hosted services are started in the order they are registered and stopped in reverse order.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - ASP.NET Core starts hosted services in registration order during startup, and stops them in reverse order during shutdown. This ensures dependencies are available when services start and are cleaned up properly.
</details>

### 5. OperationCanceledException during shutdown should always be logged as an error.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - OperationCanceledException during a cancellation-requested shutdown is expected behavior, not an error. Use `catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)` to handle it gracefully without error logging.
</details>

## Code Challenge

### 6. What issues exist in this BackgroundService implementation?

```csharp
public class ConsumerService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new SurgewaveConsumer<string, string>(opt =>
            opt.BootstrapServers = "localhost:9092");
        consumer.Subscribe("my-topic");

        while (true)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromMinutes(5));
            if (result != null)
                Console.WriteLine(result.Value);
        }
    }
}
```

<details>
<summary>Answer</summary>
Issues:

1. **No cancellation check**: `while (true)` ignores stoppingToken, preventing graceful shutdown
2. **Long timeout**: 5-minute timeout delays shutdown significantly
3. **No consumer disposal**: Consumer is never disposed, leaking resources
4. **No exception handling**: Errors crash the service silently
5. **No logging**: Console.WriteLine instead of proper ILogger

Fixed version:
```csharp
public class ConsumerService : BackgroundService
{
    private readonly ILogger<ConsumerService> _logger;

    public ConsumerService(ILogger<ConsumerService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
            opt.BootstrapServers = "localhost:9092");
        consumer.Subscribe("my-topic");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(1));
                if (result != null)
                    _logger.LogInformation("Received: {Value}", result.Value);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break; // Normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming message");
            }
        }

        _logger.LogInformation("Consumer stopped gracefully");
    }
}
```
</details>

### 7. Why should the ConsumeAsync timeout be kept reasonably short (1-5 seconds)?

<details>
<summary>Answer</summary>
Short timeouts are important because:

1. **Responsive shutdown**: When stoppingToken is cancelled, the current ConsumeAsync call must complete (or timeout) before the cancellation is detected. A 5-minute timeout means up to 5 minutes before the service can shut down.

2. **Health check responsiveness**: Liveness/readiness probes may fail if the service appears unresponsive during long waits.

3. **Cancellation detection**: The cancellation check (`IsCancellationRequested`) only happens between ConsumeAsync calls, so shorter timeouts mean faster detection.

4. **Resource release**: Other services waiting for this service to stop during shutdown will be blocked.

Recommended timeout: 1-5 seconds, balancing responsiveness with avoiding excessive polling overhead.
</details>

## Score
- 7/7: Excellent! You understand hosted services for Surgewave consumers
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding
