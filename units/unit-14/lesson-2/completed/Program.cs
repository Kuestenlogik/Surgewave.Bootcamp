// Surgewave Bootcamp - Unit 19, Lesson 2: Message Framing
// COMPLETED SOLUTION

using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 19.2 - Message Framing ===");
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

// Step 1 & 2 - VarInt encoding and decoding
Console.WriteLine("--- VarInt Encoding ---");
var testValues = new uint[] { 1, 127, 128, 300, 16384 };
foreach (var value in testValues)
{
    var encoded = EncodeUnsignedVarInt(value);
    var decoded = DecodeUnsignedVarInt(encoded, out _);
    Console.WriteLine($"Value {value,5}: [{BitConverter.ToString(encoded).Replace("-", " "),-11}] ({encoded.Length} byte(s)) -> decoded: {decoded}");
}
Console.WriteLine();

// Step 3 - ZigZag encoding for signed integers
Console.WriteLine("--- ZigZag Encoding ---");
var signedTests = new int[] { 0, -1, 1, -2, 2, -127, 127 };
foreach (var value in signedTests)
{
    var zigzag = ZigZagEncode(value);
    var decoded = ZigZagDecode(zigzag);
    Console.WriteLine($"{value,4} -> {zigzag,3} -> {decoded,4}");
}
Console.WriteLine();

// Step 4 - Big-endian byte order demonstration
Console.WriteLine("--- Big-Endian Byte Order ---");
var testInt = 0x12345678; // 305,419,896
Console.WriteLine($"Int32 value {testInt} (0x{testInt:X8}):");

// Big-endian (network byte order - used by Kafka)
var bigEndianBytes = new byte[4];
BinaryPrimitives.WriteInt32BigEndian(bigEndianBytes, testInt);
Console.WriteLine($"  Big-endian bytes: {BitConverter.ToString(bigEndianBytes).Replace("-", " ")}");

// Little-endian (native on x86/x64)
var littleEndianBytes = BitConverter.GetBytes(testInt);
Console.WriteLine($"  Little-endian bytes: {BitConverter.ToString(littleEndianBytes).Replace("-", " ")}");
Console.WriteLine();

// Step 5 - Length-prefixed framing
Console.WriteLine("--- Length-Prefixed Framing ---");
var message = "Hello, Surgewave!";
var messageBytes = Encoding.UTF8.GetBytes(message);
Console.WriteLine($"Message: \"{message}\"");

var framedMessage = new byte[4 + messageBytes.Length];
BinaryPrimitives.WriteInt32BigEndian(framedMessage.AsSpan(0, 4), messageBytes.Length);
messageBytes.CopyTo(framedMessage.AsSpan(4));
Console.WriteLine($"Framed: {BitConverter.ToString(framedMessage).Replace("-", " ")}");
Console.WriteLine($"         ^length={messageBytes.Length,-3} ^--- message content ---^");
Console.WriteLine();

// Step 6 - Compact string encoding
Console.WriteLine("--- Compact String ---");
var compactNull = new List<byte>();
WriteCompactString(compactNull, null);
Console.WriteLine($"Null string: [{BitConverter.ToString(compactNull.ToArray()).Replace("-", " ")}]");

var compactEmpty = new List<byte>();
WriteCompactString(compactEmpty, "");
Console.WriteLine($"Empty string: [{BitConverter.ToString(compactEmpty.ToArray()).Replace("-", " ")}]");

var compactHello = new List<byte>();
WriteCompactString(compactHello, "hello");
Console.WriteLine($"\"hello\": [{BitConverter.ToString(compactHello.ToArray()).Replace("-", " ")}]");
Console.WriteLine($"          ^len=6 (5+1)");
Console.WriteLine();

// Step 7 - Send and receive framed messages
Console.WriteLine("--- Send/Receive Test ---");
using var tcpClient = new TcpClient("localhost", runtime.Port);
using var stream = tcpClient.GetStream();

// Build a Metadata request (API Key 3, Version 0)
var header = BuildRequestHeader(3, 0, 1, "framing-test");

// Add -1 for topics array (all topics) in Metadata v0 body
var body = new byte[4];
BinaryPrimitives.WriteInt32BigEndian(body, -1); // -1 = all topics

var fullRequest = new byte[header.Length + body.Length];
header.CopyTo(fullRequest.AsSpan(0));
body.CopyTo(fullRequest.AsSpan(header.Length));

// Frame the request
var request = new byte[4 + fullRequest.Length];
BinaryPrimitives.WriteInt32BigEndian(request.AsSpan(0, 4), fullRequest.Length);
fullRequest.CopyTo(request.AsSpan(4));

