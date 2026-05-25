# Knowledge Assessment: Lesson 3.3 - Error Handling

## Multiple Choice

### 1. What is the purpose of exponential backoff in retry logic?
- [ ] A) To make retries faster
- [ ] B) To progressively increase delay between retries, avoiding overwhelming a failing service
- [ ] C) To reduce the number of retries needed
- [ ] D) To encrypt retry attempts

<details>
<summary>Answer</summary>
**B)** To progressively increase delay between retries, avoiding overwhelming a failing service. Each retry waits longer (e.g., 2s, 4s, 8s), giving the failing system time to recover.
</details>

### 2. What is a Dead Letter Queue (DLQ)?
- [ ] A) A queue for high-priority messages
- [ ] B) A storage location for messages that failed processing after all retries
- [ ] C) A queue for messages awaiting acknowledgment
- [ ] D) A temporary buffer during network issues

<details>
<summary>Answer</summary>
**B)** A storage location for messages that failed processing after all retries. DLQs preserve failed messages for later analysis and manual intervention.
</details>

### 3. Which type of failure should be retried?
- [ ] A) Invalid message format (permanent)
- [ ] B) Network timeout (transient)
- [ ] C) Missing required field (permanent)
- [ ] D) Deserialization error (permanent)

<details>
<summary>Answer</summary>
**B)** Network timeout (transient). Transient failures like timeouts or temporary unavailability may succeed on retry. Permanent failures like invalid data should go to DLQ immediately.
</details>

### 4. What is a circuit breaker pattern?
- [ ] A) A way to encrypt messages
- [ ] B) A pattern that stops calling a failing service after repeated failures
- [ ] C) A method for splitting messages
- [ ] D) A way to prioritize messages

<details>
<summary>Answer</summary>
**B)** A pattern that stops calling a failing service after repeated failures. Circuit breakers prevent cascading failures by "opening" after too many failures and only allowing periodic test requests.
</details>

## True/False

### 5. Adding error details as headers to DLQ messages helps with debugging.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Including headers like `x-error`, `x-retry-count`, and `x-failed-at` provides valuable context for understanding why a message failed and aids in debugging.
</details>

### 6. All exceptions should be caught and the message sent to DLQ immediately.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Only permanent/non-recoverable errors should go to DLQ immediately. Transient errors (like network issues) should be retried first, with DLQ as the final fallback.
</details>

## Code Challenge

### 7. Complete this retry-with-DLQ implementation:

```csharp
async Task ProcessWithRetry<T>(
    ConsumeResult<string, T> message,
    Func<T, Task> processor,
    IProducer<string, T> dlqProducer,
    int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await processor(message.Value);
            return; // Success - exit the method
        }
        catch (Exception ex) when (attempt < ____________)
        {
            // Transient failure - calculate delay and retry
            var delay = TimeSpan.FromSeconds(Math._______(2, attempt));
            Console.WriteLine($"Retry {attempt}/{maxRetries} in {delay.TotalSeconds}s: {ex.Message}");
            await Task._______(delay);
        }
    }

    // All retries exhausted - send to DLQ
    var dlqHeaders = new Headers
    {
        { "x-error", Encoding.UTF8.GetBytes("Max retries exceeded") },
        { "x-original-topic", Encoding.UTF8.GetBytes(message._______) },
        { "x-retry-count", BitConverter.GetBytes(maxRetries) }
    };

    await dlqProducer.ProduceAsync(
        $"{message.Topic}.____________",  // DLQ topic name convention
        message.Key,
        message.Value,
        dlqHeaders);
}
```

<details>
<summary>Answer</summary>
```csharp
async Task ProcessWithRetry<T>(
    ConsumeResult<string, T> message,
    Func<T, Task> processor,
    IProducer<string, T> dlqProducer,
    int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await processor(message.Value);
            return; // Success - exit the method
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            // Transient failure - calculate delay and retry
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
            Console.WriteLine($"Retry {attempt}/{maxRetries} in {delay.TotalSeconds}s: {ex.Message}");
            await Task.Delay(delay);
        }
    }

    // All retries exhausted - send to DLQ
    var dlqHeaders = new Headers
    {
        { "x-error", Encoding.UTF8.GetBytes("Max retries exceeded") },
        { "x-original-topic", Encoding.UTF8.GetBytes(message.Topic) },
        { "x-retry-count", BitConverter.GetBytes(maxRetries) }
    };

    await dlqProducer.ProduceAsync(
        $"{message.Topic}.dlq",  // DLQ topic name convention
        message.Key,
        message.Value,
        dlqHeaders);
}
```

Key points:
- `attempt < maxRetries` - only catch exceptions before the last attempt
- `Math.Pow(2, attempt)` - exponential backoff (2^attempt seconds)
- `Task.Delay` - async wait between retries
- `message.Topic` - original topic for DLQ metadata
- `.dlq` suffix - common convention for dead letter queue topics
</details>

## Score
- 7/7: Excellent! You understand error handling patterns
- 5-6/7: Good grasp of retry and DLQ concepts
- 3-4/7: Review exponential backoff and DLQ patterns
- <3/7: Re-do the lesson focusing on failure types
