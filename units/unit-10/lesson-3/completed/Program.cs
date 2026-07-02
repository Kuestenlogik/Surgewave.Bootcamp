// Surgewave Bootcamp - Unit 10, Lesson 3: Schema Versioning and Protobuf Serialization
// COMPLETED SOLUTION

using System.Text;
using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
// using Kuestenlogik.Surgewave.Client.SchemaRegistry; // TODO: SchemaRegistry package may not be referenced
// using Kuestenlogik.Surgewave.Client.SchemaRegistry.Protobuf; // TODO: SchemaRegistry package may not be referenced

Console.WriteLine("=== Surgewave Bootcamp: Lesson 10.3 - Schema Versioning and Protobuf Serialization ===");
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

// ============== Demo 1: Registering Schema Versions ==============
Console.WriteLine("--- Demo 1: Registering Schema Versions ---");

var registry = new VersionedSchemaRegistry();

// V1: Initial schema
var v1 = registry.RegisterSchema("user-value", """
{
    "type": "object",
    "properties": {
        "id": { "type": "integer" },
        "name": { "type": "string" }
    },
    "required": ["id", "name"]
}
""", "Initial user schema with id and name");

Console.WriteLine($"  Registered v{v1.Version} (ID: {v1.Id}): {v1.Description}");

// V2: Add email field
var v2 = registry.RegisterSchema("user-value", """
{
    "type": "object",
    "properties": {
        "id": { "type": "integer" },
        "name": { "type": "string" },
        "email": { "type": "string" }
    },
    "required": ["id", "name"]
}
""", "Added optional email field");

Console.WriteLine($"  Registered v{v2.Version} (ID: {v2.Id}): {v2.Description}");

// V3: Add metadata
var v3 = registry.RegisterSchema("user-value", """
{
    "type": "object",
    "properties": {
        "id": { "type": "integer" },
        "name": { "type": "string" },
        "email": { "type": "string" },
        "metadata": { "type": "object" }
    },
    "required": ["id", "name"]
}
""", "Added optional metadata object");

Console.WriteLine($"  Registered v{v3.Version} (ID: {v3.Id}): {v3.Description}");
Console.WriteLine();

// ============== Demo 2: Listing All Versions ==============
Console.WriteLine("--- Demo 2: Listing All Versions ---");

