// Surgewave Bootcamp - Unit 19, Lesson 4: Protocol Debugging
// COMPLETED SOLUTION

using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 19.4 - Protocol Debugging ===");
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

// Step 1 - Create a hex dump of a sample request
Console.WriteLine("--- Hex Dump Demo ---");
var sampleRequest = BuildMetadataRequest(1, "minimal-client");
Console.WriteLine("Sample Metadata Request:");
Console.WriteLine(HexDump.Format(sampleRequest));

// Step 2 - Analyze the request structure
Console.WriteLine("--- Request Analysis ---");
AnalyzeRequest(sampleRequest);
Console.WriteLine();

// Step 3 - Demonstrate error code lookup
Console.WriteLine("--- Error Code Reference ---");
var errorCodes = new short[] { 0, 1, 3, 5, 6, 7, 35 };
foreach (var code in errorCodes)
{
    Console.WriteLine($"  Error {code,2}: {KafkaErrors.Describe(code)}");
}
Console.WriteLine();

// Step 4 - Use protocol tracer for request/response
Console.WriteLine("--- Protocol Tracing ---");
using var tcpClient = new TcpClient("localhost", runtime.Port);
using var stream = tcpClient.GetStream();

var tracer = new ProtocolTracer(stream, verbose: false);

// Send traced requests
var metadataRequest = BuildMetadataRequest(1, "debug-client");
var response = await tracer.SendRequestAsync(metadataRequest, "Metadata (all topics)");

// Analyze the response
AnalyzeResponse(response);

// Step 5 - Debug exercise - common problems
Console.WriteLine("\n--- Debug Exercise: Common Problems ---");
DebugExercise();

// Step 6 - Demonstrate verbose tracing
Console.WriteLine("\n--- Verbose Trace Example ---");
using var tcpClient2 = new TcpClient("localhost", runtime.Port);
using var stream2 = tcpClient2.GetStream();

var verboseTracer = new ProtocolTracer(stream2, verbose: true);
var apiVersionsRequest = BuildApiVersionsRequest(2, "verbose-client");
var apiResponse = await verboseTracer.SendRequestAsync(apiVersionsRequest, "ApiVersions");

// Parse some API versions from response
Console.WriteLine("\n--- Parsed API Versions ---");
ParseApiVersionsResponse(apiResponse);

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

static byte[] BuildApiVersionsRequest(int correlationId, string clientId)
{
    var writer = new ProtocolWriter();
    writer.WriteInt16(18);  // API Key (ApiVersions)
    writer.WriteInt16(0);   // Version
    writer.WriteInt32(correlationId);
    writer.WriteString(clientId);

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

    var length = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0, 4));
    Console.WriteLine($"  Length field: {length} (payload should be {length} bytes)");

    var actualPayload = data.Length - 4;
    if (length != actualPayload)
    {
        Console.WriteLine($"  WARNING: Length mismatch! Actual payload: {actualPayload}");
    }

    var apiKey = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(4, 2));
    Console.WriteLine($"  API Key: {apiKey} ({GetApiName(apiKey)})");

    var apiVersion = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(6, 2));
    Console.WriteLine($"  API Version: {apiVersion}");

    var correlationId = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(8, 4));
    Console.WriteLine($"  Correlation ID: {correlationId}");

    var clientIdLen = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(12, 2));
    if (clientIdLen > 0)
    {
        var clientId = Encoding.UTF8.GetString(data, 14, clientIdLen);
        Console.WriteLine($"  Client ID: \"{clientId}\" ({clientIdLen} bytes)");
    }
    else if (clientIdLen == -1)
    {
        Console.WriteLine("  Client ID: null");
    }
}

static void AnalyzeResponse(byte[] data)
{
    Console.WriteLine("\nResponse Structure:");
    Console.WriteLine($"  Total size: {data.Length} bytes");
    Console.WriteLine($"  Correlation ID: {BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0, 4))}");

    // Try to detect error code
    if (data.Length >= 6)
    {
        var possibleError = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(4, 2));
        if (possibleError != 0)
        {
            Console.WriteLine($"  Possible error at offset 4: {possibleError} ({KafkaErrors.Describe(possibleError)})");
        }
    }
}

