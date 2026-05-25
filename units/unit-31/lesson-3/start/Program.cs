// Surgewave Bootcamp - Unit 21, Lesson 3: Persistent Deduplication
//
// In this lesson, you'll learn:
// - Implement file-based persistent deduplication
// - Use compacted topics for distributed state
// - Build checkpointing for crash recovery
// - Create hybrid in-memory + persistent stores
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 21.3 - Persistent Deduplication ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

// Create temp directory for persistent storage
var tempDir = Path.Combine(Path.GetTempPath(), $"surgewave-dedup-{Guid.NewGuid():N}");
Directory.CreateDirectory(tempDir);
Console.WriteLine($"Temp directory: {tempDir}");

// TODO: Step 1 - Implement a file-based persistent store
// public class FileBasedDeduplicationStore : IDisposable
// {
//     private readonly string _filePath;
//     private readonly HashSet<string> _processedKeys;
//     private readonly StreamWriter _appendWriter;
//     private readonly object _lock = new();
//
//     public FileBasedDeduplicationStore(string filePath)
//     {
//         _filePath = filePath;
//         _processedKeys = new HashSet<string>();
//
//         if (File.Exists(filePath))
//         {
//             foreach (var line in File.ReadLines(filePath))
//             {
//                 if (!string.IsNullOrWhiteSpace(line))
//                     _processedKeys.Add(line.Trim());
//             }
//         }
//
//         _appendWriter = new StreamWriter(filePath, append: true);
//     }
//
//     public bool IsDuplicate(string key)
//     {
//         lock (_lock) { return _processedKeys.Contains(key); }
//     }
//
//     public void MarkProcessed(string key)
//     {
//         lock (_lock)
//         {
//             if (_processedKeys.Add(key))
//             {
//                 _appendWriter.WriteLine(key);
//                 _appendWriter.Flush();
//             }
//         }
//     }
//
//     public int Count => _processedKeys.Count;
//
//     public void Dispose() => _appendWriter.Dispose();
// }

// TODO: Step 2 - Test file-based store
// Console.WriteLine("\n--- File-Based Persistent Store ---");
// var filePath = Path.Combine(tempDir, "dedup-keys.txt");
//
// using (var store = new FileBasedDeduplicationStore(filePath))
// {
//     for (int i = 0; i < 10; i++)
//     {
//         store.MarkProcessed($"order:{i:D3}");
//     }
//     Console.WriteLine($"Processed 10 messages, store contains {store.Count} keys");
// }
//
// Console.WriteLine("Simulating restart...");
// using (var recoveredStore = new FileBasedDeduplicationStore(filePath))
// {
//     Console.WriteLine($"Recovered {recoveredStore.Count} keys from file");
//     Console.WriteLine($"order:005 is duplicate: {recoveredStore.IsDuplicate("order:005")}");
// }

// TODO: Step 3 - Implement a checkpointed store
// public class CheckpointedDeduplicationStore
// {
//     private readonly string _checkpointPath;
//     private readonly HashSet<string> _processedKeys = new();
//     private long _lastCheckpointOffset;
//     private int _messagesSinceCheckpoint;
//     private readonly int _checkpointInterval;
//
//     public CheckpointedDeduplicationStore(string checkpointPath, int checkpointInterval = 10)
//     {
//         _checkpointPath = checkpointPath;
//         _checkpointInterval = checkpointInterval;
//         LoadCheckpoint();
//     }
//
//     private void LoadCheckpoint()
//     {
//         if (!File.Exists(_checkpointPath)) return;
//         var json = File.ReadAllText(_checkpointPath);
//         var checkpoint = JsonSerializer.Deserialize<Checkpoint>(json);
//         if (checkpoint != null)
//         {
//             _lastCheckpointOffset = checkpoint.Offset;
//             foreach (var key in checkpoint.ProcessedKeys)
//                 _processedKeys.Add(key);
//         }
//     }
//
//     public bool IsDuplicate(string key) => _processedKeys.Contains(key);
//
//     public void MarkProcessed(string key, long offset)
//     {
//         _processedKeys.Add(key);
//         _messagesSinceCheckpoint++;
//         if (_messagesSinceCheckpoint >= _checkpointInterval)
//         {
//             SaveCheckpoint(offset);
//             _messagesSinceCheckpoint = 0;
//         }
//     }
//
//     public void SaveCheckpoint(long offset)
//     {
//         var checkpoint = new Checkpoint
//         {
//             Offset = offset,
//             Timestamp = DateTime.UtcNow,
//             ProcessedKeys = _processedKeys.ToList()
//         };
//         File.WriteAllText(_checkpointPath, JsonSerializer.Serialize(checkpoint));
//         _lastCheckpointOffset = offset;
//         Console.WriteLine($"  Checkpoint saved at offset {offset}");
//     }
//
//     public long LastCheckpointOffset => _lastCheckpointOffset;
//     public int Count => _processedKeys.Count;
//
//     private class Checkpoint
//     {
//         public long Offset { get; set; }
//         public DateTime Timestamp { get; set; }
//         public List<string> ProcessedKeys { get; set; } = new();
//     }
// }

// TODO: Step 4 - Test checkpointed store
// Console.WriteLine("\n--- Checkpointed Store ---");
// var checkpointPath = Path.Combine(tempDir, "checkpoint.json");
//
// var checkpointStore = new CheckpointedDeduplicationStore(checkpointPath, checkpointInterval: 10);
// Console.WriteLine("Processing 50 messages with checkpoint interval of 10...");
//
// for (int i = 0; i < 50; i++)
// {
//     checkpointStore.MarkProcessed($"order:{i:D3}", i);
// }
//
// Console.WriteLine("Simulating crash before final checkpoint...");
// var recoveredCheckpointStore = new CheckpointedDeduplicationStore(checkpointPath, checkpointInterval: 10);
// Console.WriteLine($"Recovered store with {recoveredCheckpointStore.Count} keys");
// Console.WriteLine($"Resume processing from offset: {recoveredCheckpointStore.LastCheckpointOffset}");

// Cleanup
try
{
    Directory.Delete(tempDir, recursive: true);
}
catch
{
    // Ignore cleanup errors
}

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Persistent stores survive application restarts");
Console.WriteLine("2. File-based stores are simple but don't scale horizontally");
Console.WriteLine("3. Compacted topics provide distributed deduplication state");
Console.WriteLine("4. Checkpointing reduces recovery time and reprocessing");
Console.WriteLine("5. Hybrid stores combine fast lookups with durability");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
