# Lesson 9.2: Metrics & Monitoring

> **Difficulty:** Intermediate | **Duration:** 30 min | **Prerequisites:** Lesson 9.1

Expose application metrics for monitoring systems.

## Objectives

- Understand metrics types: counters, gauges, histograms
- Use System.Diagnostics.Metrics for instrumentation
- Implement Prometheus-style metric patterns

## Concepts

### Metric Types

| Type | Description | Use Case |
|------|-------------|----------|
| Counter | Monotonically increasing value | Messages produced, errors |
| Gauge | Value that can go up or down | Active connections, queue size |
| Histogram | Distribution of values | Latency, message sizes |

### System.Diagnostics.Metrics

.NET provides a modern metrics API:

```csharp
var meter = new Meter("Surgewave.Application");
var messagesProduced = meter.CreateCounter<long>("messages_produced");
var activeConsumers = meter.CreateGauge<int>("active_consumers");
var latency = meter.CreateHistogram<double>("produce_latency_ms");
```

### Recording Metrics

```csharp
// Increment counter
messagesProduced.Add(1, new KeyValuePair<string, object?>("topic", "orders"));

// Set gauge value
activeConsumers.Record(5);

// Record histogram value
latency.Record(15.5);
```

## Exercise

In this lesson, you'll:
1. Create counters for message throughput
2. Track consumer lag with gauges
3. Measure latencies with histograms

## Key Takeaways

- Choose the right metric type for your data
- Use tags/labels to add dimensions
- Export metrics to monitoring systems
