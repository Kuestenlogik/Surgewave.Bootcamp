// Surgewave Bootcamp - Unit 24, Lesson 5: Retention and Lifecycle Policies
// COMPLETED SOLUTION

using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 24.5 - Retention and Lifecycle Policies ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var now = DateTime.UtcNow;

// Step 1 & 2 - Define and create retention policy
var ordersPolicy = new RetentionPolicy(
    Name: "Standard Orders",
    TopicPattern: "orders*",
    TierRules: new List<TierRule>
    {
        new(StorageTier.Hot, TimeSpan.FromHours(1), StorageTier.Warm),
        new(StorageTier.Warm, TimeSpan.FromDays(7), StorageTier.Cold),
        new(StorageTier.Cold, TimeSpan.FromDays(30), StorageTier.Archive)
    },
    TotalRetention: TimeSpan.FromDays(90)
);

Console.WriteLine($"\n--- Retention Policy: {ordersPolicy.Name} ---");
Console.WriteLine($"  Topic Pattern: {ordersPolicy.TopicPattern}");
Console.WriteLine($"  Total Retention: {ordersPolicy.TotalRetention.TotalDays} days");
Console.WriteLine("  Tier Rules:");
foreach (var rule in ordersPolicy.TierRules)
{
    Console.WriteLine($"    {rule.Tier,-8} -> {rule.NextTier,-8} after {rule.MaxAge.TotalDays:F2} days");
}

// Step 3 - Create simulated segments at various ages
var segments = new List<SegmentInfo>();
for (int i = 0; i < 20; i++)
{
    var ageHours = i * 24 * 5; // Each segment is 5 days apart
    segments.Add(new SegmentInfo(
        SegmentId: $"seg-{i:D4}",
        Topic: "orders",
        Partition: 0,
        BaseOffset: i * 10000,
        EndOffset: (i + 1) * 10000 - 1,
        MessageCount: 10000,
        SizeBytes: 50 * 1024 * 1024, // 50 MB each
        CreatedAt: now.AddHours(-ageHours),
        LastAccessedAt: now.AddHours(-ageHours / 2),
        CurrentTier: StorageTier.Hot,
        IsCompressed: false
    ));
}

Console.WriteLine($"\n--- Simulated Segments ({segments.Count} segments, 0-{(segments.Count - 1) * 5} days old) ---");
foreach (var seg in segments.Take(5))
{
    var age = now - seg.CreatedAt;
    Console.WriteLine($"  {seg.SegmentId}: {age.TotalDays:F1} days old [{seg.CurrentTier}] {seg.SizeBytes / (1024 * 1024)} MB");
}
Console.WriteLine("  ...");
foreach (var seg in segments.Skip(segments.Count - 3))
{
    var age = now - seg.CreatedAt;
    Console.WriteLine($"  {seg.SegmentId}: {age.TotalDays:F1} days old [{seg.CurrentTier}] {seg.SizeBytes / (1024 * 1024)} MB");
}

// Step 4 - Run the retention policy checker
var checker = new RetentionPolicyChecker(new List<RetentionPolicy> { ordersPolicy });
var actions = checker.Evaluate(segments, now);

Console.WriteLine($"\n--- Retention Actions ({actions.Count} total) ---");
foreach (var action in actions)
{
    var actionDesc = action.ActionType switch
    {
        RetentionActionType.Migrate =>
            $"MIGRATE {action.FromTier} -> {action.ToTier}",
        RetentionActionType.Delete =>
            $"DELETE from {action.FromTier}",
        _ => action.ActionType.ToString()
    };

    Console.WriteLine($"  [{action.SegmentId}] {actionDesc}: {action.Reason}");
}

// Step 5 - Cost analysis
Console.WriteLine("\n--- Cost Analysis ---");

var totalGb = segments.Sum(s => s.SizeBytes) / (1024m * 1024m * 1024m);
var hotOnlyCost = totalGb * 0.50m;
Console.WriteLine($"  Without tiering: ${hotOnlyCost:F2}/month (all {totalGb:F1} GB in Hot)");

// Simulate applying tier migrations to calculate tiered cost
var tieredSegments = segments.Select(seg =>
{
    var age = now - seg.CreatedAt;
    StorageTier tier;
    if (age.TotalDays > 90)
        tier = StorageTier.Archive; // Would be deleted, but for cost comparison
    else if (age.TotalDays > 30)
        tier = StorageTier.Archive;
    else if (age.TotalDays > 7)
        tier = StorageTier.Cold;
    else if (age.TotalHours > 1)
        tier = StorageTier.Warm;
    else
        tier = StorageTier.Hot;

    return seg with { CurrentTier = tier };
}).ToList();

