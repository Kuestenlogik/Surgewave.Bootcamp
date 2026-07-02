// Surgewave Bootcamp - Unit 25, Lesson 1: Pipeline UI Overview
// COMPLETED SOLUTION

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

// Step 3 - Build a simple pipeline
Console.WriteLine("\n--- Pipeline Definition ---");

var pipeline = new PipelineDefinition(
    Id: "order-processing-v1",
    Name: "Order Processing Pipeline",
    Version: "1.0.0",
    Nodes: new List<PipelineNode>
    {
        new SourceNode("source-orders", "Order Events", new NodePosition(100, 100), "raw-orders", "json"),
        new TransformNode("filter-valid", "Filter Valid Orders", new NodePosition(300, 100), "Filter", "order.Total > 0", new List<string> { "source-orders" }),
        new SinkNode("sink-validated", "Validated Orders", new NodePosition(500, 100), "validated-orders", new List<string> { "filter-valid" })
    },
    Edges: new List<PipelineEdge>
    {
        new PipelineEdge("source-orders", "filter-valid"),
        new PipelineEdge("filter-valid", "sink-validated")
    }
);

// Step 5 - Visualize the pipeline
PipelineVisualizer.PrintPipeline(pipeline);

// Step 4 - Validate the pipeline
var validator = new PipelineValidator();
var result = validator.Validate(pipeline);
Console.WriteLine($"\n--- Validation Result ---");
Console.WriteLine($"Valid: {result.IsValid}");
Console.WriteLine($"Errors: {result.Errors.Count}");
Console.WriteLine($"Warnings: {result.Warnings.Count}");

if (result.Errors.Count > 0)
{
    Console.WriteLine("Errors:");
    foreach (var error in result.Errors)
        Console.WriteLine($"  - {error}");
}

if (result.Warnings.Count > 0)
{
    Console.WriteLine("Warnings:");
    foreach (var warning in result.Warnings)
        Console.WriteLine($"  - {warning}");
}

// Step 6 - Simulate pipeline execution
Console.WriteLine("\n--- Pipeline Execution Simulation ---");

foreach (var node in pipeline.Nodes.OfType<SourceNode>())
{
    Console.WriteLine($"[Pipeline] Creating consumer for topic: {node.Topic}");
}

foreach (var node in pipeline.Nodes.OfType<TransformNode>())
{
    Console.WriteLine($"[Pipeline] Setting up transform: {node.TransformType}");
    Console.WriteLine($"           Expression: {node.Expression}");
}

foreach (var node in pipeline.Nodes.OfType<SinkNode>())
{
    Console.WriteLine($"[Pipeline] Creating producer for topic: {node.Topic}");
}

// Simulate actual data flow with Surgewave
Console.WriteLine("\n--- Simulating Data Flow ---");

await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "pipeline-simulator";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

// Produce test orders
var orders = new[]
{
    new Order("001", "Alice", 150.00m),
    new Order("002", "Bob", 0.00m),      // Will be filtered
    new Order("003", "Carol", 75.50m)
};

foreach (var order in orders)
{
    await producer.ProduceAsync("raw-orders", order.Id, JsonSerializer.Serialize(order));
}

// Consume and process (simulating the visual pipeline)
consumer.Subscribe("raw-orders");
var processedCount = 0;
var filteredCount = 0;

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

try
{
    while (!cts.Token.IsCancellationRequested && processedCount + filteredCount < orders.Length)
    {
        var record = await consumer.ConsumeAsync(cts.Token);
        if (record?.Value is null) continue;

        var order = JsonSerializer.Deserialize<Order>(record.Value);
        if (order is null) continue;

        Console.WriteLine($"[Source] Received: Order #{order.Id} - ${order.Total:F2}");

        // Apply filter transform
        if (order.Total > 0)
        {
            Console.WriteLine($"[Transform] Passed filter: order.Total > 0");
            Console.WriteLine($"[Sink] Written to validated-orders");
            processedCount++;
        }
        else
        {
            Console.WriteLine($"[Transform] Filtered out: order.Total > 0 (value was {order.Total})");
            filteredCount++;
        }

        Console.WriteLine();
    }
}
catch (OperationCanceledException)
{
    // Expected
}

Console.WriteLine($"Processed: {processedCount + filteredCount} messages, Passed: {processedCount}, Filtered: {filteredCount}");

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
// Data types
// ============================================================================

public record Order(string Id, string Customer, decimal Total);

// ============================================================================
// Node types (Step 1)
// ============================================================================

public abstract record PipelineNode(string Id, string Name, NodePosition Position);

public record NodePosition(int X, int Y);

public record SourceNode(
    string Id,
    string Name,
    NodePosition Position,
    string Topic,
    string Format
) : PipelineNode(Id, Name, Position);

public record TransformNode(
    string Id,
    string Name,
    NodePosition Position,
    string TransformType,
    string Expression,
    List<string> Inputs
) : PipelineNode(Id, Name, Position);

