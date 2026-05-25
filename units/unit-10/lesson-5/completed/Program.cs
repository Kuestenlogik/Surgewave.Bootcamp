// Surgewave Bootcamp - Unit 10, Lesson 5: Schema in Practice
// COMPLETED SOLUTION

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 10.5 - Schema in Practice ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// ============== Setup: Schema Registry ==============
var registry = new SchemaRegistry();

// Register v1 schema
var v1Schema = """
{
    "type": "object",
    "properties": {
        "id": { "type": "integer" },
        "name": { "type": "string" }
    },
    "required": ["id", "name"]
}
""";
var v1 = registry.RegisterSchema("user-events-value", v1Schema, "Initial schema");
Console.WriteLine($"Registered schema v{v1.Version} (ID: {v1.Id})");

// ============== Demo 1: Schema-Aware Producer ==============
Console.WriteLine();
Console.WriteLine("--- Demo 1: Schema-Aware Producer ---");

await using var rawProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var schemaProducer = new SchemaAwareProducer(rawProducer, registry);

// Produce valid messages
Console.WriteLine("  Producing valid messages:");
var valid1 = await schemaProducer.ProduceAsync("user-events", "user-1", """{"id": 1, "name": "Alice"}""");
Console.WriteLine($"    Message 1: {(valid1 ? "OK" : "FAILED")}");

var valid2 = await schemaProducer.ProduceAsync("user-events", "user-2", """{"id": 2, "name": "Bob"}""");
Console.WriteLine($"    Message 2: {(valid2 ? "OK" : "FAILED")}");

// Try producing invalid messages
Console.WriteLine("  Producing invalid messages:");
var invalid1 = await schemaProducer.ProduceAsync("user-events", "user-3", """{"id": "not-a-number", "name": "Charlie"}""");
Console.WriteLine($"    Invalid type: {(invalid1 ? "OK" : "REJECTED")}");

var invalid2 = await schemaProducer.ProduceAsync("user-events", "user-4", """{"id": 4}""");
Console.WriteLine($"    Missing required: {(invalid2 ? "OK" : "REJECTED")}");

Console.WriteLine();

// ============== Demo 2: Schema-Aware Consumer ==============
Console.WriteLine("--- Demo 2: Schema-Aware Consumer ---");

await using var rawConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "schema-demo-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

rawConsumer.Subscribe("user-events");

var schemaConsumer = new SchemaAwareConsumer(rawConsumer, registry);

Console.WriteLine("  Consuming messages with schema validation:");
var consumed = 0;
var timeout = DateTime.UtcNow.AddSeconds(2);

while (consumed < 2 && DateTime.UtcNow < timeout)
{
    var message = await schemaConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
    if (message != null)
    {
        Console.WriteLine($"    Key: {message.Key}, SchemaID: {message.SchemaId}, Valid: {message.IsValid}");
        Console.WriteLine($"      Value: {message.Value}");
        consumed++;
    }
}
Console.WriteLine();

// ============== Demo 3: Schema Evolution ==============
Console.WriteLine("--- Demo 3: Schema Evolution ---");

// Register v2 schema (backward compatible - adds optional field)
var v2Schema = """
{
    "type": "object",
    "properties": {
        "id": { "type": "integer" },
        "name": { "type": "string" },
        "email": { "type": "string" }
    },
    "required": ["id", "name"]
}
""";
var v2 = registry.RegisterSchema("user-events-value", v2Schema, "Added optional email field");
Console.WriteLine($"  Registered schema v{v2.Version} (ID: {v2.Id})");

// Producer now uses v2 schema
schemaProducer.ClearCache(); // Force refresh

Console.WriteLine("  Producing v2 messages:");
var v2msg1 = await schemaProducer.ProduceAsync("user-events", "user-5",
    """{"id": 5, "name": "Diana", "email": "diana@example.com"}""");
Console.WriteLine($"    V2 with email: {(v2msg1 ? "OK" : "FAILED")}");

var v2msg2 = await schemaProducer.ProduceAsync("user-events", "user-6",
    """{"id": 6, "name": "Eve"}""");
Console.WriteLine($"    V2 without email: {(v2msg2 ? "OK" : "FAILED")}");
Console.WriteLine();

// ============== Demo 4: Consuming Mixed Versions ==============
Console.WriteLine("--- Demo 4: Consuming Mixed Versions ---");

Console.WriteLine("  Consuming messages with different schema versions:");
timeout = DateTime.UtcNow.AddSeconds(2);

