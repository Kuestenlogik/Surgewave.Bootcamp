# Knowledge Assessment: Lesson 10.6 - Live Schema Inference & AI-Assisted Evolution

## Multiple Choice

### 1. What threshold does Surgewave use (by default) to decide a field is "required" in an inferred schema?

- [ ] A) 50% of samples contain the field
- [ ] B) 75% of samples contain the field
- [ ] C) 95% of samples contain the field
- [ ] D) 100% of samples contain the field

<details>
<summary>Answer</summary>
**C)** A field is marked as `required` when it appears in ≥ 95% of sampled messages. Using 100% would be too strict (one outlier sample makes everything optional); using 50% would mark too many optional fields as required.
</details>

### 2. The format detection heuristic classifies a string as `"format": "email"` when:

- [ ] A) The field name contains "email"
- [ ] B) ≥ 90% of sampled string values match the email regex pattern
- [ ] C) Any one sampled value contains an @ symbol
- [ ] D) The JSON schema type is "string" and the field is required

<details>
<summary>Answer</summary>
**B)** Format detection is value-based, not name-based, and uses a configurable threshold (default 90%). Checking only one sample would produce false positives; requiring 100% would miss valid email fields with occasional null or empty samples.
</details>

### 3. Which REST call derives a schema from live traffic WITHOUT registering it?

- [ ] A) `POST /schemas/infer/{topic}/register`
- [ ] B) `GET /schemas/infer/{topic}`
- [ ] C) `PUT /schemas/{subject}`
- [ ] D) `POST /schemas/{subject}/versions`

<details>
<summary>Answer</summary>
**B)** `GET /schemas/infer/{topic}` derives and returns the schema for inspection without side effects. `POST /schemas/infer/{topic}/register` derives AND registers the schema in one step.
</details>

### 4. What breaking level is assigned when a required field is removed from a schema?

- [ ] A) None
- [ ] B) Minor
- [ ] C) Major
- [ ] D) Critical

<details>
<summary>Answer</summary>
**C)** Removing a required field is a **Major** breaking change. Existing consumers that depend on the field will either throw deserialization errors or produce incorrect behaviour when the field is absent. Coordination with all consumers is required before registering such a change.
</details>

### 5. What happens when the LLM provider is unavailable and `FallbackToRulesBased: true` is set?

- [ ] A) The evolution analysis is aborted
- [ ] B) An exception is thrown to the caller
- [ ] C) The LLM suggestion is null; rule-based analysis (breaking level + migration code) is returned normally
- [ ] D) The schema registration is blocked until the LLM responds

<details>
<summary>Answer</summary>
**C)** The LLM enhancement is always best-effort. If the LLM call fails (network error, rate limit, key not configured), the exception is caught and logged at Debug level. The `AiSuggestion` field in the response is `null`, but all rule-based outputs (breaking level, detected changes, generated migration code) are returned as normal.
</details>

## True/False

### 6. Format detection in schema inference examines field names (e.g., "email", "createdAt") to determine the format.

- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** — Format detection is entirely value-based. Surgewave inspects the actual string content of sampled values and applies regex heuristics (e.g., date-time pattern, email pattern). Relying on field names would be fragile: a field named "description" could contain emails, and a field named "email" might store IDs.
</details>

### 7. Adding an optional field to an existing schema is classified as a Minor breaking change and is safe to register without stopping consumers.

- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** — Adding an optional field is backward compatible (old consumers simply ignore the new field) and is classified as **Minor**. Consumers do not need to be updated before the schema is registered, though they may be updated at any time to take advantage of the new field.
</details>

## Code Challenge

### 8. The following inference code always marks every field as required. Find and fix the bug:

```csharp
private JsonSchema BuildSchema(Dictionary<string, FieldStats> stats, int totalSamples)
{
    var properties = new Dictionary<string, JsonSchemaProperty>();
    var required   = new List<string>();

    foreach (var (name, fs) in stats)
    {
        required.Add(name); // Bug here?
        properties[name] = new JsonSchemaProperty { Type = InferJsonType(fs) };
    }

    return new JsonSchema { Properties = properties, Required = required };
}
```

<details>
<summary>Answer</summary>
**Bug:** `required.Add(name)` is called unconditionally for every field, regardless of how often it appeared in samples.

**Fix:** Only add to `required` when the field appeared in ≥ 95% of samples:

```csharp
private JsonSchema BuildSchema(Dictionary<string, FieldStats> stats, int totalSamples)
{
    var properties = new Dictionary<string, JsonSchemaProperty>();
    var required   = new List<string>();

    foreach (var (name, fs) in stats)
    {
        // Only mark as required when present in ≥ 95% of samples
        if (fs.Count >= totalSamples * 0.95)
            required.Add(name);

        properties[name] = new JsonSchemaProperty { Type = InferJsonType(fs) };
    }

    return new JsonSchema { Properties = properties, Required = required };
}
```

The threshold (0.95) is configurable via `InferenceOptions.RequiredFieldThreshold`.
</details>

### 9. Design an `EvolutionAdvice` struct that carries all the information a consumer team needs to act on a schema change. What fields would you include and why?

<details>
<summary>Answer</summary>

```csharp
public record EvolutionAdvice
{
    /// <summary>Schema subject (e.g., "orders-value").</summary>
    public required string Subject { get; init; }

    /// <summary>All detected field-level changes.</summary>
    public required IReadOnlyList<SchemaChange> Changes { get; init; }

    /// <summary>Overall breaking level: None / Minor / Major.</summary>
    public required BreakingLevel BreakingLevel { get; init; }

    /// <summary>
    /// Auto-generated C# code snippet showing how to update consumer models.
    /// Ready to paste but should be reviewed before committing.
    /// </summary>
    public required string MigrationCode { get; init; }

    /// <summary>
    /// Natural-language explanation from LLM, if configured. Null otherwise.
    /// Provides context that rule-based analysis cannot express.
    /// </summary>
    public string? AiSuggestion { get; init; }

    /// <summary>
    /// True when this advice can be acted on immediately without consumer coordination.
    /// False for Major changes requiring a coordination window.
    /// </summary>
    public bool IsSafeToRegisterImmediately =>
        BreakingLevel is BreakingLevel.None or BreakingLevel.Minor;

    /// <summary>
    /// Suggested action for the team.
    /// </summary>
    public string RecommendedAction => BreakingLevel switch
    {
        BreakingLevel.None  => "Safe to register — no consumer changes required.",
        BreakingLevel.Minor => "Safe to register — consumers should update models at next deploy.",
        BreakingLevel.Major => "Coordinate with all consumer teams before registering.",
        _                   => "Unknown — review manually."
    };
}
```

**Rationale for each field:**
- `Subject` — identifies which schema is affected
- `Changes` — specific diffs for engineering review
- `BreakingLevel` — quick machine-readable severity for CI gates
- `MigrationCode` — eliminates manual C# model updates; saves time and reduces errors
- `AiSuggestion` — adds contextual human-readable guidance without requiring LLM always be present
- `IsSafeToRegisterImmediately` — allows automated pipelines to auto-register Minor/None changes
- `RecommendedAction` — plain-language next step shown in the Control UI and Slack alerts
</details>

## Score

- 9/9: Excellent! You understand schema inference and AI-assisted evolution end to end
- 7-8/9: Good — review the format detection and LLM fallback sections
- 5-6/9: Review the lesson and retry
- <5/9: Re-do Lessons 10.3–10.5 before continuing
