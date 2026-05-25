# Quiz: Lesson 1.3 - Consuming Messages

## Multiple Choice

### 1. What is the difference between Subscribe and Assign in Surgewave?
- [ ] A) Subscribe is faster, Assign is more reliable
- [ ] B) Subscribe auto-discovers partitions, Assign manually specifies partitions
- [ ] C) Subscribe is for groups, Assign is for single consumers
- [ ] D) Subscribe is synchronous, Assign is asynchronous

<details>
<summary>Answer</summary>
**B)** Subscribe auto-discovers partitions, Assign manually specifies partitions. Subscribe automatically consumes from all partitions of a topic, while Assign gives you explicit control over which partition(s) to consume.
</details>

### 2. What does `AutoOffsetReset.Earliest` mean?
- [ ] A) Reset offset to 0 after each message
- [ ] B) Start consuming from the first message in the topic
- [ ] C) Use the earliest available consumer group
- [ ] D) Reset consumer position when errors occur

<details>
<summary>Answer</summary>
**B)** Start consuming from the first message in the topic. When a consumer starts with no committed offset, Earliest means it will begin from offset 0 (the oldest available message).
</details>

### 3. Which properties does ConsumeResult contain?
- [ ] A) Only Key and Value
- [ ] B) Topic, Partition, Offset, Timestamp, Key, Value, and Headers
- [ ] C) Key, Value, and MessageId
- [ ] D) Topic, Key, Value, and GroupId

<details>
<summary>Answer</summary>
**B)** Topic, Partition, Offset, Timestamp, Key, Value, and Headers. ConsumeResult provides complete metadata about the consumed message.
</details>

### 4. When would you use `AutoOffsetReset.Latest` instead of `Earliest`?
- [ ] A) When you want to process all historical messages
- [ ] B) When you only care about new messages from this point forward
- [ ] C) When running benchmarks
- [ ] D) When using consumer groups

<details>
<summary>Answer</summary>
**B)** When you only care about new messages from this point forward. Latest means the consumer will skip existing messages and only receive messages produced after it subscribes.
</details>

## True/False

### 5. A consumer can subscribe to multiple topics at once using `consumer.Subscribe("topic1", "topic2")`.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The Subscribe method accepts multiple topic names as parameters, allowing a single consumer to receive messages from multiple topics.
</details>

### 6. When using Assign, the consumer automatically receives messages from all partitions.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Assign is for manual partition assignment. You must explicitly specify which partition(s) to consume from, such as `consumer.Assign("topic", partition: 0, offset: 0)`.
</details>

## Code Challenge

### 7. Complete the consumer code to read messages from the beginning of a topic:

```csharp
await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    // What option ensures we start from the beginning?
    options.______________ = _____________;
});

consumer.Subscribe("events");

// Consume with a 5-second timeout
var result = await consumer.ConsumeAsync(________________);

if (result != null)
{
    Console.WriteLine($"Key: {result.Key}, Value: {result.Value}");
}
```

<details>
<summary>Answer</summary>
```csharp
await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe("events");

var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));

if (result != null)
{
    Console.WriteLine($"Key: {result.Key}, Value: {result.Value}");
}
```

`AutoOffsetReset.Earliest` ensures the consumer starts from the beginning, and `TimeSpan.FromSeconds(5)` sets a 5-second timeout for the consume operation.
</details>

## Score
- 7/7: Excellent! You understand consuming messages
- 5-6/7: Good grasp of consumer basics
- 3-4/7: Review subscription modes and offset reset
- <3/7: Re-do the lesson and practice consuming
