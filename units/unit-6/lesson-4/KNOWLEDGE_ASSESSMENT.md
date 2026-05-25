# Knowledge Assessment: Lesson 18.4 - Custom Checkpointing

## Multiple Choice

### 1. Why would you use custom checkpointing instead of Surgewave's built-in offset storage?
- [ ] A) To reduce network traffic
- [ ] B) To achieve exactly-once processing by storing offsets atomically with application state
- [ ] C) To support more partitions
- [ ] D) To enable compression

<details>
<summary>Answer</summary>
**B)** Custom checkpointing enables exactly-once processing by storing the offset in the same transaction as the application state change. If both succeed together, you guarantee the message was processed exactly once.
</details>

### 2. What is the core problem that custom checkpointing solves?
- [ ] A) Slow message processing
- [ ] B) The database write and offset commit are separate operations that can fail independently
- [ ] C) Too many consumers in a group
- [ ] D) Message serialization errors

<details>
<summary>Answer</summary>
**B)** The exactly-once problem occurs because database write and offset commit are separate operations. If a crash occurs between them, you get either data written twice (if offset wasn't committed) or data lost (if offset was committed but write failed).
</details>

### 3. Which checkpoint storage option is best for high-performance distributed consumers?
- [ ] A) File-based checkpoint
- [ ] B) Database checkpoint
- [ ] C) Redis checkpoint
- [ ] D) Application state

<details>
<summary>Answer</summary>
**C)** Redis provides high-performance, distributed checkpoint storage ideal for consumers that need fast reads/writes and coordination across multiple instances.
</details>

## True/False

### 4. In the transactional pattern, if the commit fails, both the database write AND the checkpoint update are rolled back.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - By performing both operations in the same database transaction, either both succeed (commit) or both fail (rollback). This atomicity is what enables exactly-once semantics.
</details>

### 5. File-based checkpointing is recommended for distributed consumer deployments.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - File-based checkpointing is simple and good for single-consumer or development scenarios, but it doesn't work well for distributed deployments where multiple consumers need to coordinate. Use database or Redis for distributed scenarios.
</details>

## Code Challenge

### 6. What's wrong with this checkpoint recovery code?
```csharp
var checkpoint = await LoadCheckpointAsync();
// checkpoint = { "orders-0": 150, "orders-1": 200 }

consumer.Subscribe("orders");

foreach (var (tp, offset) in checkpoint)
{
    consumer.Seek(tp.Topic, tp.Partition, offset);
}
```

<details>
<summary>Answer</summary>
The seek happens after Subscribe, but partition assignment might not have happened yet. Also, if the checkpoint stores the LAST processed offset, you should seek to offset + 1 to avoid reprocessing. Better approach:
```csharp
var checkpoint = await LoadCheckpointAsync();

// Handle assignment event to seek after partitions are assigned
consumer.PartitionsAssigned += async (sender, e) =>
{
    foreach (var tp in e.Partitions)
    {
        if (checkpoint.TryGetValue((tp.Topic, tp.Partition), out var lastOffset))
        {
            consumer.Seek(tp.Topic, tp.Partition, lastOffset + 1);
        }
    }
};

consumer.Subscribe("orders");
```
</details>

### 7. In the transactional checkpoint pattern, what should be stored in the checkpoint table?
- [ ] A) Only the topic name
- [ ] B) Topic, partition, and the next offset to process
- [ ] C) The entire message content
- [ ] D) Only the consumer group ID

<details>
<summary>Answer</summary>
**B)** The checkpoint should store the topic, partition, and offset. Typically you store the offset of the last successfully processed message (and seek to offset + 1 on recovery) or store the next offset to process directly.
</details>

## Score
- 7/7: Excellent! You understand custom checkpointing
- 5-6/7: Good grasp of exactly-once patterns
- 3-4/7: Review the transactional pattern and recovery flow
- <3/7: Re-do the lesson focusing on why custom checkpointing matters
