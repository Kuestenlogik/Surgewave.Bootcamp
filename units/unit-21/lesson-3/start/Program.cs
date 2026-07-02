// Surgewave Bootcamp - Unit 4, Lesson 3: Monitoring
//
// In this lesson, you'll learn to:
// - Enable metrics endpoints
// - View broker and client metrics
// - Monitor consumer lag
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
// using Kuestenlogik.Surgewave.Client.Native.Operations.Admin; // TODO: Admin client not yet implemented
using Kuestenlogik.Surgewave.Client.Consumer;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 4.3 - Monitoring ===");
        Console.WriteLine();

        // TODO: Start broker with metrics endpoint enabled
        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            // TODO: Enable metrics on port 9090
            // .WithMetricsPort(9090)
            .Build()
            .StartAsync();

        Console.WriteLine($"Broker started on port {runtime.Port}");
        // Console.WriteLine($"Metrics available at http://localhost:9090/metrics");

        await using var producer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            // TODO: Enable client metrics
            // opt.EnableMetrics = true;
        });

        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            opt.GroupId = "monitoring-demo";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        // Send some messages
        for (int i = 0; i < 100; i++)
        {
            await producer.ProduceAsync("metrics-demo", $"key-{i}", $"value-{i}");
        }

        // TODO: Fetch and display metrics
        // using var httpClient = new HttpClient();
        // var metrics = await httpClient.GetStringAsync("http://localhost:9090/metrics");

        // TODO: Get producer statistics
        // var stats = producer.GetStatistics();

        // TODO: Monitor consumer lag using admin client

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
