// Surgewave Bootcamp - Unit 3, Lesson 1: Custom Serialization
//
// In this lesson, you'll learn to:
// - Create custom serializers for complex types
// - Configure producers and consumers with custom serialization
// - Send and receive domain objects
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
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

// TODO: Implement JsonSerializer<T> that implements both ISerializer<T> and IDeserializer<T>
public class JsonSerializer<T> : ISerializer<T>, IDeserializer<T>
{
    public byte[]? Serialize(T? data, string topic)
    {
        // Hint: Return null if data is null, otherwise use System.Text.Json.JsonSerializer.SerializeToUtf8Bytes
        throw new NotImplementedException();
    }

    public T Deserialize(ReadOnlySpan<byte> data, string topic)
    {
        // Hint: Use System.Text.Json.JsonSerializer.Deserialize<T>
        throw new NotImplementedException();
    }
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
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine($"Broker started on port {runtime.Port}");

        // TODO: Create producer with JsonSerializer<Order>
        // Hint: opt.ValueSerializer = new JsonSerializer<Order>();
        await using var producer = new SurgewaveProducer<string, Order>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            // TODO: Set ValueSerializer
        });

        // TODO: Create consumer with JsonSerializer<Order>
        // Hint: opt.ValueDeserializer = new JsonSerializer<Order>();
        await using var consumer = new SurgewaveConsumer<string, Order>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
            // TODO: Set ValueDeserializer
        });

        consumer.Subscribe("orders");

        // TODO: Create and send an order
        // var order = new Order("ORD-001", "CUST-123", 99.99m, DateTime.UtcNow);
        // await producer.ProduceAsync("orders", order.OrderId, order);

        Console.WriteLine("Sent order to topic 'orders'");

        // TODO: Consume and display the order
        // var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
        // if (result != null)
        // {
        //     Console.WriteLine($"Received: {result.Value}");
        // }

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
