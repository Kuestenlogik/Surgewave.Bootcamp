// Surgewave Bootcamp - Unit 10, Lesson 1: Schema Basics
// COMPLETED SOLUTION

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 10.1 - Schema Basics ===");
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

// ============== Demo 1: Schema Registry Basics ==============
Console.WriteLine("--- Demo 1: Schema Registry Basics ---");

var registry = new InMemorySchemaRegistry();

// Register a schema for user-events topic
var schemaV1 = """
{
    "type": "object",
    "properties": {
        "userId": { "type": "integer" },
        "action": { "type": "string" },
        "timestamp": { "type": "string", "format": "date-time" }
    },
    "required": ["userId", "action"]
}
""";

var schemaId1 = registry.RegisterSchema("user-events-value", schemaV1);
Console.WriteLine($"  Registered schema v1 with ID: {schemaId1}");

// Retrieve by ID
var retrievedSchema = registry.GetSchema(schemaId1);
Console.WriteLine($"  Retrieved schema by ID {schemaId1}: {(retrievedSchema != null ? "found" : "not found")}");
Console.WriteLine();

// ============== Demo 2: Schema Versioning ==============
Console.WriteLine("--- Demo 2: Schema Versioning ---");

// Register a second version (add email field)
var schemaV2 = """
{
    "type": "object",
    "properties": {
        "userId": { "type": "integer" },
        "action": { "type": "string" },
        "timestamp": { "type": "string", "format": "date-time" },
        "email": { "type": "string", "format": "email" }
    },
    "required": ["userId", "action"]
}
""";

var schemaId2 = registry.RegisterSchema("user-events-value", schemaV2);
Console.WriteLine($"  Registered schema v2 with ID: {schemaId2}");

// List all versions
Console.WriteLine("  All versions for 'user-events-value':");
foreach (var versionId in registry.GetVersions("user-events-value"))
{
    Console.WriteLine($"    - Schema ID: {versionId}");
}

// Get latest
var latest = registry.GetLatestSchema("user-events-value");
Console.WriteLine($"  Latest schema ID: {latest?.Id}");
Console.WriteLine();

// ============== Demo 3: Subject Naming Convention ==============
Console.WriteLine("--- Demo 3: Subject Naming Convention ---");

// Register schemas for different topics
var orderSchemaId = registry.RegisterSchema("orders-value", """
{
    "type": "object",
    "properties": {
        "orderId": { "type": "string" },
        "amount": { "type": "number" },
        "currency": { "type": "string" }
    },
    "required": ["orderId", "amount"]
}
""");

var orderKeySchemaId = registry.RegisterSchema("orders-key", """
{
    "type": "string",
    "description": "Order ID as key"
}
""");

Console.WriteLine($"  orders-value schema ID: {orderSchemaId}");
Console.WriteLine($"  orders-key schema ID: {orderKeySchemaId}");
Console.WriteLine("  Convention: <topic>-value, <topic>-key");
Console.WriteLine();

// ============== Demo 4: Schema ID in Messages ==============
Console.WriteLine("--- Demo 4: Schema ID in Messages ---");

// Create a message payload
var userEvent = new { userId = 123, action = "login", timestamp = DateTime.UtcNow.ToString("O") };
var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(userEvent));

Console.WriteLine($"  Original payload size: {payload.Length} bytes");

// Serialize with schema ID prefix
var messageWithSchema = SerializeWithSchema(schemaId1, payload);
Console.WriteLine($"  Message with schema ID: {messageWithSchema.Length} bytes (4-byte ID prefix)");

// Deserialize and extract schema ID
var (extractedId, extractedPayload) = DeserializeWithSchema(messageWithSchema);
Console.WriteLine($"  Extracted schema ID: {extractedId}");
Console.WriteLine($"  Extracted payload: {Encoding.UTF8.GetString(extractedPayload)}");
Console.WriteLine();

// ============== Demo 5: Using with Surgewave ==============
Console.WriteLine("--- Demo 5: Using with Surgewave ---");

