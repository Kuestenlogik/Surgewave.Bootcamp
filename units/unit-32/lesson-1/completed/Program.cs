// Surgewave Bootcamp - Unit 27, Lesson 1: Hybrid Cloud Concepts
// COMPLETED SOLUTION

using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 27.1 - Hybrid Cloud Concepts ===");
Console.WriteLine();

// Step 1 - Define cluster configuration models
var clusterConfigs = new Dictionary<string, ClusterConfig>
{
    ["us-east-1"] = new ClusterConfig("us-east-1", "US East", "AWS", ClusterRole.Primary),
    ["eu-west-1"] = new ClusterConfig("eu-west-1", "EU West", "Azure", ClusterRole.Secondary),
    ["ap-south-1"] = new ClusterConfig("ap-south-1", "AP South", "GCP", ClusterRole.Edge)
};

// Step 2 - Start multiple simulated clusters
var clusters = new Dictionary<string, SurgewaveRuntime>();
var clusterStats = new Dictionary<string, ClusterStats>();

Console.WriteLine("--- Starting Multi-Region Clusters ---");

// US East (Primary - AWS)
var usEast = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
clusters["us-east-1"] = usEast;
clusterStats["us-east-1"] = new ClusterStats();
Console.WriteLine($"[{clusterConfigs["us-east-1"].Region}] Primary cluster started on port {usEast.Port} ({clusterConfigs["us-east-1"].CloudProvider})");

// EU West (Secondary - Azure)
var euWest = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
clusters["eu-west-1"] = euWest;
clusterStats["eu-west-1"] = new ClusterStats();
Console.WriteLine($"[{clusterConfigs["eu-west-1"].Region}] Secondary cluster started on port {euWest.Port} ({clusterConfigs["eu-west-1"].CloudProvider})");

// AP South (Edge - GCP)
var apSouth = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
clusters["ap-south-1"] = apSouth;
clusterStats["ap-south-1"] = new ClusterStats();
Console.WriteLine($"[{clusterConfigs["ap-south-1"].Region}] Edge cluster started on port {apSouth.Port} ({clusterConfigs["ap-south-1"].CloudProvider})");

// Step 3 - Implement regional topic naming
Console.WriteLine("\n--- Topic Naming Conventions ---");
var usOrdersTopic = TopicNaming.Regional("us-east", "orders.created");
var euOrdersTopic = TopicNaming.Regional("eu-west", "orders.created");
var apOrdersTopic = TopicNaming.Regional("ap-south", "orders.created");
var globalConfigTopic = TopicNaming.Global("config.updates");
var aggregateOrdersTopic = TopicNaming.Aggregate("orders.all");

Console.WriteLine($"Regional (US): {usOrdersTopic}");
Console.WriteLine($"Regional (EU): {euOrdersTopic}");
Console.WriteLine($"Regional (AP): {apOrdersTopic}");
Console.WriteLine($"Global: {globalConfigTopic}");
Console.WriteLine($"Aggregate: {aggregateOrdersTopic}");

// Step 4 - Create cluster locator for proximity-based selection
Console.WriteLine("\n--- Cluster Selection Demo ---");
var clusterEndpoints = new Dictionary<string, (string BootstrapServers, int SimulatedLatencyMs)>
{
    ["us-east-1"] = ($"localhost:{usEast.Port}", 10),
    ["eu-west-1"] = ($"localhost:{euWest.Port}", 100),
    ["ap-south-1"] = ($"localhost:{apSouth.Port}", 200)
};

var locator = new ClusterLocator(clusterEndpoints);

var usSelection = locator.FindNearest("US");
Console.WriteLine($"Client in US -> Connect to: {usSelection.ClusterId} ({usSelection.LatencyMs}ms latency)");

var euSelection = locator.FindNearest("EU");
Console.WriteLine($"Client in EU -> Connect to: {euSelection.ClusterId} ({euSelection.LatencyMs}ms latency)");

var apSelection = locator.FindNearest("APAC");
Console.WriteLine($"Client in APAC -> Connect to: {apSelection.ClusterId} ({apSelection.LatencyMs}ms latency)");

// Step 5 - Create regional producers and produce messages
Console.WriteLine("\n--- Regional Message Production ---");

await using var usProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{usEast.Port}";
    opt.ClientId = "producer-us-east";
});

await using var euProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{euWest.Port}";
    opt.ClientId = "producer-eu-west";
});

await using var apProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{apSouth.Port}";
    opt.ClientId = "producer-ap-south";
});

// US orders
var usOrders = new[]
{
    new { OrderId = "US-001", Region = "us-east", Amount = 99.99, Customer = "Alice" },
    new { OrderId = "US-002", Region = "us-east", Amount = 149.50, Customer = "Bob" },
    new { OrderId = "US-003", Region = "us-east", Amount = 75.00, Customer = "Carol" }
};

foreach (var order in usOrders)
{
    await usProducer.ProduceAsync(usOrdersTopic, order.OrderId, JsonSerializer.Serialize(order));
    clusterStats["us-east-1"].MessagesProduced++;
    Console.WriteLine($"[us-east-1] Produced order {order.OrderId} to {usOrdersTopic}");
}

// EU orders
var euOrders = new[]
{
    new { OrderId = "EU-001", Region = "eu-west", Amount = 199.99, Customer = "Hans" },
    new { OrderId = "EU-002", Region = "eu-west", Amount = 89.00, Customer = "Marie" }
};

