# Lesson 27.2: Instructions

Follow these step-by-step instructions to implement multi-region deployment.

## Step 1: Define Region Configuration

Create data structures for multi-region setup:

```csharp
public record RegionConfig(
    string RegionId,
    string DisplayName,
    bool IsPrimary,
    string[] ReplicatesTo
);

public record RegionEndpoint(
    string RegionId,
    string BootstrapServers,
    RegionHealth Health,
    int LatencyMs
);

public enum RegionHealth
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}
```

## Step 2: Start Regional Clusters

Create Surgewave clusters for each region:

```csharp
var regions = new Dictionary<string, RegionConfig>
{
    ["us-east-1"] = new("us-east-1", "US East (N. Virginia)", true, new[] { "us-west-2", "eu-west-1" }),
    ["us-west-2"] = new("us-west-2", "US West (Oregon)", false, new[] { "us-east-1" }),
    ["eu-west-1"] = new("eu-west-1", "EU West (Ireland)", false, new[] { "us-east-1" })
};

var clusters = new Dictionary<string, SurgewaveRuntime>();

foreach (var (regionId, config) in regions)
{
    var runtime = await SurgewaveRuntime.CreateBuilder()
        .WithPort(0)
        .WithStorageEngine(StorageEngines.Memory)
        .WithAutoCreateTopics(true)
        .Build()
        .StartAsync();

    clusters[regionId] = runtime;
    Console.WriteLine($"[{config.DisplayName}] Started on port {runtime.Port} (Primary: {config.IsPrimary})");
}
```

## Step 3: Implement Health Monitoring

Create a health checker for regions:

```csharp
public class RegionHealthMonitor
{
    private readonly Dictionary<string, RegionHealth> _healthStatus = new();
    private readonly Dictionary<string, int> _consecutiveFailures = new();
    private readonly int _failureThreshold;

    public RegionHealthMonitor(int failureThreshold = 3)
    {
        _failureThreshold = failureThreshold;
    }

    public async Task<RegionHealth> CheckHealthAsync(string regionId, string bootstrapServers)
    {
        try
        {
            // Simulate health check (in production, this would be a real connection test)
            await using var producer = new SurgewaveProducer<string, string>(opt =>
            {
                opt.BootstrapServers = bootstrapServers;
                opt.ClientId = $"health-check-{regionId}";
            });

            // If we can create a producer, region is healthy
            _consecutiveFailures[regionId] = 0;
            _healthStatus[regionId] = RegionHealth.Healthy;
            return RegionHealth.Healthy;
        }
        catch
        {
            _consecutiveFailures.TryGetValue(regionId, out var failures);
            _consecutiveFailures[regionId] = failures + 1;

            if (_consecutiveFailures[regionId] >= _failureThreshold)
            {
                _healthStatus[regionId] = RegionHealth.Unhealthy;
                return RegionHealth.Unhealthy;
            }

            _healthStatus[regionId] = RegionHealth.Degraded;
            return RegionHealth.Degraded;
        }
    }

    public RegionHealth GetHealth(string regionId)
        => _healthStatus.GetValueOrDefault(regionId, RegionHealth.Unknown);
}
```

## Step 4: Create Region-Aware Connection Manager

Build a manager that selects the best region:

```csharp
public class MultiRegionConnectionManager
{
    private readonly Dictionary<string, RegionEndpoint> _endpoints;
    private readonly RegionHealthMonitor _healthMonitor;
    private string? _currentRegion;

    public MultiRegionConnectionManager(
        Dictionary<string, RegionEndpoint> endpoints,
        RegionHealthMonitor healthMonitor)
    {
        _endpoints = endpoints;
        _healthMonitor = healthMonitor;
    }

    public RegionEndpoint GetBestEndpoint(string preferredRegion)
    {
        // Try preferred region first if healthy
        if (_endpoints.TryGetValue(preferredRegion, out var preferred) &&
            _healthMonitor.GetHealth(preferredRegion) == RegionHealth.Healthy)
        {
            _currentRegion = preferredRegion;
            return preferred;
        }

        // Fallback to any healthy region with lowest latency
        var healthyEndpoint = _endpoints
            .Where(e => _healthMonitor.GetHealth(e.Key) == RegionHealth.Healthy)
            .OrderBy(e => e.Value.LatencyMs)
            .FirstOrDefault();

        if (healthyEndpoint.Value != null)
        {
            _currentRegion = healthyEndpoint.Key;
            return healthyEndpoint.Value;
        }

        throw new InvalidOperationException("No healthy regions available!");
    }

    public string? CurrentRegion => _currentRegion;
}
```

