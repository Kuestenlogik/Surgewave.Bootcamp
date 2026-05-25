// Surgewave Bootcamp - Unit 25 Project: Visual Pipeline - Multi-Stage Data Processing
//
// Build a multi-stage data processing pipeline with:
// - Source: Read from Surgewave topic
// - Transform: Filter, enrich, and route orders
// - Sink: Write to output topics
// - Monitoring: Collect and display pipeline metrics
//
// See the project README.md for full requirements.

using System.Diagnostics;
using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Visual Pipeline Project: E-Commerce Order Processing ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;
Console.WriteLine($"[Setup] Surgewave broker started on port {runtime.Port}");
Console.WriteLine("[Setup] Pipeline: Source -> Filter -> Enrich -> Router -> Sinks");

// TODO: Phase 1 - Produce test orders
// Console.WriteLine("\n--- Phase 1: Producing Test Orders ---");
// await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
// Generate 200 test orders with varied data (valid/invalid, high/standard priority)

// TODO: Phase 2 - Process pipeline
// Console.WriteLine("\n--- Phase 2: Processing Pipeline ---");
// Create metrics collector
// Read messages from source topic
// Apply filter -> enrich -> route -> sink

// TODO: Phase 3 - Display dashboard
// Console.WriteLine("\n--- Phase 3: Pipeline Dashboard ---");
// Show per-node metrics

// TODO: Phase 4 - Analysis
// Console.WriteLine("\n--- Phase 4: Analysis ---");
// Find bottleneck, report drop rates, error rates

Console.WriteLine();
Console.WriteLine("=== Pipeline Complete ===");