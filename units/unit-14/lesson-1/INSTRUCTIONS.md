# Lesson 19.1: Instructions

Follow these step-by-step instructions to understand Kafka protocol basics.

## Step 1: Understand the Request Header Structure

The request header consists of:

```csharp
// Request header layout (v2):
// - API Key: 2 bytes (Int16)
// - API Version: 2 bytes (Int16)
// - Correlation ID: 4 bytes (Int32)
// - Client ID: nullable string (2 bytes length + UTF-8 bytes)

public static byte[] BuildRequestHeader(short apiKey, short apiVersion, int correlationId, string? clientId)
{
    using var ms = new MemoryStream();
    using var writer = new BinaryWriter(ms);

    // Write API Key (big-endian)
    writer.Write(BitConverter.IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(apiKey)
        : apiKey);

    // Write API Version (big-endian)
    writer.Write(BitConverter.IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(apiVersion)
        : apiVersion);

    // Write Correlation ID (big-endian)
    writer.Write(BitConverter.IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(correlationId)
        : correlationId);

    // Write Client ID (nullable string)
    WriteNullableString(writer, clientId);

    return ms.ToArray();
}
```

## Step 2: Implement Nullable String Encoding

Kafka uses a specific format for nullable strings:

```csharp
public static void WriteNullableString(BinaryWriter writer, string? value)
{
    if (value == null)
    {
        // Null is encoded as -1 (0xFFFF in big-endian)
        writer.Write((byte)0xFF);
        writer.Write((byte)0xFF);
    }
    else
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var length = (short)bytes.Length;

        // Write length (big-endian)
        writer.Write(BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(length)
            : length);

        // Write string bytes
        writer.Write(bytes);
    }
}
```

## Step 3: Parse a Response Header

Response headers are simpler - just the correlation ID:

```csharp
public static int ParseResponseHeader(byte[] data)
{
    // Response header: Correlation ID (4 bytes, big-endian)
    var correlationId = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0, 4));
    return correlationId;
}
```

## Step 4: Implement Correlation ID Tracking

Use a dictionary to track pending requests:

```csharp
private readonly ConcurrentDictionary<int, TaskCompletionSource<byte[]>> _pendingRequests = new();
private int _correlationIdCounter = 0;

public async Task<byte[]> SendRequestAsync(byte[] request)
{
    var correlationId = Interlocked.Increment(ref _correlationIdCounter);
    var tcs = new TaskCompletionSource<byte[]>();

    _pendingRequests[correlationId] = tcs;

    // Send request with correlation ID embedded
    await _stream.WriteAsync(request);

    // Wait for response (matched by correlation ID)
    return await tcs.Task;
}

public void HandleResponse(int correlationId, byte[] response)
{
    if (_pendingRequests.TryRemove(correlationId, out var tcs))
    {
        tcs.SetResult(response);
    }
}
```

## Step 5: Build an ApiVersions Request

The ApiVersions request is minimal - just the header:

```csharp
public static byte[] BuildApiVersionsRequest(int correlationId, string clientId)
{
    // API Key 18 (ApiVersions), Version 0
    var header = BuildRequestHeader(18, 0, correlationId, clientId);

    // ApiVersions v0 has no body - just return header with length prefix
    var totalLength = header.Length;
    var result = new byte[4 + totalLength];

    // Write length prefix (big-endian)
    BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(0, 4), totalLength);

    // Copy header
    header.CopyTo(result.AsSpan(4));

    return result;
}
```

## Step 6: Parse ApiVersions Response

Parse the response to see supported versions:

```csharp
public static List<ApiVersion> ParseApiVersionsResponse(byte[] data)
{
    var offset = 0;

    // Skip correlation ID (already parsed)
    offset += 4;

    // Error code (2 bytes)
    var errorCode = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(offset, 2));
    offset += 2;

    if (errorCode != 0)
        throw new Exception($"ApiVersions failed with error code: {errorCode}");

    // Array length (4 bytes)
    var arrayLength = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset, 4));
    offset += 4;

    var versions = new List<ApiVersion>();
    for (int i = 0; i < arrayLength; i++)
    {
        var apiKey = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(offset, 2));
        offset += 2;

        var minVersion = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(offset, 2));
        offset += 2;

        var maxVersion = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(offset, 2));
        offset += 2;

        versions.Add(new ApiVersion(apiKey, minVersion, maxVersion));
    }

    return versions;
}

public record ApiVersion(short ApiKey, short MinVersion, short MaxVersion);
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Building request headers with proper byte ordering
- Nullable string encoding
- Correlation ID tracking
- ApiVersions request/response handling
- Displaying supported API version ranges

## Sample Output

```
=== Surgewave Bootcamp: Lesson 19.1 - Protocol Basics ===

Request Header Structure:
  API Key: 2 bytes (big-endian)
  API Version: 2 bytes (big-endian)
  Correlation ID: 4 bytes (big-endian)
  Client ID: nullable string

Building ApiVersions request (API Key 18, Version 0)...
Request bytes: 00 00 00 0E 00 12 00 00 00 00 00 01 00 0B 74 65 73 74 2D 63 6C 69 65 6E 74

Sending request to Surgewave broker...

ApiVersions Response:
  Error Code: 0 (Success)
  Supported APIs:
    API  0 (Produce)         : versions 0-9
    API  1 (Fetch)           : versions 0-13
    API  2 (ListOffsets)     : versions 0-7
    API  3 (Metadata)        : versions 0-12
    API  8 (OffsetCommit)    : versions 0-8
    API  9 (OffsetFetch)     : versions 0-8
    API 10 (FindCoordinator) : versions 0-4
    API 11 (JoinGroup)       : versions 0-9
    API 14 (SyncGroup)       : versions 0-5
    API 18 (ApiVersions)     : versions 0-3
    ... (more APIs)

Protocol negotiation: Client will use highest mutually supported version

Lesson complete!
```

## Knowledge Check

1. Why does Kafka use big-endian byte order?
2. What happens if a client sends a request with an unsupported API version?
3. How does the correlation ID enable multiplexing on a single connection?
4. Why is ApiVersions v0 always supported?
5. What's the difference between a null string and an empty string in the protocol?

---

**Next:** [Lesson 19.2: Message Format](../lesson-2/README.md)
