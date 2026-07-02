// Unit 18 Project: Replay System - Time-Travel Consumer
// Build a message replay system with seek and checkpoint capabilities

using System.Diagnostics;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Abstractions;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Time-Travel Replay System ===\n");

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

// State
var currentOffset = 0L;
var isPaused = false;
var savedCheckpoint = (Checkpoint?)null;
var stats = new ReplayStats();

const string TopicName = "events";
const string CheckpointFile = "checkpoint.json";

// Event types for simulation
var eventTypes = new[] { "UserLogin", "PageView", "Purchase", "Logout", "Search", "AddToCart" };
var random = new Random(42);

// Start embedded broker
Console.WriteLine("[Surgewave] Starting embedded broker...");

await using var surgewave = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .Build()
    .StartAsync();

await using var client = await SurgewaveClient.Create(surgewave.BootstrapServers).BuildAsync();
var nativeClient = client.NativeClient!;

// Create topic
await nativeClient.Topics.CreateAsync(TopicName, partitions: 1);
Console.WriteLine($"[Surgewave] Created topic: {TopicName}");

// TODO: Step 1 - Produce historical events
Console.WriteLine("\n[Setup] Creating events topic with historical data...");

// TODO: Produce 1000 events spanning 24 hours
// - Start from a base date (e.g., 2024-01-01 00:00:00)
// - Each event is ~1.44 minutes apart (1440 minutes / 1000 events)
// - Vary event types randomly

var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
var totalEvents = 1000;

// TODO: Implement event production
// for (int i = 0; i < totalEvents; i++)
// {
//     var timestamp = baseDate.AddMinutes(i * 1.44);
//     var eventType = eventTypes[random.Next(eventTypes.Length)];
//     var payload = GeneratePayload(eventType);
//
//     var evt = new EventMessage(i, timestamp, eventType, payload);
//     var json = JsonSerializer.Serialize(evt);
//
//     await nativeClient.Messaging.SendAsync(TopicName, partition: 0, key: null, value: json);
// }

