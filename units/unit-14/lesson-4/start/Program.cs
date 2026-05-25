// Surgewave Bootcamp - Unit 19, Lesson 4: Protocol Debugging
//
// In this lesson, you'll learn:
// - Hex dump analysis for protocol messages
// - Error code interpretation
// - Protocol tracing and logging
// - Common debugging scenarios
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 19.4 - Protocol Debugging ===");
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

// TODO: Step 1 - Create a hex dump of a sample request
Console.WriteLine("--- Hex Dump Demo ---");
var sampleRequest = BuildMetadataRequest(1, "minimal-client");
Console.WriteLine("Sample Metadata Request:");
// Console.WriteLine(HexDump.Format(sampleRequest));
Console.WriteLine("(uncomment code to see hex dump)");
Console.WriteLine();

// TODO: Step 2 - Analyze the request structure
Console.WriteLine("--- Request Analysis ---");
// AnalyzeRequest(sampleRequest);
Console.WriteLine("(uncomment code to see analysis)");
Console.WriteLine();

// TODO: Step 3 - Demonstrate error code lookup
Console.WriteLine("--- Error Code Reference ---");
var errorCodes = new short[] { 0, 1, 3, 5, 6, 7, 35 };
foreach (var code in errorCodes)
{
    Console.WriteLine($"  Error {code,2}: {KafkaErrors.Describe(code)}");
}
Console.WriteLine();

// TODO: Step 4 - Use protocol tracer for request/response
Console.WriteLine("--- Protocol Tracing ---");
// using var tcpClient = new TcpClient("localhost", runtime.Port);
// using var stream = tcpClient.GetStream();
//
// var tracer = new ProtocolTracer(stream, verbose: true);
//
// // Send traced requests
// var metadataRequest = BuildMetadataRequest(1, "debug-client");
// var response = await tracer.SendRequestAsync(metadataRequest, "Metadata (all topics)");
//
// // Analyze the response
// AnalyzeResponse(response);
Console.WriteLine("(uncomment code to see protocol tracing)");
Console.WriteLine();

// TODO: Step 5 - Debug exercise - common problems
Console.WriteLine("--- Debug Exercise: Common Problems ---");
DebugExercise();

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Hex dumps show exact bytes on the wire");
Console.WriteLine("2. Always check byte order (big-endian for Kafka)");
Console.WriteLine("3. Correlation ID mismatches indicate response routing issues");
Console.WriteLine("4. Error codes in responses explain failures");
Console.WriteLine("5. Protocol tracing helps diagnose timing issues");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// --- Helper Methods ---

static byte[] BuildMetadataRequest(int correlationId, string clientId)
{
    var writer = new ProtocolWriter();
    writer.WriteInt16(3);  // API Key
    writer.WriteInt16(0);  // Version
    writer.WriteInt32(correlationId);
    writer.WriteString(clientId);
    writer.WriteInt32(-1); // All topics

    var body = writer.ToArray();
    var framed = new byte[4 + body.Length];
    BinaryPrimitives.WriteInt32BigEndian(framed.AsSpan(0, 4), body.Length);
    body.CopyTo(framed.AsSpan(4));
    return framed;
}

static void AnalyzeRequest(byte[] data)
{
    Console.WriteLine("Request Structure:");
    Console.WriteLine($"  Total size: {data.Length} bytes");
    Console.WriteLine($"  Length field: {BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0, 4))}");
    Console.WriteLine($"  API Key: {BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(4, 2))}");
    Console.WriteLine($"  API Version: {BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(6, 2))}");
    Console.WriteLine($"  Correlation ID: {BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(8, 4))}");

    var clientIdLen = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(12, 2));
    if (clientIdLen > 0)
    {
        var clientId = Encoding.UTF8.GetString(data, 14, clientIdLen);
        Console.WriteLine($"  Client ID: \"{clientId}\"");
    }
}

static void AnalyzeResponse(byte[] data)
{
    Console.WriteLine("\nResponse Structure:");
    Console.WriteLine($"  Total size: {data.Length} bytes");
    Console.WriteLine($"  Correlation ID: {BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0, 4))}");
}

