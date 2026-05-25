# Lesson 18.2: Seek Operations

> **Difficulty:** Advanced | **Duration:** 30 min | **Prerequisites:** Lesson 18.1

## Overview

Learn how to use seek operations to navigate within partitions - jumping to specific offsets, timestamps, or partition boundaries for replay and recovery scenarios.

## Learning Objectives

- Seek to a specific offset within a partition
- Seek to the beginning or end of a partition
- Seek to a timestamp to replay messages from a point in time
- Understand use cases for seek operations

## Why Seek Operations?

Seek operations allow you to control where consumption starts:

| Use Case | Seek Operation |
|----------|----------------|
| Reprocess all messages | `Seek(offset: 0)` or `SeekToBeginning()` |
| Skip to latest | `SeekToEnd()` or `Seek(latestOffset)` |
| Replay from timestamp | `SeekToTimestamp(dateTime)` |
| Resume from checkpoint | `Seek(savedOffset)` |
| Skip corrupted messages | `Seek(currentOffset + 1)` |

## Key Concepts

### Partition Offset Model

A partition is a sequential, append-only log of offsets from Earliest to Latest. You can `Seek()` to any valid offset to set the consumer's next read position.

| Property | Description |
|----------|-------------|
| **Immutable** | Once assigned, offsets never change |
| **Sequential** | Monotonically increasing within a partition |
| **Partition-specific** | Each partition has its own independent offset sequence |

### Seek Operations

| Operation | Code |
|-----------|------|
| **Seek to exact offset** | `consumer.Seek(topic, partition, offset)` — next consume returns the message at that offset |
| **Seek to beginning** | `consumer.Seek(topic, partition, 0)` or use `GetEarliestOffsetAsync()` |
| **Seek to end** | `var latest = await GetLatestOffsetAsync(topic, partition);` then `consumer.Seek(topic, partition, latest)` |
| **Seek to timestamp** | `var offset = await GetOffsetForTimestampAsync(topic, partition, timestamp);` then `consumer.Seek(topic, partition, offset)` |

### Timestamp-Based Seek

Timestamp-based seek returns the first offset whose timestamp is greater than or equal to the requested time.

| Offset | 0 | 1 | 2 | **3** | 4 | 5 | 6 | 7 |
|--------|---|---|---|-------|---|---|---|---|
| Time | 10:00 | 10:01 | 10:05 | **10:10** | 10:15 | 10:20 | 10:25 | 10:30 |

`SeekToTimestamp(10:10)` → returns offset **3** (first offset with timestamp ≥ 10:10).

### Seek Use Cases

| Use Case | Steps |
|----------|-------|
| **Replay for Debugging** | Seek to beginning; replay all messages to find issue |
| **Disaster Recovery** | Seek to timestamp before failure; reprocess from that point |
| **Skip Poison Messages** | Catch deserialization error; seek past the bad message; continue processing |
| **Reset Consumer Group** | Seek to beginning or end; commit new position (effectively resets the group) |
| **Time-based Processing** | Seek to `timestamp = now - 1 hour`; process only messages from last hour |

## Seek Methods

| Method | Description |
|--------|-------------|
| `Seek(topic, partition, offset)` | Seek to specific offset |
| `Position(topic, partition)` | Get current position |
| External offset lookup | Use messaging operations for offset queries |

## Exercise

In this exercise, you'll:

1. Seek to the beginning of a partition
2. Seek to a specific offset
3. Seek to the end of a partition
4. Seek to a timestamp to replay recent messages

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
