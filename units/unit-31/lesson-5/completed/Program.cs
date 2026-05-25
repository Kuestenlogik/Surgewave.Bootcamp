// Surgewave Bootcamp - Unit 21, Lesson 5: End-to-End Exactly-Once
// COMPLETED SOLUTION

using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 21.5 - End-to-End Exactly-Once ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var topic = "events-topic";

// Step 1 - Outbox Pattern Demo
Console.WriteLine("\n--- Outbox Pattern Demo ---");
var outbox = new OutboxStore();

// Simulate business transactions that write to outbox
Console.WriteLine("Processing order ORD-001...");
outbox.AddEntry("OrderCreated", JsonSerializer.Serialize(new { OrderId = "ORD-001", Amount = 99.99 }));
Console.WriteLine("  Added OrderCreated event to outbox");

Console.WriteLine("Processing order ORD-002...");
outbox.AddEntry("OrderCreated", JsonSerializer.Serialize(new { OrderId = "ORD-002", Amount = 149.99 }));
Console.WriteLine("  Added OrderCreated event to outbox");

// Create producer for outbox publishing
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

// Publish outbox entries (simulating outbox publisher)
Console.WriteLine("\nPublishing outbox entries...");
var outboxPublisher = new OutboxPublisher(outbox, producer, topic);
await outboxPublisher.PublishPendingAsync();
Console.WriteLine($"Published {outboxPublisher.PublishedCount} outbox entries");

// Step 2 - Saga Coordination Demo
Console.WriteLine("\n--- Saga Coordination Demo ---");
var sagaCoordinator = new SagaCoordinator();

// Start a saga
var sagaId = sagaCoordinator.StartSaga();
Console.WriteLine($"Starting saga {sagaId}");

// Execute saga steps
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

// Simulate a failure
sagaCoordinator.TryAdvanceStep(sagaId, "ChargePayment", () =>
{
    Console.WriteLine("  Step 3: ChargePayment - FAILED!");
    return false; // Simulate failure
});

var sagaState = sagaCoordinator.GetSagaState(sagaId);
Console.WriteLine($"\nSaga status: {sagaState?.Status}");
Console.WriteLine($"Completed steps: {string.Join(", ", sagaState?.CompletedSteps ?? new List<string>())}");
Console.WriteLine($"Error: {sagaState?.Error ?? "None"}");

// Successful saga
var saga2Id = sagaCoordinator.StartSaga();
Console.WriteLine($"\nStarting saga {saga2Id}");

sagaCoordinator.TryAdvanceStep(saga2Id, "ValidateOrder", () =>
{
    Console.WriteLine("  Step 1: ValidateOrder - SUCCESS");
    return true;
});

sagaCoordinator.TryAdvanceStep(saga2Id, "ProcessPayment", () =>
{
    Console.WriteLine("  Step 2: ProcessPayment - SUCCESS");
    return true;
});

sagaCoordinator.CompleteSaga(saga2Id);
var saga2State = sagaCoordinator.GetSagaState(saga2Id);
Console.WriteLine($"Saga {saga2Id} status: {saga2State?.Status}");

// Step 3 - Exactly-Once Consumer Demo
Console.WriteLine("\n--- Exactly-Once Consumer Demo ---");
var exactlyOnceConsumer = new ExactlyOnceConsumer();

var testMessages = new (string msgId, string idemKey, string orderId)[]
{
    ("msg-001", "idem-001", "ORD-100"),
    ("msg-001", "idem-001", "ORD-100"), // Duplicate message ID
    ("msg-002", "idem-001", "ORD-100"), // Different msg ID, same idempotency key
    ("msg-003", "idem-002", "ORD-101"), // Unique message
    ("msg-004", "idem-002", "ORD-101"), // Same idempotency key as previous
};

foreach (var (msgId, idemKey, orderId) in testMessages)
{
    Console.WriteLine($"\nProcessing: msgId={msgId}, idemKey={idemKey}");

    await exactlyOnceConsumer.ProcessMessageAsync(
        messageId: msgId,
        idempotencyKey: idemKey,
        processAction: async () =>
        {
            Console.WriteLine($"    Executing business logic for {orderId}");
            await Task.Delay(10); // Simulate async work
            return $"processed:{orderId}";
        },
        sideEffect: async () =>
        {
            Console.WriteLine($"    Sending notification for {orderId}");
            await Task.Delay(5); // Simulate side effect
        });
}