var costReport = RetentionCostCalculator.CalculateMonthlyCost(tieredSegments, ordersPolicy);
Console.WriteLine($"  With tiering:    ${costReport.TotalMonthlyCost:F2}/month");
var savings = (1 - costReport.TotalMonthlyCost / hotOnlyCost) * 100;
Console.WriteLine($"  Savings:         {savings:F1}%");

Console.WriteLine("\n  Cost breakdown by tier:");
foreach (var (tier, cost) in costReport.CostByTier.Where(kvp => kvp.Value > 0))
{
    var tierSegCount = tieredSegments.Count(s => s.CurrentTier == tier);
    Console.WriteLine($"    {tier,-8}: ${cost:F2}/month ({tierSegCount} segments)");
}

// Step 6 - Test legal hold
Console.WriteLine("\n--- Legal Hold Test ---");
var holdPolicy = ordersPolicy with { LegalHoldActive = true };
var holdChecker = new RetentionPolicyChecker(new List<RetentionPolicy> { holdPolicy });
var holdActions = holdChecker.Evaluate(segments, now);
Console.WriteLine($"  Actions with legal hold active: {holdActions.Count} (all blocked)");
Console.WriteLine($"  Actions without legal hold:     {actions.Count}");
Console.WriteLine("  Legal hold prevents all migrations and deletions.");

// Test segment-level legal hold
Console.WriteLine("\n--- Segment-Level Legal Hold ---");
var segWithHold = segments.Select((s, i) => i == segments.Count - 1
    ? s with { LegalHold = true }
    : s).ToList();

var segHoldActions = checker.Evaluate(segWithHold, now);
var blockedSeg = segments[^1];
var blockedAge = now - blockedSeg.CreatedAt;
Console.WriteLine($"  Segment {blockedSeg.SegmentId} ({blockedAge.TotalDays:F0} days old) has legal hold");
Console.WriteLine($"  Would normally be deleted, but is preserved");
Console.WriteLine($"  Actions without segment hold: {actions.Count}");
Console.WriteLine($"  Actions with segment hold:    {segHoldActions.Count}");

// Demonstrate different policy comparison
Console.WriteLine("\n--- Policy Comparison ---");
var policies = new[]
{
    new RetentionPolicy("Conservative", "orders*",
        new List<TierRule>
        {
            new(StorageTier.Hot, TimeSpan.FromDays(1), StorageTier.Warm),
            new(StorageTier.Warm, TimeSpan.FromDays(30), StorageTier.Cold),
            new(StorageTier.Cold, TimeSpan.FromDays(180), StorageTier.Archive)
        },
        TimeSpan.FromDays(365)),
    ordersPolicy,
    new RetentionPolicy("Aggressive", "orders*",
        new List<TierRule>
        {
            new(StorageTier.Hot, TimeSpan.FromMinutes(30), StorageTier.Warm),
            new(StorageTier.Warm, TimeSpan.FromDays(1), StorageTier.Cold),
            new(StorageTier.Cold, TimeSpan.FromDays(7), StorageTier.Archive)
        },
        TimeSpan.FromDays(30))
};

foreach (var policy in policies)
{
    var policyChecker = new RetentionPolicyChecker(new List<RetentionPolicy> { policy });
    var policyActions = policyChecker.Evaluate(segments, now);
    var deletes = policyActions.Count(a => a.ActionType == RetentionActionType.Delete);
    var migrates = policyActions.Count(a => a.ActionType == RetentionActionType.Migrate);
    Console.WriteLine($"  {policy.Name,-15}: {migrates} migrations, {deletes} deletions (retention: {policy.TotalRetention.TotalDays}d)");
}

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Retention policies define data lifecycle across tiers");
Console.WriteLine("2. TTL can be set at global, topic, or partition level");
Console.WriteLine("3. Legal holds override all retention rules");
Console.WriteLine("4. Tiered retention can save 60-80% in storage costs");
Console.WriteLine("5. Compliance requirements must be considered in policy design");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============================================================================
// Supporting types
// ============================================================================

public enum StorageTier
{
    Hot,
    Warm,
    Cold,
    Archive
}

