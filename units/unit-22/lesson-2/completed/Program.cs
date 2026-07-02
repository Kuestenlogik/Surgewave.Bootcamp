// Surgewave Bootcamp - Unit 9, Lesson 2: Metrics & Monitoring
// COMPLETED SOLUTION

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 9.2 - Metrics & Monitoring ===");
Console.WriteLine();

// ============== Demo 1: Create Meters and Instruments ==============
Console.WriteLine("--- Demo 1: Create Meters and Instruments ---");

var meter = new Meter("Surgewave.Application", "1.0.0");

// Counters - for cumulative values
var messagesProduced = meter.CreateCounter<long>(
    "surgewave_messages_produced_total",
    "messages",
    "Total number of messages produced");

var messagesConsumed = meter.CreateCounter<long>(
    "surgewave_messages_consumed_total",
    "messages",
    "Total number of messages consumed");

var errorsTotal = meter.CreateCounter<long>(
    "surgewave_errors_total",
    "errors",
    "Total number of errors");

// Histograms - for distributions
var produceLatency = meter.CreateHistogram<double>(
    "surgewave_produce_latency_ms",
    "ms",
    "Latency of produce operations");

var consumeLatency = meter.CreateHistogram<double>(
    "surgewave_consume_latency_ms",
    "ms",
    "Latency of consume operations");

var messageSize = meter.CreateHistogram<long>(
    "surgewave_message_size_bytes",
    "bytes",
    "Size of messages in bytes");

// Observable gauge - for current values
var lagValue = 0L;
var consumerLag = meter.CreateObservableGauge(
    "surgewave_consumer_lag",
    () => lagValue,
    "messages",
    "Current consumer lag");

Console.WriteLine("  Created metrics:");
Console.WriteLine("    - surgewave_messages_produced_total (counter)");
Console.WriteLine("    - surgewave_messages_consumed_total (counter)");
Console.WriteLine("    - surgewave_errors_total (counter)");
Console.WriteLine("    - surgewave_produce_latency_ms (histogram)");
Console.WriteLine("    - surgewave_consume_latency_ms (histogram)");
Console.WriteLine("    - surgewave_message_size_bytes (histogram)");
Console.WriteLine("    - surgewave_consumer_lag (gauge)");
Console.WriteLine();

// ============== Demo 2: Metrics Listener ==============
Console.WriteLine("--- Demo 2: Metrics Listener ---");

var metricsCollector = new MetricsCollector();

using var listener = new MeterListener();

listener.InstrumentPublished = (instrument, meterListener) =>
{
    if (instrument.Meter.Name == "Surgewave.Application")
    {
        Console.WriteLine($"  Subscribing to: {instrument.Name}");
        meterListener.EnableMeasurementEvents(instrument);
    }
};

listener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
{
    metricsCollector.RecordLong(instrument.Name, value, tags);
});

listener.SetMeasurementEventCallback<double>((instrument, value, tags, state) =>
{
    metricsCollector.RecordDouble(instrument.Name, value, tags);
});

listener.Start();
Console.WriteLine();

// ============== Demo 3: Record Counter Metrics ==============
Console.WriteLine("--- Demo 3: Record Counter Metrics ---");

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

// Create producer
await using var producer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

// Produce messages with metrics
var topics = new[] { "orders", "events", "logs" };
var random = new Random(42);

for (var i = 0; i < 10; i++)
{
    var topic = topics[i % topics.Length];
    var key = $"key-{i}";
    var value = $"{{\"id\":{i},\"data\":\"message-{i}\"}}";
    var valueBytes = Encoding.UTF8.GetBytes(value);

    // Measure produce latency
    var sw = Stopwatch.StartNew();
    await producer.ProduceAsync(topic,
        Encoding.UTF8.GetBytes(key),
        valueBytes);
    sw.Stop();

    // Record metrics with tags
    messagesProduced.Add(1,
        new KeyValuePair<string, object?>("topic", topic));

    produceLatency.Record(sw.Elapsed.TotalMilliseconds,
        new KeyValuePair<string, object?>("topic", topic));

    messageSize.Record(valueBytes.Length,
        new KeyValuePair<string, object?>("topic", topic));

    // Simulate lag increase
    lagValue = 10 - i;

    Console.WriteLine($"  Produced to {topic}: key={key}, latency={sw.Elapsed.TotalMilliseconds:F2}ms");
}

Console.WriteLine();

// ============== Demo 4: Record Consumer Metrics ==============
Console.WriteLine("--- Demo 4: Record Consumer Metrics ---");

await using var consumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "metrics-demo";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe(topics);

var consumeTimeout = DateTime.UtcNow.AddSeconds(3);
var consumed = 0;

