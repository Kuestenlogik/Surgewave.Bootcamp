// Surgewave Bootcamp - Unit 17, Lesson 3: Chunking
//
// In this lesson, you'll learn to:
// - Implement message chunking for large payloads
// - Design chunk headers with sequence and metadata
// - Reassemble chunks in order on the consumer side
// - Handle missing chunks and out-of-order delivery
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

// TODO: Add using statements
// Hint: System.Security.Cryptography, System.Text, System.Text.Json
// Hint: Kuestenlogik.Surgewave.Broker, Kuestenlogik.Surgewave.Runtime, Kuestenlogik.Surgewave.Client, Kuestenlogik.Surgewave.Client.Consumer

Console.WriteLine("Chunking Demo");
Console.WriteLine("=============");
Console.WriteLine();

// TODO: Start embedded Surgewave broker
// Hint: Use SurgewaveRuntime.CreateBuilder()

// TODO: Create a large message (5 MB of random data)
// Hint: Use Random.Shared.NextBytes() to fill a byte array

// TODO: Create a MessageChunker with 1 MB chunk size
// Hint: var chunker = new MessageChunker(1024 * 1024);

// TODO: Chunk the message and print chunk info
// Hint: var chunks = chunker.Chunk(originalData).ToList();

// TODO: Send all chunks to Surgewave
// - Create producer with byte[] value type
// - Use message ID as key (ensures same partition)
// - Serialize each chunk before sending

// TODO: Consume chunks and reassemble
// - Create consumer
// - Create MessageReassembler
// - For each consumed message, deserialize and add to reassembler
// - When complete, get the reassembled data

// TODO: Verify the reassembled message matches original
// Hint: Use SequenceEqual() to compare byte arrays

// TODO: Implement ChunkHeader record
// - MessageId: Guid
// - ChunkIndex: int (0-based)
// - TotalChunks: int
// - TotalSize: long
// - ChunkSize: int
// - Checksum: string
// - Timestamp: DateTime

// TODO: Implement ChunkMessage record
// - Header: ChunkHeader
// - Data: byte[]
// - Serialize() method: returns byte[] with header length prefix
// - Deserialize(byte[]) static method: parses bytes back to ChunkMessage

// TODO: Implement MessageChunker class
// - Constructor takes chunkSize (default 1 MB)
// - Chunk(byte[] data) method returns IEnumerable<ChunkMessage>
// - ComputeChecksum(byte[] data) private method

// TODO: Implement MessageReassembler class
// - AddChunk(ChunkMessage) method returns (bool IsComplete, byte[]? Data)
// - Track pending messages by MessageId
// - Reassemble when all chunks received
// - Verify checksum after reassembly
