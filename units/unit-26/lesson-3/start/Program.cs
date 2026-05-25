// Surgewave Bootcamp - Unit 24, Lesson 3: S3/MinIO Integration
//
// In this lesson, you'll learn:
// - How to configure S3-compatible storage as cold tier
// - Object key naming conventions
// - Uploading and retrieving segments
// - Cost optimization with storage classes
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 24.3 - S3/MinIO Integration ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var topic = "orders";
var s3SimPath = Path.Combine(Path.GetTempPath(), "surgewave-s3-sim", Guid.NewGuid().ToString());

// TODO: Step 1 - Define S3 configuration
// public record S3StorageConfig(
//     string Endpoint,
//     string Bucket,
//     string KeyPrefix,
//     string AccessKey,
//     string SecretKey,
//     S3StorageClass StorageClass = S3StorageClass.Standard
// );
//
// public enum S3StorageClass
// {
//     Standard,
//     StandardIA,
//     GlacierInstantRetrieval,
//     GlacierFlexible,
//     GlacierDeepArchive
// }
//
// var config = new S3StorageConfig(
//     Endpoint: "http://localhost:9000",
//     Bucket: "surgewave-archive",
//     KeyPrefix: "surgewave/topics",
//     AccessKey: "minioadmin",
//     SecretKey: "minioadmin",
//     StorageClass: S3StorageClass.GlacierInstantRetrieval
// );
//
// Console.WriteLine("\n--- S3 Configuration ---");
// Console.WriteLine($"Endpoint: {config.Endpoint} (simulated)");
// Console.WriteLine($"Bucket: {config.Bucket}");
// Console.WriteLine($"Key Prefix: {config.KeyPrefix}");
// Console.WriteLine($"Storage Class: {config.StorageClass}");

// TODO: Step 2 - Create S3 key generator
// See INSTRUCTIONS.md for the S3KeyGenerator implementation
// var testKey = S3KeyGenerator.GenerateKey("surgewave/topics", "orders", 0, 0);
// Console.WriteLine($"\nGenerated key example: {testKey}");

// TODO: Step 3 - Create simulated S3 client
// See INSTRUCTIONS.md for the SimulatedS3Client implementation

// TODO: Step 4 - Create segment data and upload to S3
// await using var producer = new SurgewaveProducer<string, string>(opt =>
// {
//     opt.BootstrapServers = $"localhost:{runtime.Port}";
// });
//
// Console.WriteLine("\n--- Uploading Segments to S3 ---");
//
// var uploader = new S3SegmentUploader(s3Client, config);
//
// for (int seg = 0; seg < 2; seg++)
// {
//     // Create segment data
//     var segmentData = new MemoryStream();
//     var writer = new BinaryWriter(segmentData);
//
//     for (int i = 0; i < 1000; i++)
//     {
//         var offset = seg * 1000 + i;
//         var key = $"order-{offset:D6}";
//         var value = $"{{\"orderId\": \"{key}\", \"amount\": {Random.Shared.Next(100, 10000)}}}";
//
//         // Write to Surgewave
//         await producer.ProduceAsync(topic, key, value);
//
//         // Write to segment buffer
//         writer.Write((long)offset);
//         writer.Write(DateTime.UtcNow.Ticks);
//         writer.Write(key);
//         writer.Write(value);
//     }
//
//     // Upload segment
//     var result = await uploader.UploadSegmentAsync(
//         topic, 0, seg * 1000, segmentData.ToArray(), 1000);
//
//     Console.WriteLine($"Uploaded: {result.Key}");
//     Console.WriteLine($"  Original: {result.OriginalSize:N0} bytes");
//     Console.WriteLine($"  Compressed: {result.CompressedSize:N0} bytes ({result.CompressionRatio:P1})");
// }

// TODO: Step 5 - List objects in S3
// Console.WriteLine("\n--- S3 Objects Listed ---");
// foreach (var obj in s3Client.ListObjects(config.Bucket, config.KeyPrefix))
// {
//     Console.WriteLine($"{obj.Key} ({obj.Size:N0} bytes)");
// }

// TODO: Step 6 - Retrieve segment from S3
// Console.WriteLine("\n--- Retrieving Segment from S3 ---");
// var retriever = new S3SegmentRetriever(s3Client, config);
// var retrieved = await retriever.RetrieveSegmentAsync(topic, 0, 0);
// Console.WriteLine($"Retrieved: {retrieved.Key}");
// Console.WriteLine($"  Compressed: {retrieved.CompressedSize:N0} bytes");
// Console.WriteLine($"  Decompressed: {retrieved.DecompressedSize:N0} bytes");
// Console.WriteLine($"  Retrieval time: {retrieved.RetrievalTime.TotalMilliseconds:F1}ms");

// TODO: Step 7 - Calculate cost comparison
// Console.WriteLine("\n--- Cost Comparison (for 100 GB) ---");
// var testBytes = 100L * 1024 * 1024 * 1024;
// var costs = S3CostCalculator.CompareCosts(testBytes);
// Console.WriteLine($"Standard:              ${costs.Standard:F2}/month");
// Console.WriteLine($"Standard-IA:           ${costs.StandardIA:F2}/month");
// Console.WriteLine($"Glacier Instant:       ${costs.GlacierIR:F2}/month  <-- Recommended");
// Console.WriteLine($"Glacier Flexible:      ${costs.GlacierFlexible:F2}/month");
// Console.WriteLine($"Glacier Deep Archive:  ${costs.GlacierDeep:F2}/month");

// Cleanup
try
{
    if (Directory.Exists(s3SimPath))
    {
        Directory.Delete(s3SimPath, recursive: true);
    }
}
catch { /* Ignore cleanup errors */ }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. S3/MinIO provides scalable cold tier storage");
Console.WriteLine("2. Use consistent key naming for organization");
Console.WriteLine("3. Compress segments before upload to reduce costs");
Console.WriteLine("4. Choose storage class based on access patterns");
Console.WriteLine("5. MinIO enables local S3-compatible development");

Console.WriteLine();
Console.WriteLine("Lesson complete!");