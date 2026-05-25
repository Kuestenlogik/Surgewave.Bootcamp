# Lesson 24.2: Instructions

Follow these step-by-step instructions to implement local file system tier storage.

## Step 1: Define Segment Configuration

Create configuration for segment management:

```csharp
public record SegmentConfig(
    string BasePath,
    long MaxSegmentBytes,
    TimeSpan MaxSegmentAge,
    CompressionType Compression
);

public enum CompressionType { None, Lz4, Zstd, Gzip }

var config = new SegmentConfig(
    BasePath: Path.Combine(Path.GetTempPath(), "surgewave-cold-tier"),
    MaxSegmentBytes: 10 * 1024 * 1024,  // 10 MB for demo
    MaxSegmentAge: TimeSpan.FromMinutes(5),
    Compression: CompressionType.Gzip
);
```

## Step 2: Create a Segment Writer

Implement a writer that creates segment files:

```csharp
public class SegmentWriter : IDisposable
{
    private readonly string _basePath;
    private readonly long _maxBytes;
    private readonly CompressionType _compression;

    private FileStream? _currentSegment;
    private long _currentOffset;
    private long _bytesWritten;
    private string _currentPath = "";

    public SegmentWriter(SegmentConfig config)
    {
        _basePath = config.BasePath;
        _maxBytes = config.MaxSegmentBytes;
        _compression = config.Compression;
        _currentOffset = 0;

        Directory.CreateDirectory(_basePath);
    }

    public async Task<long> WriteAsync(string key, string value)
    {
        // Roll segment if needed
        if (_currentSegment == null || _bytesWritten >= _maxBytes)
        {
            await RollSegmentAsync();
        }

        // Serialize message
        var messageBytes = SerializeMessage(_currentOffset, key, value);

        // Write to segment
        await _currentSegment!.WriteAsync(messageBytes);
        _bytesWritten += messageBytes.Length;

        return _currentOffset++;
    }

    private async Task RollSegmentAsync()
    {
        // Close current segment
        if (_currentSegment != null)
        {
            await _currentSegment.FlushAsync();
            _currentSegment.Close();
            _currentSegment.Dispose();

            // Compress the closed segment
            if (_compression != CompressionType.None)
            {
                await CompressSegmentAsync(_currentPath);
            }
        }

        // Create new segment
        var segmentName = $"{_currentOffset:D20}.log";
        _currentPath = Path.Combine(_basePath, segmentName);
        _currentSegment = new FileStream(_currentPath, FileMode.Create, FileAccess.Write);
        _bytesWritten = 0;
    }

    private byte[] SerializeMessage(long offset, string key, string value)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(offset);
        writer.Write(DateTime.UtcNow.Ticks);
        writer.Write(key);
        writer.Write(value);

        return ms.ToArray();
    }

    private async Task CompressSegmentAsync(string path)
    {
        var compressedPath = path + ".gz";
        using var input = File.OpenRead(path);
        using var output = File.Create(compressedPath);
        using var gzip = new GZipStream(output, CompressionLevel.Optimal);
        await input.CopyToAsync(gzip);

        // Delete uncompressed file
        File.Delete(path);
    }

    public void Dispose()
    {
        _currentSegment?.Dispose();
    }
}
```

## Step 3: Create a Segment Reader

Implement reading from segment files:

```csharp
public class SegmentReader
{
    private readonly string _basePath;

    public SegmentReader(string basePath)
    {
        _basePath = basePath;
    }

    public async IAsyncEnumerable<StoredMessage> ReadAllAsync()
    {
        var segmentFiles = Directory.GetFiles(_basePath, "*.log*")
            .OrderBy(f => f);

        foreach (var file in segmentFiles)
        {
            await foreach (var message in ReadSegmentAsync(file))
            {
                yield return message;
            }
        }
    }

    private async IAsyncEnumerable<StoredMessage> ReadSegmentAsync(string path)
    {
        Stream stream;

        if (path.EndsWith(".gz"))
        {
            // Decompress on-the-fly
            var fileStream = File.OpenRead(path);
            stream = new GZipStream(fileStream, CompressionMode.Decompress);
        }
        else
        {
            stream = File.OpenRead(path);
        }

        using var reader = new BinaryReader(stream);

        while (stream.Position < stream.Length)
        {
            var offset = reader.ReadInt64();
            var timestamp = new DateTime(reader.ReadInt64());
            var key = reader.ReadString();
            var value = reader.ReadString();

            yield return new StoredMessage(offset, key, value, timestamp);
        }
    }
}

public record StoredMessage(long Offset, string Key, string Value, DateTime Timestamp);
```

