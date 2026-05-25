# Lesson 10.2: JSON Schema - Instructions

## Step 1: Understand JSON Schema Structure

A JSON Schema has these main components:

```json
{
    "$schema": "https://json-schema.org/draft/2020-12/schema",
    "type": "object",
    "title": "User Event",
    "description": "An event representing user activity",
    "properties": {
        "userId": {
            "type": "integer",
            "description": "Unique user identifier"
        },
        "action": {
            "type": "string",
            "enum": ["login", "logout", "purchase", "view"]
        }
    },
    "required": ["userId", "action"]
}
```

## Step 2: Create a Schema Validator

Implement a simple JSON Schema validator:

```csharp
public class JsonSchemaValidator
{
    private readonly JsonElement _schema;

    public JsonSchemaValidator(string schemaJson)
    {
        _schema = JsonDocument.Parse(schemaJson).RootElement;
    }

    public ValidationResult Validate(string json)
    {
        var data = JsonDocument.Parse(json).RootElement;
        var errors = new List<string>();
        ValidateElement(data, _schema, "", errors);
        return new ValidationResult(errors.Count == 0, errors);
    }

    private void ValidateElement(JsonElement data, JsonElement schema,
                                  string path, List<string> errors)
    {
        // Check type
        if (schema.TryGetProperty("type", out var typeElement))
        {
            var expectedType = typeElement.GetString();
            if (!MatchesType(data, expectedType))
            {
                errors.Add($"{path}: expected {expectedType}");
                return;
            }
        }

        // Check required properties
        if (schema.TryGetProperty("required", out var required))
        {
            foreach (var req in required.EnumerateArray())
            {
                var propName = req.GetString()!;
                if (!data.TryGetProperty(propName, out _))
                {
                    errors.Add($"{path}: missing required property '{propName}'");
                }
            }
        }

        // Recursively validate properties
        // ... implement property validation
    }
}
```

## Step 3: Define Schemas for Message Types

Create schemas for common patterns:

```csharp
// User event schema
var userEventSchema = """
{
    "type": "object",
    "properties": {
        "userId": { "type": "integer" },
        "action": {
            "type": "string",
            "enum": ["login", "logout", "purchase"]
        },
        "timestamp": { "type": "string" },
        "metadata": {
            "type": "object",
            "additionalProperties": true
        }
    },
    "required": ["userId", "action", "timestamp"]
}
""";

// Order schema with nested objects
var orderSchema = """
{
    "type": "object",
    "properties": {
        "orderId": { "type": "string" },
        "customer": {
            "type": "object",
            "properties": {
                "id": { "type": "integer" },
                "email": { "type": "string" }
            },
            "required": ["id"]
        },
        "items": {
            "type": "array",
            "items": {
                "type": "object",
                "properties": {
                    "sku": { "type": "string" },
                    "quantity": { "type": "integer", "minimum": 1 }
                }
            },
            "minItems": 1
        }
    },
    "required": ["orderId", "customer", "items"]
}
""";
```

## Step 4: Validate Before Producing

Integrate validation with producers:

```csharp
async Task ProduceValidated(string topic, string key, string jsonMessage)
{
    var schema = registry.GetLatestSchema($"{topic}-value");
    if (schema == null)
        throw new Exception($"No schema registered for {topic}");

    var validator = new JsonSchemaValidator(schema.Value.Schema);
    var result = validator.Validate(jsonMessage);

    if (!result.IsValid)
    {
        Console.WriteLine($"Validation failed: {string.Join(", ", result.Errors)}");
        return;
    }

    var payload = Encoding.UTF8.GetBytes(jsonMessage);
    var messageWithSchema = SerializeWithSchema(schema.Value.Id, payload);
    await producer.ProduceAsync(topic, Encoding.UTF8.GetBytes(key), messageWithSchema);
}
```

## Step 5: Handle Validation Errors

Provide meaningful error messages:

```csharp
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

    public SchemaValidationException(List<string> errors)
        : base($"Schema validation failed: {string.Join("; ", errors)}")
    {
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
- Schemas defined for different message types
- Valid messages passing validation
- Invalid messages rejected with error details
- Successful production of validated messages
