# Lesson 35.4: Transport Fault Injection

### ChaosRaftTransport

The `ChaosRaftTransport` wraps an `IRaftTransport` to inject faults into Raft consensus communication between brokers.

```csharp
var chaosTransport = new ChaosRaftTransport(innerTransport, engine, brokerId: 0, loggerFactory);
```

### Intercepted Raft RPCs

| RPC | NetworkPartition | LeaderElectionDisruption | NodeCrash | ConnectionReset | SlowNetwork |
|-----|------------------|--------------------------|-----------|-----------------|-------------|
| `SendPreVoteAsync` | IOException | Returns VoteGranted=false | InvalidOperationException | IOException | Delay |
| `SendRequestVoteAsync` | IOException | Returns VoteGranted=false | InvalidOperationException | IOException | Delay |
| `SendAppendEntriesAsync` | IOException | -- | InvalidOperationException | IOException | Delay |
| `IsPeerReachableAsync` | Returns false | -- | InvalidOperationException | -- | -- |

### How Each Fault Works

**NetworkPartition** -- Blocks specific peer communication:
```csharp
engine.ActivateFault(FaultType.NetworkPartition, new FaultScope
{
    BrokerId = 0,
    TargetPeerId = 1
});
// Broker 0 cannot communicate with peer 1
// Throws IOException on send, returns false on reachability check
```

**LeaderElectionDisruption** -- Drops vote requests:
```csharp
engine.ActivateFault(FaultType.LeaderElectionDisruption, new FaultScope
{
    BrokerId = 0,
    TargetPeerId = 1
});
// PreVote and RequestVote to peer 1 return VoteGranted=false
// AppendEntries is NOT affected (only election traffic)
```

**NodeCrash** -- All operations throw:
```csharp
engine.ActivateFault(FaultType.NodeCrash, new FaultScope { BrokerId = 0 });
// Every transport operation throws InvalidOperationException
// Every storage operation throws IOException
```

**ConnectionReset** -- Simulates TCP connection reset:
```csharp
engine.ActivateFault(FaultType.ConnectionReset, new FaultScope
{
    BrokerId = 0,
    TargetPeerId = 2
});
// Throws IOException: "Connection reset by peer 2 (simulated)"
```

**SlowNetwork** -- Per-peer configurable latency:
```csharp
engine.ActivateFault(FaultType.SlowNetwork, new FaultScope { BrokerId = 0 },
    latency: TimeSpan.FromMilliseconds(300));
// Every RPC from broker 0 is delayed by 300ms
```

---

[Back to Unit 31](../README.md)
