# Quiz: Lesson 1.4 - Consumer Groups

## Multiple Choice

### 1. What happens when a consumer in a group fails?
- [ ] A) All messages are lost
- [ ] B) The group stops consuming
- [ ] C) Its partitions are reassigned to other consumers in the group
- [ ] D) A new consumer is automatically created

<details>
<summary>Answer</summary>
**C)** Its partitions are reassigned to other consumers in the group. This is the rebalancing mechanism that provides fault tolerance in consumer groups.
</details>

### 2. In a consumer group, how many consumers can read from a single partition?
- [ ] A) Unlimited consumers
- [ ] B) Exactly one consumer
- [ ] C) Up to three consumers
- [ ] D) Depends on the replication factor

<details>
<summary>Answer</summary>
**B)** Exactly one consumer. Each partition is assigned to exactly one consumer in the group, ensuring ordered processing and preventing duplicate consumption within the group.
</details>

### 3. What is the purpose of offset commits?
- [ ] A) To persist messages to disk
- [ ] B) To track the consumer group's progress through the topic
- [ ] C) To acknowledge message delivery to producers
- [ ] D) To compress messages for storage

<details>
<summary>Answer</summary>
**B)** To track the consumer group's progress through the topic. Committed offsets allow consumers to resume from where they left off after restarts.
</details>

### 4. What is the default behavior for offset commits in Surgewave?
- [ ] A) Manual commit only
- [ ] B) Commit after each message
- [ ] C) Auto-commit at periodic intervals
- [ ] D) No commits, always start from beginning

<details>
<summary>Answer</summary>
**C)** Auto-commit at periodic intervals. By default, `EnableAutoCommit` is true and offsets are committed automatically based on `AutoCommitIntervalMs`.
</details>

## True/False

### 5. Setting `EnableAutoCommit = false` means you must call `CommitAsync()` manually to save progress.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - When auto-commit is disabled, the consumer will not automatically commit offsets. You must explicitly call `CommitAsync()` to save progress.
</details>

### 6. A rebalancing event occurs only when consumers crash.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Rebalancing occurs whenever consumers join OR leave the group, not just when they crash. Adding new consumers also triggers rebalancing.
</details>

## Code Challenge

### 7. What's wrong with this consumer group setup?

```csharp
await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
    options.EnableAutoCommit = false;
});

consumer.Subscribe("orders");

while (true)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(1));
    if (result != null)
    {
        await ProcessOrder(result.Value);
        // What's missing here?
    }
}
```

<details>
<summary>Answer</summary>
The issue is that **GroupId is not set** and **offsets are never committed**.

1. Consumer groups require a `GroupId` to coordinate
2. With `EnableAutoCommit = false`, you must manually commit

Fix:
```csharp
await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.GroupId = "order-processors";  // Required for consumer groups
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
    options.EnableAutoCommit = false;
});

consumer.Subscribe("orders");

while (true)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(1));
    if (result != null)
    {
        await ProcessOrder(result.Value);
        await consumer.CommitAsync(result);  // Commit after processing
    }
}
```
</details>

## Score
- 7/7: Excellent! You understand consumer groups
- 5-6/7: Good understanding of group coordination
- 3-4/7: Review offset management and rebalancing
- <3/7: Re-do the lesson focusing on group concepts
