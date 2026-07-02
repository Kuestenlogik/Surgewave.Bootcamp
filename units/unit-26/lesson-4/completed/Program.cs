// Surgewave Bootcamp - Unit 24, Lesson 4: Azure Blob Storage
// COMPLETED SOLUTION

using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 24.4 - Azure Blob Storage ===");
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
var azureSimPath = Path.Combine(Path.GetTempPath(), "surgewave-azure-sim", Guid.NewGuid().ToString());

// Step 1 - Define Azure configuration
var config = new AzureBlobStorageConfig(
    AccountName: "surgewavearchive",
    ContainerName: "surgewave-segments",
    BlobPrefix: "topics",
    AccessTier: AzureAccessTier.Cold
);

Console.WriteLine("\n--- Azure Configuration ---");
Console.WriteLine($"Account: {config.AccountName} (simulated)");
Console.WriteLine($"Container: {config.ContainerName}");
Console.WriteLine($"Blob Prefix: {config.BlobPrefix}");
Console.WriteLine($"Access Tier: {config.AccessTier}");

// Step 2 - Create simulated Azure Blob client
var azureClient = new SimulatedAzureBlobClient(azureSimPath);

// Step 3 - Create segment data and upload to Azure
await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

Console.WriteLine("\n--- Uploading Segments to Azure ---");

var uploader = new AzureBlobUploader(azureClient, config);

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

    Console.WriteLine($"  Blob: {result.BlobName}");
    Console.WriteLine($"  Original: {result.OriginalSize:N0} bytes");
    Console.WriteLine($"  Compressed: {result.CompressedSize:N0} bytes");
    Console.WriteLine($"  Access Tier: {result.AccessTier}");
    Console.WriteLine();
}

// Step 4 - List blobs with their access tiers
Console.WriteLine("--- Blobs Listed ---");
foreach (var blob in azureClient.ListBlobs(config.ContainerName, config.BlobPrefix))
{
    Console.WriteLine($"{blob.Name} ({blob.Size:N0} bytes) [{blob.AccessTier}]");
}

// Step 5 - Simulate lifecycle transition to Archive
Console.WriteLine("\n--- Simulating Lifecycle Transition to Archive ---");
var blobName = $"{config.BlobPrefix}/{topic}/0/{0:D20}.log.zst";
await azureClient.SetBlobTierAsync(config.ContainerName, blobName, AzureAccessTier.Archive);
Console.WriteLine($"Blob {blobName} moved to Archive tier");

// Step 6 - Retrieve from Archive tier (with rehydration)
Console.WriteLine("\n--- Retrieving from Archive Tier ---");
var retriever = new AzureBlobRetriever(azureClient, config);
var retrieved = await retriever.RetrieveSegmentAsync(topic, 0, 0);
Console.WriteLine($"Retrieved: {retrieved.BlobName}");
Console.WriteLine($"  Decompressed: {retrieved.DecompressedSize:N0} bytes");
Console.WriteLine($"  Retrieval time: {retrieved.RetrievalTime.TotalMilliseconds:F1}ms");
if (retrieved.WasRehydrated)
    Console.WriteLine($"  (Includes simulated rehydration from Archive)");

// Step 7 - Calculate cost comparison
Console.WriteLine("\n--- Cost Comparison (100 GB, 10k reads, 1k writes/month) ---");
var storageBytes = 100L * 1024 * 1024 * 1024;
var reads = 10000;
var writes = 1000;

foreach (var tier in Enum.GetValues<AzureAccessTier>())
{
    var cost = AzureCostCalculator.CalculateMonthlyCost(storageBytes, reads, writes, tier);
    var recommended = tier == AzureAccessTier.Cold ? "  <-- Recommended for cold tier" : "";
    var archiveNote = tier == AzureAccessTier.Archive ? "  (but requires rehydration!)" : "";
    Console.WriteLine($"{tier,-8}: ${cost:F2}/month{recommended}{archiveNote}");
}

// Cleanup
try
{
    if (Directory.Exists(azureSimPath))
    {
        Directory.Delete(azureSimPath, recursive: true);
    }
}
catch { /* Ignore cleanup errors */ }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Azure provides four access tiers with different cost profiles");
Console.WriteLine("2. Archive tier requires rehydration before reading");
Console.WriteLine("3. Lifecycle policies automate tier transitions");
Console.WriteLine("4. Cold tier offers good balance of cost and instant access");
Console.WriteLine("5. Use managed identity for secure Azure access");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Supporting types

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

public class SimulatedAzureBlobClient
{
    private readonly string _localPath;
    private readonly Dictionary<string, BlobMetadata> _blobs = new();

    public SimulatedAzureBlobClient(string localPath)
    {
        _localPath = localPath;
        Directory.CreateDirectory(_localPath);
    }

    public async Task<AzureUploadResult> UploadBlobAsync(
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

        return new AzureUploadResult(blobName, data.Length);
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
                    RehydrationStatus = "pending"
                };

                // Simulate rehydration delay (instant for demo)
                await Task.Delay(100);

                _blobs[blobName] = meta with
                {
                    AccessTier = tier,
                    RehydrationStatus = "complete"
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
    string? RehydrationStatus
);

public record AzureUploadResult(string BlobName, long Size);

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
        await _client.UploadBlobAsync(
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
        if (metadata?.AccessTier == AzureAccessTier.Archive && metadata.RehydrationStatus != "complete")
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