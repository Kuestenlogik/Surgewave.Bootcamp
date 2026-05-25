# Lesson 21.1: Instructions

Follow these step-by-step instructions to implement deduplication concepts.

## Step 1: Define Messages with Idempotency Keys

Create a message type that includes an idempotency key:

```csharp
public record OrderEvent(
    string IdempotencyKey,  // Unique identifier for deduplication
    string OrderId,
    string Action,          // "created", "updated", "cancelled"
    decimal Amount,
    DateTime Timestamp
);
```

The idempotency key should be:
- Unique per logical operation
- Deterministic (same operation = same key)
- Included in the message payload

## Step 2: Create an Idempotency Key Generator

Generate consistent keys for messages:

```csharp
public static class IdempotencyKeyGenerator
{
    // Natural key from business identifier
    public static string FromOrderId(string orderId, string action)
        => $"order:{orderId}:{action}";

    // Composite key with timestamp window
    public static string FromComposite(string source, string entity, string action, DateTime timestamp)
        => $"{source}:{entity}:{action}:{timestamp:yyyy-MM-dd-HH}";

    // Hash-based key from content
    public static string FromContent(string content)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash)[..32];
    }
}
```

## Step 3: Implement a Simple Deduplication Checker

Create a basic in-memory deduplication store:

```csharp
public class DeduplicationChecker
{
    private readonly HashSet<string> _processedKeys = new();
    private readonly object _lock = new();

    public bool IsDuplicate(string idempotencyKey)
    {
        lock (_lock)
        {
            return _processedKeys.Contains(idempotencyKey);
        }
    }

    public bool TryMarkAsProcessed(string idempotencyKey)
    {
        lock (_lock)
        {
            return _processedKeys.Add(idempotencyKey);
        }
    }

    public int ProcessedCount => _processedKeys.Count;
}
```

## Step 4: Process Messages with Deduplication

Apply deduplication during message processing:

```csharp
var dedupChecker = new DeduplicationChecker();
var duplicateCount = 0;
var processedCount = 0;

while (!cancellationToken.IsCancellationRequested)
{
    var result = await consumer.ConsumeAsync(cancellationToken);
    if (result == null) continue;

    var order = JsonSerializer.Deserialize<OrderEvent>(result.Value);

    // Check for duplicate
    if (dedupChecker.IsDuplicate(order.IdempotencyKey))
    {
        duplicateCount++;
        Console.WriteLine($"DUPLICATE detected: {order.IdempotencyKey}");
        await consumer.CommitAsync(result);
        continue;
    }

    // Process the message
    Console.WriteLine($"Processing: {order.OrderId} - {order.Action}");
    await ProcessOrderAsync(order);

    // Mark as processed
    dedupChecker.TryMarkAsProcessed(order.IdempotencyKey);
    processedCount++;

    await consumer.CommitAsync(result);
}
```

## Step 5: Simulate Duplicate Scenarios

Intentionally create duplicates to test your deduplication:

```csharp
// Scenario 1: Same message sent twice (producer retry)
var order1 = new OrderEvent(
    IdempotencyKeyGenerator.FromOrderId("ORD-001", "created"),
    "ORD-001", "created", 99.99m, DateTime.UtcNow);

await producer.ProduceAsync(topic, order1.OrderId, JsonSerializer.Serialize(order1));
await producer.ProduceAsync(topic, order1.OrderId, JsonSerializer.Serialize(order1)); // Duplicate!

// Scenario 2: Different messages (should both process)
var order2 = new OrderEvent(
    IdempotencyKeyGenerator.FromOrderId("ORD-002", "created"),
    "ORD-002", "created", 149.99m, DateTime.UtcNow);

await producer.ProduceAsync(topic, order2.OrderId, JsonSerializer.Serialize(order2));
```

## Step 6: Track Deduplication Statistics

Monitor your deduplication effectiveness:

```csharp
Console.WriteLine();
Console.WriteLine("--- Deduplication Statistics ---");
Console.WriteLine($"Total messages received: {processedCount + duplicateCount}");
Console.WriteLine($"Unique messages processed: {processedCount}");
Console.WriteLine($"Duplicates detected: {duplicateCount}");
Console.WriteLine($"Deduplication rate: {(double)duplicateCount / (processedCount + duplicateCount):P2}");
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Message types with idempotency keys
- Multiple key generation strategies
- In-memory deduplication checking
- Duplicate simulation and detection
- Statistics tracking

## Sample Output

```
=== Surgewave Bootcamp: Lesson 21.1 - Deduplication Concepts ===

Surgewave broker started on port 12345

--- Idempotency Key Strategies ---
Natural key: order:ORD-001:created
Composite key: payment-svc:order-123:charge:2024-01-15-10
Hash-based key: A3F2B8C9D4E5F6A7B8C9D0E1F2A3B4C5

--- Simulating Message Production ---
Producing unique message: order:ORD-001:created
Producing DUPLICATE: order:ORD-001:created
Producing unique message: order:ORD-002:created
Producing unique message: order:ORD-003:updated
Producing DUPLICATE: order:ORD-003:updated
Producing DUPLICATE: order:ORD-003:updated
Produced 6 messages (3 unique, 3 duplicates)

--- Processing with Deduplication ---
Processing: ORD-001 - created ($99.99)
DUPLICATE detected: order:ORD-001:created (skipping)
Processing: ORD-002 - created ($149.99)
Processing: ORD-003 - updated ($199.99)
DUPLICATE detected: order:ORD-003:updated (skipping)
DUPLICATE detected: order:ORD-003:updated (skipping)

--- Deduplication Statistics ---
Total messages received: 6
Unique messages processed: 3
Duplicates detected: 3
Deduplication rate: 50.00%

Lesson complete!
```

## Knowledge Check

1. What causes message duplicates in at-least-once delivery systems?
2. What are the pros and cons of natural keys vs UUID-based idempotency keys?
3. Why should you store the idempotency key BEFORE processing the message?
4. How would you handle a scenario where the deduplication store is unavailable?
5. What happens to your deduplication if the consumer restarts with a fresh store?

---

**Next:** [Lesson 21.2: In-Memory Deduplication](../lesson-2/README.md)
