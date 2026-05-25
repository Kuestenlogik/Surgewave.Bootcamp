# Lesson 19.5: Instructions

Follow these step-by-step instructions to learn protocol extension techniques.

## Step 1: Implement VarInt for Tagged Fields

Tagged fields use unsigned varints:

```csharp
public static class VarInt
{
    public static byte[] EncodeUnsigned(uint value)
    {
        var result = new List<byte>();
        while (value > 0x7F)
        {
            result.Add((byte)((value & 0x7F) | 0x80));
            value >>= 7;
        }
        result.Add((byte)value);
        return result.ToArray();
    }

    public static uint DecodeUnsigned(ReadOnlySpan<byte> data, out int bytesRead)
    {
        uint result = 0;
        int shift = 0;
        bytesRead = 0;

        while (true)
        {
            byte b = data[bytesRead++];
            result |= (uint)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;
            shift += 7;
        }

        return result;
    }
}
```

## Step 2: Create Tagged Field Data Structures

```csharp
public record TaggedField(uint TagId, byte[] Data)
{
    public byte[] Encode()
    {
        var result = new List<byte>();
        result.AddRange(VarInt.EncodeUnsigned(TagId));
        result.AddRange(VarInt.EncodeUnsigned((uint)Data.Length));
        result.AddRange(Data);
        return result.ToArray();
    }

    public static TaggedField Decode(ReadOnlySpan<byte> data, out int bytesRead)
    {
        bytesRead = 0;

        var tagId = VarInt.DecodeUnsigned(data, out int tagIdBytes);
        bytesRead += tagIdBytes;

        var size = VarInt.DecodeUnsigned(data.Slice(bytesRead), out int sizeBytes);
        bytesRead += sizeBytes;

        var fieldData = data.Slice(bytesRead, (int)size).ToArray();
        bytesRead += (int)size;

        return new TaggedField(tagId, fieldData);
    }
}

public class TaggedFieldSet
{
    private readonly List<TaggedField> _fields = new();

    public void Add(uint tagId, byte[] data)
    {
        _fields.Add(new TaggedField(tagId, data));
    }

    public byte[] Encode()
    {
        var result = new List<byte>();

        // Number of tagged fields (varint)
        result.AddRange(VarInt.EncodeUnsigned((uint)_fields.Count));

        // Each tagged field
        foreach (var field in _fields.OrderBy(f => f.TagId))
        {
            result.AddRange(field.Encode());
        }

        return result.ToArray();
    }

    public static TaggedFieldSet Decode(ReadOnlySpan<byte> data, out int bytesRead)
    {
        var set = new TaggedFieldSet();
        bytesRead = 0;

        var count = VarInt.DecodeUnsigned(data, out int countBytes);
        bytesRead += countBytes;

        for (int i = 0; i < count; i++)
        {
            var field = TaggedField.Decode(data.Slice(bytesRead), out int fieldBytes);
            bytesRead += fieldBytes;
            set._fields.Add(field);
        }

        return set;
    }

    public byte[]? Get(uint tagId) =>
        _fields.FirstOrDefault(f => f.TagId == tagId)?.Data;

    public IEnumerable<TaggedField> All => _fields;
}
```

## Step 3: Implement Custom Trace Header

```csharp
public static class TraceExtension
{
    public const uint TagId = 10000;

    public static byte[] CreateTraceData(Guid traceId, long spanId, byte flags = 0)
    {
        var result = new byte[25]; // 16 + 8 + 1

        // Trace ID (16 bytes)
        traceId.ToByteArray().CopyTo(result.AsSpan(0, 16));

        // Span ID (8 bytes, big-endian)
        BinaryPrimitives.WriteInt64BigEndian(result.AsSpan(16, 8), spanId);

        // Flags (1 byte)
        result[24] = flags;

        return result;
    }

    public static (Guid TraceId, long SpanId, byte Flags) ParseTraceData(byte[] data)
    {
        var traceId = new Guid(data.AsSpan(0, 16));
        var spanId = BinaryPrimitives.ReadInt64BigEndian(data.AsSpan(16, 8));
        var flags = data[24];

        return (traceId, spanId, flags);
    }
}
```

## Step 4: Build Request with Extensions

