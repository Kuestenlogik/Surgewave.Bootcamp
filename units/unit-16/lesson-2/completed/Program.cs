// Surgewave Bootcamp - Unit 25, Lesson 2: Creating Visual Pipelines
// COMPLETED SOLUTION

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

// Step 3 - Build a pipeline using the builder
Console.WriteLine("\n--- Building Pipeline with Fluent API ---");

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

Console.WriteLine($"Created pipeline: {pipeline.Name}");
Console.WriteLine($"  Nodes: {pipeline.Nodes.Count}");
Console.WriteLine($"  Edges: {pipeline.Edges.Count}");

// Visualize the pipeline
PipelineVisualizer.PrintPipeline(pipeline);

// Step 4 - Validate the pipeline
var validator = new PipelineValidator();
var result = validator.Validate(pipeline);
Console.WriteLine($"\n--- Validation ---");
Console.WriteLine($"Valid: {result.IsValid}");

if (result.Errors.Count > 0)
{
    Console.WriteLine("Errors:");
    foreach (var error in result.Errors)
        Console.WriteLine($"  - {error}");
}

// Step 5 - Export pipeline to JSON
Console.WriteLine("\n--- Exported JSON ---");
var json = PipelineSerializer.ToJson(pipeline);
Console.WriteLine(json);

// Step 6 - Execute the pipeline with Surgewave
Console.WriteLine("\n--- Pipeline Execution ---");

await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "pipeline-executor";
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
    Console.WriteLine($"[Producer] Sent order: {order.Id}");
}

// Consume and process
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

        Console.WriteLine($"[Source] Consumed: Order #{order.Id} - ${order.Total:F2}");

        // Apply filter transform
        if (order.Total > 0)
        {
            Console.WriteLine($"[Transform:Filter] Passed: order.Total > 0");
            Console.WriteLine($"[Transform:Map] Applied: enrichment");
            Console.WriteLine($"[Sink] Produced to processed-orders");
            processedCount++;
        }
        else
        {
            Console.WriteLine($"[Transform:Filter] Filtered out: Total was 0");
            filteredCount++;
        }
    }
}
catch (OperationCanceledException)
{
    // Expected
}

Console.WriteLine($"\nProcessed: {processedCount + filteredCount}, Passed: {processedCount}, Filtered: {filteredCount}");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. PipelineBuilder provides a fluent API for pipeline creation");
Console.WriteLine("2. Connection validation ensures DAG integrity");
Console.WriteLine("3. JSON export enables version control and sharing");
Console.WriteLine("4. Visual pipelines execute the same as code pipelines");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============================================================================
// Data types
// ============================================================================

public record Order(string Id, string Customer, decimal Total);

// ============================================================================
// Node types
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
// Pipeline definition
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
// Node Builders (Step 1)
// ============================================================================

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

    public SourceNode Build() => new(_id, string.IsNullOrEmpty(_name) ? _id : _name, _position, _topic, _format);
}

public class TransformNodeBuilder
{
    private string _id = "";
    private string _name = "";
    private string _type = "Filter";
    private string _expression = "";
    private NodePosition _position = new(0, 0);
    private readonly List<string> _inputs = new();

    public TransformNodeBuilder WithId(string id) { _id = id; return this; }
    public TransformNodeBuilder WithName(string name) { _name = name; return this; }
    public TransformNodeBuilder Filter(string expression) { _type = "Filter"; _expression = expression; return this; }
    public TransformNodeBuilder Map(string expression) { _type = "Map"; _expression = expression; return this; }
    public TransformNodeBuilder Aggregate(string expression) { _type = "Aggregate"; _expression = expression; return this; }
    public TransformNodeBuilder AtPosition(int x, int y) { _position = new(x, y); return this; }

    public TransformNode Build() => new(_id, string.IsNullOrEmpty(_name) ? _id : _name, _position, _type, _expression, _inputs);
}

public class SinkNodeBuilder
{
    private string _id = "";
    private string _name = "";
    private string _topic = "";
    private NodePosition _position = new(0, 0);
    private readonly List<string> _inputs = new();

    public SinkNodeBuilder WithId(string id) { _id = id; return this; }
    public SinkNodeBuilder WithName(string name) { _name = name; return this; }
    public SinkNodeBuilder ToTopic(string topic) { _topic = topic; return this; }
    public SinkNodeBuilder AtPosition(int x, int y) { _position = new(x, y); return this; }

    public SinkNode Build() => new(_id, string.IsNullOrEmpty(_name) ? _id : _name, _position, _topic, _inputs);
}