while (DateTime.UtcNow < timeout)
{
    var message = await schemaConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
    if (message != null)
    {
        var version = registry.GetSchemaById(message.SchemaId)?.Version ?? 0;
        Console.WriteLine($"    Key: {message.Key}, Schema: v{version}, Valid: {message.IsValid}");

        // Parse and show data
        var data = JsonDocument.Parse(message.Value).RootElement;
        var hasEmail = data.TryGetProperty("email", out var email);
        Console.WriteLine($"      Name: {data.GetProperty("name")}, Email: {(hasEmail ? email.GetString() : "(not in schema)")}");
    }
}
Console.WriteLine();

// ============== Demo 5: Dead Letter Queue ==============
Console.WriteLine("--- Demo 5: Dead Letter Queue ---");

await using var dlqProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var processor = new MessageProcessor(schemaConsumer, dlqProducer);

// Produce a message that will be manually invalidated for demo
await rawProducer.ProduceAsync("user-events",
    Encoding.UTF8.GetBytes("bad-message"),
    Encoding.UTF8.GetBytes("not-valid-schema-format")); // No schema ID prefix

Console.WriteLine("  Processing messages (invalid ones go to DLQ):");

await using var dlqConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "dlq-reader";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

dlqConsumer.Subscribe("user-events-dlq");

// Process a few messages
for (int i = 0; i < 3; i++)
{
    await processor.ProcessWithDeadLetterAsync("user-events", "user-events-dlq");
}

// Check DLQ
Console.WriteLine("  Checking Dead Letter Queue:");
timeout = DateTime.UtcNow.AddSeconds(1);
while (DateTime.UtcNow < timeout)
{
    var dlqMessage = await dlqConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
    if (dlqMessage?.Value != null)
    {
        var dlqData = Encoding.UTF8.GetString(dlqMessage.Value);
        Console.WriteLine($"    DLQ message: {dlqData[..Math.Min(100, dlqData.Length)]}...");
    }
}
Console.WriteLine();

// ============== Demo 6: Full Integration Example ==============
Console.WriteLine("--- Demo 6: Full Integration Example ---");

// Simulate a complete workflow
var orderRegistry = new SchemaRegistry();
orderRegistry.RegisterSchema("orders-value", """
{
    "type": "object",
    "properties": {
        "orderId": { "type": "string" },
        "customerId": { "type": "integer" },
        "items": {
            "type": "array",
            "items": {
                "type": "object",
                "properties": {
                    "sku": { "type": "string" },
                    "quantity": { "type": "integer" }
                },
                "required": ["sku", "quantity"]
            }
        },
        "total": { "type": "number" }
    },
    "required": ["orderId", "customerId", "items", "total"]
}
""", "Order schema v1");

await using var orderProducerRaw = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var orderProducer = new SchemaAwareProducer(orderProducerRaw, orderRegistry);

Console.WriteLine("  Processing orders:");
var orders = new[]
{
    """{"orderId": "ORD-001", "customerId": 100, "items": [{"sku": "ITEM-A", "quantity": 2}], "total": 49.99}""",
    """{"orderId": "ORD-002", "customerId": 101, "items": [{"sku": "ITEM-B", "quantity": 1}, {"sku": "ITEM-C", "quantity": 3}], "total": 129.99}""",
    """{"orderId": "ORD-003", "customerId": 102, "items": [], "total": 0}""" // Valid but empty
};

foreach (var order in orders)
{
    var result = await orderProducer.ProduceAsync("orders", "order-key", order);
    var orderId = JsonDocument.Parse(order).RootElement.GetProperty("orderId").GetString();
    Console.WriteLine($"    {orderId}: {(result ? "Produced" : "Rejected")}");
}
Console.WriteLine();

// Summary
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Schema-aware producers validate before sending");
Console.WriteLine("2. Schema-aware consumers validate after receiving");
Console.WriteLine("3. Cache validators for performance");
Console.WriteLine("4. Support multiple schema versions gracefully");
Console.WriteLine("5. Route invalid messages to dead-letter queues");
Console.WriteLine("6. Schema evolution enables safe system updates");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Message Types ==============

public record ConsumedMessage(
    string Topic,
    string? Key,
    string Value,
    int SchemaId,
    bool IsValid,
    List<string> ValidationErrors
);

public record SchemaVersion(
    int Id,
    int Version,
    string Subject,
    string Schema,
    DateTime RegisteredAt,
    string? Description
);

// ============== Schema Registry ==============

public class SchemaRegistry
{
    private readonly Dictionary<int, SchemaVersion> _schemasById = new();
    private readonly Dictionary<string, List<SchemaVersion>> _subjectVersions = new();
    private int _nextId = 1;
    private readonly object _lock = new();

