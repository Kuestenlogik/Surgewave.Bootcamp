# Lesson 3.5: Exactly-Once Semantics

> **Difficulty:** Intermediate | **Duration:** 45 min | **Prerequisites:** Lesson 3.4

## Overview

Learn how to implement exactly-once processing with idempotent producers and transactional consumers.

## Learning Objectives

- Understand delivery guarantees (at-most-once, at-least-once, exactly-once)
- Enable idempotent producers
- Implement exactly-once consume-transform-produce
- Handle duplicate detection

## Delivery Guarantees

| Guarantee | Description | Risk |
|-----------|-------------|------|
| **At-most-once** | Fire and forget | Message loss |
| **At-least-once** | Retry until ack | Duplicates |
| **Exactly-once** | No loss, no duplicates | None |

## Key Concepts

### Idempotent Producer

Prevents duplicates from producer retries:

```csharp
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.EnableIdempotence = true;
});
```

### Exactly-Once Processing

Combine transactions with consumer offset management:

```csharp
// Consumer with isolation
var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.IsolationLevel = IsolationLevel.ReadCommitted;
    opt.EnableAutoCommit = false;
});

// Transactional producer
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.TransactionalId = "processor-1";
    opt.EnableIdempotence = true;
});
```

## Exercise

In this exercise, you'll:

1. Enable idempotent production
2. Implement exactly-once consume-transform-produce
3. Verify no duplicates exist

---

**Instructions:** → [INSTRUCTIONS.md](INSTRUCTIONS.md)
