# Lesson 19.3: Instructions

Follow these step-by-step instructions to build a minimal Kafka client from scratch.

## Step 1: Create the Protocol Writer Helper

First, create a helper class for writing protocol messages:

```csharp
public class ProtocolWriter
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

    public void WriteInt64(long value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(buffer, value);
        _stream.Write(buffer);
    }

    public void WriteString(string? value)
    {
        if (value == null)
        {
            WriteInt16(-1);
        }
        else
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            WriteInt16((short)bytes.Length);
            _stream.Write(bytes);
        }
    }

    public void WriteBytes(byte[]? value)
    {
        if (value == null)
        {
            WriteInt32(-1);
        }
        else
        {
            WriteInt32(value.Length);
            _stream.Write(value);
        }
    }

    public void WriteRaw(byte[] data) => _stream.Write(data);

    public byte[] ToArray() => _stream.ToArray();
}
```

## Step 2: Create the Protocol Reader Helper

```csharp
public class ProtocolReader
{
    private readonly byte[] _data;
    private int _offset;

    public ProtocolReader(byte[] data) => _data = data;

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
```

## Step 3: Build the Request Header

```csharp
public byte[] BuildRequestHeader(short apiKey, short apiVersion, int correlationId, string clientId)
{
    var writer = new ProtocolWriter();
    writer.WriteInt16(apiKey);
    writer.WriteInt16(apiVersion);
    writer.WriteInt32(correlationId);
    writer.WriteString(clientId);
    return writer.ToArray();
}

public byte[] FrameRequest(byte[] message)
{
    var framed = new byte[4 + message.Length];
    BinaryPrimitives.WriteInt32BigEndian(framed.AsSpan(0, 4), message.Length);
    message.CopyTo(framed.AsSpan(4));
    return framed;
}
```

## Step 4: Build a Metadata Request

```csharp
public byte[] BuildMetadataRequest(int correlationId, string clientId, string[]? topics = null)
{
    var writer = new ProtocolWriter();

    // Header
    writer.WriteRaw(BuildRequestHeader(3, 0, correlationId, clientId));

    // Body: topics array
    if (topics == null)
    {
        // -1 = all topics
        writer.WriteInt32(-1);
    }
    else
    {
        writer.WriteInt32(topics.Length);
        foreach (var topic in topics)
        {
            writer.WriteString(topic);
        }
    }

    return FrameRequest(writer.ToArray());
}
```

## Step 5: Parse Metadata Response

```csharp
public (List<BrokerInfo> brokers, List<TopicInfo> topics) ParseMetadataResponse(byte[] data)
{
    var reader = new ProtocolReader(data);

    // Skip correlation ID (already verified)
    reader.Skip(4);

    // Parse brokers array
    var brokerCount = reader.ReadInt32();
    var brokers = new List<BrokerInfo>();

    for (int i = 0; i < brokerCount; i++)
    {
        var nodeId = reader.ReadInt32();
        var host = reader.ReadString()!;
        var port = reader.ReadInt32();
        brokers.Add(new BrokerInfo(nodeId, host, port));
    }

    // Parse topics array
    var topicCount = reader.ReadInt32();
    var topics = new List<TopicInfo>();

    for (int i = 0; i < topicCount; i++)
    {
        var errorCode = reader.ReadInt16();
        var name = reader.ReadString()!;
        var partitionCount = reader.ReadInt32();
        var partitions = new List<PartitionInfo>();

        for (int j = 0; j < partitionCount; j++)
        {
            var partError = reader.ReadInt16();
            var partId = reader.ReadInt32();
            var leader = reader.ReadInt32();
            // Skip replicas and ISR arrays for simplicity
            var replicaCount = reader.ReadInt32();
            reader.Skip(replicaCount * 4);
            var isrCount = reader.ReadInt32();
            reader.Skip(isrCount * 4);

            partitions.Add(new PartitionInfo(partId, leader, partError));
        }

        topics.Add(new TopicInfo(name, partitions, errorCode));
    }

    return (brokers, topics);
}

public record BrokerInfo(int NodeId, string Host, int Port);
public record TopicInfo(string Name, List<PartitionInfo> Partitions, short ErrorCode);
public record PartitionInfo(int Id, int Leader, short ErrorCode);
```

