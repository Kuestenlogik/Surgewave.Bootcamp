# Lesson 4.3: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Enable Metrics Endpoint

```csharp
var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithMetricsPort(9090)  // Prometheus endpoint on :9090/metrics
    .Build()
    .StartAsync();

Console.WriteLine($"Metrics available at http://localhost:9090/metrics");
```

## Step 2: View Metrics Programmatically

```csharp
using var httpClient = new HttpClient();
var metrics = await httpClient.GetStringAsync("http://localhost:9090/metrics");

Console.WriteLine("Available Metrics:");
foreach (var line in metrics.Split('\n').Where(l => l.StartsWith("surgewave_")))
{
    Console.WriteLine($"  {line}");
}
```

## Step 3: Configure Prometheus

Create `prometheus.yml`:

```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'surgewave'
    static_configs:
      - targets: ['localhost:9090']
```

## Step 4: Monitor Consumer Lag

```csharp
// Get consumer group lag
var admin = new SurgewaveAdminClient(opt =>
    opt.BootstrapServers = bootstrapServers);

var groups = await admin.ListConsumerGroupsAsync();

foreach (var group in groups)
{
    var offsets = await admin.GetConsumerGroupOffsetsAsync(group.GroupId);

    foreach (var offset in offsets)
    {
        var highWatermark = await admin.GetHighWatermarkAsync(offset.Topic, offset.Partition);
        var lag = highWatermark - offset.Offset;

        Console.WriteLine($"Group {group.GroupId}: {offset.Topic}/{offset.Partition} lag={lag}");
    }
}
```

## Step 5: Create Alert Rules

Example Prometheus alert rules:

```yaml
groups:
  - name: surgewave_alerts
    rules:
      - alert: HighConsumerLag
        expr: surgewave_consumer_lag > 10000
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High consumer lag detected"

      - alert: UnderReplicatedPartitions
        expr: surgewave_broker_under_replicated_partitions > 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Under-replicated partitions detected"
```

## Step 6: Client Metrics

```csharp
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.EnableMetrics = true;
});

// Access client metrics
var stats = producer.GetStatistics();
Console.WriteLine($"Messages sent: {stats.MessagesSent}");
Console.WriteLine($"Bytes sent: {stats.BytesSent}");
Console.WriteLine($"Average latency: {stats.AverageLatencyMs}ms");
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. What metrics indicate a producer problem?
2. How do you detect under-replicated partitions?
3. What's a healthy consumer lag target?

---

**Next:** → [Lesson 4.4: Security](../lesson-4/README.md)
