// Surgewave Bootcamp - Unit 10, Lesson 4: Compatibility Modes
// COMPLETED SOLUTION

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 10.4 - Compatibility Modes ===");
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

// ============== Demo 1: Backward Compatibility ==============
Console.WriteLine("--- Demo 1: Backward Compatibility ---");
Console.WriteLine("  Rule: New schema can read data written with old schema");
Console.WriteLine();

var checker = new CompatibilityChecker();

// V1: Base schema
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

// V2: Add optional field (backward compatible)
var v2OptionalField = """
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

var backwardOk = checker.Check(v1Schema, v2OptionalField, CompatibilityMode.Backward);
Console.WriteLine($"  Add optional field: Compatible={backwardOk.IsCompatible}");

// V2b: Add required field (backward incompatible)
var v2RequiredField = """
{
    "type": "object",
    "properties": {
        "id": { "type": "integer" },
        "name": { "type": "string" },
        "email": { "type": "string" }
    },
    "required": ["id", "name", "email"]
}
""";

var backwardFail = checker.Check(v1Schema, v2RequiredField, CompatibilityMode.Backward);
Console.WriteLine($"  Add required field: Compatible={backwardFail.IsCompatible}");
if (!backwardFail.IsCompatible)
{
    Console.WriteLine($"    Errors: {string.Join(", ", backwardFail.Errors)}");
}
Console.WriteLine();

// ============== Demo 2: Forward Compatibility ==============
Console.WriteLine("--- Demo 2: Forward Compatibility ---");
Console.WriteLine("  Rule: Old schema can read data written with new schema");
Console.WriteLine();

// Remove optional field (forward compatible)
var v2RemoveOptional = """
{
    "type": "object",
    "properties": {
        "id": { "type": "integer" }
    },
    "required": ["id"]
}
""";

// Starting from v1 with name required
var forwardRemoveRequired = checker.Check(v1Schema, v2RemoveOptional, CompatibilityMode.Forward);
Console.WriteLine($"  Remove required field: Compatible={forwardRemoveRequired.IsCompatible}");
if (!forwardRemoveRequired.IsCompatible)
{
    Console.WriteLine($"    Errors: {string.Join(", ", forwardRemoveRequired.Errors)}");
}

// Add field (forward compatible - old consumers ignore it)
var forwardAddField = checker.Check(v1Schema, v2OptionalField, CompatibilityMode.Forward);
Console.WriteLine($"  Add new field: Compatible={forwardAddField.IsCompatible}");
Console.WriteLine();

// ============== Demo 3: Full Compatibility ==============
Console.WriteLine("--- Demo 3: Full Compatibility ---");
Console.WriteLine("  Rule: Both backward AND forward compatible");
Console.WriteLine();

// Add optional field only (full compatible)
var fullAddOptional = checker.Check(v1Schema, v2OptionalField, CompatibilityMode.Full);
Console.WriteLine($"  Add optional field: Compatible={fullAddOptional.IsCompatible}");

// Add required field (full incompatible - breaks backward)
var fullAddRequired = checker.Check(v1Schema, v2RequiredField, CompatibilityMode.Full);
Console.WriteLine($"  Add required field: Compatible={fullAddRequired.IsCompatible}");
if (!fullAddRequired.IsCompatible)
{
    Console.WriteLine($"    Errors: {string.Join(", ", fullAddRequired.Errors)}");
}

// Remove required field (full incompatible - breaks forward)
var fullRemoveRequired = checker.Check(v1Schema, v2RemoveOptional, CompatibilityMode.Full);
Console.WriteLine($"  Remove required field: Compatible={fullRemoveRequired.IsCompatible}");
Console.WriteLine();

// ============== Demo 4: Type Changes ==============
Console.WriteLine("--- Demo 4: Type Changes ---");

var intSchema = """{"type":"object","properties":{"value":{"type":"integer"}},"required":["value"]}""";
var numberSchema = """{"type":"object","properties":{"value":{"type":"number"}},"required":["value"]}""";
var stringSchema = """{"type":"object","properties":{"value":{"type":"string"}},"required":["value"]}""";

// int -> number (widening, generally safe)
var intToNumber = checker.Check(intSchema, numberSchema, CompatibilityMode.Backward);
Console.WriteLine($"  integer -> number: Compatible={intToNumber.IsCompatible}");

// int -> string (type change, incompatible)
var intToString = checker.Check(intSchema, stringSchema, CompatibilityMode.Backward);
Console.WriteLine($"  integer -> string: Compatible={intToString.IsCompatible}");
if (!intToString.IsCompatible)
{
    Console.WriteLine($"    Errors: {string.Join(", ", intToString.Errors)}");
}
Console.WriteLine();

// ============== Demo 5: Registry with Compatibility ==============
Console.WriteLine("--- Demo 5: Registry with Compatibility Enforcement ---");

var registry = new CompatibleSchemaRegistry();

// Set backward compatibility for user-value subject
registry.SetCompatibility("user-value", CompatibilityMode.Backward);
Console.WriteLine("  Set 'user-value' compatibility: BACKWARD");

// Register v1
var regV1 = registry.RegisterSchema("user-value", v1Schema, "Initial schema");
Console.WriteLine($"  Registered v{regV1.Version}: OK");

// Register v2 with optional field (should succeed)
try
{
    var regV2 = registry.RegisterSchema("user-value", v2OptionalField, "Add optional email");
    Console.WriteLine($"  Registered v{regV2.Version}: OK (optional field added)");
}
catch (SchemaIncompatibleException ex)
{
    Console.WriteLine($"  Registration failed: {ex.Message}");
}

// Try to register v3 with required field (should fail)
try
{
    var regV3 = registry.RegisterSchema("user-value", v2RequiredField, "Add required email");
    Console.WriteLine($"  Registered v{regV3.Version}: OK");
}
catch (SchemaIncompatibleException ex)
{
    Console.WriteLine($"  Registration failed (expected): {ex.Errors[0]}");
}
Console.WriteLine();

// ============== Demo 6: Different Modes Per Subject ==============
Console.WriteLine("--- Demo 6: Different Modes Per Subject ---");

// No compatibility for development subject
registry.SetCompatibility("dev-events", CompatibilityMode.None);
Console.WriteLine("  Set 'dev-events' compatibility: NONE");

var devV1 = registry.RegisterSchema("dev-events", """{"type":"object","properties":{"x":{"type":"integer"}}}""");
Console.WriteLine($"  Registered v{devV1.Version}");

// Completely different schema (normally incompatible)
var devV2 = registry.RegisterSchema("dev-events", """{"type":"object","properties":{"y":{"type":"string"}}}""");
Console.WriteLine($"  Registered v{devV2.Version}: Any change allowed with NONE mode");

// Full compatibility for production
registry.SetCompatibility("prod-events", CompatibilityMode.Full);
Console.WriteLine("  Set 'prod-events' compatibility: FULL");
Console.WriteLine();

// Summary
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. BACKWARD: New schema reads old data (add optional, remove any)");
Console.WriteLine("2. FORWARD: Old schema reads new data (add any, remove optional)");
Console.WriteLine("3. FULL: Both directions (most restrictive, safest)");
Console.WriteLine("4. NONE: No checking (for development only)");
Console.WriteLine("5. Set compatibility per subject based on requirements");

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

public record FieldInfo(string Name, string Type, bool IsRequired);

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

// ============== Compatibility Checker ==============

public class CompatibilityChecker
{
    public CompatibilityResult Check(string oldSchema, string newSchema, CompatibilityMode mode)
    {
        if (mode == CompatibilityMode.None)
            return new CompatibilityResult(true, new List<string>());

        var oldFields = ExtractFieldInfo(oldSchema);
        var newFields = ExtractFieldInfo(newSchema);
        var errors = new List<string>();

        if (mode == CompatibilityMode.Backward || mode == CompatibilityMode.Full)
        {
            CheckBackwardCompatibility(oldFields, newFields, errors);
        }

        if (mode == CompatibilityMode.Forward || mode == CompatibilityMode.Full)
        {
            CheckForwardCompatibility(oldFields, newFields, errors);
        }

        return new CompatibilityResult(errors.Count == 0, errors);
    }

    private void CheckBackwardCompatibility(
        Dictionary<string, FieldInfo> oldFields,
        Dictionary<string, FieldInfo> newFields,
        List<string> errors)
    {
        // New required fields that don't exist in old schema break backward compatibility
        foreach (var (name, info) in newFields)
        {
            if (info.IsRequired && !oldFields.ContainsKey(name))
            {
                errors.Add($"BACKWARD: New required field '{name}' - old data won't have it");
            }
        }

        // Type changes
        foreach (var (name, oldInfo) in oldFields)
        {
            if (newFields.TryGetValue(name, out var newInfo))
            {
                if (!IsBackwardTypeCompatible(oldInfo.Type, newInfo.Type))
                {
                    errors.Add($"BACKWARD: Field '{name}' type changed from '{oldInfo.Type}' to '{newInfo.Type}'");
                }
            }
        }
    }

    private void CheckForwardCompatibility(
        Dictionary<string, FieldInfo> oldFields,
        Dictionary<string, FieldInfo> newFields,
        List<string> errors)
    {
        // Removed required fields break forward compatibility
        foreach (var (name, info) in oldFields)
        {
            if (info.IsRequired && !newFields.ContainsKey(name))
            {
                errors.Add($"FORWARD: Required field '{name}' removed - old consumers expect it");
            }
        }

        // Type changes
        foreach (var (name, oldInfo) in oldFields)
        {
            if (newFields.TryGetValue(name, out var newInfo))
            {
                if (!IsForwardTypeCompatible(oldInfo.Type, newInfo.Type))
                {
                    errors.Add($"FORWARD: Field '{name}' type changed from '{oldInfo.Type}' to '{newInfo.Type}'");
                }
            }
        }
    }

    private static bool IsBackwardTypeCompatible(string oldType, string newType)
    {
        if (oldType == newType) return true;

        // Widening is generally safe for backward compatibility
        if (oldType == "integer" && newType == "number") return true;

        return false;
    }

    private static bool IsForwardTypeCompatible(string oldType, string newType)
    {
        if (oldType == newType) return true;

        // Old consumer expects old type, new data has new type
        // Widening breaks forward (old expects int, gets float)
        return false;
    }

    private static Dictionary<string, FieldInfo> ExtractFieldInfo(string schema)
    {
        var fields = new Dictionary<string, FieldInfo>();

        try
        {
            var doc = JsonDocument.Parse(schema);
            var root = doc.RootElement;

            var required = new HashSet<string>();
            if (root.TryGetProperty("required", out var reqArray))
            {
                foreach (var req in reqArray.EnumerateArray())
                {
                    required.Add(req.GetString() ?? "");
                }
            }

            if (root.TryGetProperty("properties", out var props))
            {
                foreach (var prop in props.EnumerateObject())
                {
                    var type = "any";
                    if (prop.Value.TryGetProperty("type", out var typeElement))
                    {
                        type = typeElement.GetString() ?? "any";
                    }

                    fields[prop.Name] = new FieldInfo(
                        prop.Name,
                        type,
                        required.Contains(prop.Name)
                    );
                }
            }
        }
        catch
        {
            // Return empty on parse failure
        }

        return fields;
    }
}

// ============== Schema Registry with Compatibility ==============

public record SchemaVersion(
    int Id,
    int Version,
    string Subject,
    string Schema,
    DateTime RegisteredAt,
    string? Description
);

public class CompatibleSchemaRegistry
{
    private readonly Dictionary<int, SchemaVersion> _schemasById = new();
    private readonly Dictionary<string, List<SchemaVersion>> _subjectVersions = new();
    private readonly Dictionary<string, CompatibilityMode> _subjectCompatibility = new();
    private readonly CompatibilityChecker _checker = new();
    private int _nextId = 1;
    private readonly object _lock = new();

    public void SetCompatibility(string subject, CompatibilityMode mode)
    {
        lock (_lock)
        {
            _subjectCompatibility[subject] = mode;
        }
    }

    public CompatibilityMode GetCompatibility(string subject)
    {
        lock (_lock)
        {
            return _subjectCompatibility.GetValueOrDefault(subject, CompatibilityMode.Backward);
        }
    }

    public SchemaVersion RegisterSchema(string subject, string schema, string? description = null)
    {
        lock (_lock)
        {
            var mode = GetCompatibility(subject);
            var latest = GetLatestVersion(subject);

            // Check compatibility with latest version
            if (latest != null && mode != CompatibilityMode.None)
            {
                var result = _checker.Check(latest.Schema, schema, mode);
                if (!result.IsCompatible)
                {
                    throw new SchemaIncompatibleException(
                        $"Schema incompatible with {mode} mode",
                        mode,
                        result.Errors
                    );
                }
            }

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

    public SchemaVersion? GetLatestVersion(string subject)
    {
        if (!_subjectVersions.TryGetValue(subject, out var versions) || versions.Count == 0)
            return null;

        return versions[^1];
    }

    public SchemaVersion? GetSchemaById(int id)
    {
        return _schemasById.TryGetValue(id, out var version) ? version : null;
    }
}
