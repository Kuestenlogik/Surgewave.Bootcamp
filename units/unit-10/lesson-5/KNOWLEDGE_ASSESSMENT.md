# Quiz: Lesson 10.5 - Schema in Practice

## Multiple Choice

### 1. Why should producers cache schema IDs?
- [ ] A) To reduce memory usage
- [ ] B) To avoid repeated registry lookups for every message
- [ ] C) To enable offline operation
- [ ] D) To support schema versioning

<details>
<summary>Answer</summary>
**B)** Caching schema IDs avoids a network round-trip to the registry for every message produced. Once the schema is registered and ID obtained, it can be reused for all subsequent messages of that type.
</details>

### 2. What should happen to messages that fail schema validation on consume?
- [ ] A) Silently drop them
- [ ] B) Retry indefinitely
- [ ] C) Send to a dead-letter queue for investigation
- [ ] D) Modify the schema to accept them

<details>
<summary>Answer</summary>
**C)** Invalid messages should be sent to a dead-letter queue (DLQ) for investigation. This prevents blocking the consumer while preserving the problematic messages for debugging and potential manual remediation.
</details>

### 3. In the message format [SchemaID|Payload], why is the schema ID prepended?
- [ ] A) For encryption purposes
- [ ] B) To enable the consumer to retrieve the correct schema for deserialization
- [ ] C) For compression
- [ ] D) For ordering guarantees

<details>
<summary>Answer</summary>
**B)** Prepending the schema ID allows consumers to look up the exact schema version used to serialize the message, ensuring correct deserialization even when multiple schema versions exist.
</details>

## True/False

### 4. Validation should only happen at produce time, not consume time.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Validation should happen at BOTH produce time (to catch errors early and prevent bad data from entering the system) AND consume time (to handle edge cases, corrupted data, or messages from non-compliant producers).
</details>

### 5. Consumers should maintain a cache of validators for different schema versions.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Messages in a topic may have been produced with different schema versions. Caching validators by schema ID avoids repeatedly fetching schemas and constructing validators for each message.
</details>

## Code Challenge

### 6. What's missing from this schema-aware producer implementation?

```csharp
public class SchemaAwareProducer<T>
{
    private readonly SurgewaveProducer<byte[], byte[]> _producer;
    private readonly ISchemaRegistry _registry;
    private readonly string _subject;

    public async Task ProduceAsync(string topic, string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        var payload = Encoding.UTF8.GetBytes(json);

        await _producer.ProduceAsync(topic,
            Encoding.UTF8.GetBytes(key), payload);
    }
}
```

<details>
<summary>Answer</summary>
Several critical pieces are missing:

1. **Schema ID lookup/caching** - No call to registry to get schema ID
2. **Validation** - No validation of the JSON against the schema
3. **Schema ID prepending** - The schema ID is not included in the message

Fix:
```csharp
public class SchemaAwareProducer<T>
{
    private readonly SurgewaveProducer<byte[], byte[]> _producer;
    private readonly ISchemaRegistry _registry;
    private readonly string _subject;
    private int? _cachedSchemaId;  // Cache schema ID

    public async Task ProduceAsync(string topic, string key, T value)
    {
        // 1. Get or cache schema ID
        var schemaId = _cachedSchemaId ??= await _registry.GetSchemaIdAsync(_subject);

        // 2. Serialize
        var json = JsonSerializer.Serialize(value);

        // 3. Validate against schema
        var schema = await _registry.GetSchemaAsync(schemaId);
        ValidateOrThrow(json, schema);

        // 4. Prepend schema ID to payload
        var payload = Encoding.UTF8.GetBytes(json);
        var message = PrependSchemaId(schemaId, payload);

        await _producer.ProduceAsync(topic,
            Encoding.UTF8.GetBytes(key), message);
    }
}
```
</details>

### 7. Complete the consumer's schema extraction logic:

```csharp
public async Task<T?> ConsumeAsync()
{
    var result = await _consumer.ConsumeAsync();
    if (result?.Value == null) return default;

    // Extract schema ID from first 4 bytes, payload from rest
    // How do you extract the schema ID and payload?
    var schemaId = ???;
    var payload = ???;

    // ... rest of processing
}
```

<details>
<summary>Answer</summary>

```csharp
public async Task<T?> ConsumeAsync()
{
    var result = await _consumer.ConsumeAsync();
    if (result?.Value == null) return default;

    // Extract schema ID from first 4 bytes (assuming 4-byte integer ID)
    var schemaId = BitConverter.ToInt32(result.Value, 0);

    // Extract payload (everything after the schema ID)
    var payload = new byte[result.Value.Length - 4];
    Array.Copy(result.Value, 4, payload, 0, payload.Length);

    // Or using Span for zero-copy:
    // var schemaId = BinaryPrimitives.ReadInt32BigEndian(result.Value.AsSpan(0, 4));
    // var payload = result.Value.AsSpan(4);

    // Get validator and validate
    var validator = await GetOrCreateValidatorAsync(schemaId);
    var json = Encoding.UTF8.GetString(payload);
    validator.Validate(json).ThrowIfInvalid();

    // Deserialize and return
    return JsonSerializer.Deserialize<T>(json);
}
```

Note: The exact format (4-byte ID, big/little endian) depends on your schema registry protocol.
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
