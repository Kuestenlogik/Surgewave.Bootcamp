// Surgewave Bootcamp - Unit 10, Lesson 1: Schema Basics
//
// In this lesson, you'll learn to:
// - Understand the purpose of schema registry
// - Implement a basic in-memory schema registry
// - Register and retrieve schemas
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 10.1 - Schema Basics ===");
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

// TODO: Create an InMemorySchemaRegistry instance
// Hint: var registry = new InMemorySchemaRegistry();

// TODO: Register a schema for "user-events-value" subject
// The schema should define userId (integer), action (string), timestamp (string)
// Hint: var schemaId = registry.RegisterSchema("user-events-value", """...""");

// TODO: Retrieve the schema by ID
// Hint: var schema = registry.GetSchema(schemaId);

// TODO: Get the latest schema for the subject
// Hint: var latest = registry.GetLatestSchema("user-events-value");

// TODO: Register a second version of the schema (add "email" field)
// Hint: var schemaId2 = registry.RegisterSchema("user-events-value", """...""");

// TODO: List all versions for the subject
// Hint: foreach (var version in registry.GetVersions("user-events-value")) ...

// TODO: Use SerializeWithSchema to create a message with schema ID prefix
// Hint: var messageWithSchema = SerializeWithSchema(schemaId, payload);

// TODO: Use DeserializeWithSchema to extract schema ID from message
// Hint: var (extractedId, extractedPayload) = DeserializeWithSchema(messageWithSchema);

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Helper Methods ==============

#pragma warning disable CS8321 // Local function is declared but never used
static byte[] SerializeWithSchema(int schemaId, byte[] payload)
{
    // TODO: Prepend 4-byte schema ID to payload
    // Hint: Use BitConverter.TryWriteBytes
    throw new NotImplementedException();
}

static (int SchemaId, byte[] Payload) DeserializeWithSchema(byte[] data)
{
    // TODO: Extract 4-byte schema ID and remaining payload
    // Hint: Use BitConverter.ToInt32 and array slicing
    throw new NotImplementedException();
}
#pragma warning restore CS8321

// ============== Schema Registry Interface ==============

public interface ISchemaRegistry
{
    int RegisterSchema(string subject, string schema);
    string? GetSchema(int id);
    (int Id, string Schema)? GetLatestSchema(string subject);
    IEnumerable<int> GetVersions(string subject);
}

// TODO: Implement InMemorySchemaRegistry
// Hint: Use dictionaries to store schemas by ID and track subject versions
