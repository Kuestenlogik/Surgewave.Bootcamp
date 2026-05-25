# Lesson 18.4: Custom Checkpointing

> **Difficulty:** Advanced | **Duration:** 45 min | **Prerequisites:** Lesson 18.3

## Overview

Learn how to implement custom checkpoint storage for exactly-once semantics when integrating with external systems like databases, Redis, or files.

## Learning Objectives

- Store offsets in external systems (file, database, Redis)
- Implement exactly-once processing with external state
- Recover from failures using custom checkpoints
- Coordinate offset commits with external transactions

## Why Custom Checkpointing?

Built-in offset storage has limitations for exactly-once processing:

| Approach | Use Case | Guarantee |
|----------|----------|-----------|
| Auto-commit | Simple consumers | At-most-once |
| Manual commit to Surgewave | Standalone consumers | At-least-once |
| Custom checkpoint | External state integration | Exactly-once* |

*With proper transaction coordination

## Key Concepts

### The Exactly-Once Problem

The standard at-least-once flow — Receive → Process → Write to Database → Commit Offset — has a gap: if a crash occurs between the database write and the offset commit, the data is written but the offset is not committed. On restart the message is reprocessed and the data is written twice. Because the two operations are separate, a crash between them causes inconsistency.

### Custom Checkpoint Solution

The solution is to store the offset together with the application data inside the same database transaction:

```sql
BEGIN TRANSACTION
  INSERT data           -- application write
  UPDATE checkpoint     -- offset write
COMMIT                  -- both succeed or both fail (atomic)
```

### Checkpoint Storage Options

| Storage | Example | Good For |
|---------|---------|----------|
| **File-based** | `{ "orders-0": 150, "orders-1": 200 }` JSON file | Single consumer, development, simple deployments |
| **Database** | `UPDATE checkpoints SET offset = @offset WHERE ...` | Database-backed applications, transactional exactly-once |
| **Redis** | `HSET checkpoints "orders-0" "150"` | High-performance, distributed consumers |
| **Application state** | `order.LastProcessedOffset = result.Offset` | Event sourcing, CQRS patterns |

### Recovery Flow

On startup: Load checkpoint from storage → Seek to saved offset → Resume processing from exactly where processing left off.

```csharp
var checkpoint = await LoadCheckpointAsync();
// checkpoint = { "orders-0": 150 }

foreach (var (tp, offset) in checkpoint)
{
    consumer.Seek(tp.Topic, tp.Partition, offset);
}
```

### Transactional Pattern

```csharp
using var transaction = await db.BeginTransactionAsync();
try
{
    // 1. Process message and write to database
    await db.Orders.AddAsync(new Order { ... });

    // 2. Update checkpoint in same transaction
    await db.Checkpoints.UpdateAsync(
        topic, partition, result.Offset + 1);

    // 3. Commit both atomically
    await transaction.CommitAsync();
}
catch
{
    // Both operations rolled back
    await transaction.RollbackAsync();
    throw;
}
```

## Exercise

In this exercise, you'll:

1. Implement a file-based checkpoint store
2. Save and load checkpoints on startup/shutdown
3. Coordinate checkpoint updates with message processing
4. Handle recovery after simulated crash

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
