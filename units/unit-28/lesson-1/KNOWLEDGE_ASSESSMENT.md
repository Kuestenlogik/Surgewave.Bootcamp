# Knowledge Assessment: Lesson 3.1 - Custom Serialization

## Multiple Choice

### 1. Why do you need custom serialization for complex types in Surgewave?
- [ ] A) Surgewave doesn't support any built-in serialization
- [ ] B) Surgewave's default serializers only handle strings and byte arrays
- [ ] C) Custom serialization is required for security
- [ ] D) All messages must be encrypted

<details>
<summary>Answer</summary>
**B)** Surgewave's default serializers only handle strings and byte arrays. For domain objects and complex types, you need custom serializers to convert objects to/from bytes.
</details>

### 2. Which serialization format is best for high-throughput scenarios?
- [ ] A) JSON - because it's human-readable
- [ ] B) XML - because it's well-established
- [ ] C) Protobuf - because it's compact and fast
- [ ] D) YAML - because it's easy to edit

<details>
<summary>Answer</summary>
**C)** Protobuf - because it's compact and fast. Protobuf produces smaller payloads and serializes/deserializes faster than text-based formats like JSON or XML.
</details>

### 3. What are the two interfaces used for custom serialization in Surgewave?
- [ ] A) `IEncoder<T>` and `IDecoder<T>`
- [ ] B) `ISerializer<T>` and `IDeserializer<T>`
- [ ] C) `IConverter<T>` and `IFormatter<T>`
- [ ] D) `IWriter<T>` and `IReader<T>`

<details>
<summary>Answer</summary>
**B)** `ISerializer<T>` and `IDeserializer<T>`. The producer uses `ISerializer<T>.Serialize(T? data, string topic)` and the consumer uses `IDeserializer<T>.Deserialize(ReadOnlySpan<byte> data, string topic)`.
</details>

### 4. When would JSON serialization be preferred over Protobuf?
- [ ] A) When maximum throughput is required
- [ ] B) When debugging and human-readability are important
- [ ] C) When message size must be minimized
- [ ] D) When schema changes are not allowed

<details>
<summary>Answer</summary>
**B)** When debugging and human-readability are important. JSON is human-readable and has flexible schemas, making it easier to debug and work with during development.
</details>

## True/False

### 5. System.Text.Json is the recommended JSON library for .NET Core and later.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - System.Text.Json is the built-in, high-performance JSON library for modern .NET. It's faster and uses less memory than Newtonsoft.Json for most scenarios.
</details>

### 6. A custom serializer must be thread-safe because Surgewave may call it from multiple threads.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Surgewave producers and consumers may operate on multiple threads, so serializers must be thread-safe. Stateless serializers (like the JSON example) are inherently thread-safe.
</details>

## Code Challenge

### 7. Complete this custom JSON serializer implementation:

```csharp
public class JsonSerializer<T> : ISerializer<T>, ____________<T>
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public byte[]? Serialize(T? data, string topic)
    {
        return data is null ? null : System.Text.Json.JsonSerializer.____________(data, _options);
    }

    public T Deserialize(ReadOnlySpan<byte> data, string topic)
    {
        return System.Text.Json.JsonSerializer.____________<T>(data, _options)!;
    }
}

// Usage with producer and consumer:
await using var producer = new SurgewaveProducer<string, Order>(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.____________ = new JsonSerializer<Order>();
});

await using var consumer = new SurgewaveConsumer<string, Order>(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.____________ = new JsonSerializer<Order>();
});
```

<details>
<summary>Answer</summary>
```csharp
public class JsonSerializer<T> : ISerializer<T>, IDeserializer<T>
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public byte[]? Serialize(T? data, string topic)
    {
        return data is null ? null : System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data, _options);
    }

    public T Deserialize(ReadOnlySpan<byte> data, string topic)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(data, _options)!;
    }
}

// Usage with producer and consumer:
await using var producer = new SurgewaveProducer<string, Order>(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.ValueSerializer = new JsonSerializer<Order>();
});

await using var consumer = new SurgewaveConsumer<string, Order>(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.ValueDeserializer = new JsonSerializer<Order>();
});
```

Key points:
- Implement both `ISerializer<T>` and `IDeserializer<T>`
- `SerializeToUtf8Bytes` directly produces a byte array
- `Deserialize<T>` converts bytes back to the object
- Producer uses `ValueSerializer`, consumer uses `ValueDeserializer`
</details>

## Score
- 7/7: Excellent! You understand custom serialization
- 5-6/7: Good grasp of serializer concepts
- 3-4/7: Review the ISerializer interface and JSON usage
- <3/7: Re-do the lesson and implement a custom serializer
