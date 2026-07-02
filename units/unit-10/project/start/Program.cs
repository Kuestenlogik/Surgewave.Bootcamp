// Unit 10 Project: Schema Evolution
// Build a versioned API event system with schema compatibility

using System.Text.Json;
using System.Text.Json.Serialization;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client.Native;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Schema Evolution: Versioned User Events ===\n");

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
    private readonly Dictionary<string, (int Version, string[] Fields)> _schemas = new();

    public void Register(string name, int version, string[] fields)
    {
        _schemas[$"{name}_v{version}"] = (version, fields);
        Console.WriteLine($"  - {name} v{version} (fields: {string.Join(", ", fields)})");
    }

    // TODO: Implement compatibility checking
    public bool IsBackwardCompatible(string name, int newVersion, int oldVersion)
    {
        // Backward compatible = new schema can read old data
        // All required fields in old version must exist in new version
        // TODO: Implement this check
        return false;
    }

    public bool IsForwardCompatible(string name, int newVersion, int oldVersion)
    {
        // Forward compatible = old schema can read new data
        // No new required fields in new version
        // TODO: Implement this check
        return false;
    }
}

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

// Start embedded broker
await using var surgewave = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .Build()
    .StartAsync();

// Create native client for fluent API
await using var client = new SurgewaveNativeClient("localhost", surgewave.Port);
await client.ConnectAsync();

// TODO: Create "user-events" topic

// Register schemas
Console.WriteLine("[Registry] Registered schemas:");
var registry = new SimpleSchemaRegistry();

// TODO: Register V1, V2, V3 schemas with their fields
// Hint: registry.Register("UserEvent", 1, new[] { "UserId", "Name", "Email" });

Console.WriteLine();

// TODO: Check and display compatibility
Console.WriteLine("[Compatibility] V2 is backward compatible with V1: ???");
Console.WriteLine("[Compatibility] V3 is backward compatible with V2: ???");
Console.WriteLine("[Compatibility] V3 is forward compatible with V2: ???");
Console.WriteLine();

// Sample events in different versions
var v1Event = new UserEventV1("U001", "Alice", "alice@example.com");
var v2Event = new UserEventV2("U002", "Bob", "bob@example.com", "+1-555-0123",
    new AddressV2("123 Main St", "Seattle", "USA"));
var v3Event = new UserEventV3("U003", "Charlie", "charlie@example.com", null, null,
    new PreferencesV3(true, "en-US", "America/Los_Angeles"));

// Step 1: Produce events with version headers
Console.WriteLine("[Producer] Producing mixed-version events...");

// TODO: Produce V1 event with header "schema-version: 1"
Console.WriteLine($"[Producer] {v1Event.UserId} (v1) -> User: {v1Event.Name}");

// TODO: Produce V2 event with header "schema-version: 2"
Console.WriteLine($"[Producer] {v2Event.UserId} (v2) -> User: {v2Event.Name}, Phone: {v2Event.Phone}");

// TODO: Produce V3 event with header "schema-version: 3"
Console.WriteLine($"[Producer] {v3Event.UserId} (v3) -> User: {v3Event.Name}, Preferences: {{Newsletter={v3Event.Preferences?.Newsletter}}}");

Console.WriteLine();

// Step 2: Consume as V2 consumer (demonstrating backward compatibility)
Console.WriteLine("[Consumer V2] Processing all events as V2...");

// TODO: Consume from "user-events" topic
// For each message:
//   1. Read schema-version header
//   2. Deserialize to UserEventV2
//   3. Handle missing fields (V1 data won't have Phone/Address)
//   4. Warn about unknown fields (V3 has Preferences that V2 doesn't know)

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

// TODO: Implement V2 consumer here

Console.WriteLine();

// Step 3: Consume as V3 consumer (demonstrating full compatibility)
Console.WriteLine("[Consumer V3] Processing all events as V3...");

// TODO: Implement V3 consumer here
// This consumer can handle all versions gracefully

Console.WriteLine();

Console.WriteLine("=== Schema Evolution Complete ===");