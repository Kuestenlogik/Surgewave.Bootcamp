# Lesson 27.1: Instructions

Follow these step-by-step instructions to implement hybrid cloud concepts.

## Step 1: Define the Cluster Configuration Model

Create data structures to represent multi-cluster deployments:

```csharp
// Cluster configuration for hybrid cloud
public record ClusterConfig(
    string ClusterId,
    string Region,
    string CloudProvider,
    int Port,
    ClusterRole Role
);

public enum ClusterRole
{
    Primary,    // Active cluster handling all traffic
    Secondary,  // Standby for failover
    Edge        // Regional edge cluster
}

public enum CloudProvider
{
    AWS,
    Azure,
    GCP,
    OnPremise
}
```

These models help us reason about multi-cluster topologies.

## Step 2: Create Multiple Simulated Clusters

Start multiple Surgewave runtimes to simulate a multi-region deployment:

```csharp
// Simulate three regional clusters
var clusters = new Dictionary<string, SurgewaveRuntime>();

var usEastConfig = new ClusterConfig("us-east-1", "US East", "AWS", 0, ClusterRole.Primary);
var euWestConfig = new ClusterConfig("eu-west-1", "EU West", "Azure", 0, ClusterRole.Secondary);
var apSouthConfig = new ClusterConfig("ap-south-1", "AP South", "GCP", 0, ClusterRole.Edge);

// Start US East cluster (Primary)
var usEast = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
clusters["us-east-1"] = usEast;
Console.WriteLine($"[{usEastConfig.Region}] Primary cluster started on port {usEast.Port}");

// Start EU West cluster (Secondary)
var euWest = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
clusters["eu-west-1"] = euWest;
Console.WriteLine($"[{euWestConfig.Region}] Secondary cluster started on port {euWest.Port}");
```

## Step 3: Implement Regional Topic Naming

Create topics with region prefixes for clear data locality:

```csharp
// Topic naming convention for hybrid cloud
public static class TopicNaming
{
    public static string Regional(string region, string baseName)
        => $"{region}.{baseName}";

    public static string Global(string baseName)
        => $"global.{baseName}";

    public static string Aggregate(string baseName)
        => $"agg.{baseName}";
}

// Usage
var usOrdersTopic = TopicNaming.Regional("us-east", "orders.created");
var euOrdersTopic = TopicNaming.Regional("eu-west", "orders.created");
var globalConfigTopic = TopicNaming.Global("config.updates");
var aggregateOrdersTopic = TopicNaming.Aggregate("orders.all");
```

## Step 4: Create Regional Producers

Connect producers to their nearest cluster:

```csharp
// US-based producer connects to US East cluster
await using var usProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{usEast.Port}";
    opt.ClientId = "producer-us-east";
});

// EU-based producer connects to EU West cluster
await using var euProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{euWest.Port}";
    opt.ClientId = "producer-eu-west";
});

// Produce to regional topics
await usProducer.ProduceAsync("us-east.orders.created",
    "order-001", JsonSerializer.Serialize(new { OrderId = "US-001", Region = "us-east" }));

await euProducer.ProduceAsync("eu-west.orders.created",
    "order-002", JsonSerializer.Serialize(new { OrderId = "EU-001", Region = "eu-west" }));
```

## Step 5: Implement Cluster Selection Logic

Create a simple cluster locator based on simulated latency:

