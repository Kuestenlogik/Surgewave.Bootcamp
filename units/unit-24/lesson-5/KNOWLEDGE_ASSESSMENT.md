# Knowledge Assessment: Lesson 15.5 - DR Testing

## Multiple Choice

### 1. What is the purpose of chaos engineering in disaster recovery?
- [ ] A) To cause random production outages
- [ ] B) To systematically test system resilience by introducing controlled failures
- [ ] C) To increase system complexity
- [ ] D) To replace traditional testing

<details>
<summary>Answer</summary>
**B)** Chaos engineering involves deliberately introducing controlled failures in a systematic way to test how systems respond. This builds confidence that recovery procedures work and helps discover weaknesses before real incidents occur.
</details>

### 2. What should a DR runbook include?
- [ ] A) Only technical commands
- [ ] B) Decision criteria, contact info, step-by-step procedures, and validation steps
- [ ] C) Marketing materials
- [ ] D) Historical incident reports only

<details>
<summary>Answer</summary>
**B)** A comprehensive DR runbook includes: overview and scope, prerequisites/access requirements, decision criteria for when to invoke DR, contact information, detailed step-by-step procedures, validation/verification steps, and rollback procedures.
</details>

### 3. How often should failover drills be conducted?
- [ ] A) Once when the system is deployed
- [ ] B) Monthly or quarterly, depending on criticality
- [ ] C) Only after incidents
- [ ] D) Never, to avoid service disruption

<details>
<summary>Answer</summary>
**B)** DR drills should be conducted regularly - monthly for critical systems, quarterly for others. This ensures procedures remain current, team members are trained, and issues are discovered before real disasters.
</details>

## True/False

### 4. Chaos engineering experiments should start with a hypothesis about expected system behavior.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Every chaos experiment should begin with a hypothesis, such as "When broker-1 fails, consumers should failover to broker-2 within 30 seconds with zero message loss." This makes results measurable and meaningful.
</details>

### 5. Blameless post-mortems focus on finding who made mistakes to prevent future errors.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Blameless post-mortems focus on understanding WHAT happened and HOW to prevent recurrence, not WHO is at fault. Blaming individuals discourages transparency and honest reporting, hindering learning and improvement.
</details>

## Code Challenge

### 6. What issues exist in this chaos experiment implementation?

```csharp
public class ChaosExperiment
{
    public void Run()
    {
        // Kill broker
        Process.Start("docker", "stop surgewave-broker");

        // Check if consumers recovered
        Thread.Sleep(10000);
        var healthy = CheckConsumerHealth();

        Console.WriteLine($"Result: {healthy}");
    }
}
```

<details>
<summary>Answer</summary>
Issues:

1. **No hypothesis definition**: No clear expected outcome
2. **No steady state verification**: Doesn't verify system is healthy before experiment
3. **No blast radius control**: Could affect production
4. **Blocking Thread.Sleep**: Poor practice, doesn't allow cancellation
5. **Fixed timeout**: May not be enough or may be too much
6. **No metrics collection**: Only binary healthy/unhealthy
7. **No rollback**: Doesn't restore the broker after experiment
8. **Console output**: Should use proper logging and reporting
9. **No safety guards**: No way to abort if things go wrong

