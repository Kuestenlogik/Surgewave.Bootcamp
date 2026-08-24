# Lesson 35.3: Storage Fault Injection

### ChaosLogSegmentFactory and ChaosLogSegment

The `ChaosLogSegmentFactory` wraps an `ILogSegmentFactory` so that every segment it creates is a `ChaosLogSegment`. The chaos segment delegates to the real segment but checks for active faults before each operation.

```csharp
var innerFactory = new MemoryLogSegmentFactory();
var chaosFactory = new ChaosLogSegmentFactory(innerFactory, engine, brokerId: 0, loggerFactory);
```

### Integration via SurgewaveRuntimeBuilder

The easiest way to wire chaos into a runtime:

```csharp
var engine = new ChaosEngine();
var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithBrokerId(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithChaosEngine(engine, loggerFactory)  // Wraps storage with chaos
    .Build()
    .StartAsync();
```

### Intercepted Operations

The `ChaosLogSegment` intercepts these storage operations:

| Operation | DiskIoError | StorageFullError | MessageCorruption | SlowNetwork | PartialWrite | NodeCrash |
|-----------|-------------|------------------|-------------------|-------------|--------------|-----------|
| `AppendBatchAsync` | IOException | IOException | -- | Delay | Truncates data | IOException |
| `ReadBatchesAsync` | IOException | -- | Bit flips in CRC | Delay | -- | IOException |
| `FlushAsync` | IOException | -- | -- | Delay | -- | IOException |
| `GetFirstMessageOffset` | IOException | -- | -- | -- | -- | IOException |

### How Each Fault Works

**DiskIoError** -- Throws `IOException` on any read or write:
```csharp
engine.ActivateFault(FaultType.DiskIoError, new FaultScope { BrokerId = 0 });
// All storage operations on broker 0 now throw:
// IOException: "Chaos: Disk I/O error simulated on broker 0"
```

**StorageFullError** -- Throws `IOException` on writes only:
```csharp
engine.ActivateFault(FaultType.StorageFullError, new FaultScope { BrokerId = 0 });
// Write operations throw:
// IOException: "Chaos: No space left on device (simulated) on broker 0"
```

**MessageCorruption** -- Flips 1-3 random bits in read data:
```csharp
engine.ActivateFault(FaultType.MessageCorruption, new FaultScope { BrokerId = 0 });
// ReadBatchesAsync returns data with subtle CRC corruption
// This tests whether consumers detect and handle corrupt messages
```

**SlowNetwork** -- Injects configurable latency:
```csharp
engine.ActivateFault(FaultType.SlowNetwork, new FaultScope { BrokerId = 0 },
    latency: TimeSpan.FromMilliseconds(500));
// Every storage operation is delayed by 500ms
```

**PartialWrite** -- Truncates write data to half:
```csharp
engine.ActivateFault(FaultType.PartialWrite, new FaultScope { BrokerId = 0 });
// AppendBatchAsync writes only the first half of the data
// Tests recovery from incomplete writes
```

---

[Back to Unit 31](../README.md)
