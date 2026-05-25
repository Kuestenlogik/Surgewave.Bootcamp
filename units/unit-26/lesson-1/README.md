# Lesson 24.1: Tiered Storage Concepts

> **Difficulty:** Advanced | **Duration:** 35 min | **Prerequisites:** Unit 23 completion

## Overview

Learn the fundamental concepts of tiered storage in distributed messaging systems. Understand how data moves between hot, warm, and cold tiers based on age, access patterns, and cost optimization goals.

## Learning Objectives

- Understand the hot/warm/cold storage tier model
- Learn when and why data moves between tiers
- Explore cost-performance tradeoffs for each tier
- Configure basic tier settings in Surgewave
- Monitor tier utilization and data distribution

## Storage Tier Characteristics

| Tier | Storage | Latency | Cost | Retention | Access | Format |
|------|---------|---------|------|-----------|--------|--------|
| **Hot** (Active) | Memory + NVMe SSD | < 1 ms | $$$$ | Minutes–hours | Real-time streaming, active consumers | Uncompressed or light compression |
| **Warm** (Recent) | SSD or fast HDD | 1–10 ms | $$ | Hours–days | Recent replays, catch-up consumers | Compressed segments |
| **Cold** (Archive) | HDD, S3, Azure Blob, GCS | 10 ms–minutes | $ | Days–years | Historical analysis, compliance, audit | Highly compressed, possibly encrypted |

## Data Movement Triggers

| Trigger Type | Configuration | Timeline | Best For |
|-------------|--------------|----------|---------|
| **Time-Based** (most common) | `hot.retention.ms=3600000`, `warm.retention.ms=604800000`, `retention.ms=7776000000` | `0–1 h` → HOT → `1 h–7 d` → WARM → `7 d–90 d` → COLD | Predictable data lifecycle |
| **Size-Based** | `hot.segment.bytes=1 GB`, `warm.segment.bytes=10 GB` | Move when partition reaches size limit | High-throughput topics with predictable sizes |
| **Access-Pattern** (Adaptive) | Monitors read frequency per segment | Moves rarely-accessed data to cold earlier; keeps hot data longer | Variable access patterns, cost optimisation |

## Cost Analysis Model

Example: 100 GB/day ingestion, 30-day retention.

| Scenario | Breakdown | Monthly Cost | Savings |
|----------|-----------|-------------|---------|
| **No tiering** (all SSD) | 3 000 GB × $0.10/GB | $300 | — |
| **With tiering** (1d hot, 7d warm, 23d cold) | Hot: 100 GB × $0.50 = $50; Warm: 600 GB × $0.10 = $60; Cold: 2 300 GB × $0.01 = $23 | $133 | 56% |
| **Aggressive tiering** (1h hot, 1d warm, 29d cold) | Hot: 4 GB × $0.50 = $2; Warm: 100 GB × $0.10 = $10; Cold: 2 900 GB × $0.01 = $29 | $41 | 86% |

## Tier Configuration in Surgewave

```csharp
// Basic tiered storage configuration
var config = new TieredStorageConfig
{
    // Hot tier: In-memory + local fast storage
    HotTierRetention = TimeSpan.FromHours(1),
    HotTierMaxSizeBytes = 10L * 1024 * 1024 * 1024, // 10 GB

    // Warm tier: Local SSD storage
    WarmTierRetention = TimeSpan.FromDays(7),
    WarmTierPath = "/data/surgewave/warm",
    WarmTierCompression = CompressionType.Lz4,

    // Cold tier: Object storage
    ColdTierRetention = TimeSpan.FromDays(90),
    ColdTierProvider = StorageProvider.S3,
    ColdTierCompression = CompressionType.Zstd,

    // Lifecycle
    EnableAutoTiering = true,
    TieringCheckInterval = TimeSpan.FromMinutes(5)
};
```

## Monitoring Tier Distribution

Topic `orders-events` (30.2 GB, 302 segments):

| Tier | Size | Share | Segments | Avg Age | Read Rate |
|------|------|-------|---------|---------|-----------|
| Hot | 2.4 GB | 8% | 24 | 28 min | 1.2 k/s |
| Warm | 18.7 GB | 62% | 187 | 3.2 days | 45/s |
| Cold | 9.1 GB | 30% | 91 | 21 days | 0.2/s |

Cost estimate: $14.23/month (vs $30.20 without tiering — 53% savings).

## Best Practices

| Practice | Description |
|----------|-------------|
| Profile access patterns first | Understand your data access before configuring tiers |
| Start conservative | Begin with longer hot/warm retention, optimize later |
| Monitor retrieval latency | Cold tier access must meet your SLA requirements |
| Use compression wisely | Higher compression for cold tier, faster for hot |
| Plan for compliance | Some data must stay accessible for audit purposes |
| Test disaster recovery | Ensure you can restore from cold tier if needed |

## Exercise

In this exercise, you will:

1. Configure a Surgewave broker with tiered storage
2. Produce messages and observe tier distribution
3. Monitor data movement between tiers
4. Calculate cost savings for different configurations
5. Retrieve data from different tiers and measure latency

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
