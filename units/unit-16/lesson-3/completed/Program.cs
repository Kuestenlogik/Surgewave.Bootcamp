// Surgewave Bootcamp - Unit 25, Lesson 3: Connector Nodes and Configuration
// COMPLETED SOLUTION

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

// Step 4 - Build a pipeline with validated configs
Console.WriteLine("\n--- Creating Configured Pipeline ---");

var sourceConfig = new TopicSourceConfig("order-events", "json", "earliest");
var filterConfig = new FilterConfig("order.total > 0 && order.status != 'cancelled'");
var sinkConfig = new TopicSinkConfig("processed-orders", "json", "order.id", "byKey");

Console.WriteLine($"[Config] TopicSource: {sourceConfig.Topic} ({sourceConfig.Format}, {sourceConfig.StartOffset})");
Console.WriteLine($"[Config] Filter: {filterConfig.Expression}");
Console.WriteLine($"[Config] TopicSink: {sinkConfig.Topic} ({sinkConfig.Format}, {sinkConfig.Partitioner})");

// Validate configurations
Console.WriteLine("\n--- Validation ---");

var sourceErrors = ConfigValidator.ValidateTopicSourceConfig(sourceConfig);
Console.WriteLine($"TopicSourceConfig: {(sourceErrors.Count == 0 ? "Valid" : string.Join(", ", sourceErrors))}");

var filterErrors = ConfigValidator.ValidateFilterConfig(filterConfig);
Console.WriteLine($"FilterConfig: {(filterErrors.Count == 0 ? "Valid" : string.Join(", ", filterErrors))}");

var sinkErrors = ConfigValidator.ValidateTopicSinkConfig(sinkConfig);
Console.WriteLine($"TopicSinkConfig: {(sinkErrors.Count == 0 ? "Valid" : string.Join(", ", sinkErrors))}");

// Build the pipeline with enhanced nodes
var pipeline = new EnhancedPipelineBuilder("order-processing")
    .WithName("Order Processing Pipeline")
    .AddTopicSource("source", "Order Events", sourceConfig, new NodePosition(100, 100))
    .AddFilter("filter", "Valid Orders Filter", filterConfig, new NodePosition(300, 100))
    .AddTopicSink("sink", "Processed Orders", sinkConfig, new NodePosition(500, 100))
    .Connect("source", "filter")
    .Connect("filter", "sink")
    .Build();

Console.WriteLine($"Pipeline: {(pipeline.Nodes.Count > 0 ? "Valid" : "Invalid")}");

// Step 5 - Use connector templates
Console.WriteLine("\n--- Using Template ---");
var templatePipeline = ConnectorTemplates.OrderProcessingPipeline();
Console.WriteLine($"Created pipeline from template: {templatePipeline.Name}");
Console.WriteLine($"  Nodes: {templatePipeline.Nodes.Count}");
Console.WriteLine($"  Edges: {templatePipeline.Edges.Count}");

// Step 6 - Execute the pipeline
Console.WriteLine("\n--- Executing Pipeline ---");

await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = sourceConfig.ConsumerGroup ?? "pipeline-executor";
    opt.AutoOffsetReset = sourceConfig.StartOffset == "earliest" ? AutoOffsetReset.Earliest : AutoOffsetReset.Latest;
});

// Produce test orders
var orders = new[]
{
    new Order("001", 150.00m, "pending"),
    new Order("002", 0.00m, "pending"),       // Will be filtered (total = 0)
    new Order("003", 75.50m, "cancelled"),    // Will be filtered (cancelled)
    new Order("004", 200.00m, "pending")
};

foreach (var order in orders)
{
    await producer.ProduceAsync(sourceConfig.Topic, order.Id, JsonSerializer.Serialize(order));
}

// Process with configured pipeline
consumer.Subscribe(sourceConfig.Topic);
var received = 0;
var passed = 0;
var dropped = 0;

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

try
{
    while (!cts.Token.IsCancellationRequested && received < orders.Length)
    {
        var record = await consumer.ConsumeAsync(cts.Token);
        if (record?.Value is null) continue;

        var order = JsonSerializer.Deserialize<Order>(record.Value);
        if (order is null) continue;

        received++;
        Console.WriteLine($"[Source:{sourceConfig.Topic}] Received: {record.Value}");

        // Apply filter
        var passesFilter = order.Total > 0 && order.Status != "cancelled";

        if (passesFilter)
        {
            Console.WriteLine($"[Filter] Passed: total > 0, status != cancelled");
            Console.WriteLine($"[Sink:{sinkConfig.Topic}] Produced with key: {order.Id}");
            passed++;
        }
        else
        {
            var reason = order.Total <= 0 ? "total was 0" : "status was cancelled";
            Console.WriteLine($"[Filter] Dropped: {reason}");
            dropped++;
        }

        Console.WriteLine();
    }
}
catch (OperationCanceledException)
{
    // Expected
}

