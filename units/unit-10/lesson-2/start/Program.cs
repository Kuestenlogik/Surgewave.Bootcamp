// Surgewave Bootcamp - Unit 10, Lesson 2: JSON Schema
//
// In this lesson, you'll learn to:
// - Define JSON schemas for message validation
// - Implement a schema validator
// - Validate messages before producing
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 10.2 - JSON Schema ===");
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

// TODO: Define a JSON schema for user events
// The schema should have: userId (integer), action (string), timestamp (string)
// Hint: var userEventSchema = """{ "type": "object", ... }""";

// TODO: Create a JsonSchemaValidator with the schema
// Hint: var validator = new JsonSchemaValidator(userEventSchema);

// TODO: Validate a valid message
// Hint: var validResult = validator.Validate("""{"userId": 1, "action": "login", "timestamp": "..."}""");

// TODO: Validate an invalid message (missing required field)
// Hint: var invalidResult = validator.Validate("""{"userId": 1}""");

// TODO: Validate a message with wrong type
// Hint: var wrongTypeResult = validator.Validate("""{"userId": "not-a-number", ...}""");

// TODO: Define a schema with nested objects (e.g., order with items array)
// Hint: var orderSchema = """{ "type": "object", "properties": { "items": { "type": "array" } } }""";

// TODO: Create a validated producer wrapper
// Hint: Create a method that validates before producing

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Validation Result ==============

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

// TODO: Implement JsonSchemaValidator class
// The class should:
// - Parse the schema in the constructor
// - Validate JSON data against the schema
// - Return ValidationResult with errors
// Hint: Use JsonDocument to parse both schema and data
