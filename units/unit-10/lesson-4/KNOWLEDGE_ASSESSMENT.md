# Quiz: Lesson 10.4 - Compatibility Modes

## Multiple Choice

### 1. What does BACKWARD compatibility mean?
- [ ] A) Old consumers can read new data
- [ ] B) New consumers can read old data
- [ ] C) Data flows backward through the system
- [ ] D) Schemas can revert to previous versions

<details>
<summary>Answer</summary>
**B)** BACKWARD compatibility means a new schema version can read data written with the old schema. This is important during rolling deployments where new consumers may need to process existing (old) messages.
</details>

### 2. Which compatibility mode is the most restrictive (safest)?
- [ ] A) NONE
- [ ] B) BACKWARD
- [ ] C) FORWARD
- [ ] D) FULL

<details>
<summary>Answer</summary>
**D)** FULL compatibility is the most restrictive because it requires both BACKWARD (new reads old) AND FORWARD (old reads new) compatibility. Only changes that work in both directions are allowed.
</details>

### 3. What type of schema change is safe under BACKWARD compatibility?
- [ ] A) Removing required fields
- [ ] B) Adding optional fields with defaults
- [ ] C) Changing field types
- [ ] D) Renaming fields

<details>
<summary>Answer</summary>
**B)** Adding optional fields with defaults is backward compatible because new consumers can read old data (the new field uses its default when missing from old data).
</details>

## True/False

### 4. FORWARD compatibility allows you to add required fields without defaults.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - FORWARD compatibility means old consumers read new data. If you add a required field, old consumers won't know about it and won't provide a value, causing validation failures. Only optional fields (that can be ignored) are safe to add.
</details>

### 5. The recommended default compatibility mode for most use cases is BACKWARD.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - BACKWARD is the most common default because it supports the typical deployment pattern: upgrade consumers first (they can still read old messages), then upgrade producers (new messages work with upgraded consumers).
</details>

## Code Challenge

### 6. Will this schema change pass FULL compatibility checking?

```json
// Version 1
{
    "type": "object",
    "properties": {
        "id": { "type": "integer" },
        "name": { "type": "string" }
    },
    "required": ["id", "name"]
}

// Version 2 (proposed)
{
    "type": "object",
    "properties": {
        "id": { "type": "integer" },
        "name": { "type": "string" },
        "email": { "type": "string" }
    },
    "required": ["id", "name", "email"]
}
```

<details>
<summary>Answer</summary>
**No**, this change will FAIL full compatibility:

- **BACKWARD check FAILS**: New schema (v2) requires `email`, but old data doesn't have it. New consumer cannot read old data.
- **FORWARD check FAILS**: Old schema (v1) doesn't know about `email`, so it can't validate new data that includes it (if using strict validation).

To pass FULL compatibility, `email` must be optional:
```json
// Version 2 (compatible)
{
    "properties": {
        "id": { "type": "integer" },
        "name": { "type": "string" },
        "email": { "type": "string", "default": "" }
    },
    "required": ["id", "name"]  // email NOT required
}
```
</details>

### 7. Match each compatibility mode with its safe changes:

| Mode | Safe Changes |
|------|--------------|
| BACKWARD | ? |
| FORWARD | ? |
| FULL | ? |

Options:
- A) Add optional fields, remove required fields
- B) Add required fields, remove optional fields
- C) Add/remove optional fields only

<details>
<summary>Answer</summary>

| Mode | Safe Changes |
|------|--------------|
| BACKWARD | **A) Add optional fields, remove required fields** - New schema reads old data; missing new fields use defaults, removed fields are ignored |
| FORWARD | **B) Add required fields, remove optional fields** - Old schema reads new data; new required fields are ignored, removed optional fields don't cause errors |
| FULL | **C) Add/remove optional fields only** - Must work both ways; only optional fields are safe in both directions |

Key insight: BACKWARD and FORWARD are inverses of each other, and FULL is their intersection.
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
