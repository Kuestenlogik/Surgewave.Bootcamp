# Quiz: Lesson 10.3 - Schema Versioning

## Multiple Choice

### 1. Why should schemas be immutable once registered?
- [ ] A) To save storage space
- [ ] B) To ensure existing messages remain valid and consumers can always decode them
- [ ] C) To improve query performance
- [ ] D) To reduce network traffic

<details>
<summary>Answer</summary>
**B)** Immutable schemas ensure that messages written with a specific schema version can always be read by consumers who have that schema. Modifying a schema in place could break existing consumers or make historical data unreadable.
</details>

### 2. How are version numbers typically assigned in a schema registry?
- [ ] A) Manually by the developer
- [ ] B) Auto-incremented per subject
- [ ] C) Based on timestamp
- [ ] D) Randomly generated

<details>
<summary>Answer</summary>
**B)** Version numbers are auto-incremented per subject. Each new schema registered under a subject gets the next sequential version number (1, 2, 3, etc.).
</details>

### 3. What happens when you register a schema that is identical to an existing version?
- [ ] A) A new version is always created
- [ ] B) An error is thrown
- [ ] C) The existing schema ID and version are returned (no duplicate created)
- [ ] D) The old version is deleted

<details>
<summary>Answer</summary>
**C)** Most schema registries are idempotent - registering an identical schema returns the existing schema ID and version rather than creating a duplicate. This makes registration safe to retry.
</details>

## True/False

### 4. A schema registry should support retrieving any historical version of a schema.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Historical versions must be accessible because old messages in topics may have been produced with earlier schema versions. Consumers need to retrieve those schemas to decode the messages.
</details>

### 5. Schema version metadata can include information like author and deprecation status.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Version metadata can include timestamp of registration, author/source information, description of changes, and deprecation status - all useful for auditing and managing schema lifecycle.
</details>

## Code Challenge

### 6. Looking at this schema evolution, what changed between versions?

```
Subject: orders-value

Version 1: { orderId: string, amount: number }
Version 2: { orderId: string, amount: number, currency: string }
Version 3: { orderId: string, amount: number, currency: string, tax: number }
```

<details>
<summary>Answer</summary>
The evolution shows **additive changes** (adding new fields):

- **V1 to V2**: Added `currency` field (string)
- **V2 to V3**: Added `tax` field (number)

This is a safe evolution pattern because:
1. New fields can have default values for backward compatibility
2. Old consumers can ignore unknown fields for forward compatibility
3. No existing fields were modified or removed
</details>

### 7. What's wrong with this schema versioning approach?

```csharp
public class SchemaRegistry
{
    private Dictionary<string, string> _schemas = new();  // subject -> schema

    public void Register(string subject, string schema)
    {
        _schemas[subject] = schema;  // Overwrites existing!
    }

    public string Get(string subject) => _schemas[subject];
}
```

<details>
<summary>Answer</summary>
Multiple issues:

1. **No version history** - Each registration overwrites the previous schema, losing version history
2. **No immutability** - Existing schemas can be modified, breaking consumers
3. **No version tracking** - Cannot retrieve specific versions
4. **No schema IDs** - Cannot efficiently reference schemas in messages

Fix:
```csharp
public class SchemaRegistry
{
    private int _nextId = 1;
    private Dictionary<string, List<(int Id, int Version, string Schema)>> _schemas = new();

    public (int Id, int Version) Register(string subject, string schema)
    {
        if (!_schemas.ContainsKey(subject))
            _schemas[subject] = new();

        var versions = _schemas[subject];

        // Check for existing identical schema
        var existing = versions.FirstOrDefault(v => v.Schema == schema);
        if (existing != default)
            return (existing.Id, existing.Version);

        // Add new version
        var id = _nextId++;
        var version = versions.Count + 1;
        versions.Add((id, version, schema));
        return (id, version);
    }

    public string GetByVersion(string subject, int version) =>
        _schemas[subject].First(v => v.Version == version).Schema;
}
```
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
