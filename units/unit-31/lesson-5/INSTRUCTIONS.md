# Lesson 21.5: Instructions

Follow these step-by-step instructions to implement end-to-end exactly-once semantics.

## Step 1: Implement the Outbox Pattern

Create an outbox table simulator and publisher:

```csharp
public class OutboxStore
{
    private readonly List<OutboxEntry> _entries = new();
    private int _nextId = 1;
    private readonly object _lock = new();

    public record OutboxEntry(
        int Id,
        string EventType,
        string Payload,
        bool Published,
        DateTime CreatedAt);

    public int AddEntry(string eventType, string payload)
    {
        lock (_lock)
        {
            var entry = new OutboxEntry(_nextId++, eventType, payload, false, DateTime.UtcNow);
            _entries.Add(entry);
            return entry.Id;
        }
    }

    public List<OutboxEntry> GetUnpublishedEntries()
    {
        lock (_lock)
        {
            return _entries.Where(e => !e.Published).OrderBy(e => e.Id).ToList();
        }
    }

    public void MarkAsPublished(int id)
    {
        lock (_lock)
        {
            var index = _entries.FindIndex(e => e.Id == id);
            if (index >= 0)
            {
                _entries[index] = _entries[index] with { Published = true };
            }
        }
    }
}

public class OutboxPublisher
{
    private readonly OutboxStore _outbox;
    private readonly SurgewaveProducer<string, string> _producer;
    private readonly string _topic;

    public int PublishedCount { get; private set; }

    public OutboxPublisher(OutboxStore outbox, SurgewaveProducer<string, string> producer, string topic)
    {
        _outbox = outbox;
        _producer = producer;
        _topic = topic;
    }

    public async Task PublishPendingAsync(CancellationToken ct = default)
    {
        var entries = _outbox.GetUnpublishedEntries();

        foreach (var entry in entries)
        {
            await _producer.ProduceAsync(_topic, entry.Id.ToString(), entry.Payload, ct);
            _outbox.MarkAsPublished(entry.Id);
            PublishedCount++;
            Console.WriteLine($"  Published outbox entry {entry.Id}: {entry.EventType}");
        }
    }
}
```

## Step 2: Create a Saga Coordinator

Implement a simple saga state machine:

```csharp
public class SagaCoordinator
{
    private readonly Dictionary<string, SagaState> _sagas = new();
    private readonly object _lock = new();

    public enum SagaStatus { Started, InProgress, Compensating, Completed, Failed }

    public record SagaState(
        string SagaId,
        SagaStatus Status,
        int CurrentStep,
        List<string> CompletedSteps,
        string? Error);

    public string StartSaga()
    {
        var sagaId = Guid.NewGuid().ToString("N")[..8];
        lock (_lock)
        {
            _sagas[sagaId] = new SagaState(sagaId, SagaStatus.Started, 0, new List<string>(), null);
        }
        return sagaId;
    }

    public bool TryAdvanceStep(string sagaId, string stepName, Func<bool> executeStep)
    {
        lock (_lock)
        {
            if (!_sagas.TryGetValue(sagaId, out var state))
                return false;

            if (state.Status == SagaStatus.Compensating || state.Status == SagaStatus.Failed)
                return false;

            _sagas[sagaId] = state with
            {
                Status = SagaStatus.InProgress,
                CurrentStep = state.CurrentStep + 1
            };
        }

        try
        {
            var success = executeStep();
            lock (_lock)
            {
                if (success)
                {
                    var state = _sagas[sagaId];
                    state.CompletedSteps.Add(stepName);
                }
                else
                {
                    StartCompensation(sagaId, $"Step {stepName} failed");
                }
            }
            return success;
        }
        catch (Exception ex)
        {
            StartCompensation(sagaId, ex.Message);
            return false;
        }
    }

    public void CompleteSaga(string sagaId)
    {
        lock (_lock)
        {
            if (_sagas.TryGetValue(sagaId, out var state))
            {
                _sagas[sagaId] = state with { Status = SagaStatus.Completed };
            }
        }
    }

    private void StartCompensation(string sagaId, string error)
    {
        lock (_lock)
        {
            if (_sagas.TryGetValue(sagaId, out var state))
            {
                _sagas[sagaId] = state with
                {
                    Status = SagaStatus.Compensating,
                    Error = error
                };
            }
        }
    }

    public SagaState? GetSagaState(string sagaId)
    {
        lock (_lock)
        {
            return _sagas.TryGetValue(sagaId, out var state) ? state : null;
        }
    }
}
```

