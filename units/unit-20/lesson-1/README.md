# Lesson 20.1: Content Safety with Guardrails

### The IGuardrail Interface

Every guardrail implements `IGuardrail`, providing a consistent evaluation contract:

```csharp
public interface IGuardrail
{
    string Name { get; }
    string Description { get; }

    Task<GuardrailResult> EvaluateAsync(
        string content,
        GuardrailContext? context = null,
        CancellationToken ct = default);
}
```

### GuardrailResult

The result of evaluating content against a guardrail:

```csharp
public sealed record GuardrailResult
{
    public required bool Passed { get; init; }
    public required string GuardrailName { get; init; }
    public string? Reason { get; init; }
    public GuardrailSeverity Severity { get; init; }
    public IReadOnlyList<GuardrailViolation> Violations { get; init; }
    public string? SanitizedContent { get; init; }
    public TimeSpan EvaluationDuration { get; init; }
}
```

**Key properties:**
- `Passed` -- Whether the content passed the check
- `Violations` -- List of specific violations found, each with type, description, start index, length, and matched text
- `SanitizedContent` -- The content with violations removed or redacted (null if no changes needed)
- `Severity` -- The highest severity level found

### GuardrailSeverity

```
Info     --> Informational finding, no action required
Warning  --> Potential issue detected, review recommended
Error    --> Definite violation, content should be blocked or redacted
Critical --> Critical security violation, content must be blocked immediately
```

### GuardrailAction

Defines what to do when a violation is detected:

```
Pass   --> Allow content through without modification
Warn   --> Allow content but log a warning
Block  --> Block the content entirely
Redact --> Redact the violating portions and allow the rest
```

---

[Back to Unit 20](../README.md)
