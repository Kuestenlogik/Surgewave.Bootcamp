// Surgewave Bootcamp - Unit 17, Lesson 2: Compression
// COMPLETED SOLUTION

using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("Compression Demo");
Console.WriteLine("================");
Console.WriteLine();

// Start broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");

// Generate test payload
var testPayload = GenerateTestPayload(100_000);
var testData = Encoding.UTF8.GetBytes(testPayload);
Console.WriteLine($"Test payload: {testData.Length:N0} bytes of JSON data");
Console.WriteLine();

// Benchmark all codecs
var codecs = new[] { CompressionCodec.None, CompressionCodec.GZip, CompressionCodec.LZ4, CompressionCodec.Zstd };
var results = new List<CompressionResult>();

Console.WriteLine("Compression Results:");
Console.WriteLine("+----------+-------------+--------+--------------+--------------+");
Console.WriteLine("| Codec    | Compressed  | Ratio  | Compress ms  | Decompress ms|");
Console.WriteLine("+----------+-------------+--------+--------------+--------------+");

foreach (var codec in codecs)
{
    var result = BenchmarkCodec(testData, codec);
    results.Add(result);

    Console.WriteLine($"| {codec,-8} | {result.CompressedSize,11:N0} | {result.Ratio,5:F1}x  | {result.CompressTimeMs,12:F1} | {result.DecompressTimeMs,12:F1} |");
}

Console.WriteLine("+----------+-------------+--------+--------------+--------------+");
Console.WriteLine();

var bestRatio = results.Where(r => r.Codec != CompressionCodec.None).OrderByDescending(r => r.Ratio).First();
var fastestCompress = results.Where(r => r.Codec != CompressionCodec.None).OrderBy(r => r.CompressTimeMs).First();
var fastestDecompress = results.Where(r => r.Codec != CompressionCodec.None).OrderBy(r => r.DecompressTimeMs).First();

Console.WriteLine($"Best compression ratio: {bestRatio.Codec} ({bestRatio.Ratio:F1}x)");
Console.WriteLine($"Fastest compression: {fastestCompress.Codec} ({fastestCompress.CompressTimeMs:F1}ms)");
Console.WriteLine($"Fastest decompression: {fastestDecompress.Codec} ({fastestDecompress.DecompressTimeMs:F1}ms)");
Console.WriteLine();

Console.WriteLine("Sending compressed messages to Surgewave...");

await using var producer = new SurgewaveProducer<string, byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

await using var consumer = new SurgewaveConsumer<string, byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "compression-demo-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

const string topic = "compressed-messages";

// Send messages with different compressions
foreach (var codec in codecs)
{
    var compressed = CompressionHelper.Compress(testData, codec);
    var key = $"codec-{codec}";
    await producer.ProduceAsync(topic, key, compressed);
}

// Consume and decompress
consumer.Subscribe(topic);
var allMatch = true;
var receivedCount = 0;

var timeout = DateTime.UtcNow.AddSeconds(5);
while (receivedCount < codecs.Length && DateTime.UtcNow < timeout)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
    if (result != null)
    {
        var codecName = result.Key!.Replace("codec-", "");
        var codec = Enum.Parse<CompressionCodec>(codecName);
        var decompressed = CompressionHelper.Decompress(result.Value, codec);

        if (!decompressed.SequenceEqual(testData))
        {
            allMatch = false;
        }
        receivedCount++;
    }
}

Console.WriteLine("All messages sent and consumed successfully!");
Console.WriteLine($"Decompressed payloads match originals: {allMatch}");
Console.WriteLine();
Console.WriteLine("Demo completed!");

