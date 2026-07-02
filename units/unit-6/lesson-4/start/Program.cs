// Surgewave Bootcamp - Unit 18, Lesson 4: Custom Checkpointing
//
// In this lesson, you'll learn:
// - Store offsets in external systems (file-based)
// - Recover from failures using custom checkpoints
// - Implement exactly-once patterns with external state
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 18.4 - Custom Checkpointing ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

// Variables used in TODO sections below
#pragma warning disable CS0219
var topic = "orders";
var partition = 0;
#pragma warning restore CS0219
var checkpointFile = Path.Combine(Path.GetTempPath(), "surgewave-checkpoint-demo.txt");
Console.WriteLine($"Checkpoint file: {checkpointFile}");

// Clean up from previous runs
if (File.Exists(checkpointFile))
{
    File.Delete(checkpointFile);
    Console.WriteLine("Cleaned up previous checkpoint file");
}

// TODO: Step 1 - Define and implement a file-based checkpoint store
// class FileCheckpointStore
// {
//     private readonly string _filePath;
//
//     public FileCheckpointStore(string filePath)
//     {
//         _filePath = filePath;
//     }
//
//     public async Task<Dictionary<(string Topic, int Partition), long>> LoadAsync()
//     {
//         var offsets = new Dictionary<(string, int), long>();
//
//         if (!File.Exists(_filePath))
//             return offsets;
//
//         var lines = await File.ReadAllLinesAsync(_filePath);
//         foreach (var line in lines)
//         {
//             var parts = line.Split('|');
//             if (parts.Length == 3)
//             {
//                 var t = parts[0];
//                 var p = int.Parse(parts[1]);
//                 var o = long.Parse(parts[2]);
//                 offsets[(t, p)] = o;
//             }
//         }
//
//         return offsets;
//     }
//
//     public async Task SaveAsync(string topic, int partition, long offset)
//     {
//         var offsets = await LoadAsync();
//         offsets[(topic, partition)] = offset;
//         await SaveAllAsync(offsets);
//     }
//
//     public async Task SaveAllAsync(Dictionary<(string Topic, int Partition), long> offsets)
//     {
//         var lines = offsets.Select(kvp =>
//             $"{kvp.Key.Topic}|{kvp.Key.Partition}|{kvp.Value}");
//         await File.WriteAllLinesAsync(_filePath, lines);
//     }
// }

// TODO: Step 2 - Create producer and produce test messages
// await using var producer = new SurgewaveProducer<string, string>(options =>
// {
//     options.BootstrapServers = $"localhost:{runtime.Port}";
// });
//
// Console.WriteLine("\nProducing 10 test messages...");
// for (int i = 1; i <= 10; i++)
// {
//     await producer.ProduceAsync(topic, $"key-{i}", $"Order-{i}");
// }
// Console.WriteLine("Messages produced.");

// TODO: Step 3 - Create consumer
// await using var consumer = new SurgewaveConsumer<string, string>(options =>
// {
//     options.BootstrapServers = $"localhost:{runtime.Port}";
//     options.AutoOffsetReset = AutoOffsetReset.Earliest;
// });
//
// consumer.Assign(topic, partition, offset: 0);

// TODO: Step 4 - Create checkpoint store and load existing checkpoints
// var checkpointStore = new FileCheckpointStore(checkpointFile);
// var checkpoints = await checkpointStore.LoadAsync();
//
// if (checkpoints.Count > 0)
// {
//     Console.WriteLine("\nLoading existing checkpoints...");
//     foreach (var ((t, p), offset) in checkpoints)
//     {
//         Console.WriteLine($"  Loaded checkpoint: {t}-{p} -> {offset}");
//         consumer.Seek(t, p, offset);
//     }
// }
// else
// {
//     Console.WriteLine("\nNo existing checkpoints, starting from beginning");
// }

// TODO: Step 5 - First run - process messages and checkpoint, then "crash"
// Console.WriteLine("\n--- Initial Run (will 'crash' after 5 messages) ---");
// var processedInFirstRun = new List<string>();
//
// for (int i = 0; i < 5; i++)
// {
//     var result = await consumer.ConsumeAsync();
//     if (result == null) continue;
//
//     Console.WriteLine($"Processing: {result.Value} at offset {result.Offset}");
//     processedInFirstRun.Add(result.Value!);
//
//     // Save checkpoint after processing
//     await checkpointStore.SaveAsync(result.Topic, result.Partition, result.Offset + 1);
//     Console.WriteLine($"  Checkpointed: {result.Topic}-{result.Partition} -> {result.Offset + 1}");
// }
//
// Console.WriteLine("\nSIMULATING CRASH!");
// Console.WriteLine($"Processed in first run: {string.Join(", ", processedInFirstRun)}");

// TODO: Step 6 - Recovery run - reload checkpoints and continue
// Console.WriteLine("\n--- Recovery Run ---");
//
// // Reload checkpoints (simulating fresh start)
// var recoveredCheckpoints = await checkpointStore.LoadAsync();
// foreach (var ((t, p), offset) in recoveredCheckpoints)
// {
//     Console.WriteLine($"Loaded checkpoint: {t}-{p} -> {offset}");
//     consumer.Seek(t, p, offset);
// }
//
// Console.WriteLine("Seeking to saved offset...");
//
// // Continue processing
// var processedInSecondRun = new List<string>();
// for (int i = 0; i < 5; i++)
// {
//     var result = await consumer.ConsumeAsync();
//     if (result == null) break;
//
//     Console.WriteLine($"Processing: {result.Value} at offset {result.Offset}");
//     processedInSecondRun.Add(result.Value!);
//
//     await checkpointStore.SaveAsync(result.Topic, result.Partition, result.Offset + 1);
// }
//
// Console.WriteLine($"\nProcessed in second run: {string.Join(", ", processedInSecondRun)}");

// TODO: Step 7 - Verify no duplicates
// var allProcessed = processedInFirstRun.Concat(processedInSecondRun).ToList();
// var duplicates = allProcessed.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);
//
// if (!duplicates.Any())
// {
//     Console.WriteLine("\nNo duplicate processing! Recovery successful.");
// }
// else
// {
//     Console.WriteLine($"\nDuplicates found: {string.Join(", ", duplicates)}");
// }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Custom checkpoints enable exactly-once with external systems");
Console.WriteLine("2. Store offset + 1 (next offset to consume)");
Console.WriteLine("3. Checkpoint atomically with your data for true exactly-once");
Console.WriteLine("4. Load checkpoints on startup and seek to saved positions");
Console.WriteLine("5. Consider batch checkpointing for performance");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Clean up
if (File.Exists(checkpointFile))
{
    File.Delete(checkpointFile);
}
