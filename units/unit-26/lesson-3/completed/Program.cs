// Surgewave Bootcamp - Unit 24, Lesson 3: S3/MinIO Integration
// COMPLETED SOLUTION

using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 24.3 - S3/MinIO Integration ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var topic = "orders";
var s3SimPath = Path.Combine(Path.GetTempPath(), "surgewave-s3-sim", Guid.NewGuid().ToString());

// Step 1 - Define S3 configuration
var config = new S3StorageConfig(
    Endpoint: "http://localhost:9000",
    Bucket: "surgewave-archive",
    KeyPrefix: "surgewave/topics",
    AccessKey: "minioadmin",
    SecretKey: "minioadmin",
    StorageClass: S3StorageClass.GlacierInstantRetrieval
);

Console.WriteLine("\n--- S3 Configuration ---");
Console.WriteLine($"Endpoint: {config.Endpoint} (simulated)");
Console.WriteLine($"Bucket: {config.Bucket}");
Console.WriteLine($"Key Prefix: {config.KeyPrefix}");
Console.WriteLine($"Storage Class: {config.StorageClass}");

// Step 2 - Demonstrate key generation
var testKey = S3KeyGenerator.GenerateKey("surgewave/topics", "orders", 0, 0);
Console.WriteLine($"\nGenerated key example: {testKey}");

// Step 3 - Create simulated S3 client
var s3Client = new SimulatedS3Client(s3SimPath);

// Step 4 - Create segment data and upload to S3
await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

Console.WriteLine("\n--- Uploading Segments to S3 ---");

var uploader = new S3SegmentUploader(s3Client, config);

for (int seg = 0; seg < 2; seg++)
{
    Console.WriteLine($"Uploading segment {seg} (1000 messages)...");

    // Create segment data
    using var segmentData = new MemoryStream();
    using var writer = new BinaryWriter(segmentData, Encoding.UTF8, leaveOpen: true);

    for (int i = 0; i < 1000; i++)
    {
        var offset = seg * 1000 + i;
        var key = $"order-{offset:D6}";
        var value = $"{{\"orderId\": \"{key}\", \"amount\": {Random.Shared.Next(100, 10000)}, \"timestamp\": \"{DateTime.UtcNow:O}\"}}";

        // Write to Surgewave
        await producer.ProduceAsync(topic, key, value);

        // Write to segment buffer
        writer.Write((long)offset);
        writer.Write(DateTime.UtcNow.Ticks);
        writer.Write(key);
        writer.Write(value);
    }

    // Upload segment
    var result = await uploader.UploadSegmentAsync(
        topic, 0, seg * 1000, segmentData.ToArray(), 1000);

    Console.WriteLine($"  Key: {result.Key}");
    Console.WriteLine($"  Original: {result.OriginalSize:N0} bytes");
    Console.WriteLine($"  Compressed: {result.CompressedSize:N0} bytes");
    Console.WriteLine($"  Ratio: {result.CompressionRatio:P1}");
    Console.WriteLine();
}

// Step 5 - List objects in S3
Console.WriteLine("--- S3 Objects Listed ---");
foreach (var obj in s3Client.ListObjects(config.Bucket, config.KeyPrefix))
{
    Console.WriteLine($"{obj.Key} ({obj.Size:N0} bytes)");
}

// Step 6 - Retrieve segment from S3
Console.WriteLine("\n--- Retrieving Segment from S3 ---");
var retriever = new S3SegmentRetriever(s3Client, config);
var retrieved = await retriever.RetrieveSegmentAsync(topic, 0, 0);
Console.WriteLine($"Retrieved: {retrieved.Key}");
Console.WriteLine($"  Compressed: {retrieved.CompressedSize:N0} bytes");
Console.WriteLine($"  Decompressed: {retrieved.DecompressedSize:N0} bytes");
Console.WriteLine($"  Retrieval time: {retrieved.RetrievalTime.TotalMilliseconds:F1}ms");