public record SinkNode(
    string Id,
    string Name,
    NodePosition Position,
    string Topic,
    List<string> Inputs
) : PipelineNode(Id, Name, Position);

// ============================================================================
// Pipeline definition (Step 2)
// ============================================================================

public record PipelineDefinition(
    string Id,
    string Name,
    string Version,
    List<PipelineNode> Nodes,
    List<PipelineEdge> Edges
);

public record PipelineEdge(string From, string To);

// ============================================================================
// Validation (Step 4)
// ============================================================================

public record ValidationResult(
    bool IsValid,
    List<string> Errors,
    List<string> Warnings
);

public class PipelineValidator
{
    public ValidationResult Validate(PipelineDefinition pipeline)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var nodeIds = pipeline.Nodes.Select(n => n.Id).ToHashSet();

        // Check for duplicate node IDs
        var duplicates = pipeline.Nodes
            .GroupBy(n => n.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var dup in duplicates)
        {
            errors.Add($"Duplicate node ID: {dup}");
        }

        // Validate edges reference existing nodes
        foreach (var edge in pipeline.Edges)
        {
            if (!nodeIds.Contains(edge.From))
                errors.Add($"Edge references non-existent source: {edge.From}");
            if (!nodeIds.Contains(edge.To))
                errors.Add($"Edge references non-existent target: {edge.To}");
        }

        // Check for cycles (DAG validation)
        if (HasCycle(pipeline))
        {
            errors.Add("Pipeline contains a cycle - must be acyclic");
        }

        // Check for disconnected nodes
        var connectedNodes = new HashSet<string>();
        foreach (var edge in pipeline.Edges)
        {
            connectedNodes.Add(edge.From);
            connectedNodes.Add(edge.To);
        }
        var disconnected = nodeIds.Except(connectedNodes);
        foreach (var node in disconnected)
        {
            warnings.Add($"Node {node} is disconnected from the pipeline");
        }

        // Check for source nodes
        var nodesWithIncoming = pipeline.Edges.Select(e => e.To).ToHashSet();
        var sourceNodes = pipeline.Nodes.Where(n => !nodesWithIncoming.Contains(n.Id)).ToList();
        if (sourceNodes.Count == 0)
        {
            errors.Add("Pipeline has no source nodes");
        }

        // Check for sink nodes
        var nodesWithOutgoing = pipeline.Edges.Select(e => e.From).ToHashSet();
        var sinkNodes = pipeline.Nodes.Where(n => !nodesWithOutgoing.Contains(n.Id)).ToList();
        if (sinkNodes.Count == 0)
        {
            errors.Add("Pipeline has no sink nodes");
        }

        return new ValidationResult(errors.Count == 0, errors, warnings);
    }

    private bool HasCycle(PipelineDefinition pipeline)
    {
        // Build adjacency list
        var adj = new Dictionary<string, List<string>>();
        foreach (var node in pipeline.Nodes)
            adj[node.Id] = new List<string>();
        foreach (var edge in pipeline.Edges)
        {
            if (adj.ContainsKey(edge.From))
                adj[edge.From].Add(edge.To);
        }

        // DFS for cycle detection
        var visited = new HashSet<string>();
        var inStack = new HashSet<string>();

        bool Dfs(string node)
        {
            if (!adj.ContainsKey(node)) return false;

            visited.Add(node);
            inStack.Add(node);

            foreach (var neighbor in adj[node])
            {
                if (!visited.Contains(neighbor))
                {
                    if (Dfs(neighbor)) return true;
                }
                else if (inStack.Contains(neighbor))
                {
                    return true;
                }
            }

            inStack.Remove(node);
            return false;
        }

        return pipeline.Nodes.Any(n => !visited.Contains(n.Id) && Dfs(n.Id));
    }
}

// ============================================================================
// Visualization (Step 5)
// ============================================================================

public static class PipelineVisualizer
{
    public static void PrintPipeline(PipelineDefinition pipeline)
    {
        Console.WriteLine();
        Console.WriteLine($"Pipeline: {pipeline.Name} (v{pipeline.Version})");
        Console.WriteLine(new string('=', 50));

        // Group nodes by X position for layered display
        var layers = pipeline.Nodes
            .OrderBy(n => n.Position.X)
            .GroupBy(n => n.Position.X / 200)
            .ToList();

        foreach (var layer in layers)
        {
            Console.WriteLine();
            foreach (var node in layer)
            {
                var icon = node switch
                {
                    SourceNode => "[S]",
                    TransformNode => "[T]",
                    SinkNode => "[K]",
                    _ => "[?]"
                };
                Console.WriteLine($"  {icon} {node.Name} ({node.Id})");
            }

            // Print edges to next layer
            var nextLayer = layers.SkipWhile(l => l.Key != layer.Key).Skip(1).FirstOrDefault();
            if (nextLayer != null)
            {
                Console.WriteLine("     |");
                Console.WriteLine("     v");
            }
        }

        Console.WriteLine();
        Console.WriteLine(new string('=', 50));
    }
}