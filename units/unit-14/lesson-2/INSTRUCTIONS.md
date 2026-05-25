# Lesson 19.2: Instructions

Follow these step-by-step instructions to understand message format in the Kafka protocol.

## Step 1: Understand VarInt Encoding

VarInt uses 7 bits per byte, with the MSB as continuation flag:

```csharp
public static byte[] EncodeVarInt(int value)
{
    // ZigZag encode to handle negative numbers efficiently
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

public static int DecodeVarInt(ReadOnlySpan<byte> data, out int bytesRead)
{
    uint value = 0;
    int shift = 0;
    bytesRead = 0;

    while (true)
    {
        byte b = data[bytesRead++];
        value |= (uint)(b & 0x7F) << shift;
        if ((b & 0x80) == 0) break;
        shift += 7;
    }

    // ZigZag decode
    return (int)(value >> 1) ^ -(int)(value & 1);
}
```

## Step 2: Build a Record Structure

Individual records within a batch use VarInt for efficiency:

```csharp
public static byte[] BuildRecord(
    int timestampDelta,
    int offsetDelta,
    byte[]? key,
    byte[] value,
    Dictionary<string, byte[]>? headers = null)
{
    var record = new List<byte>();

    // Attributes (1 byte, currently unused)
    record.Add(0x00);

    // Timestamp delta (VarInt)
    record.AddRange(EncodeVarInt(timestampDelta));

    // Offset delta (VarInt)
    record.AddRange(EncodeVarInt(offsetDelta));

    // Key (VarInt length + bytes, -1 = null)
    if (key == null)
    {
        record.AddRange(EncodeVarInt(-1));
    }
    else
    {
        record.AddRange(EncodeVarInt(key.Length));
        record.AddRange(key);
    }

    // Value (VarInt length + bytes)
    record.AddRange(EncodeVarInt(value.Length));
    record.AddRange(value);

    // Headers
    var headerList = headers?.ToList() ?? new List<KeyValuePair<string, byte[]>>();
    record.AddRange(EncodeVarInt(headerList.Count));

    foreach (var header in headerList)
    {
        var keyBytes = Encoding.UTF8.GetBytes(header.Key);
        record.AddRange(EncodeVarInt(keyBytes.Length));
        record.AddRange(keyBytes);
        record.AddRange(EncodeVarInt(header.Value.Length));
        record.AddRange(header.Value);
    }

    // Prepend record length
    var lengthPrefix = EncodeVarInt(record.Count);
    return lengthPrefix.Concat(record).ToArray();
}
```

## Step 3: Build a Record Batch

The record batch wraps multiple records with metadata and CRC:

```csharp
public static byte[] BuildRecordBatch(
    long baseOffset,
    long firstTimestamp,
    byte[][] records,
    CompressionType compression = CompressionType.None)
{
    using var ms = new MemoryStream();
    using var writer = new BinaryWriter(ms);

    // Base offset (8 bytes)
    WriteBigEndian(writer, baseOffset);

    // Batch length placeholder (4 bytes) - filled in later
    var batchLengthPosition = ms.Position;
    writer.Write(0);

    // Partition leader epoch (4 bytes)
    WriteBigEndian(writer, -1);

    // Magic (1 byte) - 2 = record batch format
    writer.Write((byte)2);

    // CRC placeholder (4 bytes) - filled in later
    var crcPosition = ms.Position;
    writer.Write(0);

    var crcStartPosition = ms.Position;

    // Attributes (2 bytes)
    short attributes = (short)compression;
    WriteBigEndian(writer, attributes);

    // Last offset delta (4 bytes)
    WriteBigEndian(writer, records.Length - 1);

    // First timestamp (8 bytes)
    WriteBigEndian(writer, firstTimestamp);

    // Max timestamp (8 bytes)
    WriteBigEndian(writer, firstTimestamp);

    // Producer ID (8 bytes) - -1 for non-transactional
    WriteBigEndian(writer, -1L);

    // Producer epoch (2 bytes)
    WriteBigEndian(writer, (short)-1);

    // Base sequence (4 bytes) - -1 for non-transactional
    WriteBigEndian(writer, -1);

    // Records array length (4 bytes)
    WriteBigEndian(writer, records.Length);

    // Write each record
    foreach (var record in records)
    {
        writer.Write(record);
    }

    // Calculate and write CRC32C
    var batchData = ms.ToArray();
    var crcData = batchData.AsSpan((int)crcStartPosition);
    var crc = CalculateCrc32C(crcData);

    // Write batch length
    var batchLength = batchData.Length - 12; // Exclude baseOffset and batchLength
    BinaryPrimitives.WriteInt32BigEndian(
        batchData.AsSpan((int)batchLengthPosition, 4),
        batchLength);

    // Write CRC
    BinaryPrimitives.WriteInt32BigEndian(
        batchData.AsSpan((int)crcPosition, 4),
        (int)crc);

    return batchData;
}
```

