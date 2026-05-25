# Lesson 6.3: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Design Base Schema

```csharp
// Version 1: Initial schema
public record OrderCreatedV1
{
    public required string OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required decimal Amount { get; init; }
    public required DateTime CreatedAt { get; init; }
}
```

## Step 2: Evolve Schema (Backward Compatible)

```csharp
// Version 2: Added optional fields
public record OrderCreatedV2
{
    public required string OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required decimal Amount { get; init; }
    public required DateTime CreatedAt { get; init; }

    // New optional fields (backward compatible)
    public string? Currency { get; init; } = "USD";
    public string? PromoCode { get; init; }
    public List<string> Tags { get; init; } = new();
}
```

## Step 3: Implement Tolerant Reader

```csharp
public class TolerantJsonSerializer<T> : ISerializer<T>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // Ignore unknown properties (forward compatible)
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        // Handle missing properties (backward compatible)
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public byte[] Serialize(T data) =>
        JsonSerializer.SerializeToUtf8Bytes(data, Options);

    public T Deserialize(byte[] data) =>
        JsonSerializer.Deserialize<T>(data, Options)!;
}
```

## Step 4: Add Schema Version Header

```csharp
public async Task ProduceWithVersion<T>(
    IProducer<string, T> producer,
    string topic,
    string key,
    T value,
    int schemaVersion)
{
    var headers = new Headers
    {
        { "schema-version", BitConverter.GetBytes(schemaVersion) },
        { "schema-type", Encoding.UTF8.GetBytes(typeof(T).Name) }
    };

    await producer.ProduceAsync(topic, key, value, headers);
}
```

## Step 5: Handle Multiple Versions

```csharp
public class VersionedEventHandler
{
    public async Task HandleOrderCreated(ConsumeResult<string, byte[]> message)
    {
        var version = GetSchemaVersion(message.Headers);

        OrderCreatedV2 order = version switch
        {
            1 => UpgradeV1ToV2(Deserialize<OrderCreatedV1>(message.Value)),
            2 => Deserialize<OrderCreatedV2>(message.Value),
            _ => throw new UnknownSchemaVersionException(version)
        };

        await ProcessOrder(order);
    }

    private OrderCreatedV2 UpgradeV1ToV2(OrderCreatedV1 v1) => new()
    {
        OrderId = v1.OrderId,
        CustomerId = v1.CustomerId,
        Amount = v1.Amount,
        CreatedAt = v1.CreatedAt,
        Currency = "USD", // Default for old events
        PromoCode = null,
        Tags = new()
    };
}
```

## Step 6: Schema Registry Pattern

```csharp
public interface ISchemaRegistry
{
    Task<int> RegisterSchema<T>(string subject);
    Task<Type> GetSchema(string subject, int version);
    Task<bool> IsCompatible<T>(string subject);
}

public class InMemorySchemaRegistry : ISchemaRegistry
{
    private readonly Dictionary<(string, int), Type> _schemas = new();
    private readonly Dictionary<string, int> _latestVersions = new();

    public Task<int> RegisterSchema<T>(string subject)
    {
        var version = _latestVersions.GetValueOrDefault(subject, 0) + 1;
        _schemas[(subject, version)] = typeof(T);
        _latestVersions[subject] = version;
        return Task.FromResult(version);
    }
}
```

## Schema Format Comparison

| Format | Schema Required | Human Readable | Size | Evolution |
|--------|-----------------|----------------|------|-----------|
| JSON | No | Yes | Large | Manual |
| Protobuf | Yes | No | Small | Built-in |
| Avro | Yes | No | Small | Built-in |
| MessagePack | No | No | Small | Manual |

## Evolution Strategies

### Strategy 1: Topic Per Version

```
orders.created.v1  →  orders.created.v2  →  orders.created.v3
```

### Strategy 2: Single Topic, Version Header

```
orders.created
  └── header: schema-version = 1, 2, 3...
```

### Strategy 3: Envelope Pattern

```csharp
public record EventEnvelope
{
    public required string EventType { get; init; }
    public required int SchemaVersion { get; init; }
    public required JsonElement Payload { get; init; }
    public required DateTime Timestamp { get; init; }
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

---

**Next:** → [Lesson 6.4: Event-Driven Patterns](../lesson-4/README.md)
