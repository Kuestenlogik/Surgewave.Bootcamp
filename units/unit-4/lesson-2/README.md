# Lesson 6.2: Partitioning Strategies

> **Difficulty:** Intermediate | **Duration:** 30 min | **Prerequisites:** Lesson 6.1

## Overview

Learn how to choose partition keys and counts for optimal ordering, scalability, and performance.

## Learning Objectives

- Understand how partitioning affects ordering and parallelism
- Choose appropriate partition keys for different use cases
- Avoid hot partitions and skewed distributions
- Plan partition counts for growth

## Why Partitioning Matters

Partitioning determines:
- **Ordering**: Messages with the same key go to the same partition (ordered)
- **Parallelism**: Each partition can have one consumer per group
- **Scalability**: More partitions = more potential throughput

## Key Concepts

### Partition Key Selection

| Key Strategy | Ordering | Distribution | Use Case |
|--------------|----------|--------------|----------|
| Entity ID | Per-entity | Usually good | Orders, users |
| Tenant ID | Per-tenant | Varies | Multi-tenant |
| Random/null | None | Perfect | Logs, metrics |
| Timestamp | None | Time-skewed | Time-series |
| Composite | Fine-grained | Varies | Complex routing |

### The Hot Partition Problem

Bad: Partition by country (hot partition on US)

| Partition | Load |
|-----------|------|
| 0 (US) | 80% of traffic |
| 1 (UK) | 10% |
| 2 (DE) | 5% |
| 3 (Other) | 5% |

Good: Partition by user ID (even distribution)

| Partition | Load |
|-----------|------|
| 0 | ~25% |
| 1 | ~25% |
| 2 | ~25% |
| 3 | ~25% |

## Exercise

In this exercise, you'll:

1. Implement different partitioning strategies
2. Measure partition distribution
3. Identify and fix hot partitions

---

**Instructions:** → [INSTRUCTIONS.md](INSTRUCTIONS.md)
