# Knowledge Assessment: Lesson 26.2 - Producer Optimization

## Multiple Choice

### 1. Which configuration maximizes throughput at the cost of latency?
- [ ] A) BatchSize=1KB, LingerMs=0
- [ ] B) BatchSize=64KB, LingerMs=50
- [ ] C) BatchSize=4KB, LingerMs=5
- [ ] D) BatchSize=16KB, LingerMs=0

<details>
<summary>Answer</summary>
**B)** Large batch size (64KB) with high linger time (50ms) maximizes batching efficiency. More messages per batch means fewer network round trips, increasing throughput but adding latency as messages wait.
</details>

### 2. Which compression algorithm offers the best balance of speed and compression ratio?
- [ ] A) Gzip
- [ ] B) None
- [ ] C) LZ4
- [ ] D) Zstd

<details>
<summary>Answer</summary>
**C)** LZ4 is specifically designed for speed with acceptable compression ratios (2-4x). It's the default choice for most use cases because it adds minimal CPU overhead while reducing network usage.
</details>

### 3. What does LingerMs=0 mean for producer behavior?
- [ ] A) Messages are never sent
- [ ] B) Messages are batched indefinitely
- [ ] C) Batches are sent immediately when batch is full or any message is ready
- [ ] D) Each message is sent in its own batch immediately

<details>
<summary>Answer</summary>
**C)** LingerMs=0 means the producer doesn't wait to fill batches. A batch is sent as soon as it reaches BatchSize OR when any message is ready to send. Messages can still be batched together if they arrive quickly enough.
</details>

### 4. Which async pattern provides the highest throughput?
- [ ] A) Sequential await
- [ ] B) Fire-and-forget without flush
- [ ] C) Batched concurrent tasks with controlled concurrency
- [ ] D) Single await per batch

<details>
<summary>Answer</summary>
**C)** Batched concurrent tasks with controlled concurrency balances parallelism with resource management. It allows multiple in-flight requests while preventing thread pool exhaustion.
</details>

### 5. When should you use Acks=All?
- [ ] A) For maximum throughput
- [ ] B) For lowest latency
- [ ] C) When message durability is critical
- [ ] D) For fire-and-forget scenarios

<details>
<summary>Answer</summary>
**C)** Acks=All ensures all in-sync replicas acknowledge the message before the producer considers it successful. This is essential for critical data where message loss is unacceptable, such as financial transactions.
</details>

## True/False

### 6. Increasing batch size always improves throughput without any downsides.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - While larger batches improve throughput, they increase memory usage, add latency (waiting for batch to fill), and can cause larger data loss if the producer crashes with a full batch buffer.
</details>

### 7. Fire-and-forget (not awaiting ProduceAsync) is safe for production use.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Fire-and-forget without proper error handling can silently lose messages. Errors are not propagated, and you won't know if messages failed. Always use FlushAsync() or callback patterns for production.
</details>

### 8. Zstd compression typically reduces network traffic more than LZ4.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Zstd typically achieves 4-8x compression ratios compared to LZ4's 2-4x, significantly reducing network traffic at the cost of higher CPU usage.
</details>

## Code Challenge

### 9. What's wrong with this high-throughput producer implementation?

```csharp
var tasks = new List<Task>();

for (int i = 0; i < 1_000_000; i++)
{
    tasks.Add(producer.ProduceAsync(topic, $"key-{i}", largeMessage));
}

await Task.WhenAll(tasks);
```

<details>
<summary>Answer</summary>
This code has several issues:

1. **Memory exhaustion**: Creating 1 million tasks simultaneously can exhaust memory
2. **Thread pool starvation**: Too many concurrent operations can starve the thread pool
3. **No backpressure**: Nothing limits how fast messages are queued

Corrected implementation:

