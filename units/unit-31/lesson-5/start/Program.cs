// Surgewave Bootcamp - Unit 21, Lesson 5: End-to-End Exactly-Once
//
// In this lesson, you'll learn:
// - Implement the transactional outbox pattern
// - Coordinate distributed sagas with idempotency
// - Combine multiple deduplication strategies
// - Handle failure scenarios gracefully
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

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

// TODO: Step 1 - Implement the Outbox Pattern
// public class OutboxStore
// {
//     private readonly List<OutboxEntry> _entries = new();
//     private int _nextId = 1;
//     private readonly object _lock = new();
//
//     public record OutboxEntry(int Id, string EventType, string Payload, bool Published, DateTime CreatedAt);
//
//     public int AddEntry(string eventType, string payload)
//     {
//         lock (_lock)
//         {
//             var entry = new OutboxEntry(_nextId++, eventType, payload, false, DateTime.UtcNow);
//             _entries.Add(entry);
//             return entry.Id;
//         }
//     }
//
//     public List<OutboxEntry> GetUnpublishedEntries()
//     {
//         lock (_lock) { return _entries.Where(e => !e.Published).OrderBy(e => e.Id).ToList(); }
//     }
//
//     public void MarkAsPublished(int id)
//     {
//         lock (_lock)
//         {
//             var index = _entries.FindIndex(e => e.Id == id);
//             if (index >= 0) _entries[index] = _entries[index] with { Published = true };
//         }
//     }
// }

// TODO: Step 2 - Create an Outbox Publisher
// var outbox = new OutboxStore();
// Console.WriteLine("\n--- Outbox Pattern Demo ---");
//
// // Simulate business transactions that write to outbox
// outbox.AddEntry("OrderCreated", JsonSerializer.Serialize(new { OrderId = "ORD-001", Amount = 99.99 }));
// Console.WriteLine("Added OrderCreated event to outbox");
// outbox.AddEntry("PaymentReceived", JsonSerializer.Serialize(new { PaymentId = "PAY-001", Amount = 99.99 }));
// Console.WriteLine("Added PaymentReceived event to outbox");
//
// // Publish outbox entries
// await using var producer = new SurgewaveProducer<string, string>(options =>
// {
//     options.BootstrapServers = $"localhost:{runtime.Port}";
// });
//
// foreach (var entry in outbox.GetUnpublishedEntries())
// {
//     await producer.ProduceAsync(topic, entry.Id.ToString(), entry.Payload);
//     outbox.MarkAsPublished(entry.Id);
//     Console.WriteLine($"Published outbox entry {entry.Id}: {entry.EventType}");
// }

// TODO: Step 3 - Implement Saga Coordinator
// public class SagaCoordinator
// {
//     public enum SagaStatus { Started, InProgress, Compensating, Completed, Failed }
//     private readonly Dictionary<string, SagaStatus> _sagas = new();
//
//     public string StartSaga() { var id = Guid.NewGuid().ToString("N")[..8]; _sagas[id] = SagaStatus.Started; return id; }
//     public void SetStatus(string sagaId, SagaStatus status) { _sagas[sagaId] = status; }
//     public SagaStatus GetStatus(string sagaId) => _sagas.TryGetValue(sagaId, out var s) ? s : SagaStatus.Failed;
// }

// TODO: Step 4 - Implement Exactly-Once Consumer
// public class ExactlyOnceConsumer
// {
//     private readonly HashSet<string> _processedIds = new();
//     private readonly HashSet<string> _idempotencyKeys = new();
//
//     public bool TryProcess(string messageId, string idempotencyKey, Action processAction)
//     {
//         if (_processedIds.Contains(messageId)) return false;
//         if (_idempotencyKeys.Contains(idempotencyKey)) return false;
//
//         _processedIds.Add(messageId);
//         processAction();
//         _idempotencyKeys.Add(idempotencyKey);
//         return true;
//     }
// }

// TODO: Step 5 - Test the complete pipeline with duplicates
// var consumer = new ExactlyOnceConsumer();
//
// var messages = new[]
// {
//     ("msg-001", "idem-001", "ORD-100"),
//     ("msg-001", "idem-001", "ORD-100"), // Duplicate message ID
//     ("msg-002", "idem-001", "ORD-100"), // Same idempotency key
//     ("msg-003", "idem-002", "ORD-101"), // New unique message
// };
//
// Console.WriteLine("\n--- Exactly-Once Consumer Demo ---");
// foreach (var (msgId, idemKey, orderId) in messages)
// {
//     var processed = consumer.TryProcess(msgId, idemKey, () =>
//     {
//         Console.WriteLine($"  Processing order {orderId}");
//     });
//     Console.WriteLine($"Message {msgId}: {(processed ? "PROCESSED" : "SKIPPED (duplicate)")}");
// }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Outbox pattern ensures events are published exactly once");
Console.WriteLine("2. Sagas coordinate multi-step transactions with compensation");
Console.WriteLine("3. Combine message ID + idempotency key for robust deduplication");
Console.WriteLine("4. Surgewave transactions enable atomic read-process-write");
Console.WriteLine("5. Defense in depth: multiple deduplication layers");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
