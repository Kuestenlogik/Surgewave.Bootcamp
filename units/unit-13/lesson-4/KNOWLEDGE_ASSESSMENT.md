# Knowledge Assessment: Lesson 13.4 - Middleware & Endpoints

## Multiple Choice

### 1. What is the purpose of correlation ID middleware?
- [ ] A) To encrypt messages
- [ ] B) To track requests across services by propagating a unique identifier
- [ ] C) To validate message schemas
- [ ] D) To compress response data

<details>
<summary>Answer</summary>
**B)** Correlation ID middleware extracts or generates a unique identifier for each request and propagates it through the system. This enables distributed tracing and makes it easier to debug issues across multiple services.
</details>

### 2. Where should correlation IDs be stored within a request so message handlers can access them?
- [ ] A) In a static variable
- [ ] B) In HttpContext.Items
- [ ] C) In the request body
- [ ] D) In a database

<details>
<summary>Answer</summary>
**B)** HttpContext.Items is the appropriate place to store request-scoped data like correlation IDs. It's accessible throughout the request pipeline and is automatically cleaned up when the request completes.
</details>

### 3. Which HTTP response header typically contains the correlation ID?
- [ ] A) Content-Type
- [ ] B) Authorization
- [ ] C) X-Correlation-ID
- [ ] D) Cache-Control

<details>
<summary>Answer</summary>
**C)** X-Correlation-ID is the conventional header name for correlation IDs. The middleware should add this header to responses so clients can correlate their requests with server-side logs.
</details>

## True/False

### 4. Minimal API endpoints can inject services directly as parameters without explicitly using HttpContext.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Minimal APIs support parameter injection. Services registered with DI can be injected directly as endpoint parameters, along with special types like HttpContext, CancellationToken, etc.
</details>

### 5. The X-Correlation-ID header should only be generated if the client doesn't provide one.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - If the client provides a correlation ID, it should be used to maintain traceability across the entire request chain. Only generate a new one if the client didn't provide one, ensuring end-to-end tracking.
</details>

## Code Challenge

### 6. What issues exist in this correlation middleware?

```csharp
public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private string _correlationId = Guid.NewGuid().ToString();

    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Items["CorrelationId"] = _correlationId;
        await _next(context);
    }
}
```

<details>
<summary>Answer</summary>
Critical issues:

1. **Static correlation ID**: `_correlationId` is set once in the constructor, so ALL requests get the SAME correlation ID (middleware is singleton)
2. **Doesn't check for client-provided ID**: Should use client's X-Correlation-ID if provided
3. **Doesn't add header to response**: Clients won't know the correlation ID
4. **Thread safety**: If `_correlationId` were ever modified, it would have race conditions

Fixed version:
```csharp
public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract from request or generate new
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        // Store for use during request
        context.Items["CorrelationId"] = correlationId;

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
```
</details>

### 7. Complete this Minimal API endpoint that produces a message with the correlation ID as a header:

```csharp
app.MapPost("/api/topics/{topic}/messages", async (
    string topic,
    MessageRequest request,
    SurgewaveProducer<string, string> producer,
    HttpContext context) =>
{
    // Get correlation ID from context
    // Produce message with correlation ID as a header
    // Return appropriate response
});
```

<details>
<summary>Answer</summary>
```csharp
app.MapPost("/api/topics/{topic}/messages", async (
    string topic,
    MessageRequest request,
    SurgewaveProducer<string, string> producer,
    HttpContext context) =>
{
    // Get correlation ID from context (set by middleware)
    var correlationId = context.Items["CorrelationId"]?.ToString()
        ?? Guid.NewGuid().ToString();

    // Create message headers with correlation ID
    var headers = new Dictionary<string, string>
    {
        ["X-Correlation-ID"] = correlationId
    };

    try
    {
        // Produce message with headers
        await producer.ProduceAsync(
            topic,
            request.Key,
            request.Value,
            headers);

        return Results.Created(
            $"/api/topics/{topic}/messages/{request.Key}",
            new
            {
                Success = true,
                CorrelationId = correlationId,
                Topic = topic,
                Key = request.Key
            });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            extensions: new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId
            });
    }
});

public record MessageRequest(string Key, string Value);
```
</details>

## Score
- 7/7: Excellent! You understand middleware and Minimal API endpoints
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding
