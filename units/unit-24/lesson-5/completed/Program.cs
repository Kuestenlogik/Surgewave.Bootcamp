// Surgewave Bootcamp - Unit 15, Lesson 5: DR Testing
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 15.5 - DR Testing ===");
Console.WriteLine();

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

// Step 1: Create chaos runner
var chaosRunner = new ChaosRunner();

// Step 2: Define failure scenarios
var scenarios = new FailureScenarios(bootstrapServers);

// Step 3: Run broker failure experiment
Console.WriteLine("=== Running Chaos Experiments ===");
var brokerExperiment = scenarios.CreateBrokerFailureExperiment();
var brokerResult = await chaosRunner.RunExperimentAsync(brokerExperiment);
Console.WriteLine($"\nExperiment result: {(brokerResult.HypothesisHeld ? "PASSED" : "FAILED")}");
Console.WriteLine($"Recovery time: {brokerResult.RecoveryTime}");
Console.WriteLine();

// Step 4: Run network partition experiment
var networkExperiment = scenarios.CreateNetworkPartitionExperiment();
var networkResult = await chaosRunner.RunExperimentAsync(networkExperiment);
Console.WriteLine($"\nExperiment result: {(networkResult.HypothesisHeld ? "PASSED" : "FAILED")}");
Console.WriteLine($"Recovery time: {networkResult.RecoveryTime}");
Console.WriteLine();

// Step 5: Create and execute DR runbook
var runbook = DRRunbookFactory.CreateDatacenterFailoverRunbook(bootstrapServers, bootstrapServers);
var runbookResult = await runbook.ExecuteAsync();
Console.WriteLine($"\nRunbook execution: {(runbookResult.Success ? "COMPLETED" : "FAILED")}");
Console.WriteLine($"Total duration: {runbookResult.Duration}");
Console.WriteLine();

// Step 6: Collect and report metrics
var metrics = new DRMetrics
{
    DrillStartTime = DateTime.UtcNow.AddMinutes(-1),
    DrillEndTime = DateTime.UtcNow,
    DetectionTime = TimeSpan.FromSeconds(10),
    DecisionTime = TimeSpan.FromSeconds(5),
    ExecutionTime = TimeSpan.FromSeconds(15),
    ValidationTime = TimeSpan.FromSeconds(5),
    ActualRPO = TimeSpan.FromSeconds(5),
    TargetRTO = TimeSpan.FromMinutes(15),
    TargetRPO = TimeSpan.FromMinutes(5)
};

metrics.PrintReport();

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Chaos engineering validates resilience hypotheses");
Console.WriteLine("2. Regular drills build team confidence");
Console.WriteLine("3. Runbooks ensure consistent execution");
Console.WriteLine("4. Metrics prove RTO/RPO compliance");
Console.WriteLine("5. Test regularly and improve continuously");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Chaos Experiment Classes
public class ChaosExperiment
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Hypothesis { get; set; } = "";
    public TimeSpan MaxDuration { get; set; } = TimeSpan.FromMinutes(5);
    public Func<Task<bool>> SteadyStateCheck { get; set; } = () => Task.FromResult(true);
    public Func<Task> FaultInjection { get; set; } = () => Task.CompletedTask;
    public Func<Task> Rollback { get; set; } = () => Task.CompletedTask;
}

