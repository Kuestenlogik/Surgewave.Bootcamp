# Knowledge Assessment: Lesson 3.6 - Compacted Topics

## Multiple Choice

### 1. What does log compaction do?
- [ ] A) Compresses all messages for storage efficiency
- [ ] B) Keeps only the latest value for each key
- [ ] C) Deletes all messages older than a time limit
- [ ] D) Merges multiple partitions into one

<details>
<summary>Answer</summary>
**B)** Keeps only the latest value for each key. Log compaction removes older values for the same key, retaining only the most recent update for each unique key.
</details>

### 2. What is a tombstone in the context of compacted topics?
- [ ] A) A message marking the end of a partition
- [ ] B) A message with a null value that signals key deletion
- [ ] C) An error message
- [ ] D) A message header for debugging

<details>
<summary>Answer</summary>
**B)** A message with a null value that signals key deletion. After compaction, a tombstone causes the key to be completely removed from the log.
</details>

### 3. When would you use compacted topics instead of time-based retention?
- [ ] A) For event logs and metrics
- [ ] B) For state stores, changelogs, and configuration
- [ ] C) For high-throughput streams
- [ ] D) For temporary data

<details>
<summary>Answer</summary>
**B)** For state stores, changelogs, and configuration. Compacted topics are ideal when you need the "current state" of entities, like user profiles or config settings.
</details>

### 4. What happens to a key's data after a tombstone is written and compaction runs?
- [ ] A) The key's latest value is preserved
- [ ] B) The key is completely removed from the log
- [ ] C) The tombstone replaces all previous values
- [ ] D) An error is logged

<details>
<summary>Answer</summary>
**B)** The key is completely removed from the log. Tombstones mark keys for deletion, and after compaction (and the delete retention period), the key has no entries in the log.
</details>

## True/False

### 5. In a compacted topic, message ordering within a key is still guaranteed.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Compaction only removes older values for a key; it doesn't change the order. The latest value for each key is always the most recently produced value.
</details>

### 6. Tombstones are immediately removed from the log during compaction.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Tombstones are retained for a configurable period (DeleteRetention, default 24 hours) to allow downstream consumers to see the deletion before the tombstone is removed.
</details>

## Code Challenge

### 7. Complete this code for working with compacted topics:

```csharp
// Create a compacted topic for user profiles
await client.Topics.CreateAsync("user-profiles", new TopicConfig
{
    Partitions = 3,
    CleanupPolicy = CleanupPolicy.____________  // Enable compaction
});

// Producer for user updates
await using var producer = new SurgewaveProducer<string, UserProfile>(options =>
{
    options.BootstrapServers = "localhost:9092";
});

// Create/Update a user (key = user ID)
var user = new UserProfile { Name = "Alice", Email = "alice@example.com" };
await producer.ProduceAsync("user-profiles", "user-123", user);

// Update the user (same key, new value - old value will be compacted away)
user = user with { Email = "alice.new@example.com" };
await producer.ProduceAsync("user-profiles", "user-123", user);

// Delete the user (tombstone = null value)
await producer.ProduceAsync("user-profiles", "user-123", ____________);

// Consumer reading from compacted topic gets:
// - Latest value for each key (or no entry if tombstoned)
await using var consumer = new SurgewaveConsumer<string, UserProfile>(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.AutoOffsetReset = AutoOffsetReset.____________;  // Start from beginning
});
```

<details>
<summary>Answer</summary>
```csharp
// Create a compacted topic for user profiles
await client.Topics.CreateAsync("user-profiles", new TopicConfig
{
    Partitions = 3,
    CleanupPolicy = CleanupPolicy.Compact  // Enable compaction
});

// Producer for user updates
await using var producer = new SurgewaveProducer<string, UserProfile>(options =>
{
    options.BootstrapServers = "localhost:9092";
});

// Create/Update a user (key = user ID)
var user = new UserProfile { Name = "Alice", Email = "alice@example.com" };
await producer.ProduceAsync("user-profiles", "user-123", user);

// Update the user (same key, new value - old value will be compacted away)
user = user with { Email = "alice.new@example.com" };
await producer.ProduceAsync("user-profiles", "user-123", user);

// Delete the user (tombstone = null value)
await producer.ProduceAsync("user-profiles", "user-123", null);

// Consumer reading from compacted topic gets:
// - Latest value for each key (or no entry if tombstoned)
await using var consumer = new SurgewaveConsumer<string, UserProfile>(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;  // Start from beginning
});
```

Key points:
- `CleanupPolicy.Compact` enables log compaction
- Sending `null` as the value creates a tombstone
- Use `Earliest` to read all current state from a compacted topic
- After compaction, only the latest value per key remains
</details>

## Score
- 7/7: Excellent! You understand compacted topics
- 5-6/7: Good grasp of compaction concepts
- 3-4/7: Review tombstones and retention settings
- <3/7: Re-do the lesson focusing on use cases
