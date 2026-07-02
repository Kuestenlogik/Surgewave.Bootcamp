// Surgewave Bootcamp - Unit 10, Lesson 3: Schema Versioning
//
// In this lesson, you'll learn to:
// - Implement versioned schema registry
// - Track version history
// - Compare schema versions
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 10.3 - Schema Versioning ===");
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

// TODO: Create a VersionedSchemaRegistry instance
// Hint: var registry = new VersionedSchemaRegistry();

// TODO: Register version 1 of a user schema (id, name)
// Hint: var v1 = registry.RegisterSchema("user-value", """...""", "Initial schema");

// TODO: Register version 2 with added email field
// Hint: var v2 = registry.RegisterSchema("user-value", """...""", "Added email");

// TODO: Register version 3 with added metadata field
// Hint: var v3 = registry.RegisterSchema("user-value", """...""", "Added metadata");

// TODO: Get all versions for the subject
// Hint: foreach (var version in registry.GetAllVersions("user-value")) ...

// TODO: Get a specific version by number
// Hint: var specific = registry.GetSchemaByVersion("user-value", 2);

// TODO: Compare two versions to see what changed
// Hint: var diff = registry.CompareVersions("user-value", 1, 3);

// TODO: Try registering the same schema again (should return existing)
// Hint: var duplicate = registry.RegisterSchema("user-value", sameSchema);

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Schema Version Record ==============

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

// TODO: Implement VersionedSchemaRegistry
// The class should:
// - Track schemas by global ID and by subject/version
// - Auto-increment version numbers per subject
// - Detect duplicate schemas (return existing if same)
// - Support getting latest, by version, or all versions
// - Compare versions to show differences
