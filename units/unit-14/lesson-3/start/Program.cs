// Surgewave Bootcamp - Unit 19, Lesson 3: Building a Minimal Client
//
// In this lesson, you'll learn:
// - Establish raw TCP connections to a broker
// - Build and send Metadata requests
// - Build and send Produce requests
// - Build and send Fetch requests
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Buffers.Binary;
using System.IO.Hashing;
using System.Net.Sockets;
using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 19.3 - Building a Minimal Client ===");
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

// TODO: Step 1 - Connect to the broker
// using var tcpClient = new TcpClient("localhost", runtime.Port);
// using var stream = tcpClient.GetStream();
// Console.WriteLine($"Connected to Surgewave broker on port {runtime.Port}");

// TODO: Step 2 - Create protocol writer and reader helpers
// These are provided below as reference implementations

// TODO: Step 3 - Build and send a Metadata request
// Console.WriteLine("\n--- Metadata Request ---");
// var metadataRequest = BuildMetadataRequest(1, "minimal-client", null);
// Console.WriteLine("Sending Metadata request...");
// await stream.WriteAsync(metadataRequest);
//
// var metadataResponse = await ReadFramedMessageAsync(stream);
// Console.WriteLine($"Response: {metadataResponse.Length} bytes");
//
// // Parse the response
// var (brokers, topics) = ParseMetadataResponse(metadataResponse);
// Console.WriteLine("\nBrokers:");
// foreach (var broker in brokers)
// {
//     Console.WriteLine($"  Broker {broker.NodeId}: {broker.Host}:{broker.Port}");
// }
// Console.WriteLine($"\nTopics: {(topics.Count == 0 ? "(none yet)" : string.Join(", ", topics.Select(t => t.Name)))}");

// TODO: Step 4 - Build and send a Produce request
// Console.WriteLine("\n--- Produce Request ---");
// var topic = "test-topic";
// var key = "test-key";
// var value = "Hello from minimal client!";
//
// var produceRequest = BuildProduceRequest(2, "minimal-client", topic, 0, key, value);
// Console.WriteLine($"Sending Produce request for topic '{topic}'...");
// await stream.WriteAsync(produceRequest);
//
// var produceResponse = await ReadFramedMessageAsync(stream);
// ParseProduceResponse(produceResponse);

// TODO: Step 5 - Build and send a Fetch request
// Console.WriteLine("\n--- Fetch Request ---");
// var fetchRequest = BuildFetchRequest(3, "minimal-client", topic, 0, 0);
// Console.WriteLine("Sending Fetch request for offset 0...");
// await stream.WriteAsync(fetchRequest);
//
// var fetchResponse = await ReadFramedMessageAsync(stream);
// ParseFetchResponse(fetchResponse);

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Metadata request discovers brokers and topic partitions");
Console.WriteLine("2. Produce requests include message sets with CRC32 checksums");
Console.WriteLine("3. Fetch requests specify topic, partition, offset, and max bytes");
Console.WriteLine("4. All responses include error codes for each operation");
Console.WriteLine("5. Building clients from scratch teaches protocol deeply");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// --- Helper Methods ---

static async Task<byte[]> ReadFramedMessageAsync(NetworkStream stream)
{
    var lengthBuffer = new byte[4];
    await stream.ReadExactlyAsync(lengthBuffer);
    var length = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);
    var buffer = new byte[length];
    await stream.ReadExactlyAsync(buffer);
    return buffer;
}

static byte[] BuildRequestHeader(short apiKey, short apiVersion, int correlationId, string clientId)
{
    var writer = new ProtocolWriter();
    writer.WriteInt16(apiKey);
    writer.WriteInt16(apiVersion);
    writer.WriteInt32(correlationId);
    writer.WriteString(clientId);
    return writer.ToArray();
}

static byte[] FrameRequest(byte[] message)
{
    var framed = new byte[4 + message.Length];
    BinaryPrimitives.WriteInt32BigEndian(framed.AsSpan(0, 4), message.Length);
    message.CopyTo(framed.AsSpan(4));
    return framed;
}

