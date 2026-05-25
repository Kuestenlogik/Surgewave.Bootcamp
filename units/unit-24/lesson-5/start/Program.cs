// Surgewave Bootcamp - Unit 15, Lesson 5: DR Testing
//
// In this lesson, you'll learn:
// - Design and execute chaos engineering experiments
// - Conduct failover drills with realistic scenarios
// - Create comprehensive DR runbooks
// - Measure and improve RTO/RPO metrics
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 15.5 - DR Testing ===");
Console.WriteLine();

// TODO: Step 1 - Define chaos experiment classes
// public class ChaosExperiment { ... }
// public class ExperimentResult { ... }

// TODO: Step 2 - Define runbook classes
// public class DRRunbook { ... }
// public class RunbookStep { ... }
// public class RunbookExecutionResult { ... }

// Start a Surgewave runtime
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave runtime started on port {runtime.Port}");
Console.WriteLine();

var bootstrapServers = $"localhost:{runtime.Port}";

// TODO: Step 3 - Create chaos runner
// var chaosRunner = new ChaosRunner();

// TODO: Step 4 - Define failure scenarios
// var scenarios = new FailureScenarios(bootstrapServers, runtime);

// TODO: Step 5 - Run broker failure experiment
// Console.WriteLine("=== Running Chaos Experiments ===");
// var brokerExperiment = scenarios.CreateBrokerFailureExperiment();
// var brokerResult = await chaosRunner.RunExperimentAsync(brokerExperiment);
// Console.WriteLine($"\nExperiment result: {(brokerResult.HypothesisHeld ? "PASSED" : "FAILED")}");
// Console.WriteLine($"Recovery time: {brokerResult.RecoveryTime}");
// Console.WriteLine();

// TODO: Step 6 - Run network partition experiment
// var networkExperiment = scenarios.CreateNetworkPartitionExperiment();
// var networkResult = await chaosRunner.RunExperimentAsync(networkExperiment);
// Console.WriteLine($"\nExperiment result: {(networkResult.HypothesisHeld ? "PASSED" : "FAILED")}");
// Console.WriteLine($"Recovery time: {networkResult.RecoveryTime}");
// Console.WriteLine();

// TODO: Step 7 - Create and execute DR runbook
// Console.WriteLine("=== Executing DR Runbook ===");
// var runbook = CreateDatacenterFailoverRunbook(bootstrapServers, bootstrapServers);
// var runbookResult = await runbook.ExecuteAsync();
// Console.WriteLine($"\nRunbook execution: {(runbookResult.Success ? "COMPLETED" : "FAILED")}");
// Console.WriteLine($"Total duration: {runbookResult.Duration}");
// Console.WriteLine();

// TODO: Step 8 - Collect and report metrics
// var metrics = new DRMetrics
// {
//     DrillStartTime = DateTime.UtcNow.AddMinutes(-1),
//     DrillEndTime = DateTime.UtcNow,
//     DetectionTime = TimeSpan.FromSeconds(10),
//     DecisionTime = TimeSpan.FromSeconds(5),
//     ExecutionTime = TimeSpan.FromSeconds(15),
//     ValidationTime = TimeSpan.FromSeconds(5),
//     ActualRPO = TimeSpan.FromSeconds(5),
//     TargetRTO = TimeSpan.FromMinutes(15),
//     TargetRPO = TimeSpan.FromMinutes(5)
// };
//
// metrics.PrintReport();

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Chaos engineering validates resilience hypotheses");
Console.WriteLine("2. Regular drills build team confidence");
Console.WriteLine("3. Runbooks ensure consistent execution");
Console.WriteLine("4. Metrics prove RTO/RPO compliance");
Console.WriteLine("5. Test regularly and improve continuously");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// TODO: Implement ChaosRunner class
// public class ChaosRunner
// {
//     public async Task<ExperimentResult> RunExperimentAsync(...) { ... }
// }

// TODO: Implement FailureScenarios class
// public class FailureScenarios
// {
//     public ChaosExperiment CreateBrokerFailureExperiment() { ... }
//     public ChaosExperiment CreateNetworkPartitionExperiment() { ... }
// }

// TODO: Implement DRMetrics class
// public class DRMetrics
// {
//     public void PrintReport() { ... }
// }
