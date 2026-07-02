// Surgewave Bootcamp - Unit 25, Lesson 5: Pipeline Monitoring and Debugging
//
// In this lesson, you'll learn:
// - Implement pipeline metrics collection
// - Build a monitoring dashboard
// - Create message tracing for debugging
// - Detect pipeline bottlenecks and issues
// - Visualize pipeline health
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 25.5 - Pipeline Monitoring and Debugging ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");
var bootstrapServers = runtime.BootstrapServers;

// TODO: Step 1 - Define metrics types
// public record NodeMetrics { ... }
// public record NodeHealth(string NodeId, string Status, ...);

// TODO: Step 2 - Implement metrics collector
// var collector = new PipelineMetricsCollector();

// TODO: Step 3 - Implement health calculator
// public static class HealthCalculator { ... }

// TODO: Step 4 - Implement message tracer
// var tracer = new MessageTracer();

// TODO: Step 5 - Process messages through a monitored pipeline
// Console.WriteLine("\n--- Processing 100 messages through monitored pipeline ---");
// await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
//
// for (int i = 0; i < 100; i++)
// {
//     var message = $"{{\"orderId\":\"ORD-{i:D4}\",\"amount\":{Random.Shared.Next(10, 500)}}}";
//     var traceId = tracer.StartTrace(message);
//
//     // Source: receive message
//     var sw = Stopwatch.StartNew();
//     await producer.ProduceAsync("input-orders", $"key-{i}", message);
//     sw.Stop();
//     collector.RecordProcessed("source", "Order Source", message.Length, sw.Elapsed.TotalMilliseconds);
//
//     // Transform: validate
//     sw.Restart();
//     var isValid = !message.Contains("\"amount\":0");
//     sw.Stop();
//     if (isValid)
//         collector.RecordProcessed("transform", "Order Validator", message.Length, sw.Elapsed.TotalMilliseconds);
//     else
//         collector.RecordDropped("transform", "Order Validator");
//
//     // Sink: produce to output
//     sw.Restart();
//     await producer.ProduceAsync("output-orders", $"key-{i}", message);
//     sw.Stop();
//     collector.RecordProcessed("sink", "Output Sink", message.Length, sw.Elapsed.TotalMilliseconds);
// }

// TODO: Step 6 - Display monitoring dashboard
// PipelineDashboard.Display(collector.GetAllMetrics());

// TODO: Step 7 - Show message trace
// Console.WriteLine("\n--- Message Trace ---");
// var trace = tracer.GetTrace(someTraceId);
// foreach (var entry in trace) { ... }

// TODO: Step 8 - Detect pipeline issues
// Console.WriteLine("\n--- Pipeline Issues Detected ---");
// var bottleneck = FindBottleneck(collector.GetAllMetrics());

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Monitor throughput, latency (P50/P90/P99), and error rate per node");
Console.WriteLine("2. Message tracing helps debug data transformation issues");
Console.WriteLine("3. The bottleneck is the node with the lowest throughput");
Console.WriteLine("4. Health status should be graduated (OK -> WARN -> CRIT)");
Console.WriteLine("5. Continuous monitoring establishes baselines for anomaly detection");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============================================================================
// TODO: Add monitoring types below
// ============================================================================

// public record NodeMetrics { ... }
// public record TraceEntry(...);
// public class PipelineMetricsCollector { ... }
// public static class HealthCalculator { ... }
// public class MessageTracer { ... }
// public static class PipelineDashboard { ... }