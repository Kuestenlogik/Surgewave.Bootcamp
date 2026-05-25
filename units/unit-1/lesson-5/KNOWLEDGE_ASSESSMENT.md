# Quiz: Lesson 1.5 - Batching & Message Sizing

## Multiple Choice

### 1. Why does batching dramatically improve throughput?
- [ ] A) Messages are compressed better in batches
- [ ] B) Multiple messages are sent in a single network round-trip instead of one per message
- [ ] C) The broker processes batches in parallel
- [ ] D) Batching disables message validation

<details>
<summary>Answer</summary>
**B)** Multiple messages are sent in a single network round-trip instead of one per message. This reduces network overhead significantly, which is often the main bottleneck.
</details>

### 2. What does the `LingerMs` setting control?
- [ ] A) Maximum time a message can stay in the topic
- [ ] B) Time to wait for a batch to fill before sending
- [ ] C) Consumer timeout for reading messages
- [ ] D) Network connection timeout

<details>
<summary>Answer</summary>
**B)** Time to wait for a batch to fill before sending. Higher values allow more messages to accumulate in a batch, improving throughput but adding latency.
</details>

### 3. What is the optimal message size range for Surgewave?
- [ ] A) Less than 100 bytes
- [ ] B) 1KB - 100KB
- [ ] C) 1MB - 10MB
- [ ] D) As large as possible

<details>
<summary>Answer</summary>
**B)** 1KB - 100KB. This range provides a good balance of throughput and latency. Smaller messages have high overhead per message, while larger messages can cause memory pressure and timeouts.
</details>

### 4. When should you call explicit flush instead of letting batches fill naturally?
- [ ] A) For every message
- [ ] B) At the end of a batch or when time-critical
- [ ] C) Only when the application is shutting down
- [ ] D) Never, auto-flush is always better

<details>
<summary>Answer</summary>
**B)** At the end of a batch or when time-critical. Explicit flush ensures messages are sent immediately when you need guaranteed delivery timing, like at the end of a transaction.
</details>

## True/False

### 5. With batching enabled, Surgewave can achieve approximately 9x better throughput compared to no batching.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The lesson demonstrates that batching can improve throughput from ~1,176 msg/sec to ~10,526 msg/sec, roughly a 9x improvement.
</details>

### 6. Consumer batch fetching uses the `BatchSize` configuration option.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Consumers use `FetchMaxBytes` to control how much data to fetch per request. `BatchSize` is a producer setting for controlling how many messages to batch together.
</details>

## Code Challenge

### 7. Configure this producer for maximum throughput in a high-volume scenario:

```csharp
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = "localhost:9092";

    // Configure for high throughput
    options._______ = 1000;   // Batch up to 1000 messages
    options._______ = 50;     // Wait up to 50ms for batch to fill
});

// Produce many messages
for (int i = 0; i < 10000; i++)
{
    await producer.ProduceAsync("events", $"key-{i}", $"Event {i}");
}

// What should you call here to ensure all messages are sent?
await producer._______();
```

<details>
<summary>Answer</summary>
```csharp
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = "localhost:9092";

    options.BatchSize = 1000;   // Batch up to 1000 messages
    options.LingerMs = 50;      // Wait up to 50ms for batch to fill
});

for (int i = 0; i < 10000; i++)
{
    await producer.ProduceAsync("events", $"key-{i}", $"Event {i}");
}

await producer.FlushAsync();  // Ensure all buffered messages are sent
```

`BatchSize` controls max messages per batch, `LingerMs` controls wait time, and `FlushAsync()` ensures all buffered messages are sent before continuing.
</details>

## Score
- 7/7: Excellent! You understand batching and performance tuning
- 5-6/7: Good grasp of throughput optimization
- 3-4/7: Review batching configuration options
- <3/7: Re-do the lesson and run the performance examples