static byte[] BuildMetadataRequest(int correlationId, string clientId, string[]? topics)
{
    var writer = new ProtocolWriter();
    writer.WriteRaw(BuildRequestHeader(3, 0, correlationId, clientId));

    if (topics == null)
    {
        writer.WriteInt32(-1); // All topics
    }
    else
    {
        writer.WriteInt32(topics.Length);
        foreach (var topic in topics)
            writer.WriteString(topic);
    }

    return FrameRequest(writer.ToArray());
}

static (List<BrokerInfo> brokers, List<TopicInfo> topics) ParseMetadataResponse(byte[] data)
{
    var reader = new ProtocolReader(data);
    reader.Skip(4); // Correlation ID

    var brokerCount = reader.ReadInt32();
    var brokers = new List<BrokerInfo>();
    for (int i = 0; i < brokerCount; i++)
    {
        var nodeId = reader.ReadInt32();
        var host = reader.ReadString()!;
        var port = reader.ReadInt32();
        brokers.Add(new BrokerInfo(nodeId, host, port));
    }

    var topicCount = reader.ReadInt32();
    var topics = new List<TopicInfo>();
    for (int i = 0; i < topicCount; i++)
    {
        var errorCode = reader.ReadInt16();
        var name = reader.ReadString()!;
        var partitionCount = reader.ReadInt32();
        for (int j = 0; j < partitionCount; j++)
        {
            reader.Skip(2 + 4 + 4); // error, partition, leader
            var replicaCount = reader.ReadInt32();
            reader.Skip(replicaCount * 4);
            var isrCount = reader.ReadInt32();
            reader.Skip(isrCount * 4);
        }
        topics.Add(new TopicInfo(name, errorCode));
    }

    return (brokers, topics);
}

static byte[] BuildProduceRequest(int correlationId, string clientId, string topic, int partition, string key, string value)
{
    var writer = new ProtocolWriter();
    writer.WriteRaw(BuildRequestHeader(0, 0, correlationId, clientId));

    writer.WriteInt16(1);     // Required acks
    writer.WriteInt32(5000);  // Timeout

    writer.WriteInt32(1);     // 1 topic
    writer.WriteString(topic);

    writer.WriteInt32(1);     // 1 partition
    writer.WriteInt32(partition);

    var messageSet = BuildMessageSet(key, value);
    writer.WriteInt32(messageSet.Length);
    writer.WriteRaw(messageSet);

    return FrameRequest(writer.ToArray());
}

static byte[] BuildMessageSet(string key, string value)
{
    var keyBytes = Encoding.UTF8.GetBytes(key);
    var valueBytes = Encoding.UTF8.GetBytes(value);

    // Build message content (for CRC)
    var content = new ProtocolWriter();
    content.WriteInt8(0);  // Magic v0
    content.WriteInt8(0);  // Attributes
    content.WriteBytes(keyBytes);
    content.WriteBytes(valueBytes);
    var contentBytes = content.ToArray();

    var crc = Crc32.HashToUInt32(contentBytes);

    var message = new ProtocolWriter();
    message.WriteInt64(0);  // Offset (assigned by broker)
    message.WriteInt32(4 + contentBytes.Length);  // Message size
    message.WriteInt32((int)crc);
    message.WriteRaw(contentBytes);

    return message.ToArray();
}

static void ParseProduceResponse(byte[] data)
{
    var reader = new ProtocolReader(data);
    reader.Skip(4); // Correlation ID

    var topicCount = reader.ReadInt32();
    Console.WriteLine("Produce response:");
    for (int i = 0; i < topicCount; i++)
    {
        var topic = reader.ReadString();
        var partCount = reader.ReadInt32();
        for (int j = 0; j < partCount; j++)
        {
            var partition = reader.ReadInt32();
            var error = reader.ReadInt16();
            var offset = reader.ReadInt64();
            Console.WriteLine($"  Topic: {topic}");
            Console.WriteLine($"  Partition: {partition}");
            Console.WriteLine($"  Error: {error} {(error == 0 ? "(Success)" : "")}");
            Console.WriteLine($"  Offset: {offset}");
        }
    }
}

static byte[] BuildFetchRequest(int correlationId, string clientId, string topic, int partition, long offset)
{
    var writer = new ProtocolWriter();
    writer.WriteRaw(BuildRequestHeader(1, 0, correlationId, clientId));

    writer.WriteInt32(-1);     // Replica ID
    writer.WriteInt32(5000);   // Max wait
    writer.WriteInt32(1);      // Min bytes

    writer.WriteInt32(1);      // 1 topic
    writer.WriteString(topic);

    writer.WriteInt32(1);      // 1 partition
    writer.WriteInt32(partition);
    writer.WriteInt64(offset);
    writer.WriteInt32(1048576); // Max bytes

    return FrameRequest(writer.ToArray());
}

