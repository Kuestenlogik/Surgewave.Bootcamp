# Knowledge Assessment: Lesson 21.1 - Deduplication Concepts

## Multiple Choice

### 1. Which delivery semantic commits the offset BEFORE processing the message?
- [ ] A) At-least-once
- [ ] B) At-most-once
- [ ] C) Exactly-once
- [ ] D) Best-effort

<details>
<summary>Answer</summary>
**B)** At-most-once commits the offset before processing. If processing fails, the message is lost (never redelivered), but there are zero duplicates.
</details>

### 2. What is the recommended idempotency key strategy when you have a natural business identifier?
- [ ] A) Generate a UUID
- [ ] B) Use the message offset
- [ ] C) Use the natural key (e.g., order ID)
- [ ] D) Hash the message content

<details>
<summary>Answer</summary>
**C)** When a natural business identifier exists (like order ID), it's the preferred idempotency key strategy because it has business meaning and is easy to debug.
</details>

### 3. Why do message duplicates occur during a consumer group rebalance?
- [ ] A) The broker sends duplicate messages
- [ ] B) Messages are reprocessed because offsets weren't committed
- [ ] C) Network packets are duplicated
- [ ] D) The producer resends messages

<details>
<summary>Answer</summary>
**B)** During rebalance, a consumer may have processed messages but not yet committed offsets. The new consumer receiving the partition starts from the last committed offset, reprocessing those messages.
</details>

### 4. Which operation is idempotent?
- [ ] A) INSERT INTO orders (id, total) VALUES (123, 100)
- [ ] B) UPDATE accounts SET balance = balance + 100
- [ ] C) INSERT INTO orders ... ON CONFLICT (id) DO UPDATE SET total = 100
- [ ] D) DELETE FROM orders; INSERT INTO orders ...

<details>
<summary>Answer</summary>
**C)** UPSERT (INSERT ... ON CONFLICT DO UPDATE) is idempotent because it produces the same result whether executed once or multiple times. Regular INSERT creates duplicates, and balance += 100 increments each time.
</details>

### 5. What is the correct order of steps in deduplication processing?
- [ ] A) Process -> Check duplicate -> Store key -> Commit
- [ ] B) Check duplicate -> Process -> Store key -> Commit
- [ ] C) Store key -> Check duplicate -> Process -> Commit
- [ ] D) Commit -> Check duplicate -> Process -> Store key

<details>
<summary>Answer</summary>
**B)** The correct order is: Check for duplicate first (fail fast), then process if not duplicate, store the key to mark as processed, then commit the offset.
</details>

## True/False

### 6. Relying solely on message offset for deduplication is sufficient for most production systems.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Message offsets are not sufficient because: (1) the same message content can exist at different offsets if retried by producers, (2) offsets are partition-specific, and (3) offsets don't survive topic recreation. Always include an idempotency key in the message itself.
</details>

### 7. A SET operation (e.g., SET x = 5) is idempotent because executing it multiple times produces the same result.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - SET x = 5 is idempotent because no matter how many times you execute it, x will always be 5. This is in contrast to x = x + 5 which increases x on each execution.
</details>

## Code Challenge

### 8. What's wrong with this deduplication implementation?

```csharp
var processedKeys = new HashSet<string>();

async Task ProcessMessage(Message message)
{
    await HandleBusinessLogic(message);

    if (processedKeys.Contains(message.IdempotencyKey))
    {
        Console.WriteLine("Duplicate detected");
        return;
    }

    processedKeys.Add(message.IdempotencyKey);
    await CommitOffset(message);
}
```

<details>
<summary>Answer</summary>
The duplicate check happens AFTER the business logic is processed, not before. This means duplicates are still processed:

```csharp
var processedKeys = new HashSet<string>();

async Task ProcessMessage(Message message)
{
    // Check for duplicate FIRST (before processing)
    if (processedKeys.Contains(message.IdempotencyKey))
    {
        Console.WriteLine("Duplicate detected, skipping");
        await CommitOffset(message);  // Still commit to advance past duplicate
        return;
    }

    // Process only if not a duplicate
    await HandleBusinessLogic(message);

    // Mark as processed after successful processing
    processedKeys.Add(message.IdempotencyKey);
    await CommitOffset(message);
}
```

The deduplication check must happen before processing to prevent duplicate side effects. Also, consider adding the key before processing (with rollback on failure) to prevent race conditions.
</details>

## Score
- 8/8: Excellent!
- 6-7/8: Good understanding
- 4-5/8: Review the lesson
- <4/8: Re-do the lesson
