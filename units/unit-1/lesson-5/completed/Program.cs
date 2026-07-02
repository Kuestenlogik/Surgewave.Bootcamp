// Surgewave Bootcamp - Unit 1, Lesson 5: Batching & Message Sizing
// COMPLETED SOLUTION

using System.Diagnostics;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
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
// Wait for linger timeout
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