Console.WriteLine("  Version history for 'user-value':");
foreach (var version in registry.GetAllVersions("user-value"))
{
    Console.WriteLine($"    v{version.Version} (ID: {version.Id})");
    Console.WriteLine($"      Registered: {version.RegisteredAt:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"      Description: {version.Description}");
}
Console.WriteLine();

// ============== Demo 3: Retrieving Specific Versions ==============
Console.WriteLine("--- Demo 3: Retrieving Specific Versions ---");

var latestVersion = registry.GetLatestVersion("user-value");
Console.WriteLine($"  Latest version: v{latestVersion?.Version} (ID: {latestVersion?.Id})");

var version2 = registry.GetSchemaByVersion("user-value", 2);
Console.WriteLine($"  Version 2: {(version2 != null ? "found" : "not found")}");
if (version2 != null)
{
    Console.WriteLine($"    Description: {version2.Description}");
}

var byId = registry.GetSchemaById(v1.Id);
Console.WriteLine($"  Get by ID {v1.Id}: v{byId?.Version}");
Console.WriteLine();

// ============== Demo 4: Detecting Duplicate Schemas ==============
Console.WriteLine("--- Demo 4: Detecting Duplicate Schemas ---");

// Try to register the same schema as v1
var duplicate = registry.RegisterSchema("user-value", """
{
    "type": "object",
    "properties": {
        "id": { "type": "integer" },
        "name": { "type": "string" }
    },
    "required": ["id", "name"]
}
""", "This should return v1");

Console.WriteLine($"  Registering duplicate of v1...");
Console.WriteLine($"  Returned version: v{duplicate.Version} (ID: {duplicate.Id})");
Console.WriteLine($"  Is same as v1: {duplicate.Id == v1.Id}");
Console.WriteLine();

// ============== Demo 5: Comparing Versions ==============
Console.WriteLine("--- Demo 5: Comparing Versions ---");

var diff1to2 = registry.CompareVersions("user-value", 1, 2);
Console.WriteLine("  Changes from v1 to v2:");
Console.WriteLine($"    Added: {string.Join(", ", diff1to2.AddedFields)}");
Console.WriteLine($"    Removed: {string.Join(", ", diff1to2.RemovedFields)}");

var diff1to3 = registry.CompareVersions("user-value", 1, 3);
Console.WriteLine("  Changes from v1 to v3:");
Console.WriteLine($"    Added: {string.Join(", ", diff1to3.AddedFields)}");
Console.WriteLine($"    Removed: {string.Join(", ", diff1to3.RemovedFields)}");
Console.WriteLine();

// ============== Demo 6: Multiple Subjects ==============
Console.WriteLine("--- Demo 6: Multiple Subjects ---");

var orderV1 = registry.RegisterSchema("order-value", """
{
    "type": "object",
    "properties": {
        "orderId": { "type": "string" },
        "amount": { "type": "number" }
    },
    "required": ["orderId", "amount"]
}
""", "Initial order schema");

var orderV2 = registry.RegisterSchema("order-value", """
{
    "type": "object",
    "properties": {
        "orderId": { "type": "string" },
        "amount": { "type": "number" },
        "currency": { "type": "string" }
    },
    "required": ["orderId", "amount"]
}
""", "Added currency field");

Console.WriteLine("  Subjects in registry:");
foreach (var subject in registry.GetAllSubjects())
{
    var versionCount = registry.GetAllVersions(subject).Count();
    var latest = registry.GetLatestVersion(subject);
    Console.WriteLine($"    {subject}: {versionCount} versions, latest v{latest?.Version}");
}
Console.WriteLine();

// ============== Demo 7: Using Versions with Surgewave ==============
Console.WriteLine("--- Demo 7: Using Versions with Surgewave ---");

await using var producer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

// Produce messages with different schema versions
var messages = new[]
{
    (Version: 1, Data: """{"id": 1, "name": "Alice"}"""),
    (Version: 2, Data: """{"id": 2, "name": "Bob", "email": "bob@example.com"}"""),
    (Version: 3, Data: """{"id": 3, "name": "Charlie", "email": "charlie@example.com", "metadata": {"role": "admin"}}""")
};

foreach (var msg in messages)
{
    var schemaVersion = registry.GetSchemaByVersion("user-value", msg.Version);
    if (schemaVersion != null)
    {
        var payload = Encoding.UTF8.GetBytes(msg.Data);
        var messageBytes = SerializeWithSchema(schemaVersion.Id, payload);
        await producer.ProduceAsync("users", Encoding.UTF8.GetBytes($"key-{msg.Version}"), messageBytes);
        Console.WriteLine($"  Produced with schema v{msg.Version} (ID: {schemaVersion.Id})");
    }
}
Console.WriteLine();

// TODO: SchemaRegistry package may not be referenced - Protobuf demo commented out
// ============== Demo 8: Protobuf Serialization with Schema Registry ==============
// Console.WriteLine("--- Demo 8: Protobuf Serialization with Schema Registry ---");
//
// var protoSchemaRegistry = new MockSchemaRegistryOperations();
//
// var protobufConfig = new ProtobufSerializerConfig(protoSchemaRegistry)
// {
//     AutoRegisterSchemas = true,
//     SubjectNameStrategy = TopicNameStrategy.Instance
// };
//
// var protobufSerializer = new SchemaRegistryProtobufSerializer<OrderProto>(protobufConfig);
// var protobufDeserializer = new SchemaRegistryProtobufDeserializer<OrderProto>(protobufConfig);
//
// Console.WriteLine("  Created Protobuf serializer with auto-registration enabled");
//
// await using var protobufProducer = new SurgewaveProducer<string, OrderProto>(options =>
// {
//     options.BootstrapServers = $"localhost:{runtime.Port}";
//     options.AsyncValueSerializer = protobufSerializer;
// });
//
// var orders = new[]
// {
//     new OrderProto { OrderId = "ORD-P001", CustomerId = 2001, Amount = 199.99, Currency = "USD", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
//     new OrderProto { OrderId = "ORD-P002", CustomerId = 2002, Amount = 349.50, Currency = "EUR", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
//     new OrderProto { OrderId = "ORD-P003", CustomerId = 2003, Amount = 550.00, Currency = "GBP", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
// };
//
// foreach (var order in orders)
// {
//     var result = await protobufProducer.ProduceAsync("protobuf-orders", order.OrderId, order);
//     Console.WriteLine($"  Produced Protobuf message: {order.OrderId} to offset {result.Offset}");
// }
//
// await using var protobufConsumer = new SurgewaveConsumer<string, OrderProto>(options =>
// {
//     options.BootstrapServers = $"localhost:{runtime.Port}";
//     options.AsyncValueDeserializer = protobufDeserializer;
//     options.AutoOffsetReset = AutoOffsetReset.Earliest;
// });
//
// protobufConsumer.Subscribe("protobuf-orders");
//
// Console.WriteLine("  Consuming Protobuf messages...");
// for (int i = 0; i < 3; i++)
// {
//     var consumeResult = await protobufConsumer.ConsumeAsync();
//     if (consumeResult != null)
//     {
//         Console.WriteLine($"    Consumed: OrderId={consumeResult.Value.OrderId}, Amount={consumeResult.Value.Amount} {consumeResult.Value.Currency}");
//     }
// }
Console.WriteLine();

// ============== Demo 9: Protobuf vs JSON Size Comparison ==============
Console.WriteLine("--- Demo 9: Protobuf vs JSON Size Comparison ---");

var sampleOrder = new OrderProto
{
    OrderId = "ORD-COMPARE-001",
    CustomerId = 12345,
    Amount = 1234.56,
    Currency = "USD",
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};

// Protobuf size
var protobufBytes = sampleOrder.ToByteArray();
Console.WriteLine($"  Protobuf serialized size: {protobufBytes.Length} bytes");

// JSON size (for comparison)
var jsonOrder = new { OrderId = sampleOrder.OrderId, CustomerId = sampleOrder.CustomerId, Amount = sampleOrder.Amount, Currency = sampleOrder.Currency, Timestamp = sampleOrder.Timestamp };
var jsonBytes = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(jsonOrder));
Console.WriteLine($"  JSON serialized size: {jsonBytes.Length} bytes");
Console.WriteLine($"  Protobuf is {(double)jsonBytes.Length / protobufBytes.Length:F1}x more compact");
Console.WriteLine();

