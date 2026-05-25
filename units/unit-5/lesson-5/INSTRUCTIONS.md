# Lesson 7.5: State Stores - Instructions

## Step 1: KeyValue Store Basics

Create and access a key-value state store:

```csharp
// Aggregations automatically create state stores
var counts = stream
    .GroupByKey()
    .Count();  // Creates internal state store

// Or explicitly materialize with name
var counts = stream
    .GroupByKey()
    .Count(Materialized<string, long>.As("my-counts-store"));
```

## Step 2: Querying State Stores

Access state stores from the running application:

```csharp
var app = builder.Build();
app.Start();

// Get the state store
var store = app.GetStateStore<IReadOnlyKeyValueStore<string, long>>("my-counts-store");

// Point lookup
var count = store.Get("myKey");

// Iterate all
foreach (var kv in store.All())
{
    Console.WriteLine($"{kv.Key}: {kv.Value}");
}

// Range scan (requires ordered keys)
foreach (var kv in store.Range("a", "z"))
{
    Console.WriteLine($"{kv.Key}: {kv.Value}");
}
```

## Step 3: Window Store

Access windowed state:

```csharp
var windowedCounts = stream
    .GroupByKey()
    .WindowedBy(TumblingWindows.Of(TimeSpan.FromMinutes(5)))
    .Count(Materialized<string, long>.As("windowed-counts"));

// Query window store
var windowStore = app.GetStateStore<IReadOnlyWindowStore<string, long>>("windowed-counts");

// Fetch specific window
var count = windowStore.Fetch("myKey", windowStartMs);

// Fetch time range
var results = windowStore.Fetch("myKey", fromTime, toTime);
foreach (var kv in results)
{
    Console.WriteLine($"Window [{kv.Key.Window.StartMs}]: {kv.Value}");
}
```

## Step 4: Custom State Store

Register a custom state store:

```csharp
builder.AddStateStore(
    Stores.KeyValueStore<string, MyState>("my-custom-store")
);

// Access in processor
var store = context.GetStateStore<IKeyValueStore<string, MyState>>("my-custom-store");
store.Put("key", new MyState(...));
var state = store.Get("key");
```

## Step 5: Persistent vs In-Memory

Choose storage backend:

```csharp
// In-memory (default, fast, lost on restart)
Stores.KeyValueStore<string, long>("counts")

// Persistent (survives restarts)
Stores.PersistentKeyValueStore<string, long>(
    "counts",
    Serdes.String(),
    Serdes.Int64()
)
```

## Complete Example

```csharp
var builder = new StreamsBuilder("analytics-service");

// Count events by type
var eventCounts = builder.Stream<string, int>("events")
    .GroupByKey()
    .Count(Materialized<string, long>.As("event-counts"));

// Windowed counts
var hourlyCounts = builder.Stream<string, int>("events")
    .GroupByKey()
    .WindowedBy(TumblingWindows.Of(TimeSpan.FromHours(1)))
    .Count(Materialized<Windowed<string>, long>.As("hourly-counts"));

var app = builder.Build();
app.Start();

// Process events
app.ProcessRecord("events", "click", 1, ts);
app.ProcessRecord("events", "view", 1, ts);
app.ProcessRecord("events", "click", 1, ts);

// Query state
var counts = app.GetStateStore<IReadOnlyKeyValueStore<string, long>>("event-counts");
Console.WriteLine($"Clicks: {counts.Get("click")}");
Console.WriteLine($"Views: {counts.Get("view")}");
```

## Interactive Queries

Expose state via API:

```csharp
// REST API endpoint
app.MapGet("/counts/{key}", (string key) =>
{
    var store = streamsApp.GetStateStore<IReadOnlyKeyValueStore<string, long>>("counts");
    return store.Get(key);
});
```

## Exercises

1. **Event Counter**: Build a queryable event counter
2. **Time-Based Metrics**: Query windowed aggregations
3. **Real-time Dashboard**: Expose metrics via REST API
