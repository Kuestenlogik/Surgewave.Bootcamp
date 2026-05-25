// Surgewave Bootcamp - Unit 15, Lesson 3: Failover Procedures
//
// In this lesson, you'll learn:
// - Detect cluster and broker failures
// - Implement manual and automatic failover
// - Execute DNS and load balancer switching
// - Validate post-failover health
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 15.3 - Failover Procedures ===");
Console.WriteLine();

// TODO: Step 1 - Define failover configuration
// public class FailoverConfig { ... }

// TODO: Step 2 - Define health check result classes
// public class HealthCheckResult { ... }

// Start primary and secondary clusters
Console.WriteLine("Starting primary cluster...");
await using var primaryRuntime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
Console.WriteLine($"  Primary started on port {primaryRuntime.Port}");

Console.WriteLine("Starting secondary cluster...");
await using var secondaryRuntime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
Console.WriteLine($"  Secondary started on port {secondaryRuntime.Port}");
Console.WriteLine();

// TODO: Step 3 - Create failover configuration
// var config = new FailoverConfig
// {
//     PrimaryBootstrapServers = $"localhost:{primaryRuntime.Port}",
//     SecondaryBootstrapServers = $"localhost:{secondaryRuntime.Port}",
//     HealthCheckInterval = TimeSpan.FromSeconds(2),
//     FailureThreshold = 3,
//     AutoFailoverEnabled = false
// };

// TODO: Step 4 - Create failover coordinator
// var coordinator = new FailoverCoordinator(config);

// TODO: Step 5 - Start health monitoring
// using var cts = new CancellationTokenSource();
// var monitorTask = Task.Run(() => coordinator.MonitorAsync(cts.Token));
//
// Console.WriteLine("Health monitoring...");
// await Task.Delay(5000);

// TODO: Step 6 - Simulate primary failure (stop responding to health checks)
// Console.WriteLine("\nSimulating primary failure...");
// For this demo, we'll manually trigger failover since we can't easily
// simulate network failures

// TODO: Step 7 - Trigger manual failover
// Console.WriteLine("\nTriggering manual failover...");
// var result = await coordinator.ExecuteFailoverAsync(FailoverTrigger.Manual);
//
// if (result.Success)
// {
//     Console.WriteLine($"Failover successful!");
//     Console.WriteLine($"  New active cluster: {result.NewActiveCluster}");
//     Console.WriteLine($"  Duration: {result.Duration}");
// }
// else
// {
//     Console.WriteLine($"Failover failed: {result.Error}");
// }

// TODO: Step 8 - Validate the new active cluster
// Console.WriteLine("\nValidating new active cluster...");
// var validation = await coordinator.ValidateClusterAsync(config.SecondaryBootstrapServers);
// Console.WriteLine($"  Connectivity: {(validation.ConnectivityOk ? "OK" : "FAILED")}");
// Console.WriteLine($"  Topics accessible: {(validation.TopicsAccessible ? "OK" : "FAILED")}");
// Console.WriteLine($"  Produce/consume: {(validation.ProduceConsumeOk ? "OK" : "FAILED")}");

// TODO: Step 9 - Stop monitoring
// cts.Cancel();

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Health checks detect cluster failures");
Console.WriteLine("2. Failure thresholds prevent false positives");
Console.WriteLine("3. Manual failover requires operator confirmation");
Console.WriteLine("4. Automatic failover reduces RTO but needs safeguards");
Console.WriteLine("5. Always validate after failover");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// TODO: Implement FailoverCoordinator class
// public class FailoverCoordinator { ... }

// TODO: Implement ClusterHealthChecker class
// public class ClusterHealthChecker { ... }

// TODO: Implement DnsManager class
// public class DnsManager { ... }

// TODO: Define enums and result classes
// public enum FailoverTrigger { Manual, Automatic }
// public enum ClusterRole { Primary, Secondary }
// public class FailoverResult { ... }
// public class ValidationResult { ... }
