// Unit 18 Project: Replay System - Time-Travel Consumer - COMPLETED
// A message replay system with seek and checkpoint capabilities

using System.Diagnostics;
using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Abstractions;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Time-Travel Replay System ===\n");

// State
var currentOffset = 0L;
var isPaused = false;
var savedCheckpoint = (Checkpoint?)null;
var stats = new ReplayStats();
var messageCache = new List<EventMessage>();

const string TopicName = "events";
const string CheckpointFile = "checkpoint.json";

// Event types for simulation
var eventTypes = new[] { "UserLogin", "PageView", "Purchase", "Logout", "Search", "AddToCart" };
var random = new Random(42);

// Start embedded broker
Console.WriteLine("[Surgewave] Starting embedded broker...");

await using var surgewave = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .Build()
    .StartAsync();

await using var client = await SurgewaveClient.Create(surgewave.BootstrapServers).BuildAsync();
var nativeClient = client.NativeClient!;

// Create topic
await nativeClient.Topics.CreateAsync(TopicName, partitions: 1);
Console.WriteLine($"[Surgewave] Created topic: {TopicName}");

// Helper to generate payload based on event type
string GeneratePayload(string eventType)
{
    return eventType switch
    {
        "UserLogin" => JsonSerializer.Serialize(new { user = $"user{random.Next(100)}" }),
        "PageView" => JsonSerializer.Serialize(new { page = $"/{new[] { "home", "dashboard", "profile", "settings" }[random.Next(4)]}" }),
        "Purchase" => JsonSerializer.Serialize(new { amount = Math.Round(random.NextDouble() * 500, 2) }),
        "Logout" => JsonSerializer.Serialize(new { user = $"user{random.Next(100)}" }),
        "Search" => JsonSerializer.Serialize(new { query = $"term{random.Next(50)}" }),
        "AddToCart" => JsonSerializer.Serialize(new { productId = $"SKU-{random.Next(1000)}" }),
        _ => "{}"
    };
}

// Step 1: Produce historical events
Console.WriteLine("\n[Setup] Creating events topic with historical data...");

var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
var totalEvents = 1000;

for (int i = 0; i < totalEvents; i++)
{
    var timestamp = baseDate.AddMinutes(i * 1.44);
    var eventType = eventTypes[random.Next(eventTypes.Length)];
    var payload = GeneratePayload(eventType);

    var evt = new EventMessage(i, timestamp, eventType, payload);
    messageCache.Add(evt); // Cache for seeking

    var json = JsonSerializer.Serialize(evt);
    await nativeClient.Messaging.SendAsync(TopicName, partition: 0, key: null, value: json);
}

Console.WriteLine($"[Setup] Produced {totalEvents} events spanning 24 hours");
Console.WriteLine($"[Setup] Earliest: {baseDate:yyyy-MM-dd HH:mm:ss}, Latest: {baseDate.AddMinutes((totalEvents - 1) * 1.44):yyyy-MM-dd HH:mm:ss}");

// Step 2: Implement seek to offset
void SeekToOffset(long offset)
{
    if (offset < 0 || offset >= totalEvents)
    {
        Console.WriteLine($"[Seek] Invalid offset {offset}. Valid range: 0-{totalEvents - 1}");
        return;
    }

    currentOffset = offset;
    stats.TotalSeeks++;
    Console.WriteLine($"[Seek] Position set to offset {offset}");
}

// Step 3: Implement seek to timestamp
void SeekToTimestamp(DateTime targetTime)
{
    Console.WriteLine($"[Seek] Searching for timestamp {targetTime:yyyy-MM-dd HH:mm:ss}...");

    var closestOffset = 0L;
    var closestDiff = TimeSpan.MaxValue;

    foreach (var evt in messageCache)
    {
        var diff = (evt.Timestamp - targetTime).Duration();
        if (diff < closestDiff)
        {
            closestDiff = diff;
            closestOffset = evt.Offset;
        }
    }

    currentOffset = closestOffset;
    stats.TotalSeeks++;
    Console.WriteLine($"[Seek] Found: offset {closestOffset} (closest match)");
}

// Step 4: Implement pause/resume
void Pause()
{
    isPaused = true;
    stats.Duration.Stop();

    var rate = stats.Duration.Elapsed.TotalSeconds > 0
        ? stats.MessagesReplayed / stats.Duration.Elapsed.TotalSeconds
        : 0;

    Console.WriteLine($"[Replay] Consumption paused at offset {currentOffset}");
    Console.WriteLine($"[Stats] Processed: {stats.MessagesReplayed} messages, Duration: {stats.Duration.Elapsed.TotalSeconds:F0}s, Rate: {rate:F1} msg/s");
}

void Resume()
{
    isPaused = false;
    stats.Duration.Start();
    Console.WriteLine($"[Replay] Resuming from offset {currentOffset}");
}

