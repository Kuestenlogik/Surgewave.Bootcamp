// Surgewave Bootcamp - Unit 25, Lesson 3: Connector Nodes and Configuration
//
// In this lesson, you'll learn:
// - Configure source connectors (Topic, File, HTTP, Timer)
// - Configure transform nodes (Filter, Map, FlatMap, Aggregate)
// - Configure sink connectors (Topic, File, HTTP, Console)
// - Implement configuration validation
// - Create reusable connector templates
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 25.3 - Connector Nodes and Configuration ===");
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

// TODO: Step 1 - Define detailed configuration classes
// public record TopicSourceConfig(string Topic, string Format = "json", ...);
// public record FilterConfig(string Expression, bool DropOnError = true);
// public record TopicSinkConfig(string Topic, string Format = "json", ...);

// TODO: Step 2 - Create enhanced node classes with typed configs
// public record EnhancedSourceNode(...);
// public record EnhancedTransformNode(...);
// public record EnhancedSinkNode(...);

// TODO: Step 3 - Implement configuration validators
// public static class ConfigValidator { ... }

// TODO: Step 4 - Build an enhanced pipeline with validated configs
// Console.WriteLine("\n--- Creating Configured Pipeline ---");
// var sourceConfig = new TopicSourceConfig("order-events", "json", "earliest");
// var filterConfig = new FilterConfig("order.total > 0 && order.status != 'cancelled'");
// var sinkConfig = new TopicSinkConfig("processed-orders", "json", "order.id", "byKey");
//
// // Validate configurations
// Console.WriteLine("\n--- Validation ---");
// var sourceErrors = ConfigValidator.ValidateTopicSourceConfig(sourceConfig);
// Console.WriteLine($"TopicSourceConfig: {(sourceErrors.Count == 0 ? "Valid" : string.Join(", ", sourceErrors))}");

// TODO: Step 5 - Create and use connector templates
// Console.WriteLine("\n--- Using Template ---");
// var templatePipeline = ConnectorTemplates.OrderProcessingPipeline();
// Console.WriteLine($"Created pipeline from template: {templatePipeline.Name}");

// TODO: Step 6 - Execute the pipeline with Surgewave
// Test the configured pipeline with sample data

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Connectors have typed configurations for safety");
Console.WriteLine("2. Configuration validation catches errors early");
Console.WriteLine("3. Templates enable reusable, consistent configurations");
Console.WriteLine("4. Different connector types serve different use cases");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============================================================================
// Base types (from previous lessons)
// ============================================================================

public abstract record PipelineNode(string Id, string Name, NodePosition Position);
public record NodePosition(int X, int Y);
public record PipelineDefinition(string Id, string Name, string Version, List<PipelineNode> Nodes, List<PipelineEdge> Edges);
public record PipelineEdge(string From, string To);

// ============================================================================
// TODO: Add configuration classes
// ============================================================================

// Source configurations
// public record TopicSourceConfig(string Topic, string Format = "json", string StartOffset = "earliest", string? ConsumerGroup = null);

// Transform configurations
// public record FilterConfig(string Expression, bool DropOnError = true);
// public record MapConfig(string Expression, string OutputFormat = "json");

// Sink configurations
// public record TopicSinkConfig(string Topic, string Format = "json", string? KeyExpression = null, string Partitioner = "byKey");

// ============================================================================
// TODO: Add enhanced node types
// ============================================================================

// public enum SourceType { Topic, File, Http, Timer }
// public enum TransformType { Filter, Map, FlatMap, Aggregate, Join }
// public enum SinkType { Topic, File, Http, Console }

// public record EnhancedSourceNode(string Id, string Name, NodePosition Position, SourceType Type, object Config) : PipelineNode(Id, Name, Position);
// public record EnhancedTransformNode(string Id, string Name, NodePosition Position, TransformType Type, object Config, List<string> Inputs) : PipelineNode(Id, Name, Position);
// public record EnhancedSinkNode(string Id, string Name, NodePosition Position, SinkType Type, object Config, List<string> Inputs) : PipelineNode(Id, Name, Position);

// ============================================================================
// TODO: Add configuration validator
// ============================================================================

// public static class ConfigValidator { ... }

// ============================================================================
// TODO: Add connector templates
// ============================================================================

// public static class ConnectorTemplates { ... }