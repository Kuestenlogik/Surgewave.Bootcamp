// Surgewave Bootcamp - Unit 9, Lesson 3: Distributed Tracing
// COMPLETED SOLUTION

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 9.3 - Distributed Tracing ===");
Console.WriteLine();

// ============== Demo 1: Create ActivitySource ==============
Console.WriteLine("--- Demo 1: Create ActivitySource ---");

var activitySource = new ActivitySource("Surgewave.Application", "1.0.0");
var traceCollector = new TraceCollector();

// Create listener to observe activities
using var listener = new ActivityListener
{
    ShouldListenTo = source => source.Name == "Surgewave.Application",
    Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
        ActivitySamplingResult.AllDataAndRecorded,
    ActivityStarted = activity =>
    {
        traceCollector.OnActivityStarted(activity);
        var indent = new string(' ', GetDepth(activity) * 2);
        Console.WriteLine($"  {indent}[START] {activity.DisplayName}");
    },
    ActivityStopped = activity =>
    {
        traceCollector.OnActivityStopped(activity);
        var indent = new string(' ', GetDepth(activity) * 2);
        Console.WriteLine($"  {indent}[STOP]  {activity.DisplayName} ({activity.Duration.TotalMilliseconds:F2}ms)");
    }
};

ActivitySource.AddActivityListener(listener);

Console.WriteLine("  ActivitySource 'Surgewave.Application' created");
Console.WriteLine("  ActivityListener registered");
Console.WriteLine();

// ============== Demo 2: Simple Activities ==============
Console.WriteLine("--- Demo 2: Simple Activities ---");

using (var activity = activitySource.StartActivity("SimpleOperation"))
{
    activity?.SetTag("operation.type", "test");
    await Task.Delay(50);
    activity?.SetStatus(ActivityStatusCode.Ok);
}

Console.WriteLine();

// ============== Demo 3: Nested Activities ==============
Console.WriteLine("--- Demo 3: Nested Activities ---");

using (var parentActivity = activitySource.StartActivity("ProcessOrder"))
{
    parentActivity?.SetTag("order.id", "ORD-123");

    // Child: Validate
    using (var validateActivity = activitySource.StartActivity("ValidateOrder"))
    {
        await Task.Delay(20);
        validateActivity?.SetTag("validation.passed", true);
        validateActivity?.SetStatus(ActivityStatusCode.Ok);
    }

    // Child: Save
    using (var saveActivity = activitySource.StartActivity("SaveOrder"))
    {
        await Task.Delay(30);
        saveActivity?.SetTag("database", "orders_db");
        saveActivity?.SetStatus(ActivityStatusCode.Ok);
    }

    // Child: Notify
    using (var notifyActivity = activitySource.StartActivity("NotifyCustomer"))
    {
        await Task.Delay(10);
        notifyActivity?.SetTag("notification.channel", "email");
        notifyActivity?.SetStatus(ActivityStatusCode.Ok);
    }

    parentActivity?.SetStatus(ActivityStatusCode.Ok);
}

Console.WriteLine();

// ============== Demo 4: Trace Propagation Through Messages ==============
Console.WriteLine("--- Demo 4: Trace Propagation Through Messages ---");

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

await using var producer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

await using var consumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "tracing-demo";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe("orders");

// Simulate producer service
using (var requestActivity = activitySource.StartActivity("HandleHttpRequest", ActivityKind.Server))
{
    requestActivity?.SetTag("http.method", "POST");
    requestActivity?.SetTag("http.url", "/api/orders");

    var orderId = $"ORD-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

    // Create message with trace context
    using (var produceActivity = activitySource.StartActivity("ProduceOrderEvent", ActivityKind.Producer))
    {
        produceActivity?.SetTag("messaging.system", "surgewave");
        produceActivity?.SetTag("messaging.destination", "orders");
        produceActivity?.SetTag("messaging.destination_kind", "topic");

        var message = new
        {
            orderId,
            customerId = "CUST-456",
            amount = 99.99,
            traceContext = new
            {
                traceParent = Activity.Current?.Id,
                traceState = Activity.Current?.TraceStateString
            }
        };

        var messageJson = JsonSerializer.Serialize(message);

        await producer.ProduceAsync(
            "orders",
            Encoding.UTF8.GetBytes(orderId),
            Encoding.UTF8.GetBytes(messageJson));

        produceActivity?.SetStatus(ActivityStatusCode.Ok);
    }

    requestActivity?.SetStatus(ActivityStatusCode.Ok);
}

Console.WriteLine();

// ============== Demo 5: Consumer Continues Trace ==============
Console.WriteLine("--- Demo 5: Consumer Continues Trace ---");

var consumeTimeout = DateTime.UtcNow.AddSeconds(2);

