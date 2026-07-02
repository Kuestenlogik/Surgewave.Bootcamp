// Surgewave Bootcamp - Unit 19, Lesson 1: Protocol Basics
//
// In this lesson, you'll learn:
// - Kafka wire protocol structure
// - Request/response format with headers
// - API keys and versioning
// - Correlation ID tracking
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 19.1 - Protocol Basics ===");
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

// TODO: Step 1 - Understand the request header structure
Console.WriteLine("Request Header Structure:");
Console.WriteLine("  API Key: 2 bytes (big-endian)");
Console.WriteLine("  API Version: 2 bytes (big-endian)");
Console.WriteLine("  Correlation ID: 4 bytes (big-endian)");
Console.WriteLine("  Client ID: nullable string");
Console.WriteLine();

// TODO: Step 2 - Build an ApiVersions request header manually
// The ApiVersions request (API Key 18) is the simplest request to build
// because it has no body in version 0.
//
// byte[] BuildRequestHeader(short apiKey, short apiVersion, int correlationId, string? clientId)
// {
//     using var ms = new MemoryStream();
//     using var writer = new BinaryWriter(ms);
//
//     // Write API Key (big-endian) - API Key 18 is ApiVersions
//     // ...
//
//     // Write API Version (big-endian) - Version 0
//     // ...
//
//     // Write Correlation ID (big-endian)
//     // ...
//
//     // Write Client ID (nullable string: length prefix + UTF-8 bytes)
//     // ...
//
//     return ms.ToArray();
// }

// TODO: Step 3 - Build the complete ApiVersions request with length prefix
// var correlationId = 1;
// var clientId = "test-client";
// var header = BuildRequestHeader(18, 0, correlationId, clientId);
//
// // Add 4-byte length prefix (total message size excluding the length field itself)
// var request = new byte[4 + header.Length];
// BinaryPrimitives.WriteInt32BigEndian(request.AsSpan(0, 4), header.Length);
// header.CopyTo(request.AsSpan(4));
//
// Console.WriteLine("Building ApiVersions request (API Key 18, Version 0)...");
// Console.WriteLine($"Request bytes: {BitConverter.ToString(request).Replace("-", " ")}");

// TODO: Step 4 - Send the request and receive the response
// using var tcpClient = new TcpClient("localhost", runtime.Port);
// using var stream = tcpClient.GetStream();
//
// await stream.WriteAsync(request);
// Console.WriteLine("\nSent request to Surgewave broker...");
//
// // Read response length (4 bytes)
// var lengthBuffer = new byte[4];
// await stream.ReadExactlyAsync(lengthBuffer);
// var responseLength = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);
//
// // Read response body
// var responseBuffer = new byte[responseLength];
// await stream.ReadExactlyAsync(responseBuffer);

// TODO: Step 5 - Parse the response correlation ID
// var responseCorrelationId = BinaryPrimitives.ReadInt32BigEndian(responseBuffer.AsSpan(0, 4));
// Console.WriteLine($"\nResponse correlation ID: {responseCorrelationId}");
// Console.WriteLine($"Matches request: {responseCorrelationId == correlationId}");

// TODO: Step 6 - Parse the ApiVersions response body
// Parse error code and API version array from the response
//
// var offset = 4; // Skip correlation ID
// var errorCode = BinaryPrimitives.ReadInt16BigEndian(responseBuffer.AsSpan(offset, 2));
// offset += 2;
//
// Console.WriteLine($"\nApiVersions Response:");
// Console.WriteLine($"  Error Code: {errorCode}");
//
// if (errorCode == 0)
// {
//     var arrayLength = BinaryPrimitives.ReadInt32BigEndian(responseBuffer.AsSpan(offset, 4));
//     offset += 4;
//
//     Console.WriteLine($"  Supported APIs ({arrayLength} total):");
//
//     for (int i = 0; i < arrayLength && i < 10; i++)  // Show first 10
//     {
//         var apiKey = BinaryPrimitives.ReadInt16BigEndian(responseBuffer.AsSpan(offset, 2));
//         offset += 2;
//         var minVersion = BinaryPrimitives.ReadInt16BigEndian(responseBuffer.AsSpan(offset, 2));
//         offset += 2;
//         var maxVersion = BinaryPrimitives.ReadInt16BigEndian(responseBuffer.AsSpan(offset, 2));
//         offset += 2;
//
//         var apiName = GetApiName(apiKey);
//         Console.WriteLine($"    API {apiKey,2} ({apiName,-18}): versions {minVersion}-{maxVersion}");
//     }
//
//     if (arrayLength > 10)
//         Console.WriteLine($"    ... and {arrayLength - 10} more APIs");
// }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. All multi-byte integers are big-endian (network byte order)");
Console.WriteLine("2. Every request has a correlation ID that matches the response");
Console.WriteLine("3. ApiVersions (API Key 18) is used for protocol negotiation");
Console.WriteLine("4. Clients should use the highest mutually supported version");
Console.WriteLine("5. Nullable strings use -1 (0xFFFF) to indicate null");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

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
    _ => $"Unknown({apiKey})"
};
