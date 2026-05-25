# Lesson 34.6: Performance Regression Suite

### Overview

The `Kuestenlogik.Surgewave.Benchmarks.Regression` project is a CLI tool that compares BenchmarkDotNet results against saved baselines to automatically detect performance regressions.

### Commands

```bash
# Compare current results against baseline
dotnet run --project benchmarks/Kuestenlogik.Surgewave.Benchmarks.Regression -- compare \
    --baseline benchmarks/baselines/unit-benchmarks.json \
    --current BenchmarkDotNet.Artifacts/results/report.json

# Update baseline with new results
dotnet run --project benchmarks/Kuestenlogik.Surgewave.Benchmarks.Regression -- update-baseline \
    --source BenchmarkDotNet.Artifacts/results/report.json \
    --target benchmarks/baselines/unit-benchmarks.json

# Generate a Markdown report
dotnet run --project benchmarks/Kuestenlogik.Surgewave.Benchmarks.Regression -- report \
    --baseline benchmarks/baselines/unit-benchmarks.json \
    --current BenchmarkDotNet.Artifacts/results/report.json
```

### RegressionDetector

The core comparison engine with configurable thresholds:

```csharp
var config = new RegressionConfig
{
    LatencyThresholdPercent = 15.0,      // Flag if mean time increases >15%
    ThroughputThresholdPercent = 10.0,   // Flag if ops/sec drops >10%
    AllocationThresholdPercent = 20.0,   // Flag if allocations increase >20%
    ExcludedBenchmarks = ["FlakeyBenchmark"],
    CategoryOverrides = new()
    {
        ["Storage"] = new CategoryThresholds
        {
            LatencyThresholdPercent = 20.0  // More lenient for storage
        }
    }
};

var detector = new RegressionDetector(config);
var results = detector.Compare(baseline, current);
```

### RegressionSeverity

Each comparison result is classified as:

| Severity | Meaning |
|----------|---------|
| `Stable` | Within threshold -- no significant change |
| `Regression` | Performance degraded beyond threshold |
| `Improvement` | Performance improved beyond threshold |
| `New` | Benchmark exists in current but not baseline |

### RegressionReportGenerator

Generates Markdown reports suitable for PR comments:

```csharp
var markdown = RegressionReportGenerator.GenerateMarkdown(results, baseline, current);
// Output:
// ## Performance Regression Report
//
// **Status:** FAIL
// **Benchmarks:** 42 total | 2 regressions | 3 improvements | 35 stable | 2 new
//
// ### Regressions
// | Benchmark | Metric | Baseline | Current | Delta |
// |-----------|--------|----------|---------|-------|
// | ProduceBenchmark.SingleMessage | Mean (ns) | 1.23us | 1.89us | +53.7% |
```

### Baseline Management

Baselines are stored as BenchmarkDotNet JSON exports in `benchmarks/baselines/`:

```
benchmarks/
  baselines/
    unit-benchmarks.json
    storage-benchmarks.json
    integration-benchmarks.json
```

Update baselines after intentional performance changes:

```powershell
# PowerShell script for local updates
.\scripts\update-baselines.ps1
```

### CI Integration

The regression suite runs in GitHub Actions on pull requests:

1. PR triggers benchmark run
2. Results are compared against baselines in the repository
3. A Markdown report is posted as a PR comment
4. The check fails if any regression is detected
5. Developers fix the regression or update the baseline if the change is intentional

---

[Back to Unit 33](../README.md)
