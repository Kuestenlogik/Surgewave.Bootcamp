// Surgewave Bootcamp - Unit 27, Lesson 5: Hybrid On-Premise and Cloud
//
// In this lesson, you'll learn:
// - Data classification and sovereignty routing
// - Store-and-forward for network partition resilience
// - Cloud bursting for elastic scaling
// - Edge-to-cloud data pipelines
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 27.5 - Hybrid On-Premise and Cloud ===");
Console.WriteLine();

// TODO: Step 1 - Set up on-premise and cloud clusters
// var onPremise = await SurgewaveRuntime.CreateBuilder()
//     .WithPort(0)
//     .WithStorageMode(StorageMode.Memory)
//     .WithAutoCreateTopics(true)
//     .Build()
//     .StartAsync();
//
// var cloud = await SurgewaveRuntime.CreateBuilder()
//     .WithPort(0)
//     .WithStorageMode(StorageMode.Memory)
//     .WithAutoCreateTopics(true)
//     .Build()
//     .StartAsync();
//
// Console.WriteLine($"On-Premise cluster: port {onPremise.Port}");
// Console.WriteLine($"Cloud cluster: port {cloud.Port}");

// TODO: Step 2 - Implement data classification
// public enum DataSensitivity { Public, Internal, Confidential, Restricted }
//
// public record DataClassification(
//     string TopicPattern,
//     DataSensitivity Sensitivity,
//     bool RequiresEncryption,
//     string[] AllowedRegions
// );
//
// public class DataClassifier { ... }

// TODO: Step 3 - Implement hybrid router with data sensitivity routing
// public class HybridRouter
// {
//     public async Task RouteMessageAsync(string topic, string key, string value)
//     {
//         var classification = _classifier.Classify(topic);
//         // Route based on sensitivity...
//     }
// }

// TODO: Step 4 - Implement store-and-forward agent
// public class StoreAndForwardAgent
// {
//     private readonly Queue<PendingMessage> _buffer = new();
//
//     public async Task SendToCloudAsync(string topic, string key, string value,
//         SurgewaveProducer<string, string> cloudProducer) { ... }
//
//     // Handle network partition
//     // Buffer messages when cloud is unavailable
//     // Flush buffer when connectivity is restored
// }

// TODO: Step 5 - Implement cloud bursting
// public class CloudBurstController
// {
//     public bool ShouldBurst(double currentRate) { ... }
// }

// TODO: Step 6 - Run end-to-end simulation
// - Send mixed workload with different sensitivity levels
// - Simulate network partition
// - Simulate load spike for cloud bursting
// - Print audit report

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Classify data by sensitivity before routing");
Console.WriteLine("2. Keep confidential/restricted data on-premise");
Console.WriteLine("3. Use store-and-forward for network partition resilience");
Console.WriteLine("4. Cloud bursting provides elastic scaling for peak loads");
Console.WriteLine("5. Always audit cross-boundary data movement");

Console.WriteLine();
Console.WriteLine("Lesson complete!");