foreach (var order in euOrders)
{
    await euProducer.ProduceAsync(euOrdersTopic, order.OrderId, JsonSerializer.Serialize(order));
    clusterStats["eu-west-1"].MessagesProduced++;
    Console.WriteLine($"[eu-west-1] Produced order {order.OrderId} to {euOrdersTopic}");
}

// APAC order
var apOrder = new { OrderId = "AP-001", Region = "ap-south", Amount = 299.99, Customer = "Yuki" };
await apProducer.ProduceAsync(apOrdersTopic, apOrder.OrderId, JsonSerializer.Serialize(apOrder));
clusterStats["ap-south-1"].MessagesProduced++;
Console.WriteLine($"[ap-south-1] Produced order {apOrder.OrderId} to {apOrdersTopic}");

// Step 6 - Consume from regional clusters
Console.WriteLine("\n--- Regional Message Consumption ---");

await using var usConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{usEast.Port}";
    opt.GroupId = "regional-processor-us";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
usConsumer.Subscribe(usOrdersTopic);

await using var euConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{euWest.Port}";
    opt.GroupId = "regional-processor-eu";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
euConsumer.Subscribe(euOrdersTopic);

await using var apConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{apSouth.Port}";
    opt.GroupId = "regional-processor-ap";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
apConsumer.Subscribe(apOrdersTopic);

// Consume US orders
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
try
{
    for (int i = 0; i < usOrders.Length; i++)
    {
        var result = await usConsumer.ConsumeAsync(cts.Token);
        if (result != null)
        {
            clusterStats["us-east-1"].MessagesConsumed++;
            Console.WriteLine($"[us-east-1] Consumed: {result.Key}");
        }
    }
}
catch (OperationCanceledException) { }

// Consume EU orders
using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(2));
try
{
    for (int i = 0; i < euOrders.Length; i++)
    {
        var result = await euConsumer.ConsumeAsync(cts2.Token);
        if (result != null)
        {
            clusterStats["eu-west-1"].MessagesConsumed++;
            Console.WriteLine($"[eu-west-1] Consumed: {result.Key}");
        }
    }
}
catch (OperationCanceledException) { }

// Consume APAC orders
using var cts3 = new CancellationTokenSource(TimeSpan.FromSeconds(2));
try
{
    var result = await apConsumer.ConsumeAsync(cts3.Token);
    if (result != null)
    {
        clusterStats["ap-south-1"].MessagesConsumed++;
        Console.WriteLine($"[ap-south-1] Consumed: {result.Key}");
    }
}
catch (OperationCanceledException) { }

// Step 7 - Display cluster statistics
Console.WriteLine("\n--- Cluster Statistics ---");
foreach (var (clusterId, stats) in clusterStats)
{
    var config = clusterConfigs[clusterId];
    Console.WriteLine($"Cluster: {clusterId} ({config.Role})");
    Console.WriteLine($"  - Cloud Provider: {config.CloudProvider}");
    Console.WriteLine($"  - Messages Produced: {stats.MessagesProduced}");
    Console.WriteLine($"  - Messages Consumed: {stats.MessagesConsumed}");
    Console.WriteLine($"  - Role: {config.Role}");
    Console.WriteLine();
}

// Cleanup
await usEast.DisposeAsync();
await euWest.DisposeAsync();
await apSouth.DisposeAsync();

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Hybrid cloud provides resilience and low latency");
Console.WriteLine("2. Use regional topic prefixes for clear data locality");
Console.WriteLine("3. Producers should connect to nearest cluster");
Console.WriteLine("4. Plan topology based on use case (Active-Passive vs Active-Active)");
Console.WriteLine("5. Monitor cluster health and replication lag");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Supporting types

public record ClusterConfig(
    string ClusterId,
    string Region,
    string CloudProvider,
    ClusterRole Role
);

public enum ClusterRole
{
    Primary,    // Active cluster handling all traffic
    Secondary,  // Standby for failover
    Edge        // Regional edge cluster
}

public class ClusterStats
{
    public int MessagesProduced { get; set; }
    public int MessagesConsumed { get; set; }
    public int ReplicationLag { get; set; }
}

public static class TopicNaming
{
    public static string Regional(string region, string baseName)
        => $"{region}.{baseName}";

    public static string Global(string baseName)
        => $"global.{baseName}";

    public static string Aggregate(string baseName)
        => $"agg.{baseName}";
}

public class ClusterLocator
{
    private readonly Dictionary<string, (string BootstrapServers, int SimulatedLatencyMs)> _clusters;

    public ClusterLocator(Dictionary<string, (string, int)> clusters)
    {
        _clusters = clusters;
    }

    public (string ClusterId, string BootstrapServers, int LatencyMs) FindNearest(string clientRegion)
    {
        // Simulate latency-based selection based on client region
        var latencies = new Dictionary<string, Dictionary<string, int>>
        {
            ["US"] = new() { ["us-east-1"] = 10, ["eu-west-1"] = 100, ["ap-south-1"] = 200 },
            ["EU"] = new() { ["us-east-1"] = 100, ["eu-west-1"] = 10, ["ap-south-1"] = 150 },
            ["APAC"] = new() { ["us-east-1"] = 200, ["eu-west-1"] = 150, ["ap-south-1"] = 10 }
        };

        var regionLatencies = latencies.GetValueOrDefault(clientRegion, latencies["US"]);

        var nearest = regionLatencies
            .Where(kv => _clusters.ContainsKey(kv.Key))
            .OrderBy(kv => kv.Value)
            .First();

        return (nearest.Key, _clusters[nearest.Key].BootstrapServers, nearest.Value);
    }
}