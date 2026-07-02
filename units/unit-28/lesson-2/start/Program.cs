// Surgewave Bootcamp - Unit 3, Lesson 2: Headers & Metadata
//
// In this lesson, you'll learn to:
// - Add headers to messages
// - Read headers from consumed messages
// - Propagate correlation IDs through pipelines
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 3.2 - Headers & Metadata ===");
        Console.WriteLine();

        // Start embedded broker
        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine($"Broker started on port {runtime.Port}");

        await using var producer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = $"localhost:{runtime.Port}");

        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        consumer.Subscribe("events");

        // TODO: Create headers with correlation ID
        // var correlationId = Guid.NewGuid().ToString();
        // var headers = new Headers
        // {
        //     { "x-correlation-id", Encoding.UTF8.GetBytes(correlationId) },
        //     { "x-source", Encoding.UTF8.GetBytes("order-service") }
        // };

        // TODO: Send message with headers
        // await producer.ProduceAsync("events", "event-1", "Order created", headers);
        Console.WriteLine("Sent message with headers");

        // TODO: Consume and read headers
        // var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
        // if (result != null)
        // {
        //     Console.WriteLine($"Message: {result.Value}");
        //     foreach (var header in result.Headers)
        //     {
        //         var value = Encoding.UTF8.GetString(header.Value);
        //         Console.WriteLine($"  {header.Key}: {value}");
        //     }
        // }

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
