# Lesson 15.5: DR Testing

> **Difficulty:** Advanced | **Duration:** 60 min | **Prerequisites:** Lesson 15.4

## Overview

Learn how to test disaster recovery procedures through chaos engineering, failover drills, and runbook creation to ensure your DR strategy works when you need it.

## Learning Objectives

- Design and execute chaos engineering experiments
- Conduct failover drills with realistic scenarios
- Create comprehensive DR runbooks
- Measure and improve RTO/RPO metrics
- Build confidence in DR procedures

## Why DR Testing Matters

Regular DR testing provides:

| Benefit | Description |
|---------|-------------|
| Validation | Confirm procedures actually work |
| Training | Team gains hands-on experience |
| Discovery | Find gaps before real disasters |
| Metrics | Measure actual RTO/RPO |
| Compliance | Meet audit requirements |

## Chaos Engineering Architecture

**Experiment definition:** Hypothesis — "When broker-1 fails, consumers failover to broker-2 within 30 seconds with zero message loss." Steady State: all consumers receiving messages. Method: kill broker-1 process. Blast Radius: single broker. Rollback: restart broker-1.

**Execution steps:**
1. Verify steady state
2. Inject fault (kill broker, network partition, etc.)
3. Observe behavior — measure recovery time, data loss
4. Verify recovery — system returns to steady state
5. Rollback — restore failed component
6. Analyze results — did hypothesis hold?

## Failover Drill Scenarios

| Scenario | Actions | Measures |
|----------|---------|---------|
| **1. Single Broker Failure** | Stop one broker; verify partition leadership transfers; confirm consumers continue | Time to stabilization |
| **2. Network Partition** | Simulate split between DCs; verify split-brain prevention; test partition healing | Data consistency after heal |
| **3. Complete Datacenter Failure** | Simulate entire primary DC offline; execute full failover to secondary; verify all topics | Total recovery time |
| **4. Storage Corruption** | Simulate disk corruption; verify automatic detection; restore from replica or backup | Data integrity |

## DR Runbook Structure

| Section | Contents |
|---------|---------|
| **1. Overview** | Purpose and scope; RTO/RPO objectives; contact information and escalation |
| **2. Preparation** | Prerequisites and access requirements; tool/credential verification; communication channels |
| **3. Decision Criteria** | When to initiate DR; who can authorize failover; Go/No-go checklist |
| **4. Execution Steps** | 1. Assess situation; 2. Notify stakeholders; 3. Execute failover; 4. Validate recovery; 5. Resume operations |
| **5. Validation** | Health check procedures; data integrity verification; service restoration confirmation |
| **6. Rollback** | Rollback criteria; rollback procedure; post-rollback validation |

## Metrics Collection

**Recovery Time Metrics (example drill):**

| Phase | Duration |
|-------|---------|
| Detection | 45 s |
| Decision | 2 min |
| Execution | 8 min |
| Validation | 3 min |
| **Total RTO** | **13.75 min** (target 15 min — PASS) |

**Data Loss Metrics:** Last replicated 10:29:45; failure at 10:30:00 — actual RPO = 15 s (target 5 min — PASS).

**Historical drill results:**

| Drill Date | Scenario | RTO | RPO | Result |
|------------|----------|-----|-----|--------|
| 2024-01-15 | DC Failover | 13.75 m | 15 s | PASS |
| 2024-01-01 | Broker Failure | 2.5 m | 0 s | PASS |
| 2023-12-15 | Network Partition | 5.2 m | 30 s | PASS |
| 2023-12-01 | Storage Failure | 45 m | 5 m | FAIL |

## Best Practices

| Practice | Description |
|----------|-------------|
| Regular Drills | Conduct monthly DR drills |
| Realistic Scenarios | Test full scenarios, not just components |
| Automated Testing | Automate chaos experiments where possible |
| Documentation | Keep runbooks current |
| Blameless Reviews | Learn from failures without blame |
| Continuous Improvement | Improve procedures after each drill |

## Exercise

In this exercise, you will:

1. Build a chaos engineering framework
2. Define and execute failure scenarios
3. Create a DR runbook with executable steps
4. Measure RTO/RPO during drills
5. Generate drill reports

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
