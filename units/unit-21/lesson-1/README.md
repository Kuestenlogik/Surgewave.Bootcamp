# Lesson 4.1: Clustering

> **Difficulty:** Intermediate | **Duration:** 45 min | **Prerequisites:** Unit 3 completion

## Overview

Learn how to set up and manage multi-broker Surgewave clusters for high availability and scalability.

## Learning Objectives

- Understand cluster architecture
- Configure multiple brokers
- Handle broker failures
- Use cluster metadata

## Why Clustering?

| Single Broker | Cluster |
|---------------|---------|
| Single point of failure | High availability |
| Limited throughput | Horizontal scaling |
| All partitions local | Distributed partitions |

## Key Concepts

### Cluster Membership

Brokers discover each other through:
- Configuration file with seed brokers
- Gossip protocol for membership updates
- Leader election for partitions

### Broker Configuration

```csharp
var broker1 = await SurgewaveRuntime.CreateBuilder()
    .WithBrokerId(1)
    .WithPort(9092)
    .WithClusterSeeds("localhost:9093", "localhost:9094")
    .Build()
    .StartAsync();
```

### Client Bootstrap

Clients connect to any broker and discover the full cluster:

```csharp
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092,localhost:9093,localhost:9094";
});
```

## Exercise

In this exercise, you'll:

1. Start a 3-broker cluster
2. Create a replicated topic
3. Observe partition distribution

---

**Instructions:** → [INSTRUCTIONS.md](INSTRUCTIONS.md)
