# Lesson 3.2: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Create Headers

```csharp
using System.Text;
using Kuestenlogik.Surgewave.Client;

var correlationId = Guid.NewGuid().ToString();
var headers = new Headers
{
    { "x-correlation-id", Encoding.UTF8.GetBytes(correlationId) },
    { "x-source", Encoding.UTF8.GetBytes("order-service") },
    { "x-timestamp", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
};
```

## Step 2: Send Message with Headers

```csharp
await producer.ProduceAsync(
    topic: "events",
    key: "event-001",
    value: "Order created",
    headers: headers);
```

## Step 3: Read Headers from Consumed Messages

```csharp
var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
if (result != null)
{
    // Get correlation ID
    if (result.Headers.TryGetValue("x-correlation-id", out var correlationBytes))
    {
        var correlationId = Encoding.UTF8.GetString(correlationBytes);
        Console.WriteLine($"Correlation ID: {correlationId}");
    }

    // Iterate all headers
    foreach (var header in result.Headers)
    {
        var value = Encoding.UTF8.GetString(header.Value);
        Console.WriteLine($"  {header.Key}: {value}");
    }
}
```

## Step 4: Propagate Headers Through Pipeline

```csharp
// Consume from input topic
var input = await consumer.ConsumeAsync(timeout);

// Process and forward with same correlation ID
var outputHeaders = new Headers
{
    { "x-correlation-id", input.Headers["x-correlation-id"] },
    { "x-source", Encoding.UTF8.GetBytes("processor-service") }
};

await producer.ProduceAsync("output", key, processedValue, outputHeaders);
```

## Step 5: Create Helper Extension Methods

```csharp
public static class HeaderExtensions
{
    public static string? GetCorrelationId(this Headers headers)
    {
        if (headers.TryGetValue("x-correlation-id", out var bytes))
            return Encoding.UTF8.GetString(bytes);
        return null;
    }

    public static Headers WithCorrelationId(this Headers headers, string correlationId)
    {
        headers["x-correlation-id"] = Encoding.UTF8.GetBytes(correlationId);
        return headers;
    }
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. What's the difference between headers and message payload?
2. How do you propagate context in a multi-service pipeline?
3. What header format is used for W3C Trace Context?

---

**Next:** → [Lesson 3.3: Error Handling](../lesson-3/README.md)
