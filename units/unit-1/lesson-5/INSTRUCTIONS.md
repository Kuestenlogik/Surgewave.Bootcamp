# Lesson 1.5: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Understand Batching Trade-offs

Batching configuration involves trade-offs:

| Setting | Higher Value | Lower Value |
|---------|--------------|-------------|
| BatchSize | Better throughput, more memory | Lower latency, less memory |
| LingerMs | Better batching, higher latency | Lower latency, smaller batches |
| FetchMaxBytes | Larger fetches, more memory | Smaller fetches, lower memory |

---

## Step 2: Add Using Statements

```csharp
using System.Diagnostics;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
```

Note: We add `System.Diagnostics` for `Stopwatch` to measure performance.

---

## Step 3: Create the Baseline Producer (No Batching)

First, measure performance without batching optimization:

```csharp
// Step 2 - Test 1: No batching (baseline)
Console.WriteLine("--- Test 1: No Batching (Baseline) ---");
Console.WriteLine("Producing 1000 messages without batching...");

await using var producerNoBatch = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.BatchSize = 1;      // Minimal batching
    options.LingerMs = 0;       // No waiting
});

var sw = Stopwatch.StartNew();
const int messageCount = 1000;

for (int i = 0; i < messageCount; i++)
{
    await producerNoBatch.ProduceAsync("perf-test", $"key-{i}", $"message-{i}");
}

sw.Stop();
var noBatchTime = sw.ElapsedMilliseconds;
var noBatchThroughput = messageCount * 1000.0 / Math.Max(1, noBatchTime);

Console.WriteLine($"  Time: {noBatchTime}ms");
Console.WriteLine($"  Throughput: {noBatchThroughput:N0} msg/sec");
Console.WriteLine();
```

**What this does:**
- Sets `BatchSize = 1` to disable batching
- Sets `LingerMs = 0` to send immediately
- Measures time to produce 1000 messages

---

## Step 4: Create the Batching Producer

Now compare with optimized batching:

```csharp
// Step 3 - Test 2: With batching
Console.WriteLine("--- Test 2: With Batching ---");
Console.WriteLine("Producing 1000 messages with batching...");

await using var producerBatched = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.BatchSize = 100;    // Batch up to 100 messages
    options.LingerMs = 5;       // Wait up to 5ms for batch to fill
});

sw.Restart();

for (int i = 0; i < messageCount; i++)
{
    await producerBatched.ProduceAsync("perf-test-batched", $"key-{i}", $"message-{i}");
}

// Flush to ensure all messages are sent
await producerBatched.FlushAsync();

sw.Stop();
var batchTime = sw.ElapsedMilliseconds;
var batchThroughput = messageCount * 1000.0 / Math.Max(1, batchTime);
var improvement = noBatchTime / (double)Math.Max(1, batchTime);

Console.WriteLine($"  Time: {batchTime}ms");
Console.WriteLine($"  Throughput: {batchThroughput:N0} msg/sec");
Console.WriteLine($"  Improvement: {improvement:F1}x faster!");
Console.WriteLine();
```

**Key differences:**
- `BatchSize = 100` batches up to 100 messages together
- `LingerMs = 5` waits up to 5ms for more messages
- `FlushAsync()` ensures all batched messages are sent

---

## Step 5: Test Message Size Impact

Different message sizes have different throughput characteristics:

```csharp
// Step 4 - Test 3: Message size impact
Console.WriteLine("--- Test 3: Message Size Impact ---");

await using var producerSize = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.BatchSize = 100;    // Batch up to 100 messages
    options.LingerMs = 5;
});

// Small messages (100 bytes)
var smallPayload = new string('x', 100);
sw.Restart();
for (int i = 0; i < 100; i++)
{
    await producerSize.ProduceAsync("size-test", $"key-{i}", smallPayload);
}
await producerSize.FlushAsync();
sw.Stop();
Console.WriteLine($"Producing 100 small messages (100 bytes)...");
Console.WriteLine($"  Throughput: {100 * 1000.0 / Math.Max(1, sw.ElapsedMilliseconds):N0} msg/sec");

// Medium messages (10KB)
var mediumPayload = new string('x', 10240);
sw.Restart();
for (int i = 0; i < 100; i++)
{
    await producerSize.ProduceAsync("size-test", $"key-{i}", mediumPayload);
}
await producerSize.FlushAsync();
sw.Stop();
Console.WriteLine($"Producing 100 medium messages (10KB)...");
Console.WriteLine($"  Throughput: {100 * 1000.0 / Math.Max(1, sw.ElapsedMilliseconds):N0} msg/sec");

// Large messages (100KB)
var largePayload = new string('x', 102400);
sw.Restart();
for (int i = 0; i < 100; i++)
{
    await producerSize.ProduceAsync("size-test", $"key-{i}", largePayload);
}
await producerSize.FlushAsync();
sw.Stop();
Console.WriteLine($"Producing 100 large messages (100KB)...");
Console.WriteLine($"  Throughput: {100 * 1000.0 / Math.Max(1, sw.ElapsedMilliseconds):N0} msg/sec");

Console.WriteLine();
```

