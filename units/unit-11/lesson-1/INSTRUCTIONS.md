# Lesson 5.1: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Start Surgewave Broker

```csharp
// Surgewave broker with Kafka-compatible settings
var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)  // Standard Kafka port
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker listening on port {runtime.Port}");
Console.WriteLine("Kafka clients can connect using standard configuration");
```

## Step 2: Connect with Confluent.Kafka

If you have Confluent.Kafka installed:

```csharp
using Confluent.Kafka;

var producerConfig = new ProducerConfig
{
    BootstrapServers = "localhost:9092"  // Points to Surgewave!
};

using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

// Standard Kafka API - works with Surgewave
var result = await producer.ProduceAsync("my-topic",
    new Message<string, string> { Key = "key", Value = "value" });

Console.WriteLine($"Message delivered to {result.TopicPartitionOffset}");
```

## Step 3: Connect Consumer

```csharp
var consumerConfig = new ConsumerConfig
{
    BootstrapServers = "localhost:9092",
    GroupId = "my-consumer-group",
    AutoOffsetReset = AutoOffsetReset.Earliest
};

using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();

consumer.Subscribe("my-topic");

var result = consumer.Consume(TimeSpan.FromSeconds(5));
if (result != null)
{
    Console.WriteLine($"Received: {result.Message.Key} = {result.Message.Value}");
}
```

## Step 4: Test Admin Operations

```csharp
var adminConfig = new AdminClientConfig
{
    BootstrapServers = "localhost:9092"
};

using var admin = new AdminClientBuilder(adminConfig).Build();

// Create topic (Kafka Admin API)
await admin.CreateTopicsAsync(new[]
{
    new TopicSpecification
    {
        Name = "new-topic",
        NumPartitions = 3,
        ReplicationFactor = 1
    }
});

// List topics
var metadata = admin.GetMetadata(TimeSpan.FromSeconds(10));
foreach (var topic in metadata.Topics)
{
    Console.WriteLine($"Topic: {topic.Topic}, Partitions: {topic.Partitions.Count}");
}
```

## Step 5: Verify Feature Compatibility

Run your existing Kafka code and check:

1. **Produces succeed**: Messages are stored
2. **Consumes work**: Messages are delivered
3. **Consumer groups**: Offsets are tracked
4. **Transactions**: If used, verify atomicity

## Compatibility Notes

### Fully Compatible

- ProducerConfig / ConsumerConfig
- Message headers
- Consumer groups and rebalancing
- Transactions
- Exactly-once semantics
- Admin operations (create/delete topics)

### Differences

- Some broker configs may have different defaults
- Metrics names may differ
- Log format is different (but transparent to clients)

## Complete Solution

See `completed/Program.cs` for the full implementation.

---

**Next:** → [Lesson 5.2: API Wrapper Migration](../lesson-2/README.md)
