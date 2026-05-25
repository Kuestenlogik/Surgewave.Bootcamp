# Knowledge Assessment: Lesson 9.2 - Metrics & Monitoring

## Multiple Choice

### 1. Which metric type is appropriate for tracking the number of messages produced?
- [ ] A) Gauge
- [ ] B) Counter
- [ ] C) Histogram
- [ ] D) Timer

<details>
<summary>Answer</summary>
**B)** Counter is the correct choice because message count is monotonically increasing - you only add to it, never subtract. Counters track cumulative totals.
</details>

### 2. What metric type should be used for tracking current queue size that can go up or down?
- [ ] A) Counter
- [ ] B) Histogram
- [ ] C) Gauge
- [ ] D) Timer

<details>
<summary>Answer</summary>
**C)** Gauge is for values that can increase or decrease, like active connections, queue size, or memory usage. It represents a point-in-time measurement.
</details>

### 3. Which metric type provides distribution statistics like percentiles?
- [ ] A) Counter
- [ ] B) Gauge
- [ ] C) Histogram
- [ ] D) Summary

<details>
<summary>Answer</summary>
**C)** Histogram records the distribution of values (like latency measurements) and enables calculation of percentiles, averages, and other statistical measures.
</details>

## True/False

### 4. Tags/labels on metrics add dimensions for filtering and grouping.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Tags (like `topic="orders"`) allow you to break down metrics by different dimensions, enabling queries like "show message count per topic" or "filter latency by partition".
</details>

### 5. Gauges should be used for tracking error counts because errors can be fixed.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Error counts should use Counters because the total number of errors only increases over time. Gauges are for values that fluctuate up and down. The fact that errors can be "fixed" doesn't reduce the historical count.
</details>

## Code Challenge

### 6. What's wrong with this metrics setup?

```csharp
var meter = new Meter("Surgewave.Application");
var latency = meter.CreateCounter<double>("produce_latency_ms");

// Later in code:
latency.Add(15.5);  // Record a latency of 15.5ms
```

<details>
<summary>Answer</summary>
The issue is using a **Counter for latency**. Latency should use a **Histogram** because:
1. Latency values are individual measurements, not cumulative totals
2. You need percentiles (p50, p99) which only histograms provide
3. Counters sum values, so you'd get cumulative latency which is meaningless

Fix:
```csharp
var meter = new Meter("Surgewave.Application");
var latency = meter.CreateHistogram<double>("produce_latency_ms");

// Later in code:
latency.Record(15.5);  // Record a latency measurement
```
</details>

### 7. Add proper tags to this metric recording:

```csharp
var meter = new Meter("Surgewave.Application");
var messagesProduced = meter.CreateCounter<long>("messages_produced");

// Record a message produced to the "orders" topic on partition 3
messagesProduced.Add(1);  // How do you add topic and partition tags?
```

<details>
<summary>Answer</summary>
Use KeyValuePair tags to add dimensions:

```csharp
var meter = new Meter("Surgewave.Application");
var messagesProduced = meter.CreateCounter<long>("messages_produced");

// Record with tags for filtering/grouping
messagesProduced.Add(1,
    new KeyValuePair<string, object?>("topic", "orders"),
    new KeyValuePair<string, object?>("partition", 3));
```

Now you can query metrics filtered by topic or partition, or group by either dimension.
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
