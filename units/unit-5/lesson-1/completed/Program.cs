// Surgewave Bootcamp - Unit 7, Lesson 1: Streams Basics
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Streams;
using Microsoft.Extensions.Logging;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 7.1 - Streams Basics ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// Create a logger factory
using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

// Create a StreamsBuilder
var builder = new StreamsBuilder { ApplicationId = "text-processor" };

// Define the processing topology
builder.Stream<string, string>("text-input")
    .Filter((key, value) => !string.IsNullOrEmpty(value))
    .MapValues(value => value.ToUpperInvariant())
    .Peek((key, value) => Console.WriteLine($"  Processing: {key} -> {value}"))
    .To("text-output");

// Build the topology
var topology = builder.Build();

// Print the topology
Console.WriteLine("--- Topology ---");
Console.WriteLine(topology.Describe());
Console.WriteLine();

// Create streams configuration
var config = new StreamsConfig
{
    ApplicationId = "text-processor",
    BootstrapServers = $"localhost:{runtime.Port}"
};

// Create and start the application
await using var app = new StreamsApplication(config, topology, loggerFactory);
app.Start();
Console.WriteLine("Application started!");
Console.WriteLine();

// Process test records
Console.WriteLine("--- Processing Records ---");
var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

app.ProcessRecord("text-input", "msg-1", "hello world", timestamp);
app.ProcessRecord("text-input", "msg-2", "", timestamp);  // Will be filtered
app.ProcessRecord("text-input", "msg-3", "surgewave streams rock", timestamp);
app.ProcessRecord("text-input", "msg-4", "real-time processing", timestamp);

Console.WriteLine();

// Demonstrate ForEach terminal operation
Console.WriteLine("--- ForEach Example ---");
var builder2 = new StreamsBuilder { ApplicationId = "foreach-demo" };
builder2.Stream<string, string>("events")
    .ForEach((key, value) => Console.WriteLine($"  Event: [{key}] {value}"));

var topology2 = builder2.Build();
var config2 = new StreamsConfig
{
    ApplicationId = "foreach-demo",
    BootstrapServers = $"localhost:{runtime.Port}"
};
await using var app2 = new StreamsApplication(config2, topology2, loggerFactory);
app2.Start();
app2.ProcessRecord("events", "e1", "User logged in", timestamp);
app2.ProcessRecord("events", "e2", "Order placed", timestamp);
await app2.CloseAsync();

Console.WriteLine();

// Demonstrate Print for debugging
Console.WriteLine("--- Print Example ---");
var builder3 = new StreamsBuilder { ApplicationId = "print-demo" };
builder3.Stream<string, string>("debug-topic")
    .Print();  // Prints to console

var topology3 = builder3.Build();
var config3 = new StreamsConfig
{
    ApplicationId = "print-demo",
    BootstrapServers = $"localhost:{runtime.Port}"
};
await using var app3 = new StreamsApplication(config3, topology3, loggerFactory);
app3.Start();
app3.ProcessRecord("debug-topic", "d1", "Debug message 1", timestamp);
app3.ProcessRecord("debug-topic", "d2", "Debug message 2", timestamp);
await app3.CloseAsync();

Console.WriteLine();

// Clean shutdown
await app.CloseAsync();

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. StreamsBuilder creates the processing topology");
Console.WriteLine("2. Stream<K,V>() reads from input topics");
Console.WriteLine("3. Filter, Map, Peek chain transformations");
Console.WriteLine("4. To() writes to output topics");
Console.WriteLine("5. ForEach() and Print() are terminal operations");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
