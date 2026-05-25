# Knowledge Assessment: Lesson 9.1 - Logging & Diagnostics

## Multiple Choice

### 1. What is the primary benefit of structured logging over plain text logging?
- [ ] A) Structured logs are smaller in size
- [ ] B) Structured logs capture both message and data for querying
- [ ] C) Structured logs are faster to write
- [ ] D) Structured logs don't require a logging framework

<details>
<summary>Answer</summary>
**B)** Structured logging captures both the message template and named data values, making logs searchable and queryable. For example, you can filter logs by Topic="orders" when using `logger.LogInformation("Produced to {Topic}", topic)`.
</details>

### 2. Which log level should be used for operation failures that need investigation?
- [ ] A) Debug
- [ ] B) Information
- [ ] C) Warning
- [ ] D) Error

<details>
<summary>Answer</summary>
**D)** Error level is for operation failures that require attention. Warning is for unexpected but handled situations, while Information is for normal operations.
</details>

### 3. How do you subscribe to diagnostic events from a specific Surgewave component?
- [ ] A) Use ILogger.Subscribe()
- [ ] B) Use DiagnosticListener.AllListeners.Subscribe()
- [ ] C) Use EventSource.Listen()
- [ ] D) Use Console.ReadLine()

<details>
<summary>Answer</summary>
**B)** DiagnosticListener.AllListeners.Subscribe() allows you to listen for diagnostic listeners by name (e.g., "Surgewave.Producer") and then subscribe to their events.
</details>

## True/False

### 4. The Trace log level should be used for normal application operations in production.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Trace is for very detailed diagnostics and is typically too verbose for production. Information level is appropriate for normal operations in production.
</details>

### 5. Named parameters in structured logging (like {Topic}) are captured as searchable data.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - When you use `logger.LogInformation("Message to {Topic}", topic)`, the Topic parameter is captured as structured data that can be filtered and queried in log aggregation systems.
</details>

## Code Challenge

### 6. What's wrong with this logging statement?

```csharp
logger.LogInformation("Produced " + count + " messages to " + topic + " in " + elapsed + "ms");
```

<details>
<summary>Answer</summary>
This uses **string concatenation instead of structured logging**. The data values (count, topic, elapsed) are embedded in the string and cannot be queried separately. Additionally, string concatenation allocates memory even when the log level is disabled.

Fix:
```csharp
logger.LogInformation("Produced {Count} messages to {Topic} in {Elapsed}ms", count, topic, elapsed);
```

Now Count, Topic, and Elapsed are captured as separate, queryable fields.
</details>

### 7. Complete this diagnostic event subscription to log producer events:

```csharp
DiagnosticListener.AllListeners.Subscribe(listener =>
{
    if (listener.Name == "Surgewave.Producer")
    {
        // What goes here?
    }
});
```

<details>
<summary>Answer</summary>
You need to subscribe to the specific listener with an observer that handles the diagnostic events:

```csharp
DiagnosticListener.AllListeners.Subscribe(listener =>
{
    if (listener.Name == "Surgewave.Producer")
    {
        listener.Subscribe(new ProducerDiagnosticObserver());
        // Or using a lambda:
        // listener.Subscribe(new Observer<KeyValuePair<string, object?>>(evt =>
        // {
        //     Console.WriteLine($"Event: {evt.Key}, Data: {evt.Value}");
        // }));
    }
});
```

The observer will receive diagnostic events emitted by the Surgewave Producer component.
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
