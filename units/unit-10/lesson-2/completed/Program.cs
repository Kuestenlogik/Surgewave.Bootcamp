// Surgewave Bootcamp - Unit 10, Lesson 2: JSON Schema and Avro Serialization
// COMPLETED SOLUTION

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
// using Kuestenlogik.Surgewave.Client.SchemaRegistry; // TODO: SchemaRegistry package may not be referenced
// using Kuestenlogik.Surgewave.Client.SchemaRegistry.Avro; // TODO: SchemaRegistry package may not be referenced

Console.WriteLine("=== Surgewave Bootcamp: Lesson 10.2 - JSON Schema and Avro Serialization ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// ============== Demo 1: Basic Schema Validation ==============
Console.WriteLine("--- Demo 1: Basic Schema Validation ---");

var userEventSchema = """
{
    "type": "object",
    "properties": {
        "userId": { "type": "integer" },
        "action": { "type": "string" },
        "timestamp": { "type": "string" }
    },
    "required": ["userId", "action", "timestamp"]
}
""";

var validator = new JsonSchemaValidator(userEventSchema);

// Valid message
var validJson = """{"userId": 123, "action": "login", "timestamp": "2024-01-15T10:30:00Z"}""";
var validResult = validator.Validate(validJson);
Console.WriteLine($"  Valid message: IsValid={validResult.IsValid}");

// Missing required field
var missingFieldJson = """{"userId": 123, "action": "login"}""";
var missingResult = validator.Validate(missingFieldJson);
Console.WriteLine($"  Missing field: IsValid={missingResult.IsValid}, Errors={string.Join(", ", missingResult.Errors)}");

// Wrong type
var wrongTypeJson = """{"userId": "not-a-number", "action": "login", "timestamp": "2024-01-15"}""";
var wrongTypeResult = validator.Validate(wrongTypeJson);
Console.WriteLine($"  Wrong type: IsValid={wrongTypeResult.IsValid}, Errors={string.Join(", ", wrongTypeResult.Errors)}");
Console.WriteLine();

// ============== Demo 2: Enum Validation ==============
Console.WriteLine("--- Demo 2: Enum Validation ---");

var enumSchema = """
{
    "type": "object",
    "properties": {
        "status": {
            "type": "string",
            "enum": ["pending", "active", "completed", "cancelled"]
        },
        "priority": {
            "type": "integer",
            "enum": [1, 2, 3]
        }
    },
    "required": ["status"]
}
""";

var enumValidator = new JsonSchemaValidator(enumSchema);

var validEnum = """{"status": "active", "priority": 2}""";
var enumValid = enumValidator.Validate(validEnum);
Console.WriteLine($"  Valid enum: IsValid={enumValid.IsValid}");

var invalidEnum = """{"status": "unknown"}""";
var enumInvalid = enumValidator.Validate(invalidEnum);
Console.WriteLine($"  Invalid enum: IsValid={enumInvalid.IsValid}, Errors={string.Join(", ", enumInvalid.Errors)}");
Console.WriteLine();

// ============== Demo 3: Nested Objects ==============
Console.WriteLine("--- Demo 3: Nested Objects ---");

var orderSchema = """
{
    "type": "object",
    "properties": {
        "orderId": { "type": "string" },
        "customer": {
            "type": "object",
            "properties": {
                "id": { "type": "integer" },
                "email": { "type": "string" }
            },
            "required": ["id"]
        },
        "total": { "type": "number" }
    },
    "required": ["orderId", "customer"]
}
""";

var orderValidator = new JsonSchemaValidator(orderSchema);

var validOrder = """
{
    "orderId": "ORD-001",
    "customer": { "id": 42, "email": "user@example.com" },
    "total": 99.99
}
""";
var orderResult = orderValidator.Validate(validOrder);
Console.WriteLine($"  Valid order: IsValid={orderResult.IsValid}");

var invalidOrder = """
{
    "orderId": "ORD-002",
    "customer": { "email": "missing-id@example.com" }
}
""";
var invalidOrderResult = orderValidator.Validate(invalidOrder);
Console.WriteLine($"  Missing nested required: IsValid={invalidOrderResult.IsValid}");
Console.WriteLine($"    Errors: {string.Join(", ", invalidOrderResult.Errors)}");
Console.WriteLine();

// ============== Demo 4: Array Validation ==============
Console.WriteLine("--- Demo 4: Array Validation ---");

var arraySchema = """
{
    "type": "object",
    "properties": {
        "tags": {
            "type": "array",
            "items": { "type": "string" },
            "minItems": 1
        },
        "scores": {
            "type": "array",
            "items": { "type": "integer" }
        }
    },
    "required": ["tags"]
}
""";

var arrayValidator = new JsonSchemaValidator(arraySchema);

var validArray = """{"tags": ["important", "urgent"], "scores": [85, 90, 95]}""";
var arrayResult = arrayValidator.Validate(validArray);
Console.WriteLine($"  Valid arrays: IsValid={arrayResult.IsValid}");

var emptyArray = """{"tags": []}""";
var emptyResult = arrayValidator.Validate(emptyArray);
Console.WriteLine($"  Empty array (minItems=1): IsValid={emptyResult.IsValid}, Errors={string.Join(", ", emptyResult.Errors)}");

var wrongItemType = """{"tags": ["valid", 123, "also-valid"]}""";
var wrongItemResult = arrayValidator.Validate(wrongItemType);
Console.WriteLine($"  Wrong item type: IsValid={wrongItemResult.IsValid}, Errors={string.Join(", ", wrongItemResult.Errors)}");
Console.WriteLine();

// ============== Demo 5: Numeric Constraints ==============
Console.WriteLine("--- Demo 5: Numeric Constraints ---");

var numericSchema = """
{
    "type": "object",
    "properties": {
        "age": {
            "type": "integer",
            "minimum": 0,
            "maximum": 150
        },
        "score": {
            "type": "number",
            "minimum": 0,
            "maximum": 100
        }
    },
    "required": ["age"]
}
""";

var numericValidator = new JsonSchemaValidator(numericSchema);

var validNumeric = """{"age": 25, "score": 85.5}""";
var numericResult = numericValidator.Validate(validNumeric);
Console.WriteLine($"  Valid numeric: IsValid={numericResult.IsValid}");

var outOfRange = """{"age": -5}""";
var rangeResult = numericValidator.Validate(outOfRange);
Console.WriteLine($"  Out of range: IsValid={rangeResult.IsValid}, Errors={string.Join(", ", rangeResult.Errors)}");
Console.WriteLine();

// ============== Demo 6: Validated Producer ==============
Console.WriteLine("--- Demo 6: Validated Producer ---");

var registry = new InMemorySchemaRegistry();
var schemaId = registry.RegisterSchema("events-value", userEventSchema);

await using var producer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var validatedProducer = new ValidatedProducer(producer, registry);

// Produce valid message
var validEvent = """{"userId": 1, "action": "purchase", "timestamp": "2024-01-15T12:00:00Z"}""";
var produceResult = await validatedProducer.ProduceValidatedAsync("events", "user-1", validEvent);
Console.WriteLine($"  Valid produce: Success={produceResult}");

// Try to produce invalid message
var invalidEvent = """{"userId": "not-an-int", "action": "view"}""";
var invalidProduceResult = await validatedProducer.ProduceValidatedAsync("events", "user-2", invalidEvent);
Console.WriteLine($"  Invalid produce: Success={invalidProduceResult}");
Console.WriteLine();

// TODO: SchemaRegistry package may not be referenced - Avro demo commented out
// ============== Demo 7: Avro Serialization with Schema Registry ==============
// Console.WriteLine("--- Demo 7: Avro Serialization with Schema Registry ---");
//
// var schemaRegistryOps = new MockSchemaRegistryOperations();
//
// var avroConfig = new AvroSerializerConfig(schemaRegistryOps)
// {
//     AutoRegisterSchemas = true,
//     SubjectNameStrategy = TopicNameStrategy.Instance
// };
//
// var avroSerializer = new SchemaRegistryAvroSerializer<OrderEvent>(avroConfig);
// var avroDeserializer = new SchemaRegistryAvroDeserializer<OrderEvent>(avroConfig);
//
// Console.WriteLine("  Created Avro serializer with auto-registration enabled");
//
// await using var avroProducer = new SurgewaveProducer<string, OrderEvent>(options =>
// {
//     options.BootstrapServers = $"localhost:{runtime.Port}";
//     options.AsyncValueSerializer = avroSerializer;
// });
//
// var orderEvents = new[]
// {
//     new OrderEvent { OrderId = "ORD-001", CustomerId = 1001, Amount = 99.99m, Currency = "USD", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
//     new OrderEvent { OrderId = "ORD-002", CustomerId = 1002, Amount = 149.50m, Currency = "EUR", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
//     new OrderEvent { OrderId = "ORD-003", CustomerId = 1003, Amount = 250.00m, Currency = "GBP", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
// };
//
// foreach (var orderEvent in orderEvents)
// {
//     var result = await avroProducer.ProduceAsync("order-events", orderEvent.OrderId, orderEvent);
//     Console.WriteLine($"  Produced Avro message: {orderEvent.OrderId} to offset {result.Offset}");
// }
//
// await using var avroConsumer = new SurgewaveConsumer<string, OrderEvent>(options =>
// {
//     options.BootstrapServers = $"localhost:{runtime.Port}";
//     options.AsyncValueDeserializer = avroDeserializer;
//     options.AutoOffsetReset = AutoOffsetReset.Earliest;
// });
//
// avroConsumer.Subscribe("order-events");
//
// Console.WriteLine("  Consuming Avro messages...");
// for (int i = 0; i < 3; i++)
// {
//     var consumeResult = await avroConsumer.ConsumeAsync();
//     if (consumeResult != null)
//     {
//         Console.WriteLine($"    Consumed: OrderId={consumeResult.Value.OrderId}, Amount={consumeResult.Value.Amount} {consumeResult.Value.Currency}");
//     }
// }
Console.WriteLine();

// Summary
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. JSON Schema defines structure, types, and constraints");
Console.WriteLine("2. Required fields ensure data completeness");
Console.WriteLine("3. Enum restricts values to a predefined set");
Console.WriteLine("4. Nested objects and arrays need recursive validation");
Console.WriteLine("5. Validate at produce time to prevent bad data");
Console.WriteLine("6. Avro provides compact binary serialization with schema evolution");
Console.WriteLine("7. Use SchemaRegistryAvroSerializer for automatic schema registration");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Validation Classes ==============

public record ValidationResult(bool IsValid, List<string> Errors)
{
    public void ThrowIfInvalid()
    {
        if (!IsValid)
            throw new SchemaValidationException(Errors);
    }
}

public class SchemaValidationException : Exception
{
    public List<string> Errors { get; }

    public SchemaValidationException() : base("Schema validation failed")
    {
        Errors = new List<string>();
    }

    public SchemaValidationException(string message) : base(message)
    {
        Errors = new List<string>();
    }

    public SchemaValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = new List<string>();
    }

    public SchemaValidationException(List<string> errors)
        : base($"Schema validation failed: {string.Join("; ", errors)}")
    {
        Errors = errors;
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
        // Check type
        if (schema.TryGetProperty("type", out var typeElement))
        {
            var expectedType = typeElement.GetString();
            if (!MatchesType(data, expectedType))
            {
                errors.Add($"{path}: expected type '{expectedType}', got '{data.ValueKind}'");
                return;
            }
        }

        // Check enum
        if (schema.TryGetProperty("enum", out var enumValues))
        {
            if (!IsInEnum(data, enumValues))
            {
                errors.Add($"{path}: value not in enum");
            }
        }

        // Check numeric constraints
        ValidateNumericConstraints(data, schema, path, errors);

        // Check object properties
        if (data.ValueKind == JsonValueKind.Object)
        {
            ValidateObject(data, schema, path, errors);
        }

        // Check array items
        if (data.ValueKind == JsonValueKind.Array)
        {
            ValidateArray(data, schema, path, errors);
        }
    }

    private void ValidateObject(JsonElement data, JsonElement schema, string path, List<string> errors)
    {
        // Check required properties
        if (schema.TryGetProperty("required", out var required))
        {
            foreach (var req in required.EnumerateArray())
            {
                var propName = req.GetString()!;
                if (!data.TryGetProperty(propName, out _))
                {
                    errors.Add($"{path}: missing required property '{propName}'");
                }
            }
        }

        // Validate each property against its schema
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

    private void ValidateArray(JsonElement data, JsonElement schema, string path, List<string> errors)
    {
        var count = data.GetArrayLength();

        // Check minItems
        if (schema.TryGetProperty("minItems", out var minItems))
        {
            if (count < minItems.GetInt32())
            {
                errors.Add($"{path}: array has {count} items, minimum is {minItems.GetInt32()}");
            }
        }

        // Check maxItems
        if (schema.TryGetProperty("maxItems", out var maxItems))
        {
            if (count > maxItems.GetInt32())
            {
                errors.Add($"{path}: array has {count} items, maximum is {maxItems.GetInt32()}");
            }
        }

        // Validate items
        if (schema.TryGetProperty("items", out var itemSchema))
        {
            var index = 0;
            foreach (var item in data.EnumerateArray())
            {
                ValidateElement(item, itemSchema, $"{path}[{index}]", errors);
                index++;
            }
        }
    }

    private void ValidateNumericConstraints(JsonElement data, JsonElement schema, string path, List<string> errors)
    {
        if (data.ValueKind != JsonValueKind.Number)
            return;

        var value = data.GetDouble();

        if (schema.TryGetProperty("minimum", out var min))
        {
            if (value < min.GetDouble())
            {
                errors.Add($"{path}: value {value} is less than minimum {min.GetDouble()}");
            }
        }

        if (schema.TryGetProperty("maximum", out var max))
        {
            if (value > max.GetDouble())
            {
                errors.Add($"{path}: value {value} is greater than maximum {max.GetDouble()}");
            }
        }
    }

    private static bool MatchesType(JsonElement data, string? expectedType)
    {
        return expectedType switch
        {
            "string" => data.ValueKind == JsonValueKind.String,
            "integer" => data.ValueKind == JsonValueKind.Number && IsInteger(data),
            "number" => data.ValueKind == JsonValueKind.Number,
            "boolean" => data.ValueKind == JsonValueKind.True || data.ValueKind == JsonValueKind.False,
            "array" => data.ValueKind == JsonValueKind.Array,
            "object" => data.ValueKind == JsonValueKind.Object,
            "null" => data.ValueKind == JsonValueKind.Null,
            _ => true
        };
    }

    private static bool IsInteger(JsonElement data)
    {
        if (data.TryGetInt64(out _)) return true;
        var d = data.GetDouble();
        return Math.Abs(d - Math.Floor(d)) < 0.0001;
    }

    private static bool IsInEnum(JsonElement data, JsonElement enumValues)
    {
        foreach (var enumValue in enumValues.EnumerateArray())
        {
            if (ElementsEqual(data, enumValue))
                return true;
        }
        return false;
    }

    private static bool ElementsEqual(JsonElement a, JsonElement b)
    {
        if (a.ValueKind != b.ValueKind) return false;

        return a.ValueKind switch
        {
            JsonValueKind.String => a.GetString() == b.GetString(),
            JsonValueKind.Number => a.GetDouble() == b.GetDouble(),
            JsonValueKind.True or JsonValueKind.False => true,
            JsonValueKind.Null => true,
            _ => false
        };
    }
}

// ============== Schema Registry ==============

public interface ISchemaRegistry
{
    int RegisterSchema(string subject, string schema);
    string? GetSchema(int id);
    (int Id, string Schema)? GetLatestSchema(string subject);
}

public class InMemorySchemaRegistry : ISchemaRegistry
{
    private readonly Dictionary<int, string> _schemasById = new();
    private readonly Dictionary<string, List<int>> _subjectVersions = new();
    private int _nextId = 1;

    public int RegisterSchema(string subject, string schema)
    {
        var id = _nextId++;
        _schemasById[id] = schema;

        if (!_subjectVersions.ContainsKey(subject))
            _subjectVersions[subject] = new List<int>();

        _subjectVersions[subject].Add(id);
        return id;
    }

    public string? GetSchema(int id)
    {
        return _schemasById.TryGetValue(id, out var schema) ? schema : null;
    }

    public (int Id, string Schema)? GetLatestSchema(string subject)
    {
        if (!_subjectVersions.TryGetValue(subject, out var versions) || versions.Count == 0)
            return null;

        var latestId = versions[^1];
        return (latestId, _schemasById[latestId]);
    }
}

// ============== Validated Producer ==============

public class ValidatedProducer
{
    private readonly SurgewaveProducer<byte[], byte[]> _producer;
    private readonly ISchemaRegistry _registry;
    private readonly Dictionary<string, JsonSchemaValidator> _validators = new();

    public ValidatedProducer(SurgewaveProducer<byte[], byte[]> producer, ISchemaRegistry registry)
    {
        _producer = producer;
        _registry = registry;
    }

    public async Task<bool> ProduceValidatedAsync(string topic, string key, string jsonMessage)
    {
        var subject = $"{topic}-value";
        var schemaInfo = _registry.GetLatestSchema(subject);

        if (schemaInfo == null)
        {
            Console.WriteLine($"    No schema registered for {subject}");
            return false;
        }

        if (!_validators.TryGetValue(subject, out var validator))
        {
            validator = new JsonSchemaValidator(schemaInfo.Value.Schema);
            _validators[subject] = validator;
        }

        var result = validator.Validate(jsonMessage);
        if (!result.IsValid)
        {
            Console.WriteLine($"    Validation failed: {string.Join(", ", result.Errors)}");
            return false;
        }

        var payload = Encoding.UTF8.GetBytes(jsonMessage);
        var messageWithSchema = SerializeWithSchema(schemaInfo.Value.Id, payload);
        await _producer.ProduceAsync(topic, Encoding.UTF8.GetBytes(key), messageWithSchema);
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

// ============== Avro Domain Classes ==============

/// <summary>
/// Order event for Avro serialization demonstration.
/// </summary>
public class OrderEvent
{
    public string OrderId { get; set; } = "";
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public long Timestamp { get; set; }
}

// ============== Mock Schema Registry Operations ==============

/// <summary>
/// Mock implementation of ISchemaRegistryOperations for demonstration.
/// In production, use a real schema registry client.
/// </summary>
public class MockSchemaRegistryOperations : Kuestenlogik.Surgewave.Client.Native.Operations.Schema.ISchemaRegistryOperations
{
    private readonly Dictionary<int, SchemaRegistryInfo> _schemasById = new();
    private readonly Dictionary<string, List<(int Version, int Id, string Schema, string SchemaType)>> _subjects = new();
    private int _nextId = 1;

    public Task<Kuestenlogik.Surgewave.Client.Native.Operations.Schema.SchemaRegistrationResult> RegisterSchemaAsync(
        string subject, string schemaString, string schemaType, CancellationToken cancellationToken = default)
    {
        if (!_subjects.ContainsKey(subject))
            _subjects[subject] = new();

        // Check if schema already exists
        var existing = _subjects[subject].FirstOrDefault(s => s.Schema == schemaString);
        if (existing.Schema != null)
        {
            return Task.FromResult(new Kuestenlogik.Surgewave.Client.Native.Operations.Schema.SchemaRegistrationResult(existing.Id, existing.Version));
        }

        var id = _nextId++;
        var version = _subjects[subject].Count + 1;
        _subjects[subject].Add((version, id, schemaString, schemaType));
        _schemasById[id] = new SchemaRegistryInfo(id, subject, version, schemaString, schemaType);

        return Task.FromResult(new Kuestenlogik.Surgewave.Client.Native.Operations.Schema.SchemaRegistrationResult(id, version));
    }

    public Task<Kuestenlogik.Surgewave.Client.Native.Operations.Schema.SchemaInfo?> GetSchemaByIdAsync(
        int schemaId, CancellationToken cancellationToken = default)
    {
        if (_schemasById.TryGetValue(schemaId, out var info))
        {
            return Task.FromResult<Kuestenlogik.Surgewave.Client.Native.Operations.Schema.SchemaInfo?>(
                new Kuestenlogik.Surgewave.Client.Native.Operations.Schema.SchemaInfo
                {
                    Id = info.Id,
                    Subject = info.Subject,
                    Version = info.Version,
                    SchemaString = info.Schema,
                    SchemaType = info.SchemaType
                });
        }
        return Task.FromResult<Kuestenlogik.Surgewave.Client.Native.Operations.Schema.SchemaInfo?>(null);
    }

    public Task<Kuestenlogik.Surgewave.Client.Native.Operations.Schema.SchemaInfo?> GetSchemaByVersionAsync(
        string subject, int version, CancellationToken cancellationToken = default)
    {
        if (_subjects.TryGetValue(subject, out var versions))
        {
            var found = versions.FirstOrDefault(v => v.Version == version);
            if (found.Schema != null)
            {
                return Task.FromResult<Kuestenlogik.Surgewave.Client.Native.Operations.Schema.SchemaInfo?>(
                    new Kuestenlogik.Surgewave.Client.Native.Operations.Schema.SchemaInfo
                    {
                        Id = found.Id,
                        Subject = subject,
                        Version = found.Version,
                        SchemaString = found.Schema,
                        SchemaType = found.SchemaType
                    });
            }
        }
        return Task.FromResult<Kuestenlogik.Surgewave.Client.Native.Operations.Schema.SchemaInfo?>(null);
    }

    public Task<IReadOnlyList<int>> GetSubjectVersionsAsync(
        string subject, CancellationToken cancellationToken = default)
    {
        if (_subjects.TryGetValue(subject, out var versions))
        {
            return Task.FromResult<IReadOnlyList<int>>(versions.Select(v => v.Version).ToList());
        }
        return Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
    }

    private sealed record SchemaRegistryInfo(int Id, string Subject, int Version, string Schema, string SchemaType);
}