## Step 3: Implement Exactly-Once Consumer

Create a consumer that combines all deduplication strategies:

```csharp
public class ExactlyOnceConsumer
{
    private readonly HashSet<string> _processedMessageIds = new();
    private readonly Dictionary<string, string> _idempotencyResults = new();
    private readonly HashSet<string> _executedEffects = new();
    private readonly object _lock = new();

    public int ProcessedCount { get; private set; }
    public int DuplicatesSkipped { get; private set; }
    public int EffectsExecuted { get; private set; }
    public int EffectsSkipped { get; private set; }

    public async Task<bool> ProcessMessageAsync(
        string messageId,
        string idempotencyKey,
        Func<Task<string>> processAction,
        Func<Task>? sideEffect = null)
    {
        // Layer 1: Message ID deduplication
        lock (_lock)
        {
            if (_processedMessageIds.Contains(messageId))
            {
                DuplicatesSkipped++;
                Console.WriteLine($"    Duplicate message ID: {messageId}");
                return false;
            }
        }

        // Layer 2: Idempotency key check
        lock (_lock)
        {
            if (_idempotencyResults.ContainsKey(idempotencyKey))
            {
                DuplicatesSkipped++;
                Console.WriteLine($"    Duplicate idempotency key: {idempotencyKey}");
                return false;
            }
        }

        // Mark as in progress
        lock (_lock)
        {
            _processedMessageIds.Add(messageId);
        }

        // Process the message
        var result = await processAction();

        // Store idempotency result
        lock (_lock)
        {
            _idempotencyResults[idempotencyKey] = result;
            ProcessedCount++;
        }

        // Layer 3: Effectful once pattern
        if (sideEffect != null)
        {
            var effectId = $"effect:{idempotencyKey}";
            bool shouldExecute;

            lock (_lock)
            {
                shouldExecute = _executedEffects.Add(effectId);
            }

            if (shouldExecute)
            {
                await sideEffect();
                EffectsExecuted++;
            }
            else
            {
                EffectsSkipped++;
            }
        }

        return true;
    }
}
```

## Step 4: Create a Complete Pipeline

Combine outbox, saga, and exactly-once consumer:

```csharp
// Simulate a business transaction with outbox
async Task<string> ProcessOrderWithOutbox(string orderId, OutboxStore outbox)
{
    // Simulate business logic
    Console.WriteLine($"  Processing order {orderId}");

    // Add event to outbox (in real app, same DB transaction)
    var eventPayload = JsonSerializer.Serialize(new
    {
        EventType = "OrderCreated",
        OrderId = orderId,
        Timestamp = DateTime.UtcNow
    });

    var entryId = outbox.AddEntry("OrderCreated", eventPayload);
    Console.WriteLine($"  Added outbox entry {entryId}");

    return $"order:{orderId}:processed";
}

// Publish outbox entries
async Task PublishOutboxAsync(OutboxStore outbox, OutboxPublisher publisher)
{
    Console.WriteLine("Publishing outbox entries...");
    await publisher.PublishPendingAsync();
}
```

## Step 5: Test Failure and Recovery

Simulate failures and verify recovery:

```csharp
// Simulate a failing saga
var sagaId = sagaCoordinator.StartSaga();
Console.WriteLine($"\nStarting saga {sagaId}");

sagaCoordinator.TryAdvanceStep(sagaId, "CreateOrder", () =>
{
    Console.WriteLine("  Step 1: CreateOrder - SUCCESS");
    return true;
});

sagaCoordinator.TryAdvanceStep(sagaId, "ReserveInventory", () =>
{
    Console.WriteLine("  Step 2: ReserveInventory - SUCCESS");
    return true;
});

sagaCoordinator.TryAdvanceStep(sagaId, "ChargePayment", () =>
{
    Console.WriteLine("  Step 3: ChargePayment - FAILED!");
    return false; // Simulate failure
});

var state = sagaCoordinator.GetSagaState(sagaId);
Console.WriteLine($"Saga status: {state?.Status}");
Console.WriteLine($"Completed steps: {string.Join(", ", state?.CompletedSteps ?? new())}");
Console.WriteLine($"Error: {state?.Error}");
```

