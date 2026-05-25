# Knowledge Assessment: Lesson 26.4 - Memory Management & GC Tuning

## Multiple Choice

### 1. What is the primary benefit of using ArrayPool<T> instead of allocating new arrays?
- [ ] A) Arrays are automatically zeroed
- [ ] B) Reduced GC pressure by reusing buffers
- [ ] C) Smaller array sizes
- [ ] D) Thread-safe array access

<details>
<summary>Answer</summary>
**B)** ArrayPool<T> maintains a pool of reusable arrays. Instead of allocating new arrays (which become garbage), you rent from the pool and return when done. This dramatically reduces Gen0 allocations and GC pressure.
</details>

### 2. Why can't Span<T> be stored as a field in a class?
- [ ] A) It's too large
- [ ] B) It's a ref struct and can only live on the stack
- [ ] C) It requires unsafe code
- [ ] D) It's not serializable

<details>
<summary>Answer</summary>
**B)** Span<T> is a `ref struct`, meaning it can only exist on the stack. This restriction ensures the GC doesn't need to track it and enables zero-overhead memory access. Use Memory<T> when you need heap storage or async method compatibility.
</details>

### 3. Which GC latency mode provides the highest throughput?
- [ ] A) Interactive
- [ ] B) LowLatency
- [ ] C) SustainedLowLatency
- [ ] D) Batch

<details>
<summary>Answer</summary>
**D)** Batch mode maximizes throughput by allowing the GC to perform full blocking collections. It does not try to minimize pause times. Use it for offline/batch processing where latency doesn't matter but throughput is critical.
</details>

### 4. What is the key difference between ServerGC and WorkstationGC?
- [ ] A) ServerGC is faster
- [ ] B) ServerGC uses one heap per CPU core; WorkstationGC uses a single shared heap
- [ ] C) WorkstationGC doesn't support Gen2 collection
- [ ] D) ServerGC requires more code changes

<details>
<summary>Answer</summary>
**B)** ServerGC creates one GC heap per logical processor, enabling parallel garbage collection. This provides higher throughput on multi-core servers but uses more memory. WorkstationGC uses a single heap, which is better for client applications with limited cores.
</details>

### 5. When should you use Memory<T> instead of Span<T>?
- [ ] A) When processing synchronously
- [ ] B) When you need to store the reference in a field or pass it to async methods
- [ ] C) When the data is small
- [ ] D) When using unsafe code

<details>
<summary>Answer</summary>
**B)** Memory<T> can be stored on the heap (in class fields, collections, async state machines) while Span<T> cannot. Use Memory<T> when you need to pass memory references across async boundaries or store them in objects. Access the data via the .Span property for actual processing.
</details>

## True/False

### 6. GC.TryStartNoGCRegion() permanently disables garbage collection.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - GC.TryStartNoGCRegion() temporarily suppresses GC for a specified amount of memory allocation. It must be followed by GC.EndNoGCRegion(). If the allocation budget is exceeded, a full GC will occur. It's used for brief latency-critical sections, not permanent suppression.
</details>

### 7. Returning a buffer to ArrayPool<T> automatically clears its contents.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - By default, ArrayPool<T>.Return() does NOT clear the buffer contents. You must pass `clearArray: true` if you need the buffer zeroed (e.g., for security-sensitive data). Skipping the clear is faster but means rented buffers may contain stale data.
</details>

### 8. Gen2 garbage collections are the most expensive type.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Gen2 collections scan the entire managed heap (all generations). They are full blocking collections (unless background GC is enabled) and can cause significant pauses. Minimizing Gen2 collections is key to reducing tail latency in Surgewave applications.
</details>

## Code Challenge

### 9. What memory management issues exist in this message processing code?

```csharp
async Task ProcessMessagesAsync(SurgewaveConsumer<string, string> consumer, int count)
{
    for (int i = 0; i < count; i++)
    {
        var result = await consumer.ConsumeAsync();
        if (result?.Value == null) continue;

        // Parse the message
        var parts = result.Value.Split('|');
        var key = parts[0];
        var payload = Convert.FromBase64String(parts[1]);
        var timestamp = DateTime.Parse(parts[2]);

        // Create processing result
        var output = new byte[payload.Length * 2];
        Array.Copy(payload, output, payload.Length);

        // Convert to string for logging
        var logMessage = $"Processed {key} at {timestamp}: {BitConverter.ToString(output)}";
        Console.WriteLine(logMessage);
    }
}
```

<details>
<summary>Answer</summary>
**Issues:**

