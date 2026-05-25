# Knowledge Assessment: Lesson 7.5 - State Stores

## Multiple Choice

### 1. What is a KeyValueStore used for?
- [ ] A) Storing message offsets
- [ ] B) Storing current state with point lookups and range scans
- [ ] C) Storing window boundaries
- [ ] D) Storing consumer group metadata

<details>
<summary>Answer</summary>
**B)** KeyValueStore is used for storing current state with point lookup (get by key) and range scan (iterate over key ranges) access patterns. It's the most common state store type for maintaining aggregations.
</details>

### 2. How are state stores made fault-tolerant?
- [ ] A) They are stored in a database
- [ ] B) They are backed by changelog topics and restored on restart
- [ ] C) They are replicated to all brokers
- [ ] D) They are stored in memory only

<details>
<summary>Answer</summary>
**B)** State stores are backed by changelog topics. Every change to the store is written to a changelog topic. On restart, the store is restored by replaying the changelog, ensuring no state is lost.
</details>

### 3. What is an interactive query?
- [ ] A) A query that modifies state
- [ ] B) A query from outside the streams application to read state store data
- [ ] C) A query that joins tables
- [ ] D) A query that creates new topics

<details>
<summary>Answer</summary>
**B)** Interactive queries allow external applications to query state stores directly, without consuming from output topics. This enables building serving layers that expose current aggregation state via APIs.
</details>

## True/False

### 4. WindowStore stores state without any time dimension.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - WindowStore specifically stores time-windowed state. It supports queries by key AND time range, making it suitable for windowed aggregations. KeyValueStore is the one without a time dimension.
</details>

### 5. State stores are automatically restored from changelog topics when the application restarts.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - When a streams application restarts, state stores are automatically restored by replaying records from their changelog topics. This provides durability without requiring external databases.
</details>

## Code Challenge

### 6. Create a real-time analytics service that:
1. Counts page views per URL
2. Exposes the counts via an interactive query endpoint

```csharp
var builder = new StreamsBuilder("analytics");
var pageViews = builder.Stream<string, PageView>("page-views");  // keyed by URL

// TODO: Aggregate into state store and expose for queries
```

<details>
<summary>Answer</summary>
```csharp
var builder = new StreamsBuilder("analytics");
var pageViews = builder.Stream<string, PageView>("page-views");

// Create state store for page view counts
var pageViewCounts = pageViews
    .GroupByKey()  // Group by URL (the key)
    .Count(Materialized.As<string, long, KeyValueStore<string, long>>("page-view-counts"));

// Build the app
var app = builder.Build();
app.Start();

// Interactive query: Get count for a specific URL
public long GetPageViewCount(string url)
{
    var store = app.Store<ReadOnlyKeyValueStore<string, long>>("page-view-counts");
    return store.Get(url) ?? 0;
}

// Interactive query: Get all counts
public IEnumerable<KeyValuePair<string, long>> GetAllCounts()
{
    var store = app.Store<ReadOnlyKeyValueStore<string, long>>("page-view-counts");
    return store.All();
}

// Interactive query: Get top N URLs by count
public IEnumerable<KeyValuePair<string, long>> GetTopUrls(int n)
{
    var store = app.Store<ReadOnlyKeyValueStore<string, long>>("page-view-counts");
    return store.All()
                .OrderByDescending(kv => kv.Value)
                .Take(n);
}
```

**Expose via REST API:**
```csharp
app.MapGet("/analytics/{url}", (string url) =>
    new { Url = url, Views = GetPageViewCount(url) });

app.MapGet("/analytics/top/{n}", (int n) =>
    GetTopUrls(n).Select(kv => new { Url = kv.Key, Views = kv.Value }));
```
</details>

### 7. You need to track user session activity with 5-minute inactivity timeout. Design the state store solution:

```csharp
var builder = new StreamsBuilder("sessions");
var userEvents = builder.Stream<string, UserEvent>("user-events");  // keyed by userId

// TODO: Track sessions with SessionStore
```

<details>
<summary>Answer</summary>
```csharp
var builder = new StreamsBuilder("sessions");
var userEvents = builder.Stream<string, UserEvent>("user-events");

// Session window with 5-minute inactivity gap
var sessionWindow = SessionWindows.With(TimeSpan.FromMinutes(5));

// Aggregate user events into sessions
var sessions = userEvents
    .GroupByKey()
    .WindowedBy(sessionWindow)
    .Aggregate(
        // Initializer: Create empty session
        () => new UserSession { Events = new List<string>(), StartTime = null },

        // Aggregator: Add event to session
        (userId, evt, session) => new UserSession
        {
            UserId = userId,
            Events = session.Events.Append(evt.EventType).ToList(),
            StartTime = session.StartTime ?? evt.Timestamp,
            LastEventTime = evt.Timestamp,
            EventCount = session.EventCount + 1
        },

        // Merger: Combine sessions (when windows merge)
        (session1, session2) => new UserSession
        {
            UserId = session1.UserId,
            Events = session1.Events.Concat(session2.Events).ToList(),
            StartTime = Min(session1.StartTime, session2.StartTime),
            LastEventTime = Max(session1.LastEventTime, session2.LastEventTime),
            EventCount = session1.EventCount + session2.EventCount
        },

        // Materialized state store
        Materialized.As<string, UserSession, SessionStore<string, UserSession>>("user-sessions")
    );

// Output completed sessions
sessions.ToStream()
    .Filter((windowedKey, session) => session.EventCount > 0)
    .Map((windowedKey, session) => KeyValuePair.Create(
        session.UserId,
        new CompletedSession
        {
            UserId = session.UserId,
            SessionStart = windowedKey.Window.Start,
            SessionEnd = windowedKey.Window.End,
            Duration = windowedKey.Window.End - windowedKey.Window.Start,
            EventCount = session.EventCount,
            Events = session.Events
        }))
    .To("completed-sessions");

var app = builder.Build();
app.Start();

// Interactive query: Get active sessions
public UserSession GetActiveSession(string userId)
{
    var store = app.Store<ReadOnlySessionStore<string, UserSession>>("user-sessions");
    // Get the most recent session for this user
    return store.Fetch(userId).LastOrDefault().Value;
}
```

**Session store characteristics:**
1. Groups events by user within inactivity windows
2. Automatically merges sessions when new events arrive within gap
3. Sessions "close" after 5 minutes of inactivity
4. Can query active sessions via interactive queries
</details>

## Score
- 7/7: Excellent! You understand state stores
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