// Helper methods
static string GenerateTestPayload(int approximateSize)
{
    var items = new List<object>();

    var singleItem = new
    {
        id = Guid.NewGuid(),
        timestamp = DateTime.UtcNow,
        name = "Test Item with a reasonably long name for compression testing",
        description = "This is a longer description that contains repeated patterns which compress well. " +
                     "Compression algorithms work by finding repeated sequences and replacing them with shorter references.",
        values = new[] { 1.23, 4.56, 7.89, 10.11, 12.13 },
        tags = new[] { "tag1", "tag2", "tag3", "important", "compression-test" },
        metadata = new Dictionary<string, string>
        {
            ["source"] = "compression-demo",
            ["version"] = "1.0",
            ["environment"] = "development"
        }
    };

    var singleItemJson = JsonSerializer.Serialize(singleItem);
    var itemsNeeded = approximateSize / singleItemJson.Length + 1;

    for (int i = 0; i < itemsNeeded; i++)
    {
        items.Add(new
        {
            id = Guid.NewGuid(),
            timestamp = DateTime.UtcNow.AddSeconds(i),
            name = $"Test Item {i} with a reasonably long name for compression testing",
            description = "This is a longer description that contains repeated patterns which compress well. " +
                         "Compression algorithms work by finding repeated sequences and replacing them with shorter references.",
            values = new double[] { 1.23 + i, 4.56 + i, 7.89 + i, 10.11 + i, 12.13 + i },
            tags = new string[] { "tag1", "tag2", "tag3", "important", "compression-test" },
            metadata = new Dictionary<string, string>
            {
                ["source"] = "compression-demo",
                ["version"] = "1.0",
                ["environment"] = "development"
            }
        });
    }

    return JsonSerializer.Serialize(items);
}

static CompressionResult BenchmarkCodec(byte[] data, CompressionCodec codec)
{
    // Warm up
    var compressed = CompressionHelper.Compress(data, codec);
    CompressionHelper.Decompress(compressed, codec);

    // Benchmark compression
    var sw = Stopwatch.StartNew();
    const int iterations = 10;
    for (int i = 0; i < iterations; i++)
    {
        compressed = CompressionHelper.Compress(data, codec);
    }
    sw.Stop();
    var compressTime = sw.Elapsed.TotalMilliseconds / iterations;

    // Benchmark decompression
    sw.Restart();
    for (int i = 0; i < iterations; i++)
    {
        CompressionHelper.Decompress(compressed, codec);
    }
    sw.Stop();
    var decompressTime = sw.Elapsed.TotalMilliseconds / iterations;

    return new CompressionResult
    {
        Codec = codec,
        OriginalSize = data.Length,
        CompressedSize = compressed.Length,
        CompressTimeMs = compressTime,
        DecompressTimeMs = decompressTime
    };
}

// Types
public enum CompressionCodec
{
    None,
    GZip,
    LZ4,
    Zstd
}

public record CompressionResult
{
    public CompressionCodec Codec { get; init; }
    public int OriginalSize { get; init; }
    public int CompressedSize { get; init; }
    public double Ratio => (double)OriginalSize / CompressedSize;
    public double CompressTimeMs { get; init; }
    public double DecompressTimeMs { get; init; }
}

public static class CompressionHelper
{
    public static byte[] Compress(byte[] data, CompressionCodec codec)
    {
        return codec switch
        {
            CompressionCodec.None => data,
            CompressionCodec.GZip => CompressGZip(data),
            CompressionCodec.LZ4 => CompressLZ4(data),
            CompressionCodec.Zstd => CompressZstd(data),
            _ => throw new ArgumentException($"Unknown codec: {codec}")
        };
    }

    public static byte[] Decompress(byte[] data, CompressionCodec codec)
    {
        return codec switch
        {
            CompressionCodec.None => data,
            CompressionCodec.GZip => DecompressGZip(data),
            CompressionCodec.LZ4 => DecompressLZ4(data),
            CompressionCodec.Zstd => DecompressZstd(data),
            _ => throw new ArgumentException($"Unknown codec: {codec}")
        };
    }

    private static byte[] CompressGZip(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] DecompressGZip(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    // LZ4 simulation using DeflateStream for demo purposes
    // In production, use K4os.Compression.LZ4 NuGet package
    private static byte[] CompressLZ4(byte[] data)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Fastest))
        {
            deflate.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] DecompressLZ4(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }

    // Zstd simulation using Brotli for demo purposes
    // In production, use ZstdSharp.Port NuGet package
    private static byte[] CompressZstd(byte[] data)
    {
        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, CompressionLevel.Optimal))
        {
            brotli.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] DecompressZstd(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var brotli = new BrotliStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        brotli.CopyTo(output);
        return output.ToArray();
    }
}