Console.WriteLine($"Statistics:");
Console.WriteLine($"  Received: {received}");
Console.WriteLine($"  Passed filter: {passed}");
Console.WriteLine($"  Dropped: {dropped}");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Connectors have typed configurations for safety");
Console.WriteLine("2. Configuration validation catches errors early");
Console.WriteLine("3. Templates enable reusable, consistent configurations");
Console.WriteLine("4. Different connector types serve different use cases");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============================================================================
// Data types
// ============================================================================

public record Order(string Id, decimal Total, string Status);

// ============================================================================
// Base types
// ============================================================================

public abstract record PipelineNode(string Id, string Name, NodePosition Position);
public record NodePosition(int X, int Y);
public record PipelineDefinition(string Id, string Name, string Version, List<PipelineNode> Nodes, List<PipelineEdge> Edges);
public record PipelineEdge(string From, string To);

// ============================================================================
// Configuration classes (Step 1)
// ============================================================================

// Source configurations
public record TopicSourceConfig(
    string Topic,
    string Format = "json",
    string StartOffset = "earliest",
    string? ConsumerGroup = null
);

public record FileSourceConfig(
    string Path,
    string Format = "json",
    TimeSpan PollInterval = default,
    string? ProcessedDir = null
);

public record HttpSourceConfig(
    string Endpoint,
    int Port = 8080,
    string Authentication = "none"
);

public record TimerSourceConfig(
    TimeSpan Interval,
    string Payload = "{}"
);

// Transform configurations
public record FilterConfig(
    string Expression,
    bool DropOnError = true
);

public record MapConfig(
    string Expression,
    string OutputFormat = "json"
);

public record AggregateConfig(
    string GroupBy,
    string Aggregation,
    string WindowType = "tumbling",
    TimeSpan WindowSize = default
);

// Sink configurations
public record TopicSinkConfig(
    string Topic,
    string Format = "json",
    string? KeyExpression = null,
    string Partitioner = "byKey"
);

public record FileSinkConfig(
    string Path,
    string Format = "json",
    string FileNaming = "date",
    string? RolloverSize = null,
    TimeSpan? RolloverInterval = null
);

public record HttpSinkConfig(
    string Url,
    string Method = "POST",
    Dictionary<string, string>? Headers = null,
    int BatchSize = 1,
    int MaxRetries = 3
);

// ============================================================================
// Enhanced node types (Step 2)
// ============================================================================

public enum SourceType { Topic, File, Http, Timer }
public enum TransformType { Filter, Map, FlatMap, Aggregate, Join }
public enum SinkType { Topic, File, Http, Console }

public record EnhancedSourceNode(
    string Id,
    string Name,
    NodePosition Position,
    SourceType Type,
    object Config
) : PipelineNode(Id, Name, Position);

public record EnhancedTransformNode(
    string Id,
    string Name,
    NodePosition Position,
    TransformType Type,
    object Config,
    List<string> Inputs
) : PipelineNode(Id, Name, Position);

public record EnhancedSinkNode(
    string Id,
    string Name,
    NodePosition Position,
    SinkType Type,
    object Config,
    List<string> Inputs
) : PipelineNode(Id, Name, Position);

// ============================================================================
// Configuration validator (Step 3)
// ============================================================================

