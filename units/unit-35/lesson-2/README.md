# Lesson 35.2: The ChaosEngine

### Creating and Configuring a ChaosEngine

The `ChaosEngine` is thread-safe and manages all active faults via a `ConcurrentDictionary`. Every fault activation is recorded in the engine's `ChaosTimeline`.

```csharp
var engine = new ChaosEngine();
```

### Activating Faults

`ActivateFault` returns a unique fault ID (a GUID) used for targeted deactivation:

```csharp
// Activate a disk I/O error on broker 1
var faultId = engine.ActivateFault(
    FaultType.DiskIoError,
    new FaultScope { BrokerId = 1 }
);

// Activate slow network with 200ms latency
var latencyId = engine.ActivateFault(
    FaultType.SlowNetwork,
    new FaultScope { BrokerId = 2 },
    latency: TimeSpan.FromMilliseconds(200)
);
```

### Deactivating Faults

```csharp
// Deactivate a specific fault
engine.DeactivateFault(faultId);

// Deactivate ALL active faults
engine.DeactivateAll();
```

### FaultScope Targeting

The `FaultScope` record controls which operations are affected. Null properties act as wildcards:

```csharp
public sealed record FaultScope
{
    public int? BrokerId { get; init; }        // null = all brokers
    public string? Topic { get; init; }         // null = all topics
    public int? Partition { get; init; }         // null = all partitions
    public int? TargetPeerId { get; init; }      // null = all peers
    public double Probability { get; init; } = 1.0; // 0.0 to 1.0
}
```

**Examples:**

```csharp
// Target only broker 1
new FaultScope { BrokerId = 1 }

// Target broker 1 communicating with peer 2
new FaultScope { BrokerId = 1, TargetPeerId = 2 }

// 50% probability of triggering
new FaultScope { BrokerId = 1, Probability = 0.5 }

// Global scope (affects everything)
new FaultScope()
```

### ChaosTimeline for Diagnostics

Every fault activation, deactivation, and trigger is recorded:

```csharp
// Get all events
IReadOnlyList<ChaosEvent> events = engine.Timeline.GetEvents();

// Filter by fault type
var diskEvents = engine.Timeline.GetEvents(FaultType.DiskIoError);

// Filter by event type (Activated, Deactivated, Triggered)
var triggered = engine.Timeline.GetEvents(ChaosEventType.Triggered);

// Dump to console for debugging
engine.Timeline.DumpToOutput(Console.WriteLine);
// Output:
// === Chaos Timeline (3 events) ===
// [14:32:01.234] Activated | DiskIoError | DiskIoError fault on broker=1, ...
// [14:32:01.567] Triggered | DiskIoError | Triggered: DiskIoError on broker=1, peer=-1
// [14:32:02.890] Deactivated | DiskIoError | Deactivated: DiskIoError fault on broker=1, ...
// === End Timeline ===
```

### Querying Active Faults

```csharp
// Check if a specific fault type is active for a broker
bool active = engine.IsFaultActive(FaultType.DiskIoError, brokerId: 1);

// Get injected latency for a fault type
TimeSpan? latency = engine.GetInjectedLatency(FaultType.SlowNetwork, brokerId: 2);

// Snapshot of all active faults
IReadOnlyCollection<ActiveFault> faults = engine.ActiveFaults;
```

---

[Back to Unit 31](../README.md)
