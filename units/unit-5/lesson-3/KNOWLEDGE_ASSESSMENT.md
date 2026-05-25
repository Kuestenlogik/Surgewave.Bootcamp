# Knowledge Assessment: Lesson 7.3 - Aggregations & Windowing

## Multiple Choice

### 1. What must you do before aggregating a stream?
- [ ] A) Filter it
- [ ] B) Map it
- [ ] C) Group it by key
- [ ] D) Sort it

<details>
<summary>Answer</summary>
**C)** Before aggregation, you must group records by key using `GroupByKey()` (use existing key) or `GroupBy()` (specify a new key). Aggregations operate on groups of records with the same key.
</details>

### 2. What is the difference between tumbling and hopping windows?
- [ ] A) Tumbling windows overlap, hopping don't
- [ ] B) Hopping windows overlap, tumbling don't
- [ ] C) They are the same
- [ ] D) Tumbling windows are larger

<details>
<summary>Answer</summary>
**B)** Hopping windows overlap (they advance by less than their size), while tumbling windows are fixed and non-overlapping. Example: A 5-minute hopping window advancing every 2 minutes overlaps, while a 5-minute tumbling window doesn't overlap.
</details>

### 3. When would you use a session window?
- [ ] A) For fixed time intervals
- [ ] B) For tracking user sessions with inactivity gaps
- [ ] C) For hourly reports
- [ ] D) For moving averages

<details>
<summary>Answer</summary>
**B)** Session windows are gap-based and dynamic. They group events that occur close together, with a new session starting after an inactivity gap. Perfect for tracking user sessions where activity is irregular.
</details>

## True/False

### 4. The `Count()` aggregation returns a Stream.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - `Count()` returns a Table, not a Stream. Aggregations produce Tables because they maintain state (the current count per key). Tables represent the current state, while Streams represent events.
</details>

### 5. `Reduce()` and `Aggregate()` can produce different result types from the input.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - `Reduce()` combines values of the SAME type. `Aggregate()` can produce DIFFERENT types (custom aggregation). For example, Reduce can sum integers to an integer, while Aggregate can transform strings into a statistics object.
</details>

## Code Challenge

### 6. Build a real-time metrics system that:
1. Counts events per event type
2. Calculates 1-minute tumbling window aggregates
3. Outputs counts to a topic

```csharp
var builder = new StreamsBuilder("metrics-app");
var events = builder.Stream<string, Event>("events");
// TODO: Complete the aggregation
```

<details>
<summary>Answer</summary>
```csharp
var builder = new StreamsBuilder("metrics-app");
var events = builder.Stream<string, Event>("events");

// Task 1: Count events per event type (all time)
var countsByType = events
    .GroupBy((key, evt) => evt.EventType)  // Group by event type
    .Count();                               // Returns Table<string, long>

// Output all-time counts
countsByType.ToStream().To("event-counts");

// Task 2: 1-minute tumbling window aggregates
var windowedCounts = events
    .GroupBy((key, evt) => evt.EventType)
    .WindowedBy(TumblingWindows.Of(TimeSpan.FromMinutes(1)))
    .Count();

// Output windowed counts
windowedCounts
    .ToStream()
    .Map((windowedKey, count) => KeyValuePair.Create(
        $"{windowedKey.Key}:{windowedKey.Window.Start:HH:mm}",
        count))
    .To("event-counts-1min");

var app = builder.Build();
app.Start();
```

**Output format:**
- `event-counts`: `{"click": 1500, "view": 3200, ...}` (running totals)
- `event-counts-1min`: `{"click:14:05": 25, "click:14:06": 30, ...}` (per minute)
</details>

### 7. Explain the output for this windowing scenario:

```csharp
// Hopping window: 5 minutes size, 2 minute advance
var windowed = stream
    .GroupByKey()
    .WindowedBy(HoppingWindows.Of(TimeSpan.FromMinutes(5))
                              .AdvanceBy(TimeSpan.FromMinutes(2)))
    .Count();
```

Events arrive at: 14:00, 14:01, 14:03, 14:06

Which windows contain which events?

<details>
<summary>Answer</summary>
**Hopping Window Analysis:**

Window size: 5 minutes
Advance: 2 minutes

**Windows created:**
```
Window 1: [13:56 - 14:01) - contains: 14:00
Window 2: [13:58 - 14:03) - contains: 14:00, 14:01
Window 3: [14:00 - 14:05) - contains: 14:00, 14:01, 14:03
Window 4: [14:02 - 14:07) - contains: 14:03, 14:06
Window 5: [14:04 - 14:09) - contains: 14:06
Window 6: [14:06 - 14:11) - contains: 14:06
```

**Events per window:**
| Event Time | Windows it belongs to |
|------------|----------------------|
| 14:00 | W1, W2, W3 |
| 14:01 | W2, W3 |
| 14:03 | W3, W4 |
| 14:06 | W4, W5, W6 |

**Key insight:** Each event can appear in multiple overlapping windows. With a 5-minute window advancing every 2 minutes, most events appear in 2-3 windows.

**Count results (if all events have same key):**
```
W1 [13:56-14:01): count = 1
W2 [13:58-14:03): count = 2
W3 [14:00-14:05): count = 3  <- peak
W4 [14:02-14:07): count = 2
W5 [14:04-14:09): count = 1
W6 [14:06-14:11): count = 1
```
</details>

## Score
- 7/7: Excellent! You understand aggregations & windowing
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
