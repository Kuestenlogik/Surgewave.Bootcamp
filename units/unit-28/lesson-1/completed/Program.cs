// Surgewave Bootcamp - Unit 3, Lesson 1: Custom Serialization
// COMPLETED SOLUTION

using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Client.Serialization;

// Domain object
public record Order(
    string OrderId,
    string CustomerId,
    decimal Amount,
    DateTime CreatedAt);

// JSON Serializer implementation
public class JsonSerializer<T> : ISerializer<T>, IDeserializer<T>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public byte[]? Serialize(T? data, string topic) =>
        data is null ? null : System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data, Options);

    public T Deserialize(ReadOnlySpan<byte> data, string topic) =>
        System.Text.Json.JsonSerializer.Deserialize<T>(data, Options)!;
}

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 3.1 - Custom Serialization ===");
        Console.WriteLine();

        // Start embedded broker
        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine($"Broker started on port {runtime.Port}");

        // Create producer with custom serializer
        await using var producer = new SurgewaveProducer<string, Order>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            opt.ValueSerializer = new JsonSerializer<Order>();
        });

        // Create consumer with custom serializer
        await using var consumer = new SurgewaveConsumer<string, Order>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
            opt.ValueDeserializer = new JsonSerializer<Order>();
        });

        consumer.Subscribe("orders");

        // Create and send orders
        var orders = new[]
        {
            new Order("ORD-001", "CUST-123", 99.99m, DateTime.UtcNow),
            new Order("ORD-002", "CUST-456", 149.50m, DateTime.UtcNow),
            new Order("ORD-003", "CUST-123", 25.00m, DateTime.UtcNow)
        };

        Console.WriteLine("Sending orders...");
        foreach (var order in orders)
        {
            await producer.ProduceAsync("orders", order.OrderId, order);
            Console.WriteLine($"  Sent: {order.OrderId} - ${order.Amount}");
        }

        Console.WriteLine();
        Console.WriteLine("Consuming orders...");

        // Consume and display orders
        for (int i = 0; i < orders.Length; i++)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
            if (result != null)
            {
                Console.WriteLine($"  Received: {result.Value.OrderId}");
                Console.WriteLine($"    Customer: {result.Value.CustomerId}");
                Console.WriteLine($"    Amount: {result.Value.Amount:C}");
                Console.WriteLine($"    Created: {result.Value.CreatedAt:u}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
