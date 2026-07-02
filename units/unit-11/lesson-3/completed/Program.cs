// Surgewave Bootcamp - Unit 5, Lesson 3: Hybrid Deployments
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

// Dual-write producer for hybrid deployments
public class DualWriteProducer : IAsyncDisposable
{
    private readonly SurgewaveProducer<string, string> _primaryProducer;
    private readonly SurgewaveProducer<string, string> _secondaryProducer;
    private readonly string _primaryName;
    private readonly string _secondaryName;

    public DualWriteProducer(
        string primaryBootstrap, string primaryName,
        string secondaryBootstrap, string secondaryName)
    {
        _primaryProducer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = primaryBootstrap);
        _secondaryProducer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = secondaryBootstrap);
        _primaryName = primaryName;
        _secondaryName = secondaryName;
    }

    public async Task ProduceAsync(string topic, string key, string value)
    {
        var primaryTask = _primaryProducer.ProduceAsync(topic, key, value);
        var secondaryTask = _secondaryProducer.ProduceAsync(topic, key, value);

        await Task.WhenAll(primaryTask, secondaryTask);
    }

    public async ValueTask DisposeAsync()
    {
        await _primaryProducer.DisposeAsync();
        await _secondaryProducer.DisposeAsync();
    }
}

// Topic router for gradual migration
public class TopicRouter : IAsyncDisposable
{
    private readonly SurgewaveProducer<string, string> _surgewaveProducer;
    private readonly SurgewaveProducer<string, string> _kafkaProducer;
    private readonly HashSet<string> _migratedTopics;

    public TopicRouter(string surgewaveBootstrap, string kafkaBootstrap, IEnumerable<string> migratedTopics)
    {
        _surgewaveProducer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = surgewaveBootstrap);
        _kafkaProducer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = kafkaBootstrap);
        _migratedTopics = new HashSet<string>(migratedTopics);
    }

    public async Task ProduceAsync(string topic, string key, string value)
    {
        if (_migratedTopics.Contains(topic))
        {
            await _surgewaveProducer.ProduceAsync(topic, key, value);
        }
        else
        {
            await _kafkaProducer.ProduceAsync(topic, key, value);
        }
    }

    public void MarkAsMigrated(string topic) => _migratedTopics.Add(topic);

    public async ValueTask DisposeAsync()
    {
        await _surgewaveProducer.DisposeAsync();
        await _kafkaProducer.DisposeAsync();
    }
}

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 5.3 - Hybrid Deployments ===");
        Console.WriteLine();

        // Set up hybrid environment
        Console.WriteLine("--- Setting Up Hybrid Environment ---");

        await using var surgewave = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        await using var kafka = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9093)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine($"Surgewave broker: localhost:9092");
        Console.WriteLine($"Kafka (simulated): localhost:9093");
        Console.WriteLine();

        // Demo 1: Dual-Write Pattern
        Console.WriteLine("--- Demo 1: Dual-Write Pattern ---");
        await using var dualWrite = new DualWriteProducer(
            "localhost:9092", "Surgewave",
            "localhost:9093", "Kafka");

        for (int i = 0; i < 5; i++)
        {
            await dualWrite.ProduceAsync("orders", $"order-{i}", $"Order data {i}");
            Console.WriteLine($"  Wrote order-{i} to BOTH systems");
        }
        Console.WriteLine();

        // Verify both systems have the data
        await VerifyMessages("localhost:9092", "Surgewave", "orders", 5);
        await VerifyMessages("localhost:9093", "Kafka", "orders", 5);
        Console.WriteLine();

        // Demo 2: Topic Router Pattern
        Console.WriteLine("--- Demo 2: Topic Router Pattern ---");
        await using var router = new TopicRouter(
            "localhost:9092",
            "localhost:9093",
            migratedTopics: new[] { "events", "logs" });

        // These go to Surgewave (migrated)
        await router.ProduceAsync("events", "evt-1", "Event data");
        Console.WriteLine("  events → Surgewave (migrated)");

        await router.ProduceAsync("logs", "log-1", "Log data");
        Console.WriteLine("  logs → Surgewave (migrated)");

        // These go to Kafka (not yet migrated)
        await router.ProduceAsync("users", "usr-1", "User data");
        Console.WriteLine("  users → Kafka (not migrated)");

        await router.ProduceAsync("payments", "pay-1", "Payment data");
        Console.WriteLine("  payments → Kafka (not migrated)");

        Console.WriteLine();

        // Demo 3: Data Mirroring
        Console.WriteLine("--- Demo 3: Data Mirroring ---");

        // Produce data to "Kafka"
        await using var kafkaProducer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = "localhost:9093");

        for (int i = 0; i < 5; i++)
        {
            await kafkaProducer.ProduceAsync("legacy-data", $"key-{i}", $"Legacy value {i}");
        }
        Console.WriteLine("  Produced 5 messages to Kafka 'legacy-data' topic");

        // Mirror to Surgewave
        await using var kafkaConsumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = "localhost:9093";
            opt.GroupId = "mirror";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        await using var surgewaveProducer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = "localhost:9092");

        kafkaConsumer.Subscribe("legacy-data");

        int mirrored = 0;
        while (mirrored < 5)
        {
            var result = await kafkaConsumer.ConsumeAsync(TimeSpan.FromSeconds(1));
            if (result == null) break;

            await surgewaveProducer.ProduceAsync("legacy-data", result.Key, result.Value);
            mirrored++;
        }
        Console.WriteLine($"  Mirrored {mirrored} messages from Kafka to Surgewave");

        await VerifyMessages("localhost:9092", "Surgewave", "legacy-data", 5);
        Console.WriteLine();

        // Migration strategy summary
        Console.WriteLine("--- Migration Strategy ---");
        Console.WriteLine();
        Console.WriteLine("Phase 1: Mirror & Shadow");
        Console.WriteLine("  - Set up Surgewave cluster");
        Console.WriteLine("  - Mirror existing data");
        Console.WriteLine("  - Dual-write new data");
        Console.WriteLine();
        Console.WriteLine("Phase 2: Gradual Cutover");
        Console.WriteLine("  - Migrate consumers topic-by-topic");
        Console.WriteLine("  - Verify data consistency");
        Console.WriteLine("  - Update routing rules");
        Console.WriteLine();
        Console.WriteLine("Phase 3: Decommission");
        Console.WriteLine("  - Stop dual-writes");
        Console.WriteLine("  - Shut down Kafka");
        Console.WriteLine("  - Full Surgewave operation");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }

    private static async Task VerifyMessages(string bootstrap, string name, string topic, int expected)
    {
        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = bootstrap;
            opt.GroupId = $"verify-{Guid.NewGuid()}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        consumer.Subscribe(topic);

        int count = 0;
        while (count < expected)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
            if (result == null) break;
            count++;
        }

        Console.WriteLine($"  {name} '{topic}': {count} messages");
    }
}
