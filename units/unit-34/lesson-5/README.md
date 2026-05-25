# Lesson 34.5: Hot-Path Optimizations Applied

### LINQ Elimination

LINQ methods allocate iterator objects and delegates. On hot paths, replace with loops:

```csharp
// BAD: Allocates IEnumerable iterator + delegate
int totalSize = batches.Sum(b => b.Length);

// GOOD: Zero allocation
int totalSize = 0;
foreach (var batch in batches)
    totalSize += batch.Length;
```

### String Interning for Topic Names

Topic names are repeated millions of times. Interning avoids duplicate string allocations:

```csharp
// Without interning: each deserialized topic name is a new string object
string topic = Encoding.UTF8.GetString(topicBytes);

// With interning: all identical topic names share one string instance
string topic = string.Intern(Encoding.UTF8.GetString(topicBytes));
```

### .ToArray() Elimination

Avoid creating intermediate arrays when the consumer accepts a span or enumerable:

```csharp
// BAD: Allocates a new array
byte[] compressed = compressor.Compress(data.ToArray());

// GOOD: Pass through without copying
ReadOnlyMemory<byte> compressed = compressor.Compress(data);
```

### ValueTask Synchronous Completion Bypass

When a `ValueTask` completes synchronously, bypass the async state machine:

```csharp
public ValueTask<int> ReadAsync(Memory<byte> buffer)
{
    // Check if data is already available (common case)
    if (_buffered > 0)
    {
        int copied = CopyFromBuffer(buffer);
        return new ValueTask<int>(copied);  // No async overhead
    }

    // Rare case: actually need to do async I/O
    return ReadSlowAsync(buffer);
}
```

### PendingRequest Pooling in TCP Transport

The TCP transport pools `PendingRequest` objects to avoid allocation per in-flight request:

```csharp
// Each produce/fetch RPC creates a PendingRequest
// Without pooling: millions of allocations under load
// With pooling: fixed set of reusable objects

var request = _requestPool.Get();
request.Initialize(correlationId, callback);
_pending[correlationId] = request;
// ... await response ...
_requestPool.Return(request);
```

---

[Back to Unit 33](../README.md)
