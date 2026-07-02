// Unit 17 Project: File Processor - Large File Chunking - COMPLETED
// A file transfer system with chunking and reassembly

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Abstractions;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== File Processor - Chunking System ===\n");

// Configuration
const int ChunkSize = 64 * 1024; // 64 KB
const string TopicName = "file-chunks";

Console.WriteLine($"[Config] Chunk size: {ChunkSize / 1024} KB");
Console.WriteLine($"[Config] Topic: {TopicName}");

// Assembler state
var pendingFiles = new ConcurrentDictionary<string, List<ChunkMessage>>();
var completedFiles = new ConcurrentDictionary<string, (string FileName, byte[] Data, bool Valid)>();
var expectedNextChunk = new ConcurrentDictionary<string, int>();

// Statistics
var totalChunksSent = 0;
var totalChunksReceived = 0;
var totalBytes = 0L;
var outOfOrderChunks = 0;
var checksumFailures = 0;

// Start embedded broker
Console.WriteLine("\n[Surgewave] Starting embedded broker...");

await using var surgewave = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .Build()
    .StartAsync();

await using var client = await SurgewaveClient.Create(surgewave.BootstrapServers).BuildAsync();
var nativeClient = client.NativeClient!;

// Create topic
await nativeClient.Topics.CreateAsync(TopicName, partitions: 3);
Console.WriteLine($"[Surgewave] Created topic: {TopicName}");

// Step 1: Implement file chunking
List<ChunkMessage> ChunkFile(string fileName, byte[] fileData)
{
    var chunks = new List<ChunkMessage>();

    // Generate correlation ID
    var correlationId = Guid.NewGuid().ToString("N")[..12];

    // Calculate file checksum
    using var sha256 = SHA256.Create();
    var hashBytes = sha256.ComputeHash(fileData);
    var checksum = Convert.ToHexString(hashBytes)[..16];

    // Calculate total chunks
    var totalChunks = (int)Math.Ceiling(fileData.Length / (double)ChunkSize);

    Console.WriteLine($"[Chunker] Splitting into {totalChunks} chunks...");

    // Split file into chunks
    for (int i = 0; i < totalChunks; i++)
    {
        var offset = i * ChunkSize;
        var size = Math.Min(ChunkSize, fileData.Length - offset);
        var chunkData = new byte[size];
        Array.Copy(fileData, offset, chunkData, 0, size);

        var header = new ChunkHeader(correlationId, fileName, i, totalChunks, fileData.Length, checksum);
        chunks.Add(new ChunkMessage(header, chunkData));
    }

    return chunks;
}

// Step 2: Produce chunks for files
Console.WriteLine("\n[File 1] Processing: report.pdf (simulated 2.5 MB)");

var reportData = new byte[2_500_000];
new Random(42).NextBytes(reportData);

var reportChunks = ChunkFile("report.pdf", reportData);
var reportCorrelation = reportChunks[0].Header.CorrelationId;

foreach (var chunk in reportChunks)
{
    var json = JsonSerializer.Serialize(chunk);
    await nativeClient.Messaging.SendAsync(TopicName, partition: 0, key: chunk.Header.CorrelationId, value: json);

    if (chunk.Header.ChunkIndex < 3 || chunk.Header.ChunkIndex >= chunk.Header.TotalChunks - 1)
    {
        Console.WriteLine($"[Producer] Chunk {chunk.Header.ChunkIndex}/{chunk.Header.TotalChunks} -> {TopicName} (correlation: {chunk.Header.CorrelationId})");
    }
    else if (chunk.Header.ChunkIndex == 3)
    {
        Console.WriteLine("[Producer] ...");
    }

    totalChunksSent++;
}

Console.WriteLine($"[Producer] File report.pdf: {reportChunks.Count} chunks sent (checksum: {reportChunks[0].Header.Checksum})");

Console.WriteLine("\n[File 2] Processing: image.png (simulated 512 KB)");

var imageData = new byte[512_000];
new Random(123).NextBytes(imageData);

var imageChunks = ChunkFile("image.png", imageData);
var imageCorrelation = imageChunks[0].Header.CorrelationId;

foreach (var chunk in imageChunks)
{
    var json = JsonSerializer.Serialize(chunk);
    await nativeClient.Messaging.SendAsync(TopicName, partition: 0, key: chunk.Header.CorrelationId, value: json);

    Console.WriteLine($"[Producer] Chunk {chunk.Header.ChunkIndex}/{chunk.Header.TotalChunks} -> {TopicName} (correlation: {chunk.Header.CorrelationId})");
    totalChunksSent++;
}

Console.WriteLine($"[Producer] File image.png: {imageChunks.Count} chunks sent (checksum: {imageChunks[0].Header.Checksum})");

