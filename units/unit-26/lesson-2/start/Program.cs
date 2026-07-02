// Surgewave Bootcamp - Unit 24, Lesson 2: Local File System Tier
//
// In this lesson, you'll learn:
// - How to configure local file system as cold tier storage
// - Segment file management and compression
// - Reading and writing segments
// - Retention-based cleanup
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.IO.Compression;
using System.Text;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 24.2 - Local File System Tier ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var topic = "cold-tier-demo";
var coldTierPath = Path.Combine(Path.GetTempPath(), "surgewave-cold-tier-demo", Guid.NewGuid().ToString());

// TODO: Step 1 - Define segment configuration
// public record SegmentConfig(
//     string BasePath,
//     long MaxSegmentBytes,
//     TimeSpan MaxSegmentAge,
//     bool EnableCompression
// );
//
// var config = new SegmentConfig(
//     BasePath: coldTierPath,
//     MaxSegmentBytes: 10 * 1024,  // 10 KB for demo
//     MaxSegmentAge: TimeSpan.FromMinutes(5),
//     EnableCompression: true
// );
//
// Console.WriteLine("\n--- Cold Tier Configuration ---");
// Console.WriteLine($"Base path: {config.BasePath}");
// Console.WriteLine($"Max segment size: {config.MaxSegmentBytes:N0} bytes");
// Console.WriteLine($"Compression: {(config.EnableCompression ? "Enabled" : "Disabled")}");

// TODO: Step 2 - Create a simple segment writer class
// See INSTRUCTIONS.md for the SegmentWriter implementation
// Key methods:
// - WriteAsync(key, value) - writes a message to current segment
// - RollSegmentAsync() - creates a new segment file

// TODO: Step 3 - Write messages to segments
// await using var producer = new SurgewaveProducer<string, string>(opt =>
// {
//     opt.BootstrapServers = $"localhost:{runtime.Port}";
// });
//
// Console.WriteLine("\n--- Writing to Cold Tier ---");
// var messageCount = 100;
// Console.WriteLine($"Writing {messageCount} messages...");
//
// for (int i = 0; i < messageCount; i++)
// {
//     var key = $"order-{i:D6}";
//     var value = $"{{\"orderId\": \"{key}\", \"amount\": {Random.Shared.Next(100, 10000)}}}";
//
//     // Write to Surgewave
//     await producer.ProduceAsync(topic, key, value);
//
//     // Also write to cold tier segment (simulated archival)
//     // await segmentWriter.WriteAsync(key, value);
// }

// TODO: Step 4 - Calculate segment statistics
// var segmentFiles = Directory.GetFiles(coldTierPath, "*.log*");
// var totalSize = segmentFiles.Sum(f => new FileInfo(f).Length);
// var compressedCount = segmentFiles.Count(f => f.EndsWith(".gz"));
//
// Console.WriteLine("\n--- Segment Statistics ---");
// Console.WriteLine($"Total segments: {segmentFiles.Length}");
// Console.WriteLine($"Compressed segments: {compressedCount}");
// Console.WriteLine($"Total size: {totalSize:N0} bytes");

// TODO: Step 5 - Implement segment reader to read messages back
// See INSTRUCTIONS.md for the SegmentReader implementation
// Console.WriteLine("\n--- Reading from Cold Tier ---");
// var reader = new SegmentReader(coldTierPath);
// var messagesRead = 0;
// await foreach (var message in reader.ReadAllAsync())
// {
//     messagesRead++;
// }
// Console.WriteLine($"Read {messagesRead} messages from cold tier");

// TODO: Step 6 - Implement retention-based cleanup
// var cleaner = new SegmentCleaner(coldTierPath, TimeSpan.FromDays(90));
// var cleanupResult = cleaner.Cleanup();
// Console.WriteLine("\n--- Cleanup Results ---");
// Console.WriteLine($"Total files: {cleanupResult.TotalFiles}");
// Console.WriteLine($"Deleted files: {cleanupResult.DeletedFiles}");
// Console.WriteLine($"Bytes freed: {cleanupResult.DeletedBytes:N0}");

// Cleanup temp directory
try
{
    if (Directory.Exists(coldTierPath))
    {
        Directory.Delete(coldTierPath, recursive: true);
    }
}
catch { /* Ignore cleanup errors */ }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Segments are append-only log files");
Console.WriteLine("2. Compression reduces storage costs significantly");
Console.WriteLine("3. Index files enable fast message lookup");
Console.WriteLine("4. Retention policies automatically clean up old segments");
Console.WriteLine("5. Cold tier trades latency for lower storage costs");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
