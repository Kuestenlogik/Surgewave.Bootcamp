# Knowledge Assessment: Lesson 25.2 - Creating Visual Pipelines

## Multiple Choice

### 1. What is the primary benefit of using a fluent PipelineBuilder API?
- [ ] A) It runs faster than direct construction
- [ ] B) It provides readable, chainable configuration
- [ ] C) It automatically validates connections
- [ ] D) It generates optimized code

<details>
<summary>Answer</summary>
**B)** The fluent builder pattern provides readable, chainable configuration that makes pipeline construction self-documenting and easy to understand.
</details>

### 2. Which connection is INVALID in a visual pipeline?
- [ ] A) Source -> Transform
- [ ] B) Transform -> Transform
- [ ] C) Sink -> Transform
- [ ] D) Transform -> Sink

<details>
<summary>Answer</summary>
**C)** Sink -> Transform is invalid because sink nodes are terminal - they output data and cannot have outgoing connections.
</details>

### 3. What does the position property of a node represent?
- [ ] A) The order of execution
- [ ] B) The priority in the DAG
- [ ] C) The visual location on the canvas
- [ ] D) The partition assignment

<details>
<summary>Answer</summary>
**C)** The position property (x, y coordinates) represents where the node appears on the visual canvas. It does not affect execution order, which is determined by edges.
</details>

### 4. Why is JSON serialization important for pipeline definitions?
- [ ] A) It makes pipelines run faster
- [ ] B) It enables version control, sharing, and import/export
- [ ] C) It's required by the Surgewave runtime
- [ ] D) It compresses the pipeline size

<details>
<summary>Answer</summary>
**B)** JSON serialization enables storing pipeline definitions in version control, sharing them between teams, and importing/exporting between environments.
</details>

### 5. What validation check prevents this: `Connect("nodeA", "nodeA")`?
- [ ] A) Cycle detection
- [ ] B) Self-loop prevention
- [ ] C) Duplicate edge check
- [ ] D) Type compatibility check

<details>
<summary>Answer</summary>
**B)** Self-loop prevention specifically checks that a node cannot connect to itself, which would create an immediate cycle.
</details>

## True/False

### 6. A source node can have multiple outgoing connections to different transforms.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - A source node can fan-out to multiple transforms. This is a valid pattern for distributing data to parallel processing paths.
</details>

### 7. The order of `Connect()` calls in the builder determines execution order.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Execution order is determined by the DAG topology (the direction of edges), not by the order in which `Connect()` calls are made in the builder.
</details>

### 8. Nodes can be repositioned on the canvas without affecting data flow.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Node positions are purely visual. Data flow is determined by the edges connecting nodes, not their canvas positions.
</details>

## Code Challenge

### 9. What's wrong with this pipeline builder code?

```csharp
var pipeline = new PipelineBuilder("my-pipeline")
    .AddSource("source-1", b => b
        .FromTopic("input")
        .AtPosition(100, 100))
    .AddTransform("transform-1", b => b
        .Filter("x > 0")
        .AtPosition(200, 100))
    .AddSink("sink-1", b => b
        .ToTopic("output")
        .AtPosition(300, 100))
    .Connect("source-1", "sink-1")  // Direct connection
    .Build();
```

<details>
<summary>Answer</summary>
The pipeline has a **disconnected transform node**. The source connects directly to the sink, bypassing `transform-1`. This would generate a validation warning.

Fixed version:
```csharp
var pipeline = new PipelineBuilder("my-pipeline")
    .AddSource("source-1", b => b
        .FromTopic("input")
        .AtPosition(100, 100))
    .AddTransform("transform-1", b => b
        .Filter("x > 0")
        .AtPosition(200, 100))
    .AddSink("sink-1", b => b
        .ToTopic("output")
        .AtPosition(300, 100))
    .Connect("source-1", "transform-1")  // Connect through transform
    .Connect("transform-1", "sink-1")
    .Build();
```

Either remove the unused transform or connect it properly.
</details>

### 10. Write code to count the number of edges (connections) each node has.

<details>
<summary>Answer</summary>

```csharp
public static void PrintNodeConnections(PipelineDefinition pipeline)
{
    foreach (var node in pipeline.Nodes)
    {
        var incomingCount = pipeline.Edges.Count(e => e.To == node.Id);
        var outgoingCount = pipeline.Edges.Count(e => e.From == node.Id);

        Console.WriteLine($"Node: {node.Id}");
        Console.WriteLine($"  Incoming edges: {incomingCount}");
        Console.WriteLine($"  Outgoing edges: {outgoingCount}");

        var nodeType = node switch
        {
            SourceNode => "Source (should have 0 incoming)",
            SinkNode => "Sink (should have 0 outgoing)",
            TransformNode => "Transform",
            _ => "Unknown"
        };
        Console.WriteLine($"  Type: {nodeType}");
        Console.WriteLine();
    }
}

// Usage:
PrintNodeConnections(pipeline);
```

This helps identify:
- Source nodes that incorrectly have incoming edges
- Sink nodes that incorrectly have outgoing edges
- Disconnected nodes (0 incoming AND 0 outgoing)
</details>

## Score
- 10/10: Excellent!
- 8-9/10: Good understanding
- 6-7/10: Review the lesson
- <6/10: Re-do the lesson
