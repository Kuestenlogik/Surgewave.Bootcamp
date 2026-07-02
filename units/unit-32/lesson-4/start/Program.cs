// Surgewave Bootcamp - Unit 27, Lesson 4: Geo-Routing and Latency Optimization
//
// In this lesson, you'll learn:
// - Implementing latency-based routing
// - Configuring rack awareness
// - Measuring latency distribution
// - Optimizing for low-latency workloads
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 27.4 - Geo-Routing and Latency Optimization ===");
Console.WriteLine();

// TODO: Step 1 - Define geo-routing configuration
// public record GeoEndpoint(
//     string Region,
//     string BootstrapServers,
//     string Rack,
//     int BaseLatencyMs,
//     GeoLocation Location
// );
// public record GeoLocation(double Latitude, double Longitude);

// TODO: Step 2 - Set up regional clusters with geo information
// var endpoints = new List<GeoEndpoint>();
// Console.WriteLine("--- Setting Up Regional Clusters ---");
//
// var usEast = await SurgewaveRuntime.CreateBuilder()
//     .WithPort(0)
//     .WithStorageEngine(StorageEngines.Memory)
//     .WithAutoCreateTopics(true)
//     .Build()
//     .StartAsync();
// endpoints.Add(new GeoEndpoint("us-east-1", $"localhost:{usEast.Port}",
//     "us-east-1a", 10, new GeoLocation(39.0438, -77.4874)));

// TODO: Step 3 - Implement latency-based router
// public class LatencyBasedRouter { ... }
// var router = new LatencyBasedRouter(endpoints);
// var bestEndpoint = await router.GetBestEndpointAsync(clientContext);

// TODO: Step 4 - Implement latency tracker
// public class LatencyTracker { ... }
// var tracker = new LatencyTracker();
// tracker.Record(latencyMs);
// var stats = tracker.GetStats();

// TODO: Step 5 - Configure rack-aware consumer
// await using var consumer = new SurgewaveConsumer<string, string>(opt =>
// {
//     opt.BootstrapServers = bestEndpoint.BootstrapServers;
//     opt.GroupId = "geo-aware-consumer";
//     // opt.ClientRack = "us-east-1a";
//     // opt.FetchFromFollower = true;
// });

// TODO: Step 6 - Compare performance with/without optimization
// var withoutGeoStats = await BenchmarkEndpointAsync(randomEndpoint);
// var withGeoStats = await BenchmarkEndpointAsync(bestEndpoint);
// Console.WriteLine($"Improvement: {withoutGeoStats.Avg - withGeoStats.Avg}ms faster");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use latency-based routing to adapt to network conditions");
Console.WriteLine("2. Enable rack awareness to reduce cross-AZ costs");
Console.WriteLine("3. Monitor P99 latency, not just averages");
Console.WriteLine("4. Consider follower fetching for read-heavy workloads");
Console.WriteLine("5. Tune batching based on latency vs throughput requirements");

Console.WriteLine();
Console.WriteLine("Lesson complete!");