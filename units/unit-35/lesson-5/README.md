# Lesson 35.5: Scenario Helpers

Surgewave provides high-level scenario classes that compose multiple fault activations into meaningful failure patterns. Each scenario implements `IDisposable` for automatic cleanup.

### NetworkPartitionScenario

Creates a bidirectional partition isolating a broker from specified peers:

```csharp
var scenario = NetworkPartitionScenario.Create(engine, isolatedBrokerId: 1, otherBrokerIds: [0, 2]);
// Broker 1 is now isolated from brokers 0 and 2 (both directions)

// Restore communication
scenario.Heal();

// Or use with `using` for automatic cleanup
using var partition = NetworkPartitionScenario.Create(engine, 1, [0, 2]);
// Partition is healed when disposed
```

### BrokerCrashScenario

Simulates a complete broker crash:

```csharp
var crash = BrokerCrashScenario.Create(engine, brokerId: 1);
// All operations on broker 1 now fail

// Recover the broker
crash.Recover();
```

### DiskFailureScenario

Injects disk I/O errors on a broker:

```csharp
// Standard disk failure
var diskFail = DiskFailureScenario.Create(engine, brokerId: 1);

// Or disk-full scenario
var diskFull = DiskFailureScenario.CreateStorageFull(engine, brokerId: 1);

// Repair the disk
diskFail.Repair();
```

### LatencyInjectionScenario

Injects configurable latency:

```csharp
// Add 200ms latency to all operations on broker 1
var latency = LatencyInjectionScenario.Create(engine, brokerId: 1,
    latency: TimeSpan.FromMilliseconds(200));

// Or target a specific peer
var peerLatency = LatencyInjectionScenario.CreateForPeer(engine,
    brokerId: 0, targetPeerId: 1,
    latency: TimeSpan.FromMilliseconds(500));

// With probability (50% of operations are slow)
var probabilistic = LatencyInjectionScenario.Create(engine, brokerId: 1,
    latency: TimeSpan.FromMilliseconds(200), probability: 0.5);

// Remove latency
latency.Remove();
```

---

[Back to Unit 31](../README.md)
