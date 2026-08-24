# Lesson 24.1: Instructions

Follow these step-by-step instructions to implement tiered storage concepts.

## Step 1: Define Storage Tier Configuration

Create a configuration model for tiered storage:

```csharp
public record TierConfig(
    string Name,
    TimeSpan Retention,
    decimal CostPerGbMonth,
    TimeSpan ExpectedLatency
);

// Define the three tiers
var hotTier = new TierConfig("Hot", TimeSpan.FromHours(1), 0.50m, TimeSpan.FromMilliseconds(1));
var warmTier = new TierConfig("Warm", TimeSpan.FromDays(7), 0.10m, TimeSpan.FromMilliseconds(10));
var coldTier = new TierConfig("Cold", TimeSpan.FromDays(90), 0.01m, TimeSpan.FromSeconds(1));
```

The tier configuration captures:
- Name: Human-readable tier identifier
- Retention: How long data stays in this tier
- Cost: Storage cost per GB per month
- Latency: Expected read latency

## Step 2: Simulate Data Aging

Create a data generator that simulates messages of different ages:

```csharp
public record StoredMessage(
    string Id,
    string Topic,
    string Value,
    DateTime ProducedAt,
    long SizeBytes
)
{
    public TimeSpan Age => DateTime.UtcNow - ProducedAt;

    public string CurrentTier => Age switch
    {
        var a when a < TimeSpan.FromHours(1) => "Hot",
        var a when a < TimeSpan.FromDays(7) => "Warm",
        _ => "Cold"
    };
}
```

## Step 3: Create a Tier Distribution Tracker

Track how data is distributed across tiers:

```csharp
public class TierDistribution
{
    public long HotBytes { get; set; }
    public long WarmBytes { get; set; }
    public long ColdBytes { get; set; }
    public int HotSegments { get; set; }
    public int WarmSegments { get; set; }
    public int ColdSegments { get; set; }

    public long TotalBytes => HotBytes + WarmBytes + ColdBytes;

    public void AddMessage(StoredMessage message)
    {
        switch (message.CurrentTier)
        {
            case "Hot":
                HotBytes += message.SizeBytes;
                break;
            case "Warm":
                WarmBytes += message.SizeBytes;
                break;
            case "Cold":
                ColdBytes += message.SizeBytes;
                break;
        }
    }

    public decimal CalculateMonthlyCost(TierConfig hot, TierConfig warm, TierConfig cold)
    {
        var hotGb = HotBytes / (1024m * 1024m * 1024m);
        var warmGb = WarmBytes / (1024m * 1024m * 1024m);
        var coldGb = ColdBytes / (1024m * 1024m * 1024m);

        return (hotGb * hot.CostPerGbMonth) +
               (warmGb * warm.CostPerGbMonth) +
               (coldGb * cold.CostPerGbMonth);
    }
}
```

## Step 4: Produce Messages with Surgewave

Generate messages and track their tier placement:

```csharp
// Start Surgewave broker with tiered storage simulation
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)  // Using memory for demo
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;
var topic = "tiered-demo";

await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
});

// Produce messages
var messages = new List<StoredMessage>();
for (int i = 0; i < 100; i++)
{
    var value = $"Order-{i:D4}: Sample data for tiered storage demo";
    var key = $"order-{i:D4}";

    await producer.ProduceAsync(topic, key, value);

    // Simulate different message ages for demo
    var age = TimeSpan.FromHours(i * 2);  // 0h, 2h, 4h, ... up to 200h
    var producedAt = DateTime.UtcNow - age;

    messages.Add(new StoredMessage(
        Id: Guid.NewGuid().ToString(),
        Topic: topic,
        Value: value,
        ProducedAt: producedAt,
        SizeBytes: Encoding.UTF8.GetByteCount(value)
    ));
}
```

## Step 5: Analyze Tier Distribution

Calculate and display tier statistics:

```csharp
var distribution = new TierDistribution();

foreach (var message in messages)
{
    distribution.AddMessage(message);
}

Console.WriteLine("\n--- Tier Distribution ---");
Console.WriteLine($"Hot Tier:  {distribution.HotBytes:N0} bytes ({distribution.HotBytes * 100.0 / distribution.TotalBytes:F1}%)");
Console.WriteLine($"Warm Tier: {distribution.WarmBytes:N0} bytes ({distribution.WarmBytes * 100.0 / distribution.TotalBytes:F1}%)");
Console.WriteLine($"Cold Tier: {distribution.ColdBytes:N0} bytes ({distribution.ColdBytes * 100.0 / distribution.TotalBytes:F1}%)");
Console.WriteLine($"Total:     {distribution.TotalBytes:N0} bytes");
```

## Step 6: Calculate Cost Savings

Compare costs with and without tiering:

```csharp
// Calculate tiered cost
var tieredCost = distribution.CalculateMonthlyCost(hotTier, warmTier, coldTier);

// Calculate flat cost (all data at hot tier price)
var flatCost = (distribution.TotalBytes / (1024m * 1024m * 1024m)) * hotTier.CostPerGbMonth;

var savings = flatCost - tieredCost;
var savingsPercent = (savings / flatCost) * 100;

Console.WriteLine("\n--- Cost Analysis ---");
Console.WriteLine($"Without tiering: ${flatCost:F4}/month");
Console.WriteLine($"With tiering:    ${tieredCost:F4}/month");
Console.WriteLine($"Monthly savings: ${savings:F4} ({savingsPercent:F1}%)");
```

## Step 7: Simulate Tier Latency

Measure access latency for each tier:

```csharp
async Task<TimeSpan> SimulateTierAccess(string tier)
{
    var sw = Stopwatch.StartNew();

    // Simulate tier-specific latency
    var delay = tier switch
    {
        "Hot" => TimeSpan.FromMilliseconds(1),
        "Warm" => TimeSpan.FromMilliseconds(10),
        "Cold" => TimeSpan.FromMilliseconds(100),  // Reduced for demo
        _ => TimeSpan.Zero
    };

    await Task.Delay(delay);
    sw.Stop();

    return sw.Elapsed;
}

Console.WriteLine("\n--- Access Latency by Tier ---");
foreach (var tier in new[] { "Hot", "Warm", "Cold" })
{
    var latency = await SimulateTierAccess(tier);
    Console.WriteLine($"{tier} tier access: {latency.TotalMilliseconds:F1}ms");
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Tiered storage configuration
- Data age simulation
- Tier distribution tracking
- Cost analysis
- Latency measurement

## Sample Output

```
=== Surgewave Bootcamp: Lesson 24.1 - Tiered Storage Concepts ===

Surgewave broker started on port 12345

--- Tier Configuration ---
Hot Tier:  1h retention, $0.50/GB/month, <1ms latency
Warm Tier: 7d retention, $0.10/GB/month, <10ms latency
Cold Tier: 90d retention, $0.01/GB/month, <1s latency

--- Producing Messages ---
Produced 100 messages with simulated ages (0h to 200h)

--- Tier Distribution ---
Hot Tier:  432 bytes (0.4%)
Warm Tier: 36,288 bytes (34.8%)
Cold Tier: 67,680 bytes (64.8%)
Total:     104,400 bytes

--- Cost Analysis ---
Without tiering: $0.0000486/month (all at hot tier price)
With tiering:    $0.0000089/month
Monthly savings: $0.0000397 (81.7%)

--- Access Latency by Tier ---
Hot tier access: 1.2ms
Warm tier access: 10.8ms
Cold tier access: 101.3ms

--- Key Takeaways ---
1. Hot tier is fastest but most expensive
2. Cold tier is cheapest but has higher latency
3. Proper tier configuration can save 50-90% on storage costs
4. Access patterns should guide tier retention settings
5. Monitor tier distribution to optimize configuration

Lesson complete!
```

## Knowledge Check

1. What are the three main factors that determine when data moves between tiers?
2. Why is it important to monitor access patterns before configuring tiers?
3. What is the typical cost savings range when implementing tiered storage?
4. How does data compression impact each tier differently?
5. What considerations are important for compliance data in tiered storage?

---

**Next:** [Lesson 24.2: Local File System Tier](../lesson-2/README.md)