## Step 4: Implement Retention Cleanup

Create cleanup logic for expired segments:

```csharp
public class SegmentCleaner
{
    private readonly string _basePath;
    private readonly TimeSpan _retention;

    public SegmentCleaner(string basePath, TimeSpan retention)
    {
        _basePath = basePath;
        _retention = retention;
    }

    public CleanupResult Cleanup()
    {
        var result = new CleanupResult();
        var cutoff = DateTime.UtcNow - _retention;

        foreach (var file in Directory.GetFiles(_basePath, "*.log*"))
        {
            var fileInfo = new FileInfo(file);
            result.TotalFiles++;
            result.TotalBytes += fileInfo.Length;

            if (fileInfo.LastWriteTimeUtc < cutoff)
            {
                File.Delete(file);
                result.DeletedFiles++;
                result.DeletedBytes += fileInfo.Length;
            }
        }

        return result;
    }
}

public record CleanupResult
{
    public int TotalFiles { get; set; }
    public int DeletedFiles { get; set; }
    public long TotalBytes { get; set; }
    public long DeletedBytes { get; set; }
}
```

## Step 5: Monitor Disk Usage

Track segment statistics and disk usage:

```csharp
public class SegmentStats
{
    private readonly string _basePath;

    public SegmentStats(string basePath)
    {
        _basePath = basePath;
    }

    public DiskUsageReport GetUsageReport()
    {
        var files = Directory.GetFiles(_basePath, "*.log*");
        var totalSize = files.Sum(f => new FileInfo(f).Length);
        var compressedCount = files.Count(f => f.EndsWith(".gz"));

        return new DiskUsageReport
        {
            TotalSegments = files.Length,
            CompressedSegments = compressedCount,
            TotalSizeBytes = totalSize,
            OldestSegment = files.Any() ? File.GetLastWriteTimeUtc(files.Min()!) : null,
            NewestSegment = files.Any() ? File.GetLastWriteTimeUtc(files.Max()!) : null
        };
    }
}

public record DiskUsageReport
{
    public int TotalSegments { get; init; }
    public int CompressedSegments { get; init; }
    public long TotalSizeBytes { get; init; }
    public DateTime? OldestSegment { get; init; }
    public DateTime? NewestSegment { get; init; }

    public double CompressionRatio => TotalSegments > 0
        ? (double)CompressedSegments / TotalSegments
        : 0;
}
```

## Step 6: Integrate with Surgewave

Use the segment storage with Surgewave messages:

```csharp
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;
var topic = "cold-tier-demo";

// Write messages through Surgewave, simulate cold tier archival
await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
});

await using var segmentWriter = new SegmentWriter(config);

for (int i = 0; i < 1000; i++)
{
    var key = $"order-{i:D6}";
    var value = $"{{\"orderId\": \"{key}\", \"amount\": {Random.Shared.Next(100, 10000)}, \"timestamp\": \"{DateTime.UtcNow:O}\"}}";

    // Write to Surgewave (hot tier)
    await producer.ProduceAsync(topic, key, value);

    // Archive to cold tier (simulated)
    await segmentWriter.WriteAsync(key, value);
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Segment configuration
- Writing compressed segments
- Reading from cold tier
- Retention cleanup
- Usage monitoring

## Sample Output

```
=== Surgewave Bootcamp: Lesson 24.2 - Local File System Tier ===

Surgewave broker started on port 12345

--- Cold Tier Configuration ---
Base path: C:\Users\...\surgewave-cold-tier
Max segment size: 10 MB
Compression: Gzip

--- Writing to Cold Tier ---
Writing 1000 messages...
Created 3 segments

--- Segment Statistics ---
Total segments: 3
Compressed segments: 2
Total size: 45,678 bytes
Compression ratio: 66.7%
Oldest segment: 2024-01-15 10:30:00
Newest segment: 2024-01-15 10:30:05

--- Reading from Cold Tier ---
Read 1000 messages from cold tier
First message: order-000000
Last message: order-000999

--- Cleanup Simulation ---
Segments before cleanup: 3
Deleted 0 expired segments (retention: 90 days)

Lesson complete!
```

## Knowledge Check

1. What are the three main file types in a segment and their purposes?
2. Why is compression more aggressive in cold tier than hot tier?
3. What triggers a segment to roll (close and create new)?
4. How do index files speed up message retrieval?
5. What cleanup policies are available for expired segments?

---

**Next:** [Lesson 24.3: S3/MinIO Integration](../lesson-3/README.md)