// Step 3: Implement chunk assembly
void ProcessChunk(ChunkMessage chunk)
{
    var correlationId = chunk.Header.CorrelationId;

    // Get or create pending list
    var chunkList = pendingFiles.GetOrAdd(correlationId, _ => new List<ChunkMessage>());

    // Track expected next chunk for out-of-order detection
    var expected = expectedNextChunk.GetOrAdd(correlationId, 0);

    // Detect out-of-order arrival
    if (chunk.Header.ChunkIndex != expected)
    {
        Interlocked.Increment(ref outOfOrderChunks);
        Console.WriteLine($"[Assembler] Received chunk {chunk.Header.ChunkIndex}/{chunk.Header.TotalChunks} (correlation: {correlationId}) [out of order]");
    }
    else
    {
        Console.WriteLine($"[Assembler] Received chunk {chunk.Header.ChunkIndex}/{chunk.Header.TotalChunks} (correlation: {correlationId})");
    }

    // Update expected next chunk (track highest seen + 1)
    expectedNextChunk.AddOrUpdate(correlationId, chunk.Header.ChunkIndex + 1,
        (_, current) => Math.Max(current, chunk.Header.ChunkIndex + 1));

    lock (chunkList)
    {
        chunkList.Add(chunk);

        // Check if all chunks received
        if (chunkList.Count == chunk.Header.TotalChunks)
        {
            Console.WriteLine($"[Assembler] File {correlationId}: {chunkList.Count}/{chunk.Header.TotalChunks} chunks received");
            AssembleFile(correlationId, chunkList);
        }
    }
}

void AssembleFile(string correlationId, List<ChunkMessage> chunks)
{
    // Sort chunks by index
    chunks.Sort((a, b) => a.Header.ChunkIndex.CompareTo(b.Header.ChunkIndex));

    var header = chunks[0].Header;

    // Concatenate chunk data
    var fileData = new byte[header.TotalSize];
    var offset = 0;
    foreach (var chunk in chunks)
    {
        Array.Copy(chunk.Data, 0, fileData, offset, chunk.Data.Length);
        offset += chunk.Data.Length;
    }

    // Verify checksum
    Console.WriteLine("[Assembler] Verifying checksum...");
    using var sha256 = SHA256.Create();
    var computedHash = Convert.ToHexString(sha256.ComputeHash(fileData))[..16];
    var isValid = computedHash == header.Checksum;

    if (isValid)
    {
        Console.WriteLine($"[Assembler] File {header.FileName} reassembled successfully (checksum: VALID)");
    }
    else
    {
        Console.WriteLine($"[Assembler] File {header.FileName} checksum FAILED! Expected: {header.Checksum}, Got: {computedHash}");
        Interlocked.Increment(ref checksumFailures);
    }

    completedFiles.TryAdd(correlationId, (header.FileName, fileData, isValid));
    pendingFiles.TryRemove(correlationId, out _);
}

// Step 4: Consume and process chunks
Console.WriteLine("\n[Consumer] Starting chunk assembly...");

await using var consumer = client.CreateConsumer<string, string>();
consumer.Assign(TopicName, partition: 0, offset: 0);

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100), cts.Token);
        if (result != null)
        {
            var chunk = JsonSerializer.Deserialize<ChunkMessage>(result.Value!);
            if (chunk != null)
            {
                ProcessChunk(chunk);
                totalChunksReceived++;
                totalBytes += chunk.Data.Length;
            }

            // Stop if all files completed
            if (completedFiles.Count >= 2)
            {
                break;
            }
        }
        else
        {
            break;
        }
    }
}
catch (OperationCanceledException)
{
    // Expected timeout
}

// Step 5: Print transfer summary
Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("[Stats] Transfer Summary:");
Console.WriteLine($"  - Files processed: {completedFiles.Count}");
Console.WriteLine($"  - Total chunks sent: {totalChunksSent}");
Console.WriteLine($"  - Total chunks received: {totalChunksReceived}");
Console.WriteLine($"  - Total bytes: {totalBytes:N0}");
Console.WriteLine($"  - Out-of-order chunks: {outOfOrderChunks}");
Console.WriteLine($"  - Checksum failures: {checksumFailures}");

Console.WriteLine("\n[Files] Completed Files:");
foreach (var (corrId, (fileName, data, valid)) in completedFiles)
{
    var status = valid ? "VALID" : "INVALID";
    Console.WriteLine($"  - {fileName}: {data.Length:N0} bytes [{status}]");
}

Console.WriteLine(new string('=', 50));
Console.WriteLine("\n=== File Processor Complete ===");

// Chunk message format
public record ChunkHeader(
    string CorrelationId,
    string FileName,
    int ChunkIndex,
    int TotalChunks,
    long TotalSize,
    string Checksum
);

public record ChunkMessage(
    ChunkHeader Header,
    byte[] Data
);