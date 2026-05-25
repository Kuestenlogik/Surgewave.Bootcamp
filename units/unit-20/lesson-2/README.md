# Lesson 20.2: PII Detection & Redaction

### The PiiDetector

The `PiiDetector` uses .NET source-generated regex patterns to detect six types of personally identifiable information:

| PII Type | Pattern Example | Placeholder |
|----------|----------------|-------------|
| Email | `user@example.com` | `[REDACTED_EMAIL]` |
| Phone | `+49 170 1234567` | `[REDACTED_PHONE]` |
| Credit Card | `4111 1111 1111 1111` | `[REDACTED_CREDITCARD]` |
| SSN | `123-45-6789` | `[REDACTED_SSN]` |
| IP Address | `192.168.1.1` | `[REDACTED_IPADDRESS]` |
| German IBAN | `DE89 3704 0044 0532 0130 00` | `[REDACTED_IBAN]` |

### Basic Usage

```csharp
var detector = new PiiDetector();
var result = await detector.EvaluateAsync("Contact me at john@example.com or call 555-1234");

// result.Passed == false
// result.Violations.Count == 2
// result.SanitizedContent == "Contact me at [REDACTED_EMAIL] or call [REDACTED_PHONE]"
```

### PiiDetectorOptions

```csharp
var options = new PiiDetectorOptions
{
    DetectEmails = true,          // Default: true
    DetectPhoneNumbers = true,    // Default: true
    DetectCreditCards = true,     // Default: true
    DetectSsn = true,             // Default: true
    DetectIpAddresses = true,     // Default: true
    DetectIban = true,            // Default: true
    UseTypedPlaceholders = true,  // Default: true  (e.g., [REDACTED_EMAIL])
    RedactionPlaceholder = "[REDACTED]"  // Used when UseTypedPlaceholders = false
};

var detector = new PiiDetector(options);
```

### Selective Detection

Disable patterns you do not need:

```csharp
// Only detect German IBANs and email addresses
var options = new PiiDetectorOptions
{
    DetectEmails = true,
    DetectPhoneNumbers = false,
    DetectCreditCards = false,
    DetectSsn = false,
    DetectIpAddresses = false,
    DetectIban = true
};
```

### Detection Order

The `PiiDetector` processes patterns in a specific order to avoid false matches. IBAN detection runs before credit card detection because an IBAN like `DE89 3704 0044 0532 0130 00` contains a 16-digit sequence that would otherwise match a credit card pattern.

---

[Back to Unit 20](../README.md)
