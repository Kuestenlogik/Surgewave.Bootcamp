# Lesson 24.5: Retention and Lifecycle Policies

> **Difficulty:** Advanced | **Duration:** 45 min | **Prerequisites:** Lesson 24.4

## Overview

Learn how to implement data retention policies and lifecycle management for Surgewave tiered storage. Understand TTL configuration, automatic tier migration rules, retention enforcement, and cost optimization strategies for managing data across hot, warm, and cold tiers.

## Learning Objectives

- Configure data retention policies with TTL-based expiration
- Implement lifecycle rules for automatic tier migration
- Create retention policy checkers that evaluate data age and access patterns
- Understand cost optimization through intelligent data lifecycle management
- Design policies that balance compliance requirements with storage costs

## Retention Policy Architecture

**Policy Definition** (topic: orders):

| Tier | Retention | Action |
|------|-----------|--------|
| Hot | 1 hour | Move to Warm |
| Warm | 7 days | Move to Cold |
| Cold | 90 days | Move to Archive |
| Archive | 365 days | Delete |

**Policy Checker** (runs periodically):

1. Scan all segments across tiers
2. Check age against retention policy
3. Identify segments requiring migration or deletion
4. Execute transitions (move/compress/delete)
5. Log actions and update metrics

## TTL Configuration

| Level | Scope | Examples |
|-------|-------|---------|
| **Global TTL** | All topics (unless overridden) | Default: 7 days; transitions: Hot(1h) → Warm(7d) → Cold(30d) |
| **Topic-Level TTL** | Single topic (overrides global) | `orders`: 90 d, compliance; `events`: 7 d; `audit-log`: 7 yr, immutable; `metrics`: 30 d, compact |
| **Partition-Level TTL** | Single partition (fine-grained) | `orders/P0`: 90 d (standard); `orders/P1`: 365 d (high-value region) |

## Lifecycle Rule Types

| Category | Rules |
|----------|-------|
| **Age-Based** | Move to warm after X hours; move to cold after Y days; delete after Z days |
| **Size-Based** | Compact segments below threshold; split segments above threshold; trigger tier migration when tier exceeds size limit |
| **Access-Based** | Move to cold if not accessed in X days; promote to warm if frequently accessed from cold; never delete if access count exceeds threshold |
| **Compliance** | Minimum retention period (cannot delete before); legal hold (pause all deletion); immutable storage (write-once, read-many) |

## Cost Optimization Strategies

| Strategy | Description | Savings |
|----------|-------------|---------|
| Aggressive tier migration | Move data to cold tier quickly | 60-80% |
| Compression before migration | Compress segments during tier transition | 40-70% |
| Compaction of old segments | Merge small cold segments | 10-20% |
| Early deletion of non-critical data | Delete transient data sooner | 50-90% |
| Access-based promotion/demotion | Match tier to actual access patterns | 20-40% |

## Exercise

In this exercise, you will:

1. Define retention policies with TTL configuration
2. Implement a retention policy checker
3. Simulate data aging and tier migration
4. Calculate storage costs under different retention strategies
5. Handle compliance requirements with legal holds

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
