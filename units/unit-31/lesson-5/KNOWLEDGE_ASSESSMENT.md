# Knowledge Assessment: Lesson 21.5 - End-to-End Exactly-Once

## Multiple Choice

### 1. What is the primary purpose of the transactional outbox pattern?
- [ ] A) To improve message throughput
- [ ] B) To ensure business operation and event publishing are atomic
- [ ] C) To reduce database storage
- [ ] D) To encrypt messages

<details>
<summary>Answer</summary>
**B)** The outbox pattern ensures atomicity between the business operation and event publishing. Both the business data change and the outbox record are written in the same database transaction.
</details>

### 2. In a saga pattern, what is a "compensation" action?
- [ ] A) A retry of the failed step
- [ ] B) An action that undoes a previously completed step
- [ ] C) A payment to a third party
- [ ] D) A timeout handler

<details>
<summary>Answer</summary>
**B)** A compensation action undoes or reverses a previously completed step when a later step fails. For example, "Release Inventory" compensates for "Reserve Inventory" if payment fails.
</details>

### 3. What does "read_committed" isolation level mean for Surgewave consumers?
- [ ] A) Consumers can read any message
- [ ] B) Consumers only read messages from committed transactions
- [ ] C) Consumers commit offsets automatically
- [ ] D) Consumers read messages in commit order

<details>
<summary>Answer</summary>
**B)** With read_committed isolation, consumers only see messages from transactions that have been committed. Messages from aborted transactions or in-progress transactions are not visible.
</details>

### 4. In the end-to-end exactly-once pipeline, what guarantee does the source outbox provide?
- [ ] A) Messages are compressed
- [ ] B) Business operation and event write succeed or fail together
- [ ] C) Messages are encrypted
- [ ] D) Messages are delivered in order

<details>
<summary>Answer</summary>
**B)** The outbox ensures the business operation and event write are in the same database transaction, so they atomically succeed or fail together. The publisher then reliably delivers events to Surgewave.
</details>

### 5. What happens to offset commits in a Surgewave transaction that gets aborted?
- [ ] A) Offsets are committed anyway
- [ ] B) Offsets are rolled back along with the transaction
- [ ] C) Offsets are committed to a different topic
- [ ] D) Offsets are lost permanently

<details>
<summary>Answer</summary>
**B)** In transactional processing, offset commits are part of the transaction. If the transaction aborts, offsets are rolled back and the consumer will re-read the messages.
</details>

### 6. What is the "published" flag in an outbox table used for?
- [ ] A) To mark messages as compressed
- [ ] B) To track which events have been successfully sent to Surgewave
- [ ] C) To indicate message priority
- [ ] D) To enable message filtering

<details>
<summary>Answer</summary>
**B)** The "published" flag tracks which outbox events have been successfully produced to Surgewave. The outbox publisher reads unpublished events, sends them, and marks them published.
</details>

## True/False

### 7. In a saga, if step 3 fails, only step 3 needs to be compensated.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - If step 3 fails, all previously completed steps (steps 1 and 2) must be compensated in reverse order. The saga must unwind all state changes made before the failure.
</details>

## Code Challenge

### 8. What's wrong with this exactly-once processing implementation?

```csharp
async Task ProcessWithExactlyOnce(Message message)
{
    await producer.BeginTransactionAsync();

    try
    {
        // Process and produce result
        var result = await Transform(message);
        await producer.ProduceAsync("results", result);

        // Commit offset
        await consumer.CommitAsync(message);

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
The offset commit is done separately from the transaction, breaking atomicity. The offset should be committed as part of the same transaction:

```csharp
async Task ProcessWithExactlyOnce(Message message)
{
    await producer.BeginTransactionAsync();

    try
    {
        // Process and produce result
        var result = await Transform(message);
        await producer.ProduceAsync("results", result);

        // Send offsets to transaction (not direct commit!)
        await producer.SendOffsetsToTransactionAsync(
            new[] { new TopicPartitionOffset(message.Topic, message.Partition, message.Offset + 1) },
            consumer.ConsumerGroupMetadata);

        // Commit transaction (includes both produce and offset)
        await producer.CommitTransactionAsync();
    }
    catch
    {
        await producer.AbortTransactionAsync();
        throw;
    }
}
```

Key change: Use `SendOffsetsToTransactionAsync` instead of `consumer.CommitAsync`. This includes the offset in the transaction, so if the transaction aborts, the offset is not committed and the message will be reprocessed.
</details>

## Score
- 8/8: Excellent!
- 6-7/8: Good understanding
- 4-5/8: Review the lesson
- <4/8: Re-do the lesson
