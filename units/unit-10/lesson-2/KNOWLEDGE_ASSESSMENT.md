# Quiz: Lesson 10.2 - JSON Schema

## Multiple Choice

### 1. Which JSON Schema keyword specifies that certain fields must be present?
- [ ] A) mandatory
- [ ] B) required
- [ ] C) notNull
- [ ] D) present

<details>
<summary>Answer</summary>
**B)** The `required` keyword takes an array of field names that must be present in the JSON object for validation to pass.
</details>

### 2. What does `"additionalProperties": false` enforce in a JSON Schema?
- [ ] A) No new properties can be added to the schema
- [ ] B) Only properties defined in the schema are allowed in the data
- [ ] C) Properties cannot have default values
- [ ] D) Properties must be unique

<details>
<summary>Answer</summary>
**B)** Setting `additionalProperties: false` means the JSON object can only contain properties explicitly defined in the schema. Any extra fields will cause validation to fail.
</details>

### 3. Which JSON Schema type should be used for decimal numbers like 3.14?
- [ ] A) integer
- [ ] B) decimal
- [ ] C) number
- [ ] D) float

<details>
<summary>Answer</summary>
**C)** The `number` type accepts any numeric value including decimals. The `integer` type only accepts whole numbers without fractional parts.
</details>

## True/False

### 4. The JSON Schema "format" keyword enforces strict validation by default.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - The "format" keyword (like "email", "date-time") is primarily for documentation and annotation. Whether it enforces validation depends on the validator implementation. Many validators require explicit configuration to validate formats.
</details>

### 5. JSON Schema can validate that array items are unique using "uniqueItems": true.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Setting `"uniqueItems": true` ensures all items in the array are distinct. Duplicate values will cause validation to fail.
</details>

## Code Challenge

### 6. Write a JSON Schema for this C# record:

```csharp
public record Product(
    int Id,           // Required, must be positive
    string Name,      // Required, max 100 characters
    decimal Price,    // Required, minimum 0
    string[]? Tags    // Optional, at least 1 item if present
);
```

<details>
<summary>Answer</summary>

```json
{
    "$schema": "https://json-schema.org/draft/2020-12/schema",
    "type": "object",
    "properties": {
        "id": {
            "type": "integer",
            "minimum": 1
        },
        "name": {
            "type": "string",
            "maxLength": 100
        },
        "price": {
            "type": "number",
            "minimum": 0
        },
        "tags": {
            "type": "array",
            "items": { "type": "string" },
            "minItems": 1
        }
    },
    "required": ["id", "name", "price"],
    "additionalProperties": false
}
```

Note: `tags` is not in the `required` array since it's optional (nullable in C#).
</details>

### 7. Which of these JSON objects would FAIL validation against this schema?

```json
{
    "type": "object",
    "properties": {
        "email": { "type": "string", "maxLength": 255 },
        "score": { "type": "integer", "minimum": 0, "maximum": 100 }
    },
    "required": ["email"]
}
```

A) `{"email": "test@example.com"}`
B) `{"email": "test@example.com", "score": 50}`
C) `{"email": "test@example.com", "score": 150}`
D) `{"email": "test@example.com", "score": 50, "extra": "data"}`

<details>
<summary>Answer</summary>
**C)** `{"email": "test@example.com", "score": 150}` would fail because score (150) exceeds the maximum of 100.

- A passes: email is present, score is optional
- B passes: both fields are valid
- C **fails**: score 150 > maximum 100
- D passes: extra fields are allowed (additionalProperties is not set to false)
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
