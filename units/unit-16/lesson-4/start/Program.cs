// Surgewave Bootcamp - Unit 25, Lesson 4: DAG Patterns
//
// In this lesson, you'll learn:
// - Fan-out pattern (one source to many processors)
// - Fan-in pattern (many sources to one sink)
// - Diamond pattern (fan-out + fan-in)
// - Conditional branching (content-based routing)
// - Parallel processing patterns
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 25.4 - DAG Patterns ===");
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

// TODO: Step 1 - Define DAG pattern types
// public enum DagPatternType { FanOut, FanIn, Diamond, ConditionalBranch, ParallelProcessing }
// public abstract record DagNode(string Id, string Name, DagNodeType NodeType);
// public record DagEdge(string From, string To, string? Label = null);

// TODO: Step 2 - Implement Fan-Out pattern
// Console.WriteLine("\n--- Pattern 1: Fan-Out ---");
// var fanOut = CreateFanOutPattern();
// var executor = new DagExecutor(fanOut);
// var order = "{\"orderId\":\"ORD-001\",\"amount\":150}";
// Console.WriteLine("Processing order through fan-out pipeline...");
// var results = executor.Execute("source", order);
// foreach (var r in results)
// {
//     Console.WriteLine($"  [{r.NodeType}] {r.NodeName}: {r.Output}");
// }

// TODO: Step 3 - Implement Fan-In pattern
// Console.WriteLine("\n--- Pattern 2: Fan-In ---");
// Process events from multiple sources into one output

// TODO: Step 4 - Implement Diamond pattern
// Console.WriteLine("\n--- Pattern 3: Diamond ---");
// Split, process in parallel, merge back

// TODO: Step 5 - Implement Conditional Branch pattern
// Console.WriteLine("\n--- Pattern 4: Conditional Branch ---");
// Route messages to different handlers based on priority

// TODO: Step 6 - Execute with Surgewave
// Use Surgewave producer/consumer to demonstrate real message flow

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Fan-out sends each message to all downstream processors");
Console.WriteLine("2. Fan-in merges multiple sources into a single stream");
Console.WriteLine("3. Diamond combines fan-out and fan-in for parallel enrichment");
Console.WriteLine("4. Conditional branching routes messages based on content");
Console.WriteLine("5. DAGs must be acyclic to prevent infinite loops");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============================================================================
// TODO: Add DAG types and pattern factories below
// ============================================================================

// public enum DagPatternType { FanOut, FanIn, Diamond, ConditionalBranch, ParallelProcessing }
// public enum DagNodeType { Source, Transform, Sink, Branch, Merge }

// public record DagPattern(string Name, DagPatternType Type, List<DagNode> Nodes, List<DagEdge> Edges);
// public abstract record DagNode(string Id, string Name, DagNodeType NodeType);
// public record SourceNode(string Id, string Name, string Topic) : DagNode(Id, Name, DagNodeType.Source);
// public record TransformNode(string Id, string Name, Func<string, string?> Transform) : DagNode(Id, Name, DagNodeType.Transform);
// public record SinkNode(string Id, string Name, string OutputTopic) : DagNode(Id, Name, DagNodeType.Sink);
// public record BranchNode(string Id, string Name, Func<string, string> Router) : DagNode(Id, Name, DagNodeType.Branch);
// public record MergeNode(string Id, string Name) : DagNode(Id, Name, DagNodeType.Merge);
// public record DagEdge(string From, string To, string? Label = null);

// public record ExecutionResult(string NodeId, string NodeName, string NodeType, string Output);

// public class DagExecutor { ... }