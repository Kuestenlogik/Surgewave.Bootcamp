# Unit 10 Project: Schema Evolution

Build a versioned API event system demonstrating forward and backward schema compatibility.

## Scenario

You're evolving a user registration event API over multiple versions:
1. V1: Basic user info (id, name, email)
2. V2: Added phone and address (backward compatible)
3. V3: Deprecated phone, added preferences (forward/backward compatible)

The system must handle consumers running different versions simultaneously.

## Requirements

### Functional Requirements

- [ ] Define V1, V2, and V3 user event schemas
- [ ] Produce events in all three versions
- [ ] Consume and correctly deserialize regardless of version
- [ ] Handle missing fields gracefully (use defaults)
- [ ] Demonstrate backward and forward compatibility

### Technical Requirements

- [ ] Use schema version header in messages
- [ ] Implement version-aware deserialization
- [ ] Validate compatibility rules
- [ ] Log schema migration warnings

## Schema Versions

```csharp
// V1 - Initial schema
public record UserEventV1(
    string UserId,
    string Name,
    string Email
);

// V2 - Added fields (backward compatible)
public record UserEventV2(
    string UserId,
    string Name,
    string Email,
    string? Phone,           // New optional field
    AddressV2? Address       // New optional field
);

public record AddressV2(string Street, string City, string Country);

// V3 - Evolved schema (forward/backward compatible)
public record UserEventV3(
    string UserId,
    string Name,
    string Email,
    string? Phone,           // Deprecated but still supported
    AddressV2? Address,
    PreferencesV3? Preferences  // New optional field
);

public record PreferencesV3(bool Newsletter, string Language, string Timezone);
```

## Expected Output

```
=== Schema Evolution: Versioned User Events ===

[Registry] Registered schemas:
  - UserEvent v1 (fields: UserId, Name, Email)
  - UserEvent v2 (fields: UserId, Name, Email, Phone, Address)
  - UserEvent v3 (fields: UserId, Name, Email, Phone, Address, Preferences)

[Compatibility] V2 is backward compatible with V1: YES
[Compatibility] V3 is backward compatible with V2: YES
[Compatibility] V3 is forward compatible with V2: YES

[Producer] Producing mixed-version events...
[Producer] U001 (v1) -> User: Alice
[Producer] U002 (v2) -> User: Bob, Phone: +1-555-0123
[Producer] U003 (v3) -> User: Charlie, Preferences: {Newsletter=true}

[Consumer V2] Processing all events as V2...
[Consumer V2] U001: Alice (alice@example.com) - Phone: (not provided)
[Consumer V2] U002: Bob (bob@example.com) - Phone: +1-555-0123
[Consumer V2] U003: Charlie (charlie@example.com) - Phone: (not provided)
[Consumer V2] Warning: Unknown field 'Preferences' in message U003

[Consumer V3] Processing all events as V3...
[Consumer V3] U001: Alice - Preferences: (default)
[Consumer V3] U002: Bob - Preferences: (default)
[Consumer V3] U003: Charlie - Preferences: Newsletter=true, Lang=en-US

=== Schema Evolution Complete ===
```

## Getting Started

```bash
cd bootcamp/unit-10/project/start
dotnet run
```

## Hints

1. Store schema version in message headers: `schema-version: 3`
2. Use `JsonSerializerOptions` with `PropertyNameCaseInsensitive = true`
3. Nullable properties handle missing fields gracefully
4. Compare schema fields for compatibility checks

## Evaluation Criteria

| Criteria | Points |
|----------|--------|
| All schema versions defined correctly | 20 |
| Version headers used properly | 15 |
| Backward compatibility works | 25 |
| Forward compatibility works | 25 |
| Clear compatibility reporting | 15 |

## Solution

Compare your implementation with [completed/](completed/README.md) when done.
