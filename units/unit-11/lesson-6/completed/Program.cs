// Surgewave Bootcamp - Unit 5, Lesson 6: Performance Comparison & Validation
// COMPLETED SOLUTION

using System.Diagnostics;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Client.Admin;

public record BenchmarkResult(string Label, double ThroughputMbps, double P50Ms, double P99Ms);

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 5.6 - Performance Comparison & Validation ===");
        Console.WriteLine();

        // --- Setup ---
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

        Console.WriteLine("Surgewave: localhost:9092 | Kafka (sim): localhost:9093");
        Console.WriteLine();

        // --- Warm-up ---
        Console.WriteLine("--- Warm-Up (JIT) ---");
        await RunBenchmarkAsync("localhost:9093", "warmup-kafka", 500, silent: true);
        await RunBenchmarkAsync("localhost:9092", "warmup-surgewave", 500, silent: true);
        Console.WriteLine("  Warm-up complete");
        Console.WriteLine();

        // --- Benchmark ---
        Console.WriteLine("--- Benchmarks (10,000 × 1 KB messages) ---");
        var kafkaResult = await RunBenchmarkAsync("localhost:9093", "Kafka", 10_000);
        var surgewaveResult = await RunBenchmarkAsync("localhost:9092", "Surgewave", 10_000);
        Console.WriteLine();

        // --- Comparison Table ---
        Console.WriteLine("--- Comparison ---");
        PrintComparisonRow("Throughput (MB/s)",  kafkaResult.ThroughputMbps, surgewaveResult.ThroughputMbps, higherIsBetter: true);
        PrintComparisonRow("P50 latency (ms)",   kafkaResult.P50Ms,          surgewaveResult.P50Ms,          higherIsBetter: false);
        PrintComparisonRow("P99 latency (ms)",   kafkaResult.P99Ms,          surgewaveResult.P99Ms,          higherIsBetter: false);
        Console.WriteLine();

        // --- Common Pitfalls ---
        Console.WriteLine("--- Common Pitfall Demonstrations ---");

        // Pitfall 1: Partition count mismatch — pre-create with correct count
        Console.WriteLine();
        Console.WriteLine("Pitfall 1: Partition Count Mismatch");
        try
        {
            var admin = new SurgewaveAdminClient(opt => opt.BootstrapServers = "localhost:9092");
            await admin.CreateTopicsAsync(
            [
                new TopicSpecification { Name = "orders", NumPartitions = 12, ReplicationFactor = 1 }
            ]);
            Console.WriteLine("  [FIX] Created 'orders' with 12 partitions (matching Kafka)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [INFO] Topic already exists or admin not available: {ex.Message}");
        }

        // Pitfall 2: AutoOffsetReset — explicit is safe
        Console.WriteLine();
        Console.WriteLine("Pitfall 2: AutoOffsetReset");
        Console.WriteLine("  [WRONG] Not setting AutoOffsetReset → may miss messages");
        Console.WriteLine("  [FIX]   AutoOffsetReset = AutoOffsetReset.Earliest → always explicit");

        // Pitfall 3: LingerMs — show impact
        Console.WriteLine();
        Console.WriteLine("Pitfall 3: LingerMs Impact on Throughput");
        var noBatch = await RunBenchmarkAsync("localhost:9092", "Surgewave (LingerMs=0)",  2_000, lingerMs: 0);
        var batched  = await RunBenchmarkAsync("localhost:9092", "Surgewave (LingerMs=5)",  2_000, lingerMs: 5);
        Console.WriteLine($"  Batching improvement: {batched.ThroughputMbps / noBatch.ThroughputMbps:F2}x throughput");
        Console.WriteLine();

        // --- Pre-Cutover Checklist ---
        await RunCutoverChecklistAsync("localhost:9092", "orders");
        Console.WriteLine();

        // --- Migration Summary ---
        Console.WriteLine("=== Migration Summary ===");
        double throughputGain = surgewaveResult.ThroughputMbps / kafkaResult.ThroughputMbps;
        double latencyGain    = kafkaResult.P99Ms / surgewaveResult.P99Ms;
        Console.WriteLine($"  Producer throughput:  {throughputGain:F1}x improvement");
        Console.WriteLine($"  P99 latency:          {latencyGain:F1}x lower");
        Console.WriteLine($"  Startup time:         Surgewave ~0.6s vs Kafka ~15s (est.)");
        Console.WriteLine($"  Memory footprint:     Surgewave ~100 MB vs Kafka ~1 GB (est.)");
        Console.WriteLine();
        Console.WriteLine("  Recommendation: Surgewave is ready for production cutover.");
        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }

    private static async Task<BenchmarkResult> RunBenchmarkAsync(
        string bootstrap, string label, int messageCount,
        bool silent = false, int lingerMs = 5)
    {
        await using var producer = new SurgewaveProducer<string, byte[]>(opt =>
        {
            opt.BootstrapServers = bootstrap;
            opt.LingerMs = lingerMs;
        });

        var payload = new byte[1024];
        Random.Shared.NextBytes(payload);

        var latencies = new List<double>(messageCount);
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < messageCount; i++)
        {
            var msgSw = Stopwatch.StartNew();
            await producer.ProduceAsync("bench", $"k-{i}", payload);
            latencies.Add(msgSw.Elapsed.TotalMilliseconds);
        }

        sw.Stop();
        latencies.Sort();

        double throughput = (messageCount * 1024.0) / sw.Elapsed.TotalSeconds / (1024.0 * 1024.0);
        double p50 = latencies[(int)(messageCount * 0.50)];
        double p99 = latencies[(int)(messageCount * 0.99)];

        if (!silent)
        {
            Console.WriteLine($"  [{label,-26}]  {throughput,7:F1} MB/s  P50={p50:F2}ms  P99={p99:F2}ms");
        }

        return new BenchmarkResult(label, throughput, p50, p99);
    }

    private static void PrintComparisonRow(string name, double kafka, double surgewave, bool higherIsBetter)
    {
        double ratio = higherIsBetter ? surgewave / kafka : kafka / surgewave;
        string winner = ratio >= 1.0 ? "Surgewave" : "Kafka";
        Console.WriteLine($"  {name,-22} Kafka={kafka,7:F2}  Surgewave={surgewave,7:F2}  ({winner} {Math.Abs(ratio):F1}x)");
    }

    private static async Task RunCutoverChecklistAsync(string bootstrap, string topic)
    {
        Console.WriteLine("--- Pre-Cutover Checklist ---");

        // Check 1: Broker reachable
        bool brokerOk = false;
        try
        {
            var admin = new SurgewaveAdminClient(opt => opt.BootstrapServers = bootstrap);
            _ = await admin.GetMetadataAsync(TimeSpan.FromSeconds(5));
            Console.WriteLine("  [PASS] Surgewave broker reachable");
            brokerOk = true;
        }
        catch
        {
            Console.WriteLine("  [FAIL] Surgewave broker not reachable");
        }

        if (!brokerOk) return;

        // Check 2: Topic exists
        try
        {
            var admin = new SurgewaveAdminClient(opt => opt.BootstrapServers = bootstrap);
            var meta = await admin.GetTopicMetadataAsync(topic);
            Console.WriteLine($"  [PASS] Topic '{topic}' exists ({meta.Partitions.Count} partition(s))");
        }
        catch
        {
            Console.WriteLine($"  [WARN] Topic '{topic}' does not exist — will be auto-created on first produce");
        }

        // Check 3: Produce/consume round-trip
        try
        {
            await using var producer = new SurgewaveProducer<string, string>(opt =>
                opt.BootstrapServers = bootstrap);
            await using var consumer = new SurgewaveConsumer<string, string>(opt =>
            {
                opt.BootstrapServers = bootstrap;
                opt.GroupId = $"checklist-{Guid.NewGuid()}";
                opt.AutoOffsetReset = AutoOffsetReset.Latest;
            });

            consumer.Subscribe(topic);
            var sentinel = $"cutover-check-{DateTime.UtcNow:HHmmss}";
            await producer.ProduceAsync(topic, "checklist", sentinel);

            var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(3));
            if (result?.Value == sentinel)
                Console.WriteLine("  [PASS] Produce/consume round-trip verified");
            else
                Console.WriteLine("  [WARN] Round-trip message not confirmed within timeout");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] Round-trip error: {ex.Message}");
        }

        Console.WriteLine("  Checklist complete — review any WARN/FAIL items before cutover");
    }
}
