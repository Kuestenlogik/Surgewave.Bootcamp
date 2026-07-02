// Surgewave Bootcamp - Unit 27, Lesson 4: Geo-Routing and Latency Optimization
// COMPLETED SOLUTION

using System.Diagnostics;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 27.4 - Geo-Routing and Latency Optimization ===");
Console.WriteLine();

// Step 1 - Set up regional clusters with geo information
var clusters = new Dictionary<string, SurgewaveRuntime>();
var endpoints = new List<GeoEndpoint>();

Console.WriteLine("--- Setting Up Regional Clusters ---");

// US East
var usEast = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
clusters["us-east-1"] = usEast;
endpoints.Add(new GeoEndpoint("us-east-1", $"localhost:{usEast.Port}",
    "us-east-1a", 10, new GeoLocation(39.0438, -77.4874)));
Console.WriteLine($"[us-east-1] Started on port {usEast.Port} (Rack: us-east-1a)");

// EU West
var euWest = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
clusters["eu-west-1"] = euWest;
endpoints.Add(new GeoEndpoint("eu-west-1", $"localhost:{euWest.Port}",
    "eu-west-1a", 80, new GeoLocation(53.3498, -6.2603)));
Console.WriteLine($"[eu-west-1] Started on port {euWest.Port} (Rack: eu-west-1a)");

// AP Southeast
var apSoutheast = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
clusters["ap-southeast-1"] = apSoutheast;
endpoints.Add(new GeoEndpoint("ap-southeast-1", $"localhost:{apSoutheast.Port}",
    "ap-southeast-1a", 200, new GeoLocation(1.3521, 103.8198)));
Console.WriteLine($"[ap-southeast-1] Started on port {apSoutheast.Port} (Rack: ap-southeast-1a)");

// Step 2 - Display geo endpoints
Console.WriteLine("\n--- Geo Endpoints ---");
foreach (var endpoint in endpoints)
{
    Console.WriteLine($"{endpoint.Region}: Base latency {endpoint.BaseLatencyMs}ms (from US), " +
        $"Location: ({endpoint.Location.Latitude:F4}, {endpoint.Location.Longitude:F4})");
}

// Step 3 - Latency-based routing
Console.WriteLine("\n--- Latency-Based Routing ---");
var router = new LatencyBasedRouter(endpoints);

// Simulate US client
var usClient = new ClientContext("client-001", "US", new GeoLocation(40.7128, -74.0060));
var bestForUs = await router.GetBestEndpointAsync(usClient);

// Step 4 - Geolocation-based routing
Console.WriteLine("\n--- Geolocation-Based Routing ---");
var geoRouter = new GeoLocationRouter(endpoints, new Dictionary<string, List<string>>
{
    ["us-east-1"] = new() { "US", "CA", "MX", "BR" },
    ["eu-west-1"] = new() { "UK", "DE", "FR", "ES", "IT" },
    ["ap-southeast-1"] = new() { "JP", "AU", "SG", "KR", "IN" }
});

var testClients = new[] { ("US", "US"), ("UK", "EU"), ("JP", "APAC") };
foreach (var (country, label) in testClients)
{
    var client = new ClientContext($"client-{country}", country, null);
    var endpoint = geoRouter.GetEndpoint(client);
    Console.WriteLine($"Client in {country} -> Endpoint: {endpoint.Region}");
}

// Step 5 - Rack-aware configuration
var rackConfig = new RackAwareConfig(
    ClientRack: "us-east-1a",
    FetchFromFollower: true,
    PreferLocalRack: true
);

Console.WriteLine($"\n--- Rack-Aware Configuration ---");
Console.WriteLine($"Client Rack: {rackConfig.ClientRack}");
Console.WriteLine($"Fetch from Follower: {rackConfig.FetchFromFollower}");
Console.WriteLine($"Prefer Local Rack: {rackConfig.PreferLocalRack}");

// Step 6 - Performance comparison
Console.WriteLine("\n--- Performance Comparison ---");

