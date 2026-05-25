# Lesson 34.4: Memory Pooling Strategies

### BufferPool

Surgewave's custom buffer pool for network I/O:

```csharp
var pool = new BufferPool(
    initialSize: 4096,
    maxPooled: 100
);

byte[] buffer = pool.Rent(minSize: 8192);
try
{
    // Use buffer for network read/write
}
finally
{
    pool.Return(buffer);
}
```

### RecyclableMemoryStreamManager

Pooled `MemoryStream` instances for serialization:

```csharp
private static readonly RecyclableMemoryStreamManager StreamManager = new();

using var stream = StreamManager.GetStream();
// Write to stream without allocating a new MemoryStream each time
serializer.Serialize(stream, message);
```

### PartitionWriteRequest Object Pooling Pattern

```csharp
// A reusable request object that avoids per-message allocation
public sealed class PartitionWriteRequest
{
    public string Topic { get; set; } = "";
    public int Partition { get; set; }
    public ReadOnlyMemory<byte> Data { get; set; }
    public TaskCompletionSource<long>? Completion { get; set; }

    public void Reset()
    {
        Topic = "";
        Partition = 0;
        Data = default;
        Completion = null;
    }
}
```

### PooledCompletionSource<T>

Uses `ManualResetValueTaskSourceCore<T>` to avoid allocating a `TaskCompletionSource` per operation:

```csharp
// Standard approach: allocates per operation
var tcs = new TaskCompletionSource<long>();  // ALLOCATION

// Pooled approach: reuses from pool
var source = pool.Get();  // NO ALLOCATION
source.Reset();
// ... use source.GetTask() ...
pool.Return(source);
```

### When NOT to Pool

Pooling has overhead (tracking, returning, thread-safety). Do not pool:
- Small, short-lived value types (use `stackalloc` instead)
- Objects created once at startup
- Objects with complex initialization that cannot be reset cheaply
- Situations where pool contention exceeds allocation cost

---

[Back to Unit 33](../README.md)
