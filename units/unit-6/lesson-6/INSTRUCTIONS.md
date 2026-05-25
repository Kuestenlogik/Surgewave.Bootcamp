# Lesson 18.6: Instructions

Follow these step-by-step instructions to understand and implement push-based consumption with `SurgewaveStreamingConsumer`.

## Step 1: Create a SurgewaveStreamingConsumer

Start with the minimal setup:

```csharp
await using var consumer = new SurgewaveStreamingConsumer(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "streaming-lesson-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
    options.InitialCredits = 100;
});
```

## Step 2: Subscribe to a Topic

```csharp
await consumer.SubscribeAsync("orders", cancellationToken);
Console.WriteLine("Subscribed to 'orders'. Waiting for messages...");
```

## Step 3: Consume with IAsyncEnumerable

Use `await foreach` to process the push-delivered stream:

```csharp
await foreach (var msg in consumer.ConsumeAllAsync(cancellationToken))
{
    Console.WriteLine($"Received [{msg.Topic}:{msg.Partition}@{msg.Offset}] key={msg.Key}");

    // Process the message
    await ProcessOrderAsync(msg.Value);

    // Grant credit to allow the broker to push more
    consumer.GrantCredit(1);
}
```

## Step 4: Implement Batch Processing with Credits

Accumulate messages and grant credit after each batch is processed:

```csharp
const int BatchSize = 25;
var batch = new List<ReceivedMessage>(BatchSize);

await foreach (var msg in consumer.ConsumeAllAsync(cancellationToken))
{
    batch.Add(msg);

    if (batch.Count >= BatchSize)
    {
        Console.WriteLine($"Processing batch of {batch.Count} messages...");
        await ProcessBatchAsync(batch);

        // Commit after batch is fully processed
        foreach (var m in batch)
            await consumer.CommitAsync(m, cancellationToken);

        batch.Clear();

        // Grant credit for the next batch
        consumer.GrantCredit(BatchSize);
        Console.WriteLine($"Granted credit for next {BatchSize} messages");
    }
}
```

## Step 5: Subscribe to Multiple Topics

```csharp
await consumer.SubscribeAsync("orders", cancellationToken);
await consumer.SubscribeAsync("returns", cancellationToken);

await foreach (var msg in consumer.ConsumeAllAsync(cancellationToken))
{
    Console.WriteLine($"[{msg.Topic}] {msg.Key}");

    switch (msg.Topic)
    {
        case "orders":
            await HandleOrderAsync(msg);
            break;
        case "returns":
            await HandleReturnAsync(msg);
            break;
    }

    consumer.GrantCredit(1);
}
```

## Step 6: Dynamic Unsubscribe / Resubscribe

Change subscriptions at runtime without recreating the consumer:

```csharp
// Initially subscribed to orders
await consumer.SubscribeAsync("orders", cancellationToken);

int messageCount = 0;
await foreach (var msg in consumer.ConsumeAllAsync(cancellationToken))
{
    await ProcessAsync(msg);
    consumer.GrantCredit(1);

    messageCount++;
    if (messageCount == 50)
    {
        // Switch subscriptions dynamically
        await consumer.UnsubscribeAsync("orders", cancellationToken);
        await consumer.SubscribeAsync("shipments", cancellationToken);
        Console.WriteLine("Switched from 'orders' to 'shipments'");
    }
}
```

## Step 7: Compare Push vs Poll Throughput

Run a quick comparison to see the overhead difference:

```csharp
// Poll-based
var sw = Stopwatch.StartNew();
int pollCount = 0;
while (pollCount < 1000)
{
    var msg = await pollConsumer.ConsumeAsync(cancellationToken);
    if (msg != null) pollCount++;
}
sw.Stop();
Console.WriteLine($"Poll:  {pollCount} msgs in {sw.ElapsedMilliseconds}ms");

// Push-based
sw.Restart();
int pushCount = 0;
await foreach (var msg in streamConsumer.ConsumeAllAsync(cancellationToken))
{
    pushCount++;
    streamConsumer.GrantCredit(1);
    if (pushCount >= 1000) break;
}
sw.Stop();
Console.WriteLine($"Push:  {pushCount} msgs in {sw.ElapsedMilliseconds}ms");
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- `SurgewaveStreamingConsumer` creation and configuration
- Batch processing with credit control
- Multi-topic subscriptions
- Dynamic unsubscribe/resubscribe
- Push vs poll throughput comparison

## Sample Output

```
=== Surgewave Bootcamp: Lesson 18.6 - Streaming Consumer ===

Surgewave broker started on port 54322

--- Single Topic Push-Based Consumption ---
Subscribed to 'orders'.
Received [orders:0@0] key=ord-1
Received [orders:0@1] key=ord-2
...
Processing batch of 25 messages...
Granted credit for next 25 messages

--- Multi-Topic Streaming ---
Subscribed to 'orders' and 'returns'.
[orders] ord-100
[returns] ret-001
[orders] ord-101
...

--- Dynamic Resubscription ---
Processed 50 messages from 'orders'
Switched from 'orders' to 'shipments'
[shipments] shp-001
...

--- Push vs Poll Benchmark ---
Poll:  1000 msgs in 142ms
Push:  1000 msgs in 89ms

Lesson complete!
```

## Knowledge Check

1. What is the key difference between `SurgewaveConsumer.ConsumeAsync()` and `SurgewaveStreamingConsumer.ConsumeAllAsync()`?
2. What happens when a streaming consumer exhausts its credit budget?
3. When should you choose poll-based over push-based consumption?
4. How do you switch topics without creating a new consumer?
5. Why is credit-based flow control important for slow consumers?

---

**Congratulations!** You've completed Unit 18: Advanced Consumer Patterns!
