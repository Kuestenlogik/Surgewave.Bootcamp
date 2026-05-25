# Knowledge Assessment: Lesson 18.5 - Partition Assignment

## Multiple Choice

### 1. Which assignment strategy minimizes partition movement during rebalancing?
- [ ] A) Range Assignment
- [ ] B) RoundRobin Assignment
- [ ] C) Sticky Assignment
- [ ] D) Random Assignment

<details>
<summary>Answer</summary>
**C)** Sticky Assignment tries to preserve existing partition assignments when possible, only moving partitions that must be reassigned. This minimizes state transfer and recovery time for stateful consumers.
</details>

### 2. In Range Assignment with 7 partitions and 3 consumers, how are partitions distributed?
- [ ] A) 2, 2, 3 partitions per consumer
- [ ] B) 3, 2, 2 partitions per consumer
- [ ] C) 7, 0, 0 partitions per consumer
- [ ] D) Equal distribution is impossible

<details>
<summary>Answer</summary>
**B)** Range assignment gives the first consumer(s) one extra partition when division isn't even. With 7/3 = 2 remainder 1: C1 gets 3 (P0, P1, P2), C2 gets 2 (P3, P4), C3 gets 2 (P5, P6).
</details>

### 3. What should you do in the PartitionsRevoked event handler?
- [ ] A) Start processing messages immediately
- [ ] B) Commit pending offsets and save any buffered state before partitions are taken away
- [ ] C) Request more partitions
- [ ] D) Ignore it - Surgewave handles everything

<details>
<summary>Answer</summary>
**B)** PartitionsRevoked is your last chance to commit offsets and flush any buffered state before losing the partitions. Failing to do so may result in duplicate processing when another consumer takes over.
</details>

## True/False

### 4. RoundRobin assignment is better than Range assignment when subscribing to multiple topics.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - RoundRobin distributes partitions more evenly across consumers when multiple topics are involved, alternating between consumers for each partition regardless of topic. Range can create uneven distribution with multiple topics.
</details>

### 5. Manual partition assignment (using Assign instead of Subscribe) still requires a consumer group ID.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Manual assignment bypasses the consumer group mechanism entirely. You don't need or use a GroupId - you directly assign specific partitions to the consumer and manage offsets yourself.
</details>

## Code Challenge

### 6. What's wrong with this rebalance handler?
```csharp
consumer.PartitionsAssigned += (sender, e) =>
{
    Console.WriteLine($"Assigned {e.Partitions.Count} partitions");
};

consumer.PartitionsRevoked += (sender, e) =>
{
    Console.WriteLine($"Revoked {e.Partitions.Count} partitions");
};
```

<details>
<summary>Answer</summary>
The handlers only log but don't actually handle the events properly. PartitionsRevoked should commit offsets and save state; PartitionsAssigned should load state for new partitions:
```csharp
consumer.PartitionsRevoked += async (sender, e) =>
{
    Console.WriteLine($"Revoked {e.Partitions.Count} partitions");
    // Commit any pending offsets before losing partitions
    await consumer.CommitAsync();
    // Save any buffered state
    await FlushBuffersAsync();
};

consumer.PartitionsAssigned += async (sender, e) =>
{
    Console.WriteLine($"Assigned {e.Partitions.Count} partitions");
    // Load state for newly assigned partitions
    foreach (var tp in e.Partitions)
    {
        await LoadStateForPartitionAsync(tp);
    }
};
```
</details>

### 7. When would you choose manual partition assignment over consumer group subscription?
- [ ] A) When you want automatic load balancing
- [ ] B) When implementing exactly-once processing with external orchestration or testing specific partitions
- [ ] C) When you have too many consumers
- [ ] D) When you want simpler code

<details>
<summary>Answer</summary>
**B)** Manual assignment is useful for exactly-once processing (where you control partition ownership), testing specific partitions, or when using external orchestration (like Kubernetes StatefulSets) to manage partition assignment.
</details>

## Score
- 7/7: Excellent! You understand partition assignment strategies
- 5-6/7: Good grasp of consumer group coordination
- 3-4/7: Review assignment strategies and rebalance events
- <3/7: Re-do the lesson focusing on Range vs RoundRobin vs Sticky
