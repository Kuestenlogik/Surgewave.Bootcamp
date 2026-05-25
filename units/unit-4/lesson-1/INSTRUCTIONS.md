# Lesson 6.1: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Define Your Domain

Before creating topics, map your domain:

```
E-Commerce Domain
├── Orders
│   ├── Order Created
│   ├── Order Updated
│   ├── Order Shipped
│   └── Order Cancelled
├── Inventory
│   ├── Stock Updated
│   ├── Stock Reserved
│   └── Stock Released
├── Payments
│   ├── Payment Initiated
│   ├── Payment Completed
│   └── Payment Failed
└── Users
    ├── User Registered
    ├── User Updated
    └── User Deleted
```

## Step 2: Apply Naming Convention

```csharp
public static class TopicNames
{
    // Pattern: <domain>.<aggregate>.<version>

    // Orders domain
    public const string OrderEvents = "orders.order.v1";
    public const string OrderCommands = "orders.order.commands.v1";

    // Inventory domain
    public const string InventoryEvents = "inventory.stock.v1";

    // Payments domain
    public const string PaymentEvents = "payments.payment.v1";

    // Users domain
    public const string UserEvents = "users.user.v1";

    // Cross-cutting
    public const string AuditLog = "platform.audit.v1";
    public const string DeadLetterQueue = "platform.dlq.v1";
}
```

## Step 3: Create Topics with Appropriate Settings

```csharp
public class TopicConfiguration
{
    public string Name { get; init; } = "";
    public int Partitions { get; init; } = 6;
    public short ReplicationFactor { get; init; } = 3;
    public TimeSpan Retention { get; init; } = TimeSpan.FromDays(7);
    public bool Compacted { get; init; } = false;
}

var topicConfigs = new Dictionary<string, TopicConfiguration>
{
    // High-volume event streams
    ["orders.order.v1"] = new()
    {
        Partitions = 12,
        Retention = TimeSpan.FromDays(30)
    },

    // State snapshots (compacted)
    ["inventory.stock.state.v1"] = new()
    {
        Partitions = 6,
        Compacted = true,
        Retention = TimeSpan.MaxValue // Keep forever
    },

    // Audit log (long retention)
    ["platform.audit.v1"] = new()
    {
        Partitions = 3,
        Retention = TimeSpan.FromDays(365)
    }
};
```

## Step 4: Implement Topic Creation

```csharp
public async Task CreateDomainTopics(SurgewaveAdminClient admin)
{
    foreach (var (name, config) in topicConfigs)
    {
        var spec = new TopicSpecification
        {
            Name = name,
            NumPartitions = config.Partitions,
            ReplicationFactor = config.ReplicationFactor,
            Config = new Dictionary<string, string>
            {
                ["retention.ms"] = config.Retention.TotalMilliseconds.ToString(),
                ["cleanup.policy"] = config.Compacted ? "compact" : "delete"
            }
        };

        await admin.CreateTopicAsync(spec);
        Console.WriteLine($"Created topic: {name}");
    }
}
```

## Step 5: Validate Topic Design

Use this checklist:

```csharp
public class TopicDesignValidator
{
    public List<string> Validate(string topicName, TopicConfiguration config)
    {
        var issues = new List<string>();

        // Naming
        if (!topicName.Contains('.'))
            issues.Add("Topic name should use dot notation");

        if (topicName.Any(char.IsUpper))
            issues.Add("Topic name should be lowercase");

        // Partitions
        if (config.Partitions < 3)
            issues.Add("Consider at least 3 partitions for availability");

        if (config.Partitions > 50)
            issues.Add("Too many partitions increases overhead");

        // Retention
        if (config.Retention < TimeSpan.FromHours(1))
            issues.Add("Very short retention may cause data loss");

        return issues;
    }
}
```

## Design Anti-Patterns

### Anti-Pattern 1: The God Topic

```
❌ BAD: all-events
   - Contains orders, users, payments, everything
   - Consumers must filter irrelevant messages
   - Can't scale independently
   - Mixed retention requirements

✅ GOOD: Separate by domain
   - orders.events
   - users.events
   - payments.events
```

### Anti-Pattern 2: Topic Per Entity Instance

```
❌ BAD: user-{userId}-events
   - Thousands of topics
   - No cross-user ordering when needed
   - Management nightmare

✅ GOOD: Partition by entity ID
   - users.events (partitioned by userId)
   - Ordering per user, scalable
```

### Anti-Pattern 3: Technology in Names

```
❌ BAD: kafka-orders-topic
   - Tied to implementation

✅ GOOD: orders.order.v1
   - Describes content, not technology
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. How do you decide between one topic vs multiple topics?
2. When should you use compacted topics?
3. How does topic design affect consumer groups?

---

**Next:** → [Lesson 6.2: Partitioning Strategies](../lesson-2/README.md)
