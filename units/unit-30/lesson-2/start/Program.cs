// Surgewave Bootcamp - Unit 17, Lesson 2: Compression
//
// In this lesson, you'll learn to:
// - Understand different compression codecs (gzip, lz4, zstd)
// - Configure compression at producer level
// - Measure compression ratios and performance trade-offs
// - Decompress messages on the consumer side
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

// TODO: Add using statements
// Hint: System.Diagnostics, System.IO.Compression, System.Text, System.Text.Json
// Hint: Kuestenlogik.Surgewave.Broker, Kuestenlogik.Surgewave.Runtime, Kuestenlogik.Surgewave.Client, Kuestenlogik.Surgewave.Client.Consumer

Console.WriteLine("Compression Demo");
Console.WriteLine("================");
Console.WriteLine();

// TODO: Start embedded Surgewave broker
// Hint: Use SurgewaveRuntime.CreateBuilder()

// TODO: Generate test payload (~100KB of JSON data)
// Hint: Create a method that generates realistic JSON data

// TODO: Benchmark all codecs (None, GZip, LZ4, Zstd)
// - For each codec, compress the test data
// - Measure compression time and decompression time
// - Calculate compression ratio
// - Store results in a list

// TODO: Print results in a table format
// +----------+-------------+--------+--------------+--------------+
// | Codec    | Compressed  | Ratio  | Compress ms  | Decompress ms|
// +----------+-------------+--------+--------------+--------------+

// TODO: Find and print best results
// - Best compression ratio
// - Fastest compression
// - Fastest decompression

// TODO: Send compressed messages to Surgewave
// - Create producer with byte[] value type
// - Send messages with different codecs
// - Include codec type in message key

// TODO: Consume and verify decompression
// - Subscribe to topic
// - For each message, decompress using codec from key
// - Verify decompressed data matches original

// TODO: Implement CompressionCodec enum
// None, GZip, LZ4, Zstd

// TODO: Implement CompressionResult record
// - Codec, OriginalSize, CompressedSize
// - Ratio (calculated), CompressTimeMs, DecompressTimeMs

// TODO: Implement CompressionHelper static class
// - Compress(byte[] data, CompressionCodec codec) method
// - Decompress(byte[] data, CompressionCodec codec) method
// - Private methods for each codec
