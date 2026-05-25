// Surgewave Bootcamp - Unit 7, Lesson 1: Streams Basics
//
// In this lesson, you'll learn to:
// - Create a StreamsBuilder
// - Define a Stream from a topic
// - Apply basic transformations
// - Write to an output topic
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Streams;
using Microsoft.Extensions.Logging;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 7.1 - Streams Basics ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// Create a logger factory
using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

// TODO: Create a StreamsBuilder with application ID "text-processor"
// Hint: var builder = new StreamsBuilder { ApplicationId = "text-processor" };

// TODO: Create a stream from the "text-input" topic
// Hint: builder.Stream<string, string>("text-input")

// TODO: Add transformations:
// 1. Filter out empty messages
// 2. Convert to uppercase
// 3. Add a Peek to log processing
// Hint: .Filter((key, value) => !string.IsNullOrEmpty(value))
//       .MapValues(value => value.ToUpperInvariant())
//       .Peek((key, value) => Console.WriteLine($"  Processing: {key} -> {value}"))

// TODO: Write to "text-output" topic
// Hint: .To("text-output");

// TODO: Build the topology
// Hint: var topology = builder.Build();

// TODO: Create streams configuration and application
// Hint: var config = new StreamsConfig { ApplicationId = "text-processor", BootstrapServers = $"localhost:{runtime.Port}" };
//       await using var app = new StreamsApplication(config, topology, loggerFactory);

// TODO: Start the application
// Hint: app.Start();

// TODO: Print the topology
// Hint: Console.WriteLine(topology.Describe());

// TODO: Process test records
// Hint: var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
//       app.ProcessRecord("text-input", "msg-1", "hello world", ts);

// TODO: Close the application
// Hint: await app.CloseAsync();

Console.WriteLine();
Console.WriteLine("Lesson complete!");
