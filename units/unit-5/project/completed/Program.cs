// Unit 7 Project: Real-Time Click-Stream Analytics - COMPLETED
// Aggregate website click-stream data using Surgewave Streams API

using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Streams;
using Kuestenlogik.Surgewave.Streams.Windows;
using Microsoft.Extensions.Logging;

Console.WriteLine("=== Real-Time Click-Stream Analytics ===");
Console.WriteLine();

// Configuration
const string InputTopic = "click-events";
var pageViewResults = new Dictionary<string, int>();
var sessionResults = new Dictionary<string, SessionSummary>();
var funnelMetrics = new FunnelMetrics(0, 0, 0, 0);

// Step 1 - Start embedded Surgewave broker
Console.WriteLine("[Setup] Starting Surgewave Streams application...");

await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

Console.WriteLine("[Setup] Topology created with 4 processing nodes");
Console.WriteLine();

// Step 2 - Create streams topology for page views (tumbling windows)
Console.WriteLine("[Analytics] Building page view aggregation topology...");

var pageViewBuilder = new StreamsBuilder { ApplicationId = "page-views" };

pageViewBuilder.Stream<string, string>(InputTopic)
    // Parse JSON to ClickEvent
    .MapValues(json => JsonSerializer.Deserialize<ClickEvent>(json)!)
    // Filter only view events
    .Filter((key, evt) => evt.EventType == "view")
    // Re-key by page URL
    .SelectKey((key, evt) => evt.PageUrl)
    // Group by page
    .GroupByKey()
    // Apply tumbling window of 1 minute
    .WindowedBy(TumblingWindows.Of(TimeSpan.FromMinutes(1)))
    // Count views per page per window
    .Count()
    // Output results
    .ToStream()
    .Peek((windowed, count) =>
    {
        var windowStart = DateTimeOffset.FromUnixTimeMilliseconds(windowed.Window.StartMs);
        Console.WriteLine($"    {windowed.Key} @ {windowStart:HH:mm}: {count} views");
        pageViewResults[windowed.Key] = (int)count;
    });

// Step 3 - Create streams topology for session analysis
Console.WriteLine("[Analytics] Building session analysis topology...");

var sessionBuilder = new StreamsBuilder { ApplicationId = "sessions" };

sessionBuilder.Stream<string, string>(InputTopic)
    .MapValues(json => JsonSerializer.Deserialize<ClickEvent>(json)!)
    // Key by user ID for session tracking
    .SelectKey((key, evt) => evt.UserId)
    .GroupByKey()
    // Session windows with 30-second gap
    .WindowedBy(SessionWindows.With(TimeSpan.FromSeconds(30)))
    // Aggregate into session summary
    .Aggregate(
        () => new SessionSummary(0, 0, false, long.MaxValue, 0),
        (userId, evt, session) => new SessionSummary(
            PageViews: session.PageViews + (evt.EventType == "view" ? 1 : 0),
            Clicks: session.Clicks + (evt.EventType == "click" ? 1 : 0),
            Converted: session.Converted || evt.EventType == "purchase",
            FirstEventMs: Math.Min(session.FirstEventMs, evt.TimestampMs),
            LastEventMs: Math.Max(session.LastEventMs, evt.TimestampMs)
        ),
        (userId, left, right) => new SessionSummary(
            PageViews: left.PageViews + right.PageViews,
            Clicks: left.Clicks + right.Clicks,
            Converted: left.Converted || right.Converted,
            FirstEventMs: Math.Min(left.FirstEventMs, right.FirstEventMs),
            LastEventMs: Math.Max(left.LastEventMs, right.LastEventMs)
        )
    )
    .ToStream()
    .Peek((windowed, session) =>
    {
        sessionResults[windowed.Key] = session;
    });

// Step 4 - Create streams topology for conversion funnel
Console.WriteLine("[Analytics] Building conversion funnel topology...");

var funnelBuilder = new StreamsBuilder { ApplicationId = "funnel" };

funnelBuilder.Stream<string, string>(InputTopic)
    .MapValues(json => JsonSerializer.Deserialize<ClickEvent>(json)!)
    // Group all events together
    .SelectKey((key, evt) => "global")
    .GroupByKey()
    // Aggregate into funnel metrics
    .Aggregate(
        () => new FunnelMetrics(0, 0, 0, 0),
        (key, evt, metrics) => evt.EventType switch
        {
            "view" => metrics with { Views = metrics.Views + 1 },
            "click" => metrics with { Clicks = metrics.Clicks + 1 },
            "add_to_cart" => metrics with { AddToCarts = metrics.AddToCarts + 1 },
            "purchase" => metrics with { Purchases = metrics.Purchases + 1 },
            _ => metrics
        }
    )
    .ToStream()
    .Peek((key, metrics) => funnelMetrics = metrics);

// Step 5 - Build and start topologies
var pageViewTopology = pageViewBuilder.Build();
var sessionTopology = sessionBuilder.Build();
var funnelTopology = funnelBuilder.Build();

var pageViewConfig = new StreamsConfig { ApplicationId = "page-views", BootstrapServers = $"localhost:{runtime.Port}" };
var sessionConfig = new StreamsConfig { ApplicationId = "sessions", BootstrapServers = $"localhost:{runtime.Port}" };
var funnelConfig = new StreamsConfig { ApplicationId = "funnel", BootstrapServers = $"localhost:{runtime.Port}" };

