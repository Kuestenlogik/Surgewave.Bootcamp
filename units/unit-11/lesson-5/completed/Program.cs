// Surgewave Bootcamp - Unit 5, Lesson 5: Hybrid Deployment Patterns
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

// Cluster Link — continuously mirrors a topic from source to target
public class ClusterLinkMirror : IAsyncDisposable
{
    private readonly SurgewaveConsumer<string, string> _source;
    private readonly SurgewaveProducer<string, string> _target;
    private readonly CancellationTokenSource _cts = new();
    private Task? _mirrorTask;
    private int _mirrored;

    public int Mirrored => _mirrored;

    public ClusterLinkMirror(string sourceBootstrap, string targetBootstrap)
    {
        _source = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = sourceBootstrap;
            opt.GroupId = "cluster-link";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });
        _target = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = targetBootstrap);
    }

    public void Start(string topic)
    {
        _source.Subscribe(topic);
        _mirrorTask = Task.Run(MirrorLoopAsync);
    }

    private async Task MirrorLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            var result = await _source.ConsumeAsync(TimeSpan.FromMilliseconds(100));
            if (result != null)
            {
                await _target.ProduceAsync(result.Topic, result.Key, result.Value);
                Interlocked.Increment(ref _mirrored);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        if (_mirrorTask != null) await _mirrorTask.ConfigureAwait(false);
        await _source.DisposeAsync();
        await _target.DisposeAsync();
    }
}

// Dual-Read Consumer — reads from Surgewave (primary) with Kafka as fallback
public class DualReadConsumer : IAsyncDisposable
{
    private readonly SurgewaveConsumer<string, string> _surgewaveConsumer;
    private readonly SurgewaveConsumer<string, string> _kafkaConsumer;
    private bool _surgewavePrimary;

    public DualReadConsumer(string surgewaveBootstrap, string kafkaBootstrap, string groupId)
    {
        _surgewaveConsumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = surgewaveBootstrap;
            opt.GroupId = groupId;
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });
        _kafkaConsumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = kafkaBootstrap;
            opt.GroupId = groupId;
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });
    }

    public void Subscribe(string topic)
    {
        _surgewaveConsumer.Subscribe(topic);
        _kafkaConsumer.Subscribe(topic);
    }

    public async Task<(string Source, ConsumeResult<string, string>? Result)> ConsumeAsync(TimeSpan timeout)
    {
        if (_surgewavePrimary)
        {
            var fastTimeout = TimeSpan.FromMilliseconds(200);
            var r = await _surgewaveConsumer.ConsumeAsync(fastTimeout);
            if (r != null) return ("Surgewave", r);

            var remaining = timeout - fastTimeout;
            if (remaining > TimeSpan.Zero)
            {
                var fb = await _kafkaConsumer.ConsumeAsync(remaining);
                return ("Kafka-fallback", fb);
            }
            return ("Surgewave", null);
        }

        return ("Kafka", await _kafkaConsumer.ConsumeAsync(timeout));
    }

    public void PromoteSurgewave()
    {
        _surgewavePrimary = true;
        Console.WriteLine("  [Migration] Surgewave promoted to PRIMARY reader");
    }

    public void DemoteSurgewave()
    {
        _surgewavePrimary = false;
        Console.WriteLine("  [Rollback]  Kafka restored as PRIMARY reader");
    }

    public async ValueTask DisposeAsync()
    {
        await _surgewaveConsumer.DisposeAsync();
        await _kafkaConsumer.DisposeAsync();
    }
}

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 5.5 - Hybrid Deployment Patterns ===");
        Console.WriteLine();

        // --- Setup ---
        Console.WriteLine("--- Setting Up Hybrid Environment ---");

        await using var surgewave = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        await using var kafka = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9093)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine("Surgewave:        localhost:9092");
        Console.WriteLine("Kafka (sim):  localhost:9093");
        Console.WriteLine();

        // --- Phase 1: Seed historical data into Kafka ---
        Console.WriteLine("--- Phase 1: Seeding 20 Orders into Kafka ---");

        await using var kafkaProducer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = "localhost:9093");

        for (int i = 1; i <= 20; i++)
            await kafkaProducer.ProduceAsync("orders", $"order-{i:D3}",
                $"""{{ "id": {i}, "total": {i * 9.99:F2} }}""");

        Console.WriteLine("  20 orders seeded into Kafka");
        Console.WriteLine();

        // --- Phase 2: Start Cluster Link ---
        Console.WriteLine("--- Phase 2: Starting Cluster Link (Kafka → Surgewave) ---");

        await using var link = new ClusterLinkMirror("localhost:9093", "localhost:9092");
        link.Start("orders");

        // Allow initial sync
        await Task.Delay(600);
        Console.WriteLine($"  Cluster Link synced {link.Mirrored} messages so far");
        Console.WriteLine();

        // --- Phase 3: Dual-Read with Kafka primary ---
        Console.WriteLine("--- Phase 3: Dual-Read Consumer (Kafka is primary) ---");

        await using var dualReader = new DualReadConsumer(
            "localhost:9092", "localhost:9093", "order-processor");
        dualReader.Subscribe("orders");

        for (int i = 0; i < 5; i++)
        {
            var (source, msg) = await dualReader.ConsumeAsync(TimeSpan.FromSeconds(1));
            Console.WriteLine($"  [{source,-14}] {msg?.Key ?? "(null)"}");
        }
        Console.WriteLine();

        // --- Phase 4: Promote Surgewave ---
        Console.WriteLine("--- Phase 4: Promoting Surgewave to Primary ---");
        dualReader.PromoteSurgewave();

        for (int i = 0; i < 5; i++)
        {
            var (source, msg) = await dualReader.ConsumeAsync(TimeSpan.FromSeconds(1));
            Console.WriteLine($"  [{source,-14}] {msg?.Key ?? "(null)"}");
        }
        Console.WriteLine();

        // --- Phase 5: Rollback ---
        Console.WriteLine("--- Phase 5: Simulating Error → Rollback ---");
        dualReader.DemoteSurgewave();

        var (rollbackSource, rollbackMsg) = await dualReader.ConsumeAsync(TimeSpan.FromSeconds(1));
        Console.WriteLine($"  [{rollbackSource,-14}] {rollbackMsg?.Key ?? "(null)"}");
        Console.WriteLine();

        // --- Phase 6: Validate Counts ---
        Console.WriteLine("--- Phase 6: Validating Message Counts ---");

        var kafkaCount = await CountMessagesAsync("localhost:9093", "orders");
        var surgewaveCount  = await CountMessagesAsync("localhost:9092", "orders");

        Console.WriteLine($"  Kafka: {kafkaCount} messages");
        Console.WriteLine($"  Surgewave: {surgewaveCount} messages");
        Console.WriteLine(kafkaCount == surgewaveCount
            ? "  Counts match — safe to proceed with cutover"
            : "  Mismatch — investigate before cutting over");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }

    private static async Task<int> CountMessagesAsync(string bootstrap, string topic)
    {
        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = bootstrap;
            opt.GroupId = $"count-{Guid.NewGuid()}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });
        consumer.Subscribe(topic);

        int count = 0;
        while (true)
        {
            var r = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(300));
            if (r == null) break;
            count++;
        }
        return count;
    }
}
