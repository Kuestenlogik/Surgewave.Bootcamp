// Surgewave Bootcamp - Unit 10, Lesson 4: Compatibility Modes
//
// In this lesson, you'll learn to:
// - Understand backward, forward, and full compatibility
// - Implement compatibility checking
// - Enforce compatibility on schema registration
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 10.4 - Compatibility Modes ===");
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

// TODO: Create a CompatibilityChecker instance
// Hint: var checker = new CompatibilityChecker();

// TODO: Define v1 schema with required id field
// Hint: var v1Schema = """{"type":"object","properties":{"id":{"type":"integer"}},"required":["id"]}""";

// TODO: Define v2 schema with optional email field (backward compatible)
// Hint: var v2Schema = """{"type":"object","properties":{"id":...,"email":...},"required":["id"]}""";

// TODO: Check backward compatibility between v1 and v2
// Hint: var backwardResult = checker.Check(v1Schema, v2Schema, CompatibilityMode.Backward);

// TODO: Define v3 schema with required email (backward incompatible)
// Hint: var v3Schema = """{"type":"object","properties":{"id":...,"email":...},"required":["id","email"]}""";

// TODO: Check backward compatibility - should fail
// Hint: var incompatibleResult = checker.Check(v1Schema, v3Schema, CompatibilityMode.Backward);

// TODO: Create a CompatibleSchemaRegistry with BACKWARD mode
// Hint: var registry = new CompatibleSchemaRegistry(CompatibilityMode.Backward);

// TODO: Try registering compatible and incompatible schemas
// Hint: Use try-catch to handle SchemaIncompatibleException

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Compatibility Types ==============

public enum CompatibilityMode
{
    None,
    Backward,
    Forward,
    Full
}

public record CompatibilityResult(bool IsCompatible, List<string> Errors);

public class SchemaIncompatibleException : Exception
{
    public CompatibilityMode Mode { get; }
    public List<string> Errors { get; }

    public SchemaIncompatibleException() : base("Schema incompatible")
    {
        Mode = CompatibilityMode.None;
        Errors = new List<string>();
    }

    public SchemaIncompatibleException(string message) : base(message)
    {
        Mode = CompatibilityMode.None;
        Errors = new List<string>();
    }

    public SchemaIncompatibleException(string message, Exception innerException)
        : base(message, innerException)
    {
        Mode = CompatibilityMode.None;
        Errors = new List<string>();
    }

    public SchemaIncompatibleException(string message, CompatibilityMode mode, List<string> errors)
        : base(message)
    {
        Mode = mode;
        Errors = errors;
    }
}

// TODO: Implement CompatibilityChecker class
// The class should:
// - Extract field information from schemas
// - Check backward compatibility (new reads old)
// - Check forward compatibility (old reads new)
// - Return errors for incompatible changes

// TODO: Implement CompatibleSchemaRegistry class
// The class should:
// - Store schemas with version tracking
// - Enforce compatibility mode on registration
// - Throw SchemaIncompatibleException for invalid schemas
