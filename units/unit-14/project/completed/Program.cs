// Unit 19 Project: Mini Wire Protocol Client - COMPLETED
// A minimal TCP-based Surgewave client using raw sockets

using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Mini Wire Protocol Client ===\n");

// Start embedded Surgewave broker
Console.WriteLine("[Surgewave] Starting embedded broker...");

await using var surgewave = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .Build()
    .StartAsync();

await using var surgewaveClient = await SurgewaveClient.Create(surgewave.BootstrapServers).BuildAsync();

// Create test topics
await surgewaveClient.NativeClient!.Topics.CreateAsync("telemetry", partitions: 3);
await surgewaveClient.NativeClient!.Topics.CreateAsync("events", partitions: 1);
Console.WriteLine("[Surgewave] Created topics: telemetry (3 partitions), events (1 partition)");

var brokerHost = "localhost";
var brokerPort = surgewave.Port;

// Step 1: Establish TCP connection
Console.WriteLine($"\n[TCP] Connecting to {brokerHost}:{brokerPort}...");

using var tcpClient = new TcpClient();
await tcpClient.ConnectAsync(brokerHost, brokerPort);
var stream = tcpClient.GetStream();

Console.WriteLine("[TCP] Connected to Surgewave broker");

// Step 2: Build and send Metadata Request
Console.WriteLine("\n[Protocol] Sending Metadata request (API Key 3)");

var correlationId = 1;
var clientIdBytes = Encoding.UTF8.GetBytes("mini-client");

// Build Metadata request body
using var requestStream = new MemoryStream();

// API Key (INT16) = 3 for Metadata
WriteInt16BigEndian(requestStream, 3);

// API Version (INT16) = 0
WriteInt16BigEndian(requestStream, 0);

// Correlation ID (INT32)
WriteInt32BigEndian(requestStream, correlationId);

// Client ID (NULLABLE_STRING: 2 bytes length + bytes)
WriteInt16BigEndian(requestStream, (short)clientIdBytes.Length);
requestStream.Write(clientIdBytes);

// Topics array = null (-1) to request all topics
WriteInt32BigEndian(requestStream, -1);

var requestBody = requestStream.ToArray();
Console.WriteLine($"[Protocol] Request: CorrelationId={correlationId}, ClientId=mini-client");
Console.WriteLine($"[Protocol] Frame size: {requestBody.Length + 4} bytes");

// Send with 4-byte length prefix
await SendFrameAsync(stream, requestBody);
Console.WriteLine("[Protocol] Sent Metadata request");

// Step 3: Read Metadata response
var responseBuffer = await ReadFrameAsync(stream);

Console.WriteLine("\n[Protocol] Received Metadata response");

// Parse response
var span = new ReadOnlySpan<byte>(responseBuffer);
var offset = 0;

var respCorrelationId = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset, 4));
offset += 4;

Console.WriteLine($"[Protocol] Response: CorrelationId={respCorrelationId}");

// Skip broker array (simplified parsing)
var brokerCount = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset, 4));
offset += 4;
Console.WriteLine($"[Protocol] Brokers: {brokerCount}");

// Skip broker details (node_id, host, port for each)
for (int i = 0; i < brokerCount; i++)
{
    offset += 4; // node_id
    var hostLen = BinaryPrimitives.ReadInt16BigEndian(span.Slice(offset, 2));
    offset += 2 + hostLen; // host string
    offset += 4; // port
}

// Read topic metadata
var topicCount = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset, 4));
offset += 4;

Console.WriteLine($"[Protocol] Topics discovered: {topicCount}");

var topicsInfo = new List<(string name, int partitions)>();

for (int t = 0; t < topicCount; t++)
{
    var errorCode = BinaryPrimitives.ReadInt16BigEndian(span.Slice(offset, 2));
    offset += 2;

    var topicNameLen = BinaryPrimitives.ReadInt16BigEndian(span.Slice(offset, 2));
    offset += 2;
    var topicName = Encoding.UTF8.GetString(span.Slice(offset, topicNameLen));
    offset += topicNameLen;

    var partitionCount = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset, 4));
    offset += 4;

    topicsInfo.Add((topicName, partitionCount));
    Console.WriteLine($"  - {topicName} ({partitionCount} partitions)");

    // Skip partition details
    for (int p = 0; p < partitionCount; p++)
    {
        offset += 2; // error_code
        offset += 4; // partition_id
        offset += 4; // leader
        var replicaCount = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset, 4));
        offset += 4 + replicaCount * 4; // replicas array
        var isrCount = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset, 4));
        offset += 4 + isrCount * 4; // isr array
    }
}

