# Lesson 5.2: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Understand the Migration

The API wrapper provides the same types as Confluent.Kafka:

```csharp
// Both packages provide these types:
using Confluent.Kafka;

// ProducerConfig, ConsumerConfig
// ProducerBuilder<K,V>, ConsumerBuilder<K,V>
// Message<K,V>, ConsumeResult<K,V>
// etc.
```

## Step 2: Update Package Reference

In your .csproj file:

```xml
<!-- Before -->
<PackageReference Include="Confluent.Kafka" Version="2.x" />

<!-- After -->
<PackageReference Include="Kuestenlogik.Surgewave.Kafka.Compat" Version="1.x" />
```

## Step 3: Existing Code Continues to Work

```csharp
// This code works with BOTH packages!
using Confluent.Kafka;  // Provided by Surgewave.Kafka.Compat

var config = new ProducerConfig
{
    BootstrapServers = "localhost:9092",
    Acks = Acks.All
};

using var producer = new ProducerBuilder<string, string>(config).Build();

await producer.ProduceAsync("topic",
    new Message<string, string> { Key = "key", Value = "value" });
```

## Step 4: Consumer Code Works Too

```csharp
using Confluent.Kafka;  // From Surgewave.Kafka.Compat

var config = new ConsumerConfig
{
    BootstrapServers = "localhost:9092",
    GroupId = "my-group",
    AutoOffsetReset = AutoOffsetReset.Earliest
};

using var consumer = new ConsumerBuilder<string, string>(config).Build();
consumer.Subscribe("topic");

while (true)
{
    var result = consumer.Consume(TimeSpan.FromSeconds(1));
    if (result != null)
        Console.WriteLine($"{result.Message.Key}: {result.Message.Value}");
}
```

## Step 5: Test Thoroughly

After switching packages:

1. Run unit tests
2. Run integration tests
3. Test in staging environment
4. Monitor for errors

## Step 6: Handle Any Differences

Most code works unchanged. If you encounter issues:

```csharp
// Check for Surgewave-specific configuration
#if Surgewave_COMPAT
    // Surgewave-specific handling
#else
    // Kafka-specific handling
#endif
```

## Migration Checklist

- [ ] Update NuGet package reference
- [ ] Build project (should succeed without changes)
- [ ] Run existing tests
- [ ] Update bootstrap servers to Surgewave
- [ ] Test in non-production environment
- [ ] Deploy to production

## Complete Solution

See `completed/Program.cs` for the full implementation.

---

**Next:** → [Lesson 5.3: Hybrid Deployments](../lesson-3/README.md)
