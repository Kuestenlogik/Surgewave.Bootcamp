# Knowledge Assessment: Lesson 18.3 - Pause & Resume

## Multiple Choice

### 1. What happens to messages in a paused partition?
- [ ] A) They are deleted
- [ ] B) They accumulate in the broker and are delivered when resumed
- [ ] C) They are redirected to other partitions
- [ ] D) The producer is notified to stop sending

<details>
<summary>Answer</summary>
**B)** Messages continue to accumulate in the broker for paused partitions. When the partition is resumed, consumption continues from where it left off - no messages are lost or redirected.
</details>

### 2. In the backpressure handling pattern, when should you pause partitions?
- [ ] A) When the topic has too many partitions
- [ ] B) When the downstream system is slow or its queue depth exceeds a threshold
- [ ] C) When the producer is sending too fast
- [ ] D) At regular time intervals for maintenance

<details>
<summary>Answer</summary>
**B)** Pause partitions when the downstream system cannot keep up - indicated by high queue depth, slow response times, or high error rates. This prevents overwhelming the downstream system.
</details>

### 3. In priority-based processing, what should happen when the high-priority backlog grows?
- [ ] A) Increase processing speed for all topics
- [ ] B) Pause low-priority partitions and focus on high-priority
- [ ] C) Drop low-priority messages
- [ ] D) Add more consumers

<details>
<summary>Answer</summary>
**B)** Pause low-priority partitions to dedicate all processing capacity to high-priority messages until the backlog is cleared. Then resume low-priority processing.
</details>

## True/False

### 4. When a partition is paused, the consumer still maintains its assigned ownership of that partition.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Pausing a partition only stops message consumption; the consumer retains ownership of the partition. No rebalancing occurs, and other consumers cannot take over the paused partition.
</details>

### 5. You can pause individual partitions while continuing to consume from others assigned to the same consumer.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Pause and resume work at the partition level. You can pause partitions 0 and 2 while continuing to consume from partition 1, enabling fine-grained flow control.
</details>

## Code Challenge

### 6. What's wrong with this rate limiting implementation?
```csharp
var messagesProcessed = 0;
while (!cancellationToken.IsCancellationRequested)
{
    var result = await consumer.ConsumeAsync(cancellationToken);
    await ProcessMessageAsync(result);
    messagesProcessed++;

    if (messagesProcessed >= 100)
    {
        consumer.Pause(assignedPartitions);
        // Never resumes!
    }
}
```

<details>
<summary>Answer</summary>
The code pauses but never resumes, and doesn't reset the counter. A correct rate limiter:
```csharp
var messagesProcessed = 0;
var windowStart = DateTime.UtcNow;

while (!cancellationToken.IsCancellationRequested)
{
    // Check if we should resume (new time window)
    if ((DateTime.UtcNow - windowStart).TotalSeconds >= 1)
    {
        consumer.Resume(assignedPartitions);
        messagesProcessed = 0;
        windowStart = DateTime.UtcNow;
    }

    var result = await consumer.ConsumeAsync(cancellationToken);
    await ProcessMessageAsync(result);
    messagesProcessed++;

    if (messagesProcessed >= 100)
    {
        consumer.Pause(assignedPartitions);
        await Task.Delay(TimeSpan.FromSeconds(1) - (DateTime.UtcNow - windowStart));
    }
}
```
</details>

### 7. Which method would you use to check if a specific partition is currently paused?
- [ ] A) `consumer.IsPaused("orders", 0)`
- [ ] B) `consumer.PausedPartitions.Contains(("orders", 0))`
- [ ] C) `consumer.GetPartitionStatus("orders", 0)`
- [ ] D) `consumer.Partitions["orders", 0].Paused`

<details>
<summary>Answer</summary>
**B)** `consumer.PausedPartitions` returns the set of currently paused partitions, and you can check if a specific partition is in that set using `Contains()`.
</details>

## Score
- 7/7: Excellent! You understand pause and resume patterns
- 5-6/7: Good grasp of flow control techniques
- 3-4/7: Review backpressure and priority processing
- <3/7: Re-do the lesson focusing on when to pause/resume