while (DateTime.UtcNow < consumeTimeout && consumed < 10)
{
    var sw = Stopwatch.StartNew();
    var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
    sw.Stop();

    if (result != null)
    {
        consumed++;
        lagValue = Math.Max(0, lagValue - 1);

        messagesConsumed.Add(1,
            new KeyValuePair<string, object?>("topic", result.Topic),
            new KeyValuePair<string, object?>("group", "metrics-demo"));

        consumeLatency.Record(sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("topic", result.Topic));

        var key = Encoding.UTF8.GetString(result.Key ?? Array.Empty<byte>());
        Console.WriteLine($"  Consumed from {result.Topic}: key={key}, latency={sw.Elapsed.TotalMilliseconds:F2}ms");
    }
}

Console.WriteLine();

// ============== Demo 5: Simulate Errors ==============
Console.WriteLine("--- Demo 5: Error Tracking ---");

// Simulate some errors
for (var i = 0; i < 3; i++)
{
    try
    {
        // Simulate error conditions
        if (i % 2 == 0) throw new TimeoutException("Connection timeout");
        throw new InvalidOperationException("Invalid state");
    }
    catch (TimeoutException)
    {
        errorsTotal.Add(1,
            new KeyValuePair<string, object?>("type", "timeout"));
        Console.WriteLine("  Recorded timeout error");
    }
    catch (InvalidOperationException)
    {
        errorsTotal.Add(1,
            new KeyValuePair<string, object?>("type", "invalid_operation"));
        Console.WriteLine("  Recorded invalid operation error");
    }
}

Console.WriteLine();

// ============== Demo 6: Metrics Summary ==============
Console.WriteLine("--- Demo 6: Metrics Summary ---");

// Read the observable gauge
listener.RecordObservableInstruments();

Console.WriteLine();
metricsCollector.PrintSummary();

Console.WriteLine();

// Summary
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use Counters for cumulative values (messages, errors)");
Console.WriteLine("2. Use Histograms for distributions (latency, sizes)");
Console.WriteLine("3. Use Observable Gauges for current values (lag, connections)");
Console.WriteLine("4. Add tags/labels for multi-dimensional metrics");
Console.WriteLine("5. Use MeterListener to export metrics");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Metrics Collector ==============

public class MetricsCollector
{
    private readonly Dictionary<string, List<(long Value, TagList Tags)>> _longMetrics = new();
    private readonly Dictionary<string, List<(double Value, TagList Tags)>> _doubleMetrics = new();
    private readonly object _lock = new();

    public void RecordLong(string name, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        lock (_lock)
        {
            if (!_longMetrics.TryGetValue(name, out var list))
            {
                list = new List<(long, TagList)>();
                _longMetrics[name] = list;
            }

            var tagList = new TagList();
            foreach (var tag in tags)
            {
                tagList.Add(tag);
            }
            list.Add((value, tagList));
        }
    }

    public void RecordDouble(string name, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        lock (_lock)
        {
            if (!_doubleMetrics.TryGetValue(name, out var list))
            {
                list = new List<(double, TagList)>();
                _doubleMetrics[name] = list;
            }

            var tagList = new TagList();
            foreach (var tag in tags)
            {
                tagList.Add(tag);
            }
            list.Add((value, tagList));
        }
    }

    public void PrintSummary()
    {
        Console.WriteLine("  === Metrics Summary ===");

        lock (_lock)
        {
            // Print counter summaries (long metrics)
            foreach (var (name, values) in _longMetrics.OrderBy(x => x.Key))
            {
                if (name.Contains("total") || name.Contains("count"))
                {
                    // Counter - sum all values
                    var total = values.Sum(v => v.Value);
                    Console.WriteLine($"  {name}: {total}");

                    // Group by tags
                    var byTags = values
                        .GroupBy(v => string.Join(",", v.Tags.Select(t => $"{t.Key}={t.Value}")))
                        .Where(g => !string.IsNullOrEmpty(g.Key));

                    foreach (var group in byTags)
                    {
                        var sum = group.Sum(v => v.Value);
                        Console.WriteLine($"    [{group.Key}]: {sum}");
                    }
                }
                else
                {
                    // Histogram - show distribution
                    var allValues = values.Select(v => (double)v.Value).ToList();
                    PrintHistogramStats(name, allValues);
                }
            }

            // Print histogram summaries (double metrics)
            foreach (var (name, values) in _doubleMetrics.OrderBy(x => x.Key))
            {
                var allValues = values.Select(v => v.Value).ToList();
                PrintHistogramStats(name, allValues);
            }
        }
    }

    private void PrintHistogramStats(string name, List<double> values)
    {
        if (values.Count == 0) return;

        var sorted = values.OrderBy(v => v).ToList();
        var avg = sorted.Average();
        var min = sorted.First();
        var max = sorted.Last();
        var p50 = sorted[(int)(sorted.Count * 0.50)];
        var p99 = sorted[(int)(sorted.Count * 0.99)];

        Console.WriteLine($"  {name}:");
        Console.WriteLine($"    count={values.Count}, min={min:F2}, avg={avg:F2}, p50={p50:F2}, p99={p99:F2}, max={max:F2}");
    }
}
