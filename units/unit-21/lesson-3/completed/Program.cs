// Surgewave Bootcamp - Unit 4, Lesson 3: Monitoring
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
// using Kuestenlogik.Surgewave.Client.Admin; // TODO: Admin client not yet implemented
using Kuestenlogik.Surgewave.Client.Consumer;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 4.3 - Monitoring ===");
        Console.WriteLine();

        // Start broker with metrics endpoint
        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            // .WithMetricsPort(9090) // TODO: Not yet implemented in Surgewave
            .Build()
            .StartAsync();

        Console.WriteLine($"Broker started on port {runtime.Port}");
        Console.WriteLine($"Metrics available at http://localhost:9090/metrics");
        Console.WriteLine();

        // Create producer with metrics enabled
        await using var producer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            // opt.EnableMetrics = true; // TODO: Not yet implemented in Surgewave
        });

        // Create consumer
        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            opt.GroupId = "monitoring-demo";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
            opt.EnableAutoCommit = false;
        });

        // Send messages
        Console.WriteLine("Sending 100 messages...");
        for (int i = 0; i < 100; i++)
        {
            await producer.ProduceAsync("metrics-demo", $"key-{i}", $"value-{i}");
        }
        await producer.FlushAsync();
        Console.WriteLine("Messages sent!");
        Console.WriteLine();

        // Fetch and display metrics from endpoint
        Console.WriteLine("--- Broker Metrics (from /metrics endpoint) ---");
        try
        {
            using var httpClient = new HttpClient();
            var metrics = await httpClient.GetStringAsync("http://localhost:9090/metrics");

            var surgewaveMetrics = metrics.Split('\n')
                .Where(l => l.StartsWith("surgewave_") && !l.StartsWith("#"))
                .Take(10);

            foreach (var metric in surgewaveMetrics)
            {
                Console.WriteLine($"  {metric}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Could not fetch metrics: {ex.Message}");
        }
        Console.WriteLine();

        // Get producer statistics
        Console.WriteLine("--- Producer Statistics ---");
        // var stats = producer.GetStatistics(); // TODO: Not yet implemented in Surgewave
        // Console.WriteLine($"  Messages sent: {stats.MessagesSent}");
        // Console.WriteLine($"  Bytes sent: {stats.BytesSent}");
        // Console.WriteLine($"  Average latency: {stats.AverageLatencyMs:F2}ms");
        // Console.WriteLine($"  Errors: {stats.Errors}");
        Console.WriteLine();

        // Monitor consumer lag
        Console.WriteLine("--- Consumer Lag ---");
        consumer.Subscribe("metrics-demo");

        // Consume some messages (not all)
        for (int i = 0; i < 30; i++)
        {
            await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
        }
        await consumer.CommitAsync();

        // TODO: Admin client not yet implemented
        // await using var admin = new SurgewaveAdminClient(opt =>
        //     opt.BootstrapServers = $"localhost:{runtime.Port}");
        //
        // var groupOffsets = await admin.GetConsumerGroupOffsetsAsync("monitoring-demo");
        // foreach (var offset in groupOffsets)
        // {
        //     var highWatermark = await admin.GetHighWatermarkAsync(offset.Topic, offset.Partition);
        //     var lag = highWatermark - offset.Offset;
        //
        //     Console.WriteLine($"  {offset.Topic}/{offset.Partition}: offset={offset.Offset}, hwm={highWatermark}, lag={lag}");
        // }
        Console.WriteLine();

        // Summary
        Console.WriteLine("--- Monitoring Summary ---");
        Console.WriteLine("Key metrics to monitor:");
        Console.WriteLine("  - Messages in/out rate");
        Console.WriteLine("  - Consumer lag (should stay low)");
        Console.WriteLine("  - Request latency");
        Console.WriteLine("  - Under-replicated partitions");
        Console.WriteLine("  - Error rates");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