// Step 5: Implement checkpoint save/restore
void SaveCheckpoint(string description = "")
{
    var checkpoint = new Checkpoint(0, currentOffset, DateTime.UtcNow, description);

    try
    {
        File.WriteAllText(CheckpointFile, JsonSerializer.Serialize(checkpoint, new JsonSerializerOptions { WriteIndented = true }));
    }
    catch
    {
        // Ignore file write errors in demo
    }

    savedCheckpoint = checkpoint;
    Console.WriteLine($"[Checkpoint] Saved position: {{ partition: {checkpoint.Partition}, offset: {checkpoint.Offset}, timestamp: {checkpoint.SavedAt:o} }}");
}

void RestoreCheckpoint()
{
    // Try to load from file first
    if (File.Exists(CheckpointFile))
    {
        try
        {
            var json = File.ReadAllText(CheckpointFile);
            savedCheckpoint = JsonSerializer.Deserialize<Checkpoint>(json);
        }
        catch
        {
            // Ignore read errors
        }
    }

    if (savedCheckpoint != null)
    {
        Console.WriteLine($"[Checkpoint] Restored position: {{ partition: {savedCheckpoint.Partition}, offset: {savedCheckpoint.Offset} }}");
        SeekToOffset(savedCheckpoint.Offset);
    }
    else
    {
        Console.WriteLine("[Checkpoint] No checkpoint found");
    }
}

// Step 6: Consume events from current position
async Task ConsumeEvents(int count, CancellationToken ct)
{
    var consumed = 0;

    await using var consumer = client.CreateConsumer<string, string>();
    consumer.Assign(TopicName, partition: 0, offset: 0);

    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    cts.CancelAfter(TimeSpan.FromSeconds(5));

    try
    {
        while (!cts.Token.IsCancellationRequested && consumed < count)
        {
            if (isPaused) break;

            var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100), cts.Token);
            if (result == null) continue;

            var evt = JsonSerializer.Deserialize<EventMessage>(result.Value!);
            if (evt == null) continue;

            // Skip until we reach current offset
            if (evt.Offset < currentOffset) continue;

            Console.WriteLine($"[Event] offset={evt.Offset} | {evt.Timestamp:yyyy-MM-dd HH:mm:ss} | {evt.EventType} {evt.Payload}");

            stats.MessagesReplayed++;
            currentOffset = evt.Offset + 1;

            // Track time range
            if (!stats.EarliestSeen.HasValue || evt.Timestamp < stats.EarliestSeen)
                stats.EarliestSeen = evt.Timestamp;
            if (!stats.LatestSeen.HasValue || evt.Timestamp > stats.LatestSeen)
                stats.LatestSeen = evt.Timestamp;

            consumed++;
        }
    }
    catch (OperationCanceledException)
    {
        // Expected
    }
}

// Step 7: Run replay simulation
Console.WriteLine("\n[Replay] Starting replay consumer...");
Console.WriteLine($"[Replay] Current position: offset 0 (earliest)");

stats.Duration.Start();

// Seek to noon
Console.WriteLine("\n[Command] seek-time 2024-01-01T12:00:00");
SeekToTimestamp(new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc));

Console.WriteLine($"\n[Replay] Consuming from offset {currentOffset}...");
await ConsumeEvents(3, CancellationToken.None);

Console.WriteLine("\n[Command] pause");
Pause();

Console.WriteLine("\n[Command] checkpoint save");
SaveCheckpoint("Investigation point");

Console.WriteLine("\n[Command] seek 100");
SeekToOffset(100);

Console.WriteLine($"\n[Replay] Consuming from offset {currentOffset}...");
Resume();
await ConsumeEvents(1, CancellationToken.None);

Console.WriteLine("\n[Command] checkpoint restore");
RestoreCheckpoint();

Console.WriteLine($"\n[Replay] Resuming from checkpoint...");
await ConsumeEvents(2, CancellationToken.None);

Console.WriteLine("\n[Command] seek-end");
SeekToOffset(totalEvents - 1);
Console.WriteLine($"[Seek] Position set to latest: offset {totalEvents - 1}");

stats.Duration.Stop();

// Step 8: Print replay summary
Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("[Stats] Replay Summary:");
Console.WriteLine($"  - Total seeks: {stats.TotalSeeks}");
Console.WriteLine($"  - Messages replayed: {stats.MessagesReplayed}");
Console.WriteLine($"  - Checkpoints saved: {(savedCheckpoint != null ? 1 : 0)}");

if (stats.EarliestSeen.HasValue && stats.LatestSeen.HasValue)
{
    var timeRange = stats.LatestSeen.Value - stats.EarliestSeen.Value;
    Console.WriteLine($"  - Time range covered: {timeRange.TotalHours:F0} hours");
}

Console.WriteLine(new string('=', 50));

// Cleanup checkpoint file
try { File.Delete(CheckpointFile); } catch { }

Console.WriteLine("\n=== Replay System Complete ===");

// Event message format
public record EventMessage(
    long Offset,
    DateTime Timestamp,
    string EventType,
    string Payload
);

// Checkpoint for position persistence
public record Checkpoint(
    int Partition,
    long Offset,
    DateTime SavedAt,
    string Description
);

// Replay statistics
public class ReplayStats
{
    public int TotalSeeks;
    public long MessagesReplayed;
    public Stopwatch Duration = new();
    public DateTime? EarliestSeen;
    public DateTime? LatestSeen;
}