Console.WriteLine("Sending Metadata request to Surgewave broker...");
Console.WriteLine($"Request size: {request.Length} bytes (4 byte length + {fullRequest.Length} byte payload)");
await stream.WriteAsync(request);

// Read response with framing
var response = await ReadFramedMessageAsync(stream);
Console.WriteLine($"Received response: {response.Length} bytes");

// Verify correlation ID
var responseCorrelationId = BinaryPrimitives.ReadInt32BigEndian(response.AsSpan(0, 4));
Console.WriteLine($"Correlation ID matches: {responseCorrelationId == 1}");

Console.WriteLine();
Console.WriteLine("--- Frame Anatomy ---");
Console.WriteLine("Request frame:");
Console.WriteLine($"  [00 00 00 {fullRequest.Length:X2}] - 4-byte length prefix ({fullRequest.Length} bytes)");
Console.WriteLine($"  [00 03]       - API Key 3 (Metadata)");
Console.WriteLine($"  [00 00]       - API Version 0");
Console.WriteLine($"  [00 00 00 01] - Correlation ID 1");
Console.WriteLine($"  [00 0C ...]   - Client ID \"framing-test\" (12 chars)");
Console.WriteLine($"  [FF FF FF FF] - Topics array = -1 (all topics)");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Kafka uses 4-byte big-endian length prefix for framing");
Console.WriteLine("2. VarInt uses 7 bits per byte, MSB is continuation flag");
Console.WriteLine("3. ZigZag encoding maps small negative numbers to small VarInts");
Console.WriteLine("4. Compact strings use VarInt length where 0 = null");
Console.WriteLine("5. Always use ReadExactly to handle partial TCP reads");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// --- Helper Methods ---

static byte[] EncodeUnsignedVarInt(uint value)
{
    var result = new List<byte>();

    while (value > 0x7F)
    {
        // Take lower 7 bits, set continuation bit (0x80)
        result.Add((byte)((value & 0x7F) | 0x80));
        value >>= 7;
    }

    // Last byte (no continuation bit)
    result.Add((byte)value);

    return result.ToArray();
}

static uint DecodeUnsignedVarInt(ReadOnlySpan<byte> data, out int bytesRead)
{
    uint result = 0;
    int shift = 0;
    bytesRead = 0;

    while (true)
    {
        byte b = data[bytesRead++];

        // Add lower 7 bits to result
        result |= (uint)(b & 0x7F) << shift;

        // If MSB is 0, we're done
        if ((b & 0x80) == 0)
            break;

        shift += 7;

        if (shift >= 35)
            throw new OverflowException("VarInt is too long");
    }

    return result;
}

static uint ZigZagEncode(int value)
{
    // Maps: 0->0, -1->1, 1->2, -2->3, 2->4, ...
    return (uint)((value << 1) ^ (value >> 31));
}

static int ZigZagDecode(uint value)
{
    // Maps back: 0->0, 1->-1, 2->1, 3->-2, 4->2, ...
    return (int)(value >> 1) ^ -(int)(value & 1);
}

static void WriteCompactString(List<byte> buffer, string? value)
{
    if (value == null)
    {
        // Null is encoded as 0
        buffer.AddRange(EncodeUnsignedVarInt(0));
    }
    else
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        // Length + 1 (so 0 can represent null)
        buffer.AddRange(EncodeUnsignedVarInt((uint)(bytes.Length + 1)));
        buffer.AddRange(bytes);
    }
}

static byte[] BuildRequestHeader(short apiKey, short apiVersion, int correlationId, string? clientId)
{
    using var ms = new MemoryStream();
    using var writer = new BinaryWriter(ms);

    writer.Write(BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(apiKey) : apiKey);
    writer.Write(BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(apiVersion) : apiVersion);
    writer.Write(BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(correlationId) : correlationId);

    if (clientId == null)
    {
        writer.Write((byte)0xFF);
        writer.Write((byte)0xFF);
    }
    else
    {
        var bytes = Encoding.UTF8.GetBytes(clientId);
        writer.Write(BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness((short)bytes.Length) : (short)bytes.Length);
        writer.Write(bytes);
    }

    return ms.ToArray();
}

static async Task<byte[]> ReadFramedMessageAsync(NetworkStream stream)
{
    var lengthBuffer = new byte[4];
    await stream.ReadExactlyAsync(lengthBuffer);
    var length = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);

    if (length < 0 || length > 100_000_000)
        throw new InvalidOperationException($"Invalid message length: {length}");

    var messageBuffer = new byte[length];
    await stream.ReadExactlyAsync(messageBuffer);
    return messageBuffer;
}
