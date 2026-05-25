// Surgewave Bootcamp - Unit 10, Lesson 5: Schema in Practice
//
// In this lesson, you'll learn to:
// - Build schema-aware producers and consumers
// - Handle multiple schema versions
// - Implement graceful schema evolution
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 10.5 - Schema in Practice ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// TODO: Create schema registry and register schemas
// Hint: var registry = new SchemaRegistry();
// Hint: registry.RegisterSchema("user-events-value", v1Schema);

// TODO: Create SchemaAwareProducer
// Hint: var producer = new SchemaAwareProducer(surgewaveProducer, registry);

// TODO: Produce valid messages
// Hint: await producer.ProduceAsync("user-events", "user-1", """{"id":1,"name":"Alice"}""");

// TODO: Try producing invalid messages (should be rejected)
// Hint: await producer.ProduceAsync("user-events", "user-2", """{"invalid":"data"}""");

// TODO: Create SchemaAwareConsumer
// Hint: var consumer = new SchemaAwareConsumer(surgewaveConsumer, registry);

// TODO: Consume and validate messages
// Hint: var message = await consumer.ConsumeAsync(TimeSpan.FromSeconds(1));

// TODO: Register schema v2 and produce messages with new schema
// Hint: registry.RegisterSchema("user-events-value", v2Schema);

// TODO: Consume messages with different schema versions
// Hint: Handle both v1 and v2 messages

// TODO: Implement dead-letter queue for invalid messages
// Hint: Route invalid messages to "user-events-dlq" topic

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Message Types ==============

public record ConsumedMessage(
    string Topic,
    string? Key,
    string Value,
    int SchemaId,
    bool IsValid,
    List<string> ValidationErrors
);

// TODO: Implement SchemaAwareProducer
// - Cache schema validators per subject
// - Validate before producing
// - Prepend schema ID to messages

// TODO: Implement SchemaAwareConsumer
// - Extract schema ID from messages
// - Look up and cache validators
// - Return validation status with messages

// TODO: Implement SchemaRegistry (or reuse from previous lessons)
// TODO: Implement JsonSchemaValidator (or reuse from previous lessons)
