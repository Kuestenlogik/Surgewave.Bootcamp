# Knowledge Assessment: Lesson 18.6 - Streaming Consumer

## Multiple Choice

### 1. What does `consumer.GrantCredit(50)` do in a SurgewaveStreamingConsumer?
- [ ] A) Commits the last 50 offsets
- [ ] B) Tells the broker it can push up to 50 more messages
- [ ] C) Pauses the consumer for 50 milliseconds
- [ ] D) Subscribes to 50 additional partitions

<details>
<summary>Answer</summary>
**B)** `GrantCredit` tells the broker how many additional messages it is allowed to push. Until credit is granted the broker holds back, preventing the consumer from being overwhelmed.
</details>

### 2. Which API is correct for consuming messages with SurgewaveStreamingConsumer?
- [ ] A) `while (true) { var msg = await consumer.ConsumeAsync(); }`
- [ ] B) `await foreach (var msg in consumer.ConsumeAllAsync(ct))`
- [ ] C) `consumer.OnMessage += async msg => await Handle(msg);`
- [ ] D) `consumer.Poll(TimeSpan.FromSeconds(1))`

<details>
<summary>Answer</summary>
**B)** `SurgewaveStreamingConsumer` exposes `ConsumeAllAsync(CancellationToken)` which returns `IAsyncEnumerable<ReceivedMessage>`. The `await foreach` pattern is the idiomatic way to consume it.
</details>

### 3. When is push-based consumption (SurgewaveStreamingConsumer) preferable to poll-based?
- [ ] A) When you need manual Seek operations
- [ ] B) When building reactive/event-driven pipelines with IAsyncEnumerable composition
- [ ] C) When implementing exactly-once transactional semantics
- [ ] D) When you need a custom partition assignor

<details>
<summary>Answer</summary>
**B)** Push-based consumers integrate naturally with reactive and async-stream pipelines. Poll-based consumers are better for scenarios requiring manual offset control, custom assignment, or transactional commits.
</details>

## True/False

### 4. You must create a new SurgewaveStreamingConsumer to switch from topic A to topic B.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - You can call `UnsubscribeAsync("topicA")` and then `SubscribeAsync("topicB")` on the same consumer instance without recreating it. The `ConsumeAllAsync` enumerable continues delivering messages from the new subscription.
</details>

### 5. If `InitialCredits` is set to 0, the broker will never push any messages.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Credit is the mechanism by which the consumer authorizes the broker to send records. With 0 initial credits, the broker has no permission to push, and the consumer must call `GrantCredit(n)` before messages will flow.
</details>

## Code Challenge

### 6. What is wrong with this streaming consumer loop?
```csharp
await consumer.SubscribeAsync("events", ct);
await foreach (var msg in consumer.ConsumeAllAsync(ct))
{
    await ProcessAsync(msg);
    // nothing else
}
```

<details>
<summary>Answer</summary>
The loop never calls `consumer.GrantCredit(...)`. After the initial credit budget is exhausted, the broker stops pushing and the loop stalls. Add `consumer.GrantCredit(1)` (or a batch-sized value) after processing each message:
```csharp
await foreach (var msg in consumer.ConsumeAllAsync(ct))
{
    await ProcessAsync(msg);
    consumer.GrantCredit(1);
}
```
</details>

### 7. Which consumption model should you choose for this scenario?
> You are building a billing system that must process each payment exactly once, storing the committed offset in the same database transaction as the payment record.

- [ ] A) SurgewaveStreamingConsumer with AutoCommit = true
- [ ] B) SurgewaveConsumer with manual commit and EnableAutoCommit = false
- [ ] C) SurgewaveStreamingConsumer with InitialCredits = 1
- [ ] D) Either model works equally well

<details>
<summary>Answer</summary>
**B)** Exactly-once processing with transactional offset storage requires manual offset management via `SurgewaveConsumer`. The poll-based model gives precise control over when and how offsets are committed, including inside database transactions.
</details>

## Score
- 7/7: Excellent! You understand push-based streaming consumption
- 5-6/7: Good grasp of the streaming consumer model
- 3-4/7: Review credit-based flow control and lifecycle management
- <3/7: Re-do the lesson focusing on push vs poll trade-offs
