# Lesson 7.1: Streams Basics - Instructions

## Step 1: Create the StreamsBuilder

The StreamsBuilder is your entry point for defining stream processing logic:

```csharp
var builder = new StreamsBuilder("word-processor");
```

The application ID identifies your app for:
- Internal topic naming (repartition, changelog)
- Consumer group coordination
- State store directories

## Step 2: Define a Source Stream

Read from an input topic using `Stream<TKey, TValue>`:

```csharp
var textStream = builder.Stream<string, string>("text-input");
```

This creates a Stream that will consume all records from the topic.

## Step 3: Add Transformations

Chain operations to process the stream:

```csharp
var processedStream = textStream
    .Filter((key, value) => !string.IsNullOrEmpty(value))  // Remove empty
    .MapValues(value => value.ToUpperInvariant());          // Transform
```

Common Stream operations:
- `Filter` - Keep records matching predicate
- `MapValues` - Transform values (keeps key)
- `Map` - Transform both key and value
- `Peek` - Side effects without transformation

## Step 4: Write to Output

Send processed records to an output topic:

```csharp
processedStream.To("text-output");
```

Or use dynamic routing:

```csharp
processedStream.To((key, value) => value.Length > 100 ? "long-text" : "short-text");
```

## Step 5: Build and Run

Compile the topology and start processing:

```csharp
var app = builder.Build();
Console.WriteLine(app.Topology.Describe()); // Print topology
app.Start();
```

## Complete Example

```csharp
var builder = new StreamsBuilder("word-processor");

builder.Stream<string, string>("text-input")
    .Filter((key, value) => !string.IsNullOrEmpty(value))
    .MapValues(value => value.ToUpperInvariant())
    .Peek((key, value) => Console.WriteLine($"Processing: {key} -> {value}"))
    .To("text-output");

var app = builder.Build();
app.Start();

// Process some test records
app.ProcessRecord("text-input", "msg-1", "hello world", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
app.ProcessRecord("text-input", "msg-2", "", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
app.ProcessRecord("text-input", "msg-3", "surgewave streams", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

await app.CloseAsync();
```

## Exercises

1. **Basic Pipeline**: Complete the TODO items in `start/Program.cs`
2. **Add Logging**: Use `Peek` to log each record
3. **Dynamic Routing**: Route long messages to a different topic