// Step 7 - Calculate cost comparison
Console.WriteLine("\n--- Cost Comparison (for 100 GB) ---");
var testBytes = 100L * 1024 * 1024 * 1024;
var costs = S3CostCalculator.CompareCosts(testBytes);
Console.WriteLine($"Standard:              ${costs.Standard:F2}/month");
Console.WriteLine($"Standard-IA:           ${costs.StandardIA:F2}/month");
Console.WriteLine($"Glacier Instant:       ${costs.GlacierIR:F2}/month  <-- Recommended for cold tier");
Console.WriteLine($"Glacier Flexible:      ${costs.GlacierFlexible:F2}/month");
Console.WriteLine($"Glacier Deep Archive:  ${costs.GlacierDeep:F2}/month");

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

// Supporting types

public record S3StorageConfig(
    string Endpoint,
    string Bucket,
    string KeyPrefix,
    string AccessKey,
    string SecretKey,
    S3StorageClass StorageClass = S3StorageClass.Standard
);

public enum S3StorageClass
{
    Standard,
    StandardIA,
    IntelligentTiering,
    GlacierInstantRetrieval,
    GlacierFlexible,
    GlacierDeepArchive
}

public static class S3KeyGenerator
{
    public static string GenerateKey(string prefix, string topic, int partition, long baseOffset)
    {
        var offsetStr = baseOffset.ToString("D20");
        return $"{prefix}/{topic}/{partition}/{offsetStr}.log.zst";
    }

    public static (string topic, int partition, long baseOffset) ParseKey(string key)
    {
        var parts = key.Split('/');
        var topic = parts[^3];
        var partition = int.Parse(parts[^2]);
        var fileName = parts[^1];
        var offsetStr = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fileName));
        var baseOffset = long.Parse(offsetStr);

        return (topic, partition, baseOffset);
    }
}

public class SimulatedS3Client
{
    private readonly string _localPath;
    private readonly Dictionary<string, S3ObjectMetadata> _objects = new();

    public SimulatedS3Client(string localPath)
    {
        _localPath = localPath;
        Directory.CreateDirectory(_localPath);
    }

