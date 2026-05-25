# Lesson 26.3: Consumer Optimization

> **Difficulty:** Advanced | **Duration:** 45 min | **Prerequisites:** Lesson 26.2 completion

## Overview

Optimize Surgewave consumer performance through fetch tuning, parallel processing, and efficient partition handling. Learn to maximize consumption throughput while maintaining processing quality.

## Learning Objectives

- Configure fetch size and poll intervals for optimal performance
- Implement parallel message processing with thread safety
- Understand partition assignment and rebalancing impact
- Handle backpressure and consumer lag effectively
- Design scalable consumer group architectures

## Consumer Configuration Parameters

**Fetch Settings:**

| Parameter | Description |
|-----------|-------------|
| `FetchMinBytes` | Minimum bytes before returning |
| `FetchMaxBytes` | Maximum bytes per fetch request |
| `FetchMaxWaitMs` | Max time to wait for FetchMinBytes |
| `MaxPartitionFetchBytes` | Max bytes per partition per fetch |

**Polling Settings:**

| Parameter | Description |
|-----------|-------------|
| `MaxPollRecords` | Max records returned per poll |
| `MaxPollIntervalMs` | Max time between polls before rebalance |
| `SessionTimeoutMs` | Heartbeat timeout for consumer health |

**Processing Settings:**

| Parameter | Description |
|-----------|-------------|
| `EnableAutoCommit` | Auto-commit offsets periodically |
| `AutoCommitIntervalMs` | Interval for auto-commit |
| `AutoOffsetReset` | Where to start when no committed offset |

## Parallel Processing Patterns

| Pattern | Flow | Pros | Cons |
|---------|------|------|------|
| **Single Consumer, Sequential** | Consumer → Process → Commit → Process → Commit | Simple, ordered | Slow; blocked by slowest message |
| **Consumer Group, Multiple Consumers** | Partition 0 → Consumer 1 → Process; Partition 1 → Consumer 2 → Process; … | Horizontal scaling; parallel by partition | Ordering only within partition |
| **Single Consumer, Parallel Processing** | Consumer → Thread Pool → Process 1 / Process 2 / Process 3 → Batch Commit | Maximum parallelism per consumer | Complex offset management; ordering challenges |
| **Channel-Based Processing** | Consumer → Channel → Worker 1 / Worker 2 / Worker 3 | Backpressure; decoupled consumption/processing | Complexity; memory for buffering |

## Partition Scaling

Topic `orders` (6 partitions), Consumer Group: `order-processors`:

| Consumers | Assignment | Result |
|-----------|-----------|--------|
| **3** (optimal) | Consumer 1: P0, P1 / Consumer 2: P2, P3 / Consumer 3: P4, P5 | 2 partitions each |
| **6** (maximum parallelism) | Consumer 1: P0 / Consumer 2: P1 / … / Consumer 6: P5 | 1 partition each |
| **10** (over-provisioned) | Consumers 1–6: P0–P5 (active) / Consumers 7–10: IDLE (wasted) | 4 idle consumers |

> Rule: Max useful consumers = Number of partitions

## Backpressure Handling

**Signs of Backpressure:** consumer lag growing steadily; `MaxPollInterval` exceeded warnings; frequent rebalances; memory usage increasing.

**Solutions:**
1. Increase `MaxPollIntervalMs` for slow processing
2. Reduce `MaxPollRecords` to fetch less per poll
3. Add more consumers (up to partition count)
4. Optimize processing logic
5. Use `Channel` with bounded capacity for backpressure

## Key Configuration Settings

| Parameter | Description | Default | Recommended |
|-----------|-------------|---------|-------------|
| `FetchMinBytes` | Min bytes before fetch returns | 1 | 1KB-1MB |
| `FetchMaxBytes` | Max bytes per fetch | 50MB | 1-50MB |
| `MaxPollRecords` | Max records per poll | 500 | 100-1000 |
| `MaxPollIntervalMs` | Max time between polls | 300s | Match processing time |
| `SessionTimeoutMs` | Consumer session timeout | 45s | 10-60s |
| `EnableAutoCommit` | Auto-commit offsets | true | false for at-least-once |

## Exercise

In this exercise, you will:

1. Measure baseline consumer performance
2. Tune fetch settings for different workloads
3. Implement parallel processing with thread pool
4. Handle backpressure with bounded channels
5. Compare consumer group scaling

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