```csharp
public class ClusterLocator
{
    private readonly Dictionary<string, (string BootstrapServers, int SimulatedLatencyMs)> _clusters;

    public ClusterLocator(Dictionary<string, (string, int)> clusters)
    {
        _clusters = clusters;
    }

    public (string ClusterId, string BootstrapServers) FindNearest(string clientRegion)
    {
        // Simulate latency-based selection
        var latencies = new Dictionary<string, int>
        {
            ["us-east-1"] = clientRegion == "US" ? 10 : clientRegion == "EU" ? 100 : 200,
            ["eu-west-1"] = clientRegion == "EU" ? 10 : clientRegion == "US" ? 100 : 150,
            ["ap-south-1"] = clientRegion == "APAC" ? 10 : 200
        };

        var nearest = latencies
            .Where(kv => _clusters.ContainsKey(kv.Key))
            .OrderBy(kv => kv.Value)
            .First();

        return (nearest.Key, _clusters[nearest.Key].BootstrapServers);
    }
}

// Usage
var locator = new ClusterLocator(new Dictionary<string, (string, int)>
{
    ["us-east-1"] = ($"localhost:{usEast.Port}", 10),
    ["eu-west-1"] = ($"localhost:{euWest.Port}", 100)
});

var (clusterId, bootstrapServers) = locator.FindNearest("EU");
Console.WriteLine($"EU client should connect to: {clusterId}");
```

## Step 6: Track Cross-Cluster Statistics

Monitor message distribution across clusters:

```csharp
var clusterStats = new Dictionary<string, ClusterStats>
{
    ["us-east-1"] = new ClusterStats(),
    ["eu-west-1"] = new ClusterStats(),
    ["ap-south-1"] = new ClusterStats()
};

public class ClusterStats
{
    public int MessagesProduced { get; set; }
    public int MessagesConsumed { get; set; }
    public int ReplicationLag { get; set; }
}

// After producing
clusterStats["us-east-1"].MessagesProduced++;

// Display statistics
Console.WriteLine("\n--- Cluster Statistics ---");
foreach (var (cluster, stats) in clusterStats)
{
    Console.WriteLine($"{cluster}: Produced={stats.MessagesProduced}, Consumed={stats.MessagesConsumed}");
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Multi-cluster configuration modeling
- Multiple simulated Surgewave clusters
- Regional topic naming conventions
- Cluster selection based on proximity
- Cross-cluster statistics tracking

## Sample Output

```
=== Surgewave Bootcamp: Lesson 27.1 - Hybrid Cloud Concepts ===

--- Starting Multi-Region Clusters ---
[US East] Primary cluster started on port 12345 (AWS)
[EU West] Secondary cluster started on port 12346 (Azure)
[AP South] Edge cluster started on port 12347 (GCP)

--- Topic Naming Conventions ---
Regional (US): us-east.orders.created
Regional (EU): eu-west.orders.created
Global: global.config.updates
Aggregate: agg.orders.all

--- Cluster Selection Demo ---
Client in US -> Connect to: us-east-1 (10ms latency)
Client in EU -> Connect to: eu-west-1 (10ms latency)
Client in APAC -> Connect to: ap-south-1 (10ms latency)

--- Regional Message Production ---
[us-east-1] Produced order US-001 to us-east.orders.created
[eu-west-1] Produced order EU-001 to eu-west.orders.created
[ap-south-1] Produced order AP-001 to ap-south.orders.created

--- Cluster Statistics ---
Cluster: us-east-1 (Primary)
  - Messages Produced: 3
  - Messages Consumed: 3
  - Role: Primary

Cluster: eu-west-1 (Secondary)
  - Messages Produced: 2
  - Messages Consumed: 2
  - Role: Secondary (DR)

Cluster: ap-south-1 (Edge)
  - Messages Produced: 1
  - Messages Consumed: 1
  - Role: Edge

--- Key Takeaways ---
1. Hybrid cloud provides resilience and low latency
2. Use regional topic prefixes for clear data locality
3. Producers should connect to nearest cluster
4. Plan topology based on use case (Active-Passive vs Active-Active)
5. Monitor cluster health and replication lag

Lesson complete!
```

## Knowledge Check

1. What are the main motivations for hybrid cloud deployments?
2. How does Active-Passive differ from Active-Active topology?
3. Why should topic names include region prefixes?
4. What trade-offs does the CAP theorem describe?
5. How would you handle a network partition between two regions?

---

**Next:** [Lesson 27.2: Multi-Region Deployment](../lesson-2/README.md)
