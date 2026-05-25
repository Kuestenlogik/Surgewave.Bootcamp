# Lesson 7.4: Joins & Suppression

> **Difficulty:** Intermediate | **Duration:** 60 min | **Prerequisites:** Lesson 7.3

## Learning Objectives

- Understand join semantics in stream processing
- Implement stream-stream joins with windows
- Implement stream-table joins for enrichment
- Implement table-table joins for state correlation
- Use suppression to emit only final window results
- Control output frequency for aggregations

## Key Concepts

### Join Types

| Join | Left | Right | Window | Use Case |
|------|------|-------|--------|----------|
| Stream-Stream | Stream | Stream | Required | Correlate events |
| Stream-Table | Stream | Table | No | Enrich events |
| Table-Table | Table | Table | No | Join state |

### Join Semantics

```
Stream-Stream (Inner):
  Left:  A1----A2--------A3----
  Right: ----B1----B2----------
  Window: |--5min--|
  Output: (A1,B1), (A2,B1), (A2,B2)

Stream-Table:
  Stream: --e1--e2--e3--
  Table:  [user1=Alice, user2=Bob]
  Output: e1+Alice, e2+Bob, e3+???
```

### Suppression

By default, aggregations emit updates for every input record, which can create
a lot of downstream traffic. Suppression controls when and how results are
emitted:

```csharp
// Suppress until window closes - emit only final result
var finalCounts = events
    .GroupByKey()
    .WindowedBy(TumblingWindows.Of(TimeSpan.FromMinutes(5)))
    .Count()
    .Suppress(Suppressed<Windowed<string>>.UntilWindowClose(
        gracePeriod: TimeSpan.FromMinutes(1)
    ));
```

**Suppression Types:**

| Type | Behavior | Use Case |
|------|----------|----------|
| `UntilWindowClose` | Emit once when window closes | Final aggregation results |
| `UntilTimeLimit` | Buffer for duration, then emit | Rate limiting updates |

```
Without Suppression (every update emitted):
  Input:    A, B, C, D, E
  Output:   1, 2, 3, 4, 5  (5 outputs)

With UntilWindowClose:
  Input:    A, B, C, D, E
  Output:   5  (1 output when window closes)

With UntilTimeLimit (1 sec):
  Input:    A, B, C, D, E  (within 1 sec)
  Output:   5  (1 output after time limit)
```

### Combining Grace Period and Suppression

For accurate final results, combine grace period with suppression:

```csharp
var finalResults = metrics
    .GroupByKey()
    .WindowedBy(
        TumblingWindows
            .Of(TimeSpan.FromMinutes(5))
            .Grace(TimeSpan.FromMinutes(2))  // Wait for late records
    )
    .Aggregate(...)
    .Suppress(
        Suppressed<Windowed<string>>.UntilWindowClose(
            gracePeriod: TimeSpan.FromMinutes(2)  // Match window grace
        )
    );
```

## Exercise

Build an order enrichment pipeline:
1. Join orders with customer data (stream-table)
2. Correlate orders with payments (stream-stream)
3. Join customer and inventory tables
4. Use suppression to emit only final hourly revenue totals

## Time Estimate

~60 minutes
