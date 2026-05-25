# Knowledge Assessment: Lesson 3.2 - Headers & Metadata

## Multiple Choice

### 1. What is the main benefit of using message headers?
- [ ] A) They make messages smaller
- [ ] B) They attach metadata without modifying the payload
- [ ] C) They encrypt the message content
- [ ] D) They compress the message

<details>
<summary>Answer</summary>
**B)** They attach metadata without modifying the payload. Headers allow you to add context like correlation IDs, trace information, and routing hints without changing the business data.
</details>

### 2. What is a correlation ID used for?
- [ ] A) Encrypting messages
- [ ] B) Determining which partition to use
- [ ] C) Linking related messages across services for tracing
- [ ] D) Validating message content

<details>
<summary>Answer</summary>
**C)** Linking related messages across services for tracing. Correlation IDs allow you to trace a single request through multiple services and messages in a distributed system.
</details>

### 3. Which header format is commonly used for distributed tracing?
- [ ] A) `x-request-id`
- [ ] B) `traceparent`
- [ ] C) `message-id`
- [ ] D) `trace-token`

<details>
<summary>Answer</summary>
**B)** `traceparent`. This is the W3C Trace Context standard header format (e.g., `00-trace-span-01`) used by OpenTelemetry and other distributed tracing systems.
</details>

### 4. How are header values stored in Surgewave?
- [ ] A) As strings
- [ ] B) As byte arrays
- [ ] C) As JSON objects
- [ ] D) As integers

<details>
<summary>Answer</summary>
**B)** As byte arrays. Header values are stored as `byte[]`, so you need to encode/decode strings using `Encoding.UTF8.GetBytes()` and `Encoding.UTF8.GetString()`.
</details>

## True/False

### 5. Headers are included in the message payload size calculation.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Headers are stored separately from the payload. While they do contribute to the overall message size, they don't affect the payload itself.
</details>

### 6. Multiple headers with the same key name can be added to a single message.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Surgewave (like Kafka) allows multiple headers with the same key. This can be useful for scenarios like adding multiple trace spans or tags.
</details>

## Code Challenge

### 7. Complete the code to add and read headers:

```csharp
// Adding headers when producing
var correlationId = Guid.NewGuid().ToString();
var headers = new Headers
{
    { "x-correlation-id", ____________.GetBytes(correlationId) },
    { "x-timestamp", BitConverter.GetBytes(____________.UtcNow.Ticks) }
};

await producer.ProduceAsync("events", "key", "value", ____________);

// Reading headers when consuming
var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
if (result != null)
{
    var correlationBytes = result.Headers.____________("x-correlation-id");
    if (correlationBytes != null)
    {
        var receivedCorrelationId = Encoding.UTF8.____________(correlationBytes);
        Console.WriteLine($"Correlation ID: {receivedCorrelationId}");
    }
}
```

<details>
<summary>Answer</summary>
```csharp
// Adding headers when producing
var correlationId = Guid.NewGuid().ToString();
var headers = new Headers
{
    { "x-correlation-id", Encoding.UTF8.GetBytes(correlationId) },
    { "x-timestamp", BitConverter.GetBytes(DateTime.UtcNow.Ticks) }
};

await producer.ProduceAsync("events", "key", "value", headers);

// Reading headers when consuming
var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
if (result != null)
{
    var correlationBytes = result.Headers.GetValueOrDefault("x-correlation-id");
    if (correlationBytes != null)
    {
        var receivedCorrelationId = Encoding.UTF8.GetString(correlationBytes);
        Console.WriteLine($"Correlation ID: {receivedCorrelationId}");
    }
}
```

Key points:
- Use `Encoding.UTF8.GetBytes()` to convert strings to bytes
- Use `DateTime.UtcNow` for timestamps
- Pass `headers` as the last parameter to `ProduceAsync`
- Use `GetValueOrDefault` to safely retrieve header values
- Use `Encoding.UTF8.GetString()` to convert bytes back to strings
</details>

## Score
- 7/7: Excellent! You understand headers and metadata
- 5-6/7: Good grasp of header usage patterns
- 3-4/7: Review header encoding and reading
- <3/7: Re-do the lesson and practice with headers