Improved version:
```csharp
public class ChaosExperiment
{
    private readonly ILogger<ChaosExperiment> _logger;
    private readonly IMetricsCollector _metrics;
    private readonly IDockerClient _docker;

    public async Task<ExperimentResult> RunAsync(ExperimentConfig config)
    {
        var result = new ExperimentResult
        {
            ExperimentName = config.Name,
            Hypothesis = config.Hypothesis,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // 1. Verify steady state before experiment
            _logger.LogInformation("Verifying steady state...");
            if (!await VerifySteadyStateAsync(config.SteadyStateConditions))
            {
                result.Status = ExperimentStatus.Aborted;
                result.AbortReason = "Steady state not achieved";
                return result;
            }
            result.SteadyStateVerified = true;

            // 2. Start metrics collection
            var metricsTask = CollectMetricsAsync(result, config.Duration);

            // 3. Inject fault
            _logger.LogWarning("Injecting fault: {FaultType}", config.FaultType);
            result.FaultInjectedAt = DateTime.UtcNow;
            await InjectFaultAsync(config);

            // 4. Observe behavior (with timeout)
            _logger.LogInformation("Observing system behavior for {Duration}...", config.ObservationDuration);
            using var cts = new CancellationTokenSource(config.MaxDuration);

            var recoveryTime = await MeasureRecoveryTimeAsync(
                config.RecoveryConditions,
                cts.Token);

            result.RecoveryTime = recoveryTime;
            result.RecoveredWithinTarget = recoveryTime <= config.TargetRecoveryTime;

            // 5. Rollback - ALWAYS
            _logger.LogInformation("Rolling back fault injection...");
            await RollbackAsync(config);
            result.RolledBack = true;

            // 6. Verify steady state after
            await Task.Delay(TimeSpan.FromSeconds(10)); // Allow stabilization
            result.SteadyStateRestoredAfter = await VerifySteadyStateAsync(
                config.SteadyStateConditions);

            // 7. Collect final metrics
            await metricsTask;

            // 8. Evaluate hypothesis
            result.HypothesisHeld = EvaluateHypothesis(result, config);
            result.Status = result.HypothesisHeld
                ? ExperimentStatus.Passed
                : ExperimentStatus.Failed;

            result.EndTime = DateTime.UtcNow;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Experiment failed with exception");

            // Always rollback on failure
            try
            {
                await RollbackAsync(config);
                result.RolledBack = true;
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Rollback failed!");
            }

            result.Status = ExperimentStatus.Error;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;
            return result;
        }
    }

    private async Task<bool> VerifySteadyStateAsync(SteadyStateConditions conditions)
    {
        // Check all consumers are healthy
        var consumersHealthy = await CheckAllConsumersHealthyAsync();
        // Check no replication lag
        var noLag = await CheckReplicationLagAsync() < conditions.MaxAcceptableLag;
        // Check error rate is normal
        var normalErrors = await GetErrorRateAsync() < conditions.MaxErrorRate;

        return consumersHealthy && noLag && normalErrors;
    }
}

public class ExperimentConfig
{
    public string Name { get; set; } = "";
    public string Hypothesis { get; set; } = "";
    public string FaultType { get; set; } = ""; // broker_kill, network_partition, etc.
    public string TargetContainer { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public TimeSpan ObservationDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public TimeSpan TargetRecoveryTime { get; set; }
    public SteadyStateConditions SteadyStateConditions { get; set; } = new();
    public RecoveryConditions RecoveryConditions { get; set; } = new();
}

public class ExperimentResult
{
    public string ExperimentName { get; set; } = "";
    public string Hypothesis { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime FaultInjectedAt { get; set; }
    public TimeSpan RecoveryTime { get; set; }
    public bool RecoveredWithinTarget { get; set; }
    public bool SteadyStateVerified { get; set; }
    public bool SteadyStateRestoredAfter { get; set; }
    public bool HypothesisHeld { get; set; }
    public bool RolledBack { get; set; }
    public ExperimentStatus Status { get; set; }
    public string? AbortReason { get; set; }
    public string? ErrorMessage { get; set; }
    public List<MetricSnapshot> Metrics { get; set; } = new();
}

public enum ExperimentStatus { Passed, Failed, Aborted, Error }
```
</details>

### 7. Create a DR drill checklist and report template:

<details>
<summary>Answer</summary>
```csharp
public class DRDrillRunner
{
    public async Task<DrillReport> ExecuteDrillAsync(DrillScenario scenario)
    {
        var report = new DrillReport
        {
            ScenarioName = scenario.Name,
            ScenarioType = scenario.Type,
            ExecutionDate = DateTime.UtcNow,
            Participants = scenario.Participants,
            ChecklistItems = new List<ChecklistItem>()
        };

        // Pre-drill checklist
        report.ChecklistItems.AddRange(new[]
        {
            await ExecuteCheckAsync("Runbook available and current", scenario.RunbookVersion),
            await ExecuteCheckAsync("All participants briefed", scenario.ParticipantsBriefed),
            await ExecuteCheckAsync("Communication channels established", scenario.CommChannelReady),
            await ExecuteCheckAsync("Monitoring dashboards accessible", scenario.DashboardsAccessible),
            await ExecuteCheckAsync("Rollback procedure documented", scenario.RollbackReady),
            await ExecuteCheckAsync("Stakeholders notified", scenario.StakeholdersNotified),
        });

        // Record pre-drill baseline
        report.BaselineMetrics = await CaptureBaselineAsync();

        // Execute drill
        var drillStart = DateTime.UtcNow;

        report.ChecklistItems.AddRange(new[]
        {
            await ExecuteTimedCheckAsync("Failure detected", scenario.DetectionStep),
            await ExecuteTimedCheckAsync("Incident declared", scenario.IncidentDeclaredStep),
            await ExecuteTimedCheckAsync("Failover decision made", scenario.DecisionStep),
            await ExecuteTimedCheckAsync("Failover executed", scenario.FailoverStep),
            await ExecuteTimedCheckAsync("Service restored", scenario.RestorationStep),
            await ExecuteTimedCheckAsync("Validation completed", scenario.ValidationStep),
        });

        report.TotalDuration = DateTime.UtcNow - drillStart;

        // Capture metrics
        report.AchievedRTO = report.ChecklistItems
            .Where(c => c.Name == "Service restored")
            .Select(c => c.Duration)
            .FirstOrDefault();

        report.AchievedRPO = await MeasureDataLossAsync();

        // Post-drill verification
        report.ChecklistItems.AddRange(new[]
        {
            await ExecuteCheckAsync("All services healthy", await AllServicesHealthyAsync()),
            await ExecuteCheckAsync("No data loss beyond RPO", report.AchievedRPO <= scenario.TargetRPO),
            await ExecuteCheckAsync("Consumers resumed correctly", await ConsumersResumedAsync()),
            await ExecuteCheckAsync("Rollback completed (if applicable)", scenario.RollbackCompleted),
        });

        // Evaluate results
        report.MetTargetRTO = report.AchievedRTO <= scenario.TargetRTO;
        report.MetTargetRPO = report.AchievedRPO <= scenario.TargetRPO;
        report.OverallResult = report.MetTargetRTO && report.MetTargetRPO
            ? DrillResult.Pass
            : DrillResult.Fail;

        return report;
    }

    public string GenerateReportMarkdown(DrillReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# DR Drill Report: {report.ScenarioName}");
        sb.AppendLine();
        sb.AppendLine($"**Date:** {report.ExecutionDate:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Scenario Type:** {report.ScenarioType}");
        sb.AppendLine($"**Overall Result:** {report.OverallResult}");
        sb.AppendLine();

        sb.AppendLine("## Participants");
        foreach (var p in report.Participants)
            sb.AppendLine($"- {p.Name} ({p.Role})");
        sb.AppendLine();

        sb.AppendLine("## Recovery Metrics");
        sb.AppendLine();
        sb.AppendLine("| Metric | Target | Achieved | Status |");
        sb.AppendLine("|--------|--------|----------|--------|");
        sb.AppendLine($"| RTO | {report.TargetRTO} | {report.AchievedRTO} | {(report.MetTargetRTO ? "PASS" : "FAIL")} |");
        sb.AppendLine($"| RPO | {report.TargetRPO} | {report.AchievedRPO} | {(report.MetTargetRPO ? "PASS" : "FAIL")} |");
        sb.AppendLine();

        sb.AppendLine("## Checklist Results");
        sb.AppendLine();
        sb.AppendLine("| Step | Status | Duration | Notes |");
        sb.AppendLine("|------|--------|----------|-------|");
        foreach (var item in report.ChecklistItems)
        {
            var status = item.Passed ? "PASS" : "FAIL";
            var duration = item.Duration.HasValue ? item.Duration.Value.ToString() : "-";
            sb.AppendLine($"| {item.Name} | {status} | {duration} | {item.Notes ?? ""} |");
        }
        sb.AppendLine();

        sb.AppendLine("## Timeline");
        sb.AppendLine();
        sb.AppendLine("```");
        var startTime = report.ExecutionDate;
        foreach (var item in report.ChecklistItems.Where(c => c.Timestamp.HasValue).OrderBy(c => c.Timestamp))
        {
            var elapsed = item.Timestamp!.Value - startTime;
            sb.AppendLine($"[+{elapsed:mm\\:ss}] {item.Name}");
        }
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("## Observations & Findings");
        sb.AppendLine();
        foreach (var finding in report.Findings)
        {
            sb.AppendLine($"### {finding.Type}: {finding.Title}");
            sb.AppendLine(finding.Description);
            if (!string.IsNullOrEmpty(finding.ActionItem))
                sb.AppendLine($"**Action Item:** {finding.ActionItem}");
            sb.AppendLine();
        }

        sb.AppendLine("## Recommendations");
        sb.AppendLine();
        foreach (var rec in report.Recommendations)
        {
            sb.AppendLine($"- {rec}");
        }

        return sb.ToString();
    }
}

