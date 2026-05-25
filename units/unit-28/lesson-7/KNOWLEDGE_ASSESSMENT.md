# Knowledge Assessment: Lesson 3.7 - Ephemeral Topics

## Multiple Choice

### 1. What is the primary characteristic of an ephemeral topic?
- [ ] A) Messages are compressed for storage efficiency
- [ ] B) Messages are stored in a ring buffer without persistence
- [ ] C) Messages are replicated across all brokers
- [ ] D) Messages are encrypted at rest

<details>
<summary>Answer</summary>
**B)** Messages are stored in a ring buffer without persistence. Ephemeral topics use a fixed-size ring buffer that wraps around when full, evicting the oldest messages. No data survives a broker restart.
</details>

### 2. What happens when the ring buffer of an ephemeral topic is full?
- [ ] A) The producer receives an error and stops
- [ ] B) The oldest messages are evicted and LogStartOffset advances
- [ ] C) The buffer automatically doubles in size
- [ ] D) Messages are spilled to disk

<details>
<summary>Answer</summary>
**B)** The oldest messages are evicted and LogStartOffset advances. The ring buffer wraps around, overwriting the oldest data to make room for new messages.
</details>

### 3. Which scenario is best suited for ephemeral topics?
- [ ] A) Audit logs that must be retained for 7 years
- [ ] B) Real-time sensor telemetry for a live dashboard
- [ ] C) Order processing in an e-commerce system
- [ ] D) Event sourcing for a banking application

<details>
<summary>Answer</summary>
**B)** Real-time sensor telemetry for a live dashboard. Ephemeral topics are ideal when only the latest data matters and historical replay is not required.
</details>

### 4. What is the default ring-buffer size for ephemeral topics?
- [ ] A) 1 MB
- [ ] B) 16 MB
- [ ] C) 64 MB
- [ ] D) 256 MB

<details>
<summary>Answer</summary>
**C)** 64 MB. The default `ephemeral.buffer.bytes` is 64 MB (67,108,864 bytes). This can be configured per topic.
</details>

## True/False

### 5. Ephemeral topic data survives a broker restart.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Ephemeral topics have no persistence. All buffered data is lost when the broker restarts. This is by design for use cases where only real-time data matters.
</details>

### 6. A consumer joining an ephemeral topic can replay the full message history from offset 0.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - A consumer can only read messages within the current ring-buffer window (from LogStartOffset to HighWatermark). Messages evicted by wrap-around are permanently gone.
</details>

### 7. Retention and compaction workers skip ephemeral topics.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Ephemeral topics manage their own lifecycle through the ring buffer. The retention and compaction background workers detect the Ephemeral cleanup policy and skip these topics entirely.
</details>

## Code Challenge

### 8. Complete this code to create and use an ephemeral topic:

```csharp
// Create an ephemeral topic with a 256 MB buffer
await nativeClient.Topics.Create("live-events")
    .WithPartitions(2)
    .WithConfig("____________", "____________")  // Set cleanup policy
    .WithConfig("____________", "____________")  // Set buffer size
    .ExecuteAsync();

// Consumer for real-time events
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    opt.GroupId = "dashboard";
    opt.AutoOffsetReset = AutoOffsetReset.____________;  // Best for ephemeral
});
```

<details>
<summary>Answer</summary>

```csharp
// Create an ephemeral topic with a 256 MB buffer
await nativeClient.Topics.Create("live-events")
    .WithPartitions(2)
    .WithConfig("cleanup.policy", "ephemeral")        // Set cleanup policy
    .WithConfig("ephemeral.buffer.bytes", "256MB")     // Set buffer size
    .ExecuteAsync();

// Consumer for real-time events
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    opt.GroupId = "dashboard";
    opt.AutoOffsetReset = AutoOffsetReset.Latest;      // Best for ephemeral
});
```

Key points:
- `cleanup.policy` set to `ephemeral` enables ring-buffer mode
- `ephemeral.buffer.bytes` accepts human-readable sizes like `256MB`
- `AutoOffsetReset.Latest` is recommended for ephemeral topics since historical replay is limited to the buffer window
- The buffer size should be chosen based on expected throughput and how much recent data consumers need
</details>

## Score
- 8/8: Excellent! You understand ephemeral topics and ring-buffer semantics
- 6-7/8: Good grasp of ephemeral concepts
- 4-5/8: Review the ring-buffer architecture and use case guidelines
- <4/8: Re-do the lesson focusing on when to use ephemeral vs persistent topics
