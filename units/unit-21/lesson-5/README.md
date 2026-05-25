# Lesson 4.5: Performance Tuning

> **Difficulty:** Intermediate | **Duration:** 45 min | **Prerequisites:** Lesson 4.4

## Overview

Learn how to optimize Surgewave for maximum throughput and minimum latency.

## Learning Objectives

- Understand performance trade-offs
- Configure batching and compression
- Tune buffer sizes
- Optimize for your workload

## Performance Dimensions

| Optimize For | Trade-off |
|--------------|-----------|
| **Throughput** | Higher latency |
| **Latency** | Lower throughput |
| **Durability** | Lower throughput |

## Key Tuning Parameters

### Producer Settings

| Setting | Effect |
|---------|--------|
| `BatchSize` | Messages batched before send |
| `LingerMs` | Time to wait for batch fill |
| `CompressionType` | Reduce network/disk I/O |
| `BufferMemory` | Memory for unsent messages |

### Consumer Settings

| Setting | Effect |
|---------|--------|
| `FetchMinBytes` | Minimum data per fetch |
| `FetchMaxWaitMs` | Max wait for fetch |
| `MaxPartitionFetchBytes` | Max data per partition |

## Exercise

In this exercise, you'll:

1. Benchmark baseline performance
2. Apply throughput optimizations
3. Apply latency optimizations
4. Compare results

---

**Instructions:** → [INSTRUCTIONS.md](INSTRUCTIONS.md)
