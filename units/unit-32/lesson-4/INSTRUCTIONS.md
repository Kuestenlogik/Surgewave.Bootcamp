# Lesson 27.4: Instructions

Follow these step-by-step instructions to implement geo-routing and latency optimization.

## Step 1: Define Geo-Routing Configuration

Create data structures for geo-aware routing:

```csharp
public record GeoEndpoint(
    string Region,
    string BootstrapServers,
    string Rack,              // Availability zone
    int BaseLatencyMs,        // Expected latency from this region
    GeoLocation Location      // Lat/Long for distance calculation
);

public record GeoLocation(double Latitude, double Longitude);

public record ClientContext(
    string ClientId,
    string ClientRegion,
    GeoLocation? Location
);
```

## Step 2: Implement Latency-Based Router

Create a router that selects the lowest-latency endpoint:

```csharp
public class LatencyBasedRouter
{
    private readonly Dictionary<string, GeoEndpoint> _endpoints;
    private readonly Dictionary<string, int> _measuredLatencies;
    private readonly int _probeIntervalMs;

    public LatencyBasedRouter(IEnumerable<GeoEndpoint> endpoints, int probeIntervalMs = 30000)
    {
        _endpoints = endpoints.ToDictionary(e => e.Region);
        _measuredLatencies = new Dictionary<string, int>();
        _probeIntervalMs = probeIntervalMs;
    }

    public async Task<GeoEndpoint> GetBestEndpointAsync(ClientContext client)
    {
        // Measure latency to all endpoints
        var latencyTasks = _endpoints.Values.Select(async endpoint =>
        {
            var latency = await MeasureLatencyAsync(endpoint);
            return (endpoint, latency);
        });

        var results = await Task.WhenAll(latencyTasks);

        // Select endpoint with lowest latency
        var best = results.OrderBy(r => r.latency).First();

        Console.WriteLine($"[Router] Best endpoint for {client.ClientRegion}: " +
            $"{best.endpoint.Region} ({best.latency}ms)");

        return best.endpoint;
    }

    private async Task<int> MeasureLatencyAsync(GeoEndpoint endpoint)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // Simulate connection test
            await using var producer = new SurgewaveProducer<string, string>(opt =>
            {
                opt.BootstrapServers = endpoint.BootstrapServers;
                opt.ClientId = "latency-probe";
            });
            sw.Stop();

            // Add simulated network latency based on distance
            var totalLatency = (int)sw.ElapsedMilliseconds + endpoint.BaseLatencyMs;
            _measuredLatencies[endpoint.Region] = totalLatency;
            return totalLatency;
        }
        catch
        {
            return int.MaxValue; // Unreachable
        }
    }
}
```

## Step 3: Implement Geolocation-Based Router

Create a router based on geographic location:

```csharp
public class GeoLocationRouter
{
    private readonly Dictionary<string, List<string>> _regionMapping;
    private readonly Dictionary<string, GeoEndpoint> _endpoints;

    public GeoLocationRouter(
        IEnumerable<GeoEndpoint> endpoints,
        Dictionary<string, List<string>> regionMapping)
    {
        _endpoints = endpoints.ToDictionary(e => e.Region);
        _regionMapping = regionMapping;
    }

    public GeoEndpoint GetEndpoint(ClientContext client)
    {
        // Find matching region based on client's country/region
        foreach (var (surgewaveRegion, countries) in _regionMapping)
        {
            if (countries.Contains(client.ClientRegion))
            {
                return _endpoints[surgewaveRegion];
            }
        }

        // Default to first available
        return _endpoints.Values.First();
    }

    // Alternative: Calculate nearest by geographic distance
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
        // Haversine formula for great-circle distance
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
```

## Step 4: Implement Latency Tracker

Track and analyze latency distribution:

```csharp
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
                P99: sorted[(int)(count * 0.99)]
            );
        }
    }
}

public record LatencyStats(int Count, long Min, long Max, long Avg, long P50, long P99);
```

## Step 5: Configure Rack-Aware Consumer

Set up a consumer with rack awareness:

```csharp
// Simulated rack-aware consumer configuration
var rackConfig = new RackAwareConfig
{
    ClientRack = "us-east-1a",
    FetchFromFollower = true,
    PreferLocalRack = true
};

Console.WriteLine($"\n--- Rack-Aware Configuration ---");
Console.WriteLine($"Client Rack: {rackConfig.ClientRack}");
Console.WriteLine($"Fetch from Follower: {rackConfig.FetchFromFollower}");
Console.WriteLine($"Prefer Local Rack: {rackConfig.PreferLocalRack}");

await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bestEndpoint.BootstrapServers;
    opt.GroupId = "geo-aware-consumer";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
    // In production, you would set:
    // opt.ClientRack = rackConfig.ClientRack;
    // opt.FetchFromFollower = rackConfig.FetchFromFollower;
});
```