public static class ConfigValidator
{
    public static List<string> ValidateTopicSourceConfig(TopicSourceConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.Topic))
            errors.Add("Topic is required");

        if (!new[] { "json", "avro", "protobuf", "raw" }.Contains(config.Format.ToLower()))
            errors.Add($"Invalid format: {config.Format}");

        if (!new[] { "earliest", "latest", "timestamp" }.Contains(config.StartOffset.ToLower()))
            errors.Add($"Invalid startOffset: {config.StartOffset}");

        return errors;
    }

    public static List<string> ValidateFilterConfig(FilterConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.Expression))
            errors.Add("Filter expression is required");

        return errors;
    }

    public static List<string> ValidateMapConfig(MapConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.Expression))
            errors.Add("Map expression is required");

        if (!new[] { "json", "avro" }.Contains(config.OutputFormat.ToLower()))
            errors.Add($"Invalid output format: {config.OutputFormat}");

        return errors;
    }

    public static List<string> ValidateTopicSinkConfig(TopicSinkConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.Topic))
            errors.Add("Topic is required");

        if (!new[] { "json", "avro" }.Contains(config.Format.ToLower()))
            errors.Add($"Invalid format: {config.Format}");

        if (!new[] { "roundRobin", "byKey", "custom" }.Contains(config.Partitioner))
            errors.Add($"Invalid partitioner: {config.Partitioner}");

        return errors;
    }
}

// ============================================================================
// Enhanced pipeline builder (Step 4)
// ============================================================================

public class EnhancedPipelineBuilder
{
    private readonly string _id;
    private string _name = "";
    private readonly List<PipelineNode> _nodes = new();
    private readonly List<PipelineEdge> _edges = new();

    public EnhancedPipelineBuilder(string id)
    {
        _id = id;
        _name = id;
    }

    public EnhancedPipelineBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public EnhancedPipelineBuilder AddTopicSource(string id, string name, TopicSourceConfig config, NodePosition position)
    {
        var errors = ConfigValidator.ValidateTopicSourceConfig(config);
        if (errors.Count > 0)
            throw new ArgumentException($"Invalid config for {id}: {string.Join(", ", errors)}");

        _nodes.Add(new EnhancedSourceNode(id, name, position, SourceType.Topic, config));
        return this;
    }

    public EnhancedPipelineBuilder AddFilter(string id, string name, FilterConfig config, NodePosition position)
    {
        var errors = ConfigValidator.ValidateFilterConfig(config);
        if (errors.Count > 0)
            throw new ArgumentException($"Invalid config for {id}: {string.Join(", ", errors)}");

        _nodes.Add(new EnhancedTransformNode(id, name, position, TransformType.Filter, config, new List<string>()));
        return this;
    }

    public EnhancedPipelineBuilder AddMap(string id, string name, MapConfig config, NodePosition position)
    {
        var errors = ConfigValidator.ValidateMapConfig(config);
        if (errors.Count > 0)
            throw new ArgumentException($"Invalid config for {id}: {string.Join(", ", errors)}");

        _nodes.Add(new EnhancedTransformNode(id, name, position, TransformType.Map, config, new List<string>()));
        return this;
    }

    public EnhancedPipelineBuilder AddTopicSink(string id, string name, TopicSinkConfig config, NodePosition position)
    {
        var errors = ConfigValidator.ValidateTopicSinkConfig(config);
        if (errors.Count > 0)
            throw new ArgumentException($"Invalid config for {id}: {string.Join(", ", errors)}");

        _nodes.Add(new EnhancedSinkNode(id, name, position, SinkType.Topic, config, new List<string>()));
        return this;
    }

    public EnhancedPipelineBuilder Connect(string from, string to)
    {
        _edges.Add(new PipelineEdge(from, to));
        return this;
    }

    public PipelineDefinition Build() => new(_id, _name, "1.0.0", _nodes, _edges);
}

// ============================================================================
// Connector templates (Step 5)
// ============================================================================

public static class ConnectorTemplates
{
    public static TopicSourceConfig OrderEventsSource() => new(
        Topic: "order-events",
        Format: "json",
        StartOffset: "earliest"
    );

    public static FilterConfig ValidOrderFilter() => new(
        Expression: "order.total > 0 && order.status != 'cancelled'",
        DropOnError: true
    );

    public static TopicSinkConfig ProcessedOrdersSink() => new(
        Topic: "processed-orders",
        Format: "json",
        KeyExpression: "order.id",
        Partitioner: "byKey"
    );

    public static PipelineDefinition OrderProcessingPipeline()
    {
        return new EnhancedPipelineBuilder("order-processing-template")
            .WithName("Order Processing (Template)")
            .AddTopicSource("source", "Order Events", OrderEventsSource(), new NodePosition(100, 100))
            .AddFilter("filter", "Valid Orders", ValidOrderFilter(), new NodePosition(300, 100))
            .AddTopicSink("sink", "Processed Orders", ProcessedOrdersSink(), new NodePosition(500, 100))
            .Connect("source", "filter")
            .Connect("filter", "sink")
            .Build();
    }
}