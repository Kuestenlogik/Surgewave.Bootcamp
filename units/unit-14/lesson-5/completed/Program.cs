// Surgewave Bootcamp - Unit 19, Lesson 5: Protocol Extensions
// COMPLETED SOLUTION

using System.Buffers.Binary;
using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 19.5 - Protocol Extensions ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");
Console.WriteLine();

// Step 1 - Demonstrate VarInt encoding
Console.WriteLine("--- VarInt Encoding ---");
var testValues = new uint[] { 1, 127, 128, 16384, 2097152 };
foreach (var value in testValues)
{
    var encoded = VarInt.EncodeUnsigned(value);
    var decodedValue = VarInt.DecodeUnsigned(encoded, out _);
    Console.WriteLine($"{value,8} -> [{BitConverter.ToString(encoded).Replace("-", " "),-15}] -> {decodedValue}");
}
Console.WriteLine();

// Step 2 - Create a tagged field
Console.WriteLine("--- Tagged Field Encoding ---");
var sampleData = new byte[] { 1, 2, 3, 4, 5 };
var tag = new TaggedField(10000, sampleData);
var encodedTag = tag.Encode();
Console.WriteLine($"Tag 10000 with {sampleData.Length} bytes:");
Console.WriteLine($"  Raw data: [{BitConverter.ToString(sampleData).Replace("-", " ")}]");
Console.WriteLine($"  Encoded:  [{BitConverter.ToString(encodedTag).Replace("-", " ")}]");
Console.WriteLine($"  Breakdown: [{BitConverter.ToString(VarInt.EncodeUnsigned(10000)).Replace("-", " ")}] = tag ID 10000");
Console.WriteLine($"             [{BitConverter.ToString(VarInt.EncodeUnsigned(5)).Replace("-", " ")}] = size 5");
Console.WriteLine($"             [{BitConverter.ToString(sampleData).Replace("-", " ")}] = data");
Console.WriteLine();

// Step 3 - Implement trace extension
Console.WriteLine("--- Trace Extension ---");
var traceId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
var spanId = 0x123456789ABCDEF0L;
byte flags = 1;

Console.WriteLine("Creating trace context:");
Console.WriteLine($"  Trace ID: {traceId}");
Console.WriteLine($"  Span ID: 0x{spanId:X16}");
Console.WriteLine($"  Flags: {flags}");

var traceData = TraceExtension.CreateTraceData(traceId, spanId, flags);
Console.WriteLine($"\nEncoded trace data: {traceData.Length} bytes");
Console.WriteLine($"  Hex: [{BitConverter.ToString(traceData).Replace("-", " ")}]");

var (parsedTraceId, parsedSpanId, parsedFlags) = TraceExtension.ParseTraceData(traceData);
Console.WriteLine($"\nParsed back:");
Console.WriteLine($"  Trace ID: {parsedTraceId}");
Console.WriteLine($"  Span ID: {parsedSpanId:X16}");
Console.WriteLine($"  Flags: {parsedFlags}");
Console.WriteLine();

// Step 4 - Create routing extension
Console.WriteLine("--- Routing Extension ---");
var routingData = RoutingExtension.CreateRoutingData(
    RoutingExtension.RoutingFlags.PreferLocal | RoutingExtension.RoutingFlags.LowLatency,
    preferredRack: "us-east-1a",
    priority: 3);

Console.WriteLine("Creating routing hints:");
Console.WriteLine($"  Flags: PreferLocal | LowLatency");
Console.WriteLine($"  Priority: 3");
Console.WriteLine($"  Preferred rack: us-east-1a");
Console.WriteLine($"\nEncoded: {routingData.Length} bytes");
Console.WriteLine($"  Hex: [{BitConverter.ToString(routingData).Replace("-", " ")}]");

var (routingFlags, priority, rack) = RoutingExtension.ParseRoutingData(routingData);
Console.WriteLine($"\nParsed routing data:");
Console.WriteLine($"  Flags: {routingFlags}");
Console.WriteLine($"  Priority: {priority}");
Console.WriteLine($"  Preferred rack: {rack}");
Console.WriteLine();

// Step 5 - Create a tagged field set with multiple extensions
Console.WriteLine("--- Tagged Field Set ---");
var tagSet = new TaggedFieldSet();
tagSet.Add(TraceExtension.TagId, traceData);
tagSet.Add(RoutingExtension.TagId, routingData);

var encodedSet = tagSet.Encode();
Console.WriteLine($"Tagged field set with {tagSet.All.Count()} tags:");
foreach (var t in tagSet.All)
{
    Console.WriteLine($"  Tag {t.TagId}: {t.Data.Length} bytes");
}
Console.WriteLine($"\nEncoded size: {encodedSet.Length} bytes");
Console.WriteLine($"  Hex: [{BitConverter.ToString(encodedSet.Take(30).ToArray()).Replace("-", " ")}...]");

var decoded = TaggedFieldSet.Decode(encodedSet, out var bytesRead);
Console.WriteLine($"\nDecoded {decoded.All.Count()} tags ({bytesRead} bytes read):");
foreach (var t in decoded.All)
{
    Console.WriteLine($"  Tag {t.TagId}: {t.Data.Length} bytes");
}
Console.WriteLine();

