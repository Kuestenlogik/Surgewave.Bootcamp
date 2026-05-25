# Lesson 24.5: Instructions

Follow these step-by-step instructions to implement retention policies and lifecycle management.

## Step 1: Define Retention Policy Types

Create models representing retention policies:

```csharp
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
```

## Step 2: Create Segment Metadata Tracker

Track segments and their current tier placement:

```csharp
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
```

## Step 3: Implement the Retention Policy Checker

Build a checker that evaluates segments against policies:

```csharp
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

            // Check legal hold
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
                    $"Exceeded total retention of {policy.TotalRetention.TotalDays} days"
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
                        $"Age {age.TotalDays:F1}d exceeds {rule.Tier} retention of {rule.MaxAge.TotalDays}d"
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

public enum RetentionActionType { Migrate, Delete, Compact, LegalHold }

public record RetentionAction(
    string SegmentId,
    RetentionActionType ActionType,
    StorageTier FromTier,
    StorageTier? ToTier,
    string Reason
);
```

## Step 4: Implement Cost Calculator

Calculate storage costs under different retention strategies:

```csharp
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
```

## Step 5: Simulate Data Aging

Create a simulation that demonstrates retention policy enforcement over time:

```csharp
// Create segments at various ages
var segments = new List<SegmentInfo>();
var now = DateTime.UtcNow;

// Generate segments spanning different age ranges
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
```

## Step 6: Apply Policy and Report

Run the checker and display results:

```csharp
var checker = new RetentionPolicyChecker(new List<RetentionPolicy> { policy });
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
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Retention policy configuration
- Policy checker evaluation
- Data aging simulation
- Cost comparison across strategies
- Legal hold enforcement

## Sample Output

```
=== Surgewave Bootcamp: Lesson 24.5 - Retention and Lifecycle Policies ===

Surgewave broker started on port 12345

--- Retention Policy: Standard Orders ---
  Topic Pattern: orders*
  Total Retention: 90 days
  Tier Rules:
    Hot  -> Warm  after 0.04 days (1 hour)
    Warm -> Cold  after 7 days
    Cold -> Archive after 30 days

--- Simulated Segments (20 segments, 0-95 days old) ---
  seg-0000: 0.0 days old [Hot] 50 MB
  seg-0001: 5.0 days old [Hot] 50 MB
  seg-0002: 10.0 days old [Hot] 50 MB
  ...

--- Retention Actions (18 total) ---
  [seg-0001] MIGRATE Hot -> Warm: Age 5.0d exceeds Hot retention of 0.0d
  [seg-0002] MIGRATE Hot -> Cold: Age 10.0d exceeds Warm retention of 7.0d
  ...
  [seg-0019] DELETE from Hot: Exceeded total retention of 90 days

--- Cost Analysis ---
  Without tiering: $50.00/month (all in Hot)
  With tiering:    $8.42/month
  Savings:         83.2%

--- Legal Hold Test ---
  Segment seg-0019 has legal hold - deletion blocked

Lesson complete!
```

## Knowledge Check

1. What are the main types of retention rules?
2. How does legal hold affect retention policy enforcement?
3. What is the cost benefit of aggressive tier migration?
4. When should you use compliance-enabled retention policies?
5. How do access-based rules differ from age-based rules?

---

**Next:** [Unit 24 Project: Tiered Storage Demo](../project/README.md)
