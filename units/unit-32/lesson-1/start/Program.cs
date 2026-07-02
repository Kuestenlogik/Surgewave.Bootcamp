// Surgewave Bootcamp - Unit 27, Lesson 1: Hybrid Cloud Concepts
//
// In this lesson, you'll learn:
// - Hybrid cloud architecture patterns
// - Multi-cluster deployment modeling
// - Regional topic naming conventions
// - Cluster selection based on proximity
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 27.1 - Hybrid Cloud Concepts ===");
Console.WriteLine();

// TODO: Step 1 - Define cluster configuration models
// public record ClusterConfig(
//     string ClusterId,
//     string Region,
//     string CloudProvider,
//     int Port,
//     ClusterRole Role
// );
//
// public enum ClusterRole { Primary, Secondary, Edge }

// TODO: Step 2 - Start multiple simulated clusters
// var clusters = new Dictionary<string, SurgewaveRuntime>();
//
// Console.WriteLine("--- Starting Multi-Region Clusters ---");
//
// // US East (Primary)
// var usEast = await SurgewaveRuntime.CreateBuilder()
//     .WithPort(0)
//     .WithStorageEngine(StorageEngines.Memory)
//     .WithAutoCreateTopics(true)
//     .Build()
//     .StartAsync();
// clusters["us-east-1"] = usEast;
// Console.WriteLine($"[US East] Primary cluster started on port {usEast.Port}");
//
// // EU West (Secondary)
// var euWest = await SurgewaveRuntime.CreateBuilder()
//     .WithPort(0)
//     .WithStorageEngine(StorageEngines.Memory)
//     .WithAutoCreateTopics(true)
//     .Build()
//     .StartAsync();
// clusters["eu-west-1"] = euWest;
// Console.WriteLine($"[EU West] Secondary cluster started on port {euWest.Port}");

// TODO: Step 3 - Implement regional topic naming
// Console.WriteLine("\n--- Topic Naming Conventions ---");
// var usOrdersTopic = $"us-east.orders.created";
// var euOrdersTopic = $"eu-west.orders.created";
// var globalConfigTopic = $"global.config.updates";
// Console.WriteLine($"Regional (US): {usOrdersTopic}");
// Console.WriteLine($"Regional (EU): {euOrdersTopic}");
// Console.WriteLine($"Global: {globalConfigTopic}");

// TODO: Step 4 - Create regional producers
// await using var usProducer = new SurgewaveProducer<string, string>(opt =>
// {
//     opt.BootstrapServers = $"localhost:{usEast.Port}";
//     opt.ClientId = "producer-us-east";
// });
//
// await using var euProducer = new SurgewaveProducer<string, string>(opt =>
// {
//     opt.BootstrapServers = $"localhost:{euWest.Port}";
//     opt.ClientId = "producer-eu-west";
// });
//
// Console.WriteLine("\n--- Regional Message Production ---");
// await usProducer.ProduceAsync(usOrdersTopic, "US-001",
//     JsonSerializer.Serialize(new { OrderId = "US-001", Region = "us-east", Amount = 99.99 }));
// Console.WriteLine("[us-east-1] Produced order US-001");
//
// await euProducer.ProduceAsync(euOrdersTopic, "EU-001",
//     JsonSerializer.Serialize(new { OrderId = "EU-001", Region = "eu-west", Amount = 149.99 }));
// Console.WriteLine("[eu-west-1] Produced order EU-001");

// TODO: Step 5 - Implement cluster selection logic
// public class ClusterLocator { ... }
// Console.WriteLine("\n--- Cluster Selection Demo ---");
// Console.WriteLine("Client in US -> Connect to: us-east-1");
// Console.WriteLine("Client in EU -> Connect to: eu-west-1");

// TODO: Step 6 - Track cluster statistics
// Console.WriteLine("\n--- Cluster Statistics ---");
// Console.WriteLine("us-east-1: Produced=X, Consumed=X");
// Console.WriteLine("eu-west-1: Produced=X, Consumed=X");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Hybrid cloud provides resilience and low latency");
Console.WriteLine("2. Use regional topic prefixes for clear data locality");
Console.WriteLine("3. Producers should connect to nearest cluster");
Console.WriteLine("4. Plan topology based on use case (Active-Passive vs Active-Active)");
Console.WriteLine("5. Monitor cluster health and replication lag");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
