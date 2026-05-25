# Lesson 9.4: Common Issues - Instructions

## Step 1: Monitor Consumer Lag

Track lag between produced and consumed offsets:

```csharp
public class LagMonitor
{
    private long _latestOffset;
    private long _committedOffset;

    public long CurrentLag => _latestOffset - _committedOffset;

    public void RecordProduce(long offset)
    {
        Interlocked.Exchange(ref _latestOffset, offset);
    }

    public void RecordCommit(long offset)
    {
        Interlocked.Exchange(ref _committedOffset, offset);
    }
}
```

## Step 2: Simulate Slow Processing

Create a consumer that processes slowly to generate lag:

```csharp
await foreach (var message in consumer.ConsumeAsync())
{
    // Slow processing causes lag to grow
    await Task.Delay(1000);

    // Check lag
    var lag = lagMonitor.CurrentLag;
    if (lag > threshold)
    {
        Console.WriteLine($"WARNING: High lag detected: {lag} messages");
    }
}
```

## Step 3: Handle Rebalancing

Implement rebalance callbacks:

```csharp
public class RebalanceHandler : IConsumerRebalanceListener
{
    public void OnPartitionsAssigned(IEnumerable<TopicPartition> partitions)
    {
        Console.WriteLine($"Assigned: {string.Join(", ", partitions)}");
        // Initialize state for new partitions
    }

    public void OnPartitionsRevoked(IEnumerable<TopicPartition> partitions)
    {
        Console.WriteLine($"Revoked: {string.Join(", ", partitions)}");
        // Commit offsets, cleanup state
    }
}
```

## Step 4: Graceful Rebalance Handling

Ensure clean handoff during rebalancing:

```csharp
OnPartitionsRevoked = partitions =>
{
    // 1. Stop processing in-flight messages
    processingCts.Cancel();

    // 2. Wait for current batch to complete
    currentBatch.Wait();

    // 3. Commit final offsets
    consumer.Commit();

    // 4. Clear local state
    localCache.Clear();
};
```

## Step 5: Configure Timeouts

Set appropriate timeouts for your workload:

```csharp
var consumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.GroupId = "my-group";

    // Session timeout - how long before consumer is considered dead
    options.SessionTimeoutMs = 45000;

    // Heartbeat interval - send heartbeats this often
    options.HeartbeatIntervalMs = 3000;

    // Max poll interval - max time between consume calls
    options.MaxPollIntervalMs = 300000;

    // Request timeout - network request timeout
    options.RequestTimeoutMs = 30000;
});
```

## Step 6: Debug Timeout Issues

Common timeout patterns and solutions:

```csharp
// Problem: Session timeout during long processing
// Solution: Process in batches, commit frequently

// Problem: Request timeout on slow network
// Solution: Increase RequestTimeoutMs

// Problem: Max poll interval exceeded
// Solution: Reduce batch size or increase interval
```

## Verification

Run the completed solution:
```bash
cd completed
dotnet run
```

You should see:
- Lag increasing during slow processing
- Rebalance events logged with partition assignments
- Timeout handling demonstrations
