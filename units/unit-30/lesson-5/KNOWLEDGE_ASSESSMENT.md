# Knowledge Assessment: Lesson 17.5 - Streaming Large Data

## Multiple Choice

### 1. What is the main advantage of streaming over buffering for large data?
- [ ] A) Faster processing speed
- [ ] B) Only a small buffer is in memory at any time, not the entire file
- [ ] C) Better compression ratios
- [ ] D) Simpler code

<details>
<summary>Answer</summary>
**B)** Streaming keeps only a small buffer (e.g., 64 KB) in memory at any time, while buffering loads the entire file. For a 100 MB file, streaming uses ~64 KB of RAM versus 100 MB for buffering.
</details>

### 2. What happens in backpressure handling when the consumer is slower than the producer?
- [ ] A) The producer continues sending, causing memory overflow
- [ ] B) The producer is slowed down or paused until the consumer catches up
- [ ] C) Messages are automatically dropped
- [ ] D) The consumer speeds up automatically

<details>
<summary>Answer</summary>
**B)** With backpressure, when the queue fills up (consumer is slow), the producer is slowed down or blocked until the consumer catches up. This prevents memory overflow and maintains system stability.
</details>

### 3. Which C# feature enables memory-efficient streaming by yielding items one at a time?
- [ ] A) List<T>
- [ ] B) Task<T>
- [ ] C) IAsyncEnumerable<T> with yield return
- [ ] D) Array<T>

<details>
<summary>Answer</summary>
**C)** IAsyncEnumerable<T> with `yield return` enables memory-efficient streaming by producing items one at a time asynchronously. Memory is freed after each iteration, keeping only one chunk in memory.
</details>

## True/False

### 4. Using `await streamSource.ToListAsync()` is an efficient way to process streaming data.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - `ToListAsync()` buffers ALL items in memory, defeating the purpose of streaming. The correct approach is to use `await foreach` to process items one at a time:
```csharp
await foreach (var chunk in streamSource)
{
    await ProcessAsync(chunk);
}
```
</details>

### 5. A bounded channel with `BoundedChannelFullMode.Wait` implements backpressure by blocking producers when the channel is full.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - A bounded channel with `FullMode = BoundedChannelFullMode.Wait` causes producers to wait/block when the channel reaches capacity, implementing backpressure to prevent unbounded memory growth.
</details>

## Code Challenge

### 6. What's wrong with this streaming implementation?
```csharp
async IAsyncEnumerable<byte[]> StreamDataAsync(Stream source)
{
    var allData = new byte[source.Length];
    await source.ReadAsync(allData, 0, allData.Length);
    yield return allData;
}
```

<details>
<summary>Answer</summary>
This code reads the entire stream into memory at once, defeating the purpose of streaming. The correct approach reads in chunks:
```csharp
async IAsyncEnumerable<byte[]> StreamDataAsync(Stream source)
{
    var buffer = new byte[64 * 1024]; // 64 KB chunks
    int bytesRead;

    while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
    {
        var chunk = new byte[bytesRead];
        Array.Copy(buffer, chunk, bytesRead);
        yield return chunk;  // Yield one chunk at a time
    }
}
```
</details>

### 7. Why might progress reporting every chunk be problematic for large files?
- [ ] A) It uses too much memory
- [ ] B) Console/UI updates every 64 KB cause significant overhead and slow down processing
- [ ] C) It violates streaming principles
- [ ] D) Progress cannot be calculated while streaming

<details>
<summary>Answer</summary>
**B)** Reporting progress for every small chunk (e.g., every 64 KB of a 50 MB file = 800 updates) causes significant overhead from console/UI updates and I/O operations. Instead, report periodically:
```csharp
if (chunksProcessed % 100 == 0 || isLastChunk)
{
    ReportProgress(processed, total);
}
```
</details>

## Score
- 7/7: Excellent! You understand streaming and backpressure
- 5-6/7: Good grasp of memory-efficient processing
- 3-4/7: Review streaming vs buffering and backpressure
- <3/7: Re-do the lesson focusing on IAsyncEnumerable and channels