    public SchemaVersion RegisterSchema(string subject, string schema, string? description = null)
    {
        lock (_lock)
        {
            if (!_subjectVersions.ContainsKey(subject))
                _subjectVersions[subject] = new List<SchemaVersion>();

            var versions = _subjectVersions[subject];

            var version = new SchemaVersion(
                Id: _nextId++,
                Version: versions.Count + 1,
                Subject: subject,
                Schema: schema,
                RegisteredAt: DateTime.UtcNow,
                Description: description
            );

            _schemasById[version.Id] = version;
            versions.Add(version);

            return version;
        }
    }

    public string? GetSchema(int id)
    {
        lock (_lock)
        {
            return _schemasById.TryGetValue(id, out var version) ? version.Schema : null;
        }
    }

    public SchemaVersion? GetSchemaById(int id)
    {
        lock (_lock)
        {
            return _schemasById.TryGetValue(id, out var version) ? version : null;
        }
    }

    public (int Id, string Schema)? GetLatestSchema(string subject)
    {
        lock (_lock)
        {
            if (!_subjectVersions.TryGetValue(subject, out var versions) || versions.Count == 0)
                return null;

            var latest = versions[^1];
            return (latest.Id, latest.Schema);
        }
    }
}

// ============== JSON Schema Validator ==============

public record ValidationResult(bool IsValid, List<string> Errors)
{
    public void ThrowIfInvalid()
    {
        if (!IsValid)
            throw new Exception($"Validation failed: {string.Join("; ", Errors)}");
    }
}

public class JsonSchemaValidator
{
    private readonly JsonElement _schema;

    public JsonSchemaValidator(string schemaJson)
    {
        _schema = JsonDocument.Parse(schemaJson).RootElement;
    }

    public ValidationResult Validate(string json)
    {
        try
        {
            var data = JsonDocument.Parse(json).RootElement;
            var errors = new List<string>();
            ValidateElement(data, _schema, "$", errors);
            return new ValidationResult(errors.Count == 0, errors);
        }
        catch (JsonException ex)
        {
            return new ValidationResult(false, new List<string> { $"Invalid JSON: {ex.Message}" });
        }
    }

    private void ValidateElement(JsonElement data, JsonElement schema, string path, List<string> errors)
    {
        if (schema.TryGetProperty("type", out var typeElement))
        {
            var expectedType = typeElement.GetString();
            if (!MatchesType(data, expectedType))
            {
                errors.Add($"{path}: expected {expectedType}");
                return;
            }
        }

        if (data.ValueKind == JsonValueKind.Object)
        {
            if (schema.TryGetProperty("required", out var required))
            {
                foreach (var req in required.EnumerateArray())
                {
                    var propName = req.GetString()!;
                    if (!data.TryGetProperty(propName, out _))
                    {
                        errors.Add($"{path}: missing required '{propName}'");
                    }
                }
            }

            if (schema.TryGetProperty("properties", out var properties))
            {
                foreach (var prop in data.EnumerateObject())
                {
                    if (properties.TryGetProperty(prop.Name, out var propSchema))
                    {
                        ValidateElement(prop.Value, propSchema, $"{path}.{prop.Name}", errors);
                    }
                }
            }
        }

        if (data.ValueKind == JsonValueKind.Array && schema.TryGetProperty("items", out var itemSchema))
        {
            var index = 0;
            foreach (var item in data.EnumerateArray())
            {
                ValidateElement(item, itemSchema, $"{path}[{index}]", errors);
                index++;
            }
        }
    }

    private static bool MatchesType(JsonElement data, string? expectedType)
    {
        return expectedType switch
        {
            "string" => data.ValueKind == JsonValueKind.String,
            "integer" => data.ValueKind == JsonValueKind.Number && data.TryGetInt64(out _),
            "number" => data.ValueKind == JsonValueKind.Number,
            "boolean" => data.ValueKind == JsonValueKind.True || data.ValueKind == JsonValueKind.False,
            "array" => data.ValueKind == JsonValueKind.Array,
            "object" => data.ValueKind == JsonValueKind.Object,
            "null" => data.ValueKind == JsonValueKind.Null,
            _ => true
        };
    }
}

// ============== Schema-Aware Producer ==============

public class SchemaAwareProducer
{
    private readonly SurgewaveProducer<byte[], byte[]> _producer;
    private readonly SchemaRegistry _registry;
    private readonly Dictionary<string, (int Id, JsonSchemaValidator Validator)> _cache = new();

    public SchemaAwareProducer(SurgewaveProducer<byte[], byte[]> producer, SchemaRegistry registry)
    {
        _producer = producer;
        _registry = registry;
    }

    public void ClearCache() => _cache.Clear();