**Observe:**
- Small messages have higher msg/sec but more overhead
- Large messages have lower msg/sec but higher bytes/sec
- Medium messages often provide the best balance

---

## Step 6: Test Flush Strategies

Compare explicit flush vs letting batches auto-flush:

```csharp
// Step 5 - Test 4: Flush strategies
Console.WriteLine("--- Test 4: Flush Strategies ---");

await using var producerFlush = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.BatchSize = 100;    // Batch up to 100 messages
    options.LingerMs = 10;
});

// Explicit flush after batch
sw.Restart();
for (int i = 0; i < 100; i++)
{
    await producerFlush.ProduceAsync("flush-test", $"key-{i}", $"message-{i}");
}
await producerFlush.FlushAsync();
sw.Stop();
Console.WriteLine($"Batch with explicit flush: {sw.ElapsedMilliseconds}ms");

// No explicit flush (relies on linger timeout)
sw.Restart();
for (int i = 0; i < 100; i++)
{
    await producerFlush.ProduceAsync("flush-test", $"key-{i}", $"message-{i}");
}
// Wait for linger timeout
await Task.Delay(15);
sw.Stop();
Console.WriteLine($"Batch with auto-flush: {sw.ElapsedMilliseconds}ms");

Console.WriteLine();
```

**When to use each:**
- **Explicit flush**: At end of request, transaction boundaries
- **Auto-flush**: Continuous high-volume streaming

---

## Step 7: Test Consumer Batch Fetching

Configure consumer for efficient batch fetching:

```csharp
// Step 6 - Test 5: Consumer batch fetching
Console.WriteLine("--- Test 5: Consumer Batch Fetching ---");
Console.WriteLine("Consuming 1000 messages...");

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "batch-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
    options.FetchMaxBytes = 1024 * 1024; // Fetch up to 1MB per request
});

consumer.Subscribe("perf-test-batched");

int consumed = 0;
sw.Restart();

while (consumed < messageCount)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
    if (result != null)
    {
        consumed++;
    }
}

sw.Stop();
var consumeThroughput = consumed * 1000.0 / Math.Max(1, sw.ElapsedMilliseconds);

Console.WriteLine($"  Total time: {sw.ElapsedMilliseconds}ms");
Console.WriteLine($"  Throughput: {consumeThroughput:N0} msg/sec");
Console.WriteLine();
```

**Consumer batching settings:**
- `FetchMaxBytes`: Maximum bytes to fetch per request (default: 1MB)

---

## Complete Solution

