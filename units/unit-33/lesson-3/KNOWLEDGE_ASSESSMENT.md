# Knowledge Assessment: Lesson 26.3 - Consumer Optimization

## Multiple Choice

### 1. If a topic has 6 partitions, what is the maximum number of consumers that can actively consume in a single consumer group?
- [ ] A) 3
- [ ] B) 6
- [ ] C) 12
- [ ] D) Unlimited

<details>
<summary>Answer</summary>
**B)** Each partition can only be assigned to one consumer in a group at a time. With 6 partitions, at most 6 consumers can be active; additional consumers will be idle.
</details>

### 2. Which fetch setting would you increase to reduce the number of fetch requests for small messages?
- [ ] A) FetchMaxWaitMs
- [ ] B) FetchMinBytes
- [ ] C) MaxPollRecords
- [ ] D) SessionTimeoutMs

<details>
<summary>Answer</summary>
**B)** FetchMinBytes specifies the minimum amount of data the server should return. Increasing it makes the server wait until more data is available, reducing the frequency of fetch requests.
</details>

### 3. What happens when a consumer exceeds MaxPollIntervalMs?
- [ ] A) The consumer continues normally
- [ ] B) The consumer is considered dead and triggers a rebalance
- [ ] C) Messages are automatically committed
- [ ] D) The fetch size is reduced

<details>
<summary>Answer</summary>
**B)** If a consumer doesn't call poll() within MaxPollIntervalMs, the broker considers it dead and triggers a rebalance, reassigning its partitions to other consumers.
</details>

### 4. Which processing pattern provides the best backpressure handling?
- [ ] A) Sequential processing
- [ ] B) Unbounded parallel processing
- [ ] C) Channel-based with bounded capacity
- [ ] D) Fire-and-forget

<details>
<summary>Answer</summary>
**C)** Bounded channels provide natural backpressure - when the channel is full, the producer (consumer reading from Kafka) waits, preventing memory exhaustion and allowing workers to catch up.
</details>

### 5. When processing messages takes 100ms each, what MaxPollIntervalMs should you configure if MaxPollRecords is 500?
- [ ] A) 30 seconds (default)
- [ ] B) At least 50 seconds (500 * 100ms)
- [ ] C) 5 seconds
- [ ] D) 10 seconds

<details>
<summary>Answer</summary>
**B)** MaxPollIntervalMs must be greater than the time to process all records from one poll. With 500 records at 100ms each = 50 seconds minimum, plus buffer for variability.
</details>

## True/False

### 6. Increasing FetchMaxBytes always improves consumer throughput.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - While larger fetches reduce network overhead, they increase memory usage and can cause longer GC pauses. There's an optimal point beyond which returns diminish or reverse.
</details>

### 7. With EnableAutoCommit=true, messages are guaranteed to be processed exactly once.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Auto-commit commits offsets periodically (based on AutoCommitIntervalMs), regardless of whether processing succeeded. If the consumer crashes after processing but before the next auto-commit, messages will be reprocessed (at-least-once semantics).
</details>

### 8. Batch processing typically has lower per-message latency than individual message processing.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Batch processing has higher individual message latency because each message must wait for the batch to fill before processing. However, it often has higher throughput due to reduced overhead per message.
</details>

## Code Challenge

### 9. What's wrong with this parallel processing implementation?

```csharp
var tasks = new List<Task>();

while (true)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    tasks.Add(Task.Run(async () =>
    {
        await ProcessAsync(result.Value);
        await consumer.CommitAsync(result);
    }));
}
```

<details>
<summary>Answer</summary>
There are multiple issues:

1. **Unbounded parallelism**: No limit on concurrent tasks, can exhaust memory/threads
2. **Commit ordering**: Commits happen out of order; a later message might commit before an earlier one fails
3. **Task list grows forever**: Tasks are added but never removed
4. **No error handling**: Failed processing silently continues

Corrected implementation:

```csharp
var semaphore = new SemaphoreSlim(10); // Limit parallelism
var pendingOffsets = new ConcurrentDictionary<int, ConsumeResult<string, string>>();
var maxOffset = new Dictionary<int, long>(); // Track max processed per partition

while (true)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    await semaphore.WaitAsync();

    _ = Task.Run(async () =>
    {
        try
        {
            await ProcessAsync(result.Value);

            // Track completion for ordered commit
            lock (maxOffset)
            {
                var partition = result.Partition;
                if (!maxOffset.ContainsKey(partition) || result.Offset > maxOffset[partition])
                {
                    maxOffset[partition] = result.Offset;
                    pendingOffsets[partition] = result;
                }
            }
        }
        catch (Exception ex)
        {
            // Handle error - don't commit this offset
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            semaphore.Release();
        }
    });

    // Periodically commit highest completed offsets
    if (shouldCommit)
    {
        foreach (var pending in pendingOffsets.Values)
        {
            await consumer.CommitAsync(pending);
        }
        pendingOffsets.Clear();
    }
}
```
</details>

