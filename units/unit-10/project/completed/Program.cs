// Unit 10 Project: Schema Evolution - COMPLETED
// A versioned API event system demonstrating schema compatibility

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Client.Producer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Schema Evolution: Versioned User Events ===\n");

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;

// Create producer
await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);

// Register schemas
Console.WriteLine("[Registry] Registered schemas:");
var registry = new SimpleSchemaRegistry();
registry.Register("UserEvent", 1, ["UserId", "Name", "Email"]);
registry.Register("UserEvent", 2, ["UserId", "Name", "Email", "Phone", "Address"]);
registry.Register("UserEvent", 3, ["UserId", "Name", "Email", "Phone", "Address", "Preferences"]);

Console.WriteLine();

// Check and display compatibility
var v2BackwardV1 = registry.IsBackwardCompatible("UserEvent", 2, 1);
var v3BackwardV2 = registry.IsBackwardCompatible("UserEvent", 3, 2);
var v3ForwardV2 = registry.IsForwardCompatible("UserEvent", 3, 2);

Console.WriteLine($"[Compatibility] V2 is backward compatible with V1: {(v2BackwardV1 ? "YES" : "NO")}");
Console.WriteLine($"[Compatibility] V3 is backward compatible with V2: {(v3BackwardV2 ? "YES" : "NO")}");
Console.WriteLine($"[Compatibility] V3 is forward compatible with V2: {(v3ForwardV2 ? "YES" : "NO")}");
Console.WriteLine();

// Sample events in different versions
var v1Event = new UserEventV1("U001", "Alice", "alice@example.com");
var v2Event = new UserEventV2("U002", "Bob", "bob@example.com", "+1-555-0123",
    new AddressV2("123 Main St", "Seattle", "USA"));
var v3Event = new UserEventV3("U003", "Charlie", "charlie@example.com", null, null,
    new PreferencesV3(true, "en-US", "America/Los_Angeles"));

// Step 1: Produce events with version headers
Console.WriteLine("[Producer] Producing mixed-version events...");

// Produce V1 event
await producer.ProduceAsync("user-events", v1Event.UserId, JsonSerializer.Serialize(v1Event, jsonOptions),
    new Dictionary<string, byte[]> { { "schema-version", Encoding.UTF8.GetBytes("1") } });
Console.WriteLine($"[Producer] {v1Event.UserId} (v1) -> User: {v1Event.Name}");

// Produce V2 event
await producer.ProduceAsync("user-events", v2Event.UserId, JsonSerializer.Serialize(v2Event, jsonOptions),
    new Dictionary<string, byte[]> { { "schema-version", Encoding.UTF8.GetBytes("2") } });
Console.WriteLine($"[Producer] {v2Event.UserId} (v2) -> User: {v2Event.Name}, Phone: {v2Event.Phone}");

// Produce V3 event
await producer.ProduceAsync("user-events", v3Event.UserId, JsonSerializer.Serialize(v3Event, jsonOptions),
    new Dictionary<string, byte[]> { { "schema-version", Encoding.UTF8.GetBytes("3") } });
Console.WriteLine($"[Producer] {v3Event.UserId} (v3) -> User: {v3Event.Name}, Preferences: {{Newsletter={v3Event.Preferences?.Newsletter}}}");

Console.WriteLine();

// Step 2: Consume as V2 consumer (demonstrating backward compatibility)
Console.WriteLine("[Consumer V2] Processing all events as V2...");

await using var consumerV2 = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "v2-consumer";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
consumerV2.Subscribe("user-events");

using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var v2ProcessedCount = 0;

try
{
    while (!cts1.Token.IsCancellationRequested)
    {
        var message = await consumerV2.ConsumeAsync(cts1.Token);
        if (message is null) continue;

        var schemaVersionBytes = message.Headers?.GetValueOrDefault("schema-version");
        var schemaVersion = schemaVersionBytes != null ? int.Parse(Encoding.UTF8.GetString(schemaVersionBytes)) : 1;

        // Deserialize to V2 (can handle V1, V2, and partially V3)
        var user = JsonSerializer.Deserialize<UserEventV2>(message.Value, jsonOptions);
        if (user is null) continue;

        var phoneDisplay = string.IsNullOrEmpty(user.Phone) ? "(not provided)" : user.Phone;
        Console.WriteLine($"[Consumer V2] {user.UserId}: {user.Name} ({user.Email}) - Phone: {phoneDisplay}");

        // Warn about unknown fields in newer versions
        if (schemaVersion > 2)
        {
            Console.WriteLine($"[Consumer V2] Warning: Unknown fields may exist in message {user.UserId} (schema v{schemaVersion})");
        }

        v2ProcessedCount++;
        if (v2ProcessedCount >= 3) break;
    }
}
catch (OperationCanceledException) { }

