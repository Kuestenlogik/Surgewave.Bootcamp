# Lesson 25.1: Instructions

Follow these step-by-step instructions to understand and implement Visual Pipeline concepts.

## Step 1: Define the Node Types

Create the base classes for pipeline nodes:

```csharp
// Base class for all pipeline nodes
public abstract record PipelineNode(
    string Id,
    string Name,
    NodePosition Position
);

// Position on the canvas
public record NodePosition(int X, int Y);

// Node types
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
```

## Step 2: Create the Pipeline Definition

Define the pipeline structure with nodes and edges:

```csharp
public record PipelineDefinition(
    string Id,
    string Name,
    string Version,
    List<PipelineNode> Nodes,
    List<PipelineEdge> Edges
);

public record PipelineEdge(string From, string To);
```

## Step 3: Build a Simple Pipeline

Create a simple source -> transform -> sink pipeline:

```csharp
var pipeline = new PipelineDefinition(
    Id: "order-processing-v1",
    Name: "Order Processing Pipeline",
    Version: "1.0.0",
    Nodes: new List<PipelineNode>
    {
        new SourceNode(
            Id: "source-orders",
            Name: "Order Events",
            Position: new NodePosition(100, 100),
            Topic: "raw-orders",
            Format: "json"
        ),
        new TransformNode(
            Id: "filter-valid",
            Name: "Filter Valid Orders",
            Position: new NodePosition(300, 100),
            TransformType: "Filter",
            Expression: "order.Total > 0",
            Inputs: new List<string> { "source-orders" }
        ),
        new SinkNode(
            Id: "sink-validated",
            Name: "Validated Orders",
            Position: new NodePosition(500, 100),
            Topic: "validated-orders",
            Inputs: new List<string> { "filter-valid" }
        )
    },
    Edges: new List<PipelineEdge>
    {
        new PipelineEdge("source-orders", "filter-valid"),
        new PipelineEdge("filter-valid", "sink-validated")
    }
);
```

## Step 4: Implement Pipeline Validation

Create a validator to check pipeline integrity:

```csharp
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

        return new ValidationResult(errors.Count == 0, errors, warnings);
    }

    private bool HasCycle(PipelineDefinition pipeline)
    {
        // Build adjacency list
        var adj = new Dictionary<string, List<string>>();
        foreach (var node in pipeline.Nodes)
            adj[node.Id] = new List<string>();
        foreach (var edge in pipeline.Edges)
            adj[edge.From].Add(edge.To);

        // DFS for cycle detection
        var visited = new HashSet<string>();
        var inStack = new HashSet<string>();

        bool Dfs(string node)
        {
            visited.Add(node);
            inStack.Add(node);
            foreach (var neighbor in adj[node])
            {
                if (!visited.Contains(neighbor) && Dfs(neighbor))
                    return true;
                if (inStack.Contains(neighbor))
                    return true;
            }
            inStack.Remove(node);
            return false;
        }

        return pipeline.Nodes.Any(n => !visited.Contains(n.Id) && Dfs(n.Id));
    }
}

public record ValidationResult(
    bool IsValid,
    List<string> Errors,
    List<string> Warnings
);
```

## Step 5: Visualize the Pipeline (ASCII Art)

Create a simple text-based visualization:

```csharp
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
            var nextLayer = layers.SkipWhile(l => l != layer).Skip(1).FirstOrDefault();
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
```

## Step 6: Simulate Pipeline Execution

Wire up the visual pipeline to Surgewave:

```csharp
// Create Surgewave runtime
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;

// Execute pipeline by interpreting nodes
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
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Pipeline definition with nodes and edges
- Pipeline validation
- ASCII visualization
- Integration with Surgewave runtime

## Sample Output

```
=== Surgewave Bootcamp: Lesson 25.1 - Pipeline UI Overview ===

Surgewave broker started on port 12345

--- Pipeline Definition ---

Pipeline: Order Processing Pipeline (v1.0.0)
==================================================

  [S] Order Events (source-orders)
     |
     v

  [T] Filter Valid Orders (filter-valid)
     |
     v

  [K] Validated Orders (sink-validated)

==================================================

--- Validation Result ---
Valid: True
Errors: 0
Warnings: 0

--- Pipeline Execution Simulation ---
[Pipeline] Creating consumer for topic: raw-orders
[Pipeline] Setting up transform: Filter
           Expression: order.Total > 0
[Pipeline] Creating producer for topic: validated-orders

--- Simulating Data Flow ---
[Source] Received: Order #001 - $150.00
[Transform] Passed filter: order.Total > 0
[Sink] Written to validated-orders

[Source] Received: Order #002 - $0.00
[Transform] Filtered out: order.Total > 0 (value was 0)

[Source] Received: Order #003 - $75.50
[Transform] Passed filter: order.Total > 0
[Sink] Written to validated-orders

Processed: 3 messages, Passed: 2, Filtered: 1

Lesson complete!
```

## Knowledge Check

1. What are the three main categories of nodes in a visual pipeline?
2. When would you choose a visual pipeline over a code-based pipeline?
3. What does DAG stand for, and why must pipelines be acyclic?
4. What validation checks should be performed on a pipeline definition?
5. How does the visual pipeline UI compile to Surgewave runtime execution?

---

**Next:** [Lesson 25.2: Creating Visual Pipelines](../lesson-2/README.md)