// Step 4: Build and send Produce Request
Console.WriteLine("\n[Protocol] Sending Produce request (API Key 0)");

correlationId = 2;
var topic = "telemetry";
var partition = 0;
var message = """{"sensor":"temp-01","value":23.5}""";
var messageBytes = Encoding.UTF8.GetBytes(message);

Console.WriteLine($"[Protocol] Topic: {topic}, Partition: {partition}");
Console.WriteLine($"[Protocol] Message: {message}");

using var produceStream = new MemoryStream();

// API Key (INT16) = 0 for Produce
WriteInt16BigEndian(produceStream, 0);

// API Version (INT16) = 0
WriteInt16BigEndian(produceStream, 0);

// Correlation ID (INT32)
WriteInt32BigEndian(produceStream, correlationId);

// Client ID (NULLABLE_STRING)
WriteInt16BigEndian(produceStream, (short)clientIdBytes.Length);
produceStream.Write(clientIdBytes);

// Required acks (INT16) = 1 (leader only)
WriteInt16BigEndian(produceStream, 1);

// Timeout (INT32) = 5000ms
WriteInt32BigEndian(produceStream, 5000);

// Topics array (1 topic)
WriteInt32BigEndian(produceStream, 1);

// Topic name
var topicBytes = Encoding.UTF8.GetBytes(topic);
WriteInt16BigEndian(produceStream, (short)topicBytes.Length);
produceStream.Write(topicBytes);

// Partitions array (1 partition)
WriteInt32BigEndian(produceStream, 1);

// Partition index
WriteInt32BigEndian(produceStream, partition);

// Build record batch (MessageSet format v0 for simplicity)
using var recordStream = new MemoryStream();

// Offset (INT64) - producer sets to 0
WriteInt64BigEndian(recordStream, 0);

// Message size placeholder - we'll calculate later
var messageSizePos = recordStream.Position;
WriteInt32BigEndian(recordStream, 0); // placeholder

var messageStartPos = recordStream.Position;

// CRC32 placeholder
WriteInt32BigEndian(recordStream, 0);

// Magic byte (INT8) = 0 for v0 message format
recordStream.WriteByte(0);

// Attributes (INT8) = 0 (no compression)
recordStream.WriteByte(0);

// Key (BYTES) = null (-1)
WriteInt32BigEndian(recordStream, -1);

// Value (BYTES)
WriteInt32BigEndian(recordStream, messageBytes.Length);
recordStream.Write(messageBytes);

// Calculate and update message size
var messageSize = (int)(recordStream.Position - messageStartPos);
var recordBytes = recordStream.ToArray();
BinaryPrimitives.WriteInt32BigEndian(recordBytes.AsSpan((int)messageSizePos, 4), messageSize);

// Calculate and update CRC32
var crcData = recordBytes.AsSpan((int)messageStartPos + 4, messageSize - 4);
var crc = CalculateCrc32(crcData);
BinaryPrimitives.WriteInt32BigEndian(recordBytes.AsSpan((int)messageStartPos, 4), (int)crc);

// Record set size (INT32)
WriteInt32BigEndian(produceStream, recordBytes.Length);
produceStream.Write(recordBytes);

var produceBody = produceStream.ToArray();

// Send Produce request
await SendFrameAsync(stream, produceBody);
Console.WriteLine("[Protocol] Sent Produce request");

// Step 5: Read Produce response
var produceResponse = await ReadFrameAsync(stream);

Console.WriteLine("\n[Protocol] Received Produce response");

var prodSpan = new ReadOnlySpan<byte>(produceResponse);
var prodOffset = 0;

var prodCorrelationId = BinaryPrimitives.ReadInt32BigEndian(prodSpan.Slice(prodOffset, 4));
prodOffset += 4;

