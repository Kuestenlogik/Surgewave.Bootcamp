// Surgewave Bootcamp - Unit 24, Lesson 5: Retention and Lifecycle Policies
//
// In this lesson, you'll learn:
// - How to define data retention policies with TTL
// - Implement lifecycle rules for automatic tier migration
// - Build a retention policy checker
// - Calculate cost savings from tiered retention
// - Handle compliance requirements with legal holds
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

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

// TODO: Step 1 - Define retention policy types
// public enum StorageTier { Hot, Warm, Cold, Archive }
//
// public record RetentionPolicy(
//     string Name,
//     string TopicPattern,
//     List<TierRule> TierRules,
//     TimeSpan TotalRetention,
//     bool ComplianceEnabled = false,
//     bool LegalHoldActive = false
// );
//
// public record TierRule(
//     StorageTier Tier,
//     TimeSpan MaxAge,
//     StorageTier? NextTier,
//     bool CompressOnTransition = true
// );

// TODO: Step 2 - Create a retention policy for orders
// var ordersPolicy = new RetentionPolicy(
//     Name: "Standard Orders",
//     TopicPattern: "orders*",
//     TierRules: new List<TierRule>
//     {
//         new(StorageTier.Hot, TimeSpan.FromHours(1), StorageTier.Warm),
//         new(StorageTier.Warm, TimeSpan.FromDays(7), StorageTier.Cold),
//         new(StorageTier.Cold, TimeSpan.FromDays(30), StorageTier.Archive)
//     },
//     TotalRetention: TimeSpan.FromDays(90)
// );
//
// Console.WriteLine($"\n--- Retention Policy: {ordersPolicy.Name} ---");
// Console.WriteLine($"  Topic Pattern: {ordersPolicy.TopicPattern}");
// Console.WriteLine($"  Total Retention: {ordersPolicy.TotalRetention.TotalDays} days");

// TODO: Step 3 - Create simulated segments at various ages
// var segments = new List<SegmentInfo>();
// for (int i = 0; i < 20; i++)
// {
//     var ageHours = i * 24 * 5; // Each segment 5 days apart
//     segments.Add(new SegmentInfo(
//         SegmentId: $"seg-{i:D4}",
//         Topic: "orders",
//         Partition: 0,
//         BaseOffset: i * 10000,
//         EndOffset: (i + 1) * 10000 - 1,
//         MessageCount: 10000,
//         SizeBytes: 50 * 1024 * 1024,
//         CreatedAt: now.AddHours(-ageHours),
//         LastAccessedAt: now.AddHours(-ageHours / 2),
//         CurrentTier: StorageTier.Hot,
//         IsCompressed: false
//     ));
// }

// TODO: Step 4 - Implement and run the retention policy checker
// var checker = new RetentionPolicyChecker(new List<RetentionPolicy> { ordersPolicy });
// var actions = checker.Evaluate(segments, now);
//
// Console.WriteLine($"\n--- Retention Actions ({actions.Count} total) ---");
// foreach (var action in actions)
// {
//     var actionDesc = action.ActionType switch
//     {
//         RetentionActionType.Migrate =>
//             $"MIGRATE {action.FromTier} -> {action.ToTier}",
//         RetentionActionType.Delete =>
//             $"DELETE from {action.FromTier}",
//         _ => action.ActionType.ToString()
//     };
//     Console.WriteLine($"  [{action.SegmentId}] {actionDesc}: {action.Reason}");
// }

// TODO: Step 5 - Calculate cost comparison
// Console.WriteLine("\n--- Cost Analysis ---");
// var hotOnlyCost = segments.Sum(s => s.SizeBytes) / (1024m * 1024m * 1024m) * 0.50m;
// Console.WriteLine($"  Without tiering: ${hotOnlyCost:F2}/month (all in Hot)");
//
// // Simulate applying tier migrations
// // Calculate cost after tiering

// TODO: Step 6 - Test legal hold
// Console.WriteLine("\n--- Legal Hold Test ---");
// var holdPolicy = ordersPolicy with { LegalHoldActive = true };
// var holdActions = new RetentionPolicyChecker(new List<RetentionPolicy> { holdPolicy })
//     .Evaluate(segments, now);
// Console.WriteLine($"  Actions with legal hold: {holdActions.Count} (should be 0)");

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
// TODO: Add supporting types below
// ============================================================================

// public enum StorageTier { Hot, Warm, Cold, Archive }
// public record RetentionPolicy(...);
// public record TierRule(...);
// public record SegmentInfo(...);
// public record RetentionAction(...);
// public enum RetentionActionType { Migrate, Delete, Compact, LegalHold }
// public class RetentionPolicyChecker { ... }
// public static class RetentionCostCalculator { ... }
// public record CostReport(...);
