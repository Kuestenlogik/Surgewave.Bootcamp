# Lesson 5.3: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Set Up Dual Brokers

```csharp
// Surgewave broker (new)
var surgewave = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageMode(StorageMode.Memory)
    .Build()
    .StartAsync();

// In production, you'd have Kafka running on different ports
// Kafka would typically be on 9093 or a different host
```

## Step 2: Implement Dual-Write Producer

```csharp
public class DualWriteProducer : IAsyncDisposable
{
    private readonly SurgewaveProducer<string, string> _surgewaveProducer;
    private readonly SurgewaveProducer<string, string> _kafkaProducer; // Or Confluent.Kafka

    public DualWriteProducer(string surgewaveBootstrap, string kafkaBootstrap)
    {
        _surgewaveProducer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = surgewaveBootstrap);

        _kafkaProducer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = kafkaBootstrap);
    }

    public async Task ProduceAsync(string topic, string key, string value)
    {
        // Write to both systems
        var surgewaveTask = _surgewaveProducer.ProduceAsync(topic, key, value);
        var kafkaTask = _kafkaProducer.ProduceAsync(topic, key, value);

        await Task.WhenAll(surgewaveTask, kafkaTask);
    }
}
```

## Step 3: Implement Topic Router

```csharp
public class TopicRouter
{
    private readonly Dictionary<string, string> _topicMapping = new()
    {
        // Topics migrated to Surgewave
        ["events"] = "surgewave",
        ["logs"] = "surgewave",

        // Topics still on Kafka
        ["orders"] = "kafka",
        ["users"] = "kafka"
    };

    private readonly SurgewaveProducer<string, string> _surgewaveProducer;
    private readonly SurgewaveProducer<string, string> _kafkaProducer;

    public async Task ProduceAsync(string topic, string key, string value)
    {
        var target = _topicMapping.GetValueOrDefault(topic, "kafka");

        if (target == "surgewave")
            await _surgewaveProducer.ProduceAsync(topic, key, value);
        else
            await _kafkaProducer.ProduceAsync(topic, key, value);
    }
}
```

## Step 4: Implement Data Mirror

```csharp
public class DataMirror
{
    public async Task MirrorTopicAsync(
        string topic,
        SurgewaveConsumer<string, string> source,
        SurgewaveProducer<string, string> destination)
    {
        source.Subscribe(topic);

        while (true)
        {
            var result = await source.ConsumeAsync(TimeSpan.FromSeconds(1));
            if (result == null) break;

            await destination.ProduceAsync(topic, result.Key, result.Value);

            Console.WriteLine($"Mirrored: {topic}/{result.Partition}@{result.Offset}");
        }
    }
}
```

## Step 5: Validate Data Consistency

```csharp
public async Task<bool> ValidateConsistency(string topic)
{
    var kafkaCount = await CountMessages(kafkaConsumer, topic);
    var surgewaveCount = await CountMessages(surgewaveConsumer, topic);

    Console.WriteLine($"Kafka: {kafkaCount} messages");
    Console.WriteLine($"Surgewave: {surgewaveCount} messages");

    return kafkaCount == surgewaveCount;
}
```

## Migration Strategy

1. **Phase 1**: Set up Surgewave, mirror low-risk topics
2. **Phase 2**: Dual-write new topics to both systems
3. **Phase 3**: Verify consistency, switch consumers
4. **Phase 4**: Stop writing to Kafka, decommission

## Complete Solution

See `completed/Program.cs` for the full implementation.

---

**Next:** → [Lesson 5.4: Production Cutover](../lesson-4/README.md)