// Without geo-routing (use distant region)
var distantEndpoint = endpoints.First(e => e.Region == "ap-southeast-1");
var withoutGeoStats = await BenchmarkEndpointAsync(distantEndpoint, "Without Geo-Routing");

// With geo-routing (use nearest)
var withGeoStats = await BenchmarkEndpointAsync(bestForUs, "With Geo-Routing");

Console.WriteLine($"\nImprovement:");
Console.WriteLine($"  Avg Latency: {withoutGeoStats.Avg - withGeoStats.Avg}ms faster");
Console.WriteLine($"  P99 Latency: {withoutGeoStats.P99 - withGeoStats.P99}ms faster");

// Step 7 - Latency distribution analysis
Console.WriteLine("\n--- Latency Distribution ---");
foreach (var endpoint in endpoints)
{
    var stats = await MeasureEndpointLatencyAsync(endpoint);
    Console.WriteLine($"{endpoint.Region}: Min={stats.Min}ms, Max={stats.Max}ms, " +
        $"Avg={stats.Avg}ms, P50={stats.P50}ms, P99={stats.P99}ms");
}

// Cleanup
foreach (var (_, runtime) in clusters)
{
    await runtime.DisposeAsync();
}

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use latency-based routing to adapt to network conditions");
Console.WriteLine("2. Enable rack awareness to reduce cross-AZ costs");
Console.WriteLine("3. Monitor P99 latency, not just averages");
Console.WriteLine("4. Consider follower fetching for read-heavy workloads");
Console.WriteLine("5. Tune batching based on latency vs throughput requirements");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Helper methods

async Task<LatencyStats> BenchmarkEndpointAsync(GeoEndpoint endpoint, string label)
{
    var tracker = new LatencyTracker();

    await using var producer = new SurgewaveProducer<string, string>(opt =>
    {
        opt.BootstrapServers = endpoint.BootstrapServers;
        opt.ClientId = "benchmark-producer";
    });

    for (int i = 0; i < 100; i++)
    {
        var sw = Stopwatch.StartNew();
        await producer.ProduceAsync("benchmark", $"msg-{i}", "test-payload");
        sw.Stop();

        // Add simulated network latency
        var totalLatency = sw.ElapsedMilliseconds + endpoint.BaseLatencyMs;
        tracker.Record(totalLatency);
    }

    var stats = tracker.GetStats();
    Console.WriteLine($"{label} ({endpoint.Region}): Avg={stats.Avg}ms, P50={stats.P50}ms, P99={stats.P99}ms");
    return stats;
}

async Task<LatencyStats> MeasureEndpointLatencyAsync(GeoEndpoint endpoint)
{
    var tracker = new LatencyTracker();

    await using var producer = new SurgewaveProducer<string, string>(opt =>
    {
        opt.BootstrapServers = endpoint.BootstrapServers;
    });

    for (int i = 0; i < 50; i++)
    {
        var sw = Stopwatch.StartNew();
        await producer.ProduceAsync("latency-test", $"msg-{i}", "test");
        sw.Stop();

        var totalLatency = sw.ElapsedMilliseconds + endpoint.BaseLatencyMs;
        tracker.Record(totalLatency);
    }

    return tracker.GetStats();
}

// Supporting types

public record GeoEndpoint(
    string Region,
    string BootstrapServers,
    string Rack,
    int BaseLatencyMs,
    GeoLocation Location
);

public record GeoLocation(double Latitude, double Longitude);

public record ClientContext(
    string ClientId,
    string ClientRegion,
    GeoLocation? Location
);

public record RackAwareConfig(
    string ClientRack,
    bool FetchFromFollower,
    bool PreferLocalRack
);

public class LatencyBasedRouter
{
    private readonly List<GeoEndpoint> _endpoints;
    private readonly Dictionary<string, int> _measuredLatencies = new();

    public LatencyBasedRouter(IEnumerable<GeoEndpoint> endpoints)
    {
        _endpoints = endpoints.ToList();
    }

