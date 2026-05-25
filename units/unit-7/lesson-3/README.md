# Lesson 7.3: Backpressure & Flow Control

When a downstream processor is slower than the upstream, records pile up in memory. Backpressure mechanisms prevent out-of-memory conditions by bounding internal buffers and applying flow control.

## Learning Objectives

- Configure `BackpressureBuffer` with bounded channels
- Choose the right backpressure strategy (Block, DropOldest, DropNewest)
- Understand watermark-based flow control and hysteresis
- Monitor backpressure metrics

## BackpressureConfig

```csharp
public class BackpressureConfig
{
    public int MaxBufferedRecords { get; set; } = 10_000;
    public double HighWatermark { get; set; } = 0.8;  // 80%
    public double LowWatermark { get; set; } = 0.4;   // 40%
    public BackpressureStrategy Strategy { get; set; } = BackpressureStrategy.Block;
}
```

## Backpressure Strategies

| Strategy | Behavior | Data Loss | Use Case |
|----------|----------|-----------|----------|
| Block | Upstream pauses until space is available | None | Financial, transactional |
| DropOldest | Oldest buffered record is discarded | Yes (oldest) | Real-time analytics |
| DropNewest | Incoming record is discarded | Yes (newest) | Sensor telemetry |

## Watermark Flow Control

The hysteresis between high and low watermarks prevents oscillation:

- **High Watermark (80%)**: Begin slowing the upstream producer
- **Low Watermark (40%)**: Resume full speed
- Keep at least a 20% gap between watermarks to prevent rapid toggling

## Configuring Backpressure

```csharp
builder.Stream<string, SensorReading>("sensor-data")
    .WithBackpressure(new BackpressureConfig
    {
        MaxBufferedRecords = 50_000,
        HighWatermark = 0.75,
        LowWatermark = 0.3,
        Strategy = BackpressureStrategy.DropOldest
    })
    .Filter((key, value) => value.Temperature > 100)
    .To("high-temp-alerts");
```

## Backpressure Metrics

| Metric Name | Type | Description |
|-------------|------|-------------|
| `surgewave.streams.backpressure.buffer_size` | ObservableGauge | Current records in buffer |
| `surgewave.streams.backpressure.dropped_total` | Counter | Total records dropped |

## Exercise

1. Create a topology with a fast producer (10,000 records/s) and a slow consumer (1,000 records/s)
2. Run without backpressure and observe memory growth
3. Add `WithBackpressure` with `MaxBufferedRecords = 5000` and `Strategy = Block`
4. Observe that the producer throttles to match consumer speed
5. Switch to `DropOldest` and verify records are dropped when the buffer fills
6. Monitor `backpressure.buffer_size` and `backpressure.dropped_total` metrics

---

[Back to Unit 7](../README.md)
