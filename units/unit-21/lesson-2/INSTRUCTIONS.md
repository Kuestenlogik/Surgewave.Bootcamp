# Lesson 4.2: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Create Topic with Replication

```csharp
await admin.CreateTopicAsync(new TopicSpecification
{
    Name = "durable-events",
    NumPartitions = 3,
    ReplicationFactor = 3,
    Config = new Dictionary<string, string>
    {
        ["min.insync.replicas"] = "2"
    }
});
```

## Step 2: Configure Producer for Durability

```csharp
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.Acks = Acks.All;  // Wait for all ISR replicas
});
```

## Step 3: Check Replication Status

```csharp
var metadata = await admin.GetMetadataAsync();
var topic = metadata.Topics.First(t => t.Name == "durable-events");

foreach (var partition in topic.Partitions)
{
    Console.WriteLine($"Partition {partition.Id}:");
    Console.WriteLine($"  Leader: Broker {partition.Leader}");
    Console.WriteLine($"  Replicas: [{string.Join(", ", partition.Replicas)}]");
    Console.WriteLine($"  ISR: [{string.Join(", ", partition.Isr)}]");

    var underReplicated = partition.Replicas.Count > partition.Isr.Count;
    if (underReplicated)
        Console.WriteLine($"  WARNING: Under-replicated!");
}
```

## Step 4: Monitor ISR Changes

```csharp
// Periodically check ISR status
while (true)
{
    var metadata = await admin.GetMetadataAsync();

    foreach (var topic in metadata.Topics)
    {
        foreach (var partition in topic.Partitions)
        {
            if (partition.Isr.Count < partition.Replicas.Count)
            {
                Console.WriteLine($"ALERT: {topic.Name}/{partition.Id} under-replicated");
                Console.WriteLine($"  Expected: {partition.Replicas.Count}, ISR: {partition.Isr.Count}");
            }
        }
    }

    await Task.Delay(TimeSpan.FromSeconds(5));
}
```

## Step 5: Test Replica Failure

```csharp
// Send message with Acks.All
var result = await producer.ProduceAsync("durable-events", "key", "value");
Console.WriteLine($"Message stored on {metadata.GetPartition(result.Partition).Isr.Count} replicas");

// Simulate broker failure
Console.WriteLine("Stopping broker 3...");
await broker3.DisposeAsync();

// Verify writes still work (with min.insync.replicas=2)
await producer.ProduceAsync("durable-events", "key2", "value2");
Console.WriteLine("Write succeeded with 2 ISR replicas");

// Stop another broker - writes should fail
Console.WriteLine("Stopping broker 2...");
await broker2.DisposeAsync();

try
{
    await producer.ProduceAsync("durable-events", "key3", "value3");
}
catch (NotEnoughReplicasException)
{
    Console.WriteLine("Write failed: Not enough ISR replicas");
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. What's the difference between replicas and ISR?
2. Why set min.insync.replicas < replication factor?
3. How does Acks.All interact with ISR?

---

**Next:** → [Lesson 4.3: Monitoring](../lesson-3/README.md)
