// Surgewave Bootcamp - Unit 25, Lesson 4: DAG Patterns
// COMPLETED SOLUTION

using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 25.4 - DAG Patterns ===");
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

// ============================================================================
// Pattern 1: Fan-Out
// ============================================================================
Console.WriteLine("\n--- Pattern 1: Fan-Out ---");
Console.WriteLine("One source distributes to multiple processors in parallel.");

var fanOut = DagPatterns.CreateFanOutPattern();
var fanOutExecutor = new DagExecutor(fanOut);

var order = "{\"orderId\":\"ORD-001\",\"amount\":150}";
Console.WriteLine($"Processing order through fan-out pipeline...");

var fanOutResults = fanOutExecutor.Execute("source", order);
foreach (var r in fanOutResults)
{
    Console.WriteLine($"  [{r.NodeType}] {r.NodeName}: {r.Output}");
}

var sinkCount = fanOutResults.Count(r => r.NodeType == "sink");
Console.WriteLine($"  Result: 1 input -> {sinkCount} outputs");

// Demonstrate with Surgewave
await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
await producer.ProduceAsync("orders", "ORD-001", order);
Console.WriteLine($"  [Surgewave] Produced order to 'orders' topic");

// ============================================================================
// Pattern 2: Fan-In
// ============================================================================
Console.WriteLine("\n--- Pattern 2: Fan-In ---");
Console.WriteLine("Multiple sources merge into a single output.");

var fanIn = DagPatterns.CreateFanInPattern();
var fanInExecutor = new DagExecutor(fanIn);

var regions = new[] { ("us-source", "US order #100"), ("eu-source", "EU order #200"), ("apac-source", "APAC order #300") };

foreach (var (sourceId, message) in regions)
{
    var results = fanInExecutor.Execute(sourceId, message);
    var sourceName = results.First().NodeName;
    var transformName = results.FirstOrDefault(r => r.NodeType == "transform")?.NodeName ?? "?";
    Console.WriteLine($"  [source] {sourceName} -> [transform] {transformName} -> [merge] -> [sink] Global Events");

    // Produce to Surgewave
    await producer.ProduceAsync($"events-{sourceId.Replace("-source", "")}", sourceId, message);
}
Console.WriteLine($"  Result: {regions.Length} inputs -> 1 output");

// ============================================================================
// Pattern 3: Diamond
// ============================================================================
Console.WriteLine("\n--- Pattern 3: Diamond ---");
Console.WriteLine("Source splits to parallel paths, then merges back.");

var diamond = DagPatterns.CreateDiamondPattern();
var diamondExecutor = new DagExecutor(diamond);

var rawOrder = "{\"orderId\":\"ORD-001\",\"customer\":\"Alice\"}";
var diamondResults = diamondExecutor.Execute("source", rawOrder);

Console.WriteLine("  [source] -> [validate] + [enrich] -> [merge] -> [sink]");
var fastResult = diamondResults.FirstOrDefault(r => r.NodeName == "Quick Validation");
var enrichResult = diamondResults.FirstOrDefault(r => r.NodeName == "Data Enrichment");
if (fastResult is not null)
    Console.WriteLine($"  Fast path: {fastResult.Output}");
if (enrichResult is not null)
    Console.WriteLine($"  Enrich path: {enrichResult.Output}");
Console.WriteLine("  Combined result sent to enriched-orders");

// ============================================================================
// Pattern 4: Conditional Branch
// ============================================================================
Console.WriteLine("\n--- Pattern 4: Conditional Branch ---");
Console.WriteLine("Routes messages to different handlers based on content.");

var branch = DagPatterns.CreateConditionalBranchPattern();
var branchExecutor = new DagExecutor(branch);

var testOrders = new[]
{
    ("{\"orderId\":\"ORD-100\",\"priority\":\"high\",\"amount\":500}", "high"),
    ("{\"orderId\":\"ORD-200\",\"priority\":\"medium\",\"amount\":100}", "medium"),
    ("{\"orderId\":\"ORD-300\",\"priority\":\"low\",\"amount\":25}", "low")
};

foreach (var (orderMsg, expectedPriority) in testOrders)
{
    var results = branchExecutor.Execute("source", orderMsg);
    var sinkResult = results.LastOrDefault(r => r.NodeType == "sink");
    var handlerResult = results.FirstOrDefault(r => r.NodeType == "transform");

    var queueName = sinkResult?.NodeName ?? "Unknown";
    Console.WriteLine($"  {expectedPriority.ToUpper()} priority -> {queueName}");

    // Produce to Surgewave
    await producer.ProduceAsync("orders", $"ORD-{expectedPriority}", orderMsg);
}

// ============================================================================
// Summary: DAG Visualization
// ============================================================================
Console.WriteLine("\n--- DAG Pattern Summary ---");

var patterns = new[] { fanOut, fanIn, diamond, branch };
foreach (var pattern in patterns)
{
    var nodeCount = pattern.Nodes.Count;
    var edgeCount = pattern.Edges.Count;
    var sources = pattern.Nodes.Count(n => n.NodeType == DagNodeType.Source);
    var sinks = pattern.Nodes.Count(n => n.NodeType == DagNodeType.Sink);
    Console.WriteLine($"  {pattern.Name,-25} | Nodes: {nodeCount} | Edges: {edgeCount} | Sources: {sources} | Sinks: {sinks}");
}

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Fan-out sends each message to all downstream processors");
Console.WriteLine("2. Fan-in merges multiple sources into a single stream");
Console.WriteLine("3. Diamond combines fan-out and fan-in for parallel enrichment");
Console.WriteLine("4. Conditional branching routes messages based on content");
Console.WriteLine("5. DAGs must be acyclic to prevent infinite loops");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============================================================================
// DAG Types
// ============================================================================

public enum DagPatternType { FanOut, FanIn, Diamond, ConditionalBranch, ParallelProcessing }
public enum DagNodeType { Source, Transform, Sink, Branch, Merge }

public record DagPattern(string Name, DagPatternType Type, List<DagNode> Nodes, List<DagEdge> Edges);
public abstract record DagNode(string Id, string Name, DagNodeType NodeType);
public record SourceNode(string Id, string Name, string Topic) : DagNode(Id, Name, DagNodeType.Source);
public record TransformNode(string Id, string Name, Func<string, string?> Transform) : DagNode(Id, Name, DagNodeType.Transform);
public record SinkNode(string Id, string Name, string OutputTopic) : DagNode(Id, Name, DagNodeType.Sink);
public record BranchNode(string Id, string Name, Func<string, string> Router) : DagNode(Id, Name, DagNodeType.Branch);
public record MergeNode(string Id, string Name) : DagNode(Id, Name, DagNodeType.Merge);
public record DagEdge(string From, string To, string? Label = null);

public record ExecutionResult(string NodeId, string NodeName, string NodeType, string Output);

// ============================================================================
// DAG Pattern Factory
// ============================================================================

public static class DagPatterns
{
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

    public static DagPattern CreateConditionalBranchPattern()
    {
        return new DagPattern(
            Name: "Priority Router",
            Type: DagPatternType.ConditionalBranch,
            Nodes: new List<DagNode>
            {
                new SourceNode("source", "All Orders", "orders"),
                new BranchNode("router", "Priority Router",
                    msg => msg.Contains("\"priority\":\"high\"") ? "high" :
                           msg.Contains("\"priority\":\"medium\"") ? "medium" : "low"),
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
}

// ============================================================================
// DAG Executor
// ============================================================================

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
        if (!_nodeMap.TryGetValue(nodeId, out var node)) return;

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
