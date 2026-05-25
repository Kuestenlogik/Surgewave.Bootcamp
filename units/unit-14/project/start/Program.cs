// Unit 19 Project: Mini Wire Protocol Client
// Build a minimal TCP-based Surgewave client using raw sockets

using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Mini Wire Protocol Client ===\n");

// Start embedded Surgewave broker
Console.WriteLine("[Surgewave] Starting embedded broker...");

await using var surgewave = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .Build()
    .StartAsync();

await using var surgewaveClient = await SurgewaveClient.Create(surgewave.BootstrapServers).BuildAsync();

// Create a test topic
await surgewaveClient.NativeClient!.Topics.CreateAsync("telemetry", partitions: 3);
Console.WriteLine($"[Surgewave] Created topic: telemetry (3 partitions)");

var brokerHost = "localhost";
var brokerPort = surgewave.Port;

// TODO: Step 1 - Establish TCP connection
// Use TcpClient to connect to the broker
Console.WriteLine($"\n[TCP] Connecting to {brokerHost}:{brokerPort}...");

// TODO: Create TcpClient and connect
// Hint: var tcpClient = new TcpClient();
//       await tcpClient.ConnectAsync(brokerHost, brokerPort);
//       var stream = tcpClient.GetStream();

Console.WriteLine("[TCP] Connected to Surgewave broker");

// TODO: Step 2 - Build Metadata Request
// API Key 3 = Metadata, Version 0
// Request format:
//   Size (4 bytes) | API Key (2) | API Version (2) | Correlation ID (4) | Client ID | Topics Array
Console.WriteLine("\n[Protocol] Building Metadata request (API Key 3)...");

var correlationId = 1;
var clientId = "mini-client";

// TODO: Build the request frame
// Hint: Use MemoryStream and BinaryWriter, or work with byte arrays directly
// Remember: Kafka uses BIG ENDIAN byte order!

// Frame structure:
// - API Key: INT16 (2 bytes) = 3 for Metadata
// - API Version: INT16 (2 bytes) = 0
// - Correlation ID: INT32 (4 bytes)
// - Client ID: NULLABLE_STRING (2 bytes length + bytes)
// - Topics: NULLABLE_ARRAY = -1 for null (request all topics)

// TODO: Write request frame to buffer
// byte[] requestBuffer = ...;

// TODO: Calculate total size and prepend 4-byte length
// int frameSize = requestBuffer.Length;
// Write size prefix + request to stream

Console.WriteLine($"[Protocol] Request: CorrelationId={correlationId}, ClientId={clientId}");
// Console.WriteLine($"[Protocol] Frame size: {frameSize} bytes");

// TODO: Step 3 - Send the request
// await stream.WriteAsync(frameWithLength);
// await stream.FlushAsync();

Console.WriteLine("[Protocol] Sent Metadata request");

// TODO: Step 4 - Read the response
// First read 4 bytes for response length
// byte[] lengthBuffer = new byte[4];
// await stream.ReadExactlyAsync(lengthBuffer);
// int responseLength = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);

// Then read the response body
// byte[] responseBuffer = new byte[responseLength];
// await stream.ReadExactlyAsync(responseBuffer);

Console.WriteLine("\n[Protocol] Received Metadata response");

// TODO: Step 5 - Parse the response
// - Read Correlation ID (4 bytes) - should match our request
// - Read broker information
// - Read topic metadata

// Hint: Create a ReadOnlySpan<byte> and use BinaryPrimitives to read values
// var span = new ReadOnlySpan<byte>(responseBuffer);
// var respCorrelationId = BinaryPrimitives.ReadInt32BigEndian(span);

Console.WriteLine($"[Protocol] Response: CorrelationId=?");
Console.WriteLine("[Protocol] Topics discovered: ?");

// TODO: Step 6 - Build Produce Request
// API Key 0 = Produce, Version 0
Console.WriteLine("\n[Protocol] Building Produce request (API Key 0)...");

correlationId = 2;
var topic = "telemetry";
var partition = 0;
var message = """{"sensor":"temp-01","value":23.5}""";

Console.WriteLine($"[Protocol] Topic: {topic}, Partition: {partition}");
Console.WriteLine($"[Protocol] Message: {message}");

// TODO: Build Produce request
// Produce Request v0:
// - acks: INT16 (1 = leader only, -1 = all)
// - timeout: INT32 (milliseconds)
// - topics: ARRAY of topic data
//   - topic name: STRING
//   - partitions: ARRAY of partition data
//     - partition: INT32
//     - record set: RECORDS (complex format, simplified here)

// TODO: Send produce request and read response

// TODO: Step 7 - Parse Produce response
// - base_offset: INT64
// - error_code: INT16
// - timestamp: INT64 (if supported)

Console.WriteLine("\n[Protocol] Received Produce response");
Console.WriteLine("[Protocol] Offset assigned: ?");

// TODO: Step 8 - Clean up
// tcpClient.Close();

Console.WriteLine("\n=== Mini Client Complete ===");
