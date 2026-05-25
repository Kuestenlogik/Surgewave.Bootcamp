# Lesson 34.2: Zero-Allocation Patterns

### Why Allocations Matter

Every heap allocation in .NET eventually triggers garbage collection. On the produce/fetch hot path, this means:
- GC pauses cause P99 latency spikes (10-50ms for a Gen2 collection)
- More allocations = more frequent collections = worse tail latency
- Server GC helps but cannot eliminate the problem

### Span<T> and Memory<T>

`Span<T>` is a stack-only view over contiguous memory. It avoids copying data:

```csharp
// BAD: Creates a new byte array (allocation)
byte[] header = data[0..16].ToArray();

// GOOD: Creates a view without allocation
Span<byte> header = data.AsSpan(0, 16);
ReadOnlySpan<byte> readHeader = data.AsSpan(0, 16);
```

`Memory<T>` is the heap-friendly cousin of `Span<T>`, used with async methods:

```csharp
// Span<T> cannot be used in async methods
// Memory<T> can
async ValueTask ProcessAsync(ReadOnlyMemory<byte> data)
{
    // Slice without allocation
    var header = data.Slice(0, 16);
    await socket.SendAsync(header);
}
```

### stackalloc for Small Buffers

For small temporary buffers (typically <1KB), allocate on the stack:

```csharp
// BAD: Heap allocation for temporary buffer
byte[] temp = new byte[64];

// GOOD: Stack allocation (no GC pressure)
Span<byte> temp = stackalloc byte[64];
WriteHeader(temp);
```

### ArrayPool<byte>.Shared

For larger buffers, rent from the shared pool:

```csharp
byte[] buffer = ArrayPool<byte>.Shared.Rent(minimumLength: 4096);
try
{
    int bytesRead = await stream.ReadAsync(buffer);
    ProcessData(buffer.AsSpan(0, bytesRead));
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

### ObjectPool<T> for Complex Objects

```csharp
// Pool reusable request objects
var pool = new ObjectPool<PendingRequest>(() => new PendingRequest());

var request = pool.Get();
try
{
    request.Initialize(correlationId, topic, partition);
    await SendAsync(request);
}
finally
{
    request.Reset();
    pool.Return(request);
}
```

### ValueTask vs Task

`ValueTask` avoids allocating a `Task` object when the result is available synchronously:

```csharp
// BAD: Always allocates a Task
public async Task<int> GetCachedValueAsync(string key)
{
    if (_cache.TryGetValue(key, out var value))
        return value;  // Task allocated just to wrap an int
    return await FetchFromDatabaseAsync(key);
}

// GOOD: No allocation on cache hit
public ValueTask<int> GetCachedValueAsync(string key)
{
    if (_cache.TryGetValue(key, out var value))
        return new ValueTask<int>(value);  // No allocation
    return new ValueTask<int>(FetchFromDatabaseAsync(key));
}
```

---

[Back to Unit 33](../README.md)
