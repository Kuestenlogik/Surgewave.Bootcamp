# Lesson 35.7: FaultSchedule for Timed Chaos

### Scheduling Faults with Delays

The `FaultSchedule` activates a fault after a specified delay and optionally deactivates it after a duration. This is useful for simulating transient failures during long-running tests.

```csharp
// Activate a disk error after 5 seconds, lasting 10 seconds
var schedule = FaultSchedule.Create(
    engine,
    FaultType.DiskIoError,
    new FaultScope { BrokerId = 0 },
    activateAfter: TimeSpan.FromSeconds(5),
    duration: TimeSpan.FromSeconds(10)
);

// The fault will:
// - Activate at T+5s
// - Deactivate at T+15s (5s delay + 10s duration)

// Cancel the schedule early if needed
schedule.Dispose();
```

### Permanent Delayed Faults

Omit the `duration` parameter for faults that activate after a delay but stay active indefinitely:

```csharp
var schedule = FaultSchedule.Create(
    engine,
    FaultType.NetworkPartition,
    new FaultScope { BrokerId = 1, TargetPeerId = 2 },
    activateAfter: TimeSpan.FromSeconds(30)
);
// Partition activates at T+30s and stays active until manually deactivated
```

### Composing Multiple Schedules

```csharp
// Simulate cascading failures
var schedules = new List<FaultSchedule>
{
    // Network gets slow at T+5s for 20s
    FaultSchedule.Create(engine, FaultType.SlowNetwork,
        new FaultScope { BrokerId = 0 },
        activateAfter: TimeSpan.FromSeconds(5),
        duration: TimeSpan.FromSeconds(20)),

    // Disk fails at T+10s for 15s
    FaultSchedule.Create(engine, FaultType.DiskIoError,
        new FaultScope { BrokerId = 1 },
        activateAfter: TimeSpan.FromSeconds(10),
        duration: TimeSpan.FromSeconds(15)),

    // Broker crashes at T+20s for 10s
    FaultSchedule.Create(engine, FaultType.NodeCrash,
        new FaultScope { BrokerId = 2 },
        activateAfter: TimeSpan.FromSeconds(20),
        duration: TimeSpan.FromSeconds(10))
};

// Run test workload during the chaos window
await RunLongTestWorkload(TimeSpan.FromSeconds(45));

// Cleanup
foreach (var s in schedules) s.Dispose();
```

---

[Back to Unit 31](../README.md)
