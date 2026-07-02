// Surgewave Bootcamp - Unit 12, Lesson 5: Integration Patterns
//
// In this lesson, you'll learn:
// - Bridge imperative and reactive code
// - Merge multiple observable sources
// - Build complete reactive pipelines
// - Implement async enrichment patterns
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 12.5 - Integration Patterns ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var port = runtime.Port;
Console.WriteLine($"Broker started on port {port}");
Console.WriteLine();

// TODO: Step 1 - Create MergeObservable<T>
// - Takes multiple IObservable<T> sources
// - Forwards all values from all sources
// - Completes when ALL sources complete

// TODO: Step 2 - Create IntervalObservable
// - Emits sequential numbers at specified interval
// - Optional count limit

// TODO: Step 3 - Create SelectManyObservable
// - For each source value, create inner observable
// - Flatten all inner values to output
// - Handle completion when outer and all inners complete

// TODO: Step 4 - Create FromAsyncObservable<T>
// - Convert Task<T> factory to IObservable<T>
// - Emit single value then complete

// TODO: Step 5 - Add extension methods
// public static class IntegrationExtensions { ... }

Console.WriteLine("--- Testing Interval Observable ---");
// TODO: Create interval that emits 5 values, 200ms apart

Console.WriteLine();
Console.WriteLine("--- Testing Merge with Multiple Topics ---");

// TODO: Produce to multiple topics and merge the streams
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{port}";
});

// Produce to orders topic
// Produce to payments topic
// Merge both streams

Console.WriteLine();
Console.WriteLine("--- Testing SelectMany for Async Enrichment ---");
// TODO: Use SelectMany to enrich messages with async data lookup

Console.WriteLine();
Console.WriteLine("--- Building Complete Pipeline ---");
// TODO: Build a pipeline that:
// 1. Merges multiple topics
// 2. Filters valid messages
// 3. Transforms to domain objects
// 4. Enriches with async lookup
// 5. Outputs to console

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Merge combines multiple streams into one");
Console.WriteLine("2. Interval creates time-based observables");
Console.WriteLine("3. SelectMany enables async enrichment/expansion");
Console.WriteLine("4. FromAsync bridges Task<T> to IObservable<T>");
Console.WriteLine("5. Patterns compose into complete reactive pipelines");

Console.WriteLine();
Console.WriteLine("Unit 12 Complete! You've mastered Reactive Integration!");
