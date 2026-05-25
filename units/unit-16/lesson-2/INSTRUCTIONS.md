# Lesson 25.2: Instructions

Follow these step-by-step instructions to create visual pipelines using a builder pattern.

## Step 1: Create Node Builder Classes

Create fluent builders for each node type:

```csharp
public class SourceNodeBuilder
{
    private string _id = "";
    private string _name = "";
    private string _topic = "";
    private string _format = "json";
    private NodePosition _position = new(0, 0);

    public SourceNodeBuilder WithId(string id) { _id = id; return this; }
    public SourceNodeBuilder WithName(string name) { _name = name; return this; }
    public SourceNodeBuilder FromTopic(string topic) { _topic = topic; return this; }
    public SourceNodeBuilder WithFormat(string format) { _format = format; return this; }
    public SourceNodeBuilder AtPosition(int x, int y) { _position = new(x, y); return this; }

    public SourceNode Build() => new(_id, _name, _position, _topic, _format);
}

public class TransformNodeBuilder
{
    private string _id = "";
    private string _name = "";
    private string _type = "Filter";
    private string _expression = "";
    private NodePosition _position = new(0, 0);
    private List<string> _inputs = new();

    public TransformNodeBuilder WithId(string id) { _id = id; return this; }
    public TransformNodeBuilder WithName(string name) { _name = name; return this; }
    public TransformNodeBuilder Filter(string expression) { _type = "Filter"; _expression = expression; return this; }
    public TransformNodeBuilder Map(string expression) { _type = "Map"; _expression = expression; return this; }
    public TransformNodeBuilder AtPosition(int x, int y) { _position = new(x, y); return this; }

    public TransformNode Build() => new(_id, _name, _position, _type, _expression, _inputs);
}

public class SinkNodeBuilder
{
    private string _id = "";
    private string _name = "";
    private string _topic = "";
    private NodePosition _position = new(0, 0);
    private List<string> _inputs = new();

    public SinkNodeBuilder WithId(string id) { _id = id; return this; }
    public SinkNodeBuilder WithName(string name) { _name = name; return this; }
    public SinkNodeBuilder ToTopic(string topic) { _topic = topic; return this; }
    public SinkNodeBuilder AtPosition(int x, int y) { _position = new(x, y); return this; }

    public SinkNode Build() => new(_id, _name, _position, _topic, _inputs);
}
```

## Step 2: Create the Pipeline Builder

Implement a fluent pipeline builder:

```csharp
public class PipelineBuilder
{
    private readonly string _id;
    private string _name = "";
    private string _version = "1.0.0";
    private readonly List<PipelineNode> _nodes = new();
    private readonly List<PipelineEdge> _edges = new();

    public PipelineBuilder(string id)
    {
        _id = id;
        _name = id;
    }

    public PipelineBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public PipelineBuilder WithVersion(string version)
    {
        _version = version;
        return this;
    }

    public PipelineBuilder AddSource(string id, Action<SourceNodeBuilder> configure)
    {
        var builder = new SourceNodeBuilder().WithId(id);
        configure(builder);
        _nodes.Add(builder.Build());
        return this;
    }

    public PipelineBuilder AddTransform(string id, Action<TransformNodeBuilder> configure)
    {
        var builder = new TransformNodeBuilder().WithId(id);
        configure(builder);
        _nodes.Add(builder.Build());
        return this;
    }

    public PipelineBuilder AddSink(string id, Action<SinkNodeBuilder> configure)
    {
        var builder = new SinkNodeBuilder().WithId(id);
        configure(builder);
        _nodes.Add(builder.Build());
        return this;
    }

    public PipelineBuilder Connect(string from, string to)
    {
        _edges.Add(new PipelineEdge(from, to));
        return this;
    }

    public PipelineDefinition Build()
    {
        return new PipelineDefinition(_id, _name, _version, _nodes, _edges);
    }
}
```

## Step 3: Build a Pipeline Using the Builder

Create a pipeline with the fluent API:

```csharp
var pipeline = new PipelineBuilder("order-processing")
    .WithName("Order Processing Pipeline")
    .WithVersion("1.0.0")
    .AddSource("source-orders", b => b
        .WithName("Raw Orders")
        .FromTopic("raw-orders")
        .WithFormat("json")
        .AtPosition(100, 100))
    .AddTransform("filter-valid", b => b
        .WithName("Filter Valid")
        .Filter("order.Total > 0")
        .AtPosition(300, 100))
    .AddTransform("enrich-order", b => b
        .WithName("Enrich Order")
        .Map("{ ...order, processed: true }")
        .AtPosition(500, 100))
    .AddSink("sink-processed", b => b
        .WithName("Processed Orders")
        .ToTopic("processed-orders")
        .AtPosition(700, 100))
    .Connect("source-orders", "filter-valid")
    .Connect("filter-valid", "enrich-order")
    .Connect("enrich-order", "sink-processed")
    .Build();
```

## Step 4: Implement Connection Validation

Validate that connections follow the rules:

