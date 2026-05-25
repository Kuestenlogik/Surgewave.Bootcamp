# Knowledge Assessment: Lesson 25.1 - Pipeline UI Overview

## Multiple Choice

### 1. What does DAG stand for in the context of visual pipelines?
- [ ] A) Data Aggregation Graph
- [ ] B) Directed Acyclic Graph
- [ ] C) Dynamic Application Gateway
- [ ] D) Distributed Analytics Grid

<details>
<summary>Answer</summary>
**B)** Directed Acyclic Graph. A DAG is a graph where edges have direction (data flows one way) and there are no cycles (no loops that would cause infinite processing).
</details>

### 2. When is a visual pipeline the BEST choice?
- [ ] A) When implementing complex machine learning algorithms
- [ ] B) When you need extensive unit testing coverage
- [ ] C) When rapidly prototyping a data integration flow
- [ ] D) When building performance-critical hot paths

<details>
<summary>Answer</summary>
**C)** Visual pipelines excel at rapid prototyping and data integration scenarios where you need immediate visual feedback and the ability to quickly modify topology without code changes.
</details>

### 3. Which component validates that a pipeline has no cycles?
- [ ] A) Pipeline Canvas
- [ ] B) Node Palette
- [ ] C) DAG Validator
- [ ] D) Properties Panel

<details>
<summary>Answer</summary>
**C)** The DAG Validator checks for cycles using graph traversal algorithms (like DFS) to ensure the pipeline structure is acyclic.
</details>

### 4. What is the correct order of pipeline lifecycle phases?
- [ ] A) Operation -> Design -> Deployment
- [ ] B) Design -> Operation -> Deployment
- [ ] C) Design -> Deployment -> Operation
- [ ] D) Deployment -> Design -> Operation

<details>
<summary>Answer</summary>
**C)** The correct order is Design (create and configure nodes) -> Deployment (compile and start) -> Operation (monitor and debug).
</details>

### 5. Which node type would you use to combine data from multiple paths?
- [ ] A) Branch Node
- [ ] B) Filter Node
- [ ] C) Merge Node
- [ ] D) Source Node

<details>
<summary>Answer</summary>
**C)** A Merge Node combines multiple input paths into a single output stream. Branch Nodes do the opposite - splitting one path into multiple.
</details>

## True/False

### 6. A visual pipeline can contain cycles (loops) as long as there is a termination condition.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Visual pipelines must be Directed Acyclic Graphs (DAGs). Cycles are not allowed because they would cause infinite data loops. For iterative processing, use alternative patterns like external feedback via topics.
</details>

### 7. Visual pipelines are compiled to the same execution format as code-based pipelines.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Visual pipelines are compiled to execution plans that run on the same Surgewave runtime. The visual representation is just a different way to define the same underlying pipeline structure.
</details>

### 8. Disconnected nodes (not connected to any edge) cause pipeline validation to fail.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Disconnected nodes typically generate warnings, not errors. The pipeline may still be valid but the disconnected node won't process any data. Best practice is to remove unused nodes.
</details>

## Code Challenge

### 9. What's wrong with this pipeline definition?

```csharp
var pipeline = new PipelineDefinition(
    Id: "my-pipeline",
    Name: "My Pipeline",
    Version: "1.0.0",
    Nodes: new List<PipelineNode>
    {
        new SourceNode("source-1", "Source", new NodePosition(100, 100), "events", "json"),
        new TransformNode("transform-1", "Filter", new NodePosition(200, 100), "Filter", "x > 0", new List<string> { "source-1" }),
        new TransformNode("transform-2", "Map", new NodePosition(300, 100), "Map", "x * 2", new List<string> { "transform-1" }),
        new SinkNode("sink-1", "Output", new NodePosition(400, 100), "results", new List<string> { "transform-2" })
    },
    Edges: new List<PipelineEdge>
    {
        new PipelineEdge("source-1", "transform-1"),
        new PipelineEdge("transform-1", "transform-2"),
        new PipelineEdge("transform-2", "sink-1"),
        new PipelineEdge("sink-1", "source-1")  // <-- Issue here
    }
);
```

<details>
<summary>Answer</summary>
The pipeline has a **cycle**: `sink-1` connects back to `source-1`, creating an infinite loop: `source-1 -> transform-1 -> transform-2 -> sink-1 -> source-1 -> ...`

This violates the DAG (Directed Acyclic Graph) requirement. The edge from `sink-1` to `source-1` must be removed.

```csharp
Edges: new List<PipelineEdge>
{
    new PipelineEdge("source-1", "transform-1"),
    new PipelineEdge("transform-1", "transform-2"),
    new PipelineEdge("transform-2", "sink-1")
    // Remove: new PipelineEdge("sink-1", "source-1")
}
```
</details>

### 10. Write code to find all source nodes (nodes with no incoming edges) in a pipeline.

<details>
<summary>Answer</summary>

```csharp
public static List<PipelineNode> FindSourceNodes(PipelineDefinition pipeline)
{
    // Get all nodes that are targets of edges
    var nodesWithIncoming = pipeline.Edges
        .Select(e => e.To)
        .ToHashSet();

    // Source nodes have no incoming edges
    return pipeline.Nodes
        .Where(n => !nodesWithIncoming.Contains(n.Id))
        .ToList();
}

// Usage:
var sourceNodes = FindSourceNodes(pipeline);
foreach (var node in sourceNodes)
{
    Console.WriteLine($"Source node: {node.Name} ({node.Id})");
}
```

Note: This finds nodes with no incoming edges, which are the starting points of the pipeline. In a properly structured pipeline, these should be SourceNode types.
</details>

## Score
- 10/10: Excellent!
- 8-9/10: Good understanding
- 6-7/10: Review the lesson
- <6/10: Re-do the lesson
