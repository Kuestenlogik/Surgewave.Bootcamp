# Knowledge Assessment: Lesson 3.5 - Exactly-Once Semantics

## Multiple Choice

### 1. What are the three delivery guarantees in message streaming?
- [ ] A) Fast, Slow, Medium
- [ ] B) At-most-once, At-least-once, Exactly-once
- [ ] C) Guaranteed, Best-effort, Transactional
- [ ] D) Sync, Async, Batch

<details>
<summary>Answer</summary>
**B)** At-most-once, At-least-once, Exactly-once. These represent the spectrum from "fire and forget" (may lose messages) to "retry until ack" (may have duplicates) to "no loss, no duplicates" (ideal).
</details>

### 2. What does an idempotent producer prevent?
- [ ] A) Message loss
- [ ] B) Duplicate messages from producer retries
- [ ] C) Consumer lag
- [ ] D) Network timeouts

<details>
<summary>Answer</summary>
**B)** Duplicate messages from producer retries. When a producer retries after a timeout (without knowing if the original succeeded), idempotency ensures only one copy is stored.
</details>

### 3. What is the purpose of `IsolationLevel.ReadCommitted` in consumers?
- [ ] A) To improve performance
- [ ] B) To only see messages from committed transactions
- [ ] C) To enable parallel consumption
- [ ] D) To reduce memory usage

<details>
<summary>Answer</summary>
**B)** To only see messages from committed transactions. This prevents consumers from seeing messages that may be rolled back, which is essential for exactly-once semantics.
</details>

### 4. What is the risk of "at-least-once" delivery without idempotency?
- [ ] A) Message loss
- [ ] B) Duplicate processing of the same message
- [ ] C) Slow performance
- [ ] D) Connection timeouts

<details>
<summary>Answer</summary>
**B)** Duplicate processing of the same message. With at-least-once, messages are retried until acknowledged, which can result in duplicates if the ack is lost but the message was actually stored.
</details>

## True/False

### 5. Enabling `EnableIdempotence = true` automatically provides exactly-once delivery end-to-end.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Idempotence only prevents duplicates from producer retries. For true exactly-once semantics, you also need transactional consumers with offset management and ReadCommitted isolation.
</details>

### 6. Exactly-once semantics require combining transactions with consumer offset management.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Exactly-once processing requires the consume-offset-commit and produce operations to be atomic. This is achieved by including offset commits within the producer transaction.
</details>

## Code Challenge

### 7. Configure this producer and consumer for exactly-once semantics:

```csharp
// Idempotent transactional producer
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    opt.TransactionalId = "processor-1";
    opt.____________ = true;  // Prevent duplicate produces
});

// Consumer that only reads committed messages
var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    opt.GroupId = "exactly-once-group";
    opt.____________ = IsolationLevel.____________;  // Only committed
    opt.____________ = false;  // Manual offset control
});

// Exactly-once processing loop
await producer.InitTransactionsAsync();

while (!cancellationToken.IsCancellationRequested)
{
    var message = await consumer.ConsumeAsync(cancellationToken);
    if (message == null) continue;

    await producer.BeginTransactionAsync();
    try
    {
        // Process and produce
        var result = Transform(message.Value);
        await producer.ProduceAsync("output", message.Key, result);

        // Commit offsets as part of transaction
        await producer.SendOffsetsToTransactionAsync(
            consumer.____________,  // Get consumer's offsets
            consumer.____________);  // Get consumer's group

        await producer.CommitTransactionAsync();
    }
    catch
    {
        await producer.AbortTransactionAsync();
        throw;
    }
}
```

<details>
<summary>Answer</summary>
```csharp
// Idempotent transactional producer
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    opt.TransactionalId = "processor-1";
    opt.EnableIdempotence = true;  // Prevent duplicate produces
});

// Consumer that only reads committed messages
var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    opt.GroupId = "exactly-once-group";
    opt.IsolationLevel = IsolationLevel.ReadCommitted;  // Only committed
    opt.EnableAutoCommit = false;  // Manual offset control
});

// Exactly-once processing loop
await producer.InitTransactionsAsync();

while (!cancellationToken.IsCancellationRequested)
{
    var message = await consumer.ConsumeAsync(cancellationToken);
    if (message == null) continue;

    await producer.BeginTransactionAsync();
    try
    {
        // Process and produce
        var result = Transform(message.Value);
        await producer.ProduceAsync("output", message.Key, result);

        // Commit offsets as part of transaction
        await producer.SendOffsetsToTransactionAsync(
            consumer.Position,  // Get consumer's offsets
            consumer.GroupId);  // Get consumer's group

        await producer.CommitTransactionAsync();
    }
    catch
    {
        await producer.AbortTransactionAsync();
        throw;
    }
}
```

Key settings for exactly-once:
- `EnableIdempotence = true` - prevents duplicate produces
- `IsolationLevel.ReadCommitted` - consumer skips uncommitted messages
- `EnableAutoCommit = false` - manual offset control
- `SendOffsetsToTransactionAsync` - atomic offset commit with produce
</details>

## Score
- 7/7: Excellent! You understand exactly-once semantics
- 5-6/7: Good grasp of delivery guarantees
- 3-4/7: Review idempotence and transaction combinations
- <3/7: Re-do the lesson focusing on the full pattern
