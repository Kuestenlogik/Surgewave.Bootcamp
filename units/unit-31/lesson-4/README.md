# Lesson 21.4: Idempotent Operations

> **Difficulty:** Advanced | **Duration:** 45 min | **Prerequisites:** Lesson 21.3

## Overview

Learn how to design and implement operations that are safe to execute multiple times. This lesson covers database upserts, atomic operations, and idempotency tokens for building resilient message processors.

## Learning Objectives

- Understand the difference between idempotent and non-idempotent operations
- Implement database upserts for safe repeated writes
- Use atomic compare-and-swap operations
- Design idempotency tokens for external API calls
- Apply the "effectful once" pattern

## Idempotent vs Non-Idempotent Operations

**Idempotent** — f(f(x)) = f(x):

| Operation | Why Idempotent |
|-----------|---------------|
| `SET x = 5` | Same result regardless of count |
| `DELETE WHERE id = 123` | Already deleted = no-op |
| `PUT /resources/123` | Replaces entire resource |
| `UPSERT (ON CONFLICT UPDATE)` | Insert or update to same state |
| `HTTP GET, HEAD, OPTIONS` | Read-only, no side effects |

**Non-Idempotent** — f(f(x)) ≠ f(x):

| Operation | Why Non-Idempotent |
|-----------|-------------------|
| `x = x + 1` | Increments each time |
| `INSERT INTO (no conflict)` | Creates duplicate rows |
| `POST /resources` | Creates new resource each time |
| `SendEmail()` | Sends multiple emails |
| `TransferMoney()` | Transfers multiple times |

## Database Upsert Patterns

**PostgreSQL / SQLite:**
```sql
INSERT INTO orders (id, status, amount, updated_at)
VALUES (@id, @status, @amount, @now)
ON CONFLICT (id) DO UPDATE SET
  status = EXCLUDED.status,
  amount = EXCLUDED.amount,
  updated_at = EXCLUDED.updated_at
WHERE orders.updated_at < EXCLUDED.updated_at;  -- Only newer
```

**SQL Server:**
```sql
MERGE INTO orders AS target
USING (SELECT @id, @status, @amount, @now) AS source
ON target.id = source.id
WHEN MATCHED THEN UPDATE SET status = source.status, ...
WHEN NOT MATCHED THEN INSERT (id, status, ...) VALUES (...);
```

**MySQL:**
```sql
INSERT INTO orders (id, status, amount, updated_at)
VALUES (@id, @status, @amount, @now)
ON DUPLICATE KEY UPDATE
  status = VALUES(status),
  amount = VALUES(amount),
  updated_at = VALUES(updated_at);
```

## Version-Based Idempotency

Database state for account 123: `balance = 1000.00, version = 5`.

Message: "Add $100 to account 123 (expecting version 5)"

```sql
UPDATE accounts
SET balance = 1100.00,
    version = 6,
    updated_at = NOW()
WHERE id = 123 AND version = 5
```

- **First execution**: Rows affected = 1 (SUCCESS)
- **Second execution**: Rows affected = 0 (version mismatch — idempotent!)

## Idempotency Token Pattern

**Client Request:**
```
POST /api/payments
Idempotency-Key: pay_abc123xyz
{"amount": 99.99, "recipient": "user@example.com"}
```

**Server Processing:**
1. Check idempotency store for key `pay_abc123xyz`
   - If found → return cached response (HTTP 200)
   - If not found:
     2. Store key with `in_progress` status
     3. Process payment
     4. Store result with key
     5. Return response

**Idempotency Store:**

| key | status | response | expires_at |
|-----|--------|----------|-----------|
| pay_abc123xyz | completed | `{"id": "p_1"...}` | 2024-01-16 |
| pay_def456uvw | in_progress | null | 2024-01-16 |

## Effectful Once Pattern

**Problem:** External side effects (email, SMS, webhook) can't be undone.

**Solution:** Track effect execution in the same transaction as the state change.

```sql
BEGIN TRANSACTION

-- Check if effect already executed
SELECT 1 FROM executed_effects WHERE effect_id = @idempotencyKey

-- IF NOT EXISTS:
--   Record effect before executing
INSERT INTO executed_effects (effect_id, created_at)
VALUES (@idempotencyKey, NOW())

--   Execute the effect (outside transaction for external calls)
--   If effect fails, transaction rolls back

COMMIT
```

If a duplicate message arrives: the query finds an existing record → effect is NOT executed again → message is acknowledged as processed.

## Best Practices

| Practice | Description |
|----------|-------------|
| Include version in updates | Prevent lost updates from concurrent processing |
| Use database transactions | Atomically check + execute + record |
| Store idempotency results | Return same response for duplicate requests |
| Set TTL on idempotency records | Prevent unbounded storage growth |
| Log duplicate detections | Monitor for excessive retries |

## Exercise

In this exercise, you will:

1. Implement database upserts for order processing
2. Create version-based optimistic concurrency
3. Build an idempotency token system
4. Implement the effectful once pattern
5. Test with simulated duplicates and failures

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
