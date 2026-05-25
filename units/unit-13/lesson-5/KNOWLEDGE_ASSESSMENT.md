# Knowledge Assessment: Lesson 13.5 - Background Processing

## Multiple Choice

### 1. What is the purpose of a producer pool?
- [ ] A) To reduce memory usage
- [ ] B) To distribute message production across multiple producers for higher throughput
- [ ] C) To encrypt messages
- [ ] D) To validate message schemas

<details>
<summary>Answer</summary>
**B)** A producer pool maintains multiple producer instances to distribute the load of message production. This enables higher throughput by allowing parallel production without the overhead of creating/destroying producers for each request.
</details>

### 2. Why should multiple consumer workers in the same consumer group process different partitions?
- [ ] A) To ensure messages are processed in random order
- [ ] B) To enable parallel processing while ensuring each message is processed once
- [ ] C) To reduce network traffic
- [ ] D) To simplify error handling

<details>
<summary>Answer</summary>
**B)** Consumer groups enable parallel processing where each partition is assigned to only one consumer in the group. This ensures each message is processed exactly once while allowing horizontal scaling of processing capacity.
</details>

### 3. In what order are hosted services stopped during application shutdown?
- [ ] A) Random order
- [ ] B) Alphabetical order
- [ ] C) Reverse of registration order
- [ ] D) Same as registration order

<details>
<summary>Answer</summary>
**C)** Hosted services are stopped in reverse registration order. This ensures that services registered later (which may depend on earlier services) are stopped first, allowing dependencies to remain available during cleanup.
</details>

## True/False

### 4. The shutdown timeout can be configured to give background services more time to complete gracefully.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The shutdown timeout can be configured using `builder.Host.ConfigureHostOptions(options => options.ShutdownTimeout = TimeSpan.FromSeconds(30))`. This gives background services time to commit offsets and clean up resources.
</details>

### 5. All consumer workers processing the same topic must use different group IDs to avoid duplicate processing.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Consumer workers processing the same topic should use the SAME group ID to ensure each message is processed by only one worker. Different group IDs would cause each worker to receive ALL messages, resulting in duplicate processing.
</details>

## Code Challenge

### 6. What problems exist in this producer pool implementation?

```csharp
public class ProducerPool
{
    private readonly List<SurgewaveProducer<string, string>> _producers = new();
    private int _currentIndex = 0;

    public ProducerPool(int poolSize)
    {
        for (int i = 0; i < poolSize; i++)
        {
            _producers.Add(new SurgewaveProducer<string, string>(opt =>
                opt.BootstrapServers = "localhost:9092"));
        }
    }

    public SurgewaveProducer<string, string> GetProducer()
    {
        _currentIndex++;
        if (_currentIndex >= _producers.Count)
            _currentIndex = 0;
        return _producers[_currentIndex];
    }
}
```

<details>
<summary>Answer</summary>
Problems:

1. **Thread safety**: `_currentIndex` is modified without synchronization, causing race conditions
2. **No disposal**: Pool doesn't implement IDisposable, producers are never cleaned up
3. **Round-robin logic bug**: Increments before using, skipping index 0 initially
4. **Hardcoded configuration**: Bootstrap servers should be configurable

Fixed version:
```csharp
public class ProducerPool : IDisposable
{
    private readonly SurgewaveProducer<string, string>[] _producers;
    private int _currentIndex = -1;
    private bool _disposed;

    public ProducerPool(int poolSize, string bootstrapServers)
    {
        _producers = new SurgewaveProducer<string, string>[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            _producers[i] = new SurgewaveProducer<string, string>(opt =>
                opt.BootstrapServers = bootstrapServers);
        }
    }

    public SurgewaveProducer<string, string> GetProducer()
    {
        // Thread-safe round-robin using Interlocked
        var index = Interlocked.Increment(ref _currentIndex);
        return _producers[Math.Abs(index % _producers.Length)];
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var producer in _producers)
        {
            producer?.Dispose();
        }
    }
}
```
</details>

### 7. Implement a graceful shutdown handler for a consumer worker that commits offsets before stopping:

```csharp
public class ConsumerWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var consumer = new SurgewaveConsumer<string, string>(...);
        consumer.Subscribe("orders");

        // Implement graceful shutdown with offset commit
    }
}
```

<details>
<summary>Answer</summary>
```csharp
public class ConsumerWorker : BackgroundService
{
    private readonly ILogger<ConsumerWorker> _logger;
    private readonly string _workerId;

    public ConsumerWorker(ILogger<ConsumerWorker> logger, string workerId)
    {
        _logger = logger;
        _workerId = workerId;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker {WorkerId}: Starting...", _workerId);

        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = "localhost:9092";
            opt.GroupId = "order-processors"; // Same group for all workers
            opt.EnableAutoCommit = false; // Manual commit for reliability
        });

        consumer.Subscribe("orders");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await consumer.ConsumeAsync(
                        TimeSpan.FromSeconds(1),
                        stoppingToken);

                    if (result != null)
                    {
                        await ProcessMessageAsync(result);

                        // Commit after successful processing
                        await consumer.CommitAsync(result);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Shutdown requested, exit loop
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker {WorkerId}: Error processing message", _workerId);
                    // Continue processing other messages
                }
            }
        }
        finally
        {
            _logger.LogInformation("Worker {WorkerId}: Stopping, committing final offsets...", _workerId);

            // Final commit before shutdown
            try
            {
                await consumer.CommitAsync();
                _logger.LogInformation("Worker {WorkerId}: Final offsets committed", _workerId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Worker {WorkerId}: Failed to commit final offsets", _workerId);
            }
        }

        _logger.LogInformation("Worker {WorkerId}: Stopped gracefully", _workerId);
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> result)
    {
        _logger.LogInformation("Worker {WorkerId}: Processing {Key}", _workerId, result.Key);
        // Processing logic here
        await Task.Delay(100); // Simulate work
    }
}
```
</details>

## Score
- 7/7: Excellent! You have mastered background processing patterns
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding
