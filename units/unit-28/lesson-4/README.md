# Lesson 3.4: Transactions

> **Difficulty:** Intermediate | **Duration:** 45 min | **Prerequisites:** Lesson 3.3

## Overview

Learn how to use transactions for atomic multi-message operations.

## Learning Objectives

- Understand transactional messaging concepts
- Implement atomic produce operations
- Use consume-transform-produce patterns
- Handle transaction failures

## Why Transactions?

Transactions ensure atomicity - all messages succeed or none do:

| Without Transactions | With Transactions |
|---------------------|-------------------|
| Partial writes possible | All-or-nothing |
| Inconsistent state | Consistent state |
| Manual rollback | Automatic rollback |

## Key Concepts

### Transaction Flow

```
BeginTransaction
  → Produce message 1
  → Produce message 2
  → Produce message 3
CommitTransaction (or AbortTransaction)
```

### Transactional Producer

```csharp
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.TransactionalId = "my-transactional-producer";
});

await producer.InitTransactionsAsync();
await producer.BeginTransactionAsync();

try
{
    await producer.ProduceAsync("topic1", "key", "value1");
    await producer.ProduceAsync("topic2", "key", "value2");
    await producer.CommitTransactionAsync();
}
catch
{
    await producer.AbortTransactionAsync();
    throw;
}
```

## Exercise

In this exercise, you'll:

1. Create a transactional producer
2. Produce to multiple topics atomically
3. Handle transaction failures

---

**Instructions:** → [INSTRUCTIONS.md](INSTRUCTIONS.md)
