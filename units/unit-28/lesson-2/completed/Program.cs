// Surgewave Bootcamp - Unit 3, Lesson 2: Headers & Metadata
// COMPLETED SOLUTION

using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

public static class HeaderExtensions
{
    public static string? GetCorrelationId(this IReadOnlyDictionary<string, byte[]> headers)
    {
        if (headers.TryGetValue("x-correlation-id", out var bytes))
            return Encoding.UTF8.GetString(bytes);
        return null;
    }

    public static Dictionary<string, byte[]> WithCorrelationId(this Dictionary<string, byte[]> headers, string correlationId)
    {
        headers["x-correlation-id"] = Encoding.UTF8.GetBytes(correlationId);
        return headers;
    }

    public static Dictionary<string, byte[]> WithSource(this Dictionary<string, byte[]> headers, string source)
    {
        headers["x-source"] = Encoding.UTF8.GetBytes(source);
        return headers;
    }
}

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

        // Create headers with correlation ID
        var correlationId = Guid.NewGuid().ToString();
        var headers = new Dictionary<string, byte[]>()
            .WithCorrelationId(correlationId)
            .WithSource("order-service");

        // Add timestamp header
        headers["x-timestamp"] = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O"));

        Console.WriteLine($"Sending message with correlation ID: {correlationId}");
        Console.WriteLine();

        // Send messages with headers
        var events = new[]
        {
            ("event-1", "Order created"),
            ("event-2", "Payment processed"),
            ("event-3", "Order shipped")
        };

        foreach (var (key, value) in events)
        {
            await producer.ProduceAsync("events", key, value, headers);
            Console.WriteLine($"  Sent: {key} - {value}");
        }

        Console.WriteLine();
        Console.WriteLine("Consuming messages with headers...");
        Console.WriteLine();

        // Consume and read headers
        for (int i = 0; i < events.Length; i++)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
            if (result != null)
            {
                Console.WriteLine($"Message: {result.Value}");
                Console.WriteLine("Headers:");
                if (result.Headers is { } resultHeaders)
                {
                    foreach (var header in resultHeaders)
                    {
                        var headerValue = Encoding.UTF8.GetString(header.Value);
                        Console.WriteLine($"  {header.Key}: {headerValue}");
                    }
                }

                // Use extension method
                var receivedCorrelationId = result.Headers?.GetCorrelationId();
                Console.WriteLine($"  (parsed correlation ID: {receivedCorrelationId})");
                Console.WriteLine();
            }
        }

        Console.WriteLine("Lesson complete!");
    }
}