Console.WriteLine($"[Protocol] Response: CorrelationId={prodCorrelationId}");

// Parse topic responses
var topicResponseCount = BinaryPrimitives.ReadInt32BigEndian(prodSpan.Slice(prodOffset, 4));
prodOffset += 4;

for (int t = 0; t < topicResponseCount; t++)
{
    var respTopicLen = BinaryPrimitives.ReadInt16BigEndian(prodSpan.Slice(prodOffset, 2));
    prodOffset += 2;
    var respTopic = Encoding.UTF8.GetString(prodSpan.Slice(prodOffset, respTopicLen));
    prodOffset += respTopicLen;

    var partitionResponseCount = BinaryPrimitives.ReadInt32BigEndian(prodSpan.Slice(prodOffset, 4));
    prodOffset += 4;

    for (int p = 0; p < partitionResponseCount; p++)
    {
        var respPartition = BinaryPrimitives.ReadInt32BigEndian(prodSpan.Slice(prodOffset, 4));
        prodOffset += 4;

        var errorCode = BinaryPrimitives.ReadInt16BigEndian(prodSpan.Slice(prodOffset, 2));
        prodOffset += 2;

        var baseOffset = BinaryPrimitives.ReadInt64BigEndian(prodSpan.Slice(prodOffset, 8));
        prodOffset += 8;

        if (errorCode == 0)
        {
            Console.WriteLine($"[Protocol] Offset assigned: {baseOffset}");
            Console.WriteLine($"[Protocol] Partition: {respPartition}, Error: None");
        }
        else
        {
            Console.WriteLine($"[Protocol] Error code: {errorCode}");
        }
    }
}

// Step 6: Verify the message was stored
Console.WriteLine("\n[Verify] Reading message back via Surgewave client...");

await using var consumer = surgewaveClient.CreateConsumer<string, string>();
consumer.Assign(topic, partition, offset: 0);

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

try
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(2), cts.Token);
    if (result != null)
    {
        Console.WriteLine($"[Verify] Message confirmed: {result.Value}");
    }
    else
    {
        Console.WriteLine("[Verify] No message received");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("[Verify] Timeout waiting for message");
}

// Clean up
tcpClient.Close();

Console.WriteLine("\n=== Mini Client Complete ===");

// Helper methods

static void WriteInt16BigEndian(Stream stream, short value)
{
    Span<byte> buffer = stackalloc byte[2];
    BinaryPrimitives.WriteInt16BigEndian(buffer, value);
    stream.Write(buffer);
}

static void WriteInt32BigEndian(Stream stream, int value)
{
    Span<byte> buffer = stackalloc byte[4];
    BinaryPrimitives.WriteInt32BigEndian(buffer, value);
    stream.Write(buffer);
}

static void WriteInt64BigEndian(Stream stream, long value)
{
    Span<byte> buffer = stackalloc byte[8];
    BinaryPrimitives.WriteInt64BigEndian(buffer, value);
    stream.Write(buffer);
}

static async Task SendFrameAsync(NetworkStream stream, byte[] body)
{
    // Write 4-byte length prefix + body
    var frame = new byte[4 + body.Length];
    BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(0, 4), body.Length);
    Buffer.BlockCopy(body, 0, frame, 4, body.Length);
    await stream.WriteAsync(frame);
    await stream.FlushAsync();
}

static async Task<byte[]> ReadFrameAsync(NetworkStream stream)
{
    // Read 4-byte length prefix
    var lengthBuffer = new byte[4];
    await stream.ReadExactlyAsync(lengthBuffer);
    var length = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);

    // Read response body
    var body = new byte[length];
    await stream.ReadExactlyAsync(body);
    return body;
}

static uint CalculateCrc32(ReadOnlySpan<byte> data)
{
    // CRC-32 (IEEE 802.3 polynomial)
    const uint polynomial = 0xEDB88320;
    uint crc = 0xFFFFFFFF;

    foreach (var b in data)
    {
        crc ^= b;
        for (int i = 0; i < 8; i++)
        {
            crc = (crc & 1) != 0 ? (crc >> 1) ^ polynomial : crc >> 1;
        }
    }

    return crc ^ 0xFFFFFFFF;
}