## Step 5: Simulate Latency Measurement

Measure round-trip time to each region:

```csharp
async Task<int> MeasureLatencyAsync(string bootstrapServers, string topic)
{
    var sw = Stopwatch.StartNew();

    await using var producer = new SurgewaveProducer<string, string>(opt =>
    {
        opt.BootstrapServers = bootstrapServers;
    });

    // Send a test message and measure time
    await producer.ProduceAsync(topic, "latency-test", DateTime.UtcNow.ToString("O"));

    sw.Stop();
    return (int)sw.ElapsedMilliseconds;
}

Console.WriteLine("\n--- Latency Measurement ---");
foreach (var (regionId, runtime) in clusters)
{
    var latency = await MeasureLatencyAsync($"localhost:{runtime.Port}", "latency-test");
    Console.WriteLine($"[{regionId}] Latency: {latency}ms");
}
```

## Step 6: Implement Automatic Failover

Demonstrate failover when primary fails:

```csharp
Console.WriteLine("\n--- Failover Simulation ---");

// Initial state: connect to primary
var primaryEndpoint = connectionManager.GetBestEndpoint("us-east-1");
Console.WriteLine($"Connected to primary: {primaryEndpoint.RegionId}");

// Simulate primary failure
Console.WriteLine("Simulating us-east-1 failure...");
await clusters["us-east-1"].DisposeAsync();
clusters.Remove("us-east-1");

// Update health status
await healthMonitor.CheckHealthAsync("us-east-1", $"localhost:99999"); // Will fail

// Get new endpoint (should failover)
var failoverEndpoint = connectionManager.GetBestEndpoint("us-east-1");
Console.WriteLine($"Failed over to: {failoverEndpoint.RegionId}");

// Produce to new region
await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = failoverEndpoint.BootstrapServers;
});
await producer.ProduceAsync("orders", "failover-order-001", "Order after failover");
Console.WriteLine("Successfully produced to failover region");
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Multi-region cluster deployment
- Health monitoring for each region
- Region-aware connection management
- Latency measurement
- Automatic failover simulation

## Sample Output

```
=== Surgewave Bootcamp: Lesson 27.2 - Multi-Region Deployment ===

--- Starting Regional Clusters ---
[US East (N. Virginia)] Primary started on port 12345
[US West (Oregon)] Secondary started on port 12346
[EU West (Ireland)] Secondary started on port 12347

--- Health Check Results ---
[us-east-1] Health: Healthy
[us-west-2] Health: Healthy
[eu-west-1] Health: Healthy

--- Latency Measurement ---
[us-east-1] Latency: 5ms (simulated: 10ms from US)
[us-west-2] Latency: 8ms (simulated: 50ms from US)
[eu-west-1] Latency: 12ms (simulated: 100ms from US)

--- Region Selection ---
Client in US -> Selected: us-east-1 (lowest latency)
Client in EU -> Selected: eu-west-1 (lowest latency)

--- Normal Operation ---
[us-east-1] Produced 5 orders
[us-east-1] Consumed 5 orders

--- Failover Simulation ---
Simulating us-east-1 failure...
[us-east-1] Health: Unhealthy (connection failed)
Failing over from us-east-1 to us-west-2...
[us-west-2] Failover complete!
[us-west-2] Produced order: failover-order-001

--- Statistics ---
Primary region uptime: 10s
Failover duration: 150ms
Messages during failover: 0 lost

Lesson complete!
```

## Knowledge Check

1. What is the difference between RTO and RPO?
2. When would you choose warm standby over hot standby?
3. Why should replication be asynchronous for cross-region?
4. How do circuit breakers help with region failures?
5. What metrics should you monitor for multi-region deployments?

---

**Next:** [Lesson 27.3: Cross-Cloud Replication](../lesson-3/README.md)
