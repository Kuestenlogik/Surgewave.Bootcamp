# Knowledge Assessment: Lesson 18.2 - Seek Operations

## Multiple Choice

### 1. What does `SeekToTimestamp(timestamp)` return?
- [ ] A) The exact offset with that timestamp
- [ ] B) The first offset with a timestamp greater than or equal to the requested time
- [ ] C) The last offset before the timestamp
- [ ] D) A random offset near the timestamp

<details>
<summary>Answer</summary>
**B)** SeekToTimestamp returns the first offset with a timestamp >= the requested time. If you seek to 10:10, you get the first message at or after 10:10.
</details>

### 2. Which seek operation would you use to reprocess all messages in a partition from the beginning?
- [ ] A) `Seek(topic, partition, latestOffset)`
- [ ] B) `Seek(topic, partition, 0)` or `SeekToBeginning()`
- [ ] C) `Seek(topic, partition, -1)`
- [ ] D) `SeekToEnd()`

<details>
<summary>Answer</summary>
**B)** `Seek(topic, partition, 0)` or calling `SeekToBeginning()` positions the consumer at the earliest available message, allowing you to reprocess all messages.
</details>

### 3. In what scenario would you use seek to skip past a specific offset?
- [ ] A) To process messages faster
- [ ] B) To skip corrupted or poison messages that cause deserialization errors
- [ ] C) To reduce storage usage
- [ ] D) To enable compression

<details>
<summary>Answer</summary>
**B)** Seek can be used to skip past corrupted or "poison" messages that cause processing errors. By catching the error and seeking to `currentOffset + 1`, you can continue processing without getting stuck.
</details>

## True/False

### 4. Offsets are globally unique across all partitions in a topic.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Offsets are partition-specific. Each partition has its own independent offset sequence starting from 0. Offset 100 in partition 0 is unrelated to offset 100 in partition 1.
</details>

### 5. After seeking to a new position, you must commit the new offset for the seek to persist across consumer restarts.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Seek only changes the in-memory position. If you want the new position to persist, you must commit it. Otherwise, on restart, the consumer will resume from the last committed offset, not the seek position.
</details>

## Code Challenge

### 6. What's wrong with this timestamp-based seek?
```csharp
var oneHourAgo = DateTime.Now.AddHours(-1);
consumer.Seek("orders", 0, oneHourAgo);
```

<details>
<summary>Answer</summary>
The Seek method takes an offset (long), not a DateTime. You need to first look up the offset for the timestamp:
```csharp
var oneHourAgo = DateTime.UtcNow.AddHours(-1);
var offset = await consumer.GetOffsetForTimestampAsync("orders", 0, oneHourAgo);
consumer.Seek("orders", 0, offset);
```
Also, use `DateTime.UtcNow` for consistency with how Surgewave stores timestamps.
</details>

### 7. Which use case is NOT appropriate for seek operations?
- [ ] A) Disaster recovery - replay from a specific point in time
- [ ] B) Skipping poison messages
- [ ] C) Improving producer throughput
- [ ] D) Resetting a consumer group to reprocess messages

<details>
<summary>Answer</summary>
**C)** Seek operations are consumer-side operations that control where consumption starts. They do not affect producer throughput. Seek is appropriate for replaying messages, skipping problems, and resetting consumer positions.
</details>

## Score
- 7/7: Excellent! You understand seek operations thoroughly
- 5-6/7: Good grasp of offset navigation
- 3-4/7: Review timestamp-based seek and use cases
- <3/7: Re-do the lesson focusing on the partition offset model
