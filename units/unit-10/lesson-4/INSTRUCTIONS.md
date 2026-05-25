# Lesson 10.4: Compatibility Modes - Instructions

## Step 1: Define Compatibility Modes

```csharp
public enum CompatibilityMode
{
    None,      // No compatibility checking
    Backward,  // New schema can read old data
    Forward,   // Old schema can read new data
    Full       // Both backward and forward
}
```

## Step 2: Understand Compatibility Rules

### Backward Compatible Changes
- Adding optional fields (with defaults)
- Removing fields
- Widening field types (int -> long)

### Forward Compatible Changes
- Adding fields (old consumers ignore)
- Removing optional fields
- Narrowing field types (long -> int, if safe)

### Breaking Changes
- Adding required fields (breaks backward)
- Removing required fields (breaks forward)
- Changing field types incompatibly
- Renaming fields

## Step 3: Implement Compatibility Checker

```csharp
public class CompatibilityChecker
{
    public CompatibilityResult Check(string oldSchema, string newSchema,
                                      CompatibilityMode mode)
    {
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
        // New required fields break backward compatibility
        foreach (var (name, info) in newFields)
        {
            if (info.IsRequired && !oldFields.ContainsKey(name))
            {
                errors.Add($"Backward: new required field '{name}' breaks compatibility");
            }
        }

        // Type changes that narrow the type
        foreach (var (name, oldInfo) in oldFields)
        {
            if (newFields.TryGetValue(name, out var newInfo))
            {
                if (!IsTypeCompatible(oldInfo.Type, newInfo.Type))
                {
                    errors.Add($"Backward: field '{name}' type changed incompatibly");
                }
            }
        }
    }
}
```

## Step 4: Set Compatibility Per Subject

```csharp
public class CompatibleSchemaRegistry
{
    private readonly Dictionary<string, CompatibilityMode> _subjectCompatibility = new();

    public void SetCompatibility(string subject, CompatibilityMode mode)
    {
        _subjectCompatibility[subject] = mode;
    }

    public SchemaVersion RegisterSchema(string subject, string schema,
                                         string? description = null)
    {
        var mode = _subjectCompatibility.GetValueOrDefault(subject, CompatibilityMode.Backward);
        var latest = GetLatestVersion(subject);

        if (latest != null && mode != CompatibilityMode.None)
        {
            var result = _checker.Check(latest.Schema, schema, mode);
            if (!result.IsCompatible)
            {
                throw new SchemaIncompatibleException(
                    $"Schema incompatible with mode {mode}: {string.Join(", ", result.Errors)}");
            }
        }

        // Proceed with registration...
    }
}
```

## Step 5: Test Compatibility Scenarios

```csharp
// Backward compatible: add optional field
var v1 = """{"type":"object","properties":{"id":{"type":"integer"}},"required":["id"]}""";
var v2 = """{"type":"object","properties":{"id":{"type":"integer"},"email":{"type":"string"}},"required":["id"]}""";
// OK: email is optional

// Backward incompatible: add required field
var v3 = """{"type":"object","properties":{"id":{"type":"integer"},"email":{"type":"string"}},"required":["id","email"]}""";
// FAIL: email is required

// Forward compatible: remove optional field
// OK: old consumers won't see the removed field

// Forward incompatible: remove required field
// FAIL: old consumers expect the field
```

## Step 6: Handle Compatibility Errors

```csharp
public class SchemaIncompatibleException : Exception
{
    public CompatibilityMode Mode { get; }
    public List<string> Errors { get; }

    public SchemaIncompatibleException(string message, CompatibilityMode mode,
                                        List<string> errors)
        : base(message)
    {
        Mode = mode;
        Errors = errors;
    }
}
```

## Verification

Run the completed solution:
```bash
cd completed
dotnet run
```

You should see:
- Backward compatible changes accepted
- Forward compatible changes accepted
- Incompatible changes rejected with errors
- Different modes enforced per subject