    public async Task<bool> ProduceAsync(string topic, string key, string jsonValue)
    {
        var subject = $"{topic}-value";

        if (!_cache.TryGetValue(subject, out var cached))
        {
            var schema = _registry.GetLatestSchema(subject);
            if (schema == null)
            {
                Console.WriteLine($"      No schema registered for {subject}");
                return false;
            }

            cached = (schema.Value.Id, new JsonSchemaValidator(schema.Value.Schema));
            _cache[subject] = cached;
        }

        var result = cached.Validator.Validate(jsonValue);
        if (!result.IsValid)
        {
            Console.WriteLine($"      Validation: {string.Join(", ", result.Errors)}");
            return false;
        }

        var payload = Encoding.UTF8.GetBytes(jsonValue);
        var message = SerializeWithSchema(cached.Id, payload);
        await _producer.ProduceAsync(topic, Encoding.UTF8.GetBytes(key), message);
        return true;
    }

    private static byte[] SerializeWithSchema(int schemaId, byte[] payload)
    {
        var result = new byte[4 + payload.Length];
        BitConverter.TryWriteBytes(result.AsSpan(0, 4), schemaId);
        payload.CopyTo(result, 4);
        return result;
    }
}

// ============== Schema-Aware Consumer ==============

public class SchemaAwareConsumer
{
    private readonly SurgewaveConsumer<byte[], byte[]> _consumer;
    private readonly SchemaRegistry _registry;
    private readonly Dictionary<int, JsonSchemaValidator> _validators = new();

    public SchemaAwareConsumer(SurgewaveConsumer<byte[], byte[]> consumer, SchemaRegistry registry)
    {
        _consumer = consumer;
        _registry = registry;
    }

    public async Task<ConsumedMessage?> ConsumeAsync(TimeSpan timeout)
    {
        var result = await _consumer.ConsumeAsync(timeout);
        if (result?.Value == null) return null;

        try
        {
            var (schemaId, payload) = DeserializeWithSchema(result.Value);

            if (!_validators.TryGetValue(schemaId, out var validator))
            {
                var schema = _registry.GetSchema(schemaId);
                if (schema == null)
                {
                    return new ConsumedMessage(
                        result.Topic,
                        result.Key != null ? Encoding.UTF8.GetString(result.Key) : null,
                        Encoding.UTF8.GetString(payload),
                        schemaId,
                        false,
                        new List<string> { $"Unknown schema ID: {schemaId}" }
                    );
                }

                validator = new JsonSchemaValidator(schema);
                _validators[schemaId] = validator;
            }

            var json = Encoding.UTF8.GetString(payload);
            var validationResult = validator.Validate(json);

            return new ConsumedMessage(
                result.Topic,
                result.Key != null ? Encoding.UTF8.GetString(result.Key) : null,
                json,
                schemaId,
                validationResult.IsValid,
                validationResult.Errors
            );
        }
        catch (Exception ex)
        {
            return new ConsumedMessage(
                result.Topic,
                result.Key != null ? Encoding.UTF8.GetString(result.Key) : null,
                Encoding.UTF8.GetString(result.Value),
                0,
                false,
                new List<string> { $"Parse error: {ex.Message}" }
            );
        }
    }

    private static (int SchemaId, byte[] Payload) DeserializeWithSchema(byte[] data)
    {
        if (data.Length < 4)
            throw new Exception("Message too short for schema ID");

        var schemaId = BitConverter.ToInt32(data, 0);
        var payload = data[4..];
        return (schemaId, payload);
    }
}

// ============== Message Processor with DLQ ==============

public class MessageProcessor
{
    private readonly SchemaAwareConsumer _consumer;
    private readonly SurgewaveProducer<byte[], byte[]> _dlqProducer;

    public MessageProcessor(SchemaAwareConsumer consumer, SurgewaveProducer<byte[], byte[]> dlqProducer)
    {
        _consumer = consumer;
        _dlqProducer = dlqProducer;
    }

    public async Task ProcessWithDeadLetterAsync(string sourceTopic, string dlqTopic)
    {
        var message = await _consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
        if (message == null) return;

        if (!message.IsValid)
        {
            var dlqPayload = JsonSerializer.Serialize(new
            {
                originalTopic = sourceTopic,
                key = message.Key,
                errors = message.ValidationErrors,
                originalValue = message.Value,
                timestamp = DateTime.UtcNow
            });

            await _dlqProducer.ProduceAsync(dlqTopic,
                Encoding.UTF8.GetBytes(message.Key ?? "unknown"),
                Encoding.UTF8.GetBytes(dlqPayload));

            Console.WriteLine($"    Sent invalid message to DLQ: {message.Key}");
            return;
        }

        // Process valid message
        Console.WriteLine($"    Processed valid message: {message.Key}");
    }
}