## Step 6: Build a Produce Request (Simplified)

```csharp
public byte[] BuildProduceRequest(int correlationId, string clientId,
    string topic, int partition, string key, string value)
{
    var writer = new ProtocolWriter();

    // Header
    writer.WriteRaw(BuildRequestHeader(0, 0, correlationId, clientId));

    // Body
    writer.WriteInt16(1);     // Required acks (1 = leader only)
    writer.WriteInt32(5000);  // Timeout (5 seconds)

    // Topics array (1 topic)
    writer.WriteInt32(1);
    writer.WriteString(topic);

    // Partitions array (1 partition)
    writer.WriteInt32(1);
    writer.WriteInt32(partition);

    // Build message set
    var messageSet = BuildMessageSet(key, value);
    writer.WriteBytes(messageSet);

    return FrameRequest(writer.ToArray());
}

private byte[] BuildMessageSet(string key, string value)
{
    var keyBytes = Encoding.UTF8.GetBytes(key);
    var valueBytes = Encoding.UTF8.GetBytes(value);

    var messageWriter = new ProtocolWriter();

    // Message content (for CRC calculation)
    var contentWriter = new ProtocolWriter();
    contentWriter.WriteInt8(0);  // Magic byte (v0)
    contentWriter.WriteInt8(0);  // Attributes (no compression)
    contentWriter.WriteBytes(keyBytes);
    contentWriter.WriteBytes(valueBytes);
    var content = contentWriter.ToArray();

    // Calculate CRC32
    var crc = CalculateCrc32(content);

    // Offset (will be assigned by broker, use -1)
    messageWriter.WriteInt64(-1);

    // Message size
    messageWriter.WriteInt32(4 + content.Length); // CRC + content

    // CRC32
    messageWriter.WriteInt32((int)crc);

    // Message content
    messageWriter.WriteRaw(content);

    return messageWriter.ToArray();
}
```

## Step 7: Build a Fetch Request

```csharp
public byte[] BuildFetchRequest(int correlationId, string clientId,
    string topic, int partition, long offset, int maxBytes = 1048576)
{
    var writer = new ProtocolWriter();

    // Header
    writer.WriteRaw(BuildRequestHeader(1, 0, correlationId, clientId));

    // Body
    writer.WriteInt32(-1);    // Replica ID (-1 for consumers)
    writer.WriteInt32(5000);  // Max wait time (ms)
    writer.WriteInt32(1);     // Min bytes

    // Topics array (1 topic)
    writer.WriteInt32(1);
    writer.WriteString(topic);

    // Partitions array (1 partition)
    writer.WriteInt32(1);
    writer.WriteInt32(partition);  // Partition ID
    writer.WriteInt64(offset);     // Fetch offset
    writer.WriteInt32(maxBytes);   // Max bytes

    return FrameRequest(writer.ToArray());
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Protocol writer and reader helpers
- Metadata request/response handling
- Produce request building
- Fetch request building
- End-to-end message flow

## Sample Output

```
=== Surgewave Bootcamp: Lesson 19.3 - Building a Minimal Client ===

Connected to Surgewave broker on port 12345

--- Metadata Request ---
Sending Metadata request...
Response: 156 bytes

Brokers:
  Broker 0: localhost:12345

Topics: (none yet)

--- Produce Request ---
Sending Produce request for topic 'test-topic'...
Produce response:
  Topic: test-topic
  Partition: 0
  Error: 0 (Success)
  Offset: 0

--- Fetch Request ---
Sending Fetch request for offset 0...
Fetch response:
  Topic: test-topic
  Partition: 0
  Error: 0 (Success)
  Messages:
    Offset 0: key="test-key", value="Hello from minimal client!"

Lesson complete!
```

## Knowledge Check

1. Why do we need to build a message set for Produce requests?
2. What does the replica ID of -1 mean in a Fetch request?
3. How does the broker assign offsets to produced messages?
4. What happens if you send a Fetch request with an invalid offset?
5. Why is CRC32 important in the message format?

---

**Next:** [Lesson 19.4: Protocol Debugging](../lesson-4/README.md)
