# Lesson 7.2: Per-Processor Metrics

Surgewave exposes per-processor OpenTelemetry metrics so you can monitor each node's performance individually rather than only at the topology level.

## Learning Objectives

- Understand `ProcessorNodeMetrics` and its four core metrics
- Use OTEL instruments for records in/out, latency, and errors
- Monitor state store operations with `StateStoreMetrics`
- Access metrics programmatically via `StreamsMetrics`

## ProcessorNodeMetrics

Each processor node automatically tracks four metrics:

| Metric Name | Type | Unit | Description |
|-------------|------|------|-------------|
| `surgewave.streams.processor.records_in` | Counter | records | Total records received by this node |
| `surgewave.streams.processor.records_out` | Counter | records | Total records forwarded by this node |
| `surgewave.streams.processor.latency` | Histogram | ms | Processing time per record |
| `surgewave.streams.processor.errors` | Counter | records | Total processing errors |

All metrics are tagged with `processor_name` and `topology_id` labels.

## StateStoreMetrics

State stores also expose per-operation metrics:

| Metric Name | Type | Unit | Description |
|-------------|------|------|-------------|
| `surgewave.streams.store.put_latency` | Histogram | ms | Put operation duration |
| `surgewave.streams.store.get_latency` | Histogram | ms | Get operation duration |
| `surgewave.streams.store.delete_latency` | Histogram | ms | Delete operation duration |
| `surgewave.streams.store.entries` | ObservableGauge | entries | Current entry count |

## Accessing Metrics

```csharp
var metrics = streams.Metrics;
var filterMetrics = metrics.GetProcessorMetrics("filter-orders");

Console.WriteLine($"Records In:  {filterMetrics.RecordsIn}");
Console.WriteLine($"Records Out: {filterMetrics.RecordsOut}");
Console.WriteLine($"Error Count: {filterMetrics.Errors}");
```

## Exercise

1. Build a 3-node topology: Source -> Filter -> Enrichment -> Sink
2. Export metrics to the console using `ConsoleExporter`
3. Observe `records_in` and `records_out` for the Filter node to calculate filter-out ratio
4. Track `latency` histograms for the Enrichment node
5. Monitor `store.entries` gauge to watch state store growth

---

[Back to Unit 7](../README.md)
