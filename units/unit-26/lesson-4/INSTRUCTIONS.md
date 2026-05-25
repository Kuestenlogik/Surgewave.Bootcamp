# Lesson 24.4: Instructions

Follow these step-by-step instructions to implement Azure Blob Storage integration for cold tier storage.

## Step 1: Define Azure Configuration

Create a configuration model for Azure Blob Storage:

```csharp
public record AzureBlobStorageConfig(
    string AccountName,
    string ContainerName,
    string BlobPrefix,
    AzureAccessTier AccessTier = AzureAccessTier.Cool
);

public enum AzureAccessTier
{
    Hot,
    Cool,
    Cold,
    Archive
}
```

## Step 2: Create Access Tier Cost Calculator

Calculate costs for different Azure tiers:

```csharp
public static class AzureCostCalculator
{
    private static readonly Dictionary<AzureAccessTier, TierCost> Costs = new()
    {
        [AzureAccessTier.Hot] = new(0.0184m, 0.00044m, 0.00055m),
        [AzureAccessTier.Cool] = new(0.01m, 0.001m, 0.01m),
        [AzureAccessTier.Cold] = new(0.0036m, 0.01m, 0.018m),
        [AzureAccessTier.Archive] = new(0.00099m, 0.55m, 0.011m)
    };

    public static decimal CalculateMonthlyCost(
        long storageBytes,
        int monthlyReads,
        int monthlyWrites,
        AzureAccessTier tier)
    {
        var cost = Costs[tier];
        var storageGb = storageBytes / (1024m * 1024m * 1024m);

        var storageCost = storageGb * cost.StoragePerGb;
        var readCost = (monthlyReads / 10000m) * cost.ReadPer10k;
        var writeCost = (monthlyWrites / 10000m) * cost.WritePer10k;

        return storageCost + readCost + writeCost;
    }
}

public record TierCost(decimal StoragePerGb, decimal ReadPer10k, decimal WritePer10k);
```

## Step 3: Create Simulated Azure Blob Client

Since we're not connecting to real Azure, create a simulated client:

```csharp
public class SimulatedAzureBlobClient
{
    private readonly string _localPath;
    private readonly Dictionary<string, BlobMetadata> _blobs = new();

    public SimulatedAzureBlobClient(string localPath)
    {
        _localPath = localPath;
        Directory.CreateDirectory(_localPath);
    }

    public async Task<UploadResult> UploadBlobAsync(
        string container,
        string blobName,
        byte[] data,
        AzureAccessTier accessTier,
        Dictionary<string, string>? metadata = null)
    {
        var path = Path.Combine(_localPath, container, blobName.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await File.WriteAllBytesAsync(path, data);

        var blobMeta = new BlobMetadata(
            Name: blobName,
            Size: data.Length,
            LastModified: DateTime.UtcNow,
            AccessTier: accessTier,
            CustomMetadata: metadata ?? new(),
            RehydrationStatus: null
        );

        _blobs[blobName] = blobMeta;

        return new UploadResult(blobName, data.Length);
    }

    public async Task<byte[]> DownloadBlobAsync(string container, string blobName)
    {
        var meta = _blobs[blobName];

        // Simulate archive tier behavior
        if (meta.AccessTier == AzureAccessTier.Archive && meta.RehydrationStatus != "complete")
        {
            throw new InvalidOperationException(
                "Blob is in Archive tier. Must rehydrate before reading.");
        }

        var path = Path.Combine(_localPath, container, blobName.Replace('/', Path.DirectorySeparatorChar));
        return await File.ReadAllBytesAsync(path);
    }

    public async Task SetBlobTierAsync(string container, string blobName, AzureAccessTier tier)
    {
        if (_blobs.TryGetValue(blobName, out var meta))
        {
            var currentTier = meta.AccessTier;

            // Simulate rehydration from Archive
            if (currentTier == AzureAccessTier.Archive && tier != AzureAccessTier.Archive)
            {
                _blobs[blobName] = meta with
                {
                    RehydrationStatus = "pending",
                    RehydrateTargetTier = tier
                };

                // Simulate instant rehydration for demo
                await Task.Delay(100);

                _blobs[blobName] = meta with
                {
                    AccessTier = tier,
                    RehydrationStatus = "complete",
                    RehydrateTargetTier = null
                };
            }
            else
            {
                _blobs[blobName] = meta with { AccessTier = tier };
            }
        }
    }

    public IEnumerable<BlobMetadata> ListBlobs(string container, string? prefix = null)
    {
        return _blobs.Values
            .Where(b => prefix == null || b.Name.StartsWith(prefix))
            .OrderBy(b => b.Name);
    }

    public BlobMetadata? GetBlobMetadata(string blobName)
    {
        return _blobs.TryGetValue(blobName, out var meta) ? meta : null;
    }
}

public record BlobMetadata(
    string Name,
    long Size,
    DateTime LastModified,
    AzureAccessTier AccessTier,
    Dictionary<string, string> CustomMetadata,
    string? RehydrationStatus,
    AzureAccessTier? RehydrateTargetTier = null
);

public record UploadResult(string BlobName, long Size);
```