### 10. Design a consumer configuration for this scenario:

**Scenario**: Log aggregation system
- 50MB/sec of log data across 12 partitions
- Each log line is ~500 bytes
- Processing is simple (parse JSON, store in database)
- Latency is not critical, throughput is priority
- Database performs best with batch inserts of 1000 records

What settings would you recommend?

<details>
<summary>Answer</summary>
**Recommended Configuration:**

```csharp
// Consumer settings
opt.BootstrapServers = bootstrapServers;
opt.GroupId = "log-aggregator";
opt.FetchMinBytes = 1024 * 1024;        // 1MB - wait for substantial data
opt.FetchMaxBytes = 10 * 1024 * 1024;   // 10MB - large fetches
opt.MaxPollRecords = 2000;               // Room for batch + extra
opt.MaxPollIntervalMs = 60000;           // 60s for slow DB batches
opt.EnableAutoCommit = false;            // Manual commit after DB write
opt.AutoOffsetReset = AutoOffsetReset.Earliest;
```

**Processing architecture:**

```csharp
// Channel for decoupled consumption
var channel = Channel.CreateBounded<ConsumeResult<string, string>>(
    new BoundedChannelOptions(5000));  // Buffer for batching

// Consumer task
_ = Task.Run(async () =>
{
    while (true)
    {
        var result = await consumer.ConsumeAsync();
        await channel.Writer.WriteAsync(result);
    }
});

// Batch processing task
var batch = new List<LogEntry>(1000);
await foreach (var result in channel.Reader.ReadAllAsync())
{
    batch.Add(ParseLog(result.Value));

    if (batch.Count >= 1000)
    {
        await database.BulkInsertAsync(batch);
        await consumer.CommitAsync(result); // Commit after successful insert
        batch.Clear();
    }
}
```

**Reasoning:**

1. **12 consumers** (one per partition) for maximum parallelism
2. **Large FetchMinBytes/MaxBytes** for efficient network usage
3. **MaxPollRecords=2000** allows batching 1000 with overhead
4. **Channel with capacity 5000** handles burst + batching buffer
5. **Batch size 1000** matches database optimal insert size
6. **Manual commit after insert** ensures at-least-once delivery
</details>

## Scenario Analysis

### 11. Your consumer shows this pattern:
- Consumer lag: Growing steadily
- Poll frequency: Decreasing over time
- Processing time per message: Constant at 50ms
- Rebalances: Frequent (every few minutes)

What is the likely issue and how would you fix it?

<details>
<summary>Answer</summary>
**Analysis:**

The symptoms indicate **MaxPollInterval is being exceeded**:
- Growing lag = falling behind
- Decreasing poll frequency = processing taking too long between polls
- Frequent rebalances = consumer being kicked out

**Root cause:**
If MaxPollRecords=500 (default) and processing takes 50ms each:
- Total processing time = 500 * 50ms = 25 seconds per poll
- If MaxPollIntervalMs=30000 (30 seconds default), you're close to the limit
- Any variation causes timeout and rebalance

**Solutions (in order of preference):**

1. **Increase MaxPollIntervalMs:**
   ```csharp
   opt.MaxPollIntervalMs = 120000; // 2 minutes
   ```

2. **Decrease MaxPollRecords:**
   ```csharp
   opt.MaxPollRecords = 100; // 100 * 50ms = 5 seconds
   ```

3. **Add parallel processing:**
   ```csharp
   var semaphore = new SemaphoreSlim(10); // 10 concurrent
   // 500 / 10 * 50ms = 2.5 seconds total
   ```

4. **Optimize processing logic:**
   - Add caching
   - Batch database calls
   - Use async I/O properly

5. **Scale out consumers:**
   - Add more consumers (up to partition count)
   - Each consumer handles fewer messages

**Recommended combination:**
```csharp
opt.MaxPollIntervalMs = 60000;  // Safety margin
opt.MaxPollRecords = 200;        // Reduce batch size
// Plus parallel processing with semaphore
```
</details>

## Score
- 11/11: Excellent! Ready for memory optimization
- 8-10/11: Good understanding, review missed concepts
- 5-7/11: Review the lesson before continuing
- <5/11: Re-do the lesson and exercises
