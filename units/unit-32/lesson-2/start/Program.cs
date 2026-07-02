// Surgewave Bootcamp - Unit 27, Lesson 2: Multi-Region Deployment
//
// In this lesson, you'll learn:
// - Deploying Surgewave across multiple regions
// - Health monitoring and automatic failover
// - Region-aware connection management
// - Latency measurement across regions
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 27.2 - Multi-Region Deployment ===");
Console.WriteLine();

// TODO: Step 1 - Define region configuration
// public record RegionConfig(
//     string RegionId,
//     string DisplayName,
//     bool IsPrimary,
//     string[] ReplicatesTo
// );
//
// var regions = new Dictionary<string, RegionConfig>
// {
//     ["us-east-1"] = new("us-east-1", "US East (N. Virginia)", true, new[] { "us-west-2" }),
//     ["us-west-2"] = new("us-west-2", "US West (Oregon)", false, new[] { "us-east-1" }),
//     ["eu-west-1"] = new("eu-west-1", "EU West (Ireland)", false, new[] { "us-east-1" })
// };

// TODO: Step 2 - Start regional clusters
// var clusters = new Dictionary<string, SurgewaveRuntime>();
//
// Console.WriteLine("--- Starting Regional Clusters ---");
// foreach (var (regionId, config) in regions)
// {
//     var runtime = await SurgewaveRuntime.CreateBuilder()
//         .WithPort(0)
//         .WithStorageEngine(StorageEngines.Memory)
//         .WithAutoCreateTopics(true)
//         .Build()
//         .StartAsync();
//
//     clusters[regionId] = runtime;
//     Console.WriteLine($"[{config.DisplayName}] Started on port {runtime.Port}");
// }

// TODO: Step 3 - Implement health monitoring
// public class RegionHealthMonitor { ... }
// var healthMonitor = new RegionHealthMonitor();
//
// Console.WriteLine("\n--- Health Check Results ---");
// foreach (var (regionId, runtime) in clusters)
// {
//     var health = await healthMonitor.CheckHealthAsync(regionId, $"localhost:{runtime.Port}");
//     Console.WriteLine($"[{regionId}] Health: {health}");
// }

// TODO: Step 4 - Measure latency to each region
// Console.WriteLine("\n--- Latency Measurement ---");
// foreach (var (regionId, runtime) in clusters)
// {
//     var sw = Stopwatch.StartNew();
//     await using var producer = new SurgewaveProducer<string, string>(opt =>
//     {
//         opt.BootstrapServers = $"localhost:{runtime.Port}";
//     });
//     await producer.ProduceAsync("latency-test", "test", "ping");
//     sw.Stop();
//     Console.WriteLine($"[{regionId}] Latency: {sw.ElapsedMilliseconds}ms");
// }

// TODO: Step 5 - Create region-aware connection manager
// public class MultiRegionConnectionManager { ... }
// var connectionManager = new MultiRegionConnectionManager(endpoints, healthMonitor);
// var bestEndpoint = connectionManager.GetBestEndpoint("us-east-1");
// Console.WriteLine($"Selected region: {bestEndpoint.RegionId}");

// TODO: Step 6 - Simulate failover
// Console.WriteLine("\n--- Failover Simulation ---");
// Console.WriteLine("Primary us-east-1 failed...");
// // Dispose primary cluster to simulate failure
// // Get new endpoint (should fail over to secondary)

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Deploy clusters in multiple regions for high availability");
Console.WriteLine("2. Monitor health of each region continuously");
Console.WriteLine("3. Use latency-based routing for best performance");
Console.WriteLine("4. Implement automatic failover with circuit breakers");
Console.WriteLine("5. Pre-provision secondary regions for instant failover");

Console.WriteLine();
Console.WriteLine("Lesson complete!");