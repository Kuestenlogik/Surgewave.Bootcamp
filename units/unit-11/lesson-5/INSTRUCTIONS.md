# Lesson 5.5: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Start the Hybrid Environment

Simulate two brokers — one representing Kafka (port 9093) and one Surgewave (port 9092):

```csharp
// Surgewave broker (migration target)
await using var surgewave = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

// "Kafka" broker (existing system, simulated as a second Surgewave instance)
await using var kafka = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9093)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine("Surgewave: localhost:9092");
Console.WriteLine("Kafka (simulated): localhost:9093");
```

## Step 2: Seed Data in "Kafka"

Produce messages to the legacy system to represent existing historical data:

```csharp
await using var kafkaProducer = new SurgewaveProducer<string, string>(opt =>
    opt.BootstrapServers = "localhost:9093");

for (int i = 1; i <= 20; i++)
{
    await kafkaProducer.ProduceAsync("orders", $"order-{i}",
        $"""{{ "id": {i}, "total": {i * 9.99:F2} }}""");
}
Console.WriteLine("Seeded 20 orders into Kafka");
```

## Step 3: Implement Cluster Link Mirror

Mirror messages from the Kafka broker into Surgewave continuously:

```csharp
public class ClusterLinkMirror : IAsyncDisposable
{
    private readonly SurgewaveConsumer<string, string> _source;
    private readonly SurgewaveProducer<string, string> _target;
    private readonly CancellationTokenSource _cts = new();
    private Task? _mirrorTask;

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
                await _target.ProduceAsync(result.Topic, result.Key, result.Value);
        }
    }

    public async Task WaitForSyncAsync(int expectedMessages, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            // Poll until Surgewave has the expected message count
            await Task.Delay(200);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        if (_mirrorTask != null) await _mirrorTask;
        await _source.DisposeAsync();
        await _target.DisposeAsync();
    }
}
```

Usage:

```csharp
await using var link = new ClusterLinkMirror("localhost:9093", "localhost:9092");
link.Start("orders");
Console.WriteLine("Cluster Link started — mirroring orders topic");

// Wait for initial sync
await Task.Delay(500);
Console.WriteLine("Initial sync complete");
```

## Step 4: Implement Dual-Read Consumer

Create a consumer that reads from Surgewave and falls back to Kafka:

```csharp
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

    public async Task<(string Source, ConsumeResult<string, string>? Result)> ConsumeAsync(TimeSpan timeout)
    {
        if (_surgewavePrimary)
        {
            var r = await _surgewaveConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(200));
            if (r != null) return ("Surgewave", r);
            // Fall back to Kafka
            var fb = await _kafkaConsumer.ConsumeAsync(timeout - TimeSpan.FromMilliseconds(200));
            return ("Kafka-fallback", fb);
        }

        return ("Kafka", await _kafkaConsumer.ConsumeAsync(timeout));
    }

    public void PromoteSurgewave()
    {
        _surgewavePrimary = true;
        Console.WriteLine("Surgewave promoted to primary reader");
    }

    public void DemoteSurgewave()
    {
        _surgewavePrimary = false;
        Console.WriteLine("Rolled back: Kafka restored as primary reader");
    }

    public async ValueTask DisposeAsync()
    {
        await _surgewaveConsumer.DisposeAsync();
        await _kafkaConsumer.DisposeAsync();
    }
}
```

## Step 5: Demonstrate Migration and Rollback

```csharp
await using var dualReader = new DualReadConsumer(
    "localhost:9092", "localhost:9093", "order-processor");

dualReader._surgewaveConsumer.Subscribe("orders");
dualReader._kafkaConsumer.Subscribe("orders");

Console.WriteLine("--- Phase 1: Kafka is primary ---");
for (int i = 0; i < 5; i++)
{
    var (source, msg) = await dualReader.ConsumeAsync(TimeSpan.FromSeconds(1));
    Console.WriteLine($"  Read from {source}: {msg?.Key}");
}

Console.WriteLine();
Console.WriteLine("--- Phase 2: Promote Surgewave to primary ---");
dualReader.PromoteSurgewave();

for (int i = 0; i < 5; i++)
{
    var (source, msg) = await dualReader.ConsumeAsync(TimeSpan.FromSeconds(1));
    Console.WriteLine($"  Read from {source}: {msg?.Key}");
}

Console.WriteLine();
Console.WriteLine("--- Phase 3: Simulate issue, rollback to Kafka ---");
dualReader.DemoteSurgewave();

var (rollbackSource, rollbackMsg) = await dualReader.ConsumeAsync(TimeSpan.FromSeconds(1));
Console.WriteLine($"  After rollback, reading from: {rollbackSource}");
```

## Step 6: Validate Message Counts

Verify both systems hold the same data:

```csharp
async Task<int> CountMessagesAsync(string bootstrap, string topic)
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

var kafkaCount = await CountMessagesAsync("localhost:9093", "orders");
var surgewaveCount  = await CountMessagesAsync("localhost:9092", "orders");

Console.WriteLine($"Kafka: {kafkaCount} messages | Surgewave: {surgewaveCount} messages");
Console.WriteLine(kafkaCount == surgewaveCount ? "Counts match — safe to cut over" : "Mismatch — do not cut over yet");
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

---

**Next:** → [Lesson 5.6: Performance Comparison & Validation](../lesson-6/README.md)
