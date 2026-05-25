# Lesson 9.1: Logging & Diagnostics

> **Difficulty:** Intermediate | **Duration:** 30 min | **Prerequisites:** Unit 8 completion

Configure logging and diagnostic events in Surgewave applications.

## Objectives

- Configure structured logging with Microsoft.Extensions.Logging
- Subscribe to diagnostic events from Surgewave components
- Implement custom log formatting and filtering

## Concepts

### Structured Logging

Surgewave uses `ILogger` for all logging operations. Structured logging captures both message and data:

```csharp
logger.LogInformation("Produced message to {Topic} at offset {Offset}", topic, offset);
```

### Log Levels

| Level | When to Use |
|-------|-------------|
| Trace | Very detailed diagnostics |
| Debug | Development-time debugging |
| Information | Normal operations |
| Warning | Unexpected but handled |
| Error | Operation failures |
| Critical | Application crashes |

### Diagnostic Sources

Surgewave components emit diagnostic events you can subscribe to:

```csharp
DiagnosticListener.AllListeners.Subscribe(listener =>
{
    if (listener.Name == "Surgewave.Producer")
    {
        listener.Subscribe(new ProducerDiagnosticObserver());
    }
});
```

## Exercise

In this lesson, you'll:
1. Configure a logger with custom formatting
2. Subscribe to Surgewave diagnostic events
3. Implement log filtering by category

## Key Takeaways

- Use structured logging with named parameters
- Set appropriate log levels for different environments
- Diagnostic events provide detailed component insights
