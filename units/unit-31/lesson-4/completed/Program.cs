// Surgewave Bootcamp - Unit 21, Lesson 4: Idempotent Operations
// COMPLETED SOLUTION

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

var db = new InMemoryDatabase();

// Test 1: Database Upserts
Console.WriteLine("\n--- Test: Database Upserts ---");
var now = DateTime.UtcNow;

Console.WriteLine("Upserting order ORD-001 (created, $99.99)");
var result1 = db.UpsertOrder("ORD-001", "created", 99.99m, now);
Console.WriteLine($"  Result: {(result1 ? "Inserted new order" : "Skipped")}");

Console.WriteLine("Upserting order ORD-001 (shipped, $99.99) - newer timestamp");
var result2 = db.UpsertOrder("ORD-001", "shipped", 99.99m, now.AddMinutes(5));
var order1 = db.GetOrder("ORD-001");
Console.WriteLine($"  Result: {(result2 ? $"Updated existing order (version {order1?.Version})" : "Skipped")}");

Console.WriteLine("Upserting order ORD-001 with OLDER timestamp");
var result3 = db.UpsertOrder("ORD-001", "pending", 99.99m, now.AddMinutes(-5));
Console.WriteLine($"  Result: {(result3 ? "Updated" : "Skipped (stale update)")}");

// Test 2: Version-Based Updates
Console.WriteLine("\n--- Test: Version-Based Updates ---");
db.UpsertOrder("ORD-002", "created", 100m, DateTime.UtcNow);
var order2 = db.GetOrder("ORD-002")!;
Console.WriteLine($"Created order ORD-002 at version {order2.Version}");

var update1 = db.UpdateWithVersion("ORD-002", 150m, order2.Version);
Console.WriteLine($"Update with correct version ({order2.Version}): {(update1 ? "Success" : "Failed")}");

var update2 = db.UpdateWithVersion("ORD-002", 200m, order2.Version);
Console.WriteLine($"Update with stale version ({order2.Version}): {(update2 ? "Success" : "Failed (version mismatch)")}");

order2 = db.GetOrder("ORD-002")!;
var update3 = db.UpdateWithVersion("ORD-002", 175m, order2.Version);
Console.WriteLine($"Update with correct version ({order2.Version}): {(update3 ? "Success" : "Failed")}");

// Test 3: Idempotency Tokens
Console.WriteLine("\n--- Test: Idempotency Tokens ---");
var paymentKey = "pay-001";

Console.WriteLine($"Processing payment {paymentKey}...");
var (isNew1, cached1) = db.CheckIdempotencyKey(paymentKey);
if (isNew1)
{
    Console.WriteLine("  New request, processing payment...");
    var paymentResult = JsonSerializer.Serialize(new { status = "success", id = "txn-12345" });
    db.CompleteIdempotencyKey(paymentKey, paymentResult);
    Console.WriteLine($"  Payment completed: {paymentResult}");
}
else
{
    Console.WriteLine($"  Duplicate detected, returning cached: {cached1}");
}

Console.WriteLine($"Processing payment {paymentKey} again...");
var (isNew2, cached2) = db.CheckIdempotencyKey(paymentKey);
if (isNew2)
{
    Console.WriteLine("  Processing new payment...");
}
else
{
    Console.WriteLine($"  Duplicate detected, returning cached: {cached2}");
}

// Test 4: Effectful Once Pattern
Console.WriteLine("\n--- Test: Effectful Once ---");
var effectId = "notify:ORD-003:shipped";

Console.WriteLine("Processing order notification...");
var executed1 = db.TryExecuteEffect(effectId, () =>
{
    Console.WriteLine("  -> Sending email notification for ORD-003");
});
Console.WriteLine($"  Effect executed: {executed1}");

Console.WriteLine("Processing same order notification again...");
var executed2 = db.TryExecuteEffect(effectId, () =>
{
    Console.WriteLine("  -> This should NOT print!");
});
Console.WriteLine($"  Effect executed: {executed2} (prevented duplicate)");

// Test 5: Message Processing Integration
Console.WriteLine("\n--- Test: Integrated Message Processing ---");
var topic = "orders-topic";

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

// Produce messages including duplicates
var messages = new[]
{
    ("msg-001", new OrderEvent("ORD-100", "created", 50m, DateTime.UtcNow)),
    ("msg-001", new OrderEvent("ORD-100", "created", 50m, DateTime.UtcNow)), // Duplicate
    ("msg-002", new OrderEvent("ORD-100", "shipped", 50m, DateTime.UtcNow.AddMinutes(1))),
    ("msg-003", new OrderEvent("ORD-100", "pending", 50m, DateTime.UtcNow.AddMinutes(-1))), // Stale
};

foreach (var (msgId, order) in messages)
{
    var json = JsonSerializer.Serialize(new
    {
        MessageId = msgId,
        order.OrderId,
        order.Status,
        order.Amount,
        order.Timestamp
    });
    await producer.ProduceAsync(topic, msgId, json);
}

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "idempotent-processor";
    options.EnableAutoCommit = false;
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await consumer.SubscribeAsync(default, topic);

