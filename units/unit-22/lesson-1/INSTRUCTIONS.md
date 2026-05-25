# Lesson 9.1: Logging & Diagnostics - Instructions

## Step 1: Create a Logger Factory

Configure logging with console output and custom formatting:

```csharp
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Debug)
        .AddSimpleConsole(options =>
        {
            options.TimestampFormat = "[HH:mm:ss] ";
            options.SingleLine = true;
            options.IncludeScopes = true;
        });
});

var logger = loggerFactory.CreateLogger<Program>();
```

## Step 2: Use Structured Logging

Log with named parameters for structured data:

```csharp
// Good - structured with named parameters
logger.LogInformation("Produced to {Topic} at partition {Partition}", topic, partition);

// Avoid - string interpolation loses structure
logger.LogInformation($"Produced to {topic} at partition {partition}");
```

## Step 3: Implement a Diagnostic Observer

Create an observer to capture diagnostic events:

```csharp
public class SurgewaveDiagnosticObserver : IObserver<DiagnosticListener>
{
    public void OnNext(DiagnosticListener listener)
    {
        if (listener.Name.StartsWith("Surgewave"))
        {
            listener.Subscribe(new EventObserver());
        }
    }

    public void OnError(Exception error) { }
    public void OnCompleted() { }
}

public class EventObserver : IObserver<KeyValuePair<string, object?>>
{
    public void OnNext(KeyValuePair<string, object?> pair)
    {
        Console.WriteLine($"[DIAG] {pair.Key}: {pair.Value}");
    }

    public void OnError(Exception error) { }
    public void OnCompleted() { }
}
```

## Step 4: Configure Log Filtering

Filter logs by category and level:

```csharp
builder.AddFilter("Surgewave.Producer", LogLevel.Debug)
       .AddFilter("Surgewave.Consumer", LogLevel.Information)
       .AddFilter("Microsoft", LogLevel.Warning);
```

## Step 5: Use Logging Scopes

Group related log entries with scopes:

```csharp
using (logger.BeginScope(new Dictionary<string, object>
{
    ["RequestId"] = Guid.NewGuid(),
    ["Topic"] = "orders"
}))
{
    logger.LogInformation("Processing started");
    // ... operations ...
    logger.LogInformation("Processing complete");
}
```

## Verification

Run the completed solution:
```bash
cd completed
dotnet run
```

You should see:
- Timestamped log entries with different levels
- Diagnostic events from Surgewave components
- Scoped log entries grouped by request
