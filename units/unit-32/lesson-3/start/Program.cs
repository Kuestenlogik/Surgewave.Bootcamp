// Surgewave Bootcamp - Unit 27, Lesson 3: Cross-Cloud Replication
//
// In this lesson, you'll learn:
// - Setting up replication between cloud providers
// - Handling bidirectional replication
// - Conflict detection and resolution
// - Topic filtering during replication
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 27.3 - Cross-Cloud Replication ===");
Console.WriteLine();

// TODO: Step 1 - Define replication configuration
// public record ReplicationConfig(
//     string SourceCluster,
//     string TargetCluster,
//     ReplicationMode Mode,
//     string[] IncludeTopics,
//     string[] ExcludeTopics,
//     ConflictResolution ConflictStrategy
// );
//
// public enum ReplicationMode { Unidirectional, Bidirectional }
// public enum ConflictResolution { LastWriteWins, SourceWins, TargetWins }

// TODO: Step 2 - Set up multi-cloud clusters
// var cloudClusters = new Dictionary<string, (SurgewaveRuntime Runtime, string Provider)>();
//
// Console.WriteLine("--- Starting Multi-Cloud Clusters ---");
//
// var awsCluster = await SurgewaveRuntime.CreateBuilder()
//     .WithPort(0)
//     .WithStorageMode(StorageMode.Memory)
//     .WithAutoCreateTopics(true)
//     .Build()
//     .StartAsync();
// cloudClusters["aws"] = (awsCluster, "AWS");
// Console.WriteLine($"[AWS] Surgewave cluster started on port {awsCluster.Port}");
//
// // Add Azure and GCP clusters...

// TODO: Step 3 - Implement cross-cloud replicator
// public class CrossCloudReplicator { ... }
// var replicator = new CrossCloudReplicator(
//     sourceBootstrap: $"localhost:{awsCluster.Port}",
//     targetBootstrap: $"localhost:{azureCluster.Port}",
//     topics: new[] { "orders" }
// );

// TODO: Step 4 - Add conflict detection
// public class ConflictDetector { ... }
// var conflictDetector = new ConflictDetector();

// TODO: Step 5 - Implement topic filtering
// public class TopicFilter { ... }
// var filter = new TopicFilter(
//     includePatterns: new[] { "orders.*", "events.*" },
//     excludePatterns: new[] { "*.internal" }
// );

// TODO: Step 6 - Monitor replication lag
// public class ReplicationLagMonitor { ... }
// Console.WriteLine($"Replication lag: {lagMonitor.GetTotalLag()} messages");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use async replication for cross-cloud (avoid latency in critical path)");
Console.WriteLine("2. Choose conflict resolution strategy based on use case");
Console.WriteLine("3. Filter topics to replicate only what's needed");
Console.WriteLine("4. Monitor replication lag continuously");
Console.WriteLine("5. Test cloud failover scenarios regularly");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
