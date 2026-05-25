# Knowledge Assessment: Lesson 4.5 - Performance Tuning

## Multiple Choice

### 1. What is the trade-off when optimizing for throughput?
- [ ] A) Lower durability
- [ ] B) Higher latency
- [ ] C) Less security
- [ ] D) More disk usage

<details>
<summary>Answer</summary>
**B)** Optimizing for throughput typically results in higher latency. Batching more messages together improves throughput but means individual messages wait longer before being sent.
</details>

### 2. Which producer setting controls how long to wait for a batch to fill before sending?
- [ ] A) BatchSize
- [ ] B) BufferMemory
- [ ] C) LingerMs
- [ ] D) CompressionType

<details>
<summary>Answer</summary>
**C)** `LingerMs` controls how long the producer waits for additional messages to fill a batch before sending. Higher values = more batching = higher throughput but higher latency.
</details>

### 3. What does the `FetchMinBytes` consumer setting control?
- [ ] A) Maximum data per fetch
- [ ] B) Minimum data the broker should return per fetch
- [ ] C) Consumer memory allocation
- [ ] D) Offset commit size

<details>
<summary>Answer</summary>
**B)** `FetchMinBytes` sets the minimum amount of data the broker should return per fetch request. Higher values reduce the number of fetch requests but increase latency as the consumer waits for more data.
</details>

## True/False

### 4. Enabling compression always reduces throughput.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Compression typically increases throughput by reducing the amount of data sent over the network and written to disk. The CPU cost of compression is usually outweighed by reduced I/O.
</details>

### 5. Optimizing for durability (Acks.All) decreases throughput.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Acks.All requires waiting for all in-sync replicas to acknowledge the write, which takes longer than Acks.Leader or Acks.None. This increased durability comes at the cost of lower throughput.
</details>

## Code Challenge

### 6. You need to optimize this producer for maximum throughput. What changes would you make?

```csharp
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    // Default settings
});
```

<details>
<summary>Answer</summary>
For maximum throughput, configure:

```csharp
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";

    // Increase batch size for more messages per request
    opt.BatchSize = 1048576; // 1MB batches

    // Allow time for batches to fill
    opt.LingerMs = 50; // Wait up to 50ms

    // Enable compression to reduce network/disk I/O
    opt.CompressionType = CompressionType.Lz4;

    // Increase buffer for high throughput
    opt.BufferMemory = 67108864; // 64MB

    // Consider Acks.Leader for better throughput (trade-off with durability)
    opt.Acks = Acks.Leader;
});
```

Key throughput optimizations:
1. Larger BatchSize = fewer requests
2. Higher LingerMs = fuller batches
3. Compression = less I/O
4. Acks.Leader = faster acknowledgments (if durability permits)
</details>

### 7. You have a latency-sensitive application. What consumer settings would you tune?

<details>
<summary>Answer</summary>
For minimum latency, configure:

```csharp
var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    opt.GroupId = "low-latency-group";

    // Reduce minimum fetch to avoid waiting
    opt.FetchMinBytes = 1; // Return immediately with any data

    // Reduce max wait time
    opt.FetchMaxWaitMs = 10; // Wait max 10ms

    // Smaller fetch sizes for faster processing
    opt.MaxPartitionFetchBytes = 65536; // 64KB per partition
});
```

Key latency optimizations:
1. **FetchMinBytes = 1**: Broker returns data immediately without waiting to accumulate more
2. **FetchMaxWaitMs low**: Don't wait long for data
3. **Smaller MaxPartitionFetchBytes**: Process smaller batches faster

Trade-off: These settings reduce throughput because more fetch requests are needed.
</details>

## Score
- 7/7: Excellent! You understand performance tuning
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
