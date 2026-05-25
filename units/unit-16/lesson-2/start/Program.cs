// Surgewave Bootcamp - Unit 25, Lesson 2: Creating Visual Pipelines
//
// In this lesson, you'll learn:
// - Build pipelines using a fluent PipelineBuilder API
// - Create and configure nodes programmatically
// - Validate connections between nodes
// - Export/import pipeline definitions as JSON
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 25.2 - Creating Visual Pipelines ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");
var bootstrapServers = runtime.BootstrapServers;

// TODO: Step 1 - Create node builder classes
// Implement SourceNodeBuilder, TransformNodeBuilder, SinkNodeBuilder
// Each should have fluent methods for configuration

// TODO: Step 2 - Create the PipelineBuilder class
// Implement a fluent builder that supports:
// - AddSource(id, configure)
// - AddTransform(id, configure)
// - AddSink(id, configure)
// - Connect(from, to)
// - Build()

// TODO: Step 3 - Build a pipeline using the builder
// Console.WriteLine("\n--- Building Pipeline with Fluent API ---");
// var pipeline = new PipelineBuilder("order-processing")
//     .WithName("Order Processing Pipeline")
//     .WithVersion("1.0.0")
//     .AddSource("source-orders", b => b
//         .WithName("Raw Orders")
//         .FromTopic("raw-orders")
//         .AtPosition(100, 100))
//     .AddTransform("filter-valid", b => b
//         .WithName("Filter Valid")
//         .Filter("order.Total > 0")
//         .AtPosition(300, 100))
//     .AddSink("sink-processed", b => b
//         .WithName("Processed Orders")
//         .ToTopic("processed-orders")
//         .AtPosition(500, 100))
//     .Connect("source-orders", "filter-valid")
//     .Connect("filter-valid", "sink-processed")
//     .Build();

// TODO: Step 4 - Validate the pipeline
// var validator = new PipelineValidator();
// var result = validator.Validate(pipeline);
// Console.WriteLine($"\n--- Validation ---");
// Console.WriteLine($"Valid: {result.IsValid}");

// TODO: Step 5 - Export pipeline to JSON
// Console.WriteLine("\n--- Exported JSON ---");
// var json = PipelineSerializer.ToJson(pipeline);
// Console.WriteLine(json);

// TODO: Step 6 - Execute the pipeline with Surgewave
// Produce test data and consume through the pipeline

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. PipelineBuilder provides a fluent API for pipeline creation");
Console.WriteLine("2. Connection validation ensures DAG integrity");
Console.WriteLine("3. JSON export enables version control and sharing");
Console.WriteLine("4. Visual pipelines execute the same as code pipelines");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============================================================================
// TODO: Add the node and pipeline classes below
// ============================================================================

// Node types (from Lesson 25.1)
public abstract record PipelineNode(string Id, string Name, NodePosition Position);
public record NodePosition(int X, int Y);
public record SourceNode(string Id, string Name, NodePosition Position, string Topic, string Format) : PipelineNode(Id, Name, Position);
public record TransformNode(string Id, string Name, NodePosition Position, string TransformType, string Expression, List<string> Inputs) : PipelineNode(Id, Name, Position);
public record SinkNode(string Id, string Name, NodePosition Position, string Topic, List<string> Inputs) : PipelineNode(Id, Name, Position);

// Pipeline definition
public record PipelineDefinition(string Id, string Name, string Version, List<PipelineNode> Nodes, List<PipelineEdge> Edges);
public record PipelineEdge(string From, string To);

// Validation
public record ValidationResult(bool IsValid, List<string> Errors, List<string> Warnings);

// TODO: Implement SourceNodeBuilder
// public class SourceNodeBuilder { ... }

// TODO: Implement TransformNodeBuilder
// public class TransformNodeBuilder { ... }

// TODO: Implement SinkNodeBuilder
// public class SinkNodeBuilder { ... }

// TODO: Implement PipelineBuilder
// public class PipelineBuilder { ... }

// TODO: Implement PipelineValidator
// public class PipelineValidator { ... }

// TODO: Implement PipelineSerializer
// public static class PipelineSerializer { ... }