// Step 6 - Handle unknown tags gracefully
Console.WriteLine("--- Unknown Tag Handling ---");
var mixedTags = new TaggedFieldSet();
mixedTags.Add(TraceExtension.TagId, traceData);
mixedTags.Add(99999, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }); // Unknown future tag
mixedTags.Add(RoutingExtension.TagId, routingData);

Console.WriteLine("Processing tags (including unknown tag 99999):");
ProcessTaggedFields(mixedTags);
Console.WriteLine("\n  -> Unknown tags are gracefully skipped (backward compatible!)");
Console.WriteLine();

// Step 7 - Demonstrate tag ordering
Console.WriteLine("--- Tag Ordering ---");
var outOfOrder = new TaggedFieldSet();
outOfOrder.Add(20000, new byte[] { 0xAA });
outOfOrder.Add(10000, new byte[] { 0xBB });
outOfOrder.Add(15000, new byte[] { 0xCC });

Console.WriteLine("Tags added in order: 20000, 10000, 15000");
var orderedEncoded = outOfOrder.Encode();
var reDecoded = TaggedFieldSet.Decode(orderedEncoded, out _);
Console.WriteLine("Tags after encode/decode (sorted by ID):");
foreach (var t in reDecoded.All)
{
    Console.WriteLine($"  Tag {t.TagId}: [{BitConverter.ToString(t.Data)}]");
}

Console.WriteLine();
Console.WriteLine("--- Protocol Extension Best Practices ---");
Console.WriteLine("1. Use tag IDs >= 10000 for custom extensions");
Console.WriteLine("2. Document your tag ID allocations clearly");
Console.WriteLine("3. Always handle unknown tags gracefully (skip them)");
Console.WriteLine("4. Encode tags in ascending order by tag ID");
Console.WriteLine("5. Design extensions to be backward compatible");
Console.WriteLine("6. Use feature detection via ApiVersions response");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Tagged fields enable backward-compatible extensions");
Console.WriteLine("2. VarInt encoding keeps tag overhead minimal");
Console.WriteLine("3. Trace and routing extensions enable observability");
Console.WriteLine("4. Unknown tags should be logged and skipped");
Console.WriteLine("5. The flexible protocol format is the future of Kafka");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
Console.WriteLine();
Console.WriteLine("=== End of Unit 19: Wire Protocol ===");

// --- Helper Methods ---

static void ProcessTaggedFields(TaggedFieldSet tags)
{
    foreach (var tag in tags.All)
    {
        switch (tag.TagId)
        {
            case TraceExtension.TagId:
                var (traceId, spanId, flags) = TraceExtension.ParseTraceData(tag.Data);
                Console.WriteLine($"  Trace: {traceId}, Span: {spanId:X16}, Flags: {flags}");
                break;

            case RoutingExtension.TagId:
                var (routingFlags, priority, rack) = RoutingExtension.ParseRoutingData(tag.Data);
                Console.WriteLine($"  Routing: Flags={routingFlags}, Priority={priority}, Rack={rack}");
                break;

            default:
                Console.WriteLine($"  Unknown tag {tag.TagId}: {tag.Data.Length} bytes (skipped)");
                break;
        }
    }
}

// VarInt encoding utilities
static class VarInt
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

// Tagged field record
record TaggedField(uint TagId, byte[] Data)
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

// Tagged field set
class TaggedFieldSet
{
    private readonly List<TaggedField> _fields = new();

    public void Add(uint tagId, byte[] data) =>
        _fields.Add(new TaggedField(tagId, data));

    public byte[] Encode()
    {
        var result = new List<byte>();
        result.AddRange(VarInt.EncodeUnsigned((uint)_fields.Count));
        // Sort by tag ID for consistency
        foreach (var field in _fields.OrderBy(f => f.TagId))
            result.AddRange(field.Encode());
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

    public byte[]? Get(uint tagId) => _fields.FirstOrDefault(f => f.TagId == tagId)?.Data;
    public IEnumerable<TaggedField> All => _fields;
}

// Trace extension (Tag 10000)
static class TraceExtension
{
    public const uint TagId = 10000;

    public static byte[] CreateTraceData(Guid traceId, long spanId, byte flags = 0)
    {
        var result = new byte[25];
        traceId.ToByteArray().CopyTo(result.AsSpan(0, 16));
        BinaryPrimitives.WriteInt64BigEndian(result.AsSpan(16, 8), spanId);
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

// Routing extension (Tag 10001)
static class RoutingExtension
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

    public static byte[] CreateRoutingData(RoutingFlags flags, string? preferredRack = null, byte priority = 5)
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

    public static (RoutingFlags Flags, byte Priority, string? PreferredRack) ParseRoutingData(byte[] data)
    {
        var flags = (RoutingFlags)data[0];
        var priority = data[1];
        var rackLength = data[2];
        string? rack = rackLength > 0 ? Encoding.UTF8.GetString(data, 3, rackLength) : null;
        return (flags, priority, rack);
    }
}