```csharp
public class ConnectionValidator
{
    public bool CanConnect(PipelineNode from, PipelineNode to, PipelineDefinition pipeline)
    {
        // Rule 1: Sinks cannot have outgoing connections
        if (from is SinkNode)
        {
            Console.WriteLine($"Error: Sink node '{from.Id}' cannot have outgoing connections");
            return false;
        }

        // Rule 2: Sources cannot have incoming connections
        if (to is SourceNode)
        {
            Console.WriteLine($"Error: Source node '{to.Id}' cannot have incoming connections");
            return false;
        }

        // Rule 3: No self-loops
        if (from.Id == to.Id)
        {
            Console.WriteLine($"Error: Node '{from.Id}' cannot connect to itself");
            return false;
        }

        // Rule 4: No duplicate edges
        if (pipeline.Edges.Any(e => e.From == from.Id && e.To == to.Id))
        {
            Console.WriteLine($"Error: Edge from '{from.Id}' to '{to.Id}' already exists");
            return false;
        }

        // Rule 5: Check for cycles (would create)
        var tempEdges = pipeline.Edges.Append(new PipelineEdge(from.Id, to.Id)).ToList();
        var tempPipeline = pipeline with { Edges = tempEdges };
        if (HasCycle(tempPipeline))
        {
            Console.WriteLine($"Error: Connection would create a cycle");
            return false;
        }

        return true;
    }

    private bool HasCycle(PipelineDefinition pipeline) { /* same as before */ }
}
```

## Step 5: Export Pipeline to JSON

Serialize the pipeline definition:

```csharp
public static class PipelineSerializer
{
    public static string ToJson(PipelineDefinition pipeline)
    {
        var exportModel = new
        {
            id = pipeline.Id,
            name = pipeline.Name,
            version = pipeline.Version,
            metadata = new
            {
                created = DateTime.UtcNow.ToString("o"),
                nodeCount = pipeline.Nodes.Count,
                edgeCount = pipeline.Edges.Count
            },
            nodes = pipeline.Nodes.Select(n => n switch
            {
                SourceNode s => new { id = s.Id, type = "TopicSource", name = s.Name, position = new { x = s.Position.X, y = s.Position.Y }, config = new { topic = s.Topic, format = s.Format } },
                TransformNode t => new { id = t.Id, type = t.TransformType, name = t.Name, position = new { x = t.Position.X, y = t.Position.Y }, config = new { expression = t.Expression } },
                SinkNode k => new { id = k.Id, type = "TopicSink", name = k.Name, position = new { x = k.Position.X, y = k.Position.Y }, config = new { topic = k.Topic } },
                _ => throw new NotSupportedException()
            }),
            edges = pipeline.Edges.Select(e => new { from = e.From, to = e.To })
        };

        return JsonSerializer.Serialize(exportModel, new JsonSerializerOptions { WriteIndented = true });
    }
}
```

## Step 6: Execute the Pipeline with Surgewave

Run the visual pipeline using Surgewave:

```csharp
// Execute the pipeline
var executor = new PipelineExecutor(bootstrapServers);
await executor.ExecuteAsync(pipeline);
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- PipelineBuilder with fluent API
- Node builders for each type
- Connection validation
- JSON export/import
- Pipeline execution with Surgewave

## Sample Output

```
=== Surgewave Bootcamp: Lesson 25.2 - Creating Visual Pipelines ===

Surgewave broker started on port 12345

--- Building Pipeline with Fluent API ---
[Builder] Adding source: source-orders
[Builder] Adding transform: filter-valid
[Builder] Adding transform: enrich-order
[Builder] Adding sink: sink-processed
[Builder] Connecting: source-orders -> filter-valid
[Builder] Connecting: filter-valid -> enrich-order
[Builder] Connecting: enrich-order -> sink-processed

--- Pipeline Visualization ---

Pipeline: Order Processing Pipeline (v1.0.0)
==================================================

  [S] Raw Orders (source-orders)
     |
     v
  [T] Filter Valid (filter-valid)
     |
     v
  [T] Enrich Order (enrich-order)
     |
     v
  [K] Processed Orders (sink-processed)

==================================================

--- Validation ---
Valid: True

--- Exported JSON ---
{
  "id": "order-processing",
  "name": "Order Processing Pipeline",
  "version": "1.0.0",
  ...
}

--- Pipeline Execution ---
[Executor] Starting pipeline: order-processing
[Source] Consumed: Order #001 - $150.00
[Transform:Filter] Passed: order.Total > 0
[Transform:Map] Applied: enrichment
[Sink] Produced to processed-orders

Lesson complete!
```

## Knowledge Check

1. What is the purpose of the PipelineBuilder pattern?
2. Why are connection validation rules important?
3. What happens if you try to connect a sink to another node?
4. How does JSON serialization enable pipeline sharing?
5. What's the difference between node position and node connections?

---

**Next:** [Lesson 25.3: Connector Nodes and Configuration](../lesson-3/README.md)
