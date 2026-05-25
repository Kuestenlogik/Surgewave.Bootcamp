# Knowledge Assessment: Lesson 9.4 - Common Issues

## Multiple Choice

### 1. How is consumer lag calculated?
- [ ] A) Current Time - Message Timestamp
- [ ] B) Latest Offset - Committed Offset
- [ ] C) Total Messages - Processed Messages
- [ ] D) Queue Size - Buffer Size

<details>
<summary>Answer</summary>
**B)** Consumer lag is calculated as: Latest Offset - Committed Offset. This tells you how many messages are waiting to be processed.
</details>

### 2. What happens when a consumer fails to send heartbeats within the session timeout?
- [ ] A) Messages are duplicated
- [ ] B) The consumer is considered dead and a rebalance is triggered
- [ ] C) The topic is deleted
- [ ] D) The producer stops sending messages

<details>
<summary>Answer</summary>
**B)** If a consumer doesn't heartbeat within the session timeout, the broker considers it dead and triggers a partition rebalance, redistributing its partitions to other consumers in the group.
</details>

### 3. Which timeout determines the maximum time between poll() calls before triggering a rebalance?
- [ ] A) Session Timeout
- [ ] B) Heartbeat Interval
- [ ] C) Max Poll Interval
- [ ] D) Request Timeout

<details>
<summary>Answer</summary>
**C)** Max Poll Interval (default 5 minutes) is the maximum time between poll() calls. If processing takes longer than this, the consumer is considered stuck and a rebalance is triggered.
</details>

## True/False

### 4. Partition rebalancing only occurs when new consumers join a consumer group.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Rebalancing occurs when consumers join OR leave the group, when topics/partitions are added, or when a consumer fails to heartbeat. Any change to the group membership triggers rebalancing.
</details>

### 5. A sudden spike in consumer lag after a consumer restart is typically a problem that needs investigation.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - A sudden spike after restart is expected behavior. The consumer needs to catch up from its last committed offset. This is only a problem if lag continues to increase rather than decrease as the consumer catches up.
</details>

## Code Challenge

### 6. This consumer is experiencing frequent rebalances. What's the likely issue?

```csharp
while (true)
{
    var message = await consumer.ConsumeAsync();

    // Complex processing that takes 6 minutes
    await ProcessComplexWorkload(message);

    await consumer.CommitAsync();
}
```

<details>
<summary>Answer</summary>
The issue is that **processing takes 6 minutes, which exceeds the default Max Poll Interval of 5 minutes**. The broker thinks the consumer is stuck and triggers a rebalance.

Solutions:
1. Increase max.poll.interval.ms to accommodate long processing
2. Process messages asynchronously and poll more frequently
3. Reduce processing time by optimizing or parallelizing work

```csharp
// Option 1: Increase timeout
consumer.Configure(new Dictionary<string, string>
{
    ["max.poll.interval.ms"] = "600000"  // 10 minutes
});

// Option 2: Async processing
while (true)
{
    var message = await consumer.ConsumeAsync();
    _ = Task.Run(() => ProcessComplexWorkload(message));  // Don't await
    await consumer.CommitAsync();  // Commit immediately
}
```
</details>

### 7. What is the problem with this consumer lag monitoring approach?

```csharp
var lag = latestOffset - consumer.CurrentPosition;
if (lag > 1000)
{
    Console.WriteLine("WARNING: High lag detected!");
}
```

<details>
<summary>Answer</summary>
The issue is using **CurrentPosition instead of the Committed Offset**. CurrentPosition shows what the consumer has fetched, but not what has been successfully processed and committed. If the consumer crashes, it will re-process from the committed offset, not the current position.

Fix:
```csharp
var committedOffset = await consumer.GetCommittedOffsetAsync(partition);
var lag = latestOffset - committedOffset;
if (lag > 1000)
{
    Console.WriteLine($"WARNING: High lag detected! Lag: {lag} messages");
}
```

Use the committed offset for accurate lag calculation that accounts for potential re-processing after failures.
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