static void DebugExercise()
{
    // Problem 1: Wrong byte order
    Console.WriteLine("\n1. Wrong Byte Order:");
    Console.WriteLine("   Little-endian (wrong): 0x2e000000 interpreted as 771751936");
    Console.WriteLine("   Big-endian (correct):  0x0000002e interpreted as 46");

    // Problem 2: Missing length prefix
    Console.WriteLine("\n2. Missing Length Prefix:");
    Console.WriteLine("   Symptom: Broker reads garbage length, either hangs or rejects");
    Console.WriteLine("   Fix: Always prepend 4-byte big-endian length");

    // Problem 3: Truncated message
    Console.WriteLine("\n3. Truncated Message:");
    Console.WriteLine("   Symptom: Broker waits indefinitely for remaining bytes");
    Console.WriteLine("   Fix: Ensure full message is sent before closing connection");

    // Problem 4: Wrong API version
    Console.WriteLine("\n4. Wrong API Version:");
    Console.WriteLine("   Symptom: Error code 35 (UNSUPPORTED_VERSION)");
    Console.WriteLine("   Fix: Query ApiVersions first, use supported version");

    // Problem 5: Correlation ID not unique
    Console.WriteLine("\n5. Duplicate Correlation ID:");
    Console.WriteLine("   Symptom: Responses routed to wrong request handlers");
    Console.WriteLine("   Fix: Use atomic counter for unique correlation IDs");
}

// Hex Dump Utility
static class HexDump
{
    public static string Format(byte[] data, int bytesPerLine = 16)
    {
        var sb = new StringBuilder();

        for (int offset = 0; offset < data.Length; offset += bytesPerLine)
        {
            sb.Append($"{offset:X8}  ");

            for (int i = 0; i < bytesPerLine; i++)
            {
                if (offset + i < data.Length)
                    sb.Append($"{data[offset + i]:X2} ");
                else
                    sb.Append("   ");
                if (i == 7) sb.Append(" ");
            }

            sb.Append(" |");
            for (int i = 0; i < bytesPerLine && offset + i < data.Length; i++)
            {
                byte b = data[offset + i];
                sb.Append(b >= 32 && b < 127 ? (char)b : '.');
            }
            sb.AppendLine("|");
        }

        return sb.ToString();
    }
}

// Kafka Error Codes
static class KafkaErrors
{
    private static readonly Dictionary<short, string> Errors = new()
    {
        { 0, "NONE (Success)" },
        { -1, "UNKNOWN_SERVER_ERROR" },
        { 1, "OFFSET_OUT_OF_RANGE" },
        { 2, "CORRUPT_MESSAGE" },
        { 3, "UNKNOWN_TOPIC_OR_PARTITION" },
        { 5, "LEADER_NOT_AVAILABLE" },
        { 6, "NOT_LEADER_OR_FOLLOWER" },
        { 7, "REQUEST_TIMED_OUT" },
        { 35, "UNSUPPORTED_VERSION" },
        { 36, "TOPIC_ALREADY_EXISTS" },
    };

    public static string Describe(short code) =>
        Errors.TryGetValue(code, out var desc) ? desc : $"UNKNOWN({code})";
}

// Protocol Tracer
class ProtocolTracer
{
    private readonly NetworkStream _stream;
    private readonly bool _verbose;

    public ProtocolTracer(NetworkStream stream, bool verbose) =>
        (_stream, _verbose) = (stream, verbose);

    public async Task<byte[]> SendRequestAsync(byte[] request, string description)
    {
        Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss.fff}] -> REQUEST: {description}");
        Console.WriteLine($"  Size: {request.Length} bytes");

        if (_verbose)
            Console.WriteLine(HexDump.Format(request));

        await _stream.WriteAsync(request);

        var lengthBuffer = new byte[4];
        await _stream.ReadExactlyAsync(lengthBuffer);
        var responseLength = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);

        var response = new byte[responseLength];
        await _stream.ReadExactlyAsync(response);

        Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss.fff}] <- RESPONSE");
        Console.WriteLine($"  Size: {response.Length} bytes");

        if (_verbose)
            Console.WriteLine(HexDump.Format(response));

        return response;
    }
}

// Protocol Writer
class ProtocolWriter
{
    private readonly MemoryStream _stream = new();

    public void WriteInt16(short value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(buffer, value);
        _stream.Write(buffer);
    }

    public void WriteInt32(int value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        _stream.Write(buffer);
    }

    public void WriteString(string? value)
    {
        if (value == null) { WriteInt16(-1); return; }
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteInt16((short)bytes.Length);
        _stream.Write(bytes);
    }

    public byte[] ToArray() => _stream.ToArray();
}