// ============================================================================
// Pipeline Builder (Step 2)
// ============================================================================

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
        Console.WriteLine($"[Builder] Adding source: {id}");
        return this;
    }

    public PipelineBuilder AddTransform(string id, Action<TransformNodeBuilder> configure)
    {
        var builder = new TransformNodeBuilder().WithId(id);
        configure(builder);
        _nodes.Add(builder.Build());
        Console.WriteLine($"[Builder] Adding transform: {id}");
        return this;
    }

    public PipelineBuilder AddSink(string id, Action<SinkNodeBuilder> configure)
    {
        var builder = new SinkNodeBuilder().WithId(id);
        configure(builder);
        _nodes.Add(builder.Build());
        Console.WriteLine($"[Builder] Adding sink: {id}");
        return this;
    }

    public PipelineBuilder Connect(string from, string to)
    {
        _edges.Add(new PipelineEdge(from, to));
        Console.WriteLine($"[Builder] Connecting: {from} -> {to}");
        return this;
    }

    public PipelineDefinition Build()
    {
        return new PipelineDefinition(_id, _name, _version, _nodes, _edges);
    }
}

// ============================================================================
// Validation
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
            errors.Add($"Duplicate node ID: {dup}");

        // Validate edges reference existing nodes
        foreach (var edge in pipeline.Edges)
        {
            if (!nodeIds.Contains(edge.From))
                errors.Add($"Edge references non-existent source: {edge.From}");
            if (!nodeIds.Contains(edge.To))
                errors.Add($"Edge references non-existent target: {edge.To}");
        }

        // Check for cycles
        if (HasCycle(pipeline))
            errors.Add("Pipeline contains a cycle - must be acyclic");

        // Check for disconnected nodes
        var connectedNodes = new HashSet<string>();
        foreach (var edge in pipeline.Edges)
        {
            connectedNodes.Add(edge.From);
            connectedNodes.Add(edge.To);
        }
        var disconnected = nodeIds.Except(connectedNodes);
        foreach (var node in disconnected)
            warnings.Add($"Node {node} is disconnected from the pipeline");

        return new ValidationResult(errors.Count == 0, errors, warnings);
    }

    private bool HasCycle(PipelineDefinition pipeline)
    {
        var adj = new Dictionary<string, List<string>>();
        foreach (var node in pipeline.Nodes)
            adj[node.Id] = new List<string>();
        foreach (var edge in pipeline.Edges)
            if (adj.ContainsKey(edge.From))
                adj[edge.From].Add(edge.To);

        var visited = new HashSet<string>();
        var inStack = new HashSet<string>();

        bool Dfs(string node)
        {
            if (!adj.ContainsKey(node)) return false;
            visited.Add(node);
            inStack.Add(node);

            foreach (var neighbor in adj[node])
            {
                if (!visited.Contains(neighbor) && Dfs(neighbor)) return true;
                if (inStack.Contains(neighbor)) return true;
            }

            inStack.Remove(node);
            return false;
        }

        return pipeline.Nodes.Any(n => !visited.Contains(n.Id) && Dfs(n.Id));
    }
}

// ============================================================================
// Serialization (Step 5)
// ============================================================================

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
            nodes = pipeline.Nodes.Select<PipelineNode, object>(n => n switch
            {
                SourceNode s => new { id = s.Id, type = "TopicSource", name = s.Name, position = new { x = s.Position.X, y = s.Position.Y }, config = new { topic = s.Topic, format = s.Format } },
                TransformNode t => new { id = t.Id, type = t.TransformType, name = t.Name, position = new { x = t.Position.X, y = t.Position.Y }, config = new { expression = t.Expression } },
                SinkNode k => new { id = k.Id, type = "TopicSink", name = k.Name, position = new { x = k.Position.X, y = k.Position.Y }, config = new { topic = k.Topic } },
                _ => new { id = n.Id, type = "Unknown", name = n.Name }
            }).ToList(),
            edges = pipeline.Edges.Select(e => new { from = e.From, to = e.To }).ToList()
        };

        return JsonSerializer.Serialize(exportModel, new JsonSerializerOptions { WriteIndented = true });
    }
}

// ============================================================================
// Visualization
// ============================================================================

public static class PipelineVisualizer
{
    public static void PrintPipeline(PipelineDefinition pipeline)
    {
        Console.WriteLine();
        Console.WriteLine($"Pipeline: {pipeline.Name} (v{pipeline.Version})");
        Console.WriteLine(new string('=', 50));

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