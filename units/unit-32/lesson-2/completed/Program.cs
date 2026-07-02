// Surgewave Bootcamp - Unit 27, Lesson 2: Multi-Region Deployment
// COMPLETED SOLUTION

using System.Diagnostics;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 27.2 - Multi-Region Deployment ===");
Console.WriteLine();

// Step 1 - Define region configuration
var regions = new Dictionary<string, RegionConfig>
{
    ["us-east-1"] = new("us-east-1", "US East (N. Virginia)", true, new[] { "us-west-2", "eu-west-1" }),
    ["us-west-2"] = new("us-west-2", "US West (Oregon)", false, new[] { "us-east-1" }),
    ["eu-west-1"] = new("eu-west-1", "EU West (Ireland)", false, new[] { "us-east-1" })
};

// Step 2 - Start regional clusters
var clusters = new Dictionary<string, SurgewaveRuntime>();
var endpoints = new Dictionary<string, RegionEndpoint>();

Console.WriteLine("--- Starting Regional Clusters ---");
foreach (var (regionId, config) in regions)
{
    var runtime = await SurgewaveRuntime.CreateBuilder()
        .WithPort(0)
        .WithStorageEngine(StorageEngines.Memory)
        .WithAutoCreateTopics(true)
        .Build()
        .StartAsync();

    clusters[regionId] = runtime;

    // Simulate different latencies based on region
    var simulatedLatency = regionId switch
    {
        "us-east-1" => 10,
        "us-west-2" => 50,
        "eu-west-1" => 100,
        _ => 200
    };

    endpoints[regionId] = new RegionEndpoint(
        regionId,
        $"localhost:{runtime.Port}",
        RegionHealth.Unknown,
        simulatedLatency
    );

    var primaryLabel = config.IsPrimary ? " (Primary)" : " (Secondary)";
    Console.WriteLine($"[{config.DisplayName}]{primaryLabel} Started on port {runtime.Port}");
}

// Step 3 - Implement health monitoring
var healthMonitor = new RegionHealthMonitor(failureThreshold: 3);

Console.WriteLine("\n--- Health Check Results ---");
foreach (var (regionId, endpoint) in endpoints)
{
    var health = await healthMonitor.CheckHealthAsync(regionId, endpoint.BootstrapServers);
    endpoints[regionId] = endpoint with { Health = health };
    Console.WriteLine($"[{regionId}] Health: {health}");
}

// Step 4 - Measure latency to each region
Console.WriteLine("\n--- Latency Measurement ---");
var latencyResults = new Dictionary<string, int>();

foreach (var (regionId, runtime) in clusters)
{
    var sw = Stopwatch.StartNew();

    await using var producer = new SurgewaveProducer<string, string>(opt =>
    {
        opt.BootstrapServers = $"localhost:{runtime.Port}";
        opt.ClientId = $"latency-test-{regionId}";
    });

    await producer.ProduceAsync("latency-test", "test", DateTime.UtcNow.ToString("O"));
    sw.Stop();

    var measuredLatency = (int)sw.ElapsedMilliseconds;
    var simulatedLatency = endpoints[regionId].LatencyMs;
    latencyResults[regionId] = measuredLatency;

    Console.WriteLine($"[{regionId}] Measured: {measuredLatency}ms (Simulated from US: {simulatedLatency}ms)");
}

// Step 5 - Create region-aware connection manager
var connectionManager = new MultiRegionConnectionManager(endpoints, healthMonitor);

Console.WriteLine("\n--- Region Selection ---");

// Simulate clients from different locations
var clientLocations = new[] { ("US", "us-east-1"), ("EU", "eu-west-1"), ("APAC", "us-west-2") };

foreach (var (location, preferredRegion) in clientLocations)
{
    var selected = connectionManager.GetBestEndpoint(preferredRegion);
    Console.WriteLine($"Client in {location} -> Selected: {selected.RegionId} ({selected.LatencyMs}ms simulated latency)");
}

// Step 6 - Normal operation: produce and consume
Console.WriteLine("\n--- Normal Operation ---");

var primaryEndpoint = connectionManager.GetBestEndpoint("us-east-1");
Console.WriteLine($"Operating on primary: {primaryEndpoint.RegionId}");