1. **String.Split() allocates** a new string array and substrings every iteration
2. **Convert.FromBase64String()** allocates a new byte[] each time
3. **new byte[payload.Length * 2]** allocates output buffer every iteration
4. **String interpolation + BitConverter.ToString()** creates multiple temporary strings
5. **DateTime.Parse()** allocates intermediate parsing objects
6. All allocations are short-lived, causing heavy Gen0 GC pressure

**Optimized version:**

```csharp
async Task ProcessMessagesOptimized(SurgewaveConsumer<string, string> consumer, int count)
{
    var pool = ArrayPool<byte>.Shared;

    for (int i = 0; i < count; i++)
    {
        var result = await consumer.ConsumeAsync();
        if (result?.Value == null) continue;

        var message = result.Value.AsSpan();

        // Zero-copy parsing with Span<T>
        var firstSep = message.IndexOf('|');
        var lastSep = message.LastIndexOf('|');

        var key = message[..firstSep];
        var payloadB64 = message[(firstSep + 1)..lastSep];
        var timestampSpan = message[(lastSep + 1)..];

        // Pool the decode buffer
        var maxDecodedSize = (payloadB64.Length * 3 + 3) / 4;
        var payload = pool.Rent(maxDecodedSize);
        try
        {
            Convert.TryFromBase64Chars(payloadB64, payload, out var bytesWritten);

            // Pool the output buffer
            var output = pool.Rent(bytesWritten * 2);
            try
            {
                Buffer.BlockCopy(payload, 0, output, 0, bytesWritten);
                // Process output in-place...
            }
            finally
            {
                pool.Return(output);
            }
        }
        finally
        {
            pool.Return(payload);
        }
    }
}
```

**Key improvements:**
- Span<T> slicing instead of String.Split()
- ArrayPool for decode and output buffers
- No string allocations in the hot path
</details>

### 10. Design a zero-allocation message processing pipeline for this scenario:

**Scenario**: A Surgewave consumer processes 500K messages/sec. Each message is a 256-byte binary payload that needs:
1. CRC32 validation
2. Header extraction (first 16 bytes)
3. Payload transformation (XOR with key)
4. Forwarding to another topic

What memory management strategy would you use?

<details>
<summary>Answer</summary>
**Zero-Allocation Pipeline Design:**

```csharp
public class ZeroAllocPipeline
{
    private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
    private readonly SurgewaveProducer<byte[], byte[]> _forwarder;

    public void ProcessMessage(ReadOnlySpan<byte> rawMessage)
    {
        // 1. CRC32 validation (no allocation - operates on span)
        var payloadSpan = rawMessage[..^4]; // All but last 4 bytes
        var expectedCrc = BinaryPrimitives.ReadUInt32LittleEndian(rawMessage[^4..]);
        var actualCrc = Crc32.Compute(payloadSpan);
        if (actualCrc != expectedCrc) return; // Drop invalid

        // 2. Header extraction (no allocation - span slicing)
        var header = payloadSpan[..16];
        var messageType = BinaryPrimitives.ReadInt32LittleEndian(header);
        var sequenceNum = BinaryPrimitives.ReadInt64LittleEndian(header[4..]);

        // 3. Payload transformation (rent buffer, transform in-place)
        var payload = payloadSpan[16..];
        var outputBuffer = _pool.Rent(payload.Length);
        try
        {
            payload.CopyTo(outputBuffer);
            var outputSpan = outputBuffer.AsSpan(0, payload.Length);

            // XOR transform in-place
            var key = BinaryPrimitives.ReadInt32LittleEndian(header[12..]);
            for (int i = 0; i < outputSpan.Length; i++)
            {
                outputSpan[i] ^= (byte)(key >> (i % 4 * 8));
            }

            // 4. Forward (using pooled buffer)
            _forwarder.Produce("output-topic", header.ToArray(), outputBuffer);
        }
        finally
        {
            _pool.Return(outputBuffer);
        }
    }
}
```

**GC Configuration (in .csproj):**
```xml
<PropertyGroup>
    <ServerGarbageCollection>true</ServerGarbageCollection>
    <ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>
    <GarbageCollectionAdaptationMode>0</GarbageCollectionAdaptationMode>
</PropertyGroup>
```

**Runtime configuration:**
```csharp
GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
```

**Expected result:**
- Near-zero Gen0 allocations in hot path
- No Gen1/Gen2 collections during processing
- Consistent sub-millisecond P99 latency
- 500K+ msg/s throughput maintained
</details>

## Score
- 10/10: Excellent! Ready for benchmarking & profiling
- 8-9/10: Good understanding, review missed concepts
- 5-7/10: Review the lesson before continuing
- <5/10: Re-do the lesson and exercises
