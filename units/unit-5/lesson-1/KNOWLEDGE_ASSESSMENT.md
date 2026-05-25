# Knowledge Assessment: Lesson 7.1 - Streams Basics

## Multiple Choice

### 1. What is the StreamsBuilder used for?
- [ ] A) Building database queries
- [ ] B) Building stream processing topologies
- [ ] C) Building REST APIs
- [ ] D) Building configuration files

<details>
<summary>Answer</summary>
**B)** StreamsBuilder is the entry point for building stream processing applications. It creates the topology (processing graph) that defines how data flows from sources through processors to sinks.
</details>

### 2. What does a Stream represent in Surgewave Streams?
- [ ] A) A database table
- [ ] B) A fixed collection of records
- [ ] C) An unbounded stream of records
- [ ] D) A batch of messages

<details>
<summary>Answer</summary>
**C)** A Stream represents an unbounded stream of records where each record is an independent event. Records are processed in order per partition, and stateless operations don't maintain any state.
</details>

### 3. What are the three types of nodes in a topology?
- [ ] A) Input, Process, Output
- [ ] B) Source, Processor, Sink
- [ ] C) Read, Transform, Write
- [ ] D) Start, Middle, End

<details>
<summary>Answer</summary>
**B)** A topology consists of Source nodes (read from topics), Processor nodes (transform data), and Sink nodes (write to topics). These form the processing graph.
</details>

## True/False

### 4. Records in a Stream are processed in order across all partitions.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Records are processed in order PER PARTITION, not across all partitions. Each partition maintains its own ordering, but there's no global order across partitions.
</details>

### 5. You must call `Build()` and `Start()` to run a streams application.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - After defining the topology with StreamsBuilder, you call `Build()` to create the application and `Start()` to begin processing.
</details>

## Code Challenge

### 6. Complete this streams application that reads from "input-topic", filters out null values, and writes to "output-topic":

```csharp
var builder = new StreamsBuilder("my-app");
// TODO: Complete the topology
```

<details>
<summary>Answer</summary>
```csharp
var builder = new StreamsBuilder("my-app");

// Read from input topic
var stream = builder.Stream<string, string>("input-topic");

// Filter out null values
var filtered = stream.Filter((key, value) => value != null);

// Write to output topic
filtered.To("output-topic");

// Build and start
var app = builder.Build();
app.Start();
```

Key points:
1. `Stream<K,V>()` creates a source from a topic
2. `Filter()` removes records that don't match the predicate
3. `To()` writes to an output topic
4. `Build()` creates the app, `Start()` runs it
</details>

### 7. What is the difference between these two approaches?

**Approach A:**
```csharp
stream.Filter(x => x.Value != null).To("output");
```

**Approach B:**
```csharp
stream.To("output");
```

<details>
<summary>Answer</summary>
**Differences:**

**Approach A (with Filter):**
- Only non-null values are written to output
- Adds a processor node to the topology
- Records with null values are dropped
- Topology: Source -> Filter -> Sink

**Approach B (direct):**
- ALL records are written to output, including nulls
- Simpler topology with less processing
- Null values passed through unchanged
- Topology: Source -> Sink

**When to use which:**
- Use Filter when you need to exclude certain records
- Use direct `To()` when all records should pass through
- Filter is a stateless operation with minimal overhead

**Topology visualization:**
```
Approach A:                Approach B:
[input-topic]              [input-topic]
     |                          |
  Filter                       |
     |                          |
[output-topic]             [output-topic]
```
</details>

## Score
- 7/7: Excellent! You understand Streams basics
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