```csharp
var semaphore = new SemaphoreSlim(100); // Limit concurrency
var tasks = new List<Task>();

for (int i = 0; i < 1_000_000; i++)
{
    await semaphore.WaitAsync();

    var task = producer.ProduceAsync(topic, $"key-{i}", largeMessage)
        .ContinueWith(_ => semaphore.Release());

    tasks.Add(task);

    // Periodically clear completed tasks to free memory
    if (i % 10000 == 0)
    {
        var completed = tasks.Where(t => t.IsCompleted).ToList();
        foreach (var t in completed) tasks.Remove(t);
    }
}

await Task.WhenAll(tasks);
```

Or use batched approach:

```csharp
var batch = new List<Task>(1000);

for (int i = 0; i < 1_000_000; i++)
{
    batch.Add(producer.ProduceAsync(topic, $"key-{i}", largeMessage));

    if (batch.Count >= 1000)
    {
        await Task.WhenAll(batch);
        batch.Clear();
    }
}

if (batch.Count > 0)
    await Task.WhenAll(batch);
```
</details>

### 10. Given these requirements, choose the optimal configuration:

**Scenario**: Order processing system
- 10,000 orders/second average
- P99 latency must be under 20ms
- Messages are 500 bytes each
- Message loss is unacceptable

What BatchSize, LingerMs, Compression, and Acks would you recommend?

<details>
<summary>Answer</summary>
**Recommended Configuration:**

```csharp
opt.BatchSize = 16384;        // 16KB - fits ~32 messages
opt.LingerMs = 5;             // Low linger for P99 < 20ms
opt.CompressionType = CompressionType.Lz4;  // Fast compression
opt.Acks = Acks.All;          // Required for no message loss
opt.Retries = 5;              // Retry on transient failures
```

**Reasoning:**

1. **BatchSize=16KB**: Balances batching efficiency with latency. At 500 bytes/message, this fits about 32 messages per batch, reducing network overhead.

2. **LingerMs=5**: With 10K messages/second, the 16KB batch fills quickly (~1.6ms). A 5ms linger ensures batches don't wait too long while still allowing efficient batching.

3. **LZ4 Compression**: Provides 2-3x compression with minimal CPU overhead, reducing network usage without adding significant latency.

4. **Acks=All**: Required since message loss is unacceptable. This adds some latency but ensures durability.

5. **Retries=5**: Handles transient failures without message loss.

This configuration should achieve the throughput requirement while keeping P99 under 20ms (5ms linger + ~10-15ms for Acks=All round trip).
</details>

## Performance Scenario

### 11. Your producer shows these metrics:
- Throughput: 15,000 msg/s
- P50: 0.5ms
- P99: 150ms
- CPU usage: 30%
- Network usage: 20%

What is likely the bottleneck, and how would you improve it?

<details>
<summary>Answer</summary>
**Analysis:**

The large gap between P50 (0.5ms) and P99 (150ms) with low CPU and network usage suggests:

**Likely bottleneck**: Producer batching/buffering or synchronous blocking

The P99 spike to 150ms while P50 is only 0.5ms indicates that some messages are waiting much longer than others, likely due to:
1. Sequential awaiting (each message waits for previous)
2. Large linger time causing some messages to wait
3. Insufficient concurrency

**Improvements:**

1. **Check async pattern**: Ensure you're using concurrent async, not sequential:
   ```csharp
   // Bad
   foreach (var msg in messages)
       await producer.ProduceAsync(...);

   // Good
   var tasks = messages.Select(msg => producer.ProduceAsync(...));
   await Task.WhenAll(tasks);
   ```

2. **Reduce LingerMs**: If set too high, reduce to 1-5ms

3. **Increase MaxInFlightRequests**: Allow more concurrent requests:
   ```csharp
   opt.MaxInFlightRequests = 10;
   ```

4. **Check for blocking calls**: Ensure no synchronous I/O in producer path

5. **Add concurrency batching**: Use semaphore-based concurrency control:
   ```csharp
   var semaphore = new SemaphoreSlim(100);
   // ... use with async production
   ```
</details>

## Score
- 11/11: Excellent! Ready for consumer optimization
- 8-10/11: Good understanding, review missed concepts
- 5-7/11: Review the lesson before continuing
- <5/11: Re-do the lesson and exercises