## Step 4: Implement Blob Uploader

Create a service to upload segments to Azure:

```csharp
public class AzureBlobUploader
{
    private readonly SimulatedAzureBlobClient _client;
    private readonly AzureBlobStorageConfig _config;

    public AzureBlobUploader(SimulatedAzureBlobClient client, AzureBlobStorageConfig config)
    {
        _client = client;
        _config = config;
    }

    public async Task<BlobUploadResult> UploadSegmentAsync(
        string topic,
        int partition,
        long baseOffset,
        byte[] segmentData,
        int messageCount)
    {
        // Compress the segment
        var compressedData = await CompressAsync(segmentData);

        // Generate blob name
        var blobName = $"{_config.BlobPrefix}/{topic}/{partition}/{baseOffset:D20}.log.zst";

        // Prepare metadata
        var metadata = new Dictionary<string, string>
        {
            ["topic"] = topic,
            ["partition"] = partition.ToString(),
            ["baseOffset"] = baseOffset.ToString(),
            ["messageCount"] = messageCount.ToString(),
            ["uncompressedSize"] = segmentData.Length.ToString(),
            ["compressedSize"] = compressedData.Length.ToString(),
            ["compressionType"] = "zstd"
        };

        // Upload to Azure with access tier
        var result = await _client.UploadBlobAsync(
            _config.ContainerName,
            blobName,
            compressedData,
            _config.AccessTier,
            metadata
        );

        return new BlobUploadResult(
            BlobName: blobName,
            OriginalSize: segmentData.Length,
            CompressedSize: compressedData.Length,
            AccessTier: _config.AccessTier
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

public record BlobUploadResult(
    string BlobName,
    long OriginalSize,
    long CompressedSize,
    AzureAccessTier AccessTier
);
```

## Step 5: Implement Lifecycle Simulator

Simulate lifecycle management transitions:

```csharp
public class LifecycleSimulator
{
    private readonly SimulatedAzureBlobClient _client;
    private readonly LifecyclePolicy _policy;

    public LifecycleSimulator(SimulatedAzureBlobClient client, LifecyclePolicy policy)
    {
        _client = client;
        _policy = policy;
    }

    public async Task<LifecycleResult> ApplyPolicyAsync(string container, string prefix)
    {
        var result = new LifecycleResult();

        foreach (var blob in _client.ListBlobs(container, prefix))
        {
            var age = DateTime.UtcNow - blob.LastModified;

            AzureAccessTier? targetTier = null;
            bool shouldDelete = false;

            if (_policy.DeleteAfterDays.HasValue && age.TotalDays >= _policy.DeleteAfterDays)
            {
                shouldDelete = true;
                result.DeletedCount++;
            }
            else if (_policy.TierToArchiveDays.HasValue && age.TotalDays >= _policy.TierToArchiveDays)
            {
                targetTier = AzureAccessTier.Archive;
            }
            else if (_policy.TierToColdDays.HasValue && age.TotalDays >= _policy.TierToColdDays)
            {
                targetTier = AzureAccessTier.Cold;
            }
            else if (_policy.TierToCoolDays.HasValue && age.TotalDays >= _policy.TierToCoolDays)
            {
                targetTier = AzureAccessTier.Cool;
            }

            if (!shouldDelete && targetTier.HasValue && targetTier != blob.AccessTier)
            {
                await _client.SetBlobTierAsync(container, blob.Name, targetTier.Value);
                result.TransitionedCount++;
            }
        }

        return result;
    }
}

public record LifecyclePolicy(
    int? TierToCoolDays,
    int? TierToColdDays,
    int? TierToArchiveDays,
    int? DeleteAfterDays
);

public record LifecycleResult
{
    public int TransitionedCount { get; set; }
    public int DeletedCount { get; set; }
}
```