Console.WriteLine("\nProcessing messages:");
for (int i = 0; i < messages.Length; i++)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    var msg = JsonSerializer.Deserialize<JsonElement>(result.Value);
    var msgId = msg.GetProperty("MessageId").GetString()!;
    var orderId = msg.GetProperty("OrderId").GetString()!;
    var status = msg.GetProperty("Status").GetString()!;
    var amount = msg.GetProperty("Amount").GetDecimal();
    var timestamp = msg.GetProperty("Timestamp").GetDateTime();

    Console.WriteLine($"\nMessage: {msgId}");

    // Check idempotency
    var (isNew, _) = db.CheckIdempotencyKey(msgId);
    if (!isNew)
    {
        Console.WriteLine($"  DUPLICATE: Already processed, skipping");
        await consumer.CommitAsync(result);
        continue;
    }

    // Upsert order (idempotent)
    var updated = db.UpsertOrder(orderId, status, amount, timestamp);
    if (updated)
    {
        Console.WriteLine($"  Processed: {orderId} -> {status}");

        // Execute effect once
        var notifyEffectId = $"notify:{orderId}:{status}";
        db.TryExecuteEffect(notifyEffectId, () =>
        {
            Console.WriteLine($"  -> Notification sent for {orderId}");
        });
    }
    else
    {
        Console.WriteLine($"  SKIPPED: Stale update for {orderId}");
    }

    db.CompleteIdempotencyKey(msgId, "success");
    await consumer.CommitAsync(result);
}

// Statistics
Console.WriteLine("\n--- Statistics ---");
Console.WriteLine($"Upserts performed: {db.UpsertCount}");
Console.WriteLine($"Stale updates skipped: {db.SkippedCount}");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Upserts are idempotent: INSERT or UPDATE to same state");
Console.WriteLine("2. Timestamp checks prevent out-of-order processing");
Console.WriteLine("3. Version numbers enable optimistic concurrency control");
Console.WriteLine("4. Idempotency tokens allow caching API responses");
Console.WriteLine("5. Record effects BEFORE execution for effectful once");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ==================== Helper Classes ====================

public record OrderEvent(string OrderId, string Status, decimal Amount, DateTime Timestamp);

public class InMemoryDatabase
{
    private readonly Dictionary<string, OrderRecord> _orders = new();
    private readonly Dictionary<string, IdempotencyRecord> _idempotencyStore = new();
    private readonly HashSet<string> _executedEffects = new();
    private readonly object _lock = new();

    public int UpsertCount { get; private set; }
    public int SkippedCount { get; private set; }

    public record OrderRecord(
        string Id,
        string Status,
        decimal Amount,
        int Version,
        DateTime UpdatedAt);

    public record IdempotencyRecord(
        string Key,
        string Status,
        string? Result,
        DateTime CreatedAt);

    // Idempotent upsert with timestamp check
    public bool UpsertOrder(string id, string status, decimal amount, DateTime eventTime)
    {
        lock (_lock)
        {
            if (_orders.TryGetValue(id, out var existing))
            {
                // Only update if event is newer (prevents out-of-order processing)
                if (eventTime <= existing.UpdatedAt)
                {
                    SkippedCount++;
                    return false; // Stale update, skip
                }

                // Update existing record
                _orders[id] = existing with
                {
                    Status = status,
                    Amount = amount,
                    Version = existing.Version + 1,
                    UpdatedAt = eventTime
                };
            }
            else
            {
                // Insert new record
                _orders[id] = new OrderRecord(id, status, amount, 1, eventTime);
            }

            UpsertCount++;
            return true;
        }
    }

    public OrderRecord? GetOrder(string id)
    {
        lock (_lock)
        {
            return _orders.TryGetValue(id, out var order) ? order : null;
        }
    }

    // Version-based optimistic concurrency
    public bool UpdateWithVersion(string id, decimal newAmount, int expectedVersion)
    {
        lock (_lock)
        {
            if (!_orders.TryGetValue(id, out var existing))
            {
                return false; // Record not found
            }

            if (existing.Version != expectedVersion)
            {
                SkippedCount++;
                return false; // Version mismatch (concurrent update or duplicate)
            }

            _orders[id] = existing with
            {
                Amount = newAmount,
                Version = existing.Version + 1,
                UpdatedAt = DateTime.UtcNow
            };

            UpsertCount++;
            return true;
        }
    }

    // Idempotency token check
    public (bool isNew, string? cachedResult) CheckIdempotencyKey(string key)
    {
        lock (_lock)
        {
            if (_idempotencyStore.TryGetValue(key, out var record))
            {
                return (false, record.Result);
            }

            // Mark as in progress
            _idempotencyStore[key] = new IdempotencyRecord(
                key,
                "in_progress",
                null,
                DateTime.UtcNow);

            return (true, null);
        }
    }

    public void CompleteIdempotencyKey(string key, string result)
    {
        lock (_lock)
        {
            if (_idempotencyStore.TryGetValue(key, out var record))
            {
                _idempotencyStore[key] = record with
                {
                    Status = "completed",
                    Result = result
                };
            }
        }
    }

    public void FailIdempotencyKey(string key)
    {
        lock (_lock)
        {
            _idempotencyStore.Remove(key);
        }
    }

    // Effectful once pattern
    public bool TryExecuteEffect(string effectId, Action effect)
    {
        lock (_lock)
        {
            if (_executedEffects.Contains(effectId))
            {
                return false; // Effect already executed
            }

            // Record effect BEFORE executing
            _executedEffects.Add(effectId);
        }

        // Execute effect outside lock
        effect();
        return true;
    }

    public bool WasEffectExecuted(string effectId)
    {
        lock (_lock)
        {
            return _executedEffects.Contains(effectId);
        }
    }
}