## Step 4: Add Message Headers

Headers provide metadata for each message:

```csharp
// Adding headers to a message
var headers = new Dictionary<string, byte[]>
{
    ["trace-id"] = Encoding.UTF8.GetBytes("abc123"),
    ["content-type"] = Encoding.UTF8.GetBytes("application/json"),
    ["source"] = Encoding.UTF8.GetBytes("order-service")
};

var record = BuildRecord(
    timestampDelta: 0,
    offsetDelta: 0,
    key: Encoding.UTF8.GetBytes("order-1"),
    value: Encoding.UTF8.GetBytes("{\"id\": 1, \"item\": \"widget\"}"),
    headers: headers
);
```

## Step 5: Implement CRC32C Calculation

Kafka uses CRC32C (Castagnoli) for data integrity:

```csharp
public static uint CalculateCrc32C(ReadOnlySpan<byte> data)
{
    // Using System.IO.Hashing (available in .NET 6+)
    return System.IO.Hashing.Crc32.HashToUInt32(data);
}

// Alternative: Manual implementation
public static uint CalculateCrc32CManual(ReadOnlySpan<byte> data)
{
    const uint Polynomial = 0x82F63B78;
    uint crc = 0xFFFFFFFF;

    foreach (byte b in data)
    {
        crc ^= b;
        for (int i = 0; i < 8; i++)
        {
            crc = (crc >> 1) ^ ((crc & 1) * Polynomial);
        }
    }

    return crc ^ 0xFFFFFFFF;
}

// Verify CRC in a received batch
public static bool VerifyBatchCrc(ReadOnlySpan<byte> batch)
{
    // Skip: baseOffset(8) + batchLength(4) + partitionLeaderEpoch(4) + magic(1)
    var storedCrc = BinaryPrimitives.ReadUInt32BigEndian(batch.Slice(17, 4));

    // CRC covers from attributes (offset 21) to end
    var crcData = batch.Slice(21);
    var calculatedCrc = CalculateCrc32C(crcData);

    return storedCrc == calculatedCrc;
}
```

## Step 6: Implement Compression

Apply compression to the records portion of the batch:

```csharp
public enum CompressionType : short
{
    None = 0,
    Gzip = 1,
    Snappy = 2,
    Lz4 = 3,
    Zstd = 4
}

public static byte[] Compress(byte[] data, CompressionType type)
{
    switch (type)
    {
        case CompressionType.None:
            return data;

        case CompressionType.Gzip:
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
                {
                    gzip.Write(data);
                }
                return output.ToArray();
            }

        // LZ4 and ZSTD require external packages
        default:
            throw new NotSupportedException($"Compression {type} not implemented");
    }
}

public static byte[] Decompress(byte[] data, CompressionType type)
{
    switch (type)
    {
        case CompressionType.None:
            return data;

        case CompressionType.Gzip:
            using (var input = new MemoryStream(data))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }

        default:
            throw new NotSupportedException($"Compression {type} not implemented");
    }
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Record batch construction
- VarInt encoding for efficient storage
- Message headers
- CRC32C validation
- GZIP compression

## Sample Output

```
=== Surgewave Bootcamp: Lesson 19.2 - Message Format ===

--- VarInt Encoding ---
Value 0: [00] (1 byte)
Value 1: [02] (1 byte)  // ZigZag: 1 -> 2
Value -1: [01] (1 byte) // ZigZag: -1 -> 1
Value 300: [D8 04] (2 bytes)

--- Building a Record ---
Record with key="order-1", value="Hello, Kafka!"
  Attributes: 00
  Timestamp delta: 0
  Offset delta: 0
  Key length: 7
  Value length: 13
  Headers: 2
Total record size: 48 bytes

--- Message Headers ---
Header: trace-id = abc123
Header: content-type = application/json

--- CRC32C Validation ---
Original CRC: 0xABCD1234
Calculated CRC: 0xABCD1234
CRC Valid: True

--- Compression ---
Original size: 1024 bytes
GZIP compressed: 256 bytes (75% reduction)

Lesson complete!
```

## Knowledge Check

1. Why does Kafka use VarInt encoding in record batches?
2. What is the purpose of ZigZag encoding for VarInt?
3. Which bytes are included in the CRC32C calculation?
4. When would you choose LZ4 over GZIP compression?
5. How do message headers help in distributed systems?

---

**Next:** [Lesson 19.3: Building a Custom Client](../lesson-3/README.md)
