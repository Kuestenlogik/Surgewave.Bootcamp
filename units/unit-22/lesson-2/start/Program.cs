// Surgewave Bootcamp - Unit 9, Lesson 2: Metrics & Monitoring
//
// In this lesson, you'll learn to:
// - Create metrics with System.Diagnostics.Metrics
// - Track counters, histograms, and gauges
// - Implement Prometheus-style monitoring patterns
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 9.2 - Metrics & Monitoring ===");
Console.WriteLine();

// TODO: Create a Meter for application metrics
// Hint: var meter = new Meter("Surgewave.Application", "1.0.0");

// TODO: Create counter metrics
// Hint: var messagesProduced = meter.CreateCounter<long>("surgewave_messages_produced_total");
//       var messagesConsumed = meter.CreateCounter<long>("surgewave_messages_consumed_total");

// TODO: Create histogram metrics for latency
// Hint: var produceLatency = meter.CreateHistogram<double>("surgewave_produce_latency_ms");

// TODO: Create an observable gauge for consumer lag
// Hint: var consumerLag = meter.CreateObservableGauge("surgewave_consumer_lag", () => lagValue);

// TODO: Create a MeterListener to capture metrics
// Hint: var listener = new MeterListener();
//       listener.InstrumentPublished = (instrument, listener) => { ... };
//       listener.SetMeasurementEventCallback<long>((instrument, value, tags, state) => { ... });

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// TODO: Produce messages and record metrics
// Hint: var sw = Stopwatch.StartNew();
//       await producer.ProduceAsync(...);
//       sw.Stop();
//       messagesProduced.Add(1, new KeyValuePair<string, object?>("topic", "orders"));
//       produceLatency.Record(sw.Elapsed.TotalMilliseconds);

// TODO: Consume messages and record metrics
// Hint: messagesConsumed.Add(1, ...);

// TODO: Print a summary of collected metrics

Console.WriteLine();
Console.WriteLine("Lesson complete!");
