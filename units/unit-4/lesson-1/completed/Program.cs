// Surgewave Bootcamp - Unit 6, Lesson 1: Topic Design
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client.Native;

// Topic names following naming conventions
public static class TopicNames
{
    // Pattern: <domain>.<aggregate>.<version>

    // Orders domain
    public const string OrderEvents = "orders.order.v1";
    public const string OrderCommands = "orders.order.commands.v1";

    // Inventory domain
    public const string InventoryEvents = "inventory.stock.v1";
    public const string InventoryState = "inventory.stock.state.v1";

    // Payments domain
    public const string PaymentEvents = "payments.payment.v1";

    // Users domain
    public const string UserEvents = "users.user.v1";

    // Platform / Cross-cutting
    public const string AuditLog = "platform.audit.v1";
    public const string DeadLetterQueue = "platform.dlq.v1";
}

public record TopicConfiguration(
    string Name,
    int Partitions = 6,
    short ReplicationFactor = 1,
    TimeSpan? Retention = null,
    bool Compacted = false)
{
    public TimeSpan EffectiveRetention => Retention ?? TimeSpan.FromDays(7);
}

public class TopicDesignValidator
{
    public List<string> Validate(TopicConfiguration config)
    {
        var issues = new List<string>();

        // Naming validation
        if (!config.Name.Contains('.'))
            issues.Add($"[{config.Name}] Should use dot notation (domain.entity.version)");

        if (config.Name.Any(char.IsUpper))
            issues.Add($"[{config.Name}] Should be lowercase");

        if (!config.Name.Contains(".v"))
            issues.Add($"[{config.Name}] Consider adding version suffix (.v1)");

        // Partition validation
        if (config.Partitions < 3)
            issues.Add($"[{config.Name}] Consider at least 3 partitions for HA");

        if (config.Partitions > 50)
            issues.Add($"[{config.Name}] Many partitions increase overhead");

        // Retention validation
        if (config.EffectiveRetention < TimeSpan.FromHours(1))
            issues.Add($"[{config.Name}] Very short retention may cause data loss");

        return issues;
    }
}

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 6.1 - Topic Design ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .Build()
            .StartAsync();

        await using var client = new SurgewaveNativeClient("localhost", runtime.Port);
        await client.ConnectAsync();

        // Define topic configurations for e-commerce domain
        var topics = new List<TopicConfiguration>
        {
            // High-volume event streams
            new(TopicNames.OrderEvents, Partitions: 12, Retention: TimeSpan.FromDays(30)),
            new(TopicNames.PaymentEvents, Partitions: 6, Retention: TimeSpan.FromDays(30)),
            new(TopicNames.UserEvents, Partitions: 6, Retention: TimeSpan.FromDays(90)),

            // State topics (compacted)
            new(TopicNames.InventoryState, Partitions: 6, Compacted: true),

            // Platform topics
            new(TopicNames.AuditLog, Partitions: 3, Retention: TimeSpan.FromDays(365)),
            new(TopicNames.DeadLetterQueue, Partitions: 3, Retention: TimeSpan.FromDays(30))
        };

        // Validate topic design
        Console.WriteLine("--- Validating Topic Design ---");
        var validator = new TopicDesignValidator();
        var allValid = true;

        foreach (var topic in topics)
        {
            var issues = validator.Validate(topic);
            if (issues.Count == 0)
            {
                Console.WriteLine($"  [OK] {topic.Name}");
            }
            else
            {
                allValid = false;
                Console.WriteLine($"  [WARN] {topic.Name}");
                foreach (var issue in issues)
                {
                    Console.WriteLine($"       - {issue}");
                }
            }
        }

        if (allValid)
            Console.WriteLine("All topics passed validation!");

        Console.WriteLine();

        // Create topics
        Console.WriteLine("--- Creating Topics ---");
        foreach (var topic in topics)
        {
            var builder = client.Topics.Create(topic.Name)
                .WithPartitions(topic.Partitions)
                .WithReplicationFactor(topic.ReplicationFactor)
                .WithRetention(topic.EffectiveRetention);

            if (topic.Compacted)
            {
                builder.WithCompaction();
            }

            await builder.ExecuteAsync();

            var type = topic.Compacted ? "compacted" : "standard";
            Console.WriteLine($"  Created: {topic.Name} ({topic.Partitions} partitions, {type})");
        }

        Console.WriteLine();

        // Display topic structure
        Console.WriteLine("--- Topic Structure ---");
        Console.WriteLine(@"
E-Commerce Domain Topics
├── Orders
│   └── orders.order.v1 (12 partitions, 30d retention)
├── Inventory
│   └── inventory.stock.state.v1 (6 partitions, compacted)
├── Payments
│   └── payments.payment.v1 (6 partitions, 30d retention)
├── Users
│   └── users.user.v1 (6 partitions, 90d retention)
└── Platform
    ├── platform.audit.v1 (3 partitions, 365d retention)
    └── platform.dlq.v1 (3 partitions, 30d retention)
");

        // Design principles
        Console.WriteLine("--- Topic Design Principles ---");
        Console.WriteLine("1. Name by domain, not technology (orders.order.v1, not kafka-orders)");
        Console.WriteLine("2. One topic per aggregate type");
        Console.WriteLine("3. Include version for schema evolution");
        Console.WriteLine("4. Use compaction for state/snapshot topics");
        Console.WriteLine("5. Set retention based on business requirements");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
