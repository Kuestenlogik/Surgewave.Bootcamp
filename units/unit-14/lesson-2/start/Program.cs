// Surgewave Bootcamp - Unit 19, Lesson 2: Message Format
//
// In this lesson, you'll learn:
// - Record batch structure and format
// - Message headers and their uses
// - Compression algorithms (GZIP, LZ4, etc.)
// - CRC validation for data integrity
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Buffers.Binary;
using System.IO.Compression;
using System.IO.Hashing;
using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 19.2 - Message Format ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");
Console.WriteLine();

// TODO: Step 1 - Implement VarInt encoding (ZigZag encoded)
Console.WriteLine("--- VarInt Encoding (ZigZag) ---");
// VarInt uses 7 bits per byte, MSB is continuation flag
// ZigZag maps: 0->0, -1->1, 1->2, -2->3, 2->4, ...
//
// static byte[] EncodeVarInt(int value)
// {
//     // ZigZag encode
//     uint encoded = (uint)((value << 1) ^ (value >> 31));
//
//     var result = new List<byte>();
//     while (encoded > 0x7F)
//     {
//         result.Add((byte)((encoded & 0x7F) | 0x80));
//         encoded >>= 7;
//     }
//     result.Add((byte)encoded);
//     return result.ToArray();
// }
//
// Test with values: 0, 1, -1, 300
// foreach (var v in new[] { 0, 1, -1, 300 })
// {
//     var encoded = EncodeVarInt(v);
//     Console.WriteLine($"Value {v}: [{BitConverter.ToString(encoded).Replace("-", " ")}]");
// }
Console.WriteLine("(uncomment code to see VarInt encoding)");
Console.WriteLine();

// TODO: Step 2 - Build a single record
Console.WriteLine("--- Building a Record ---");
// A record contains: length, attributes, timestamp delta, offset delta, key, value, headers
//
// static byte[] BuildRecord(int timestampDelta, int offsetDelta, byte[]? key, byte[] value)
// {
//     var record = new List<byte>();
//
//     // Attributes (1 byte, currently unused)
//     record.Add(0x00);
//
//     // Timestamp delta (VarInt)
//     record.AddRange(EncodeVarInt(timestampDelta));
//
//     // Offset delta (VarInt)
//     record.AddRange(EncodeVarInt(offsetDelta));
//
//     // Key (VarInt length + bytes, -1 = null)
//     if (key == null)
//         record.AddRange(EncodeVarInt(-1));
//     else
//     {
//         record.AddRange(EncodeVarInt(key.Length));
//         record.AddRange(key);
//     }
//
//     // Value (VarInt length + bytes)
//     record.AddRange(EncodeVarInt(value.Length));
//     record.AddRange(value);
//
//     // Headers count (0 for now)
//     record.AddRange(EncodeVarInt(0));
//
//     // Prepend record length
//     var lengthPrefix = EncodeVarInt(record.Count);
//     return lengthPrefix.Concat(record).ToArray();
// }
Console.WriteLine("(uncomment code to build a record)");
Console.WriteLine();

// TODO: Step 3 - Add message headers
Console.WriteLine("--- Message Headers ---");
// Headers are key-value pairs for metadata
//
// var headers = new Dictionary<string, byte[]>
// {
//     ["trace-id"] = Encoding.UTF8.GetBytes("abc123"),
//     ["content-type"] = Encoding.UTF8.GetBytes("application/json")
// };
//
// static byte[] BuildRecordWithHeaders(int timestampDelta, int offsetDelta,
//     byte[]? key, byte[] value, Dictionary<string, byte[]> headers)
// {
//     var record = new List<byte>();
//     // ... (same as before up to value)
//
//     // Headers count
//     record.AddRange(EncodeVarInt(headers.Count));
//
//     foreach (var header in headers)
//     {
//         var keyBytes = Encoding.UTF8.GetBytes(header.Key);
//         record.AddRange(EncodeVarInt(keyBytes.Length));
//         record.AddRange(keyBytes);
//         record.AddRange(EncodeVarInt(header.Value.Length));
//         record.AddRange(header.Value);
//     }
//
//     var lengthPrefix = EncodeVarInt(record.Count);
//     return lengthPrefix.Concat(record).ToArray();
// }
Console.WriteLine("(uncomment code to add headers)");
Console.WriteLine();

// TODO: Step 4 - Calculate CRC32C
Console.WriteLine("--- CRC32C Validation ---");
// Kafka uses CRC32C (Castagnoli) for data integrity
//
// var testData = Encoding.UTF8.GetBytes("Hello, Kafka!");
// var crc = Crc32.HashToUInt32(testData);
// Console.WriteLine($"Data: \"Hello, Kafka!\"");
// Console.WriteLine($"CRC32C: 0x{crc:X8}");
Console.WriteLine("(uncomment code to calculate CRC)");
Console.WriteLine();

// TODO: Step 5 - Implement GZIP compression
Console.WriteLine("--- Compression ---");
// Compression is applied to the records portion of a batch
//
// static byte[] CompressGzip(byte[] data)
// {
//     using var output = new MemoryStream();
//     using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
//     {
//         gzip.Write(data);
//     }
//     return output.ToArray();
// }
//
// static byte[] DecompressGzip(byte[] data)
// {
//     using var input = new MemoryStream(data);
//     using var gzip = new GZipStream(input, CompressionMode.Decompress);
//     using var output = new MemoryStream();
//     gzip.CopyTo(output);
//     return output.ToArray();
// }
//
// var original = new byte[1024];
// new Random(42).NextBytes(original);
// var compressed = CompressGzip(original);
// Console.WriteLine($"Original size: {original.Length} bytes");
// Console.WriteLine($"Compressed size: {compressed.Length} bytes");
// Console.WriteLine($"Ratio: {(1.0 - (double)compressed.Length / original.Length) * 100:F1}%");
Console.WriteLine("(uncomment code to test compression)");
Console.WriteLine();

// TODO: Step 6 - Record batch structure
Console.WriteLine("--- Record Batch Structure ---");
Console.WriteLine("A record batch contains:");
Console.WriteLine("  Base Offset (8 bytes)");
Console.WriteLine("  Batch Length (4 bytes)");
Console.WriteLine("  Partition Leader Epoch (4 bytes)");
Console.WriteLine("  Magic (1 byte) = 2");
Console.WriteLine("  CRC32C (4 bytes)");
Console.WriteLine("  Attributes (2 bytes) - compression, timestamp type, etc.");
Console.WriteLine("  Last Offset Delta (4 bytes)");
Console.WriteLine("  First Timestamp (8 bytes)");
Console.WriteLine("  Max Timestamp (8 bytes)");
Console.WriteLine("  Producer ID (8 bytes)");
Console.WriteLine("  Producer Epoch (2 bytes)");
Console.WriteLine("  Base Sequence (4 bytes)");
Console.WriteLine("  Records (variable)");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Records use VarInt for efficient encoding of small values");
Console.WriteLine("2. ZigZag encoding maps negative numbers to positive for VarInt");
Console.WriteLine("3. CRC32C validates data integrity (hardware accelerated)");
Console.WriteLine("4. Compression is applied to records within a batch");
Console.WriteLine("5. Headers provide metadata without changing the message value");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Helper: VarInt encoding (ZigZag)
static byte[] EncodeVarInt(int value)
{
    uint encoded = (uint)((value << 1) ^ (value >> 31));
    var result = new List<byte>();
    while (encoded > 0x7F)
    {
        result.Add((byte)((encoded & 0x7F) | 0x80));
        encoded >>= 7;
    }
    result.Add((byte)encoded);
    return result.ToArray();
}