await using var pageViewApp = new StreamsApplication(pageViewConfig, pageViewTopology, loggerFactory);
await using var sessionApp = new StreamsApplication(sessionConfig, sessionTopology, loggerFactory);
await using var funnelApp = new StreamsApplication(funnelConfig, funnelTopology, loggerFactory);

pageViewApp.Start();
sessionApp.Start();
funnelApp.Start();

Console.WriteLine();

// Step 6 - Generate sample click events
Console.WriteLine("[Ingestion] Simulating click-stream data for 10 users...");
Console.WriteLine();

var random = new Random(42);
var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
var pages = new[] { "/home", "/products", "/products/widget", "/products/gadget", "/cart", "/checkout" };
var eventTypes = new[] { "view", "view", "view", "click", "click", "add_to_cart", "purchase" }; // Weighted

Console.WriteLine("[Analytics] Page Views (1-minute tumbling windows):");

// Generate events for multiple users
for (int userNum = 1; userNum <= 10; userNum++)
{
    var userId = $"user-{userNum:D3}";
    var sessionId = $"session-{userNum:D3}";
    var eventCount = random.Next(5, 15);

    for (int i = 0; i < eventCount; i++)
    {
        var evt = new ClickEvent(
            EventId: Guid.NewGuid().ToString(),
            UserId: userId,
            SessionId: sessionId,
            PageUrl: pages[random.Next(pages.Length)],
            EventType: eventTypes[random.Next(eventTypes.Length)],
            TimestampMs: baseTime + (userNum * 1000) + (i * random.Next(100, 5000))
        );

        // Send event to streams applications
        var json = JsonSerializer.Serialize(evt);
        pageViewApp.ProcessRecord(InputTopic, evt.UserId, json, evt.TimestampMs);
        sessionApp.ProcessRecord(InputTopic, evt.UserId, json, evt.TimestampMs);
        funnelApp.ProcessRecord(InputTopic, evt.UserId, json, evt.TimestampMs);
    }
}

// Close applications
await pageViewApp.CloseAsync();
await sessionApp.CloseAsync();
await funnelApp.CloseAsync();

Console.WriteLine();

// Step 7 - Display results
Console.WriteLine("[Analytics] Top Pages by Views:");
var topPages = pageViewResults.OrderByDescending(x => x.Value).Take(5).ToList();
for (int i = 0; i < topPages.Count; i++)
{
    var (page, count) = topPages[i];
    Console.WriteLine($"  {i + 1}. {page} - {count} views");
}
Console.WriteLine();

Console.WriteLine("[Analytics] Session Analysis (30-sec gap):");
foreach (var (userId, session) in sessionResults.OrderByDescending(x => x.Value.PageViews).Take(5))
{
    Console.WriteLine($"  User {userId}: {session.PageViews} pages, {session.Clicks} clicks, converted={session.Converted}, duration={session.Duration.TotalSeconds:F0}s");
}
Console.WriteLine();

Console.WriteLine("[Analytics] Conversion Funnel:");
Console.WriteLine($"  Views:        {funnelMetrics.Views}");
if (funnelMetrics.Views > 0)
{
    var clickRate = (decimal)funnelMetrics.Clicks / funnelMetrics.Views * 100;
    Console.WriteLine($"  Clicks:       {funnelMetrics.Clicks} ({clickRate:F1}% of views)");
}
if (funnelMetrics.Clicks > 0)
{
    var cartRate = (decimal)funnelMetrics.AddToCarts / funnelMetrics.Clicks * 100;
    Console.WriteLine($"  Add to Cart:  {funnelMetrics.AddToCarts} ({cartRate:F1}% of clicks)");
}
if (funnelMetrics.AddToCarts > 0)
{
    var purchaseRate = (decimal)funnelMetrics.Purchases / funnelMetrics.AddToCarts * 100;
    Console.WriteLine($"  Purchases:    {funnelMetrics.Purchases} ({purchaseRate:F1}% of cart)");
}
Console.WriteLine();

// Detect alerts
var alertThreshold = 8;
var highTrafficPages = pageViewResults.Where(x => x.Value > alertThreshold).ToList();
if (highTrafficPages.Any())
{
    Console.WriteLine("[Analytics] Real-time Alerts:");
    foreach (var (page, count) in highTrafficPages)
    {
        Console.WriteLine($"  ALERT: {page} page spike - {count} views in window (threshold: {alertThreshold})");
    }
    Console.WriteLine();
}

Console.WriteLine("=== Analytics Pipeline Complete ===");

#region Data Types

// Click event from the website
public record ClickEvent(
    string EventId,
    string UserId,
    string SessionId,
    string PageUrl,
    string EventType,     // "view", "click", "add_to_cart", "purchase"
    long TimestampMs
);

// Aggregated page statistics
public record PageStats(
    int ViewCount,
    int ClickCount,
    int AddToCartCount,
    int PurchaseCount
)
{
    public decimal ClickThroughRate => ViewCount > 0 ? (decimal)ClickCount / ViewCount * 100 : 0;
    public decimal ConversionRate => ClickCount > 0 ? (decimal)PurchaseCount / ClickCount * 100 : 0;
}

// User session summary
public record SessionSummary(
    int PageViews,
    int Clicks,
    bool Converted,
    long FirstEventMs,
    long LastEventMs
)
{
    public TimeSpan Duration => TimeSpan.FromMilliseconds(LastEventMs - FirstEventMs);
}

// Funnel metrics
public record FunnelMetrics(
    int Views,
    int Clicks,
    int AddToCarts,
    int Purchases
);

#endregion
