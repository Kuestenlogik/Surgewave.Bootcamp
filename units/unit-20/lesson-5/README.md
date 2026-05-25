# Lesson 20.5: Content Policy & Pipeline

### ContentPolicyGuardrail

A configurable guardrail for custom validation rules:

```csharp
var policy = new ContentPolicyGuardrail(new ContentPolicyOptions
{
    PolicyName = "ChatPolicy",
    MinContentLength = 1,
    MaxContentLength = 10000,
    ForbiddenPatterns = [@"<script>", @"javascript:"],
    RequiredPatterns = []  // At least one must match, or empty to skip
});

var result = await policy.EvaluateAsync("Hello <script>alert('xss')</script>");
// result.Passed == false
// Violation type: "ForbiddenPattern"
```

### GuardrailPipeline

Chain multiple guardrails in sequence. Sanitized content from one guardrail flows into the next:

```csharp
var pipeline = new GuardrailPipeline()
    .Add(new PiiDetector())
    .Add(new ToxicityFilter())
    .Add(new PromptInjectionDetector())
    .Add(new ContentPolicyGuardrail(new ContentPolicyOptions
    {
        MaxContentLength = 5000
    }));

var result = await pipeline.EvaluateAsync("Email me at user@test.com, this is hate speech");

// result.Passed == false
// result.ViolationCount == 2 (PII + toxicity)
// result.HighestSeverity == GuardrailSeverity.Error
// result.FinalContent == "Email me at [REDACTED_EMAIL], this is *** speech"
```

### GuardrailPipelineResult

```csharp
public sealed record GuardrailPipelineResult
{
    public required bool Passed { get; init; }
    public required IReadOnlyList<GuardrailResult> Results { get; init; }
    public string? FinalContent { get; init; }
    public int ViolationCount { get; init; }
    public GuardrailSeverity HighestSeverity { get; init; }
    public TimeSpan TotalDuration { get; init; }
}
```

### Content Flow Through the Pipeline

```
Input: "Contact user@test.com about the hate incident"
  |
  v
PII Detector: redacts email
  Output: "Contact [REDACTED_EMAIL] about the hate incident"
  |
  v
Toxicity Filter: replaces "hate"
  Output: "Contact [REDACTED_EMAIL] about the *** incident"
  |
  v
Prompt Injection Detector: passes (no injection found)
  Output: (unchanged)
  |
  v
FinalContent: "Contact [REDACTED_EMAIL] about the *** incident"
```

### DI Registration

Register guardrails via extension methods:

```csharp
services.AddSurgewaveGuardrails();

services.AddPiiDetection(options =>
{
    options.DetectIban = true;
    options.DetectEmails = true;
});

services.AddToxicityFilter(options =>
{
    options.BlockedTerms.Add("custom-term");
});

services.AddPromptInjectionDetection(options =>
{
    options.DetectBase64Payloads = false;  // Disable if not needed
});
```

---

[Back to Unit 20](../README.md)
