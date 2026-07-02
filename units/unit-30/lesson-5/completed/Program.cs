// Surgewave Bootcamp - Unit 17, Lesson 5: Streaming Large Data
// COMPLETED SOLUTION

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("Streaming Large Data Demo");
Console.WriteLine("=========================");
Console.WriteLine();

// Start broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// Configuration
var totalSize = 50 * 1024 * 1024; // 50 MB
var chunkSize = 64 * 1024; // 64 KB
var maxPendingChunks = 10;

Console.WriteLine($"Creating test data source ({totalSize / (1024 * 1024)} MB)...");
var testData = new byte[totalSize];
Random.Shared.NextBytes(testData);
Console.WriteLine();

Console.WriteLine("Starting streaming upload...");
Console.WriteLine($"  Chunk size: {chunkSize:N0} bytes (64 KB)");
Console.WriteLine($"  Backpressure threshold: {maxPendingChunks} pending chunks");
Console.WriteLine();

Console.WriteLine("Progress:");

await using var producer = new SurgewaveProducer<string, byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

const string topic = "streaming-data";

var streamer = new DataStreamer(chunkSize, maxPendingChunks);
var progress = new StreamingProgress(totalSize);

using var sourceStream = new MemoryStream(testData);
var chunkStream = streamer.StreamFromSourceAsync(sourceStream);
var backpressuredChannel = streamer.CreateBackpressuredChannel(chunkStream);

var chunksUploaded = 0;

await foreach (var chunk in backpressuredChannel.ReadAllAsync())
{
    var key = $"chunk-{chunksUploaded}";
    await producer.ProduceAsync(topic, key, chunk);
    chunksUploaded++;
    progress.Report(chunk.Length);
}

Console.WriteLine();
var uploadStats = progress.GetStats();
Console.WriteLine("Upload complete!");
Console.WriteLine($"  Total chunks: {chunksUploaded:N0}");
Console.WriteLine($"  Total bytes: {uploadStats.BytesTransferred:N0}");
Console.WriteLine($"  Duration: {uploadStats.DurationSeconds:F2} seconds");
Console.WriteLine($"  Throughput: {uploadStats.ThroughputMBps:F1} MB/s");
Console.WriteLine();

Console.WriteLine("Starting streaming download...");

await using var consumer = new SurgewaveConsumer<string, byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "streaming-demo-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe(topic);

using var downloadStream = new MemoryStream();
var chunksDownloaded = 0;
var bytesDownloaded = 0L;
var timeout = DateTime.UtcNow.AddSeconds(30);

while (chunksDownloaded < chunksUploaded && DateTime.UtcNow < timeout)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
    if (result != null)
    {
        await downloadStream.WriteAsync(result.Value);
        chunksDownloaded++;
        bytesDownloaded += result.Value.Length;
    }
}

Console.WriteLine();
Console.WriteLine("Download complete!");
Console.WriteLine($"  Received chunks: {chunksDownloaded:N0}");
Console.WriteLine($"  Received bytes: {bytesDownloaded:N0}");

// Verify integrity
var downloadedData = downloadStream.ToArray();
var dataMatches = downloadedData.SequenceEqual(testData);
Console.WriteLine($"  Data integrity: {(dataMatches ? "VERIFIED" : "FAILED")}");
Console.WriteLine();

Console.WriteLine("Demo completed!");

// Helper classes

public class StreamingProgress
{
    private readonly long _totalBytes;
    private readonly Stopwatch _stopwatch;
    private long _bytesTransferred;
    private int _lastPercentReported;

    public StreamingProgress(long totalBytes)
    {
        _totalBytes = totalBytes;
        _stopwatch = Stopwatch.StartNew();
    }

    public void Report(long bytesInChunk)
    {
        _bytesTransferred += bytesInChunk;
        var percent = (int)((_bytesTransferred * 100) / _totalBytes);

        if (percent >= _lastPercentReported + 10 || _bytesTransferred >= _totalBytes)
        {
            var elapsed = _stopwatch.Elapsed.TotalSeconds;
            var throughput = elapsed > 0 ? _bytesTransferred / elapsed / (1024 * 1024) : 0;

            var progressBar = new string('=', percent / 2.5 > 0 ? (int)(percent / 2.5) : 0);
            var remaining = new string(' ', 40 - progressBar.Length);

            Console.WriteLine($"  [{progressBar}{remaining}] {percent}% ({_bytesTransferred / (1024 * 1024)} MB / {_totalBytes / (1024 * 1024)} MB) - {throughput:F0} MB/s");

            _lastPercentReported = percent;
        }
    }

    public (long BytesTransferred, double DurationSeconds, double ThroughputMBps) GetStats()
    {
        _stopwatch.Stop();
        var duration = _stopwatch.Elapsed.TotalSeconds;
        var throughput = duration > 0 ? _bytesTransferred / duration / (1024 * 1024) : 0;
        return (_bytesTransferred, duration, throughput);
    }
}

public class DataStreamer
{
    private readonly int _chunkSize;
    private readonly int _maxPendingChunks;

    public DataStreamer(int chunkSize = 64 * 1024, int maxPendingChunks = 10)
    {
        _chunkSize = chunkSize;
        _maxPendingChunks = maxPendingChunks;
    }

    public async IAsyncEnumerable<byte[]> StreamFromSourceAsync(
        Stream source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var buffer = new byte[_chunkSize];
        int bytesRead;

        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            var chunk = new byte[bytesRead];
            Array.Copy(buffer, chunk, bytesRead);
            yield return chunk;
        }
    }

    public ChannelReader<byte[]> CreateBackpressuredChannel(
        IAsyncEnumerable<byte[]> source,
        CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(_maxPendingChunks)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true
        });

        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var chunk in source.WithCancellation(cancellationToken))
                {
                    await channel.Writer.WriteAsync(chunk, cancellationToken);
                }
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, cancellationToken);

        return channel.Reader;
    }
}
