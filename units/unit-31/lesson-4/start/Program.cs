// Surgewave Bootcamp - Unit 21, Lesson 4: Idempotent Operations
//
// In this lesson, you'll learn:
// - Implement database upserts for safe repeated writes
// - Use version-based optimistic concurrency
// - Build an idempotency token system
// - Apply the effectful once pattern
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 21.4 - Idempotent Operations ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

// TODO: Step 1 - Create an in-memory database simulator
// public class InMemoryDatabase
// {
//     private readonly Dictionary<string, OrderRecord> _orders = new();
//     private readonly Dictionary<string, IdempotencyRecord> _idempotencyStore = new();
//     private readonly HashSet<string> _executedEffects = new();
//     private readonly object _lock = new();
//
//     public int UpsertCount { get; private set; }
//     public int SkippedCount { get; private set; }
//
//     public record OrderRecord(string Id, string Status, decimal Amount, int Version, DateTime UpdatedAt);
//     public record IdempotencyRecord(string Key, string Status, string? Result, DateTime CreatedAt);
// }

// TODO: Step 2 - Implement database upsert
// public bool UpsertOrder(string id, string status, decimal amount, DateTime eventTime)
// {
//     lock (_lock)
//     {
//         if (_orders.TryGetValue(id, out var existing))
//         {
//             if (eventTime <= existing.UpdatedAt)
//             {
//                 SkippedCount++;
//                 return false; // Stale update
//             }
//             _orders[id] = existing with { Status = status, Amount = amount, Version = existing.Version + 1, UpdatedAt = eventTime };
//         }
//         else
//         {
//             _orders[id] = new OrderRecord(id, status, amount, 1, eventTime);
//         }
//         UpsertCount++;
//         return true;
//     }
// }

// TODO: Step 3 - Implement version-based updates
// public bool UpdateWithVersion(string id, decimal newAmount, int expectedVersion)
// {
//     lock (_lock)
//     {
//         if (!_orders.TryGetValue(id, out var existing)) return false;
//         if (existing.Version != expectedVersion)
//         {
//             SkippedCount++;
//             return false;
//         }
//         _orders[id] = existing with { Amount = newAmount, Version = existing.Version + 1, UpdatedAt = DateTime.UtcNow };
//         UpsertCount++;
//         return true;
//     }
// }

// TODO: Step 4 - Implement idempotency token pattern
// public (bool isNew, string? cachedResult) CheckIdempotencyKey(string key)
// {
//     lock (_lock)
//     {
//         if (_idempotencyStore.TryGetValue(key, out var record))
//             return (false, record.Result);
//
//         _idempotencyStore[key] = new IdempotencyRecord(key, "in_progress", null, DateTime.UtcNow);
//         return (true, null);
//     }
// }

// TODO: Step 5 - Implement effectful once pattern
// public bool TryExecuteEffect(string effectId, Action effect)
// {
//     lock (_lock)
//     {
//         if (_executedEffects.Contains(effectId))
//             return false;
//         _executedEffects.Add(effectId);
//     }
//     effect();
//     return true;
// }

// TODO: Step 6 - Test the idempotent operations
// var db = new InMemoryDatabase();
//
// Console.WriteLine("--- Test: Database Upserts ---");
// var now = DateTime.UtcNow;
// db.UpsertOrder("ORD-001", "created", 99.99m, now);
// Console.WriteLine($"Upserted ORD-001 (created)");
// db.UpsertOrder("ORD-001", "shipped", 99.99m, now.AddMinutes(5));
// Console.WriteLine($"Upserted ORD-001 (shipped)");
// db.UpsertOrder("ORD-001", "delivered", 99.99m, now.AddMinutes(-5)); // Old event
// Console.WriteLine($"Attempted stale update (should be skipped)");
// Console.WriteLine($"Upserts: {db.UpsertCount}, Skipped: {db.SkippedCount}");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Upserts are idempotent: INSERT or UPDATE to same state");
Console.WriteLine("2. Timestamp checks prevent out-of-order processing");
Console.WriteLine("3. Version numbers enable optimistic concurrency control");
Console.WriteLine("4. Idempotency tokens allow caching API responses");
Console.WriteLine("5. Record effects BEFORE execution for effectful once");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
