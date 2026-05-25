# Lesson 9.2: Metrics & Monitoring - Instructions

## Step 1: Create a Meter

Create a meter instance to define metrics:

```csharp
using System.Diagnostics.Metrics;

var meter = new Meter("Surgewave.Application", "1.0.0");
```

## Step 2: Define Counter Metrics

Counters track cumulative values that only increase:

```csharp
var messagesProduced = meter.CreateCounter<long>(
    "surgewave_messages_produced_total",
    "messages",
    "Total messages produced");

var messagesConsumed = meter.CreateCounter<long>(
    "surgewave_messages_consumed_total",
    "messages",
    "Total messages consumed");

var errorsTotal = meter.CreateCounter<long>(
    "surgewave_errors_total",
    "errors",
    "Total errors encountered");
```

## Step 3: Define Histogram Metrics

Histograms track the distribution of values:

```csharp
var produceLatency = meter.CreateHistogram<double>(
    "surgewave_produce_latency_ms",
    "ms",
    "Produce operation latency");

var consumeLatency = meter.CreateHistogram<double>(
    "surgewave_consume_latency_ms",
    "ms",
    "Consume operation latency");

var messageSize = meter.CreateHistogram<long>(
    "surgewave_message_size_bytes",
    "bytes",
    "Message payload size");
```

## Step 4: Define Observable Gauges

Observable gauges report current values on demand:

```csharp
var consumerLag = meter.CreateObservableGauge(
    "surgewave_consumer_lag",
    () => GetConsumerLag(),
    "messages",
    "Current consumer lag");

long GetConsumerLag()
{
    // Return current lag value
    return currentOffset - lastCommittedOffset;
}
```

## Step 5: Use Tags for Dimensions

Add tags to create multi-dimensional metrics:

```csharp
// Counter with tags
messagesProduced.Add(1,
    new KeyValuePair<string, object?>("topic", "orders"),
    new KeyValuePair<string, object?>("partition", "0"));

// Histogram with tags
produceLatency.Record(15.5,
    new KeyValuePair<string, object?>("topic", "orders"));
```

## Step 6: Create a Metrics Listener

Listen and export metrics:

```csharp
var listener = new MeterListener();
listener.InstrumentPublished = (instrument, listener) =>
{
    if (instrument.Meter.Name == "Surgewave.Application")
    {
        listener.EnableMeasurementEvents(instrument);
    }
};

listener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
{
    Console.WriteLine($"{instrument.Name}: {value}");
});

listener.Start();
```

## Verification

Run the completed solution:
```bash
cd completed
dotnet run
```

You should see:
- Counter metrics incrementing with each message
- Histogram values for latency measurements
- Observable gauge reporting consumer lag