    public async Task<PutObjectResult> PutObjectAsync(
        string bucket,
        string key,
        byte[] data,
        Dictionary<string, string>? metadata = null,
        S3StorageClass storageClass = S3StorageClass.Standard)
    {
        var path = Path.Combine(_localPath, bucket, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await File.WriteAllBytesAsync(path, data);

        var objMetadata = new S3ObjectMetadata(
            Key: key,
            Size: data.Length,
            LastModified: DateTime.UtcNow,
            StorageClass: storageClass,
            CustomMetadata: metadata ?? new()
        );

        _objects[key] = objMetadata;

        return new PutObjectResult(key, data.Length, ComputeETag(data));
    }

    public async Task<byte[]> GetObjectAsync(string bucket, string key)
    {
        var path = Path.Combine(_localPath, bucket, key.Replace('/', Path.DirectorySeparatorChar));
        return await File.ReadAllBytesAsync(path);
    }

    public IEnumerable<S3ObjectMetadata> ListObjects(string bucket, string? prefix = null)
    {
        return _objects.Values
            .Where(o => prefix == null || o.Key.StartsWith(prefix))
            .OrderBy(o => o.Key);
    }

    private static string ComputeETag(byte[] data)
    {
        var hash = MD5.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public record S3ObjectMetadata(
    string Key,
    long Size,
    DateTime LastModified,
    S3StorageClass StorageClass,
    Dictionary<string, string> CustomMetadata
);

public record PutObjectResult(string Key, long Size, string ETag);

public class S3SegmentUploader
{
    private readonly SimulatedS3Client _client;
    private readonly S3StorageConfig _config;

    public S3SegmentUploader(SimulatedS3Client client, S3StorageConfig config)
    {
        _client = client;
        _config = config;
    }

    public async Task<UploadResult> UploadSegmentAsync(
        string topic,
        int partition,
        long baseOffset,
        byte[] segmentData,
        int messageCount)
    {
        // Compress the segment
        var compressedData = await CompressAsync(segmentData);

        // Generate S3 key
        var key = S3KeyGenerator.GenerateKey(_config.KeyPrefix, topic, partition, baseOffset);

        // Prepare metadata
        var metadata = new Dictionary<string, string>
        {
            ["x-amz-meta-topic"] = topic,
            ["x-amz-meta-partition"] = partition.ToString(),
            ["x-amz-meta-base-offset"] = baseOffset.ToString(),
            ["x-amz-meta-message-count"] = messageCount.ToString(),
            ["x-amz-meta-uncompressed-size"] = segmentData.Length.ToString(),
            ["x-amz-meta-compressed-size"] = compressedData.Length.ToString()
        };

        // Upload to S3
        var result = await _client.PutObjectAsync(
            _config.Bucket,
            key,
            compressedData,
            metadata,
            _config.StorageClass
        );

        var compressionRatio = (double)compressedData.Length / segmentData.Length;

        return new UploadResult(
            Key: key,
            OriginalSize: segmentData.Length,
            CompressedSize: compressedData.Length,
            CompressionRatio: compressionRatio,
            ETag: result.ETag
        );
    }

    private static async Task<byte[]> CompressAsync(byte[] data)
    {
        using var output = new MemoryStream();
        await using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            await gzip.WriteAsync(data);
        }
        return output.ToArray();
    }
}

public record UploadResult(
    string Key,
    long OriginalSize,
    long CompressedSize,
    double CompressionRatio,
    string ETag
);

public class S3SegmentRetriever
{
    private readonly SimulatedS3Client _client;
    private readonly S3StorageConfig _config;

    public S3SegmentRetriever(SimulatedS3Client client, S3StorageConfig config)
    {
        _client = client;
        _config = config;
    }

    public async Task<RetrieveResult> RetrieveSegmentAsync(string topic, int partition, long baseOffset)
    {
        var sw = Stopwatch.StartNew();

        var key = S3KeyGenerator.GenerateKey(_config.KeyPrefix, topic, partition, baseOffset);

        // Download compressed data
        var compressedData = await _client.GetObjectAsync(_config.Bucket, key);

        // Decompress
        var decompressedData = await DecompressAsync(compressedData);

        sw.Stop();

        return new RetrieveResult(
            Key: key,
            CompressedSize: compressedData.Length,
            DecompressedSize: decompressedData.Length,
            Data: decompressedData,
            RetrievalTime: sw.Elapsed
        );
    }

    private static async Task<byte[]> DecompressAsync(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        await using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        await gzip.CopyToAsync(output);
        return output.ToArray();
    }
}

public record RetrieveResult(
    string Key,
    long CompressedSize,
    long DecompressedSize,
    byte[] Data,
    TimeSpan RetrievalTime
);

public static class S3CostCalculator
{
    private static readonly Dictionary<S3StorageClass, decimal> CostPerGbPerMonth = new()
    {
        [S3StorageClass.Standard] = 0.023m,
        [S3StorageClass.StandardIA] = 0.0125m,
        [S3StorageClass.IntelligentTiering] = 0.023m,
        [S3StorageClass.GlacierInstantRetrieval] = 0.004m,
        [S3StorageClass.GlacierFlexible] = 0.0036m,
        [S3StorageClass.GlacierDeepArchive] = 0.00099m
    };

    public static decimal CalculateMonthlyCost(long bytes, S3StorageClass storageClass)
    {
        var gb = bytes / (1024m * 1024m * 1024m);
        return gb * CostPerGbPerMonth[storageClass];
    }

    public static CostComparison CompareCosts(long bytes)
    {
        return new CostComparison(
            Standard: CalculateMonthlyCost(bytes, S3StorageClass.Standard),
            StandardIA: CalculateMonthlyCost(bytes, S3StorageClass.StandardIA),
            GlacierIR: CalculateMonthlyCost(bytes, S3StorageClass.GlacierInstantRetrieval),
            GlacierFlexible: CalculateMonthlyCost(bytes, S3StorageClass.GlacierFlexible),
            GlacierDeep: CalculateMonthlyCost(bytes, S3StorageClass.GlacierDeepArchive)
        );
    }
}

public record CostComparison(
    decimal Standard,
    decimal StandardIA,
    decimal GlacierIR,
    decimal GlacierFlexible,
    decimal GlacierDeep
);