```csharp
// Surgewave Bootcamp - Unit 1, Lesson 5: Batching & Message Sizing
// COMPLETED SOLUTION

using System.Diagnostics;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.5 - Batching & Message Sizing ===");
Console.WriteLine();

// Step 1 - Start an embedded Surgewave broker
Console.WriteLine("Starting embedded Surgewave broker...");

await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on localhost:{runtime.Port}");
Console.WriteLine();

// Step 2 - Test 1: No batching (baseline)
Console.WriteLine("--- Test 1: No Batching (Baseline) ---");
Console.WriteLine("Producing 1000 messages without batching...");

await using var producerNoBatch = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.BatchSize = 1;
    options.LingerMs = 0;
});

var sw = Stopwatch.StartNew();
const int messageCount = 1000;

for (int i = 0; i < messageCount; i++)
{
    await producerNoBatch.ProduceAsync("perf-test", $"key-{i}", $"message-{i}");
}

sw.Stop();
var noBatchTime = sw.ElapsedMilliseconds;
var noBatchThroughput = messageCount * 1000.0 / Math.Max(1, noBatchTime);

Console.WriteLine($"  Time: {noBatchTime}ms");
Console.WriteLine($"  Throughput: {noBatchThroughput:N0} msg/sec");
Console.WriteLine();

// Step 3 - Test 2: With batching
Console.WriteLine("--- Test 2: With Batching ---");
Console.WriteLine("Producing 1000 messages with batching...");

await using var producerBatched = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.BatchSize = 100;    // Batch up to 100 messages
    options.LingerMs = 5;
});

sw.Restart();

for (int i = 0; i < messageCount; i++)
{
    await producerBatched.ProduceAsync("perf-test-batched", $"key-{i}", $"message-{i}");
}

await producerBatched.FlushAsync();

sw.Stop();
var batchTime = sw.ElapsedMilliseconds;
var batchThroughput = messageCount * 1000.0 / Math.Max(1, batchTime);
var improvement = noBatchTime / (double)Math.Max(1, batchTime);

Console.WriteLine($"  Time: {batchTime}ms");
Console.WriteLine($"  Throughput: {batchThroughput:N0} msg/sec");
Console.WriteLine($"  Improvement: {improvement:F1}x faster!");
Console.WriteLine();

// Step 4 - Test 3: Message size impact
Console.WriteLine("--- Test 3: Message Size Impact ---");

await using var producerSize = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.BatchSize = 100;    // Batch up to 100 messages
    options.LingerMs = 5;
});

// Small messages (100 bytes)
var smallPayload = new string('x', 100);
sw.Restart();
for (int i = 0; i < 100; i++)
{
    await producerSize.ProduceAsync("size-test", $"key-{i}", smallPayload);
}
await producerSize.FlushAsync();
sw.Stop();
Console.WriteLine($"Producing 100 small messages (100 bytes)...");
Console.WriteLine($"  Throughput: {100 * 1000.0 / Math.Max(1, sw.ElapsedMilliseconds):N0} msg/sec");

// Medium messages (10KB)
var mediumPayload = new string('x', 10240);
sw.Restart();
for (int i = 0; i < 100; i++)
{
    await producerSize.ProduceAsync("size-test", $"key-{i}", mediumPayload);
}
await producerSize.FlushAsync();
sw.Stop();
Console.WriteLine($"Producing 100 medium messages (10KB)...");
Console.WriteLine($"  Throughput: {100 * 1000.0 / Math.Max(1, sw.ElapsedMilliseconds):N0} msg/sec");

// Large messages (100KB)
var largePayload = new string('x', 102400);
sw.Restart();
for (int i = 0; i < 100; i++)
{
    await producerSize.ProduceAsync("size-test", $"key-{i}", largePayload);
}
await producerSize.FlushAsync();
sw.Stop();
Console.WriteLine($"Producing 100 large messages (100KB)...");
Console.WriteLine($"  Throughput: {100 * 1000.0 / Math.Max(1, sw.ElapsedMilliseconds):N0} msg/sec");

Console.WriteLine();

// Step 5 - Test 4: Flush strategies
Console.WriteLine("--- Test 4: Flush Strategies ---");

await using var producerFlush = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.BatchSize = 100;    // Batch up to 100 messages
    options.LingerMs = 10;
});

// Explicit flush after batch
sw.Restart();
for (int i = 0; i < 100; i++)
{
    await producerFlush.ProduceAsync("flush-test", $"key-{i}", $"message-{i}");
}
await producerFlush.FlushAsync();
sw.Stop();
Console.WriteLine($"Batch with explicit flush: {sw.ElapsedMilliseconds}ms");

// No explicit flush (relies on linger timeout)
sw.Restart();
for (int i = 0; i < 100; i++)
{
    await producerFlush.ProduceAsync("flush-test", $"key-{i}", $"message-{i}");
}
await Task.Delay(15);
sw.Stop();
Console.WriteLine($"Batch with auto-flush: {sw.ElapsedMilliseconds}ms");

Console.WriteLine();

// Step 6 - Test 5: Consumer batch fetching
Console.WriteLine("--- Test 5: Consumer Batch Fetching ---");
Console.WriteLine("Consuming 1000 messages...");

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "batch-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
    options.FetchMaxBytes = 1024 * 1024; // Fetch up to 1MB per request
});

consumer.Subscribe("perf-test-batched");

int consumed = 0;
sw.Restart();

while (consumed < messageCount)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
    if (result != null)
    {
        consumed++;
    }
}

sw.Stop();
var consumeThroughput = consumed * 1000.0 / Math.Max(1, sw.ElapsedMilliseconds);

Console.WriteLine($"  Total time: {sw.ElapsedMilliseconds}ms");
Console.WriteLine($"  Throughput: {consumeThroughput:N0} msg/sec");
Console.WriteLine();

// Summary
Console.WriteLine("Summary:");
Console.WriteLine($"  - Batching improved throughput by ~{improvement:F0}x");
Console.WriteLine("  - Optimal message size: 1KB-100KB");
Console.WriteLine("  - Use explicit flush for time-critical batches");
Console.WriteLine();

// Step 7 - Clean shutdown
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");
```

---

## Run and Verify

```bash
cd start
dotnet run
```

---

## Experiment (Optional)

### 1. Try Different Batch Sizes

```csharp
// Compare different batch sizes (number of messages)
foreach (var batchSize in new[] { 1, 10, 50, 100, 200 })
{
    // Measure throughput with this batch size
}
```

### 2. Measure Latency vs Throughput

```csharp
// Low latency config
options.BatchSize = 1;
options.LingerMs = 0;

// High throughput config
options.BatchSize = 100;
options.LingerMs = 50;
```

### 4. Test with Real Network

When running against a remote broker, batching benefits are even more pronounced due to network latency.

---

## Knowledge Check

Before moving on, make sure you can answer:

1. Why does batching improve throughput?
2. What is the trade-off with higher `LingerMs` values?
3. When should you call `FlushAsync()` explicitly?
4. What message size range provides the best throughput?
5. How does `FetchMaxBytes` affect consumer behavior?

---

**Ready for more?** -> [Lesson 1.6: Partitioning](../lesson-6/README.md)
