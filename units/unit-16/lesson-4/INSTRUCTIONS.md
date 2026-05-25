# Lesson 25.4: Instructions

Follow these step-by-step instructions to implement DAG patterns for visual pipelines.

## Step 1: Define DAG Pattern Types

Create models for the various DAG patterns:

```csharp
public enum DagPatternType
{
    FanOut,
    FanIn,
    Diamond,
    ConditionalBranch,
    ParallelProcessing
}

public record DagPattern(
    string Name,
    DagPatternType Type,
    List<DagNode> Nodes,
    List<DagEdge> Edges
);

public abstract record DagNode(string Id, string Name, DagNodeType NodeType);
public enum DagNodeType { Source, Transform, Sink, Branch, Merge }

public record SourceNode(string Id, string Name, string Topic) : DagNode(Id, Name, DagNodeType.Source);
public record TransformNode(string Id, string Name, Func<string, string?> Transform) : DagNode(Id, Name, DagNodeType.Transform);
public record SinkNode(string Id, string Name, string OutputTopic) : DagNode(Id, Name, DagNodeType.Sink);
public record BranchNode(string Id, string Name, Func<string, string> Router) : DagNode(Id, Name, DagNodeType.Branch);
public record MergeNode(string Id, string Name) : DagNode(Id, Name, DagNodeType.Merge);

public record DagEdge(string From, string To, string? Label = null);
```

## Step 2: Implement Fan-Out Pattern

Create a pipeline that distributes messages to multiple processors:

```csharp
public static DagPattern CreateFanOutPattern()
{
    return new DagPattern(
        Name: "Order Fan-Out",
        Type: DagPatternType.FanOut,
        Nodes: new List<DagNode>
        {
            new SourceNode("source", "Order Source", "orders"),
            new TransformNode("analytics", "Analytics Processor",
                msg => $"[analytics] {msg}"),
            new TransformNode("fulfillment", "Fulfillment Processor",
                msg => $"[fulfillment] {msg}"),
            new TransformNode("notification", "Notification Processor",
                msg => $"[notification] {msg}"),
            new SinkNode("analytics-sink", "Analytics Output", "analytics-events"),
            new SinkNode("fulfillment-sink", "Fulfillment Output", "fulfillment-events"),
            new SinkNode("notification-sink", "Notification Output", "notification-events")
        },
        Edges: new List<DagEdge>
        {
            new("source", "analytics"),
            new("source", "fulfillment"),
            new("source", "notification"),
            new("analytics", "analytics-sink"),
            new("fulfillment", "fulfillment-sink"),
            new("notification", "notification-sink")
        }
    );
}
```

## Step 3: Implement Fan-In Pattern

Create a pipeline that merges multiple sources:

```csharp
public static DagPattern CreateFanInPattern()
{
    return new DagPattern(
        Name: "Multi-Region Aggregator",
        Type: DagPatternType.FanIn,
        Nodes: new List<DagNode>
        {
            new SourceNode("us-source", "US Events", "events-us"),
            new SourceNode("eu-source", "EU Events", "events-eu"),
            new SourceNode("apac-source", "APAC Events", "events-apac"),
            new TransformNode("us-transform", "US Normalizer",
                msg => $"[US] {msg}"),
            new TransformNode("eu-transform", "EU Normalizer",
                msg => $"[EU] {msg}"),
            new TransformNode("apac-transform", "APAC Normalizer",
                msg => $"[APAC] {msg}"),
            new MergeNode("merge", "Global Merge"),
            new SinkNode("global-sink", "Global Events", "events-global")
        },
        Edges: new List<DagEdge>
        {
            new("us-source", "us-transform"),
            new("eu-source", "eu-transform"),
            new("apac-source", "apac-transform"),
            new("us-transform", "merge"),
            new("eu-transform", "merge"),
            new("apac-transform", "merge"),
            new("merge", "global-sink")
        }
    );
}
```

## Step 4: Implement Diamond Pattern

Create a pattern that fans out and then fans back in:

```csharp
public static DagPattern CreateDiamondPattern()
{
    return new DagPattern(
        Name: "Enrichment Diamond",
        Type: DagPatternType.Diamond,
        Nodes: new List<DagNode>
        {
            new SourceNode("source", "Raw Orders", "raw-orders"),
            new TransformNode("fast-path", "Quick Validation",
                msg => $"[validated] {msg}"),
            new TransformNode("enrich-path", "Data Enrichment",
                msg => $"[enriched] {msg}"),
            new MergeNode("merge", "Combine Results"),
            new SinkNode("sink", "Enriched Orders", "enriched-orders")
        },
        Edges: new List<DagEdge>
        {
            new("source", "fast-path", "validate"),
            new("source", "enrich-path", "enrich"),
            new("fast-path", "merge"),
            new("enrich-path", "merge"),
            new("merge", "sink")
        }
    );
}
```

## Step 5: Implement Conditional Branch Pattern

Route messages based on content:

