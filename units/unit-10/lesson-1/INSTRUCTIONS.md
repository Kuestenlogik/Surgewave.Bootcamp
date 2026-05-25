# Lesson 10.1: Schema Basics - Instructions

## Step 1: Understand the Schema Registry Interface

The schema registry provides these core operations:

```csharp
public interface ISchemaRegistry
{
    // Register a new schema for a subject (returns schema ID)
    int RegisterSchema(string subject, string schema);

    // Get schema by ID
    string? GetSchema(int id);

    // Get latest schema for a subject
    (int Id, string Schema)? GetLatestSchema(string subject);

    // Get all versions for a subject
    IEnumerable<int> GetVersions(string subject);
}
```

## Step 2: Implement an In-Memory Schema Registry

Create a simple in-memory implementation:

```csharp
public class InMemorySchemaRegistry : ISchemaRegistry
{
    private readonly Dictionary<int, string> _schemasById = new();
    private readonly Dictionary<string, List<int>> _subjectVersions = new();
    private int _nextId = 1;

    public int RegisterSchema(string subject, string schema)
    {
        var id = _nextId++;
        _schemasById[id] = schema;

        if (!_subjectVersions.ContainsKey(subject))
            _subjectVersions[subject] = new List<int>();

        _subjectVersions[subject].Add(id);
        return id;
    }

    // ... implement other methods
}
```

## Step 3: Register Schemas for a Topic

Use subject naming convention `{topic}-value` or `{topic}-key`:

```csharp
var registry = new InMemorySchemaRegistry();

// Register schema for user-events topic
var schemaId = registry.RegisterSchema("user-events-value", """
{
    "type": "object",
    "properties": {
        "userId": { "type": "integer" },
        "action": { "type": "string" },
        "timestamp": { "type": "string", "format": "date-time" }
    },
    "required": ["userId", "action"]
}
""");

Console.WriteLine($"Registered schema with ID: {schemaId}");
```

## Step 4: Retrieve Schemas

Look up schemas by ID or subject:

```csharp
// Get schema by ID (used during consumption)
var schema = registry.GetSchema(schemaId);

// Get latest schema for subject (used during production)
var latest = registry.GetLatestSchema("user-events-value");
Console.WriteLine($"Latest version ID: {latest?.Id}");
```

## Step 5: Integrate with Surgewave

The schema ID is typically prepended to messages:

```csharp
// Producer: Serialize with schema ID prefix
byte[] SerializeWithSchema(int schemaId, byte[] payload)
{
    var result = new byte[4 + payload.Length];
    BitConverter.TryWriteBytes(result.AsSpan(0, 4), schemaId);
    payload.CopyTo(result, 4);
    return result;
}

// Consumer: Extract schema ID from message
(int SchemaId, byte[] Payload) DeserializeWithSchema(byte[] data)
{
    var schemaId = BitConverter.ToInt32(data, 0);
    var payload = data[4..];
    return (schemaId, payload);
}
```

## Verification

Run the completed solution:
```bash
cd completed
dotnet run
```

You should see:
- Schema registered with unique ID
- Schema retrieved by ID
- Subject-based schema lookup working
- Schema ID embedded in messages
