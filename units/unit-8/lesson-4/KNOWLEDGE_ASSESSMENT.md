# Knowledge Assessment: Lesson 12.4 - Backpressure

## Multiple Choice

### 1. What is backpressure in reactive programming?
- [ ] A) A technique to increase producer speed
- [ ] B) The challenge of handling producers that emit data faster than consumers can process
- [ ] C) A method for compressing message data
- [ ] D) A way to encrypt message streams

<details>
<summary>Answer</summary>
**B)** Backpressure is the challenge that arises when a producer emits data faster than a consumer can process it. Without proper handling, this leads to problems like memory exhaustion, data loss, or system deadlock.
</details>

### 2. Which backpressure strategy keeps ALL data but batches it for processing?
- [ ] A) Sample
- [ ] B) Throttle
- [ ] C) Buffer
- [ ] D) Drop Newest

<details>
<summary>Answer</summary>
**C)** Buffer keeps all data by collecting elements into batches and emitting them as groups. This preserves all data but requires memory to store the batched elements.
</details>

### 3. When should you use the Sample strategy?
- [ ] A) When all messages must be processed
- [ ] B) When only the latest value matters (e.g., current price, temperature)
- [ ] C) When order of processing is critical
- [ ] D) When you need exactly-once processing

<details>
<summary>Answer</summary>
**B)** Sample is appropriate when only the latest value matters, such as real-time sensor readings, stock prices, or status updates. It takes one value per interval, discarding intermediate values.
</details>

### 4. What is the consequence of using an unbounded buffer?
- [ ] A) Improved performance
- [ ] B) Potential memory exhaustion
- [ ] C) Automatic data compression
- [ ] D) Reduced latency

<details>
<summary>Answer</summary>
**B)** An unbounded buffer will keep growing as long as the producer is faster than the consumer, eventually leading to memory exhaustion and application crashes.
</details>

## True/False

### 5. Throttle and Sample are the same strategy with different names.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - While similar, they differ: Sample emits the latest value at regular intervals regardless of when it arrived. Throttle (or Debounce) emits a value only after a specified quiet period with no new values. Sample is time-based windowing; Throttle is activity-based.
</details>

### 6. Drop strategies always result in bounded memory usage.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Drop strategies (Drop Oldest, Drop Newest) maintain bounded memory by discarding elements when the buffer is full. This provides predictable memory usage at the cost of potential data loss.
</details>

## Code Challenge

### 7. What backpressure issue exists in this code, and how would you fix it?

```csharp
surgewaveObservable
    .Where(msg => msg.Value != null)
    .Subscribe(async msg =>
    {
        // Slow processing - takes 500ms per message
        await ProcessMessageAsync(msg);
    });
```

<details>
<summary>Answer</summary>
**Issue**: If messages arrive faster than 2/second (500ms processing each), they will queue up unboundedly. The async lambda creates a fire-and-forget situation where Subscribe returns immediately while processing continues, potentially spawning many concurrent processing tasks.

**Fixes** depending on requirements:

1. **Buffer for batch processing**:
```csharp
surgewaveObservable
    .Where(msg => msg.Value != null)
    .Buffer(TimeSpan.FromSeconds(1))
    .Subscribe(async batch =>
    {
        foreach (var msg in batch)
            await ProcessMessageAsync(msg);
    });
```

2. **Sample if only latest matters**:
```csharp
surgewaveObservable
    .Where(msg => msg.Value != null)
    .Sample(TimeSpan.FromMilliseconds(500))
    .Subscribe(async msg => await ProcessMessageAsync(msg));
```

3. **Bounded buffer with overflow policy**:
```csharp
surgewaveObservable
    .Where(msg => msg.Value != null)
    .BoundedBuffer(maxSize: 100, OverflowPolicy.DropOldest)
    .Subscribe(async msg => await ProcessMessageAsync(msg));
```
</details>

## Score
- 7/7: Excellent! You understand backpressure handling
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding
