// Surgewave Bootcamp - Unit 24 Project: Tiered Storage Simulator
// COMPLETED SOLUTION

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Tiered Storage Simulator ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;
Console.WriteLine($"[Setup] Surgewave broker started on port {runtime.Port}");

// Define retention policy
var policy = new TieredRetentionPolicy(
    HotRetention: TimeSpan.FromHours(1),
    WarmRetention: TimeSpan.FromDays(7),
    ColdRetention: TimeSpan.FromDays(30)
);

Console.WriteLine($"[Setup] Retention policy: Hot({policy.HotRetention.TotalHours}h) -> Warm({policy.WarmRetention.TotalDays}d) -> Cold({policy.ColdRetention.TotalDays}d) -> Delete");

// Create the tiered storage manager
var storageManager = new TieredStorageManager(policy);

// ============================================================================
// Phase 1: Ingest messages and create segments
// ============================================================================
Console.WriteLine("\n--- Phase 1: Ingesting Messages ---");

await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);

var now = DateTime.UtcNow;
var segmentData = new StringBuilder();

for (int i = 0; i < 1000; i++)
{
    var order = new { orderId = $"ORD-{i:D6}", amount = Random.Shared.Next(10, 1000), timestamp = now.ToString("O") };
    var json = JsonSerializer.Serialize(order);
    await producer.ProduceAsync("orders", $"key-{i}", json);
    segmentData.AppendLine(json);
}

var rawData = Encoding.UTF8.GetBytes(segmentData.ToString());
var segment0 = storageManager.CreateSegment("seg-0000", "orders", 0, 0, 999, 1000, rawData, now);
Console.WriteLine($"[Ingest] Produced 1000 messages to 'orders' topic");
Console.WriteLine($"[Ingest] Created segment {segment0.SegmentId} ({segment0.MessageCount} messages, {segment0.SizeBytes / 1024} KB) in Hot tier");

// ============================================================================
// Phase 2: Simulate time passage and tier migration
// ============================================================================
Console.WriteLine("\n--- Phase 2: Simulating Time Passage ---");

// Simulate T+1 hour
var t1 = now.AddHours(1);
Console.WriteLine($"\n[T+1 hour] Running tier migration...");
var actions1 = storageManager.RunMigration(t1);
foreach (var action in actions1)
{
    Console.WriteLine($"  [{action.Type}] {action.SegmentId}: {action.Description}");
}
PrintTierDistribution(storageManager);

// Simulate T+7 days
var t7 = now.AddDays(7);
Console.WriteLine($"\n[T+7 days] Running tier migration...");
var actions7 = storageManager.RunMigration(t7);
foreach (var action in actions7)
{
    Console.WriteLine($"  [{action.Type}] {action.SegmentId}: {action.Description}");
}
PrintTierDistribution(storageManager);

// Simulate T+30 days
var t30 = now.AddDays(30);
Console.WriteLine($"\n[T+30 days] Running tier migration...");
var actions30 = storageManager.RunMigration(t30);
foreach (var action in actions30)
{
    Console.WriteLine($"  [{action.Type}] {action.SegmentId}: {action.Description}");
}
PrintTierDistribution(storageManager);

// ============================================================================
// Phase 3: Multi-segment lifecycle
// ============================================================================
Console.WriteLine("\n--- Phase 3: Multi-Segment Lifecycle ---");

// Reset and create segments at various ages
var storageManager2 = new TieredStorageManager(policy);
var baseTime = DateTime.UtcNow;

Console.WriteLine("[Ingest] Creating 10 segments spanning 0-45 days");
for (int i = 0; i < 10; i++)
{
    var segAge = TimeSpan.FromDays(i * 5); // 0, 5, 10, 15, 20, 25, 30, 35, 40, 45 days
    var createdAt = baseTime - segAge;

    var data = new StringBuilder();
    for (int j = 0; j < 1000; j++)
    {
        data.AppendLine($"{{\"segId\":{i},\"msgId\":{j},\"ts\":\"{createdAt:O}\"}}");
    }

    var bytes = Encoding.UTF8.GetBytes(data.ToString());
    storageManager2.CreateSegment($"seg-{i:D4}", "orders", 0, i * 1000, (i + 1) * 1000 - 1, 1000, bytes, createdAt);
}

// Run migration to place segments in correct tiers
storageManager2.RunMigration(baseTime);

Console.WriteLine("\n  Tier Distribution:");
var tiers = new[] { StorageTier.Hot, StorageTier.Warm, StorageTier.Cold };
var expiredCount = 0;
foreach (var tier in tiers)
{
    var tierSegments = storageManager2.GetSegmentsInTier(tier);
    var totalKb = tierSegments.Sum(s => s.SizeBytes) / 1024;
    var cost = tierSegments.Sum(s => s.SizeBytes) / (1024m * 1024m * 1024m) * TieredStorageManager.GetTierCost(tier);
    Console.WriteLine($"    {tier,-6}: {tierSegments.Count} segments ({totalKb} KB)   - ${cost:F4}/month");
}

var deleted = storageManager2.GetDeletedCount();
Console.WriteLine($"    Expired: {deleted} segments (deleted)");

// ============================================================================
// Cost Report
// ============================================================================
Console.WriteLine("\n--- Cost Report ---");

var allSegments = storageManager2.GetAllActiveSegments();
var totalBytes = allSegments.Sum(s => s.SizeBytes);
var singleTierCostGb = totalBytes / (1024m * 1024m * 1024m) * TieredStorageManager.GetTierCost(StorageTier.Hot);
var tieredCost = 0m;
foreach (var tier in tiers)
{
    var tierSegs = storageManager2.GetSegmentsInTier(tier);
    tieredCost += tierSegs.Sum(s => s.SizeBytes) / (1024m * 1024m * 1024m) * TieredStorageManager.GetTierCost(tier);
}

Console.WriteLine($"  Single-tier cost (all Hot):  ${singleTierCostGb:F4}/month");
Console.WriteLine($"  Tiered storage cost:         ${tieredCost:F4}/month");

if (singleTierCostGb > 0)
{
    var savings = (1 - tieredCost / singleTierCostGb) * 100;
    Console.WriteLine($"  Savings:                     {savings:F1}%");
}

Console.WriteLine();
Console.WriteLine("=== Simulation Complete ===");

static void PrintTierDistribution(TieredStorageManager manager)
{
    var hot = manager.GetSegmentsInTier(StorageTier.Hot).Count;
    var warm = manager.GetSegmentsInTier(StorageTier.Warm).Count;
    var cold = manager.GetSegmentsInTier(StorageTier.Cold).Count;
    Console.WriteLine($"  Tier distribution: Hot={hot}, Warm={warm}, Cold={cold}");
}

// ============================================================================
// Supporting types
// ============================================================================

public enum StorageTier { Hot, Warm, Cold }

public record TieredRetentionPolicy(
    TimeSpan HotRetention,
    TimeSpan WarmRetention,
    TimeSpan ColdRetention
);

public class StorageSegment
{
    public string SegmentId { get; init; } = "";
    public string Topic { get; init; } = "";
    public int Partition { get; init; }
    public long BaseOffset { get; init; }
    public long EndOffset { get; init; }
    public int MessageCount { get; init; }
    public long SizeBytes { get; set; }
    public long OriginalSizeBytes { get; init; }
    public DateTime CreatedAt { get; init; }
    public StorageTier CurrentTier { get; set; }
    public bool IsCompressed { get; set; }
}

public record MigrationAction(string SegmentId, string Type, string Description);

public class TieredStorageManager
{
    private readonly TieredRetentionPolicy _policy;
    private readonly Dictionary<StorageTier, List<StorageSegment>> _tiers = new()
    {
        [StorageTier.Hot] = new(),
        [StorageTier.Warm] = new(),
        [StorageTier.Cold] = new()
    };
    private int _deletedCount;

