# Knowledge Assessment: Lesson 21.3 - Persistent Deduplication

## Multiple Choice

### 1. What is the purpose of a tombstone (null value) in a compacted topic?
- [ ] A) To mark a message as important
- [ ] B) To indicate the key should be deleted during compaction
- [ ] C) To compress the message
- [ ] D) To trigger a consumer rebalance

<details>
<summary>Answer</summary>
**B)** A tombstone (message with null value) marks that a key should be deleted. During log compaction, the tombstone eventually causes the key to be removed entirely from the topic.
</details>

### 2. Which persistence strategy provides the highest performance with horizontal scaling capabilities?
- [ ] A) Database (SQL)
- [ ] B) Key-Value Store
- [ ] C) Local File
- [ ] D) In-Memory HashSet

<details>
<summary>Answer</summary>
**B)** Key-Value stores (like Redis) provide high performance with horizontal scaling. SQL databases scale vertically, local files don't scale, and in-memory has no durability.
</details>

### 3. What should a checkpoint record contain for deduplication recovery?
- [ ] A) Only the last processed offset
- [ ] B) Only the processed key list
- [ ] C) Topic, partition, offset, timestamp, and processed keys
- [ ] D) Only the consumer group ID

<details>
<summary>Answer</summary>
**C)** A proper checkpoint includes topic, partition, offset, timestamp, and the set of processed keys. This allows complete state recovery and resumption from the correct position.
</details>

### 4. In the hybrid in-memory + persistent architecture, when is the database queried?
- [ ] A) For every message
- [ ] B) Only when the memory cache returns "not found"
- [ ] C) Only during startup
- [ ] D) Never, the cache is authoritative

<details>
<summary>Answer</summary>
**B)** In the hybrid approach, the fast memory cache is checked first. The database is only queried when the cache returns "not found," reducing database load while maintaining durability.
</details>

### 5. What SQL pattern should be used for the deduplication table's cleanup?
- [ ] A) TRUNCATE TABLE processed_messages
- [ ] B) DELETE FROM processed_messages WHERE processed_at < NOW() - INTERVAL '7 days'
- [ ] C) DROP TABLE processed_messages
- [ ] D) UPDATE processed_messages SET expired = true

<details>
<summary>Answer</summary>
**B)** DELETE with a WHERE clause on processed_at removes only expired entries while keeping recent ones. This should run periodically (e.g., daily) to prevent unbounded table growth.
</details>

## True/False

### 6. Compacted topics eventually remove all but the latest value for each key.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Log compaction retains only the most recent value for each key. Older values for the same key are removed during compaction, reducing storage while maintaining the latest state.
</details>

### 7. After recovering from a checkpoint, messages between the checkpoint offset and the crash point will never be reprocessed.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Messages between the checkpoint offset and the crash point WILL be reprocessed. However, the deduplication logic (using the restored processed_keys set) will identify and skip any that were already processed.
</details>

## Code Challenge

### 8. What's wrong with this checkpoint-based recovery implementation?

```csharp
async Task RecoverFromCheckpoint()
{
    var checkpoint = await LoadLatestCheckpoint();

    // Restore consumer position
    consumer.Seek(checkpoint.Topic, checkpoint.Partition, checkpoint.Offset);

    // Start processing
    while (true)
    {
        var message = await consumer.ConsumeAsync();
        await ProcessMessage(message);
    }
}

async Task ProcessMessage(Message message)
{
    await HandleBusinessLogic(message);
    processedKeys.Add(message.IdempotencyKey);
}
```

<details>
<summary>Answer</summary>
The recovery doesn't restore the processed keys from the checkpoint, so messages between the checkpoint and crash will be processed again as duplicates:

```csharp
async Task RecoverFromCheckpoint()
{
    var checkpoint = await LoadLatestCheckpoint();

    // MISSING: Restore processed keys from checkpoint!
    foreach (var key in checkpoint.ProcessedKeys)
    {
        processedKeys.Add(key);
    }

    // Restore consumer position
    consumer.Seek(checkpoint.Topic, checkpoint.Partition, checkpoint.Offset);

    // Start processing
    while (true)
    {
        var message = await consumer.ConsumeAsync();
        await ProcessMessage(message);
    }
}

async Task ProcessMessage(Message message)
{
    // MISSING: Check for duplicates before processing!
    if (processedKeys.Contains(message.IdempotencyKey))
    {
        Console.WriteLine("Duplicate from recovery window, skipping");
        return;
    }

    await HandleBusinessLogic(message);
    processedKeys.Add(message.IdempotencyKey);
}
```

Two critical fixes: (1) Restore processed keys from checkpoint before resuming, (2) Check for duplicates in ProcessMessage to handle messages that were processed but not checkpointed.
</details>

## Score
- 8/8: Excellent!
- 6-7/8: Good understanding
- 4-5/8: Review the lesson
- <4/8: Re-do the lesson
