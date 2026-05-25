# Lesson 5.4: Production Cutover

> **Difficulty:** Intermediate | **Duration:** 45 min | **Prerequisites:** Lesson 5.3

## Overview

Complete your Kafka to Surgewave migration with a production cutover checklist.

## Learning Objectives

- Plan a production cutover
- Execute migration steps safely
- Verify successful migration
- Handle rollback scenarios

## Prerequisites

- Completed lessons 5.1-5.3
- Understanding of your production environment
- Stakeholder approval for migration

## Cutover Checklist

### Pre-Cutover

- [ ] Surgewave cluster sized and tested
- [ ] Data mirrored and validated
- [ ] Consumers tested against Surgewave
- [ ] Rollback plan documented
- [ ] Monitoring configured
- [ ] Stakeholders notified

### During Cutover

- [ ] Pause non-critical producers
- [ ] Drain Kafka consumers
- [ ] Switch bootstrap servers
- [ ] Verify message flow
- [ ] Monitor for errors

### Post-Cutover

- [ ] Validate message counts
- [ ] Check consumer lag
- [ ] Verify application health
- [ ] Document any issues
- [ ] Decommission Kafka (after burn-in)

---

**Instructions:** → [INSTRUCTIONS.md](INSTRUCTIONS.md)
