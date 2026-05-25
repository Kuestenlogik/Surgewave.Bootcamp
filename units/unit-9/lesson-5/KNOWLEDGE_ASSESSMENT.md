# Knowledge Assessment: Lesson 8.5 - Custom Connectors

## Multiple Choice

### 1. What is the correct order of the connector lifecycle?
- [ ] A) Start -> Configure -> Validate -> Poll -> Stop -> Cleanup
- [ ] B) Configure -> Validate -> Start -> Poll/Process -> Stop -> Cleanup
- [ ] C) Validate -> Configure -> Start -> Stop -> Poll -> Cleanup
- [ ] D) Start -> Poll -> Configure -> Validate -> Stop -> Cleanup

<details>
<summary>Answer</summary>
**B)** The correct lifecycle is: Configure (set up with config) -> Validate (check config is valid) -> Start (begin operation) -> Poll/Process (do the work) -> Stop (graceful shutdown) -> Cleanup (release resources).
</details>

### 2. What design pattern is used in ConnectorFactory for creating connectors based on type?
- [ ] A) Singleton Pattern
- [ ] B) Observer Pattern
- [ ] C) Factory Pattern
- [ ] D) Strategy Pattern

<details>
<summary>Answer</summary>
**C)** The Factory Pattern is used to create connector instances based on a type string (e.g., "http", "websocket", "mqtt"), allowing configuration-driven instantiation without the caller needing to know the concrete classes.
</details>

### 3. Which component of a custom connector handles configuration parsing and application?
- [ ] A) Task Manager
- [ ] B) Config Parser
- [ ] C) Health Check
- [ ] D) Error Handler

<details>
<summary>Answer</summary>
**B)** The Config Parser component is responsible for reading the configuration dictionary, validating values, and applying them to the connector's internal state.
</details>

## True/False

### 4. The GetHealth() method allows monitoring systems to check if a connector is functioning properly.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Health checks enable monitoring and alerting systems to detect when connectors are unhealthy, degraded, or failing, allowing for automated recovery or operator notification.
</details>

### 5. Custom sink connectors should always process records one at a time for reliability.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Custom sink connectors should implement batching (processing multiple records together) for better performance. The FlushAsync() method is specifically designed to flush accumulated batches.
</details>

## Code Challenge

### 6. What is missing from this custom source connector interface?

```csharp
public interface ICustomSourceConnector
{
    void Configure(Dictionary<string, string> config);
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
    ConnectorHealth GetHealth();
}
```

<details>
<summary>Answer</summary>
Two things are missing:

1. **ValidateAsync()** - A method to validate the configuration before starting, ensuring required settings are present and valid.

2. **OnRecord event** - A way to notify when records are produced (event-driven pattern for emitting data).

Fix:
```csharp
public interface ICustomSourceConnector
{
    void Configure(Dictionary<string, string> config);
    Task ValidateAsync();  // Missing!
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
    ConnectorHealth GetHealth();
    event Action<string, string, byte[]>? OnRecord;  // Missing!
}
```
</details>

### 7. What's wrong with this connector factory implementation?

```csharp
public class ConnectorFactory
{
    public ISourceConnector CreateSource(string type, Dictionary<string, string> config)
    {
        var connector = type switch
        {
            "http" => new HttpSourceConnector(),
            "websocket" => new WebSocketSourceConnector(),
            _ => null
        };
        connector.Configure(config);
        return connector;
    }
}
```

<details>
<summary>Answer</summary>
Two issues:

1. **Null reference exception** - When an unknown type is passed, the switch returns null, and then connector.Configure(config) throws a NullReferenceException.

2. **Silent failure** - Returning null for unknown types makes debugging difficult; it's better to throw a descriptive exception.

Fix:
```csharp
public class ConnectorFactory
{
    public ISourceConnector CreateSource(string type, Dictionary<string, string> config)
    {
        var connector = type switch
        {
            "http" => new HttpSourceConnector(),
            "websocket" => new WebSocketSourceConnector(),
            _ => throw new ArgumentException($"Unknown connector type: {type}")
        };
        connector.Configure(config);
        return connector;
    }
}
```
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