```csharp
public static byte[] BuildFlexibleRequest(
    short apiKey,
    short apiVersion,
    int correlationId,
    string clientId,
    TaggedFieldSet? headerTags,
    byte[] body,
    TaggedFieldSet? bodyTags)
{
    var writer = new ProtocolWriter();

    // Request header (flexible format)
    writer.WriteInt16(apiKey);
    writer.WriteInt16(apiVersion);
    writer.WriteInt32(correlationId);
    writer.WriteCompactString(clientId);

    // Header tagged fields
    if (headerTags != null)
    {
        writer.WriteRaw(headerTags.Encode());
    }
    else
    {
        writer.WriteRaw(VarInt.EncodeUnsigned(0)); // No tags
    }

    // Request body
    writer.WriteRaw(body);

    // Body tagged fields
    if (bodyTags != null)
    {
        writer.WriteRaw(bodyTags.Encode());
    }
    else
    {
        writer.WriteRaw(VarInt.EncodeUnsigned(0)); // No tags
    }

    // Frame the request
    var content = writer.ToArray();
    var framed = new byte[4 + content.Length];
    BinaryPrimitives.WriteInt32BigEndian(framed.AsSpan(0, 4), content.Length);
    content.CopyTo(framed.AsSpan(4));

    return framed;
}
```

## Step 5: Handle Unknown Tags Gracefully

```csharp
public static void ProcessTaggedFields(TaggedFieldSet tags)
{
    foreach (var tag in tags.All)
    {
        switch (tag.TagId)
        {
            case TraceExtension.TagId:
                var (traceId, spanId, flags) = TraceExtension.ParseTraceData(tag.Data);
                Console.WriteLine($"  Trace: {traceId}, Span: {spanId:X16}, Flags: {flags}");
                break;

            case 10001: // Priority extension
                var priority = tag.Data[0];
                Console.WriteLine($"  Priority: {priority}");
                break;

            default:
                // Unknown tag - log and skip (backward compatibility)
                Console.WriteLine($"  Unknown tag {tag.TagId}: {tag.Data.Length} bytes (ignored)");
                break;
        }
    }
}
```

## Step 6: Design a Proprietary Extension

Example: Request routing hints extension:

```csharp
public static class RoutingExtension
{
    public const uint TagId = 10001;

    [Flags]
    public enum RoutingFlags : byte
    {
        None = 0,
        PreferLocal = 1,
        AvoidRemote = 2,
        LowLatency = 4,
        HighThroughput = 8
    }

    public static byte[] CreateRoutingData(
        RoutingFlags flags,
        string? preferredRack = null,
        byte priority = 5)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((byte)flags);
        writer.Write(priority);

        if (preferredRack != null)
        {
            var rackBytes = Encoding.UTF8.GetBytes(preferredRack);
            writer.Write((byte)rackBytes.Length);
            writer.Write(rackBytes);
        }
        else
        {
            writer.Write((byte)0);
        }

        return ms.ToArray();
    }

    public static (RoutingFlags Flags, byte Priority, string? PreferredRack)
        ParseRoutingData(byte[] data)
    {
        var flags = (RoutingFlags)data[0];
        var priority = data[1];
        var rackLength = data[2];

        string? rack = rackLength > 0
            ? Encoding.UTF8.GetString(data, 3, rackLength)
            : null;

        return (flags, priority, rack);
    }
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Tagged field encoding/decoding
- Trace header extension
- Routing hints extension
- Unknown tag handling
- Backward compatible design

## Sample Output

```
=== Surgewave Bootcamp: Lesson 19.5 - Protocol Extensions ===

--- VarInt Encoding ---
1 -> [01]
127 -> [7F]
128 -> [80 01]
16384 -> [80 80 01]

--- Tagged Field Encoding ---
Tag 10000 with 25 bytes:
  Encoded: 90 4E 19 [25 bytes of data]

--- Trace Extension ---
Creating trace context:
  Trace ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890
  Span ID: 0x123456789ABCDEF0
  Flags: 1

Tagged field data: [25 bytes]
Parsed back:
  Trace ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890
  Span ID: 123456789ABCDEF0
  Flags: 1

--- Routing Extension ---
Creating routing hints:
  Flags: PreferLocal, LowLatency
  Priority: 3
  Preferred rack: us-east-1a

--- Multiple Tags ---
Tagged field set with 2 tags:
  Tag 10000: 25 bytes (Trace context)
  Tag 10001: 13 bytes (Routing hints)

Encoded set: 02 90 4E 19 [...] 91 4E 0D [...]

--- Unknown Tag Handling ---
Processing tags including unknown tag 99999:
  Trace: a1b2c3d4-e5f6-7890-abcd-ef1234567890
  Unknown tag 99999: 10 bytes (ignored)
  -> Gracefully skipped, backward compatible!

Lesson complete!
```

## Knowledge Check

1. Why do tagged fields use unsigned varints for tag IDs?
2. How do you ensure your custom tags don't conflict with Kafka's?
3. What should you do when encountering an unknown tag ID?
4. Why is tag ordering important when encoding multiple tags?
5. How can you use tagged fields for feature detection?

---

**End of Unit 14: Wire Protocol**
