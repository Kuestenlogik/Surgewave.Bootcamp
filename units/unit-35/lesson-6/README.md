# Lesson 35.6: ChaosCluster for Integration Tests

### Creating a Multi-Broker Chaos Cluster

The `ChaosCluster` provides a complete multi-broker environment with integrated chaos injection. Each broker gets its own `ChaosEngine`.

```csharp
await using var cluster = await ChaosCluster.CreateAsync(brokerCount: 3, loggerFactory);
```

Each broker is created with:
- In-memory storage wrapped in `ChaosLogSegmentFactory`
- Auto-created topics enabled
- A unique `ChaosEngine` per broker

### Convenience Methods

```csharp
// Isolate broker 1 from the cluster
var partition = cluster.PartitionBroker(brokerId: 1);

// Simulate a crash on broker 2
var crash = cluster.CrashBroker(brokerId: 2);

// Add 100ms latency to broker 0
cluster.InjectLatency(brokerId: 0, TimeSpan.FromMilliseconds(100));

// Heal everything
cluster.HealAll();
```

### Producing and Consuming

```csharp
// Create a topic on broker 0
await cluster.CreateTopicAsync("test-topic", partitions: 3);

// Produce 100 messages through broker 0
await cluster.ProduceAsync("test-topic", messageCount: 100, brokerId: 0);

// Consume messages from broker 1
var messages = await cluster.ConsumeAsync("test-topic",
    expectedCount: 100,
    timeout: TimeSpan.FromSeconds(10),
    brokerId: 1);
```

### Full Integration Test Example

```csharp
[Fact]
public async Task Leader_Failover_After_Network_Partition()
{
    await using var cluster = await ChaosCluster.CreateAsync(3);

    // Create topic and produce initial data
    await cluster.CreateTopicAsync("failover-test");
    await cluster.ProduceAsync("failover-test", 50);

    // Partition broker 0 (the initial leader)
    using var partition = cluster.PartitionBroker(0);

    // Verify remaining brokers can still produce
    await cluster.ProduceAsync("failover-test", 50, brokerId: 1);

    // Heal the partition
    partition.Heal();

    // Verify all data is accessible
    var messages = await cluster.ConsumeAsync("failover-test",
        expectedCount: 100, timeout: TimeSpan.FromSeconds(15));

    Assert.Equal(100, messages.Count);

    // Inspect the chaos timeline
    var engine = cluster.GetEngine(0);
    engine.Timeline.DumpToOutput(output.WriteLine);
}
```

### Accessing Individual Engines and Runtimes

```csharp
// Get a broker's chaos engine for custom fault injection
var engine = cluster.GetEngine(brokerId: 1);
engine.ActivateFault(FaultType.MessageCorruption, new FaultScope { BrokerId = 1 });

// Get a broker's runtime for direct access
var runtime = cluster.GetRuntime(brokerId: 0);

// Get bootstrap servers for Kafka client configuration
var servers = cluster.GetBootstrapServers(brokerId: 0);

// List all broker IDs
IReadOnlyList<int> ids = cluster.BrokerIds; // [0, 1, 2]
```

---

[Back to Unit 31](../README.md)
