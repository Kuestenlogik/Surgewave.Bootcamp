# Lesson 3.6: Compacted Topics

> **Difficulty:** Intermediate | **Duration:** 30 min | **Prerequisites:** Lesson 3.5

## Overview

Learn how log compaction keeps only the latest value per key, enabling efficient changelog topics and state stores.

## Learning Objectives

- Understand log compaction concepts and use cases
- Create and use compacted (changelog) topics
- Differentiate key-based retention from time-based retention
- Use tombstones (null values) for deletion
- Configure compaction lag and cleaner settings

## Log Compaction Concepts

Log compaction ensures Surgewave retains the **latest value for each key** rather than all historical values.

**Before Compaction:**

| Offset | Key | Value |
|--------|-----|-------|
| 0 | K1 | V1 |
| 1 | K2 | V2 |
| 2 | K1 | V3 |
| 3 | K2 | V4 |
| 4 | K3 | V5 |
| 5 | K3 | V6 |

**After Compaction** (duplicates removed, only latest per key kept):

| Offset | Key | Value |
|--------|-----|-------|
| 2 | K1 | V3 |
| 3 | K2 | V4 |
| 5 | K3 | V6 |

### Timeline View

```
                      Compaction Process

Time -->

Key K1: [V1]----[V3]----------------> [V3] (V1 removed)
Key K2: [V2]----[V4]----------------> [V4] (V2 removed)
Key K3: [V5]----[V6]----------------> [V6] (V5 removed)

Before:  6 messages                  After: 3 messages
         150 bytes                          75 bytes
```

## Use Cases

| Use Case | Description |
|----------|-------------|
| **Changelog Topics** | Store state for stream processors |
| **Config Topics** | Latest configuration per service |
| **User Profiles** | Current state per user ID |
| **Entity State** | Current state of domain entities |

## Key Concepts

### Key-Based vs Time-Based Retention

| Retention Type | Keeps | Deletes | Best For |
|----------------|-------|---------|----------|
| **Time-based** | Messages within time window | Old messages | Event logs, metrics |
| **Key-based (Compaction)** | Latest value per key | Older values of same key | State stores, configs |

### Tombstones

A **tombstone** is a message with a null value that signals deletion:

```csharp
// Delete key "user-123" by sending null value
await producer.ProduceAsync("users", "user-123", null);
```

After compaction, the key will be completely removed from the log.

### Compaction Configuration

| Setting | Description | Default |
|---------|-------------|---------|
| **CompactionLag** | Minimum time before compaction | 0 (immediate) |
| **MinCleanableDirtyRatio** | Ratio of dirty log to trigger cleaning | 0.5 |
| **DeleteRetention** | Time to retain tombstones | 24 hours |

## Exercise

In this exercise, you'll:

1. Create a compacted topic for user profiles
2. Produce multiple updates for the same keys
3. Observe compaction behavior
4. Use tombstones to delete entries
5. Verify only latest values remain

---

**Instructions:** -> [INSTRUCTIONS.md](INSTRUCTIONS.md)
