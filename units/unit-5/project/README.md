# Unit 7 Project: Real-Time Click-Stream Analytics

Build a real-time analytics pipeline that aggregates website click-stream data using Surgewave Streams API.

## Scenario

You're building a real-time analytics dashboard for an e-commerce website. You need to:

1. Process incoming click events (page views, clicks, conversions)
2. Aggregate metrics by page and user session
3. Calculate rolling statistics with tumbling and hopping windows
4. Detect high-traffic pages in real-time

## Requirements

### Functional Requirements

- [ ] Ingest click events with user, page, event type, and timestamp
- [ ] Count page views per page (tumbling 1-minute windows)
- [ ] Calculate click-through rates (CTR) per page
- [ ] Detect sessions using session windows (30-second gap)
- [ ] Identify top pages by traffic (hopping 5-min window, 1-min advance)
- [ ] Track conversion funnel (view -> click -> purchase)

### Technical Requirements

- [ ] Use `StreamsBuilder` to create the topology
- [ ] Apply `TumblingWindows.Of()` for fixed windows
- [ ] Apply `SessionWindows.With()` for user sessions
- [ ] Use `GroupByKey()` and `Count()` for aggregations
- [ ] Implement `Aggregate()` for custom statistics
- [ ] Output results using `Peek()` for debugging

## Click Event Schema

```csharp
public record ClickEvent(
    string EventId,
    string UserId,
    string SessionId,
    string PageUrl,
    string EventType,     // "view", "click", "add_to_cart", "purchase"
    DateTime Timestamp,
    Dictionary<string, string>? Metadata
);

public record PageStats(
    int ViewCount,
    int ClickCount,
    int ConversionCount,
    decimal ClickThroughRate  // clicks / views
);

public record SessionSummary(
    string UserId,
    int PageViews,
    int Clicks,
    bool Converted,
    TimeSpan Duration
);
```

## Expected Output

```
=== Real-Time Click-Stream Analytics ===

[Setup] Starting Surgewave Streams application...
[Setup] Topology created with 4 processing nodes

[Ingestion] Simulating 60 seconds of click-stream data...

[Analytics] Page Views (1-minute tumbling windows):
  /home @ 14:30: 45 views
  /products @ 14:30: 32 views
  /cart @ 14:30: 18 views
  /checkout @ 14:30: 8 views

[Analytics] Session Analysis (30-sec gap):
  User user-001: 12 pages, 5 clicks, converted=true, duration=45s
  User user-002: 8 pages, 3 clicks, converted=false, duration=28s
  User user-003: 15 pages, 7 clicks, converted=true, duration=52s

[Analytics] Top Pages (5-min rolling):
  1. /home - 180 views
  2. /products - 142 views
  3. /products/widget - 98 views
  4. /cart - 67 views
  5. /checkout - 31 views

[Analytics] Conversion Funnel:
  Views:        500
  Clicks:       175 (35.0% of views)
  Add to Cart:   52 (29.7% of clicks)
  Purchases:     18 (34.6% of cart)

[Analytics] Real-time Alerts:
  ALERT: /products page spike - 89 views in last minute (threshold: 50)

=== Analytics Pipeline Complete ===
```

## Getting Started

```bash
cd bootcamp/unit-7/project/start
dotnet run
```

## Hints

1. Use `stream.GroupBy(e => e.PageUrl)` to group by page
2. Use `TumblingWindows.Of(TimeSpan.FromMinutes(1))` for 1-minute windows
3. Use `SessionWindows.With(TimeSpan.FromSeconds(30))` for sessions
4. Use `Aggregate()` with custom initializer for PageStats
5. Chain `.Filter()` before aggregation to separate event types

## Stream Topology

```
                    click-events (input topic)
                           |
                     +-----+-----+
                     |           |
                  Branch      Branch
                 (views)     (clicks)
                     |           |
              GroupByKey    GroupByKey
                     |           |
             TumblingWindow   SessionWindow
                     |           |
                  Count       Aggregate
                     |           |
                 page-views   sessions
                 (output)    (output)
```

## Evaluation Criteria

| Criteria | Points |
|----------|--------|
| Stream topology is correct | 20 |
| Windowed aggregations work | 25 |
| Session detection accurate | 20 |
| Statistics calculated properly | 20 |
| Clean code structure | 15 |

## Solution

Compare your implementation with [completed/](completed/README.md) when done.