## Step 6: Compare Performance With/Without Optimization

Run benchmarks to measure improvement:

```csharp
Console.WriteLine("\n--- Performance Comparison ---");

// Without geo-routing (random selection)
var randomEndpoint = endpoints[Random.Shared.Next(endpoints.Count)];
var withoutGeoStats = await BenchmarkEndpointAsync(randomEndpoint, "Without Geo-Routing");

// With geo-routing (nearest)
var bestEndpoint = await router.GetBestEndpointAsync(clientContext);
var withGeoStats = await BenchmarkEndpointAsync(bestEndpoint, "With Geo-Routing");

Console.WriteLine($"\nImprovement:");
Console.WriteLine($"  Avg Latency: {withoutGeoStats.Avg - withGeoStats.Avg}ms faster");
Console.WriteLine($"  P99 Latency: {withoutGeoStats.P99 - withGeoStats.P99}ms faster");

async Task<LatencyStats> BenchmarkEndpointAsync(GeoEndpoint endpoint, string label)
{
    var tracker = new LatencyTracker();

    await using var producer = new SurgewaveProducer<string, string>(opt =>
    {
        opt.BootstrapServers = endpoint.BootstrapServers;
    });

    for (int i = 0; i < 100; i++)
    {
        var sw = Stopwatch.StartNew();
        await producer.ProduceAsync("benchmark", $"msg-{i}", "test");
        sw.Stop();

        // Add simulated network latency
        var totalLatency = sw.ElapsedMilliseconds + endpoint.BaseLatencyMs;
        tracker.Record(totalLatency);
    }

    var stats = tracker.GetStats();
    Console.WriteLine($"{label} ({endpoint.Region}): Avg={stats.Avg}ms, P50={stats.P50}ms, P99={stats.P99}ms");
    return stats;
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Latency-based endpoint selection
- Geolocation-based routing with distance calculation
- Latency tracking with percentiles
- Rack-aware consumer configuration
- Performance benchmarking

## Sample Output

```
=== Surgewave Bootcamp: Lesson 27.4 - Geo-Routing and Latency Optimization ===

--- Setting Up Regional Clusters ---
[us-east-1] Started on port 12345 (Rack: us-east-1a)
[eu-west-1] Started on port 12346 (Rack: eu-west-1a)
[ap-southeast-1] Started on port 12347 (Rack: ap-southeast-1a)

--- Geo Endpoints ---
us-east-1: Base latency 10ms, Location: (39.0438, -77.4874)
eu-west-1: Base latency 80ms (from US), Location: (53.3498, -6.2603)
ap-southeast-1: Base latency 200ms (from US), Location: (1.3521, 103.8198)

--- Latency-Based Routing ---
[Router] Measuring latency to us-east-1... 12ms
[Router] Measuring latency to eu-west-1... 85ms
[Router] Measuring latency to ap-southeast-1... 205ms
[Router] Best endpoint for US client: us-east-1 (12ms)

--- Geolocation-Based Routing ---
Client in US -> Endpoint: us-east-1
Client in UK -> Endpoint: eu-west-1
Client in Japan -> Endpoint: ap-southeast-1

--- Rack-Aware Configuration ---
Client Rack: us-east-1a
Fetch from Follower: true
Prefer Local Rack: true

--- Performance Comparison ---
Without Geo-Routing (eu-west-1): Avg=87ms, P50=85ms, P99=120ms
With Geo-Routing (us-east-1): Avg=15ms, P50=12ms, P99=25ms

Improvement:
  Avg Latency: 72ms faster
  P99 Latency: 95ms faster

--- Latency Distribution ---
us-east-1: Min=8ms, Max=25ms, Avg=12ms, P50=11ms, P99=22ms
eu-west-1: Min=75ms, Max=130ms, Avg=87ms, P50=85ms, P99=120ms
ap-southeast-1: Min=190ms, Max=280ms, Avg=210ms, P50=205ms, P99=260ms

Lesson complete!
```

## Knowledge Check

1. What is the advantage of latency-based routing over geolocation-based?
2. How does rack awareness reduce costs in cloud deployments?
3. Why might P99 latency be more important than average latency?
4. When should you enable follower fetching?
5. What trade-off does batching introduce for latency?

---

**Next:** [Lesson 27.5: Hybrid On-Premise and Cloud](../lesson-5/README.md)
