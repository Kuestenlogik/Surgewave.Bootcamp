# Knowledge Assessment: Lesson 7.2 - Transformations

## Multiple Choice

### 1. What is the difference between `Map` and `MapValues`?
- [ ] A) Map is faster
- [ ] B) Map transforms key+value, MapValues transforms value only
- [ ] C) MapValues can change the key
- [ ] D) There is no difference

<details>
<summary>Answer</summary>
**B)** `Map` transforms both the key and value (can change either), while `MapValues` only transforms the value, keeping the key unchanged. MapValues is preferred when you don't need to change the key as it can be more efficient.
</details>

### 2. How many output records does `FlatMap` produce per input record?
- [ ] A) Always 1
- [ ] B) Always 0
- [ ] C) 0 to N (variable)
- [ ] D) Always 2

<details>
<summary>Answer</summary>
**C)** `FlatMap` can produce 0 to N output records per input record. It "expands" records - useful for splitting one record into multiple, or filtering by returning an empty collection.
</details>

### 3. What does the `Branch` operation do?
- [ ] A) Combines multiple streams
- [ ] B) Splits a stream into multiple streams based on predicates
- [ ] C) Creates a copy of the stream
- [ ] D) Removes duplicate records

<details>
<summary>Answer</summary>
**B)** `Branch` splits a single stream into multiple streams based on predicates. Each record goes to the first branch whose predicate matches. Useful for routing different record types to different processing paths.
</details>

## True/False

### 4. `Filter` is a stateful operation that maintains a cache of seen records.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - `Filter` is a stateless operation. It evaluates each record independently against the predicate without maintaining any state. Stateless operations process each record without reference to other records.
</details>

### 5. `Merge` combines multiple streams into a single stream.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - `Merge` combines multiple streams into one output stream. Records from all input streams appear in the merged output. This is the opposite of `Branch`.
</details>

## Code Challenge

### 6. Create a log processor that:
1. Filters out empty messages
2. Parses the log level (ERROR, WARN, INFO)
3. Routes to separate topics based on level

```csharp
var builder = new StreamsBuilder("log-processor");
var logs = builder.Stream<string, string>("raw-logs");
// TODO: Complete the processor
```

<details>
<summary>Answer</summary>
```csharp
var builder = new StreamsBuilder("log-processor");
var logs = builder.Stream<string, string>("raw-logs");

// Step 1: Filter out empty messages
var nonEmpty = logs.Filter((key, value) => !string.IsNullOrEmpty(value));

// Step 2: Branch by log level
var branches = nonEmpty.Branch(
    (key, value) => value.StartsWith("[ERROR]"),   // Branch 0: Errors
    (key, value) => value.StartsWith("[WARN]"),    // Branch 1: Warnings
    (key, value) => value.StartsWith("[INFO]"),    // Branch 2: Info
    (key, value) => true                            // Branch 3: Default (other)
);

// Step 3: Route to separate topics
branches[0].To("logs-error");
branches[1].To("logs-warn");
branches[2].To("logs-info");
branches[3].To("logs-other");

var app = builder.Build();
app.Start();
```

**Alternative with MapValues to enrich:**
```csharp
branches[0]
    .MapValues(v => $"{{\"level\":\"ERROR\",\"message\":\"{v}\"}}")
    .To("logs-error");
```
</details>

### 7. What's wrong with this transformation code?

```csharp
var stream = builder.Stream<string, Order>("orders");

// Transform to get total
var totals = stream.Map((key, order) =>
    KeyValuePair.Create(key, order.Total));

// Filter high value orders
var highValue = totals.Filter((key, total) =>
    total > 1000);

// Write to topic with order details
highValue.MapValues(total =>
    $"High value order: {total.OrderId}");  // Error!
```

<details>
<summary>Answer</summary>
**The issue:**

After the `Map` operation, `totals` is a `Stream<string, decimal>` (assuming `Total` is decimal). The stream no longer contains the full `Order` object - only the total amount.

Then in the final `MapValues`, the code tries to access `total.OrderId`, but `total` is a `decimal`, not an `Order` object.

**The fix - preserve the Order object:**

```csharp
var stream = builder.Stream<string, Order>("orders");

// Filter high value orders FIRST (while we still have the Order)
var highValue = stream.Filter((key, order) => order.Total > 1000);

// Then transform to the desired output format
var output = highValue.MapValues(order =>
    $"High value order: {order.OrderId}, Total: {order.Total}");

output.To("high-value-orders");
```

**Or if you need both the filter and a different structure:**

```csharp
var stream = builder.Stream<string, Order>("orders");

var output = stream
    .Filter((key, order) => order.Total > 1000)
    .Map((key, order) => KeyValuePair.Create(
        order.OrderId,
        new HighValueAlert(order.OrderId, order.Total, order.CustomerName)
    ));

output.To("high-value-alerts");
```

**Key lesson:** Be mindful of how transformations change the data type. Order of operations matters!
</details>

## Score
- 7/7: Excellent! You understand transformations
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