Console.WriteLine($"[Setup] Produced {totalEvents} events spanning 24 hours");
Console.WriteLine($"[Setup] Earliest: {baseDate:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"[Setup] Latest: {baseDate.AddMinutes((totalEvents - 1) * 1.44):yyyy-MM-dd HH:mm:ss}");

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

// TODO: Step 2 - Implement seek to offset
void SeekToOffset(long offset)
{
    // TODO: Validate offset is in valid range
    // TODO: Update currentOffset
    // TODO: Increment stats.TotalSeeks
    // TODO: Log the seek operation

    Console.WriteLine($"[Seek] Position set to offset {offset}");
}

// TODO: Step 3 - Implement seek to timestamp
void SeekToTimestamp(DateTime targetTime, List<EventMessage> messageCache)
{
    // TODO: Scan through messages to find closest timestamp
    // This could be optimized with binary search if timestamps are ordered

    Console.WriteLine($"[Seek] Searching for timestamp {targetTime:yyyy-MM-dd HH:mm:ss}...");

    // TODO: Find the message closest to targetTime
    // var closestOffset = 0L;
    // var closestDiff = TimeSpan.MaxValue;
    // foreach (var evt in messageCache)
    // {
    //     var diff = (evt.Timestamp - targetTime).Duration();
    //     if (diff < closestDiff)
    //     {
    //         closestDiff = diff;
    //         closestOffset = evt.Offset;
    //     }
    // }

    // TODO: After finding, set currentOffset and log result
    // Console.WriteLine($"[Seek] Found: offset {closestOffset} (closest match)");
}

// TODO: Step 4 - Implement pause/resume
void Pause()
{
    // TODO: Set isPaused = true
    // TODO: Stop the stats duration timer
    // TODO: Log current position

    Console.WriteLine($"[Replay] Consumption paused at offset {currentOffset}");
}

void Resume()
{
    // TODO: Set isPaused = false
    // TODO: Restart the stats duration timer
    // TODO: Log resume

    Console.WriteLine($"[Replay] Resuming from offset {currentOffset}");
}

// TODO: Step 5 - Implement checkpoint save/restore
void SaveCheckpoint(string description = "")
{
    // TODO: Create Checkpoint record with current position
    // TODO: Serialize to JSON and save to file
    // TODO: Store in savedCheckpoint variable

    // var checkpoint = new Checkpoint(0, currentOffset, DateTime.UtcNow, description);
    // File.WriteAllText(CheckpointFile, JsonSerializer.Serialize(checkpoint));
    // savedCheckpoint = checkpoint;

    Console.WriteLine($"[Checkpoint] Saved position: partition=0, offset={currentOffset}");
}

void RestoreCheckpoint()
{
    // TODO: Load checkpoint from file or savedCheckpoint
    // TODO: Seek to saved position

    if (savedCheckpoint != null)
    {
        Console.WriteLine($"[Checkpoint] Restored position: partition={savedCheckpoint.Partition}, offset={savedCheckpoint.Offset}");
        // SeekToOffset(savedCheckpoint.Offset);
    }
    else
    {
        Console.WriteLine("[Checkpoint] No checkpoint found");
    }
}

// TODO: Step 6 - Implement replay consumption
async Task ConsumeEvents(int count, CancellationToken ct)
{
    // TODO: Consume 'count' messages starting from currentOffset
    // TODO: Log each event
    // TODO: Update statistics

    // await using var consumer = client.CreateConsumer<string, string>();
    // consumer.Assign(TopicName, partition: 0, offset: currentOffset);
    //
    // var consumed = 0;
    // while (!ct.IsCancellationRequested && consumed < count)
    // {
    //     var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100), ct);
    //     if (result == null) break;
    //
    //     var evt = JsonSerializer.Deserialize<EventMessage>(result.Value!);
    //     if (evt != null)
    //     {
    //         Console.WriteLine($"[Event] offset={evt.Offset} | {evt.Timestamp:yyyy-MM-dd HH:mm:ss} | {evt.EventType} {evt.Payload}");
    //         stats.MessagesReplayed++;
    //         currentOffset = evt.Offset + 1;
    //         consumed++;
    //     }
    // }
}

// TODO: Step 7 - Run replay simulation
Console.WriteLine("\n[Replay] Starting replay consumer...");
Console.WriteLine($"[Replay] Current position: offset 0 (earliest)");

stats.Duration.Start();

// Simulate replay commands
Console.WriteLine("\n[Command] seek-time 2024-01-01T12:00:00");
// TODO: await SeekToTimestamp(new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc), messageCache);

Console.WriteLine("\n[Replay] Consuming 3 events...");
// TODO: await ConsumeEvents(3, CancellationToken.None);

Console.WriteLine("\n[Command] pause");
Pause();

Console.WriteLine("\n[Command] checkpoint save");
SaveCheckpoint("Investigation point");

Console.WriteLine("\n[Command] seek 100");
SeekToOffset(100);

Console.WriteLine("\n[Command] checkpoint restore");
RestoreCheckpoint();

Console.WriteLine("\n[Command] seek-end");
SeekToOffset(totalEvents - 1);
Console.WriteLine($"[Seek] Position set to latest: offset {totalEvents - 1}");

stats.Duration.Stop();

// TODO: Step 8 - Print replay summary
Console.WriteLine("\n[Stats] Replay Summary:");
Console.WriteLine($"  - Total seeks: {stats.TotalSeeks}");
Console.WriteLine($"  - Messages replayed: {stats.MessagesReplayed}");
Console.WriteLine($"  - Duration: {stats.Duration.Elapsed.TotalSeconds:F1}s");

if (stats.EarliestSeen.HasValue && stats.LatestSeen.HasValue)
{
    var timeRange = stats.LatestSeen.Value - stats.EarliestSeen.Value;
    Console.WriteLine($"  - Time range covered: {timeRange.TotalHours:F0} hours");
}

Console.WriteLine("\n=== Replay System Complete ===");
