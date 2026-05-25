# Lesson 4.1: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Start Multiple Brokers

```csharp
// Start broker 1
var broker1 = await SurgewaveRuntime.CreateBuilder()
    .WithBrokerId(1)
    .WithPort(9092)
    .WithStorageDirectory("./data/broker1")
    .Build()
    .StartAsync();

// Start broker 2
var broker2 = await SurgewaveRuntime.CreateBuilder()
    .WithBrokerId(2)
    .WithPort(9093)
    .WithStorageDirectory("./data/broker2")
    .WithClusterSeeds("localhost:9092")
    .Build()
    .StartAsync();

// Start broker 3
var broker3 = await SurgewaveRuntime.CreateBuilder()
    .WithBrokerId(3)
    .WithPort(9094)
    .WithStorageDirectory("./data/broker3")
    .WithClusterSeeds("localhost:9092")
    .Build()
    .StartAsync();
```

## Step 2: Connect to Cluster

```csharp
var producer = new SurgewaveProducer<string, string>(opt =>
{
    // List multiple brokers for failover
    opt.BootstrapServers = "localhost:9092,localhost:9093,localhost:9094";
});
```

## Step 3: Create Topic with Replication

```csharp
var admin = new SurgewaveAdminClient(opt =>
    opt.BootstrapServers = "localhost:9092,localhost:9093,localhost:9094");

await admin.CreateTopicAsync(new TopicSpecification
{
    Name = "events",
    NumPartitions = 6,
    ReplicationFactor = 3
});
```

## Step 4: View Cluster Metadata

```csharp
var metadata = await admin.GetMetadataAsync();

Console.WriteLine("Cluster ID: " + metadata.ClusterId);
Console.WriteLine("Controller: Broker " + metadata.ControllerId);
Console.WriteLine();
Console.WriteLine("Brokers:");
foreach (var broker in metadata.Brokers)
{
    Console.WriteLine($"  Broker {broker.Id}: {broker.Host}:{broker.Port}");
}

Console.WriteLine();
Console.WriteLine("Topics:");
foreach (var topic in metadata.Topics)
{
    Console.WriteLine($"  {topic.Name}:");
    foreach (var partition in topic.Partitions)
    {
        Console.WriteLine($"    Partition {partition.Id}: Leader={partition.Leader}, Replicas=[{string.Join(",", partition.Replicas)}]");
    }
}
```

## Step 5: Test Failover

```csharp
// Produce some messages
for (int i = 0; i < 100; i++)
{
    await producer.ProduceAsync("events", $"key-{i}", $"value-{i}");
}

Console.WriteLine("Stopping broker 1...");
await broker1.DisposeAsync();

// Producer should continue working via other brokers
for (int i = 100; i < 200; i++)
{
    await producer.ProduceAsync("events", $"key-{i}", $"value-{i}");
}

Console.WriteLine("Messages sent successfully after broker failure!");
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. What happens when a broker in the cluster fails?
2. How do clients discover all brokers in a cluster?
3. What is the controller broker's role?

---

**Next:** → [Lesson 4.2: Replication](../lesson-2/README.md)
