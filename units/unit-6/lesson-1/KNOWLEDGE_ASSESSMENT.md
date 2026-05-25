# Knowledge Assessment: Lesson 18.1 - Manual Offset Management

## Multiple Choice

### 1. What is the main advantage of manual offset management over auto-commit?
- [ ] A) Simpler code
- [ ] B) Full control over delivery guarantees and when offsets are committed
- [ ] C) Faster message processing
- [ ] D) Reduced network traffic

<details>
<summary>Answer</summary>
**B)** Manual offset management provides full control over when offsets are committed, allowing you to implement specific delivery guarantees (at-least-once, at-most-once) and handle errors according to your application's needs.
</details>

### 2. In at-least-once delivery, when should you commit the offset?
- [ ] A) Before receiving the message
- [ ] B) Immediately after receiving the message
- [ ] C) After successfully processing the message
- [ ] D) At fixed time intervals only

<details>
<summary>Answer</summary>
**C)** In at-least-once delivery, you commit the offset AFTER successfully processing the message. If a crash occurs before the commit, the message will be reprocessed (hence "at-least-once").
</details>

### 3. Which commit strategy offers the best balance of safety and performance?
- [ ] A) Synchronous per-message commit
- [ ] B) Batch commit (every N messages)
- [ ] C) Never commit
- [ ] D) Commit before processing

<details>
<summary>Answer</summary>
**B)** Batch commit (every N messages) offers a balance between the safety of per-message commits (fewer messages at risk) and the performance of less frequent commits (reduced roundtrips to the broker).
</details>

## True/False

### 4. With auto-commit enabled, messages processed between automatic commits will be lost if a crash occurs.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Auto-commit happens at fixed intervals. If the application crashes after processing messages but before the next auto-commit, those messages are lost because their offsets were already committed but processing wasn't complete.
</details>

### 5. At-most-once delivery guarantees that every message is processed exactly one time.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - At-most-once delivery commits before processing, so messages MAY be lost if processing fails. It guarantees messages are never processed more than once, but they might not be processed at all. This is different from exactly-once.
</details>

## Code Challenge

### 6. What's wrong with this at-least-once implementation?
```csharp
while (!cancellationToken.IsCancellationRequested)
{
    var result = await consumer.ConsumeAsync(cancellationToken);
    await consumer.CommitAsync(result, cancellationToken);
    await ProcessMessageAsync(result);
}
```

<details>
<summary>Answer</summary>
The code commits BEFORE processing, which implements at-most-once, not at-least-once delivery. If processing fails after commit, the message is lost. Correct at-least-once:
```csharp
while (!cancellationToken.IsCancellationRequested)
{
    var result = await consumer.ConsumeAsync(cancellationToken);
    await ProcessMessageAsync(result);  // Process FIRST
    await consumer.CommitAsync(result, cancellationToken);  // Then commit
}
```
</details>

### 7. Which configuration option must be set to enable manual offset management?
- [ ] A) `options.AutoOffsetReset = AutoOffsetReset.Earliest`
- [ ] B) `options.EnableAutoCommit = false`
- [ ] C) `options.GroupId = "manual-group"`
- [ ] D) `options.MaxPollIntervalMs = 300000`

<details>
<summary>Answer</summary>
**B)** `options.EnableAutoCommit = false` disables automatic offset commits, requiring you to call `CommitAsync()` manually. The other options are useful configurations but don't control manual vs automatic commits.
</details>

## Score
- 7/7: Excellent! You understand manual offset management
- 5-6/7: Good grasp of delivery semantics
- 3-4/7: Review commit strategies and their trade-offs
- <3/7: Re-do the lesson focusing on at-least-once vs at-most-once
