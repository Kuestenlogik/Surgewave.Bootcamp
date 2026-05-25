# Lesson 10.3: Schema Versioning - Instructions

## Step 1: Understand Version Structure

Each schema version has:

```csharp
public record SchemaVersion(
    int Id,                    // Global unique ID
    int Version,               // Version within subject (1, 2, 3...)
    string Subject,            // Subject name
    string Schema,             // The actual schema
    DateTime RegisteredAt,     // When registered
    string? Description        // Optional change description
);
```

## Step 2: Implement Versioned Schema Registry

Extend the schema registry to support versioning:

```csharp
public class VersionedSchemaRegistry
{
    private readonly Dictionary<int, SchemaVersion> _schemasById = new();
    private readonly Dictionary<string, List<SchemaVersion>> _subjectVersions = new();
    private int _nextId = 1;

    public SchemaVersion RegisterSchema(string subject, string schema,
                                         string? description = null)
    {
        var versions = _subjectVersions.GetValueOrDefault(subject)
                       ?? new List<SchemaVersion>();

        // Check if schema already exists (return existing version)
        foreach (var existing in versions)
        {
            if (NormalizeSchema(existing.Schema) == NormalizeSchema(schema))
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

        if (!_subjectVersions.ContainsKey(subject))
            _subjectVersions[subject] = new List<SchemaVersion>();

        _subjectVersions[subject].Add(version);
        return version;
    }

    private static string NormalizeSchema(string schema)
    {
        // Remove whitespace for comparison
        return JsonSerializer.Serialize(
            JsonDocument.Parse(schema).RootElement
        );
    }
}
```

## Step 3: Retrieve by Version Number

Get specific versions:

```csharp
public SchemaVersion? GetSchemaByVersion(string subject, int version)
{
    if (!_subjectVersions.TryGetValue(subject, out var versions))
        return null;

    return versions.FirstOrDefault(v => v.Version == version);
}

public SchemaVersion? GetLatestVersion(string subject)
{
    if (!_subjectVersions.TryGetValue(subject, out var versions) ||
        versions.Count == 0)
        return null;

    return versions[^1];
}

public IEnumerable<SchemaVersion> GetAllVersions(string subject)
{
    return _subjectVersions.GetValueOrDefault(subject)
           ?? Enumerable.Empty<SchemaVersion>();
}
```

## Step 4: Track Schema Changes

Calculate differences between versions:

```csharp
public record SchemaDiff(
    List<string> AddedFields,
    List<string> RemovedFields,
    List<string> ChangedFields
);

public SchemaDiff CompareVersions(string subject, int fromVersion, int toVersion)
{
    var from = GetSchemaByVersion(subject, fromVersion);
    var to = GetSchemaByVersion(subject, toVersion);

    if (from == null || to == null)
        throw new Exception("Version not found");

    var fromFields = ExtractFields(from.Schema);
    var toFields = ExtractFields(to.Schema);

    return new SchemaDiff(
        AddedFields: toFields.Except(fromFields).ToList(),
        RemovedFields: fromFields.Except(toFields).ToList(),
        ChangedFields: new List<string>() // Simplified
    );
}
```

## Step 5: Support Schema Evolution

Demonstrate typical evolution patterns:

```csharp
// V1: Initial schema
registry.RegisterSchema("user-value", """
{
    "type": "object",
    "properties": {
        "id": { "type": "integer" },
        "name": { "type": "string" }
    }
}
""", "Initial user schema");

// V2: Add email field
registry.RegisterSchema("user-value", """
{
    "type": "object",
    "properties": {
        "id": { "type": "integer" },
        "name": { "type": "string" },
        "email": { "type": "string" }
    }
}
""", "Added email field");

// V3: Add optional metadata
registry.RegisterSchema("user-value", """
{
    "type": "object",
    "properties": {
        "id": { "type": "integer" },
        "name": { "type": "string" },
        "email": { "type": "string" },
        "metadata": { "type": "object" }
    }
}
""", "Added optional metadata field");
```

## Verification

Run the completed solution:
```bash
cd completed
dotnet run
```

You should see:
- Multiple schema versions registered
- Version history displayed
- Schema differences calculated
- Specific versions retrieved by number
