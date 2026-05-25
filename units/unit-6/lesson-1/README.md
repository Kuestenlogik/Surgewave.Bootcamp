# Lesson 18.1: Manual Offset Management

> **Difficulty:** Advanced | **Duration:** 30 min | **Prerequisites:** Unit 17 completion

## Overview

Learn how to disable automatic offset commits and implement manual offset management strategies for precise control over message delivery guarantees.

## Learning Objectives

- Disable auto-commit and manage offsets manually
- Implement at-least-once delivery semantics
- Implement at-most-once delivery semantics
- Understand commit strategies and their trade-offs

## Why Manual Offset Management?

Auto-commit is convenient but lacks fine-grained control:

| Feature | Auto-Commit | Manual Commit |
|---------|-------------|---------------|
| Simplicity | Easy | Requires explicit calls |
| Control | Limited | Full control |
| Delivery guarantee | At-most-once (roughly) | Configurable |
| Error handling | Automatic | Application-controlled |
| Performance tuning | Fixed interval | Batch or per-message |

## Key Concepts

### Auto-Commit vs Manual Commit

**Auto-Commit Behavior:** Messages processed between automatic periodic commits may be lost on crash. For example, if M1–M4 are committed at T1 and a crash occurs before the next auto-commit, messages M5–M7 are lost.

**Manual Commit Behavior:** Commit exactly when you choose — per message, per batch, or on a schedule — giving full control over delivery semantics.

### Delivery Semantics

| Semantic | Processing Order | Crash Behavior | Use When |
|----------|-----------------|----------------|----------|
| **At-Most-Once** | Receive → **Commit** → Process → Done | Crash after commit = message LOST | Message loss is acceptable, speed is critical (metrics, logs, non-critical notifications) |
| **At-Least-Once** | Receive → **Process** → Commit → Done | Crash before commit = message REPROCESSED | No message loss, duplicates are acceptable/handled (order processing, payments with idempotency) |

### Commit Strategies

| Strategy | Pattern | Trade-off |
|----------|---------|-----------|
| **Per-message commit** | Process Msg1 → Commit → Process Msg2 → Commit → ... | Safest, slowest (one roundtrip per message) |
| **Batch commit** | Process Msg1..Msg5 → Commit → Process Msg6..Msg10 → Commit → ... | Balanced performance and safety |
| **Periodic commit** | Process messages continuously → Commit every N seconds | Good throughput, predictable commit intervals |

### Consumer Options for Manual Commit

```csharp
options.EnableAutoCommit = false;  // Disable auto-commit

// Other relevant settings:
options.AutoOffsetReset = AutoOffsetReset.Earliest;
options.GroupId = "my-consumer-group";
options.MaxPollIntervalMs = 300000;  // Max time between polls
```

## Commit Methods

| Method | Description |
|--------|-------------|
| `CommitAsync()` | Commit all assigned partitions |
| `CommitAsync(result)` | Commit offset for a specific message |
| `CommitAsync(topic, partition, offset)` | Commit specific offset |
| `CommitAsync(offsets)` | Commit multiple offsets at once |

## Exercise

In this exercise, you'll:

1. Create a consumer with auto-commit disabled
2. Implement at-least-once delivery by committing after processing
3. Implement batch commit for better performance
4. Compare the different commit strategies

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
