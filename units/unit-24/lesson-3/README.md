# Lesson 15.3: Failover Procedures

> **Difficulty:** Advanced | **Duration:** 45 min | **Prerequisites:** Lesson 15.2

## Overview

Learn how to implement and execute failover procedures for Surgewave clusters, including failure detection, manual vs automatic failover, and DNS/load balancer switching strategies.

## Learning Objectives

- Detect cluster and broker failures
- Implement manual failover procedures
- Configure automatic failover triggers
- Execute DNS and load balancer switching
- Validate post-failover health

## Why Failover Procedures Matter

Well-defined failover procedures ensure:

| Benefit | Description |
|---------|-------------|
| Minimize Downtime | Faster recovery from failures |
| Data Protection | Prevent data loss during transitions |
| Consistency | Maintain correct system state |
| Confidence | Teams know what to do in emergencies |
| Compliance | Meet SLA requirements |

## Failover Architecture

**Normal operation:** Clients → Load Balancer (`surgewave.example.com`) → Active Primary Cluster → replication → Standby Secondary Cluster.

**Failover steps:**
1. Detect primary failure
2. Verify secondary is ready (replication caught up)
3. Stop writes to primary (if possible)
4. Update DNS/LB to point to secondary
5. Start accepting traffic on secondary
6. Validate health and data integrity

**After failover:** Load Balancer now routes to Secondary (Active); Primary is Down/Repair.

## Failure Detection Strategies

| Method | How It Works | Failure Condition |
|--------|-------------|-------------------|
| **Health Check Probes** | Monitor polls `GET /health` every 10 s; HTTP 200 = healthy, 503 = unhealthy, timeout = unreachable | 3 consecutive failures |
| **Heartbeat Monitoring** | Each broker sends periodic heartbeats to the monitor | Missing heartbeat > 30 s |
| **Replication Lag Threshold** | Compare source offset (e.g. 1000) vs replica (e.g. 950); lag = 50 messages | Lag > threshold for > configured duration → Alert/Trigger |

## Manual vs Automatic Failover

| Mode | Pros | Cons | Best For |
|------|------|------|----------|
| **Manual** | Human verification; avoids false positives; handles complex decisions | Slower response; requires on-call; RTO > 15 min | Complex failure scenarios, high data-loss risk, partial failures |
| **Automatic** | Fast; no human delay; 24/7 | Risk of false positives; split-brain; may need manual rollback | Clear failure conditions, low RPO, well-tested scenarios |
| **Hybrid** (recommended) | Balances speed and safety | More complex to implement | Most production environments |

Hybrid flow: Auto-detect failure → alert team → wait for approval → auto-failover after timeout (if no response in 5 min).

## DNS Switching Strategies

| Strategy | Description |
|----------|-------------|
| **DNS Record Update** | Before: `surgewave.example.com` → `10.0.1.100` (Primary); After: → `10.0.2.100` (Secondary). Use TTL 60 s for fast propagation. |
| **Weighted DNS Routing** | Normal: Primary 100% / Secondary 0%. Failover: Primary 0% / Secondary 100%. Gradual: 50%/50% for testing. |
| **Health-based DNS** (Route 53, etc.) | Primary healthy → Active; Secondary healthy → Standby. Primary fails health check → auto-route to Secondary. |

## Failover Checklist

| Step | Action | Verification |
|------|--------|--------------|
| 1 | Detect failure | Health checks confirm primary down |
| 2 | Assess impact | Determine affected topics/consumers |
| 3 | Verify secondary | Replication lag acceptable |
| 4 | Notify team | Alert sent to on-call |
| 5 | Stop primary writes | Drain in-flight messages |
| 6 | Update DNS/LB | Point to secondary cluster |
| 7 | Enable secondary | Accept new connections |
| 8 | Verify consumers | Check consumer group offsets |
| 9 | Health check | All topics accessible |
| 10 | Document | Record timeline and actions |

## Exercise

In this exercise, you will:

1. Implement failure detection with health checks
2. Create a failover coordinator
3. Execute manual and automatic failover
4. Simulate DNS switching
5. Validate post-failover health

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
