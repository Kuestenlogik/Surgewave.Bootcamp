# Lesson 4.3: Monitoring

> **Difficulty:** Intermediate | **Duration:** 30 min | **Prerequisites:** Lesson 4.2

## Overview

Learn how to monitor Surgewave using metrics, Prometheus, and Grafana dashboards.

## Learning Objectives

- Understand Surgewave's metrics system
- Export metrics to Prometheus
- Create monitoring dashboards
- Set up alerting

## Why Monitoring?

Production systems need visibility:

| Without Monitoring | With Monitoring |
|-------------------|-----------------|
| Blind to issues | Early warning |
| Reactive fixes | Proactive ops |
| Unclear capacity | Capacity planning |

## Key Metrics

### Broker Metrics

| Metric | Description |
|--------|-------------|
| `surgewave_broker_messages_in_total` | Messages received |
| `surgewave_broker_bytes_in_total` | Bytes received |
| `surgewave_broker_requests_total` | API requests |
| `surgewave_broker_partitions_count` | Partition count |

### Producer Metrics

| Metric | Description |
|--------|-------------|
| `surgewave_producer_messages_sent_total` | Messages sent |
| `surgewave_producer_latency_ms` | Produce latency |
| `surgewave_producer_errors_total` | Send errors |

### Consumer Metrics

| Metric | Description |
|--------|-------------|
| `surgewave_consumer_messages_consumed_total` | Messages consumed |
| `surgewave_consumer_lag` | Consumer lag |
| `surgewave_consumer_commit_latency_ms` | Commit latency |

## Exercise

In this exercise, you'll:

1. Enable metrics endpoint
2. View available metrics
3. Create monitoring queries

---

**Instructions:** → [INSTRUCTIONS.md](INSTRUCTIONS.md)