// Step 4 - Full Integration with Surgewave Consumer
Console.WriteLine("\n--- Surgewave Integration Test ---");

// Produce test messages to topic
await producer.ProduceAsync(topic, "test-1", JsonSerializer.Serialize(new { Id = "msg-int-001", Key = "key-001", Data = "Test 1" }));
await producer.ProduceAsync(topic, "test-1", JsonSerializer.Serialize(new { Id = "msg-int-001", Key = "key-001", Data = "Test 1" })); // Duplicate
await producer.ProduceAsync(topic, "test-2", JsonSerializer.Serialize(new { Id = "msg-int-002", Key = "key-002", Data = "Test 2" }));

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "exactly-once-demo";
    options.EnableAutoCommit = false;
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await consumer.SubscribeAsync(default, topic);

var integrationConsumer = new ExactlyOnceConsumer();
var messageCount = 0;
var targetCount = outboxPublisher.PublishedCount + 3; // Outbox messages + test messages

Console.WriteLine($"Consuming {targetCount} messages from topic...");

while (messageCount < targetCount)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    messageCount++;

    try
    {
        var msg = JsonSerializer.Deserialize<JsonElement>(result.Value);
        var msgId = msg.TryGetProperty("Id", out var idProp) ? idProp.GetString() : $"offset-{result.Offset}";
        var idemKey = msg.TryGetProperty("Key", out var keyProp) ? keyProp.GetString() : msgId;

        await integrationConsumer.ProcessMessageAsync(
            messageId: msgId!,
            idempotencyKey: idemKey!,
            processAction: async () =>
            {
                Console.WriteLine($"  Surgewave message processed: {msgId}");
                return "ok";
            });
    }
    catch (JsonException)
    {
        // Handle non-JSON messages
        var msgId = $"raw-{result.Offset}";
        await integrationConsumer.ProcessMessageAsync(
            messageId: msgId,
            idempotencyKey: msgId,
            processAction: async () =>
            {
                Console.WriteLine($"  Raw message processed at offset {result.Offset}");
                return "ok";
            });
    }

    await consumer.CommitAsync(result);
}

// Final Statistics
Console.WriteLine("\n--- Final Statistics ---");
Console.WriteLine($"Outbox entries published: {outboxPublisher.PublishedCount}");
Console.WriteLine($"Messages processed: {exactlyOnceConsumer.ProcessedCount}");
Console.WriteLine($"Duplicates skipped: {exactlyOnceConsumer.DuplicatesSkipped}");
Console.WriteLine($"Effects executed: {exactlyOnceConsumer.EffectsExecuted}");
Console.WriteLine($"Effects skipped: {exactlyOnceConsumer.EffectsSkipped}");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Outbox pattern ensures events are published exactly once");
Console.WriteLine("2. Sagas coordinate multi-step transactions with compensation");
Console.WriteLine("3. Combine message ID + idempotency key for robust deduplication");
Console.WriteLine("4. Surgewave transactions enable atomic read-process-write");
Console.WriteLine("5. Defense in depth: multiple deduplication layers");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
Console.WriteLine();
Console.WriteLine("=== Congratulations! You have completed Unit 21: Message Deduplication ===");

// ==================== Helper Classes ====================

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
                var state = _sagas[sagaId];
                if (success)
                {
                    state.CompletedSteps.Add(stepName);
                }
                else
                {
                    _sagas[sagaId] = state with
                    {
                        Status = SagaStatus.Compensating,
                        Error = $"Step {stepName} failed"
                    };
                }
            }
            return success;
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                if (_sagas.TryGetValue(sagaId, out var state))
                {
                    _sagas[sagaId] = state with
                    {
                        Status = SagaStatus.Compensating,
                        Error = ex.Message
                    };
                }
            }
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

    public SagaState? GetSagaState(string sagaId)
    {
        lock (_lock)
        {
            return _sagas.TryGetValue(sagaId, out var state) ? state : null;
        }
    }
}

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
