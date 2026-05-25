# Unit 5 Project: Kafka to Surgewave Migration Tool

Build a migration tool that demonstrates migrating from Apache Kafka to Surgewave using the compatibility layer.

## Scenario

Your company has been running Apache Kafka for years. You've decided to migrate to Surgewave for its simplicity and native .NET support. This project demonstrates how to:

1. Use the Kafka-compatible API to run existing code unchanged
2. Migrate data between Kafka and Surgewave topics
3. Validate that all messages were migrated correctly
4. Generate a migration report

## Requirements

### Functional Requirements

- [ ] Connect to Surgewave using Kafka-compatible API (ProducerConfig/ConsumerConfig)
- [ ] Produce sample "legacy" Kafka messages to source topic
- [ ] Consume messages using Kafka-compatible consumer API
- [ ] Produce migrated messages to destination topic
- [ ] Validate message counts and content integrity
- [ ] Generate migration summary report

### Technical Requirements

- [ ] Use `SurgewaveProducer<TKey, TValue>` with Kafka-compatible configuration
- [ ] Use `SurgewaveConsumer<TKey, TValue>` with consumer groups
- [ ] Handle message headers during migration
- [ ] Track migration progress with statistics
- [ ] Implement graceful shutdown

## Data Schema

```csharp
// Legacy Kafka message format
public record LegacyOrderMessage(
    string OrderId,
    string CustomerId,
    decimal Amount,
    string Status,
    long Timestamp
);

// Migration metadata
public record MigrationStats(
    int TotalMessages,
    int MigratedMessages,
    int FailedMessages,
    TimeSpan Duration
);
```

## Expected Output

```
=== Kafka to Surgewave Migration Tool ===

[Setup] Starting embedded Surgewave broker...
[Setup] Broker ready on port 9092

[Phase 1] Creating legacy Kafka-style messages...
  Producing 100 orders to 'legacy-orders' topic...
  Produced: order-001, order-002, ... order-100
  Total produced: 100 messages

[Phase 2] Migrating messages to Surgewave...
  Consuming from 'legacy-orders'...
  Validating and transforming...
  Producing to 'surgewave-orders'...
  Progress: 25/100 (25%)
  Progress: 50/100 (50%)
  Progress: 75/100 (75%)
  Progress: 100/100 (100%)

[Phase 3] Validating migration...
  Source topic: 100 messages
  Target topic: 100 messages
  Validation: PASSED

=== Migration Report ===
Source Topic:      legacy-orders
Target Topic:      surgewave-orders
Messages Migrated: 100
Failed:            0
Duration:          1.23s
Throughput:        81.3 msg/sec
Status:            SUCCESS

=== Migration Complete ===
```

## Getting Started

```bash
cd bootcamp/unit-5/project/start
dotnet run
```

## Hints

1. Use `SurgewaveProducer<string, string>` for Kafka-compatible producing
2. Use `SurgewaveConsumer<string, string>` with `GroupId` for consuming
3. Serialize/deserialize using `System.Text.Json`
4. Track timestamps to calculate throughput
5. Use `Stopwatch` for accurate duration measurement

## Evaluation Criteria

| Criteria | Points |
|----------|--------|
| Kafka-compatible API used correctly | 25 |
| All messages migrated | 25 |
| Validation passes | 20 |
| Statistics are accurate | 15 |
| Clean code structure | 15 |

## Solution

Compare your implementation with [completed/](completed/README.md) when done.