await using var producer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

await using var consumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "schema-demo-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe("user-events");

// Produce messages with schema ID
var events = new[]
{
    new { userId = 1, action = "signup", timestamp = DateTime.UtcNow.ToString("O") },
    new { userId = 2, action = "login", timestamp = DateTime.UtcNow.ToString("O") },
    new { userId = 1, action = "purchase", timestamp = DateTime.UtcNow.ToString("O") }
};

foreach (var evt in events)
{
    var eventPayload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));
    var messageBytes = SerializeWithSchema(schemaId1, eventPayload);
    await producer.ProduceAsync("user-events", Encoding.UTF8.GetBytes($"user-{evt.userId}"), messageBytes);
    Console.WriteLine($"  Produced: user-{evt.userId} -> {evt.action}");
}

Console.WriteLine();
Console.WriteLine("  Consuming messages with schema lookup:");

var consumed = 0;
var timeout = DateTime.UtcNow.AddSeconds(2);
while (consumed < events.Length && DateTime.UtcNow < timeout)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
    if (result?.Value != null)
    {
        var (msgSchemaId, msgPayload) = DeserializeWithSchema(result.Value);
        var schema = registry.GetSchema(msgSchemaId);
        var data = JsonSerializer.Deserialize<JsonElement>(msgPayload);
        Console.WriteLine($"    Schema ID: {msgSchemaId}, Action: {data.GetProperty("action")}");
        consumed++;
    }
}
Console.WriteLine();

// Summary
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Schema Registry stores and manages schemas centrally");
Console.WriteLine("2. Schemas are organized by subject (e.g., topic-value, topic-key)");
Console.WriteLine("3. Each registered schema gets a unique ID");
Console.WriteLine("4. Schema ID is prepended to messages (4 bytes)");
Console.WriteLine("5. Consumers can look up schemas by ID for validation");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Helper Methods ==============

static byte[] SerializeWithSchema(int schemaId, byte[] payload)
{
    var result = new byte[4 + payload.Length];
    BitConverter.TryWriteBytes(result.AsSpan(0, 4), schemaId);
    payload.CopyTo(result, 4);
    return result;
}

static (int SchemaId, byte[] Payload) DeserializeWithSchema(byte[] data)
{
    var schemaId = BitConverter.ToInt32(data, 0);
    var payload = data[4..];
    return (schemaId, payload);
}

// ============== Schema Registry Implementation ==============

public interface ISchemaRegistry
{
    int RegisterSchema(string subject, string schema);
    string? GetSchema(int id);
    (int Id, string Schema)? GetLatestSchema(string subject);
    IEnumerable<int> GetVersions(string subject);
}

public class InMemorySchemaRegistry : ISchemaRegistry
{
    private readonly Dictionary<int, string> _schemasById = new();
    private readonly Dictionary<string, List<int>> _subjectVersions = new();
    private int _nextId = 1;
    private readonly object _lock = new();

    public int RegisterSchema(string subject, string schema)
    {
        lock (_lock)
        {
            var id = _nextId++;
            _schemasById[id] = schema;

            if (!_subjectVersions.ContainsKey(subject))
                _subjectVersions[subject] = new List<int>();

            _subjectVersions[subject].Add(id);
            return id;
        }
    }

    public string? GetSchema(int id)
    {
        lock (_lock)
        {
            return _schemasById.TryGetValue(id, out var schema) ? schema : null;
        }
    }

    public (int Id, string Schema)? GetLatestSchema(string subject)
    {
        lock (_lock)
        {
            if (!_subjectVersions.TryGetValue(subject, out var versions) || versions.Count == 0)
                return null;

            var latestId = versions[^1];
            return (latestId, _schemasById[latestId]);
        }
    }

    public IEnumerable<int> GetVersions(string subject)
    {
        lock (_lock)
        {
            if (_subjectVersions.TryGetValue(subject, out var versions))
                return versions.ToList();
            return Enumerable.Empty<int>();
        }
    }
}
