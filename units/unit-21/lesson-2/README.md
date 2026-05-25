# Lesson 4.2: Replication

> **Difficulty:** Intermediate | **Duration:** 45 min | **Prerequisites:** Lesson 4.1

## Overview

Learn how to configure replication for data durability and high availability.

## Learning Objectives

- Understand replication concepts
- Configure replication factor
- Manage In-Sync Replicas (ISR)
- Handle replica failures

## Why Replication?

Replication protects against data loss:

| Without Replication | With Replication (RF=3) |
|--------------------|-------------------------|
| 1 copy of data | 3 copies of data |
| Broker failure = data loss | Survives 2 broker failures |
| No durability guarantee | Strong durability |

## Key Concepts

### Replication Factor

Number of copies of each partition:
- **RF=1**: No redundancy (development only)
- **RF=2**: Tolerates 1 broker failure
- **RF=3**: Recommended for production

### In-Sync Replicas (ISR)

Replicas that are caught up with the leader:
- All replicas start in ISR
- Slow replicas are removed from ISR
- Writes only ack when all ISR replicas have the data

### Min In-Sync Replicas

Minimum ISR required for writes to succeed:
- `min.insync.replicas=2` with `RF=3`: Writes require 2 replicas
- Prevents data loss even with Acks.All

## Exercise

In this exercise, you'll:

1. Configure topic replication
2. Monitor ISR status
3. Test replica failure scenarios

---

**Instructions:** → [INSTRUCTIONS.md](INSTRUCTIONS.md)
