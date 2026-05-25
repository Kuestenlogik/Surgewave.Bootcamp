// Surgewave Bootcamp - Unit 19, Lesson 1: Protocol Basics
// COMPLETED SOLUTION

using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 19.1 - Protocol Basics ===");
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

// Step 1 - Understand the request header structure
Console.WriteLine("Request Header Structure:");
Console.WriteLine("  API Key: 2 bytes (big-endian)");
Console.WriteLine("  API Version: 2 bytes (big-endian)");
Console.WriteLine("  Correlation ID: 4 bytes (big-endian)");
Console.WriteLine("  Client ID: nullable string");
Console.WriteLine();

// Step 2 - Build an ApiVersions request header manually
var correlationId = 1;
var clientId = "test-client";
var header = BuildRequestHeader(18, 0, correlationId, clientId);

// Step 3 - Build the complete request with length prefix
var request = new byte[4 + header.Length];
BinaryPrimitives.WriteInt32BigEndian(request.AsSpan(0, 4), header.Length);
header.CopyTo(request.AsSpan(4));

Console.WriteLine("Building ApiVersions request (API Key 18, Version 0)...");
Console.WriteLine($"Request bytes: {BitConverter.ToString(request).Replace("-", " ")}");
Console.WriteLine();
Console.WriteLine("Breakdown:");
Console.WriteLine($"  Length prefix: {BitConverter.ToString(request.AsSpan(0, 4).ToArray()).Replace("-", " ")} ({header.Length} bytes)");
Console.WriteLine($"  API Key:       {BitConverter.ToString(request.AsSpan(4, 2).ToArray()).Replace("-", " ")} (18 = ApiVersions)");
Console.WriteLine($"  API Version:   {BitConverter.ToString(request.AsSpan(6, 2).ToArray()).Replace("-", " ")} (0)");
Console.WriteLine($"  Correlation:   {BitConverter.ToString(request.AsSpan(8, 4).ToArray()).Replace("-", " ")} ({correlationId})");
Console.WriteLine($"  Client ID len: {BitConverter.ToString(request.AsSpan(12, 2).ToArray()).Replace("-", " ")} ({clientId.Length} bytes)");
Console.WriteLine($"  Client ID:     {BitConverter.ToString(request.AsSpan(14).ToArray()).Replace("-", " ")} (\"{clientId}\")");

// Step 4 - Send the request and receive the response
using var tcpClient = new TcpClient("localhost", runtime.Port);
using var stream = tcpClient.GetStream();

await stream.WriteAsync(request);
Console.WriteLine("\nSent request to Surgewave broker...");

// Read response length (4 bytes)
var lengthBuffer = new byte[4];
await stream.ReadExactlyAsync(lengthBuffer);
var responseLength = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);
Console.WriteLine($"Response length: {responseLength} bytes");

// Read response body
var responseBuffer = new byte[responseLength];
await stream.ReadExactlyAsync(responseBuffer);

// Step 5 - Parse the response correlation ID
var responseCorrelationId = BinaryPrimitives.ReadInt32BigEndian(responseBuffer.AsSpan(0, 4));
Console.WriteLine($"\nResponse correlation ID: {responseCorrelationId}");
Console.WriteLine($"Matches request: {responseCorrelationId == correlationId}");

// Step 6 - Parse the ApiVersions response body
var offset = 4; // Skip correlation ID
var errorCode = BinaryPrimitives.ReadInt16BigEndian(responseBuffer.AsSpan(offset, 2));
offset += 2;

Console.WriteLine($"\nApiVersions Response:");
Console.WriteLine($"  Error Code: {errorCode} ({(errorCode == 0 ? "Success" : "Error")})");

if (errorCode == 0)
{
    var arrayLength = BinaryPrimitives.ReadInt32BigEndian(responseBuffer.AsSpan(offset, 4));
    offset += 4;

    Console.WriteLine($"\n  Supported APIs ({arrayLength} total):");

    var apisToShow = Math.Min(arrayLength, 15);
    for (int i = 0; i < apisToShow; i++)
    {
        var apiKey = BinaryPrimitives.ReadInt16BigEndian(responseBuffer.AsSpan(offset, 2));
        offset += 2;
        var minVersion = BinaryPrimitives.ReadInt16BigEndian(responseBuffer.AsSpan(offset, 2));
        offset += 2;
        var maxVersion = BinaryPrimitives.ReadInt16BigEndian(responseBuffer.AsSpan(offset, 2));
        offset += 2;

        var apiName = GetApiName(apiKey);
        Console.WriteLine($"    API {apiKey,2} ({apiName,-18}): versions {minVersion}-{maxVersion}");
    }

    if (arrayLength > apisToShow)
        Console.WriteLine($"    ... and {arrayLength - apisToShow} more APIs");
}

Console.WriteLine();
Console.WriteLine("--- Protocol Negotiation Example ---");
Console.WriteLine("Client wants to use Produce API:");
Console.WriteLine("  1. Client checks ApiVersions response for API Key 0 (Produce)");
Console.WriteLine("  2. Broker supports versions 0-9");
Console.WriteLine("  3. Client supports versions 0-7");
Console.WriteLine("  4. Client selects version 7 (highest common version)");
Console.WriteLine("  5. All Produce requests use version 7");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. All multi-byte integers are big-endian (network byte order)");
Console.WriteLine("2. Every request has a correlation ID that matches the response");
Console.WriteLine("3. ApiVersions (API Key 18) is used for protocol negotiation");
Console.WriteLine("4. Clients should use the highest mutually supported version");
Console.WriteLine("5. Nullable strings use -1 (0xFFFF) to indicate null");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Helper method to build a request header
static byte[] BuildRequestHeader(short apiKey, short apiVersion, int correlationId, string? clientId)
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

// Helper method to write a nullable string
static void WriteNullableString(BinaryWriter writer, string? value)
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

// Helper method to get API name from API key
static string GetApiName(short apiKey) => apiKey switch
{
    0 => "Produce",
    1 => "Fetch",
    2 => "ListOffsets",
    3 => "Metadata",
    4 => "LeaderAndIsr",
    5 => "StopReplica",
    6 => "UpdateMetadata",
    7 => "ControlledShutdown",
    8 => "OffsetCommit",
    9 => "OffsetFetch",
    10 => "FindCoordinator",
    11 => "JoinGroup",
    12 => "Heartbeat",
    13 => "LeaveGroup",
    14 => "SyncGroup",
    15 => "DescribeGroups",
    16 => "ListGroups",
    17 => "SaslHandshake",
    18 => "ApiVersions",
    19 => "CreateTopics",
    20 => "DeleteTopics",
    21 => "DeleteRecords",
    22 => "InitProducerId",
    23 => "OffsetForLeaderEpoch",
    24 => "AddPartitionsToTxn",
    25 => "AddOffsetsToTxn",
    26 => "EndTxn",
    27 => "WriteTxnMarkers",
    28 => "TxnOffsetCommit",
    _ => $"Unknown({apiKey})"
};
