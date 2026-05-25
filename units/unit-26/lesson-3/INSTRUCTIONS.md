# Lesson 24.3: Instructions

Follow these step-by-step instructions to implement S3/MinIO integration for cold tier storage.

## Step 1: Define S3 Configuration

Create a configuration model for S3 storage:

```csharp
public record S3StorageConfig(
    string Endpoint,
    string Bucket,
    string KeyPrefix,
    string AccessKey,
    string SecretKey,
    string? Region = null,
    bool UsePathStyle = false,
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
```

## Step 2: Create Object Key Generator

Generate consistent S3 keys for segments:

```csharp
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
        var offsetStr = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(parts[^1]));
        var baseOffset = long.Parse(offsetStr);

        return (topic, partition, baseOffset);
    }
}
```

## Step 3: Create a Simulated S3 Client

Since we're not connecting to real S3, create a simulated client:

```csharp
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
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(data);
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
```

## Step 4: Implement Segment Uploader

Create a service that uploads segments to S3:

```csharp
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

    private async Task<byte[]> CompressAsync(byte[] data)
    {
        using var output = new MemoryStream();
        await using var gzip = new GZipStream(output, CompressionLevel.Optimal);
        await gzip.WriteAsync(data);
        await gzip.FlushAsync();
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
```

## Step 5: Implement Segment Retriever

Create a service to download and decompress segments:

```csharp
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

    private async Task<byte[]> DecompressAsync(byte[] compressedData)
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
```

## Step 6: Calculate Storage Costs

Estimate costs based on storage class:

```csharp
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
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- S3 configuration
- Segment upload with compression
- Key generation and parsing
- Segment retrieval
- Cost estimation

## Sample Output

```
=== Surgewave Bootcamp: Lesson 24.3 - S3/MinIO Integration ===

Surgewave broker started on port 12345

--- S3 Configuration ---
Endpoint: http://localhost:9000 (simulated)
Bucket: surgewave-archive
Key Prefix: surgewave/topics
Storage Class: GlacierInstantRetrieval

--- Uploading Segments to S3 ---
Uploading segment 0 (1000 messages)...
  Key: surgewave/topics/orders/0/00000000000000000000.log.zst
  Original: 89,234 bytes
  Compressed: 23,456 bytes
  Ratio: 26.3%

Uploading segment 1 (1000 messages)...
  Key: surgewave/topics/orders/0/00000000000000001000.log.zst
  Original: 91,102 bytes
  Compressed: 24,001 bytes
  Ratio: 26.3%

--- S3 Objects Listed ---
surgewave/topics/orders/0/00000000000000000000.log.zst (23,456 bytes)
surgewave/topics/orders/0/00000000000000001000.log.zst (24,001 bytes)

--- Retrieving Segment from S3 ---
Retrieved: surgewave/topics/orders/0/00000000000000000000.log.zst
  Compressed: 23,456 bytes
  Decompressed: 89,234 bytes
  Retrieval time: 15.3ms

--- Cost Comparison (for 100 GB) ---
Standard:              $2.30/month
Standard-IA:           $1.25/month
Glacier Instant:       $0.40/month  <-- Recommended for cold tier
Glacier Flexible:      $0.36/month
Glacier Deep Archive:  $0.10/month

Lesson complete!
```

## Knowledge Check

1. What is the benefit of using multipart upload for large segments?
2. Why is MinIO useful for local development?
3. How do S3 storage classes affect both cost and retrieval time?
4. What metadata should be stored with each segment object?
5. Why compress segments before uploading to S3?

---

**Next:** [Lesson 24.4: Azure Blob Storage](../lesson-4/README.md)
