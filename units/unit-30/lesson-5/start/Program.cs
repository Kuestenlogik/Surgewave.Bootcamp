// Surgewave Bootcamp - Unit 17, Lesson 5: Streaming Large Data
//
// In this lesson, you'll learn to:
// - Implement streaming uploads for large data
// - Handle backpressure in data pipelines
// - Process data in a memory-efficient manner
// - Use async streams for large data transfer
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

// TODO: Add using statements
// Hint: System.Diagnostics, System.Runtime.CompilerServices, System.Threading.Channels
// Hint: Kuestenlogik.Surgewave.Broker, Kuestenlogik.Surgewave.Runtime, Kuestenlogik.Surgewave.Client, Kuestenlogik.Surgewave.Client.Consumer

Console.WriteLine("Streaming Large Data Demo");
Console.WriteLine("=========================");
Console.WriteLine();

// TODO: Start embedded Surgewave broker
// Hint: Use SurgewaveRuntime.CreateBuilder()

// TODO: Create test data (50 MB)
// Hint: Use Random.Shared.NextBytes() to fill a byte array

// TODO: Implement streaming upload
// - Create a DataStreamer instance
// - Create a StreamingProgress tracker
// - Use MemoryStream as source
// - Get async enumerable of chunks
// - Create backpressured channel
// - Send each chunk to Surgewave
// - Report progress

// TODO: Print upload statistics
// - Total chunks
// - Total bytes
// - Duration
// - Throughput

// TODO: Implement streaming download
// - Create consumer
// - Subscribe to topic
// - Read all chunks into MemoryStream
// - Track chunks and bytes received

// TODO: Verify data integrity
// - Compare downloaded data to original
// - Print result

// TODO: Implement StreamingProgress class
// - Constructor takes totalBytes
// - Report(bytesInChunk) method updates progress
// - Print progress bar every 10%
// - GetStats() returns final statistics

// TODO: Implement DataStreamer class
// - Constructor takes chunkSize and maxPendingChunks
// - StreamFromSourceAsync(Stream) returns IAsyncEnumerable<byte[]>
// - CreateBackpressuredChannel creates bounded channel with backpressure