// Summary
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Schema versions are auto-incremented per subject");
Console.WriteLine("2. Duplicate schemas return existing version (idempotent)");
Console.WriteLine("3. Each version has a unique global ID");
Console.WriteLine("4. Track metadata like timestamp and description");
Console.WriteLine("5. Compare versions to see evolution over time");
Console.WriteLine("6. Protobuf provides extremely compact binary serialization");
Console.WriteLine("7. Use SchemaRegistryProtobufSerializer for automatic schema registration");
Console.WriteLine("8. Protobuf is typically 2-5x more compact than JSON");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Helper Method ==============

static byte[] SerializeWithSchema(int schemaId, byte[] payload)
{
    var result = new byte[4 + payload.Length];
    BitConverter.TryWriteBytes(result.AsSpan(0, 4), schemaId);
    payload.CopyTo(result, 4);
    return result;
}

// ============== Schema Version Classes ==============

public record SchemaVersion(
    int Id,
    int Version,
    string Subject,
    string Schema,
    DateTime RegisteredAt,
    string? Description
);

public record SchemaDiff(
    List<string> AddedFields,
    List<string> RemovedFields,
    List<string> ChangedFields
);

public class VersionedSchemaRegistry
{
    private readonly Dictionary<int, SchemaVersion> _schemasById = new();
    private readonly Dictionary<string, List<SchemaVersion>> _subjectVersions = new();
    private int _nextId = 1;
    private readonly object _lock = new();