```csharp
public static DagPattern CreateConditionalBranchPattern()
{
    return new DagPattern(
        Name: "Priority Router",
        Type: DagPatternType.ConditionalBranch,
        Nodes: new List<DagNode>
        {
            new SourceNode("source", "All Orders", "orders"),
            new BranchNode("router", "Priority Router",
                msg => msg.Contains("priority\":\"high") ? "high" :
                       msg.Contains("priority\":\"medium") ? "medium" : "low"),
            new TransformNode("high-handler", "Priority Handler",
                msg => $"[PRIORITY] {msg}"),
            new TransformNode("medium-handler", "Standard Handler",
                msg => $"[STANDARD] {msg}"),
            new TransformNode("low-handler", "Batch Handler",
                msg => $"[BATCH] {msg}"),
            new SinkNode("high-sink", "Priority Queue", "orders-priority"),
            new SinkNode("medium-sink", "Standard Queue", "orders-standard"),
            new SinkNode("low-sink", "Batch Queue", "orders-batch")
        },
        Edges: new List<DagEdge>
        {
            new("source", "router"),
            new("router", "high-handler", "high"),
            new("router", "medium-handler", "medium"),
            new("router", "low-handler", "low"),
            new("high-handler", "high-sink"),
            new("medium-handler", "medium-sink"),
            new("low-handler", "low-sink")
        }
    );
}
```

## Step 6: Build a DAG Executor

Create an executor that processes messages through the DAG:

```csharp
public class DagExecutor
{
    private readonly DagPattern _pattern;
    private readonly Dictionary<string, DagNode> _nodeMap;
    private readonly Dictionary<string, List<DagEdge>> _outgoingEdges;

    public DagExecutor(DagPattern pattern)
    {
        _pattern = pattern;
        _nodeMap = pattern.Nodes.ToDictionary(n => n.Id);
        _outgoingEdges = pattern.Edges
            .GroupBy(e => e.From)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public List<ExecutionResult> Execute(string sourceNodeId, string message)
    {
        var results = new List<ExecutionResult>();
        ProcessNode(sourceNodeId, message, results);
        return results;
    }

    private void ProcessNode(string nodeId, string message, List<ExecutionResult> results)
    {
        var node = _nodeMap[nodeId];
        string? output = message;

        switch (node)
        {
            case TransformNode t:
                output = t.Transform(message);
                if (output is null) return; // Filtered out
                results.Add(new ExecutionResult(nodeId, node.Name, "transform", output));
                break;

            case BranchNode b:
                var route = b.Router(message);
                results.Add(new ExecutionResult(nodeId, node.Name, "branch", $"route={route}"));

                // Only follow the matching edge
                if (_outgoingEdges.TryGetValue(nodeId, out var branchEdges))
                {
                    foreach (var edge in branchEdges.Where(e => e.Label == route))
                    {
                        ProcessNode(edge.To, message, results);
                    }
                }
                return;

            case MergeNode:
                results.Add(new ExecutionResult(nodeId, node.Name, "merge", message));
                break;

            case SinkNode s:
                results.Add(new ExecutionResult(nodeId, node.Name, "sink", $"-> {s.OutputTopic}: {message}"));
                return;

            case SourceNode:
                results.Add(new ExecutionResult(nodeId, node.Name, "source", message));
                break;
        }

        // Process downstream nodes
        if (_outgoingEdges.TryGetValue(nodeId, out var edges))
        {
            foreach (var edge in edges)
            {
                ProcessNode(edge.To, output ?? message, results);
            }
        }
    }
}

public record ExecutionResult(string NodeId, string NodeName, string NodeType, string Output);
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating all four DAG patterns with Surgewave message processing.

## Sample Output

```
=== Surgewave Bootcamp: Lesson 25.4 - DAG Patterns ===

Surgewave broker started on port 12345

--- Pattern 1: Fan-Out ---
Processing order through fan-out pipeline...
  [source] Order Source: {"orderId":"ORD-001","amount":150}
  [transform] Analytics Processor: [analytics] {"orderId":"ORD-001","amount":150}
  [sink] Analytics Output: -> analytics-events
  [transform] Fulfillment Processor: [fulfillment] {"orderId":"ORD-001","amount":150}
  [sink] Fulfillment Output: -> fulfillment-events
  [transform] Notification Processor: [notification] {"orderId":"ORD-001","amount":150}
  [sink] Notification Output: -> notification-events
  Result: 1 input -> 3 outputs

--- Pattern 2: Fan-In ---
Processing events from 3 regions...
  [source] US Events -> [transform] US Normalizer -> [merge] -> [sink] Global Events
  [source] EU Events -> [transform] EU Normalizer -> [merge] -> [sink] Global Events
  [source] APAC Events -> [transform] APAC Normalizer -> [merge] -> [sink] Global Events
  Result: 3 inputs -> 1 output

--- Pattern 3: Diamond ---
Processing order through diamond pattern...
  [source] -> [validate] + [enrich] -> [merge] -> [sink]
  Fast path: [validated] {"orderId":"ORD-001"}
  Enrich path: [enriched] {"orderId":"ORD-001"}
  Combined result sent to enriched-orders

--- Pattern 4: Conditional Branch ---
Processing orders with different priorities...
  High priority -> Priority Queue
  Medium priority -> Standard Queue
  Low priority -> Batch Queue

Lesson complete!
```

## Knowledge Check

1. When would you use fan-out vs fan-in?
2. What are the advantages of the diamond pattern?
3. How does conditional branching differ from simple filtering?
4. What are the potential issues with parallel processing in a DAG?
5. How can you ensure ordering guarantees in a fan-out pattern?

---

**Next:** [Lesson 25.5: Pipeline Monitoring and Debugging](../lesson-5/README.md)
