// Surgewave Bootcamp - Unit 24, Lesson 2: Local File System Tier
// COMPLETED SOLUTION

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

// Step 1 - Define segment configuration
var config = new SegmentConfig(
    BasePath: coldTierPath,
    MaxSegmentBytes: 10 * 1024,  // 10 KB for demo
    MaxSegmentAge: TimeSpan.FromMinutes(5),
    EnableCompression: true
);

Console.WriteLine("\n--- Cold Tier Configuration ---");
Console.WriteLine($"Base path: {config.BasePath}");
Console.WriteLine($"Max segment size: {config.MaxSegmentBytes:N0} bytes");
Console.WriteLine($"Compression: {(config.EnableCompression ? "Enabled (Gzip)" : "Disabled")}");

// Step 2 & 3 - Write messages to segments
await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

Console.WriteLine("\n--- Writing to Cold Tier ---");
var messageCount = 100;
Console.WriteLine($"Writing {messageCount} messages...");

await using (var segmentWriter = new SegmentWriter(config))
{
    for (int i = 0; i < messageCount; i++)
    {
        var key = $"order-{i:D6}";
        var value = $"{{\"orderId\": \"{key}\", \"amount\": {Random.Shared.Next(100, 10000)}, \"timestamp\": \"{DateTime.UtcNow:O}\"}}";

        // Write to Surgewave (hot tier)
        await producer.ProduceAsync(topic, key, value);

        // Archive to cold tier segment
        await segmentWriter.WriteAsync(key, value);
    }
}

// Step 4 - Calculate segment statistics
var stats = new SegmentStats(coldTierPath);
var report = stats.GetUsageReport();

Console.WriteLine("\n--- Segment Statistics ---");
Console.WriteLine($"Total segments: {report.TotalSegments}");
Console.WriteLine($"Compressed segments: {report.CompressedSegments}");
Console.WriteLine($"Total size: {report.TotalSizeBytes:N0} bytes");
Console.WriteLine($"Compression ratio: {report.CompressionRatio:P1}");
if (report.OldestSegment.HasValue)
    Console.WriteLine($"Oldest segment: {report.OldestSegment:yyyy-MM-dd HH:mm:ss}");
if (report.NewestSegment.HasValue)
    Console.WriteLine($"Newest segment: {report.NewestSegment:yyyy-MM-dd HH:mm:ss}");

// Step 5 - Read messages back from cold tier
Console.WriteLine("\n--- Reading from Cold Tier ---");
var reader = new SegmentReader(coldTierPath);
var messagesRead = 0;
StoredMessage? firstMessage = null;
StoredMessage? lastMessage = null;

await foreach (var message in reader.ReadAllAsync())
{
    firstMessage ??= message;
    lastMessage = message;
    messagesRead++;
}

Console.WriteLine($"Read {messagesRead} messages from cold tier");
if (firstMessage != null)
    Console.WriteLine($"First message: {firstMessage.Key} (offset {firstMessage.Offset})");
if (lastMessage != null)
    Console.WriteLine($"Last message: {lastMessage.Key} (offset {lastMessage.Offset})");

// Step 6 - Simulate retention cleanup
Console.WriteLine("\n--- Cleanup Simulation ---");
var cleaner = new SegmentCleaner(coldTierPath, TimeSpan.FromDays(90));
Console.WriteLine($"Segments before cleanup: {report.TotalSegments}");
var cleanupResult = cleaner.Cleanup();
Console.WriteLine($"Deleted {cleanupResult.DeletedFiles} expired segments (retention: 90 days)");
Console.WriteLine($"Bytes that would be freed: {cleanupResult.DeletedBytes:N0}");

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

// Supporting types

public record SegmentConfig(
    string BasePath,
    long MaxSegmentBytes,
    TimeSpan MaxSegmentAge,
    bool EnableCompression
);

public record StoredMessage(
    long Offset,
    string Key,
    string Value,
    DateTime Timestamp
);

public class SegmentWriter : IAsyncDisposable
{
    private readonly string _basePath;
    private readonly long _maxBytes;
    private readonly bool _compress;

    private FileStream? _currentSegment;
    private BinaryWriter? _writer;
    private long _currentOffset;
    private long _bytesWritten;
    private string _currentPath = "";
    private int _segmentCount;

    public SegmentWriter(SegmentConfig config)
    {
        _basePath = config.BasePath;
        _maxBytes = config.MaxSegmentBytes;
        _compress = config.EnableCompression;
        _currentOffset = 0;
        _segmentCount = 0;

        Directory.CreateDirectory(_basePath);
    }

