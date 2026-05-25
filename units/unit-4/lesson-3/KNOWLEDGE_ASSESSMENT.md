# Knowledge Assessment: Lesson 6.3 - Event Schema Design

## Multiple Choice

### 1. What does "backward compatible" mean for schema evolution?
- [ ] A) Old producers can create new messages
- [ ] B) New consumers can read old messages
- [ ] C) Old and new schemas are identical
- [ ] D) The schema cannot be changed

<details>
<summary>Answer</summary>
**B)** Backward compatible means new consumers can read old messages. When you deploy new consumer code, it can still process messages written by older producer versions.
</details>

### 2. Which schema change is safe (backward and forward compatible)?
- [ ] A) Adding a required field
- [ ] B) Removing a required field
- [ ] C) Adding an optional field with a default value
- [ ] D) Changing a field's type

<details>
<summary>Answer</summary>
**C)** Adding an optional field with a default value is safe. Old consumers ignore the new field, new consumers use the default when reading old messages. This maintains both backward and forward compatibility.
</details>

### 3. What happens when you add a required field without a default?
- [ ] A) Old messages are automatically updated
- [ ] B) New consumers fail when reading old messages
- [ ] C) Nothing, it works fine
- [ ] D) The schema registry prevents it

<details>
<summary>Answer</summary>
**B)** Adding a required field breaks backward compatibility. New consumers cannot read old messages because the required field is missing, causing deserialization failures.
</details>

## True/False

### 4. Renaming a field without an alias is a safe schema change.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Renaming a field without an alias is unsafe. Old consumers won't find the field with the new name, and new consumers won't find the field with the old name. Use aliases for safe renames.
</details>

### 5. Forward compatibility means old consumers can read new messages.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Forward compatibility means old consumers can read new messages (possibly ignoring new fields). This is important when producers are updated before consumers.
</details>

## Code Challenge

### 6. You need to evolve this event schema. Show how to safely add `email` and `phone` fields:

**Version 1:**
```json
{
  "user_id": "123",
  "name": "Alice"
}
```

<details>
<summary>Answer</summary>
**Safe evolution to Version 2 and 3:**

**Version 2 (add email):**
```json
{
  "user_id": "123",
  "name": "Alice",
  "email": "alice@example.com"  // Optional, nullable
}
```

**Version 3 (add phone):**
```json
{
  "user_id": "123",
  "name": "Alice",
  "email": "alice@example.com",
  "phone": "+1-555-0123"  // Optional, nullable
}
```

**C# implementation with safe defaults:**
```csharp
public record UserEvent
{
    public required string UserId { get; init; }
    public required string Name { get; init; }

    // Optional fields with defaults for backward compatibility
    public string? Email { get; init; } = null;
    public string? Phone { get; init; } = null;
}

// Consumer handles missing fields gracefully
var user = JsonSerializer.Deserialize<UserEvent>(message);
var email = user.Email ?? "not provided";
```

**Key principles:**
1. New fields are optional (nullable)
2. Consumers handle null/missing values
3. No required fields added after v1
4. Document when fields were added
</details>

### 7. A developer proposes this schema change. Evaluate its compatibility:

**Before:**
```csharp
public record OrderEvent
{
    public string OrderId { get; init; }
    public decimal Amount { get; init; }  // e.g., 99.99
    public string Currency { get; init; } // e.g., "USD"
}
```

**After:**
```csharp
public record OrderEvent
{
    public string OrderId { get; init; }
    public Money Total { get; init; }  // New complex type
}

public record Money
{
    public long AmountInCents { get; init; }  // e.g., 9999
    public string Currency { get; init; }
}
```

<details>
<summary>Answer</summary>
**This change is NOT compatible. Issues:**

1. **Type change**: `Amount` (decimal) replaced with `Total.AmountInCents` (long)
2. **Structural change**: Flat fields to nested object
3. **Semantic change**: Dollars to cents representation
4. **Field removal**: `Amount` and `Currency` fields removed

**Compatibility breakdown:**
- **Backward**: BROKEN - New consumers can't read old messages (wrong structure)
- **Forward**: BROKEN - Old consumers can't read new messages (missing fields)

**Safe migration approach:**

**Phase 1: Add new fields, keep old ones**
```csharp
public record OrderEvent
{
    public string OrderId { get; init; }

    // Keep old fields (deprecated but present)
    [Obsolete("Use Total instead")]
    public decimal? Amount { get; init; }
    [Obsolete("Use Total.Currency instead")]
    public string? Currency { get; init; }

    // New field (optional during transition)
    public Money? Total { get; init; }
}
```

**Phase 2: Producers write both formats**
```csharp
var evt = new OrderEvent
{
    OrderId = "123",
    Amount = 99.99m,           // Old format
    Currency = "USD",          // Old format
    Total = new Money          // New format
    {
        AmountInCents = 9999,
        Currency = "USD"
    }
};
```

**Phase 3: After all consumers updated, deprecate old fields**
</details>

## Score
- 7/7: Excellent! You understand schema design
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
