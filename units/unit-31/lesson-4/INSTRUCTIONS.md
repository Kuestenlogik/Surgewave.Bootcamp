# Lesson 21.4: Instructions

Follow these step-by-step instructions to implement idempotent operations.

## Step 1: Create an In-Memory Database Simulator

For demonstration, we'll create a simple in-memory store that mimics database behavior:

```csharp
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
}
```

## Step 2: Implement Database Upsert

Create an idempotent upsert operation:

```csharp
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
```

## Step 3: Implement Version-Based Updates

Add optimistic concurrency control:

```csharp
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
```

## Step 4: Implement Idempotency Token Pattern

Create a system for tracking request idempotency:

```csharp
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
```

## Step 5: Implement Effectful Once Pattern

Ensure external effects execute exactly once:

```csharp
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
    // If this fails, the effect is recorded but not completed
    // Retry logic should handle this case
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
```

## Step 6: Process Messages with Idempotent Operations

Apply the patterns during message processing:

```csharp
async Task ProcessOrderMessage(string messageId, OrderEvent order, InMemoryDatabase db)
{
    // Check idempotency key
    var (isNew, cachedResult) = db.CheckIdempotencyKey(messageId);

    if (!isNew)
    {
        Console.WriteLine($"  Duplicate message {messageId}, returning cached result");
        return;
    }

    try
    {
        // Idempotent upsert
        var updated = db.UpsertOrder(order.OrderId, order.Status, order.Amount, order.Timestamp);

        if (updated)
        {
            Console.WriteLine($"  Processed order {order.OrderId}: {order.Status}");

            // Effectful once: Send notification
            var effectId = $"notify:{order.OrderId}:{order.Status}";
            db.TryExecuteEffect(effectId, () =>
            {
                Console.WriteLine($"    -> Sending notification for {order.OrderId}");
            });
        }
        else
        {
            Console.WriteLine($"  Skipped stale update for {order.OrderId}");
        }

        db.CompleteIdempotencyKey(messageId, "success");
    }
    catch (Exception ex)
    {
        db.FailIdempotencyKey(messageId);
        throw;
    }
}
```

## Step 7: Test with Duplicates and Out-of-Order Messages

Verify the idempotent behavior:

```csharp
// Test 1: Duplicate messages
Console.WriteLine("\n--- Test: Duplicate Messages ---");
var msg1 = new OrderEvent("ORD-001", "created", 100m, DateTime.UtcNow);
await ProcessOrderMessage("msg-001", msg1, db);
await ProcessOrderMessage("msg-001", msg1, db); // Duplicate!

// Test 2: Out-of-order messages
Console.WriteLine("\n--- Test: Out-of-Order Messages ---");
var older = new OrderEvent("ORD-002", "created", 100m, DateTime.UtcNow.AddMinutes(-5));
var newer = new OrderEvent("ORD-002", "shipped", 100m, DateTime.UtcNow);
await ProcessOrderMessage("msg-003", newer, db); // Process newer first
await ProcessOrderMessage("msg-002", older, db); // Older arrives late (should be skipped)

// Test 3: Version-based update
Console.WriteLine("\n--- Test: Version-Based Update ---");
db.UpsertOrder("ORD-003", "created", 100m, DateTime.UtcNow);
var order = db.GetOrder("ORD-003")!;
Console.WriteLine($"Order version: {order.Version}");

db.UpdateWithVersion("ORD-003", 150m, order.Version); // First update succeeds
db.UpdateWithVersion("ORD-003", 200m, order.Version); // Same version fails (duplicate)
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Database upsert operations
- Version-based optimistic concurrency
- Idempotency token pattern
- Effectful once pattern
- Comprehensive testing

## Sample Output

```
=== Surgewave Bootcamp: Lesson 21.4 - Idempotent Operations ===

Surgewave broker started on port 12345

--- Test: Database Upserts ---
Upserting order ORD-001 (created, $99.99)
  Result: Inserted new order
Upserting order ORD-001 (shipped, $99.99)
  Result: Updated existing order (version 1 -> 2)
Upserting order ORD-001 with OLDER timestamp
  Result: Skipped (stale update)

--- Test: Version-Based Updates ---
Created order ORD-002 at version 1
Update with correct version (1): Success
Update with stale version (1): Failed (version is now 2)
Update with correct version (2): Success

--- Test: Idempotency Tokens ---
Processing payment pay-001...
  New request, processing payment
  Payment completed: {"status": "success", "id": "txn-12345"}
Processing payment pay-001 again...
  Duplicate detected, returning cached: {"status": "success", "id": "txn-12345"}

--- Test: Effectful Once ---
Processing order notification...
  Effect notify:ORD-003:shipped executing
  -> Sending email notification
Processing same order again...
  Effect notify:ORD-003:shipped already executed (skipping)

--- Statistics ---
Upserts performed: 5
Stale updates skipped: 2
Duplicate effects prevented: 1

Lesson complete!
```

## Knowledge Check

1. What makes an operation idempotent?
2. How does timestamp-based upsert prevent out-of-order processing?
3. What is the purpose of storing a version number with each record?
4. Why do we record the effect ID BEFORE executing the effect?
5. How would you handle an idempotency key that is stuck in "in_progress" state?

---

**Next:** [Lesson 21.5: End-to-End Exactly-Once](../lesson-5/README.md)
