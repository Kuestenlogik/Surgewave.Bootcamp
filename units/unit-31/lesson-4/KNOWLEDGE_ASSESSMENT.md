# Knowledge Assessment: Lesson 21.4 - Idempotent Operations

## Multiple Choice

### 1. What makes the operation "SET balance = balance + 100" non-idempotent?
- [ ] A) It uses the SET keyword
- [ ] B) It references the current value, causing different results each execution
- [ ] C) It modifies the balance column
- [ ] D) It uses addition

<details>
<summary>Answer</summary>
**B)** The operation references the current value (balance = balance + 100), so each execution produces a different result. The first execution adds 100, the second adds another 100, etc.
</details>

### 2. In version-based optimistic concurrency, what happens when the WHERE clause doesn't match?
- [ ] A) The database throws an exception
- [ ] B) The row is deleted
- [ ] C) Rows affected = 0, indicating a concurrent modification or duplicate
- [ ] D) The transaction is automatically retried

<details>
<summary>Answer</summary>
**C)** When the version doesn't match, the UPDATE affects 0 rows. This indicates either concurrent modification (version changed) or a duplicate request (already processed and version incremented).
</details>

### 3. What is the purpose of storing an "in_progress" status in an idempotency token system?
- [ ] A) To track slow consumers
- [ ] B) To handle concurrent duplicate requests during processing
- [ ] C) To improve query performance
- [ ] D) To compress the response

<details>
<summary>Answer</summary>
**B)** The "in_progress" status handles the race condition where duplicate requests arrive while the first is still processing. Subsequent requests see "in_progress" and can wait or return an appropriate response.
</details>

### 4. Which SQL clause makes an INSERT operation idempotent?
- [ ] A) WHERE NOT EXISTS
- [ ] B) ON CONFLICT DO UPDATE / ON DUPLICATE KEY UPDATE
- [ ] C) HAVING COUNT(*) = 0
- [ ] D) GROUP BY id

<details>
<summary>Answer</summary>
**B)** ON CONFLICT DO UPDATE (PostgreSQL/SQLite) or ON DUPLICATE KEY UPDATE (MySQL) makes INSERT idempotent by converting it to an upsert that produces the same result regardless of prior state.
</details>

### 5. In the "effectful once" pattern, where should the effect execution record be stored?
- [ ] A) In a separate microservice
- [ ] B) In the message queue
- [ ] C) In the same database transaction as the business operation
- [ ] D) In application memory

<details>
<summary>Answer</summary>
**C)** The effect execution record must be stored in the same transaction as the business operation. This ensures atomicity: either both the effect record and business change commit, or neither does.
</details>

## True/False

### 6. HTTP PUT is generally considered idempotent while HTTP POST is not.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - PUT replaces a resource entirely, producing the same result regardless of repetition. POST typically creates new resources, creating duplicates if repeated.
</details>

### 7. An idempotency token's TTL should be set to infinite to ensure all duplicates are caught.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Idempotency tokens should have a reasonable TTL (e.g., 24 hours to 7 days) to prevent unbounded storage growth. Very late duplicates are rare and often indicate other issues that should be investigated.
</details>

## Code Challenge

### 8. What's wrong with this upsert implementation?

```csharp
async Task ProcessOrder(OrderMessage message)
{
    var sql = @"
        INSERT INTO orders (id, status, amount, updated_at)
        VALUES (@id, @status, @amount, @now)
        ON CONFLICT (id) DO UPDATE SET
            status = EXCLUDED.status,
            amount = EXCLUDED.amount,
            updated_at = EXCLUDED.updated_at";

    await db.ExecuteAsync(sql, new
    {
        id = message.OrderId,
        status = message.Status,
        amount = message.Amount,
        now = DateTime.UtcNow
    });
}
```

<details>
<summary>Answer</summary>
The upsert doesn't protect against out-of-order message processing. A newer message might be overwritten by an older duplicate arriving later:

```csharp
async Task ProcessOrder(OrderMessage message)
{
    var sql = @"
        INSERT INTO orders (id, status, amount, updated_at, message_timestamp)
        VALUES (@id, @status, @amount, @now, @messageTs)
        ON CONFLICT (id) DO UPDATE SET
            status = EXCLUDED.status,
            amount = EXCLUDED.amount,
            updated_at = EXCLUDED.updated_at,
            message_timestamp = EXCLUDED.message_timestamp
        WHERE orders.message_timestamp < EXCLUDED.message_timestamp";  -- Only update if newer

    var rowsAffected = await db.ExecuteAsync(sql, new
    {
        id = message.OrderId,
        status = message.Status,
        amount = message.Amount,
        now = DateTime.UtcNow,
        messageTs = message.Timestamp
    });

    if (rowsAffected == 0)
    {
        // Either inserted or message was older than current state
        Console.WriteLine($"Order {message.OrderId}: no update needed (newer state exists)");
    }
}
```

The WHERE clause ensures only newer messages update the record. This handles both duplicates and out-of-order delivery.
</details>

## Score
- 8/8: Excellent!
- 6-7/8: Good understanding
- 4-5/8: Review the lesson
- <4/8: Re-do the lesson
