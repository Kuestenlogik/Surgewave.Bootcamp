// Surgewave Bootcamp - Unit 18, Lesson 4: Custom Checkpointing
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 18.4 - Custom Checkpointing ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var topic = "orders";
var partition = 0;
var checkpointFile = Path.Combine(Path.GetTempPath(), "surgewave-checkpoint-demo.txt");
Console.WriteLine($"Checkpoint file: {checkpointFile}");

// Clean up from previous runs
if (File.Exists(checkpointFile))
{
    File.Delete(checkpointFile);
    Console.WriteLine("Cleaned up previous checkpoint file");
}

// Step 1 - File-based checkpoint store implementation
var checkpointStore = new FileCheckpointStore(checkpointFile);

// Step 2 - Create producer and produce test messages
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

Console.WriteLine("\nProducing 10 test messages...");
for (int i = 1; i <= 10; i++)
{
    await producer.ProduceAsync(topic, $"key-{i}", $"Order-{i}");
}
Console.WriteLine("Messages produced.");

// Step 3 - Create consumer
await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Assign(topic, partition, offset: 0);

// Step 4 - Load existing checkpoints
var checkpoints = await checkpointStore.LoadAsync();

if (checkpoints.Count > 0)
{
    Console.WriteLine("\nLoading existing checkpoints...");
    foreach (var ((t, p), offset) in checkpoints)
    {
        Console.WriteLine($"  Loaded checkpoint: {t}-{p} -> {offset}");
        consumer.Seek(t, p, offset);
    }
}
else
{
    Console.WriteLine("\nNo existing checkpoints, starting from beginning");
}

// Step 5 - First run - process messages and checkpoint, then "crash"
Console.WriteLine("\n--- Initial Run (will 'crash' after 5 messages) ---");
var processedInFirstRun = new List<string>();

for (int i = 0; i < 5; i++)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    Console.WriteLine($"Processing: {result.Value} at offset {result.Offset}");
    processedInFirstRun.Add(result.Value!);

    // Save checkpoint after processing
    // Store offset + 1 because that's the NEXT offset we want to consume
    await checkpointStore.SaveAsync(result.Topic, result.Partition, result.Offset + 1);
    Console.WriteLine($"  Checkpointed: {result.Topic}-{result.Partition} -> {result.Offset + 1}");
}

Console.WriteLine();
Console.WriteLine("*** SIMULATING CRASH! ***");
Console.WriteLine($"Processed in first run: {string.Join(", ", processedInFirstRun)}");

// Step 6 - Recovery run - reload checkpoints and continue
Console.WriteLine("\n--- Recovery Run ---");

// Reload checkpoints (simulating fresh start)
var recoveredCheckpoints = await checkpointStore.LoadAsync();
Console.WriteLine("Reloading checkpoints from file...");

foreach (var ((t, p), offset) in recoveredCheckpoints)
{
    Console.WriteLine($"  Restored checkpoint: {t}-{p} -> {offset}");
    consumer.Seek(t, p, offset);
}

Console.WriteLine("Resuming processing from checkpoint...");
Console.WriteLine();

// Continue processing from where we left off
var processedInSecondRun = new List<string>();
for (int i = 0; i < 5; i++)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) break;

    Console.WriteLine($"Processing: {result.Value} at offset {result.Offset}");
    processedInSecondRun.Add(result.Value!);

    await checkpointStore.SaveAsync(result.Topic, result.Partition, result.Offset + 1);
    Console.WriteLine($"  Checkpointed: {result.Topic}-{result.Partition} -> {result.Offset + 1}");
}

Console.WriteLine($"\nProcessed in second run: {string.Join(", ", processedInSecondRun)}");

// Step 7 - Verify no duplicates
Console.WriteLine("\n--- Verification ---");
var allProcessed = processedInFirstRun.Concat(processedInSecondRun).ToList();
var duplicates = allProcessed.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

Console.WriteLine($"Total messages processed: {allProcessed.Count}");
Console.WriteLine($"First run: {processedInFirstRun.Count} messages");
Console.WriteLine($"Second run: {processedInSecondRun.Count} messages");

if (duplicates.Count == 0)
{
    Console.WriteLine("\nNo duplicate processing! Recovery successful.");
}
else
{
    Console.WriteLine($"\nDuplicates found: {string.Join(", ", duplicates)}");
}

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

// File-based checkpoint store implementation
class FileCheckpointStore
{
    private readonly string _filePath;

    public FileCheckpointStore(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<Dictionary<(string Topic, int Partition), long>> LoadAsync()
    {
        var offsets = new Dictionary<(string, int), long>();

        if (!File.Exists(_filePath))
            return offsets;

        var lines = await File.ReadAllLinesAsync(_filePath);
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length == 3)
            {
                var t = parts[0];
                var p = int.Parse(parts[1]);
                var o = long.Parse(parts[2]);
                offsets[(t, p)] = o;
            }
        }

        return offsets;
    }

    public async Task SaveAsync(string topic, int partition, long offset)
    {
        var offsets = await LoadAsync();
        offsets[(topic, partition)] = offset;
        await SaveAllAsync(offsets);
    }

    public async Task SaveAllAsync(Dictionary<(string Topic, int Partition), long> offsets)
    {
        var lines = offsets.Select(kvp =>
            $"{kvp.Key.Topic}|{kvp.Key.Partition}|{kvp.Value}");
        await File.WriteAllLinesAsync(_filePath, lines);
    }
}