## Step 6: Run Integration Test

Test the complete exactly-once pipeline:

```csharp
// Produce messages including duplicates
var messages = new[]
{
    ("msg-001", "idem-001", "ORD-100"),
    ("msg-001", "idem-001", "ORD-100"), // Duplicate message ID
    ("msg-002", "idem-001", "ORD-100"), // Same idempotency key
    ("msg-003", "idem-002", "ORD-101"), // New unique message
};

foreach (var (msgId, idemKey, orderId) in messages)
{
    Console.WriteLine($"\nProcessing: msgId={msgId}, idemKey={idemKey}");

    await consumer.ProcessMessageAsync(
        messageId: msgId,
        idempotencyKey: idemKey,
        processAction: async () =>
        {
            Console.WriteLine($"    Executing business logic for {orderId}");
            return $"processed:{orderId}";
        },
        sideEffect: async () =>
        {
            Console.WriteLine($"    Sending notification for {orderId}");
        });
}

Console.WriteLine("\n--- Final Statistics ---");
Console.WriteLine($"Messages processed: {consumer.ProcessedCount}");
Console.WriteLine($"Duplicates skipped: {consumer.DuplicatesSkipped}");
Console.WriteLine($"Effects executed: {consumer.EffectsExecuted}");
Console.WriteLine($"Effects skipped: {consumer.EffectsSkipped}");
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Transactional outbox pattern
- Saga coordination with compensation
- Multi-layer deduplication
- Exactly-once consumer
- Failure and recovery testing

## Sample Output

```
=== Surgewave Bootcamp: Lesson 21.5 - End-to-End Exactly-Once ===

Surgewave broker started on port 12345

--- Outbox Pattern Demo ---
Processing order ORD-001
  Added outbox entry 1
Processing order ORD-002
  Added outbox entry 2

Publishing outbox entries...
  Published outbox entry 1: OrderCreated
  Published outbox entry 2: OrderCreated

--- Saga Coordination Demo ---
Starting saga a1b2c3d4
  Step 1: CreateOrder - SUCCESS
  Step 2: ReserveInventory - SUCCESS
  Step 3: ChargePayment - FAILED!
Saga status: Compensating
Completed steps: CreateOrder, ReserveInventory
Error: Step ChargePayment failed

--- Exactly-Once Consumer Demo ---
Processing: msgId=msg-001, idemKey=idem-001
    Executing business logic for ORD-100
    Sending notification for ORD-100

Processing: msgId=msg-001, idemKey=idem-001
    Duplicate message ID: msg-001

Processing: msgId=msg-002, idemKey=idem-001
    Duplicate idempotency key: idem-001

Processing: msgId=msg-003, idemKey=idem-002
    Executing business logic for ORD-101
    Sending notification for ORD-101

--- Final Statistics ---
Messages processed: 2
Duplicates skipped: 2
Effects executed: 2
Effects skipped: 0

Lesson complete!
```

## Knowledge Check

1. What is the purpose of the outbox pattern and when should you use it?
2. How does a saga coordinator handle failures mid-transaction?
3. Why do we need multiple layers of deduplication for exactly-once?
4. What happens if the outbox publisher crashes after producing but before marking as published?
5. How would you implement compensation logic for a failed saga step?

---

**Congratulations!** You have completed Unit 21: Message Deduplication.

**Review:**
- Lesson 21.1: Deduplication concepts and idempotency keys
- Lesson 21.2: In-memory caches, Bloom filters, and time-based expiry
- Lesson 21.3: Persistent stores and checkpointing
- Lesson 21.4: Idempotent operations and effectful once
- Lesson 21.5: End-to-end exactly-once with outbox and sagas
