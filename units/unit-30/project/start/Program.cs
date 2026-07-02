// Unit 17 Project: File Processor - Large File Chunking
// Build a file transfer system with chunking and reassembly

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

// Assembler state for tracking incomplete files
var pendingFiles = new ConcurrentDictionary<string, List<ChunkMessage>>();
var completedFiles = new ConcurrentDictionary<string, byte[]>();

// Statistics
var totalChunks = 0;
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

// TODO: Step 1 - Implement file chunking
Console.WriteLine();

// TODO: Implement ChunkFile method that:
// - Reads file bytes
// - Calculates SHA256 checksum of entire file
// - Splits into chunks of ChunkSize
// - Returns list of ChunkMessage with proper headers

List<ChunkMessage> ChunkFile(string fileName, byte[] fileData)
{
    var chunks = new List<ChunkMessage>();

    // TODO: Generate correlation ID
    // var correlationId = Guid.NewGuid().ToString("N")[..12];

    // TODO: Calculate file checksum
    // using var sha256 = SHA256.Create();
    // var hashBytes = sha256.ComputeHash(fileData);
    // var checksum = Convert.ToHexString(hashBytes)[..16];

    // TODO: Calculate total chunks
    // var totalChunks = (int)Math.Ceiling(fileData.Length / (double)ChunkSize);

    // TODO: Split file into chunks
    // for (int i = 0; i < totalChunks; i++)
    // {
    //     var offset = i * ChunkSize;
    //     var size = Math.Min(ChunkSize, fileData.Length - offset);
    //     var chunkData = new byte[size];
    //     Array.Copy(fileData, offset, chunkData, 0, size);
    //
    //     var header = new ChunkHeader(correlationId, fileName, i, totalChunks, fileData.Length, checksum);
    //     chunks.Add(new ChunkMessage(header, chunkData));
    // }

    return chunks;
}

// TODO: Step 2 - Produce chunks for sample files
Console.WriteLine("[File 1] Processing: report.pdf (simulated 2.5 MB)");

// TODO: Create simulated file data
// var reportData = new byte[2_500_000];
// new Random(42).NextBytes(reportData);

// TODO: Chunk the file
// var reportChunks = ChunkFile("report.pdf", reportData);

// TODO: Produce each chunk
// foreach (var chunk in reportChunks)
// {
//     var json = JsonSerializer.Serialize(chunk);
//     await nativeClient.Messaging.SendAsync(TopicName, partition: 0, key: chunk.Header.CorrelationId, value: json);
//     Console.WriteLine($"[Producer] Chunk {chunk.Header.ChunkIndex}/{chunk.Header.TotalChunks} -> {TopicName}");
// }

Console.WriteLine("[File 2] Processing: image.png (simulated 512 KB)");

// TODO: Process second file similarly

// TODO: Step 3 - Implement chunk assembly
Console.WriteLine("\n[Consumer] Starting chunk assembly...");

// TODO: Implement ProcessChunk method that:
// - Groups chunks by correlation ID
// - Tracks received chunk indices
// - Detects out-of-order arrival
// - Triggers assembly when all chunks received

void ProcessChunk(ChunkMessage chunk)
{
    // TODO: Get or create pending list for this correlation ID
    // var chunkList = pendingFiles.GetOrAdd(chunk.Header.CorrelationId, _ => new List<ChunkMessage>());

    // TODO: Detect out-of-order (if list not empty and this isn't next expected index)
    // if (chunkList.Count > 0 && chunk.Header.ChunkIndex != chunkList.Count)
    // {
    //     outOfOrderChunks++;
    //     Console.WriteLine($"[Assembler] Received chunk {chunk.Header.ChunkIndex}/{chunk.Header.TotalChunks} [out of order]");
    // }

    // TODO: Add chunk to list
    // chunkList.Add(chunk);

    // TODO: Check if all chunks received
    // if (chunkList.Count == chunk.Header.TotalChunks)
    // {
    //     AssembleFile(chunk.Header.CorrelationId, chunkList);
    // }
}

// TODO: Implement AssembleFile method that:
// - Sorts chunks by index
// - Concatenates chunk data
// - Verifies checksum

void AssembleFile(string correlationId, List<ChunkMessage> chunks)
{
    // TODO: Sort chunks by index
    // chunks.Sort((a, b) => a.Header.ChunkIndex.CompareTo(b.Header.ChunkIndex));

    // TODO: Get file metadata from first chunk
    // var header = chunks[0].Header;

    // TODO: Concatenate chunk data
    // var fileData = new byte[header.TotalSize];
    // var offset = 0;
    // foreach (var chunk in chunks)
    // {
    //     Array.Copy(chunk.Data, 0, fileData, offset, chunk.Data.Length);
    //     offset += chunk.Data.Length;
    // }

    // TODO: Verify checksum
    // using var sha256 = SHA256.Create();
    // var computedHash = Convert.ToHexString(sha256.ComputeHash(fileData))[..16];
    // var isValid = computedHash == header.Checksum;

    // TODO: Store completed file and log result
}

// TODO: Step 4 - Consume and process chunks
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

// TODO: Consume from topic and call ProcessChunk for each message
// await using var consumer = client.CreateConsumer<string, string>();
// consumer.Assign(TopicName, partition: 0, offset: 0);
// try
// {
//     while (!cts.Token.IsCancellationRequested)
//     {
//         var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100), cts.Token);
//         if (result != null)
//         {
//             var chunk = JsonSerializer.Deserialize<ChunkMessage>(result.Value!);
//             if (chunk != null)
//             {
//                 ProcessChunk(chunk);
//                 totalChunks++;
//                 totalBytes += chunk.Data.Length;
//             }
//         }
//         else { break; }
//     }
// }
// catch (OperationCanceledException) { }

// TODO: Step 5 - Print transfer summary
Console.WriteLine("\n[Stats] Transfer Summary:");
Console.WriteLine($"  - Files processed: {completedFiles.Count}");
Console.WriteLine($"  - Total chunks: {totalChunks}");
Console.WriteLine($"  - Total bytes: {totalBytes:N0}");
Console.WriteLine($"  - Out-of-order chunks: {outOfOrderChunks}");
Console.WriteLine($"  - Checksum failures: {checksumFailures}");

Console.WriteLine("\n=== File Processor Complete ===");