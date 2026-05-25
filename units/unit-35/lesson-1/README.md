# Lesson 35.1: Introduction to Chaos Engineering

### What Is Chaos Engineering?

Chaos engineering is the discipline of experimenting on a distributed system to build confidence in the system's capability to withstand turbulent conditions in production. Rather than waiting for failures to happen, you intentionally inject them in controlled environments.

**Core Principles:**
1. Define a "steady state" (normal system behavior)
2. Hypothesize that steady state continues during fault injection
3. Introduce real-world failure events (network partitions, disk errors, crashes)
4. Look for differences between control and experiment groups
5. Minimize the blast radius of experiments

### Surgewave's ChaosEngine Architecture

Surgewave provides a purpose-built chaos testing framework in the `Kuestenlogik.Surgewave.Testing.Chaos` package. The architecture centers around the `ChaosEngine` class, which acts as a central orchestrator for all fault injection.

```csharp
// The core chaos flow
var engine = new ChaosEngine();

// 1. Activate a fault
var faultId = engine.ActivateFault(FaultType.DiskIoError, new FaultScope { BrokerId = 1 });

// 2. Run your test — storage operations on broker 1 will now throw IOException
await RunTestScenario();

// 3. Deactivate the fault
engine.DeactivateFault(faultId);

// 4. Inspect what happened
engine.Timeline.DumpToOutput(Console.WriteLine);
```

**Key Components:**
- `ChaosEngine` -- Central orchestrator managing active faults
- `ChaosLogSegment` / `ChaosLogSegmentFactory` -- Storage layer fault injection
- `ChaosRaftTransport` -- Transport layer fault injection
- Scenario helpers -- High-level failure composition (partition, crash, disk failure, latency)
- `FaultSchedule` -- Time-delayed fault activation
- `ChaosCluster` -- Multi-broker test cluster with integrated chaos
- `ChaosTimeline` -- Event recorder for diagnostics and assertions

---

[Back to Unit 31](../README.md)
