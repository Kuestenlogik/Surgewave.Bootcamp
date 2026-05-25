# Lesson 9.3: Distributed Tracing

> **Difficulty:** Intermediate | **Duration:** 30 min | **Prerequisites:** Lesson 9.2

Implement correlation IDs and trace propagation across services.

## Objectives

- Understand distributed tracing concepts
- Use System.Diagnostics.Activity for trace propagation
- Propagate context through message headers

## Concepts

### Why Distributed Tracing?

In distributed systems, a single request may span multiple services. Tracing helps you:
- Follow requests across service boundaries
- Identify bottlenecks in multi-service flows
- Debug issues in complex architectures

### Key Terms

| Term | Description |
|------|-------------|
| Trace | End-to-end journey of a request |
| Span | A single operation within a trace |
| TraceId | Unique identifier for the entire trace |
| SpanId | Unique identifier for a single span |
| Parent SpanId | Links child spans to parents |

### System.Diagnostics.Activity

.NET provides built-in tracing support:

```csharp
using var activity = new Activity("ProduceMessage");
activity.SetTag("topic", "orders");
activity.Start();

// Your operation here

activity.Stop();
```

### Context Propagation

Pass trace context through message headers:

```csharp
// Producer: inject context
headers["traceparent"] = activity.Id;

// Consumer: extract context
var parentId = headers["traceparent"];
using var activity = new Activity("ConsumeMessage");
activity.SetParentId(parentId);
activity.Start();
```

## Exercise

In this lesson, you'll:
1. Create activities for produce/consume operations
2. Propagate trace context through headers
3. Build a trace visualization

## Key Takeaways

- Activities provide automatic parent-child relationships
- Use headers to propagate context across messages
- W3C Trace Context is the standard format