public record RetentionPolicy(
    string Name,
    string TopicPattern,
    List<TierRule> TierRules,
    TimeSpan TotalRetention,
    bool ComplianceEnabled = false,
    bool LegalHoldActive = false
);

public record TierRule(
    StorageTier Tier,
    TimeSpan MaxAge,
    StorageTier? NextTier,
    bool CompressOnTransition = true
);

public record SegmentInfo(
    string SegmentId,
    string Topic,
    int Partition,
    long BaseOffset,
    long EndOffset,
    int MessageCount,
    long SizeBytes,
    DateTime CreatedAt,
    DateTime LastAccessedAt,
    StorageTier CurrentTier,
    bool IsCompressed,
    bool LegalHold = false
);

public enum RetentionActionType { Migrate, Delete, Compact, LegalHold }

public record RetentionAction(
    string SegmentId,
    RetentionActionType ActionType,
    StorageTier FromTier,
    StorageTier? ToTier,
    string Reason
);

public class RetentionPolicyChecker
{
    private readonly List<RetentionPolicy> _policies;

    public RetentionPolicyChecker(List<RetentionPolicy> policies)
    {
        _policies = policies;
    }

    public List<RetentionAction> Evaluate(List<SegmentInfo> segments, DateTime now)
    {
        var actions = new List<RetentionAction>();

        foreach (var segment in segments)
        {
            var policy = FindMatchingPolicy(segment.Topic);
            if (policy is null) continue;

            var age = now - segment.CreatedAt;

            // Check legal hold (policy-level or segment-level)
            if (policy.LegalHoldActive || segment.LegalHold)
            {
                // Cannot move or delete segments under legal hold
                continue;
            }

            // Check total retention - should this segment be deleted?
            if (age > policy.TotalRetention)
            {
                actions.Add(new RetentionAction(
                    segment.SegmentId,
                    RetentionActionType.Delete,
                    segment.CurrentTier,
                    null,
                    $"Exceeded total retention of {policy.TotalRetention.TotalDays} days (age: {age.TotalDays:F1}d)"
                ));
                continue;
            }

            // Check tier rules for migration
            foreach (var rule in policy.TierRules.OrderByDescending(r => r.MaxAge))
            {
                if (segment.CurrentTier == rule.Tier && age > rule.MaxAge && rule.NextTier.HasValue)
                {
                    actions.Add(new RetentionAction(
                        segment.SegmentId,
                        RetentionActionType.Migrate,
                        segment.CurrentTier,
                        rule.NextTier.Value,
                        $"Age {age.TotalDays:F1}d exceeds {rule.Tier} retention of {rule.MaxAge.TotalDays:F2}d"
                    ));
                    break;
                }
            }
        }

        return actions;
    }

    private RetentionPolicy? FindMatchingPolicy(string topic)
    {
        return _policies.FirstOrDefault(p =>
            topic.StartsWith(p.TopicPattern.TrimEnd('*')) ||
            p.TopicPattern == "*");
    }
}

public static class RetentionCostCalculator
{
    private static readonly Dictionary<StorageTier, decimal> CostPerGbPerMonth = new()
    {
        [StorageTier.Hot] = 0.50m,
        [StorageTier.Warm] = 0.10m,
        [StorageTier.Cold] = 0.02m,
        [StorageTier.Archive] = 0.004m
    };

    public static CostReport CalculateMonthlyCost(
        List<SegmentInfo> segments,
        RetentionPolicy policy)
    {
        var costByTier = new Dictionary<StorageTier, decimal>();

        foreach (var tier in Enum.GetValues<StorageTier>())
        {
            var tierSegments = segments.Where(s => s.CurrentTier == tier).ToList();
            var totalGb = tierSegments.Sum(s => s.SizeBytes) / (1024m * 1024m * 1024m);
            costByTier[tier] = totalGb * CostPerGbPerMonth[tier];
        }

        return new CostReport(
            PolicyName: policy.Name,
            CostByTier: costByTier,
            TotalMonthlyCost: costByTier.Values.Sum(),
            TotalStorageGb: segments.Sum(s => s.SizeBytes) / (1024m * 1024m * 1024m)
        );
    }
}

public record CostReport(
    string PolicyName,
    Dictionary<StorageTier, decimal> CostByTier,
    decimal TotalMonthlyCost,
    decimal TotalStorageGb
);
