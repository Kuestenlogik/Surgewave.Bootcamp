# Lesson 6.2: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Understand Default Partitioning

Surgewave uses murmur2 hash of the key:

```csharp
int partition = Math.Abs(MurmurHash2(keyBytes)) % numPartitions;
```

- Same key → same partition (guaranteed)
- Null key → round-robin (no ordering)

## Step 2: Implement Custom Partitioner

```csharp
public class TenantAwarePartitioner : IPartitioner
{
    public int Partition(string topic, byte[]? key, int partitionCount)
    {
        if (key == null)
            return Random.Shared.Next(partitionCount);

        // Extract tenant from composite key "tenant:entity"
        var keyStr = Encoding.UTF8.GetString(key);
        var tenant = keyStr.Split(':')[0];

        // Hash tenant for consistent routing
        return Math.Abs(tenant.GetHashCode()) % partitionCount;
    }
}

// Usage
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.Partitioner = new TenantAwarePartitioner();
});
```

## Step 3: Analyze Partition Distribution

```csharp
public async Task AnalyzeDistribution(SurgewaveAdminClient admin, string topic)
{
    var offsets = await admin.GetTopicOffsetsAsync(topic);

    var total = offsets.Sum(o => o.High - o.Low);

    Console.WriteLine($"Topic: {topic}");
    Console.WriteLine($"Total messages: {total}");
    Console.WriteLine();

    foreach (var offset in offsets.OrderBy(o => o.Partition))
    {
        var count = offset.High - offset.Low;
        var percentage = total > 0 ? (double)count / total * 100 : 0;
        var bar = new string('█', (int)(percentage / 5));

        Console.WriteLine($"  P{offset.Partition}: {bar} {percentage:F1}% ({count:N0})");
    }

    // Check for skew
    var avg = total / (double)offsets.Count;
    var maxSkew = offsets.Max(o => Math.Abs((o.High - o.Low) - avg) / avg);

    if (maxSkew > 0.5)
        Console.WriteLine($"\n  WARNING: High skew detected ({maxSkew:P0})");
}
```

## Step 4: Partition Count Guidelines

```csharp
public int RecommendPartitionCount(
    int expectedThroughputMsgsPerSec,
    int consumerCount,
    int msgsPerSecPerPartition = 10000)
{
    // Rule 1: At least as many partitions as consumers
    var byConsumers = consumerCount;

    // Rule 2: Enough for throughput
    var byThroughput = (int)Math.Ceiling(
        expectedThroughputMsgsPerSec / (double)msgsPerSecPerPartition);

    // Rule 3: Plan for growth (2-3x current)
    var recommended = Math.Max(byConsumers, byThroughput) * 2;

    // Rule 4: Keep it reasonable
    return Math.Clamp(recommended, 3, 100);
}
```

## Step 5: Handle Partition Rebalancing

When partition count changes, keys may route differently:

```csharp
// Before: 4 partitions
// "user-123" → partition 2

// After: 8 partitions
// "user-123" → partition 6 (DIFFERENT!)

// Solutions:
// 1. Plan partition count upfront
// 2. Use sticky partitioning for stateful consumers
// 3. Handle rebalancing in consumer logic
```

## Partitioning Decision Matrix

| Requirement | Key Strategy | Example |
|-------------|--------------|---------|
| Order per customer | Customer ID | `cust-123` |
| Order per tenant | Tenant ID | `tenant-abc` |
| Global ordering | Single partition | `constant-key` |
| Max throughput | Null key (round-robin) | `null` |
| Order per session | Session ID | `sess-xyz` |
| Geographic routing | Region + Entity | `us:user-123` |

## Anti-Patterns

### Anti-Pattern 1: Timestamp as Key

```
❌ BAD: Using timestamp as partition key
   - All recent messages go to same partition
   - Hot partition during peak hours
   - No meaningful ordering

✅ GOOD: Use entity ID, add timestamp to value
```

### Anti-Pattern 2: Too Few Partitions

```
❌ BAD: 1-2 partitions
   - Can't scale consumers
   - Single point of bottleneck

✅ GOOD: Start with 6-12, plan for growth
```

### Anti-Pattern 3: Too Many Partitions

```
❌ BAD: 1000 partitions "just in case"
   - Memory overhead per partition
   - Slower rebalancing
   - More file handles

✅ GOOD: 2-3x current needs, increase later if needed
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

---

**Next:** → [Lesson 6.3: Event Schema Design](../lesson-3/README.md)
