# Lesson 3.1: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Define a Domain Object

Create a simple order class:

```csharp
public record Order(
    string OrderId,
    string CustomerId,
    decimal Amount,
    DateTime CreatedAt);
```

## Step 2: Create a JSON Serializer

Implement both `ISerializer<T>` and `IDeserializer<T>`:

```csharp
using System.Text.Json;

public class JsonSerializer<T> : ISerializer<T>, IDeserializer<T>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public byte[]? Serialize(T? data, string topic) =>
        data is null ? null : System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data, Options);

    public T Deserialize(ReadOnlySpan<byte> data, string topic) =>
        System.Text.Json.JsonSerializer.Deserialize<T>(data, Options)!;
}
```

## Step 3: Configure Producer with Custom Serializer

```csharp
var producer = new SurgewaveProducer<string, Order>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.ValueSerializer = new JsonSerializer<Order>();
});
```

## Step 4: Send Complex Objects

```csharp
var order = new Order(
    "ORD-001",
    "CUST-123",
    99.99m,
    DateTime.UtcNow);

await producer.ProduceAsync("orders", order.OrderId, order);
```

## Step 5: Configure Consumer with Custom Deserializer

Note: The consumer uses `ValueDeserializer` (not `ValueSerializer`):

```csharp
var consumer = new SurgewaveConsumer<string, Order>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.ValueDeserializer = new JsonSerializer<Order>();
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
```

## Step 6: Receive and Process Objects

```csharp
consumer.Subscribe("orders");

var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
if (result != null)
{
    Console.WriteLine($"Received order: {result.Value.OrderId}");
    Console.WriteLine($"  Customer: {result.Value.CustomerId}");
    Console.WriteLine($"  Amount: {result.Value.Amount:C}");
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. When should you use JSON vs binary serialization?
2. What happens if serialization fails?
3. How do you handle schema evolution with JSON?

---

**Next:** → [Lesson 3.2: Headers & Metadata](../lesson-2/README.md)
