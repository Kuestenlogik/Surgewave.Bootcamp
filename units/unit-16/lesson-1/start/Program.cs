// Surgewave Bootcamp - Unit 25, Lesson 1: Pipeline UI Overview
//
// In this lesson, you'll learn:
// - Visual Pipeline UI architecture and components
// - When to use visual pipelines vs code-based pipelines
// - Pipeline definitions with nodes and edges
// - Pipeline validation for DAG structure
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 25.1 - Pipeline UI Overview ===");
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

// TODO: Step 1 - Define the node types
// Create base classes for pipeline nodes:
//
// public abstract record PipelineNode(string Id, string Name, NodePosition Position);
// public record NodePosition(int X, int Y);
// public record SourceNode(...) : PipelineNode(...);
// public record TransformNode(...) : PipelineNode(...);
// public record SinkNode(...) : PipelineNode(...);

// TODO: Step 2 - Define the pipeline structure
// public record PipelineDefinition(
//     string Id,
//     string Name,
//     string Version,
//     List<PipelineNode> Nodes,
//     List<PipelineEdge> Edges
// );
// public record PipelineEdge(string From, string To);

// TODO: Step 3 - Build a simple pipeline
// Console.WriteLine("\n--- Pipeline Definition ---");
//
// var pipeline = new PipelineDefinition(
//     Id: "order-processing-v1",
//     Name: "Order Processing Pipeline",
//     Version: "1.0.0",
//     Nodes: new List<PipelineNode>
//     {
//         new SourceNode("source-orders", "Order Events", new NodePosition(100, 100), "raw-orders", "json"),
//         new TransformNode("filter-valid", "Filter Valid Orders", new NodePosition(300, 100), "Filter", "order.Total > 0", new List<string> { "source-orders" }),
//         new SinkNode("sink-validated", "Validated Orders", new NodePosition(500, 100), "validated-orders", new List<string> { "filter-valid" })
//     },
//     Edges: new List<PipelineEdge>
//     {
//         new PipelineEdge("source-orders", "filter-valid"),
//         new PipelineEdge("filter-valid", "sink-validated")
//     }
// );

// TODO: Step 4 - Implement pipeline validation
// var validator = new PipelineValidator();
// var result = validator.Validate(pipeline);
// Console.WriteLine($"\n--- Validation Result ---");
// Console.WriteLine($"Valid: {result.IsValid}");
// Console.WriteLine($"Errors: {result.Errors.Count}");
// Console.WriteLine($"Warnings: {result.Warnings.Count}");

// TODO: Step 5 - Visualize the pipeline
// PipelineVisualizer.PrintPipeline(pipeline);

// TODO: Step 6 - Simulate pipeline execution
// Console.WriteLine("\n--- Pipeline Execution Simulation ---");
// foreach (var node in pipeline.Nodes.OfType<SourceNode>())
// {
//     Console.WriteLine($"[Pipeline] Creating consumer for topic: {node.Topic}");
// }
// foreach (var node in pipeline.Nodes.OfType<TransformNode>())
// {
//     Console.WriteLine($"[Pipeline] Setting up transform: {node.TransformType}");
//     Console.WriteLine($"           Expression: {node.Expression}");
// }
// foreach (var node in pipeline.Nodes.OfType<SinkNode>())
// {
//     Console.WriteLine($"[Pipeline] Creating producer for topic: {node.Topic}");
// }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Visual pipelines use a DAG (Directed Acyclic Graph) structure");
Console.WriteLine("2. Nodes represent sources, transforms, and sinks");
Console.WriteLine("3. Edges connect nodes to define data flow");
Console.WriteLine("4. Validation ensures pipeline integrity before execution");
Console.WriteLine("5. Visual pipelines compile to the same runtime as code pipelines");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============================================================================
// TODO: Add the following classes at the end of the file
// ============================================================================

// Node types
// public abstract record PipelineNode(string Id, string Name, NodePosition Position);
// public record NodePosition(int X, int Y);
// public record SourceNode(string Id, string Name, NodePosition Position, string Topic, string Format) : PipelineNode(Id, Name, Position);
// public record TransformNode(string Id, string Name, NodePosition Position, string TransformType, string Expression, List<string> Inputs) : PipelineNode(Id, Name, Position);
// public record SinkNode(string Id, string Name, NodePosition Position, string Topic, List<string> Inputs) : PipelineNode(Id, Name, Position);

// Pipeline definition
// public record PipelineDefinition(string Id, string Name, string Version, List<PipelineNode> Nodes, List<PipelineEdge> Edges);
// public record PipelineEdge(string From, string To);

// Validation
// public record ValidationResult(bool IsValid, List<string> Errors, List<string> Warnings);
// public class PipelineValidator { ... }

// Visualization
// public static class PipelineVisualizer { ... }
