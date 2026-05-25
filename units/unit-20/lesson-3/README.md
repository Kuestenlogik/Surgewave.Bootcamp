# Lesson 20.3: Toxicity Filtering

### The ToxicityFilter

The `ToxicityFilter` uses keyword-based detection with case-insensitive word-boundary matching. It ships with a default blocklist of 25 terms covering hate speech, violence, harassment, and extremism.

### Basic Usage

```csharp
var filter = new ToxicityFilter();
var result = await filter.EvaluateAsync("This message contains hate speech");

// result.Passed == false
// result.Violations[0].MatchedText == "hate"
// result.SanitizedContent == "This message contains *** speech"
```

### ToxicityFilterOptions

```csharp
var options = new ToxicityFilterOptions
{
    UseDefaultBlocklist = true,         // Include the built-in 25-term blocklist
    BlockedTerms = ["spam", "scam"],    // Add custom blocked terms
    CaseSensitive = false,              // Default: case-insensitive
    ReplacementText = "***"             // Text used to replace blocked terms
};

var filter = new ToxicityFilter(options);
```

### Default Blocklist

The default blocklist covers: hate, kill, murder, terrorist, bomb threat, slur, racial slur, death threat, genocide, ethnic cleansing, supremacist, extremist, violent threat, assault threat, harassment, doxxing, swatting, cyberbullying, radicalization, incitement, intimidation, hate speech, discrimination, bigotry, dehumanize, lynch.

### Word-Boundary Matching

The filter uses `\b` word-boundary regex matching, so "hated" does not match the term "hate" and "skill" does not match "kill". This reduces false positives while still catching standalone toxic terms.

### Custom Blocklist Only

```csharp
var options = new ToxicityFilterOptions
{
    UseDefaultBlocklist = false,  // Skip the built-in list
    BlockedTerms = ["proprietary-term-1", "proprietary-term-2"],
    ReplacementText = "[REMOVED]"
};
```

---

[Back to Unit 20](../README.md)