Console.WriteLine();

// Step 3: Consume as V3 consumer (demonstrating full compatibility)
Console.WriteLine("[Consumer V3] Processing all events as V3...");

await using var consumerV3 = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "v3-consumer";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
consumerV3.Subscribe("user-events");

using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var v3ProcessedCount = 0;

try
{
    while (!cts2.Token.IsCancellationRequested)
    {
        var message = await consumerV3.ConsumeAsync(cts2.Token);
        if (message is null) continue;

        var schemaVersionBytes = message.Headers?.GetValueOrDefault("schema-version");
        var schemaVersion = schemaVersionBytes != null ? int.Parse(Encoding.UTF8.GetString(schemaVersionBytes)) : 1;

        // Deserialize to V3 (handles all versions)
        var user = JsonSerializer.Deserialize<UserEventV3>(message.Value, jsonOptions);
        if (user is null) continue;

        var prefsDisplay = user.Preferences is null
            ? "(default)"
            : $"Newsletter={user.Preferences.Newsletter}, Lang={user.Preferences.Language}";

        Console.WriteLine($"[Consumer V3] {user.UserId}: {user.Name} - Preferences: {prefsDisplay}");

        v3ProcessedCount++;
        if (v3ProcessedCount >= 3) break;
    }
}
catch (OperationCanceledException) { }

Console.WriteLine();

// Summary
Console.WriteLine("=== Schema Evolution Summary ===");
Console.WriteLine($"  - V1 events: Readable by V2 (backward) and V3 (backward)");
Console.WriteLine($"  - V2 events: Readable by V3 (backward)");
Console.WriteLine($"  - V3 events: Readable by V2 (forward - extra fields ignored)");
Console.WriteLine("\n  Key Principles:");
Console.WriteLine("  1. Add optional fields for backward compatibility");
Console.WriteLine("  2. Never remove required fields");
Console.WriteLine("  3. Use nullable types for new optional fields");
Console.WriteLine("  4. Include schema version in headers for awareness");

Console.WriteLine("\n=== Schema Evolution Complete ===");

// V1 - Initial schema
public record UserEventV1(
    string UserId,
    string Name,
    string Email
);

// V2 - Added fields (backward compatible with V1)
public record UserEventV2(
    string UserId,
    string Name,
    string Email,
    string? Phone = null,
    AddressV2? Address = null
);

public record AddressV2(string Street, string City, string Country);

// V3 - Evolved schema (forward/backward compatible with V2)
public record UserEventV3(
    string UserId,
    string Name,
    string Email,
    string? Phone = null,
    AddressV2? Address = null,
    PreferencesV3? Preferences = null
);

public record PreferencesV3(bool Newsletter, string Language, string Timezone);

// Schema registry for tracking versions
public class SimpleSchemaRegistry
{
    private readonly Dictionary<string, (int Version, HashSet<string> Fields)> _schemas = new();

    public void Register(string name, int version, string[] fields)
    {
        _schemas[$"{name}_v{version}"] = (version, fields.ToHashSet());
        Console.WriteLine($"  - {name} v{version} (fields: {string.Join(", ", fields)})");
    }

    public bool IsBackwardCompatible(string name, int newVersion, int oldVersion)
    {
        // Backward compatible = new schema can read old data
        // All fields in old version must exist in new version
        var oldKey = $"{name}_v{oldVersion}";
        var newKey = $"{name}_v{newVersion}";

        if (!_schemas.TryGetValue(oldKey, out var oldSchema) ||
            !_schemas.TryGetValue(newKey, out var newSchema))
            return false;

        // New schema must contain all old fields
        return oldSchema.Fields.All(f => newSchema.Fields.Contains(f));
    }

    public bool IsForwardCompatible(string name, int newVersion, int oldVersion)
    {
        // Forward compatible = old schema can read new data
        // Core fields in new version exist in old version (new optional fields are OK)
        var oldKey = $"{name}_v{oldVersion}";
        var newKey = $"{name}_v{newVersion}";

        if (!_schemas.TryGetValue(oldKey, out var oldSchema) ||
            !_schemas.TryGetValue(newKey, out var newSchema))
            return false;

        // Old schema fields must be a subset of new schema fields
        // (old consumer can ignore extra fields)
        return oldSchema.Fields.All(f => newSchema.Fields.Contains(f));
    }
}