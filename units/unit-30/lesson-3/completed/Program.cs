// Surgewave Bootcamp - Unit 17, Lesson 3: Chunking
// COMPLETED SOLUTION

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("Chunking Demo");
Console.WriteLine("=============");
Console.WriteLine();

// Start broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// Create a large message (5 MB)
var originalSize = 5 * 1024 * 1024;
var originalData = new byte[originalSize];
Random.Shared.NextBytes(originalData);

var chunkSize = 1 * 1024 * 1024; // 1 MB chunks
Console.WriteLine($"Original message size: {originalSize:N0} bytes (5 MB)");
Console.WriteLine($"Chunk size: {chunkSize:N0} bytes (1 MB)");
Console.WriteLine();

Console.WriteLine("Chunking message...");
var chunker = new MessageChunker(chunkSize);
var chunks = chunker.Chunk(originalData).ToList();

Console.WriteLine($"  Created {chunks.Count} chunks:");
foreach (var chunk in chunks)
{
    Console.WriteLine($"    Chunk {chunk.Header.ChunkIndex}: {chunk.Header.ChunkSize:N0} bytes");
}
Console.WriteLine();

Console.WriteLine("Sending chunks to Surgewave...");

await using var producer = new SurgewaveProducer<string, byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

const string topic = "chunked-messages";

foreach (var chunk in chunks)
{
    var key = chunk.Header.MessageId.ToString();
    var serialized = chunk.Serialize();
    await producer.ProduceAsync(topic, key, serialized);
    Console.WriteLine($"  Sent chunk {chunk.Header.ChunkIndex}/{chunk.Header.TotalChunks}");
}
Console.WriteLine();

Console.WriteLine("Consuming and reassembling...");

await using var consumer = new SurgewaveConsumer<string, byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "chunking-demo-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe(topic);

var reassembler = new MessageReassembler();
byte[]? reassembledData = null;
var timeout = DateTime.UtcNow.AddSeconds(10);

while (reassembledData == null && DateTime.UtcNow < timeout)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
    if (result != null)
    {
        var chunk = ChunkMessage.Deserialize(result.Value);
        Console.WriteLine($"  Received chunk {chunk.Header.ChunkIndex}/{chunk.Header.TotalChunks} for message {chunk.Header.MessageId.ToString()[..8]}");

        var (isComplete, data) = reassembler.AddChunk(chunk);
        if (isComplete)
        {
            Console.WriteLine($"  Message {chunk.Header.MessageId.ToString()[..8]} complete! Reassembling...");
            reassembledData = data;
        }
    }
}
Console.WriteLine();

if (reassembledData != null)
{
    Console.WriteLine($"Reassembled message size: {reassembledData.Length:N0} bytes");
    Console.WriteLine($"Content matches original: {reassembledData.SequenceEqual(originalData)}");
}
else
{
    Console.WriteLine("Failed to reassemble message within timeout");
}

Console.WriteLine();
Console.WriteLine("Demo completed!");

// Types and helpers

public record ChunkHeader
{
    public Guid MessageId { get; init; }
    public int ChunkIndex { get; init; }
    public int TotalChunks { get; init; }
    public long TotalSize { get; init; }
    public int ChunkSize { get; init; }
    public string Checksum { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public record ChunkMessage
{
    public ChunkHeader Header { get; init; } = null!;
    public byte[] Data { get; init; } = Array.Empty<byte>();

    public byte[] Serialize()
    {
        var headerJson = JsonSerializer.Serialize(Header);
        var headerBytes = Encoding.UTF8.GetBytes(headerJson);

        var result = new byte[4 + headerBytes.Length + Data.Length];
        BitConverter.GetBytes(headerBytes.Length).CopyTo(result, 0);
        headerBytes.CopyTo(result, 4);
        Data.CopyTo(result, 4 + headerBytes.Length);

        return result;
    }

    public static ChunkMessage Deserialize(byte[] bytes)
    {
        var headerLength = BitConverter.ToInt32(bytes, 0);
        var headerJson = Encoding.UTF8.GetString(bytes, 4, headerLength);
        var header = JsonSerializer.Deserialize<ChunkHeader>(headerJson)!;

        var dataLength = bytes.Length - 4 - headerLength;
        var data = new byte[dataLength];
        Array.Copy(bytes, 4 + headerLength, data, 0, dataLength);

        return new ChunkMessage { Header = header, Data = data };
    }
}

public class MessageChunker
{
    private readonly int _chunkSize;

    public MessageChunker(int chunkSize = 1024 * 1024)
    {
        _chunkSize = chunkSize;
    }

    public IEnumerable<ChunkMessage> Chunk(byte[] data)
    {
        var messageId = Guid.NewGuid();
        var totalChunks = (int)Math.Ceiling((double)data.Length / _chunkSize);
        var checksum = ComputeChecksum(data);
        var timestamp = DateTime.UtcNow;

        for (int i = 0; i < totalChunks; i++)
        {
            var offset = i * _chunkSize;
            var length = Math.Min(_chunkSize, data.Length - offset);
            var chunkData = new byte[length];
            Array.Copy(data, offset, chunkData, 0, length);

            yield return new ChunkMessage
            {
                Header = new ChunkHeader
                {
                    MessageId = messageId,
                    ChunkIndex = i,
                    TotalChunks = totalChunks,
                    TotalSize = data.Length,
                    ChunkSize = length,
                    Checksum = checksum,
                    Timestamp = timestamp
                },
                Data = chunkData
            };
        }
    }

    private static string ComputeChecksum(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }
}

public class MessageReassembler
{
    private readonly Dictionary<Guid, List<ChunkMessage>> _pendingMessages = new();
    private readonly Dictionary<Guid, int> _expectedChunks = new();

    public (bool IsComplete, byte[]? Data) AddChunk(ChunkMessage chunk)
    {
        var messageId = chunk.Header.MessageId;

        if (!_pendingMessages.ContainsKey(messageId))
        {
            _pendingMessages[messageId] = new List<ChunkMessage>();
            _expectedChunks[messageId] = chunk.Header.TotalChunks;
        }

        if (!_pendingMessages[messageId].Any(c => c.Header.ChunkIndex == chunk.Header.ChunkIndex))
        {
            _pendingMessages[messageId].Add(chunk);
        }

        if (_pendingMessages[messageId].Count == _expectedChunks[messageId])
        {
            var data = Reassemble(messageId);
            Cleanup(messageId);
            return (true, data);
        }

        return (false, null);
    }

    private byte[] Reassemble(Guid messageId)
    {
        var chunks = _pendingMessages[messageId]
            .OrderBy(c => c.Header.ChunkIndex)
            .ToList();

        var totalSize = chunks.First().Header.TotalSize;
        var result = new byte[totalSize];
        var offset = 0;

        foreach (var chunk in chunks)
        {
            chunk.Data.CopyTo(result, offset);
            offset += chunk.Data.Length;
        }

        var expectedChecksum = chunks.First().Header.Checksum;
        var actualChecksum = ComputeChecksum(result);

        if (expectedChecksum != actualChecksum)
        {
            throw new InvalidDataException("Checksum mismatch during reassembly");
        }

        return result;
    }

    private void Cleanup(Guid messageId)
    {
        _pendingMessages.Remove(messageId);
        _expectedChunks.Remove(messageId);
    }

    private static string ComputeChecksum(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }
}
