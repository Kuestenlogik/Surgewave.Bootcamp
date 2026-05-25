# Lesson 6.5: Capacity Planning

> **Difficulty:** Intermediate | **Duration:** 30 min | **Prerequisites:** Lesson 6.4

## Overview

Learn to properly size and plan capacity for Surgewave deployments, ensuring you have adequate resources for current and future throughput requirements.

## Learning Objectives

By the end of this lesson, you will be able to:
- Calculate storage requirements based on message size and retention
- Estimate throughput capacity per partition
- Plan cluster sizing for high availability
- Monitor and adjust capacity proactively

## Prerequisites

- Completed Lesson 6.4: Event-Driven Patterns
- Understanding of partitioning concepts

## Key Concepts

### Capacity Planning Formula

```
Storage = MessageSize × MessagesPerSecond × RetentionSeconds × ReplicationFactor

Example:
  1KB × 10,000/sec × 7 days × 3 replicas
  = 1KB × 10,000 × 604,800 × 3
  = ~18 TB
```

### Throughput Capacity

| Factor | Effect on Throughput |
|--------|----------------------|
| Message Size (larger) | Lower |
| Batch Size (larger) | Higher |
| Compression (enabled) | Higher |
| Replication Factor (higher) | Lower |
| Ack Mode = All | Lower |

### Partition Count Guidelines

| Throughput (msg/sec) | Partitions | Notes |
|---------------------|------------|-------|
| < 10,000 | 3-6 | Small workload |
| 10,000 - 100,000 | 12-24 | Medium workload |
| 100,000 - 1,000,000 | 48-96 | Large workload |
| > 1,000,000 | 100+ | Enterprise scale |

### Retention Planning

| Use Case | Retention | Storage Impact |
|----------|-----------|----------------|
| Hot data | Hours–Days | Minimal |
| Replay | Weeks | Moderate |
| Audit | Months–Years | High (consider tiering) |
| Compacted | Forever | Depends on key cardinality |

## Capacity Planning Checklist

### 1. Data Volume Analysis
- [ ] Average message size (bytes)
- [ ] Peak message size (bytes)
- [ ] Messages per second (average)
- [ ] Messages per second (peak)
- [ ] Expected growth rate (% per year)

### 2. Retention Requirements
- [ ] Retention period per topic
- [ ] Compaction vs. delete policy
- [ ] Regulatory requirements

### 3. Availability Requirements
- [ ] Replication factor
- [ ] Minimum in-sync replicas
- [ ] Recovery time objective (RTO)

### 4. Performance Requirements
- [ ] End-to-end latency target (P99)
- [ ] Consumer lag tolerance
- [ ] Batch processing windows

## Hands-On Exercise

In this exercise, you'll:
1. Build a capacity calculator
2. Simulate workloads to validate capacity
3. Create monitoring dashboards
4. Test capacity limits

## Files

- `start/Program.cs` - Starter code with TODOs
- `completed/Program.cs` - Working solution

## Duration

~30 minutes

---

**Previous:** [Lesson 6.4: Event-Driven Patterns](../lesson-4/README.md)
