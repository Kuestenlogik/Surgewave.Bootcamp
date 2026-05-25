# Lesson 10.5: Schema in Practice - Instructions

## Step 1: Build Schema-Aware Producer

Create a producer that validates messages:

```csharp
public class SchemaAwareProducer : IAsyncDisposable
{
    private readonly SurgewaveProducer<byte[], byte[]> _producer;
    private readonly ISchemaRegistry _registry;
    private readonly Dictionary<string, (int Id, JsonSchemaValidator Validator)> _cache = new();

    public async Task<bool> ProduceAsync(string topic, string key, string jsonValue)
    {
        var subject = $"{topic}-value";

        // Get or cache schema
        if (!_cache.TryGetValue(subject, out var cached))
        {
            var schema = _registry.GetLatestSchema(subject);
            if (schema == null)
                throw new Exception($"No schema for {subject}");

            cached = (schema.Value.Id, new JsonSchemaValidator(schema.Value.Schema));
            _cache[subject] = cached;
        }

        // Validate
        var result = cached.Validator.Validate(jsonValue);
        if (!result.IsValid)
        {
            Console.WriteLine($"Validation failed: {string.Join(", ", result.Errors)}");
            return false;
        }

        // Produce with schema ID
        var payload = Encoding.UTF8.GetBytes(jsonValue);
        var message = SerializeWithSchema(cached.Id, payload);
        await _producer.ProduceAsync(topic, Encoding.UTF8.GetBytes(key), message);
        return true;
    }
}
```

## Step 2: Build Schema-Aware Consumer

Create a consumer that validates and deserializes:

```csharp
public class SchemaAwareConsumer : IAsyncDisposable
{
    private readonly SurgewaveConsumer<byte[], byte[]> _consumer;
    private readonly ISchemaRegistry _registry;
    private readonly Dictionary<int, JsonSchemaValidator> _validators = new();

    public async Task<ConsumedMessage?> ConsumeAsync(TimeSpan timeout)
    {
        var result = await _consumer.ConsumeAsync(timeout);
        if (result?.Value == null) return null;

        // Extract schema ID
        var (schemaId, payload) = DeserializeWithSchema(result.Value);

        // Get or create validator
        if (!_validators.TryGetValue(schemaId, out var validator))
        {
            var schema = _registry.GetSchema(schemaId);
            if (schema == null)
                throw new Exception($"Unknown schema ID: {schemaId}");

            validator = new JsonSchemaValidator(schema);
            _validators[schemaId] = validator;
        }

        // Validate
        var json = Encoding.UTF8.GetString(payload);
        var validationResult = validator.Validate(json);

        return new ConsumedMessage(
            Topic: result.Topic,
            Key: result.Key != null ? Encoding.UTF8.GetString(result.Key) : null,
            Value: json,
            SchemaId: schemaId,
            IsValid: validationResult.IsValid,
            ValidationErrors: validationResult.Errors
        );
    }
}
```

## Step 3: Handle Invalid Messages

Route invalid messages to dead-letter queue:

```csharp
public async Task ProcessWithDeadLetter(string sourceTopic, string dlqTopic)
{
    var message = await _consumer.ConsumeAsync(TimeSpan.FromSeconds(1));
    if (message == null) return;

    if (!message.IsValid)
    {
        // Send to dead-letter queue
        await _dlqProducer.ProduceAsync(dlqTopic, message.Key ?? "unknown",
            JsonSerializer.Serialize(new
            {
                originalTopic = sourceTopic,
                errors = message.ValidationErrors,
                originalValue = message.Value,
                timestamp = DateTime.UtcNow
            }));
        return;
    }

    // Process valid message
    await ProcessMessage(message);
}
```

## Step 4: Support Multiple Schema Versions

Handle consuming messages with different schema versions:

```csharp
public async Task<T?> ConsumeTypedAsync<T>()
{
    var message = await ConsumeAsync(TimeSpan.FromSeconds(1));
    if (message == null || !message.IsValid) return default;

    // Deserialize based on schema version
    var schemaVersion = _registry.GetSchemaById(message.SchemaId);

    // For v1 -> v2 migration, fill in defaults
    if (schemaVersion?.Version == 1)
    {
        var v1Data = JsonSerializer.Deserialize<JsonElement>(message.Value);
        // Transform v1 to current format
        return TransformV1ToLatest<T>(v1Data);
    }

    return JsonSerializer.Deserialize<T>(message.Value);
}
```

## Step 5: Implement Graceful Evolution

Demonstrate evolving schemas while maintaining compatibility:

```csharp
// Phase 1: Register v1, produce v1 messages
var v1 = registry.RegisterSchema("events-value", v1Schema);
await producer.ProduceAsync("events", "k1", """{"id":1,"name":"test"}""");

// Phase 2: Register v2 (backward compatible), start producing v2
var v2 = registry.RegisterSchema("events-value", v2Schema);
await producer.ProduceAsync("events", "k2", """{"id":2,"name":"test","email":"a@b.com"}""");

// Consumer can read both v1 and v2 messages
// - v1 messages: use defaults for missing fields
// - v2 messages: full data available
```

## Verification

Run the completed solution:
```bash
cd completed
dotnet run
```

You should see:
- Schema-validated message production
- Schema-aware message consumption
- Multi-version message handling
- Invalid messages routed to DLQ