## Step 6: Implement Archive Retriever

Handle archive tier rehydration:

```csharp
public class AzureBlobRetriever
{
    private readonly SimulatedAzureBlobClient _client;
    private readonly AzureBlobStorageConfig _config;

    public AzureBlobRetriever(SimulatedAzureBlobClient client, AzureBlobStorageConfig config)
    {
        _client = client;
        _config = config;
    }

    public async Task<BlobRetrieveResult> RetrieveSegmentAsync(
        string topic,
        int partition,
        long baseOffset)
    {
        var sw = Stopwatch.StartNew();

        var blobName = $"{_config.BlobPrefix}/{topic}/{partition}/{baseOffset:D20}.log.zst";
        var metadata = _client.GetBlobMetadata(blobName);

        bool wasRehydrated = false;

        // Check if we need to rehydrate
        if (metadata?.AccessTier == AzureAccessTier.Archive)
        {
            Console.WriteLine($"Blob is in Archive tier. Initiating rehydration...");
            await _client.SetBlobTierAsync(_config.ContainerName, blobName, AzureAccessTier.Cool);
            wasRehydrated = true;
        }

        // Download
        var compressedData = await _client.DownloadBlobAsync(_config.ContainerName, blobName);
        var decompressedData = await DecompressAsync(compressedData);

        sw.Stop();

        return new BlobRetrieveResult(
            BlobName: blobName,
            CompressedSize: compressedData.Length,
            DecompressedSize: decompressedData.Length,
            Data: decompressedData,
            RetrievalTime: sw.Elapsed,
            WasRehydrated: wasRehydrated
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

public record BlobRetrieveResult(
    string BlobName,
    long CompressedSize,
    long DecompressedSize,
    byte[] Data,
    TimeSpan RetrievalTime,
    bool WasRehydrated
);
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Azure configuration
- Blob upload with access tier
- Lifecycle policy simulation
- Archive rehydration
- Cost comparison

## Sample Output

```
=== Surgewave Bootcamp: Lesson 24.4 - Azure Blob Storage ===

Surgewave broker started on port 12345

--- Azure Configuration ---
Account: surgewavearchive (simulated)
Container: surgewave-segments
Blob Prefix: topics
Access Tier: Cold

--- Uploading Segments to Azure ---
Uploading segment 0 (1000 messages)...
  Blob: topics/orders/0/00000000000000000000.log.zst
  Original: 89,234 bytes
  Compressed: 23,456 bytes
  Access Tier: Cold

Uploading segment 1 (1000 messages)...
  Blob: topics/orders/0/00000000000000001000.log.zst
  Original: 91,102 bytes
  Compressed: 24,001 bytes
  Access Tier: Cold

--- Blobs Listed ---
topics/orders/0/00000000000000000000.log.zst (23,456 bytes) [Cold]
topics/orders/0/00000000000000001000.log.zst (24,001 bytes) [Cold]

--- Simulating Lifecycle Transition to Archive ---
Blob topics/orders/0/00000000000000000000.log.zst moved to Archive tier

--- Retrieving from Archive Tier ---
Blob is in Archive tier. Initiating rehydration...
Retrieved: topics/orders/0/00000000000000000000.log.zst
  Decompressed: 89,234 bytes
  Retrieval time: 125.3ms (includes simulated rehydration)

--- Cost Comparison (100 GB, 10k reads, 1k writes/month) ---
Hot tier:     $1.89/month
Cool tier:    $1.02/month
Cold tier:    $0.47/month  <-- Recommended for cold tier
Archive tier: $0.17/month  (but $5.50 per 10k reads!)

Lesson complete!
```

## Knowledge Check

1. What are the minimum storage duration requirements for each Azure tier?
2. How does Azure Archive tier differ from S3 Glacier in terms of retrieval?
3. When would you use lifecycle management vs. manual tier transitions?
4. What is the cost implication of high read frequency on Archive tier?
5. How does managed identity improve security for Azure Blob access?

---

**Next:** [Lesson 24.5: Retention and Lifecycle Policies](../lesson-5/README.md)