    public async Task<long> WriteAsync(string key, string value)
    {
        // Roll segment if needed
        if (_currentSegment == null || _bytesWritten >= _maxBytes)
        {
            await RollSegmentAsync();
        }

        // Serialize and write message
        _writer!.Write(_currentOffset);
        _writer.Write(DateTime.UtcNow.Ticks);
        _writer.Write(key);
        _writer.Write(value);

        // Estimate bytes written
        var messageSize = 8 + 8 + key.Length + value.Length + 8;  // Approximate
        _bytesWritten += messageSize;

        return _currentOffset++;
    }

    private async Task RollSegmentAsync()
    {
        // Close current segment
        if (_currentSegment != null)
        {
            _writer!.Flush();
            _writer.Dispose();
            await _currentSegment.FlushAsync();
            _currentSegment.Dispose();

            // Compress the closed segment
            if (_compress)
            {
                await CompressSegmentAsync(_currentPath);
            }
        }

        // Create new segment
        var segmentName = $"{_currentOffset:D20}.log";
        _currentPath = Path.Combine(_basePath, segmentName);
        _currentSegment = new FileStream(_currentPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true);
        _writer = new BinaryWriter(_currentSegment, Encoding.UTF8, leaveOpen: true);
        _bytesWritten = 0;
        _segmentCount++;
    }

    private async Task CompressSegmentAsync(string path)
    {
        var compressedPath = path + ".gz";

        await using var input = File.OpenRead(path);
        await using var output = File.Create(compressedPath);
        await using var gzip = new GZipStream(output, CompressionLevel.Optimal);
        await input.CopyToAsync(gzip);

        // Delete uncompressed file
        File.Delete(path);
    }

    public async ValueTask DisposeAsync()
    {
        if (_writer != null)
        {
            _writer.Flush();
            _writer.Dispose();
        }

        if (_currentSegment != null)
        {
            await _currentSegment.FlushAsync();
            await _currentSegment.DisposeAsync();

            // Compress final segment
            if (_compress && File.Exists(_currentPath))
            {
                await CompressSegmentAsync(_currentPath);
            }
        }
    }
}

public class SegmentReader
{
    private readonly string _basePath;

    public SegmentReader(string basePath)
    {
        _basePath = basePath;
    }

    public async IAsyncEnumerable<StoredMessage> ReadAllAsync()
    {
        if (!Directory.Exists(_basePath))
            yield break;

        var segmentFiles = Directory.GetFiles(_basePath, "*.log*")
            .OrderBy(f => f)
            .ToList();

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
        FileStream? fileStream = null;

        try
        {
            if (path.EndsWith(".gz"))
            {
                fileStream = File.OpenRead(path);
                stream = new GZipStream(fileStream, CompressionMode.Decompress);
            }
            else
            {
                stream = File.OpenRead(path);
            }

            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);

            while (true)
            {
                StoredMessage message;
                try
                {
                    var offset = reader.ReadInt64();
                    var timestamp = new DateTime(reader.ReadInt64());
                    var key = reader.ReadString();
                    var value = reader.ReadString();
                    message = new StoredMessage(offset, key, value, timestamp);
                }
                catch (EndOfStreamException)
                {
                    break;
                }

                yield return message;
            }
        }
        finally
        {
            fileStream?.Dispose();
        }
    }
}

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

        if (!Directory.Exists(_basePath))
            return result;

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

public class SegmentStats
{
    private readonly string _basePath;

    public SegmentStats(string basePath)
    {
        _basePath = basePath;
    }

    public DiskUsageReport GetUsageReport()
    {
        if (!Directory.Exists(_basePath))
        {
            return new DiskUsageReport();
        }

        var files = Directory.GetFiles(_basePath, "*.log*");
        var totalSize = files.Sum(f => new FileInfo(f).Length);
        var compressedCount = files.Count(f => f.EndsWith(".gz"));

        DateTime? oldest = null;
        DateTime? newest = null;

        if (files.Length > 0)
        {
            oldest = files.Min(f => File.GetLastWriteTimeUtc(f));
            newest = files.Max(f => File.GetLastWriteTimeUtc(f));
        }

        return new DiskUsageReport
        {
            TotalSegments = files.Length,
            CompressedSegments = compressedCount,
            TotalSizeBytes = totalSize,
            OldestSegment = oldest,
            NewestSegment = newest
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