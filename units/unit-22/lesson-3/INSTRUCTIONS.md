# Lesson 9.3: Distributed Tracing - Instructions

## Step 1: Create an ActivitySource

Create a source to emit activities:

```csharp
using System.Diagnostics;

var activitySource = new ActivitySource("Surgewave.Application", "1.0.0");
```

## Step 2: Create Activities for Operations

Wrap operations in activities:

```csharp
using var activity = activitySource.StartActivity("ProduceMessage");
activity?.SetTag("messaging.system", "surgewave");
activity?.SetTag("messaging.destination", topic);
activity?.SetTag("messaging.destination_kind", "topic");

await producer.ProduceAsync(topic, key, value);

activity?.SetStatus(ActivityStatusCode.Ok);
```

## Step 3: Propagate Context Through Headers

Inject trace context into message headers:

```csharp
// Create headers with trace context
var headers = new Dictionary<string, string>();

if (Activity.Current != null)
{
    headers["traceparent"] = Activity.Current.Id!;
    headers["tracestate"] = Activity.Current.TraceStateString ?? "";
}
```

## Step 4: Extract Context on Consume

Extract and continue the trace:

```csharp
// Extract parent context from headers
var parentId = headers.TryGetValue("traceparent", out var tp) ? tp : null;

using var activity = activitySource.StartActivity(
    "ConsumeMessage",
    ActivityKind.Consumer,
    parentId ?? "");

activity?.SetTag("messaging.system", "surgewave");
activity?.SetTag("messaging.source", result.Topic);

// Process message...

activity?.SetStatus(ActivityStatusCode.Ok);
```

## Step 5: Create Nested Spans

Create child activities for sub-operations:

```csharp
using var parentActivity = activitySource.StartActivity("ProcessOrder");

// Child span for validation
using (var validationActivity = activitySource.StartActivity("ValidateOrder"))
{
    // Validate...
    validationActivity?.SetTag("order.valid", true);
}

// Child span for persistence
using (var saveActivity = activitySource.StartActivity("SaveOrder"))
{
    // Save...
    saveActivity?.SetTag("order.id", orderId);
}
```

## Step 6: Listen for Activities

Subscribe to activity events:

```csharp
var listener = new ActivityListener
{
    ShouldListenTo = source => source.Name == "Surgewave.Application",
    Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
        ActivitySamplingResult.AllDataAndRecorded,
    ActivityStarted = activity =>
    {
        Console.WriteLine($"Started: {activity.DisplayName} ({activity.Id})");
    },
    ActivityStopped = activity =>
    {
        Console.WriteLine($"Stopped: {activity.DisplayName} Duration={activity.Duration.TotalMilliseconds}ms");
    }
};

ActivitySource.AddActivityListener(listener);
```

## Verification

Run the completed solution:
```bash
cd completed
dotnet run
```

You should see:
- Parent-child activity relationships
- Trace context propagated through messages
- Complete trace visualization