public class ExperimentResult
{
    public string ExperimentName { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool HypothesisHeld { get; set; }
    public TimeSpan RecoveryTime { get; set; }
    public long MessagesLost { get; set; }
    public List<string> Observations { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public TimeSpan Duration => EndTime - StartTime;
}

// Chaos Runner
public class ChaosRunner
{
    private readonly List<ExperimentResult> _results = new();

    public async Task<ExperimentResult> RunExperimentAsync(
        ChaosExperiment experiment,
        CancellationToken ct = default)
    {
        var result = new ExperimentResult
        {
            ExperimentName = experiment.Name,
            StartTime = DateTime.UtcNow
        };

        Console.WriteLine($"\n=== Starting Chaos Experiment: {experiment.Name} ===");
        Console.WriteLine($"Hypothesis: {experiment.Hypothesis}");
        Console.WriteLine();

        try
        {
            // Step 1: Verify steady state
            Console.WriteLine("Step 1: Verifying steady state...");
            var steadyStateBefore = await experiment.SteadyStateCheck();
            if (!steadyStateBefore)
            {
                result.Errors.Add("Initial steady state check failed");
                result.HypothesisHeld = false;
                result.EndTime = DateTime.UtcNow;
                return result;
            }
            result.Observations.Add("Initial steady state verified");
            Console.WriteLine("  Steady state confirmed");

            // Step 2: Inject fault
            Console.WriteLine("Step 2: Injecting fault...");
            var faultInjectionTime = DateTime.UtcNow;
            await experiment.FaultInjection();
            result.Observations.Add($"Fault injected at {faultInjectionTime:O}");
            Console.WriteLine("  Fault injected");

            // Step 3: Monitor recovery
            Console.WriteLine("Step 3: Monitoring recovery...");
            var recoveryStart = DateTime.UtcNow;
            var recovered = false;

            while (DateTime.UtcNow - recoveryStart < experiment.MaxDuration)
            {
                await Task.Delay(500, ct);

                var steadyStateAfter = await experiment.SteadyStateCheck();
                if (steadyStateAfter)
                {
                    result.RecoveryTime = DateTime.UtcNow - faultInjectionTime;
                    result.Observations.Add($"Recovery detected after {result.RecoveryTime}");
                    recovered = true;
                    Console.WriteLine($"  Recovery detected: {result.RecoveryTime}");
                    break;
                }
            }

            if (!recovered)
            {
                result.Errors.Add("Recovery not achieved within timeout");
                result.Observations.Add("System did not recover within expected time");
            }

            // Step 4: Rollback
            Console.WriteLine("Step 4: Rolling back...");
            await experiment.Rollback();
            result.Observations.Add("Rollback completed");
            Console.WriteLine("  Rollback complete");

            // Step 5: Final steady state check
            Console.WriteLine("Step 5: Final verification...");
            var finalSteadyState = await experiment.SteadyStateCheck();
            result.HypothesisHeld = recovered && finalSteadyState;
            Console.WriteLine($"  Hypothesis held: {result.HypothesisHeld}");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Experiment failed: {ex.Message}");
            result.HypothesisHeld = false;

            try { await experiment.Rollback(); } catch { }
        }

        result.EndTime = DateTime.UtcNow;
        _results.Add(result);
        return result;
    }

    public IReadOnlyList<ExperimentResult> GetResults() => _results.AsReadOnly();
}

// Failure Scenarios
public class FailureScenarios
{
    private readonly string _bootstrapServers;

    public FailureScenarios(string bootstrapServers)
    {
        _bootstrapServers = bootstrapServers;
    }

    public ChaosExperiment CreateBrokerFailureExperiment()
    {
        return new ChaosExperiment
        {
            Name = "Broker Failure",
            Description = "Simulate broker going offline",
            Hypothesis = "System continues operating with degraded capacity",
            MaxDuration = TimeSpan.FromSeconds(30),
            SteadyStateCheck = async () => await CanProduceAndConsumeAsync(),
            FaultInjection = async () =>
            {
                Console.WriteLine("    Simulating broker failure (2 second pause)...");
                await Task.Delay(2000);
            },
            Rollback = () =>
            {
                Console.WriteLine("    Broker restarted (simulated)");
                return Task.CompletedTask;
            }
        };
    }

    public ChaosExperiment CreateNetworkPartitionExperiment()
    {
        return new ChaosExperiment
        {
            Name = "Network Partition",
            Description = "Simulate network split between components",
            Hypothesis = "System detects partition and handles gracefully",
            MaxDuration = TimeSpan.FromSeconds(30),
            SteadyStateCheck = async () => await CanProduceAndConsumeAsync(),
            FaultInjection = async () =>
            {
                Console.WriteLine("    Simulating network partition (1 second)...");
                await Task.Delay(1000);
            },
            Rollback = () =>
            {
                Console.WriteLine("    Network partition healed (simulated)");
                return Task.CompletedTask;
            }
        };
    }

    private async Task<bool> CanProduceAndConsumeAsync()
    {
        try
        {
            await using var producer = new SurgewaveProducer<string, string>(options =>
            {
                options.BootstrapServers = _bootstrapServers;
            });

            await producer.ProduceAsync("_health-test", "test", DateTime.UtcNow.ToString("O"));
            return true;
        }
        catch
        {
            return false;
        }
    }
}

// DR Runbook Classes
public class DRRunbook
{
    private readonly List<RunbookStep> _steps = new();
    private readonly Dictionary<string, object> _context = new();

    public void AddStep(RunbookStep step)
    {
        step.StepNumber = _steps.Count + 1;
        _steps.Add(step);
    }

    public async Task<RunbookExecutionResult> ExecuteAsync(CancellationToken ct = default)
    {
        var result = new RunbookExecutionResult
        {
            StartTime = DateTime.UtcNow
        };

        Console.WriteLine("\n=== Executing DR Runbook ===\n");

        foreach (var step in _steps)
        {
            Console.WriteLine($"Step {step.StepNumber}: {step.Name}");
            Console.WriteLine($"  Description: {step.Description}");

            var stepResult = new StepResult
            {
                StepNumber = step.StepNumber,
                StepName = step.Name,
                StartTime = DateTime.UtcNow
            };

            try
            {
                if (step.RequiresConfirmation)
                {
                    Console.WriteLine("  [Requires confirmation - auto-confirming for demo]");
                }

                await step.Action(_context);
                stepResult.Success = true;
                stepResult.EndTime = DateTime.UtcNow;
                Console.WriteLine($"  Status: COMPLETED ({stepResult.Duration.TotalSeconds:F1}s)");
            }
            catch (Exception ex)
            {
                stepResult.Success = false;
                stepResult.Error = ex.Message;
                stepResult.EndTime = DateTime.UtcNow;
                Console.WriteLine($"  Status: FAILED - {ex.Message}");

                if (step.CriticalStep)
                {
                    result.Success = false;
                    result.FailedAtStep = step.StepNumber;
                    result.EndTime = DateTime.UtcNow;
                    result.StepResults.Add(stepResult);
                    return result;
                }
            }

            result.StepResults.Add(stepResult);
            Console.WriteLine();
        }

        result.Success = result.StepResults.All(s => s.Success);
        result.EndTime = DateTime.UtcNow;
        return result;
    }
}

public class RunbookStep
{
    public int StepNumber { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool RequiresConfirmation { get; set; }
    public bool CriticalStep { get; set; }
    public Func<Dictionary<string, object>, Task> Action { get; set; } = _ => Task.CompletedTask;
}

public class RunbookExecutionResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public int? FailedAtStep { get; set; }
    public List<StepResult> StepResults { get; set; } = new();
    public TimeSpan Duration => EndTime - StartTime;
}

public class StepResult
{
    public int StepNumber { get; set; }
    public string StepName { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
}

// Runbook Factory
public static class DRRunbookFactory
{
    public static DRRunbook CreateDatacenterFailoverRunbook(
        string primaryServers,
        string secondaryServers)
    {
        var runbook = new DRRunbook();

        runbook.AddStep(new RunbookStep
        {
            Name = "Assess Situation",
            Description = "Verify primary cluster is actually down",
            CriticalStep = true,
            Action = async ctx =>
            {
                Console.WriteLine("    Checking primary cluster health...");
                await Task.Delay(500);
                ctx["PrimaryHealthy"] = true;
                Console.WriteLine("    Primary cluster check complete");
            }
        });

        runbook.AddStep(new RunbookStep
        {
            Name = "Notify Stakeholders",
            Description = "Send alerts to on-call team",
            Action = async ctx =>
            {
                Console.WriteLine("    Sending notifications...");
                await Task.Delay(300);
                Console.WriteLine("    Notifications sent to: ops-team, platform-lead");
            }
        });

        runbook.AddStep(new RunbookStep
        {
            Name = "Verify Secondary",
            Description = "Confirm secondary cluster is ready",
            CriticalStep = true,
            Action = async ctx =>
            {
                Console.WriteLine("    Checking secondary cluster health...");
                await Task.Delay(500);
                ctx["SecondaryHealthy"] = true;
                Console.WriteLine("    Secondary cluster is healthy and ready");
            }
        });

        runbook.AddStep(new RunbookStep
        {
            Name = "Check Replication Lag",
            Description = "Verify replication is caught up",
            Action = async ctx =>
            {
                Console.WriteLine("    Checking replication status...");
                await Task.Delay(300);
                var lag = TimeSpan.FromSeconds(5);
                ctx["ReplicationLag"] = lag;
                Console.WriteLine($"    Replication lag: {lag}");
            }
        });

        runbook.AddStep(new RunbookStep
        {
            Name = "Execute Failover",
            Description = "Switch traffic to secondary cluster",
            RequiresConfirmation = true,
            CriticalStep = true,
            Action = async ctx =>
            {
                Console.WriteLine("    Updating DNS records...");
                await Task.Delay(500);
                Console.WriteLine("    DNS updated: surgewave.example.com -> secondary");
                ctx["FailoverTime"] = DateTime.UtcNow;
            }
        });

        runbook.AddStep(new RunbookStep
        {
            Name = "Validate Services",
            Description = "Verify all services are operational",
            Action = async ctx =>
            {
                Console.WriteLine("    Running service health checks...");
                await Task.Delay(300);
                Console.WriteLine("    All services operational on secondary");
            }
        });

        runbook.AddStep(new RunbookStep
        {
            Name = "Document Incident",
            Description = "Record timeline and actions taken",
            Action = async ctx =>
            {
                Console.WriteLine("    Recording incident timeline...");
                await Task.Delay(200);
                Console.WriteLine($"    Incident documented at {DateTime.UtcNow:O}");
            }
        });

        return runbook;
    }
}

// DR Metrics
public class DRMetrics
{
    public DateTime DrillStartTime { get; set; }
    public DateTime DrillEndTime { get; set; }
    public TimeSpan DetectionTime { get; set; }
    public TimeSpan DecisionTime { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public TimeSpan ValidationTime { get; set; }
    public TimeSpan ActualRTO => DetectionTime + DecisionTime + ExecutionTime + ValidationTime;
    public TimeSpan TargetRTO { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan ActualRPO { get; set; }
    public TimeSpan TargetRPO { get; set; } = TimeSpan.FromMinutes(5);
    public bool RTOMet => ActualRTO <= TargetRTO;
    public bool RPOMet => ActualRPO <= TargetRPO;

    public void PrintReport()
    {
        Console.WriteLine("\n=== DR Drill Metrics Report ===\n");

        Console.WriteLine("Recovery Time Breakdown:");
        Console.WriteLine($"  Detection:   {DetectionTime.TotalSeconds:F1} seconds");
        Console.WriteLine($"  Decision:    {DecisionTime.TotalSeconds:F1} seconds");
        Console.WriteLine($"  Execution:   {ExecutionTime.TotalSeconds:F1} seconds");
        Console.WriteLine($"  Validation:  {ValidationTime.TotalSeconds:F1} seconds");
        Console.WriteLine($"  ---------------------");
        Console.WriteLine($"  Total RTO:   {ActualRTO.TotalMinutes:F2} minutes");
        Console.WriteLine($"  Target RTO:  {TargetRTO.TotalMinutes:F2} minutes");
        Console.WriteLine($"  RTO Status:  {(RTOMet ? "PASS" : "FAIL")}");
        Console.WriteLine();

        Console.WriteLine("Recovery Point:");
        Console.WriteLine($"  Actual RPO:  {ActualRPO.TotalSeconds:F1} seconds");
        Console.WriteLine($"  Target RPO:  {TargetRPO.TotalMinutes:F2} minutes");
        Console.WriteLine($"  RPO Status:  {(RPOMet ? "PASS" : "FAIL")}");
        Console.WriteLine();

        Console.WriteLine($"Overall Result: {(RTOMet && RPOMet ? "PASS" : "FAIL")}");
    }
}