    public async Task<GeoEndpoint> GetBestEndpointAsync(ClientContext client)
    {
        var results = new List<(GeoEndpoint Endpoint, int Latency)>();

        foreach (var endpoint in _endpoints)
        {
            var latency = await MeasureLatencyAsync(endpoint);
            results.Add((endpoint, latency));
            Console.WriteLine($"[Router] Measuring latency to {endpoint.Region}... {latency}ms");
        }

        var best = results.OrderBy(r => r.Latency).First();
        Console.WriteLine($"[Router] Best endpoint for {client.ClientRegion} client: {best.Endpoint.Region} ({best.Latency}ms)");

        return best.Endpoint;
    }

    private async Task<int> MeasureLatencyAsync(GeoEndpoint endpoint)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await using var producer = new SurgewaveProducer<string, string>(opt =>
            {
                opt.BootstrapServers = endpoint.BootstrapServers;
                opt.ClientId = "latency-probe";
            });
            sw.Stop();

            return (int)sw.ElapsedMilliseconds + endpoint.BaseLatencyMs;
        }
        catch
        {
            return int.MaxValue;
        }
    }
}

public class GeoLocationRouter
{
    private readonly Dictionary<string, GeoEndpoint> _endpoints;
    private readonly Dictionary<string, List<string>> _regionMapping;

    public GeoLocationRouter(
        IEnumerable<GeoEndpoint> endpoints,
        Dictionary<string, List<string>> regionMapping)
    {
        _endpoints = endpoints.ToDictionary(e => e.Region);
        _regionMapping = regionMapping;
    }

    public GeoEndpoint GetEndpoint(ClientContext client)
    {
        foreach (var (surgewaveRegion, countries) in _regionMapping)
        {
            if (countries.Contains(client.ClientRegion))
            {
                return _endpoints[surgewaveRegion];
            }
        }

        return _endpoints.Values.First();
    }

    public GeoEndpoint GetNearestEndpoint(ClientContext client)
    {
        if (client.Location == null)
            return _endpoints.Values.First();

        return _endpoints.Values
            .OrderBy(e => CalculateDistance(client.Location, e.Location))
            .First();
    }

    private double CalculateDistance(GeoLocation a, GeoLocation b)
    {
        var R = 6371; // Earth radius in km
        var dLat = ToRadians(b.Latitude - a.Latitude);
        var dLon = ToRadians(b.Longitude - a.Longitude);

        var lat1 = ToRadians(a.Latitude);
        var lat2 = ToRadians(b.Latitude);

        var x = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) *
                Math.Cos(lat1) * Math.Cos(lat2);
        var c = 2 * Math.Atan2(Math.Sqrt(x), Math.Sqrt(1 - x));

        return R * c;
    }

    private double ToRadians(double deg) => deg * Math.PI / 180;
}

public class LatencyTracker
{
    private readonly List<long> _samples = new();
    private readonly int _maxSamples;
    private readonly object _lock = new();

    public LatencyTracker(int maxSamples = 10000)
    {
        _maxSamples = maxSamples;
    }

    public void Record(long latencyMs)
    {
        lock (_lock)
        {
            _samples.Add(latencyMs);
            if (_samples.Count > _maxSamples)
            {
                _samples.RemoveAt(0);
            }
        }
    }

    public LatencyStats GetStats()
    {
        lock (_lock)
        {
            if (_samples.Count == 0)
                return new LatencyStats(0, 0, 0, 0, 0, 0);

            var sorted = _samples.OrderBy(x => x).ToList();
            var count = sorted.Count;

            return new LatencyStats(
                Count: count,
                Min: sorted.First(),
                Max: sorted.Last(),
                Avg: (long)sorted.Average(),
                P50: sorted[count / 2],
                P99: sorted[Math.Min((int)(count * 0.99), count - 1)]
            );
        }
    }
}

public record LatencyStats(int Count, long Min, long Max, long Avg, long P50, long P99);