await using var primaryProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = primaryEndpoint.BootstrapServers;
    opt.ClientId = "primary-producer";
});

var ordersTopic = "orders";
var ordersProduced = 0;

for (int i = 1; i <= 5; i++)
{
    var order = new { OrderId = $"ORD-{i:D3}", Amount = 100 * i, Region = "us-east" };
    await primaryProducer.ProduceAsync(ordersTopic, order.OrderId, JsonSerializer.Serialize(order));
    ordersProduced++;
}
Console.WriteLine($"[{primaryEndpoint.RegionId}] Produced {ordersProduced} orders");

await using var primaryConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = primaryEndpoint.BootstrapServers;
    opt.GroupId = "order-processor";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
primaryConsumer.Subscribe(ordersTopic);

var ordersConsumed = 0;
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
try
{
    while (!cts.IsCancellationRequested && ordersConsumed < ordersProduced)
    {
        var result = await primaryConsumer.ConsumeAsync(cts.Token);
        if (result != null) ordersConsumed++;
    }
}
catch (OperationCanceledException) { }
Console.WriteLine($"[{primaryEndpoint.RegionId}] Consumed {ordersConsumed} orders");

// Step 7 - Simulate failover
Console.WriteLine("\n--- Failover Simulation ---");
Console.WriteLine("Simulating us-east-1 failure...");

// Record failover start time
var failoverStart = Stopwatch.StartNew();

// Dispose primary to simulate failure
await clusters["us-east-1"].DisposeAsync();
clusters.Remove("us-east-1");

// Mark as unhealthy (simulate detection)
healthMonitor.MarkUnhealthy("us-east-1");
endpoints["us-east-1"] = endpoints["us-east-1"] with { Health = RegionHealth.Unhealthy };

Console.WriteLine($"[us-east-1] Health: Unhealthy (region failed)");

// Get new endpoint (will failover)
RegionEndpoint failoverEndpoint;
try
{
    failoverEndpoint = connectionManager.GetBestEndpoint("us-east-1");
    failoverStart.Stop();

    Console.WriteLine($"Failing over from us-east-1 to {failoverEndpoint.RegionId}...");
    Console.WriteLine($"[{failoverEndpoint.RegionId}] Failover complete! (took {failoverStart.ElapsedMilliseconds}ms)");
}
catch (Exception ex)
{
    Console.WriteLine($"Failover failed: {ex.Message}");
    return;
}

// Produce to failover region
await using var failoverProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = failoverEndpoint.BootstrapServers;
    opt.ClientId = "failover-producer";
});

var failoverOrder = new { OrderId = "FAILOVER-001", Amount = 999, Region = failoverEndpoint.RegionId };
await failoverProducer.ProduceAsync(ordersTopic, failoverOrder.OrderId, JsonSerializer.Serialize(failoverOrder));
Console.WriteLine($"[{failoverEndpoint.RegionId}] Produced order: {failoverOrder.OrderId}");

// Step 8 - Display statistics
Console.WriteLine("\n--- Statistics ---");
Console.WriteLine($"Primary region uptime: ~5 seconds (simulated)");
Console.WriteLine($"Failover duration: {failoverStart.ElapsedMilliseconds}ms");
Console.WriteLine($"Messages produced before failover: {ordersProduced}");
Console.WriteLine($"Messages consumed before failover: {ordersConsumed}");
Console.WriteLine($"Messages during failover: 0 lost (async replication assumed)");

// Cleanup remaining clusters
foreach (var (regionId, runtime) in clusters)
{
    await runtime.DisposeAsync();
}

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Deploy clusters in multiple regions for high availability");
Console.WriteLine("2. Monitor health of each region continuously");
Console.WriteLine("3. Use latency-based routing for best performance");
Console.WriteLine("4. Implement automatic failover with circuit breakers");
Console.WriteLine("5. Pre-provision secondary regions for instant failover");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Supporting types

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
            // Simulate health check with a producer connection test
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

    public void MarkUnhealthy(string regionId)
    {
        _healthStatus[regionId] = RegionHealth.Unhealthy;
        _consecutiveFailures[regionId] = _failureThreshold;
    }

    public RegionHealth GetHealth(string regionId)
        => _healthStatus.GetValueOrDefault(regionId, RegionHealth.Unknown);
}

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