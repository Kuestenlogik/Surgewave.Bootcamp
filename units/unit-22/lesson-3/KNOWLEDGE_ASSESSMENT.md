# Knowledge Assessment: Lesson 9.3 - Distributed Tracing

## Multiple Choice

### 1. What uniquely identifies an entire end-to-end request across multiple services?
- [ ] A) SpanId
- [ ] B) TraceId
- [ ] C) Parent SpanId
- [ ] D) Correlation Key

<details>
<summary>Answer</summary>
**B)** TraceId is the unique identifier for the entire trace - the complete journey of a request across all services. Each service creates spans with their own SpanIds, but they all share the same TraceId.
</details>

### 2. How is trace context propagated between a Surgewave producer and consumer?
- [ ] A) Through shared memory
- [ ] B) Through message headers
- [ ] C) Through a central trace server
- [ ] D) Through environment variables

<details>
<summary>Answer</summary>
**B)** Trace context is propagated through message headers. The producer injects the traceparent header, and the consumer extracts it to continue the trace.
</details>

### 3. What is a Span in distributed tracing terminology?
- [ ] A) The entire journey of a request
- [ ] B) A single operation within a trace
- [ ] C) A network connection between services
- [ ] D) A log entry

<details>
<summary>Answer</summary>
**B)** A Span represents a single operation or unit of work within a trace. Examples include "ProduceMessage", "ProcessOrder", or "QueryDatabase". Spans can be nested (parent-child relationships).
</details>

## True/False

### 4. The W3C Trace Context is the standard format for trace propagation headers.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - W3C Trace Context defines the standard `traceparent` header format, enabling interoperability between different tracing systems and services.
</details>

### 5. Each service in a distributed system should generate its own TraceId.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Only the first service (entry point) generates the TraceId. All downstream services extract and reuse the same TraceId from the propagated context, ensuring the entire request can be traced as one unit.
</details>

## Code Challenge

### 6. What's wrong with this trace propagation implementation?

```csharp
// Producer
using var activity = new Activity("ProduceMessage");
activity.Start();
await producer.ProduceAsync(topic, key, value);
activity.Stop();
// Note: headers not set
```

<details>
<summary>Answer</summary>
The trace context is **not being propagated to the message headers**. The consumer won't be able to continue the trace because it has no way to know the activity ID.

Fix:
```csharp
// Producer
using var activity = new Activity("ProduceMessage");
activity.SetTag("topic", topic);
activity.Start();

// Inject trace context into message headers
var headers = new Dictionary<string, string>
{
    ["traceparent"] = activity.Id!
};

await producer.ProduceAsync(topic, key, value, headers);
activity.Stop();
```
</details>

### 7. Complete this consumer code to properly continue the trace:

```csharp
var message = await consumer.ConsumeAsync();
var parentId = message.Headers["traceparent"];

// How do you create an activity that continues this trace?
// Your code here:
```

<details>
<summary>Answer</summary>
Use SetParentId to link the new activity to the parent trace:

```csharp
var message = await consumer.ConsumeAsync();
var parentId = message.Headers["traceparent"];

// Create activity and link to parent trace
using var activity = new Activity("ConsumeMessage");
activity.SetParentId(parentId);  // Link to parent!
activity.SetTag("topic", message.Topic);
activity.SetTag("partition", message.Partition);
activity.Start();

try
{
    // Process the message
    await ProcessMessage(message);
}
finally
{
    activity.Stop();
}
```

Now this span will appear as a child of the producer's span in trace visualizations.
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