while (DateTime.UtcNow < consumeTimeout)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
    if (result != null)
    {
        var messageJson = Encoding.UTF8.GetString(result.Value ?? Array.Empty<byte>());
        var message = JsonSerializer.Deserialize<JsonElement>(messageJson);

        // Extract trace context from message
        string? parentId = null;
        if (message.TryGetProperty("traceContext", out var traceCtx) &&
            traceCtx.TryGetProperty("traceParent", out var tp))
        {
            parentId = tp.GetString();
        }

        // Continue trace with extracted parent
        using var consumeActivity = activitySource.StartActivity(
            "ConsumeOrderEvent",
            ActivityKind.Consumer,
            parentId ?? "");

        consumeActivity?.SetTag("messaging.system", "surgewave");
        consumeActivity?.SetTag("messaging.source", result.Topic);
        consumeActivity?.SetTag("messaging.message_id", Encoding.UTF8.GetString(result.Key ?? Array.Empty<byte>()));

        // Process the message
        using (var processActivity = activitySource.StartActivity("ProcessOrderEvent"))
        {
            await Task.Delay(25);

            if (message.TryGetProperty("orderId", out var oid))
            {
                processActivity?.SetTag("order.id", oid.GetString());
            }

            processActivity?.SetStatus(ActivityStatusCode.Ok);
        }

        consumeActivity?.SetStatus(ActivityStatusCode.Ok);
        break;
    }
}

Console.WriteLine();

// ============== Demo 6: Error Tracing ==============
Console.WriteLine("--- Demo 6: Error Tracing ---");

using (var errorActivity = activitySource.StartActivity("FailingOperation"))
{
    try
    {
        await Task.Delay(10);
        throw new InvalidOperationException("Simulated error for tracing demo");
    }
    catch (Exception ex)
    {
        errorActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        errorActivity?.SetTag("exception.type", ex.GetType().Name);
        errorActivity?.SetTag("exception.message", ex.Message);
        Console.WriteLine($"  Caught and traced error: {ex.Message}");
    }
}

Console.WriteLine();

// ============== Demo 7: Trace Visualization ==============
Console.WriteLine("--- Demo 7: Trace Visualization ---");
Console.WriteLine();

traceCollector.PrintTraceTree();

Console.WriteLine();

// Summary
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use ActivitySource to create spans/activities");
Console.WriteLine("2. Activities automatically form parent-child relationships");
Console.WriteLine("3. Propagate Activity.Current.Id through message headers");
Console.WriteLine("4. Extract parent ID on consumer and continue trace");
Console.WriteLine("5. Set tags and status for rich trace data");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Helper to calculate activity depth
static int GetDepth(Activity? activity)
{
    var depth = 0;
    var current = activity?.Parent;
    while (current != null)
    {
        depth++;
        current = current.Parent;
    }
    return depth;
}

// ============== Trace Collector ==============

public class TraceCollector
{
    private readonly List<TraceSpan> _spans = new();
    private readonly object _lock = new();

    public void OnActivityStarted(Activity activity)
    {
        lock (_lock)
        {
            _spans.Add(new TraceSpan
            {
                Id = activity.Id ?? "",
                ParentId = activity.ParentId,
                Name = activity.DisplayName,
                StartTime = activity.StartTimeUtc,
                TraceId = activity.TraceId.ToString()
            });
        }
    }

    public void OnActivityStopped(Activity activity)
    {
        lock (_lock)
        {
            var span = _spans.FirstOrDefault(s => s.Id == activity.Id);
            if (span != null)
            {
                span.Duration = activity.Duration;
                span.Status = activity.Status;
                foreach (var tag in activity.Tags)
                {
                    span.Tags[tag.Key] = tag.Value ?? "";
                }
            }
        }
    }

    public void PrintTraceTree()
    {
        lock (_lock)
        {
            var traces = _spans.GroupBy(s => s.TraceId).ToList();

            foreach (var trace in traces)
            {
                Console.WriteLine($"  Trace: {trace.Key[..16]}...");

                var rootSpans = trace.Where(s => string.IsNullOrEmpty(s.ParentId) ||
                    !trace.Any(t => t.Id == s.ParentId)).ToList();

                foreach (var root in rootSpans)
                {
                    PrintSpan(root, trace.ToList(), 2);
                }

                Console.WriteLine();
            }
        }
    }

    private void PrintSpan(TraceSpan span, List<TraceSpan> allSpans, int indent)
    {
        var prefix = new string(' ', indent);
        var statusIcon = span.Status == ActivityStatusCode.Error ? "X" : "V";

        Console.WriteLine($"{prefix}[{statusIcon}] {span.Name} ({span.Duration.TotalMilliseconds:F2}ms)");

        if (span.Tags.Count > 0)
        {
            var tagStr = string.Join(", ", span.Tags.Take(3).Select(t => $"{t.Key}={t.Value}"));
            Console.WriteLine($"{prefix}    Tags: {tagStr}");
        }

        var children = allSpans.Where(s => s.ParentId == span.Id).ToList();
        foreach (var child in children)
        {
            PrintSpan(child, allSpans, indent + 2);
        }
    }
}

public class TraceSpan
{
    public string Id { get; set; } = "";
    public string? ParentId { get; set; }
    public string TraceId { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime StartTime { get; set; }
    public TimeSpan Duration { get; set; }
    public ActivityStatusCode Status { get; set; }
    public Dictionary<string, string> Tags { get; } = new();
}
