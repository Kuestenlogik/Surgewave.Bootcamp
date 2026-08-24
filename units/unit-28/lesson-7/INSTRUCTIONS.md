# Lesson 3.7: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Start Embedded Broker

Start an embedded Surgewave broker with memory storage:

```csharp
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(false)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
```

## Step 2: Create Ephemeral Topic

Use the native client to create an ephemeral topic with a small (1 MB) buffer:

```csharp
await using var nativeClient = new SurgewaveNativeClient("localhost", runtime.Port);
await nativeClient.ConnectAsync();

await nativeClient.Topics.Create("sensor-telemetry")
    .WithPartitions(1)
    .WithConfig("cleanup.policy", "ephemeral")
    .WithConfig("ephemeral.buffer.bytes", "1048576")
    .ExecuteAsync();
Console.WriteLine("Created ephemeral topic: sensor-telemetry (1 MB buffer)");
```

The key difference from persistent topics: we set `cleanup.policy` to `ephemeral` and configure the ring-buffer size with `ephemeral.buffer.bytes`.

## Step 3: Create Persistent Topic for Comparison

Create a standard persistent topic alongside the ephemeral one:

```csharp
await nativeClient.Topics.Create("sensor-telemetry-persistent")
    .WithPartitions(1)
    .ExecuteAsync();
Console.WriteLine("Created persistent topic: sensor-telemetry-persistent");
```

## Step 4: Start Consumer for Ephemeral Topic

Start a consumer in a background task **before** producing messages:

```csharp
var receivedMessages = new List<string>();
var cts = new CancellationTokenSource();

var consumerTask = Task.Run(async () =>
{
    await using var consumer = new SurgewaveConsumer<string, string>(opt =>
    {
        opt.BootstrapServers = $"localhost:{runtime.Port}";
        opt.GroupId = "ephemeral-demo";
        opt.AutoOffsetReset = AutoOffsetReset.Latest;
    });

    consumer.Subscribe("sensor-telemetry");

    while (!cts.Token.IsCancellationRequested)
    {
        var msg = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(200));
        if (msg != null)
        {
            receivedMessages.Add(msg.Value!);
            Console.WriteLine($"  [Consumer] Received: {msg.Value}");
        }
    }
});

await Task.Delay(500); // Allow consumer to connect
```

**Important:** The consumer uses `AutoOffsetReset.Latest` because ephemeral topics are designed for real-time delivery.

## Step 5: Produce Messages - Consumer Receives Live

Send sensor telemetry data while the consumer is active:

```csharp
await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

Console.WriteLine("Producing sensor readings...");
for (int i = 1; i <= 5; i++)
{
    var reading = $"{{\"sensor\": \"temp-1\", \"value\": {20.0 + i}, \"ts\": \"{DateTime.UtcNow:O}\"}}";
    await producer.ProduceAsync("sensor-telemetry", "temp-1", reading);
    await Task.Delay(100);
}
await producer.FlushAsync();
await Task.Delay(500); // Allow consumer to process
```

## Step 6: Demonstrate Messages Lost Without Active Consumer

Stop the consumer, produce more messages, then start a new consumer:

```csharp
// Stop the consumer
cts.Cancel();
await consumerTask;
int receivedBeforeStop = receivedMessages.Count;
Console.WriteLine($"Consumer received {receivedBeforeStop} messages while active");

// Produce messages with NO active consumer
Console.WriteLine("Producing messages with NO active consumer...");
for (int i = 1; i <= 5; i++)
{
    await producer.ProduceAsync("sensor-telemetry", "temp-1", $"lost-message-{i}");
}
await producer.FlushAsync();

// Start NEW consumer - these messages may not be available depending on offset
await using var lateConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.AutoOffsetReset = AutoOffsetReset.Latest;
});

lateConsumer.Subscribe("sensor-telemetry");
int lateCount = 0;
while (true)
{
    var msg = await lateConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(300));
    if (msg == null) break;
    lateCount++;
}
Console.WriteLine($"Late consumer received: {lateCount} messages (expected: 0 with Latest offset)");
```

With `AutoOffsetReset.Latest`, a new consumer starts at the current end of the log - messages produced before it connected are effectively lost.

## Step 7: Demonstrate Buffer Wrap-Around

Produce enough data to exceed the 1 MB buffer, forcing the ring buffer to evict old messages:

```csharp
Console.WriteLine("Flooding ephemeral topic to trigger buffer wrap-around...");
var largePayload = new string('X', 50_000); // ~50 KB per message

for (int i = 0; i < 50; i++) // ~2.5 MB total >> 1 MB buffer
{
    await producer.ProduceAsync("sensor-telemetry", $"sensor-{i % 10}", $"{largePayload}-{i}");
    if (i % 10 == 9) await producer.FlushAsync(); // Flush every 10 messages
}
await producer.FlushAsync();

// Read topic metadata to check LogStartOffset
var topicInfo = await nativeClient.Topics.DescribeAsync("sensor-telemetry");
foreach (var partition in topicInfo.Partitions)
{
    Console.WriteLine($"  Partition {partition.PartitionId}: LogStartOffset={partition.LogStartOffset}, HighWatermark={partition.HighWatermark}");
}

// LogStartOffset > 0 means old messages were evicted
Console.WriteLine("LogStartOffset > 0 confirms ring-buffer wrap-around (old data evicted)");
```

## Step 8: Compare Persistent vs Ephemeral

Produce the same messages to both topics and compare behavior:

```csharp
Console.WriteLine("Producing to both topics for comparison...");
for (int i = 1; i <= 10; i++)
{
    var value = $"reading-{i}";
    await producer.ProduceAsync("sensor-telemetry", "compare", value);
    await producer.ProduceAsync("sensor-telemetry-persistent", "compare", value);
}
await producer.FlushAsync();

// Read persistent topic from beginning - all messages available
await using var persistentConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
persistentConsumer.Subscribe("sensor-telemetry-persistent");

int persistentCount = 0;
while (true)
{
    var msg = await persistentConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(300));
    if (msg == null) break;
    persistentCount++;
}

Console.WriteLine($"Persistent topic: {persistentCount} messages (full history preserved)");
Console.WriteLine("Ephemeral topic: only recent messages in buffer window");
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. What happens to messages in an ephemeral topic when the buffer is full?
2. Why would you choose ephemeral over delete retention for sensor telemetry?
3. What is the default ring-buffer size for ephemeral topics?
4. Can a consumer replay the full history of an ephemeral topic?

---

**Next Unit:** -> [Unit 21: Production Operations](../../unit-21/README.md)