static void ParseFetchResponse(byte[] data)
{
    var reader = new ProtocolReader(data);
    reader.Skip(4); // Correlation ID

    var topicCount = reader.ReadInt32();
    Console.WriteLine("Fetch response:");
    for (int i = 0; i < topicCount; i++)
    {
        var topic = reader.ReadString();
        var partCount = reader.ReadInt32();
        for (int j = 0; j < partCount; j++)
        {
            var partition = reader.ReadInt32();
            var error = reader.ReadInt16();
            var highWatermark = reader.ReadInt64();
            var messageSetSize = reader.ReadInt32();

            Console.WriteLine($"  Topic: {topic}");
            Console.WriteLine($"  Partition: {partition}");
            Console.WriteLine($"  Error: {error} {(error == 0 ? "(Success)" : "")}");
            Console.WriteLine($"  High watermark: {highWatermark}");

            if (messageSetSize > 0)
            {
                Console.WriteLine("  Messages:");
                var endPos = reader.Position + messageSetSize;
                while (reader.Position < endPos && reader.Remaining >= 12)
                {
                    var msgOffset = reader.ReadInt64();
                    var msgSize = reader.ReadInt32();
                    if (msgSize <= 0 || reader.Remaining < msgSize) break;

                    reader.Skip(4); // CRC
                    reader.Skip(1); // Magic
                    reader.Skip(1); // Attributes
                    var key = reader.ReadBytes();
                    var value = reader.ReadBytes();

                    var keyStr = key != null ? Encoding.UTF8.GetString(key) : "null";
                    var valueStr = value != null ? Encoding.UTF8.GetString(value) : "null";
                    Console.WriteLine($"    Offset {msgOffset}: key=\"{keyStr}\", value=\"{valueStr}\"");
                }
            }
        }
    }
}

// Protocol Writer Helper
class ProtocolWriter
{
    private readonly MemoryStream _stream = new();

    public void WriteInt8(byte value) => _stream.WriteByte(value);

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

    public void WriteInt64(long value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(buffer, value);
        _stream.Write(buffer);
    }

    public void WriteString(string? value)
    {
        if (value == null) { WriteInt16(-1); return; }
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteInt16((short)bytes.Length);
        _stream.Write(bytes);
    }

    public void WriteBytes(byte[]? value)
    {
        if (value == null) { WriteInt32(-1); return; }
        WriteInt32(value.Length);
        _stream.Write(value);
    }

    public void WriteRaw(byte[] data) => _stream.Write(data);
    public byte[] ToArray() => _stream.ToArray();
}

// Protocol Reader Helper
class ProtocolReader
{
    private readonly byte[] _data;
    private int _offset;

    public ProtocolReader(byte[] data) => _data = data;
    public int Position => _offset;
    public int Remaining => _data.Length - _offset;

    public short ReadInt16()
    {
        var value = BinaryPrimitives.ReadInt16BigEndian(_data.AsSpan(_offset, 2));
        _offset += 2;
        return value;
    }

    public int ReadInt32()
    {
        var value = BinaryPrimitives.ReadInt32BigEndian(_data.AsSpan(_offset, 4));
        _offset += 4;
        return value;
    }

    public long ReadInt64()
    {
        var value = BinaryPrimitives.ReadInt64BigEndian(_data.AsSpan(_offset, 8));
        _offset += 8;
        return value;
    }

    public string? ReadString()
    {
        var length = ReadInt16();
        if (length < 0) return null;
        var str = Encoding.UTF8.GetString(_data, _offset, length);
        _offset += length;
        return str;
    }

    public byte[]? ReadBytes()
    {
        var length = ReadInt32();
        if (length < 0) return null;
        var bytes = _data.AsSpan(_offset, length).ToArray();
        _offset += length;
        return bytes;
    }

    public void Skip(int count) => _offset += count;
}

record BrokerInfo(int NodeId, string Host, int Port);
record TopicInfo(string Name, short ErrorCode);
