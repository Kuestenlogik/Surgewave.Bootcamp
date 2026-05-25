// Surgewave Bootcamp - Unit 24 Project: Tiered Storage Simulator
//
// Build a tiered storage simulator that demonstrates:
// - Hot -> Warm -> Cold data movement
// - Retention policy enforcement
// - Cost tracking and savings calculation
// - Segment-based storage with compression
//
// See the project README.md for full requirements.

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Tiered Storage Simulator ===");
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

// TODO: Define storage tier enum and segment model
// public enum StorageTier { Hot, Warm, Cold }

// TODO: Define retention policy
// Console.WriteLine("[Setup] Retention policy: Hot(1h) -> Warm(7d) -> Cold(30d) -> Delete");

// TODO: Phase 1 - Ingest messages and create segments
// Console.WriteLine("\n--- Phase 1: Ingesting Messages ---");
// await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
// Produce messages to 'orders' topic and group into segments

// TODO: Phase 2 - Simulate time passage and tier migration
// Console.WriteLine("\n--- Phase 2: Simulating Time Passage ---");
// Advance simulated time and run migration at each checkpoint

// TODO: Phase 3 - Multi-segment lifecycle
// Console.WriteLine("\n--- Phase 3: Multi-Segment Lifecycle ---");
// Create segments at various ages and show tier distribution

// TODO: Generate cost report
// Console.WriteLine("\n--- Cost Report ---");
// Compare single-tier vs tiered storage costs

Console.WriteLine();
Console.WriteLine("=== Simulation Complete ===");