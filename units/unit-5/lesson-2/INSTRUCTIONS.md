# Lesson 7.2: Transformations - Instructions

## Step 1: Map and MapValues

Transform records while keeping the same key:

```csharp
// MapValues - transform value only (key unchanged)
stream.MapValues(value => value.ToUpperInvariant())

// MapValues with key access
stream.MapValues((key, value) => $"[{key}] {value}")

// Map - transform both key and value
stream.Map((key, value) => KeyValue.Pair(
    key.ToUpperInvariant(),
    new ProcessedRecord(value, DateTime.UtcNow)
))
```

**When to use which:**
- `MapValues` - When key stays the same (no repartition needed)
- `Map` - When you need to change the key (may trigger repartition)

## Step 2: FlatMap and FlatMapValues

Expand one record into multiple:

```csharp
// Split text into words
stream.FlatMapValues(text => text.Split(' '))

// Expand with key transformation
stream.FlatMap((key, value) =>
    value.Split(',').Select(item => KeyValue.Pair(item, value)))
```

Use cases:
- Splitting delimited data
- Generating multiple events from one
- Unpacking arrays/collections

## Step 3: SelectKey

Change the message key:

```csharp
// Extract user ID from value as new key
stream.SelectKey((key, value) => value.UserId)
```

**Note:** Changing the key triggers repartitioning for downstream grouping/joins.

## Step 4: Branch

Split a stream into multiple branches:

```csharp
var branches = stream.Branch(
    (key, value) => value.Level == "ERROR",  // Branch 0
    (key, value) => value.Level == "WARN",   // Branch 1
    (key, value) => value.Level == "INFO"    // Branch 2
    // Remaining go to last branch if no match
);

var errorStream = branches[0];
var warnStream = branches[1];
var infoStream = branches[2];
```

Each branch is an independent Stream you can process separately.

## Step 5: Merge

Combine multiple streams into one:

```csharp
var combined = errorStream
    .Merge(warnStream)
    .Merge(infoStream);
```

Merge preserves ordering within each source stream but not across streams.

## Step 6: Repartition

Explicitly redistribute by key:

```csharp
// Repartition to internal topic
var repartitioned = stream.Repartition();

// Repartition with new key
var byUser = stream.Repartition(
    (key, value) => value.UserId
);
```

Use when:
- Downstream operations require co-partitioning
- Key has changed and you need grouping/joining

## Complete Example

```csharp
var builder = new StreamsBuilder("log-processor");

// Read raw logs
var logs = builder.Stream<string, string>("raw-logs");

// Parse and transform
var parsed = logs
    .MapValues(line => ParseLogEntry(line))
    .Filter((key, value) => value != null);

// Branch by severity
var branches = parsed.Branch(
    (key, log) => log!.Level == "ERROR",
    (key, log) => log!.Level == "WARN",
    (key, log) => log!.Level == "INFO"
);

// Process each branch
branches[0]
    .MapValues(log => $"[CRITICAL] {log!.Message}")
    .To("alerts");

branches[1]
    .MapValues(log => $"[WARNING] {log!.Message}")
    .To("warnings");

branches[2]
    .MapValues(log => log!.Message)
    .To("info-logs");

// Merge for unified processing
var all = branches[0].Merge(branches[1]).Merge(branches[2]);
all.To("all-logs");
```

## Exercises

1. **Log Parser**: Parse and route logs by level
2. **Word Splitter**: Use FlatMapValues to split sentences
3. **Key Extraction**: Use SelectKey to rekey by user ID