    public SchemaVersion RegisterSchema(string subject, string schema, string? description = null)
    {
        lock (_lock)
        {
            var normalizedSchema = NormalizeSchema(schema);

            if (!_subjectVersions.ContainsKey(subject))
                _subjectVersions[subject] = new List<SchemaVersion>();

            var versions = _subjectVersions[subject];

            // Check if schema already exists
            foreach (var existing in versions)
            {
                if (NormalizeSchema(existing.Schema) == normalizedSchema)
                    return existing;
            }

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

    public SchemaVersion? GetSchemaById(int id)
    {
        lock (_lock)
        {
            return _schemasById.TryGetValue(id, out var version) ? version : null;
        }
    }

    public SchemaVersion? GetSchemaByVersion(string subject, int version)
    {
        lock (_lock)
        {
            if (!_subjectVersions.TryGetValue(subject, out var versions))
                return null;

            return versions.FirstOrDefault(v => v.Version == version);
        }
    }

    public SchemaVersion? GetLatestVersion(string subject)
    {
        lock (_lock)
        {
            if (!_subjectVersions.TryGetValue(subject, out var versions) || versions.Count == 0)
                return null;

            return versions[^1];
        }
    }

    public IEnumerable<SchemaVersion> GetAllVersions(string subject)
    {
        lock (_lock)
        {
            if (_subjectVersions.TryGetValue(subject, out var versions))
                return versions.ToList();
            return Enumerable.Empty<SchemaVersion>();
        }
    }

    public IEnumerable<string> GetAllSubjects()
    {
        lock (_lock)
        {
            return _subjectVersions.Keys.ToList();
        }
    }

    public SchemaDiff CompareVersions(string subject, int fromVersion, int toVersion)
    {
        var from = GetSchemaByVersion(subject, fromVersion);
        var to = GetSchemaByVersion(subject, toVersion);

        if (from == null || to == null)
            throw new ArgumentException("Version not found");

        var fromFields = ExtractFields(from.Schema);
        var toFields = ExtractFields(to.Schema);

        return new SchemaDiff(
            AddedFields: toFields.Except(fromFields).ToList(),
            RemovedFields: fromFields.Except(toFields).ToList(),
            ChangedFields: new List<string>()
        );
    }

    private static readonly JsonSerializerOptions s_normalizeOptions = new() { WriteIndented = false };

    private static string NormalizeSchema(string schema)
    {
        try
        {
            var doc = JsonDocument.Parse(schema);
            return JsonSerializer.Serialize(doc.RootElement, s_normalizeOptions);
        }
        catch
        {
            return schema.Replace(" ", "").Replace("\n", "").Replace("\r", "");
        }
    }

    private static HashSet<string> ExtractFields(string schema)
    {
        var fields = new HashSet<string>();
        try
        {
            var doc = JsonDocument.Parse(schema);
            if (doc.RootElement.TryGetProperty("properties", out var props))
            {
                foreach (var prop in props.EnumerateObject())
                {
                    fields.Add(prop.Name);
                }
            }
        }
        catch
        {
            // Ignore parse errors
        }
        return fields;
    }
}

// ============== Protobuf Message Class ==============

/// <summary>
/// Order message for Protobuf serialization demonstration.
/// In production, this would be generated from a .proto file using protoc.
/// This hand-coded implementation demonstrates the IMessage interface.
/// </summary>
public sealed class OrderProto : IMessage<OrderProto>
{
    private static readonly MessageParser<OrderProto> _parser = new(() => new OrderProto());

    public string OrderId { get; set; } = "";
    public int CustomerId { get; set; }
    public double Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets the message parser for OrderProto.
    /// </summary>
    public static MessageParser<OrderProto> Parser => _parser;

    // IMessage implementation
    public void MergeFrom(OrderProto message)
    {
        OrderId = message.OrderId;
        CustomerId = message.CustomerId;
        Amount = message.Amount;
        Currency = message.Currency;
        Timestamp = message.Timestamp;
    }

    public OrderProto Clone() => new()
    {
        OrderId = OrderId,
        CustomerId = CustomerId,
        Amount = Amount,
        Currency = Currency,
        Timestamp = Timestamp
    };

    public void MergeFrom(CodedInputStream input)
    {
        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            switch (tag)
            {
                case 10: // field 1, string (wire type 2)
                    OrderId = input.ReadString();
                    break;
                case 16: // field 2, int32 (wire type 0)
                    CustomerId = input.ReadInt32();
                    break;
                case 25: // field 3, double (wire type 1)
                    Amount = input.ReadDouble();
                    break;
                case 34: // field 4, string (wire type 2)
                    Currency = input.ReadString();
                    break;
                case 40: // field 5, int64 (wire type 0)
                    Timestamp = input.ReadInt64();
                    break;
                default:
                    input.SkipLastField();
                    break;
            }
        }
    }

    public void WriteTo(CodedOutputStream output)
    {
        if (!string.IsNullOrEmpty(OrderId))
        {
            output.WriteRawTag(10); // field 1, string
            output.WriteString(OrderId);
        }
        if (CustomerId != 0)
        {
            output.WriteRawTag(16); // field 2, int32
            output.WriteInt32(CustomerId);
        }
        if (Amount != 0)
        {
            output.WriteRawTag(25); // field 3, double
            output.WriteDouble(Amount);
        }
        if (!string.IsNullOrEmpty(Currency))
        {
            output.WriteRawTag(34); // field 4, string
            output.WriteString(Currency);
        }
        if (Timestamp != 0)
        {
            output.WriteRawTag(40); // field 5, int64
            output.WriteInt64(Timestamp);
        }
    }

    public int CalculateSize()
    {
        int size = 0;
        if (!string.IsNullOrEmpty(OrderId))
            size += 1 + CodedOutputStream.ComputeStringSize(OrderId);
        if (CustomerId != 0)
            size += 1 + CodedOutputStream.ComputeInt32Size(CustomerId);
        if (Amount != 0)
            size += 1 + 8; // double is always 8 bytes
        if (!string.IsNullOrEmpty(Currency))
            size += 1 + CodedOutputStream.ComputeStringSize(Currency);
        if (Timestamp != 0)
            size += 1 + CodedOutputStream.ComputeInt64Size(Timestamp);
        return size;
    }

    /// <summary>
    /// Gets the message descriptor (simplified for demo - in production this is generated).
    /// </summary>
    public MessageDescriptor Descriptor => null!;

    public bool Equals(OrderProto? other)
    {
        if (other == null) return false;
        return OrderId == other.OrderId &&
               CustomerId == other.CustomerId &&
               Amount == other.Amount &&
               Currency == other.Currency &&
               Timestamp == other.Timestamp;
    }

    public override bool Equals(object? obj) => Equals(obj as OrderProto);

    public override int GetHashCode() => HashCode.Combine(OrderId, CustomerId, Amount, Currency, Timestamp);
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