public class DrillReport
{
    public string ScenarioName { get; set; } = "";
    public string ScenarioType { get; set; } = "";
    public DateTime ExecutionDate { get; set; }
    public List<Participant> Participants { get; set; } = new();
    public List<ChecklistItem> ChecklistItems { get; set; } = new();
    public BaselineMetrics BaselineMetrics { get; set; } = new();
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan TargetRTO { get; set; }
    public TimeSpan AchievedRTO { get; set; }
    public TimeSpan TargetRPO { get; set; }
    public TimeSpan AchievedRPO { get; set; }
    public bool MetTargetRTO { get; set; }
    public bool MetTargetRPO { get; set; }
    public DrillResult OverallResult { get; set; }
    public List<Finding> Findings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class ChecklistItem
{
    public string Name { get; set; } = "";
    public bool Passed { get; set; }
    public TimeSpan? Duration { get; set; }
    public DateTime? Timestamp { get; set; }
    public string? Notes { get; set; }
}

public record Participant(string Name, string Role);
public record Finding(string Type, string Title, string Description, string? ActionItem);
public enum DrillResult { Pass, Fail, PartialPass }
```

Example output:
```markdown
# DR Drill Report: Complete DC Failover

**Date:** 2024-01-15 10:00:00 UTC
**Scenario Type:** Datacenter Failure
**Overall Result:** Pass

## Recovery Metrics

| Metric | Target | Achieved | Status |
|--------|--------|----------|--------|
| RTO | 00:15:00 | 00:13:45 | PASS |
| RPO | 00:05:00 | 00:00:15 | PASS |

## Timeline

```
[+00:00] Failure detected
[+00:45] Incident declared
[+02:30] Failover decision made
[+10:15] Failover executed
[+13:45] Service restored
[+15:30] Validation completed
```

## Observations & Findings

### Improvement: Slow DNS propagation
DNS changes took 2 minutes to propagate. Consider lowering TTL.
**Action Item:** Reduce DNS TTL from 300 to 60 seconds

## Recommendations

- Schedule next drill for Q2 2024
- Update runbook with lessons learned
- Consider automating failover decision step
```
</details>

## Score
- 7/7: Excellent! You understand DR testing and chaos engineering
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding
