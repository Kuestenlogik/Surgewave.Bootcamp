// Surgewave Bootcamp - Unit 9, Lesson 3: Distributed Tracing
//
// In this lesson, you'll learn to:
// - Create activities for distributed tracing
// - Propagate trace context through message headers
// - Visualize traces across services
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 9.3 - Distributed Tracing ===");
Console.WriteLine();

// TODO: Create an ActivitySource
// Hint: var activitySource = new ActivitySource("Surgewave.Application", "1.0.0");

// TODO: Create an ActivityListener to observe activities
// Hint: var listener = new ActivityListener
//       {
//           ShouldListenTo = source => source.Name == "Surgewave.Application",
//           Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
//               ActivitySamplingResult.AllDataAndRecorded,
//           ActivityStarted = activity => { ... },
//           ActivityStopped = activity => { ... }
//       };
//       ActivitySource.AddActivityListener(listener);

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// TODO: Create a root activity for the request
// Hint: using var requestActivity = activitySource.StartActivity("ProcessOrder");

// TODO: Create a child activity for producing
// Hint: using var produceActivity = activitySource.StartActivity("ProduceMessage");
//       produceActivity?.SetTag("messaging.destination", "orders");

// TODO: Propagate trace context through headers
// Hint: var headers = new Dictionary<string, string>
//       {
//           ["traceparent"] = Activity.Current?.Id ?? ""
//       };

// TODO: Create a child activity for consuming
// Hint: Extract parentId from headers and create activity with parent

// TODO: Print a trace visualization

Console.WriteLine();
Console.WriteLine("Lesson complete!");
