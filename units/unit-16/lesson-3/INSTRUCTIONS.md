# Lesson 25.3: Instructions

Follow these step-by-step instructions to configure connector nodes in visual pipelines.

## Step 1: Define Detailed Configuration Classes

Create configuration classes for each connector type:

```csharp
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
```

## Step 2: Create Enhanced Node Classes

Define nodes that use the detailed configurations:

```csharp
public record EnhancedSourceNode(
    string Id,
    string Name,
    NodePosition Position,
    SourceType Type,
    object Config
) : PipelineNode(Id, Name, Position);

public enum SourceType { Topic, File, Http, Timer }

public record EnhancedTransformNode(
    string Id,
    string Name,
    NodePosition Position,
    TransformType Type,
    object Config,
    List<string> Inputs
) : PipelineNode(Id, Name, Position);

public enum TransformType { Filter, Map, FlatMap, Aggregate, Join }

public record EnhancedSinkNode(
    string Id,
    string Name,
    NodePosition Position,
    SinkType Type,
    object Config,
    List<string> Inputs
) : PipelineNode(Id, Name, Position);

public enum SinkType { Topic, File, Http, Console }
```

## Step 3: Implement Configuration Validators

Create validators for each configuration type:

```csharp
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

        // Basic expression validation (check for common operators)
        if (!config.Expression.Contains("==") && !config.Expression.Contains(">") &&
            !config.Expression.Contains("<") && !config.Expression.Contains("!="))
            errors.Add("Filter expression should contain a comparison operator");

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
```

## Step 4: Build an Enhanced Pipeline Builder

Create a builder that supports enhanced node configurations:

```csharp
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
```

## Step 5: Create Connector Templates

Define reusable templates for common patterns:

```csharp
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

    // Create a complete order processing pipeline template
    public static PipelineDefinition OrderProcessingPipeline()
    {
        return new EnhancedPipelineBuilder("order-processing-template")
            .WithName("Order Processing (Template)")
            .AddTopicSource("source", "Order Events", OrderEventsSource(), new(100, 100))
            .AddFilter("filter", "Valid Orders", ValidOrderFilter(), new(300, 100))
            .AddTopicSink("sink", "Processed Orders", ProcessedOrdersSink(), new(500, 100))
            .Connect("source", "filter")
            .Connect("filter", "sink")
            .Build();
    }
}
```

## Step 6: Execute with Surgewave

Run the configured pipeline:

```csharp
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;

// Get the source configuration
var sourceNode = pipeline.Nodes.OfType<EnhancedSourceNode>().First();
var sourceConfig = (TopicSourceConfig)sourceNode.Config;

// Create consumer with the configured settings
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = sourceConfig.ConsumerGroup ?? "pipeline-executor";
    opt.AutoOffsetReset = sourceConfig.StartOffset == "earliest"
        ? AutoOffsetReset.Earliest
        : AutoOffsetReset.Latest;
});

consumer.Subscribe(sourceConfig.Topic);
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Sample Output

```
=== Surgewave Bootcamp: Lesson 25.3 - Connector Nodes and Configuration ===

Surgewave broker started on port 12345

--- Creating Configured Pipeline ---
[Config] TopicSource: order-events (json, earliest)
[Config] Filter: order.total > 0 && order.status != 'cancelled'
[Config] TopicSink: processed-orders (json, byKey)

--- Validation ---
TopicSourceConfig: Valid
FilterConfig: Valid
TopicSinkConfig: Valid
Pipeline: Valid

--- Using Template ---
Created pipeline from template: Order Processing (Template)

--- Executing Pipeline ---
[Source:order-events] Received: {"id":"001","total":150.00,"status":"pending"}
[Filter] Passed: total > 0, status != cancelled
[Sink:processed-orders] Produced with key: 001

[Source:order-events] Received: {"id":"002","total":0.00,"status":"pending"}
[Filter] Dropped: total was 0

[Source:order-events] Received: {"id":"003","total":75.50,"status":"cancelled"}
[Filter] Dropped: status was cancelled

Statistics:
  Received: 3
  Passed filter: 1
  Dropped: 2

Lesson complete!
```

## Knowledge Check

1. What are the main categories of connectors in visual pipelines?
2. Why is configuration validation important?
3. How do connector templates improve developer productivity?
4. What is the difference between Filter and Map transforms?
5. How would you configure a sink for high-throughput scenarios?

---

**Next:** [Lesson 25.4: DAG Patterns](../lesson-4/README.md)