    public TieredStorageManager(TieredRetentionPolicy policy)
    {
        _policy = policy;
    }

    public static decimal GetTierCost(StorageTier tier) => tier switch
    {
        StorageTier.Hot => 0.50m,
        StorageTier.Warm => 0.10m,
        StorageTier.Cold => 0.02m,
        _ => 0m
    };

    public StorageSegment CreateSegment(string segmentId, string topic, int partition,
        long baseOffset, long endOffset, int messageCount, byte[] data, DateTime createdAt)
    {
        var segment = new StorageSegment
        {
            SegmentId = segmentId,
            Topic = topic,
            Partition = partition,
            BaseOffset = baseOffset,
            EndOffset = endOffset,
            MessageCount = messageCount,
            SizeBytes = data.Length,
            OriginalSizeBytes = data.Length,
            CreatedAt = createdAt,
            CurrentTier = StorageTier.Hot,
            IsCompressed = false
        };

        _tiers[StorageTier.Hot].Add(segment);
        return segment;
    }

    public List<MigrationAction> RunMigration(DateTime currentTime)
    {
        var actions = new List<MigrationAction>();

        // Process tiers from coldest to hottest to avoid double-migration in one pass
        ProcessTier(StorageTier.Cold, currentTime, actions);
        ProcessTier(StorageTier.Warm, currentTime, actions);
        ProcessTier(StorageTier.Hot, currentTime, actions);

        return actions;
    }

    private void ProcessTier(StorageTier tier, DateTime currentTime, List<MigrationAction> actions)
    {
        var segments = _tiers[tier].ToList();

        foreach (var segment in segments)
        {
            var age = currentTime - segment.CreatedAt;

            switch (tier)
            {
                case StorageTier.Hot when age > _policy.HotRetention:
                    MoveSegment(segment, StorageTier.Hot, StorageTier.Warm);
                    var compressedSize = (long)(segment.SizeBytes * 0.25); // Simulate compression
                    segment.SizeBytes = compressedSize;
                    segment.IsCompressed = true;
                    actions.Add(new MigrationAction(segment.SegmentId, "Migrate",
                        $"Hot -> Warm (age: {FormatAge(age)}, compressed: {segment.OriginalSizeBytes / 1024} KB -> {compressedSize / 1024} KB)"));
                    break;

                case StorageTier.Warm when age > _policy.WarmRetention:
                    MoveSegment(segment, StorageTier.Warm, StorageTier.Cold);
                    if (!segment.IsCompressed)
                    {
                        segment.SizeBytes = (long)(segment.SizeBytes * 0.8);
                        segment.IsCompressed = true;
                    }
                    actions.Add(new MigrationAction(segment.SegmentId, "Migrate",
                        $"Warm -> Cold (age: {FormatAge(age)})"));
                    break;

                case StorageTier.Cold when age > _policy.ColdRetention:
                    _tiers[StorageTier.Cold].Remove(segment);
                    _deletedCount++;
                    actions.Add(new MigrationAction(segment.SegmentId, "Delete",
                        $"Expired (age: {FormatAge(age)}, retention: {_policy.ColdRetention.TotalDays}d)"));
                    break;
            }
        }
    }

    private void MoveSegment(StorageSegment segment, StorageTier from, StorageTier to)
    {
        _tiers[from].Remove(segment);
        segment.CurrentTier = to;
        _tiers[to].Add(segment);
    }

    private static string FormatAge(TimeSpan age)
    {
        if (age.TotalDays >= 1)
            return $"{age.TotalDays:F1}d";
        return $"{age.TotalHours:F1}h";
    }

    public List<StorageSegment> GetSegmentsInTier(StorageTier tier) => _tiers[tier];
    public List<StorageSegment> GetAllActiveSegments() => _tiers.Values.SelectMany(s => s).ToList();
    public int GetDeletedCount() => _deletedCount;
}