static void ParseApiVersionsResponse(byte[] data)
{
    var offset = 4; // Skip correlation ID

    var errorCode = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(offset, 2));
    offset += 2;

    if (errorCode != 0)
    {
        Console.WriteLine($"Error: {KafkaErrors.Describe(errorCode)}");
        return;
    }

    var count = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset, 4));
    offset += 4;

    Console.WriteLine($"Supported APIs ({count} total):");
    for (int i = 0; i < Math.Min(count, 10); i++)
    {
        var apiKey = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(offset, 2));
        offset += 2;
        var minVersion = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(offset, 2));
        offset += 2;
        var maxVersion = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(offset, 2));
        offset += 2;

        Console.WriteLine($"  {GetApiName(apiKey),-20} (API {apiKey,2}): v{minVersion}-{maxVersion}");
    }

    if (count > 10)
        Console.WriteLine($"  ... and {count - 10} more");
}

static string GetApiName(short apiKey) => apiKey switch
{
    0 => "Produce",
    1 => "Fetch",
    2 => "ListOffsets",
    3 => "Metadata",
    8 => "OffsetCommit",
    9 => "OffsetFetch",
    10 => "FindCoordinator",
    11 => "JoinGroup",
    12 => "Heartbeat",
    13 => "LeaveGroup",
    14 => "SyncGroup",
    18 => "ApiVersions",
    19 => "CreateTopics",
    20 => "DeleteTopics",
    _ => $"Unknown({apiKey})"
};

static void DebugExercise()
{
    // Problem 1: Wrong byte order
    Console.WriteLine("\n1. Wrong Byte Order:");
    var wrongOrder = new byte[] { 0x2e, 0x00, 0x00, 0x00 };
    var correctOrder = new byte[] { 0x00, 0x00, 0x00, 0x2e };
    Console.WriteLine($"   Little-endian (wrong): {BitConverter.ToString(wrongOrder)} -> {BitConverter.ToInt32(wrongOrder)}");
    Console.WriteLine($"   Big-endian (correct):  {BitConverter.ToString(correctOrder)} -> {BinaryPrimitives.ReadInt32BigEndian(correctOrder)}");

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
        var startTime = DateTime.Now;

        // Parse request header for logging
        var apiKey = BinaryPrimitives.ReadInt16BigEndian(request.AsSpan(4, 2));
        var apiVersion = BinaryPrimitives.ReadInt16BigEndian(request.AsSpan(6, 2));
        var correlationId = BinaryPrimitives.ReadInt32BigEndian(request.AsSpan(8, 4));

        Console.WriteLine($"\n[{startTime:HH:mm:ss.fff}] -> REQUEST: {description}");
        Console.WriteLine($"  API: {GetApiName(apiKey)} ({apiKey}) v{apiVersion}");
        Console.WriteLine($"  Correlation ID: {correlationId}");
        Console.WriteLine($"  Size: {request.Length} bytes");

        if (_verbose)
        {
            Console.WriteLine("  Hex dump:");
            var hexLines = HexDump.Format(request).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in hexLines.Take(8))
                Console.WriteLine($"    {line}");
            if (hexLines.Length > 8)
                Console.WriteLine($"    ... ({hexLines.Length - 8} more lines)");
        }

        await _stream.WriteAsync(request);

        var lengthBuffer = new byte[4];
        await _stream.ReadExactlyAsync(lengthBuffer);
        var responseLength = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);

        var response = new byte[responseLength];
        await _stream.ReadExactlyAsync(response);

        var latency = (DateTime.Now - startTime).TotalMilliseconds;
        var responseCorrelationId = BinaryPrimitives.ReadInt32BigEndian(response.AsSpan(0, 4));

        Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss.fff}] <- RESPONSE");
        Console.WriteLine($"  Correlation ID: {responseCorrelationId}");
        Console.WriteLine($"  Size: {response.Length} bytes");
        Console.WriteLine($"  Latency: {latency:F1}ms");

        if (responseCorrelationId != correlationId)
        {
            Console.WriteLine($"  WARNING: Correlation ID mismatch! Expected {correlationId}");
        }

        if (_verbose)
        {
            Console.WriteLine("  Hex dump:");
            var hexLines = HexDump.Format(response).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in hexLines.Take(8))
                Console.WriteLine($"    {line}");
            if (hexLines.Length > 8)
                Console.WriteLine($"    ... ({hexLines.Length - 8} more lines)");
        }

        return response;
    }

    private static string GetApiName(short apiKey) => apiKey switch
    {
        0 => "Produce",
        1 => "Fetch",
        3 => "Metadata",
        18 => "ApiVersions",
        _ => $"API({apiKey})"
    };
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
