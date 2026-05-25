# Lesson 21.5: End-to-End Exactly-Once

> **Difficulty:** Advanced | **Duration:** 60 min | **Prerequisites:** Lesson 21.4

## Overview

Learn how to achieve end-to-end exactly-once processing by combining transactional patterns, the outbox pattern, and saga coordination. This lesson covers the most robust approaches to preventing duplicate processing in distributed systems.

## Learning Objectives

- Implement the transactional outbox pattern
- Coordinate distributed sagas with idempotency
- Use Surgewave transactions for exactly-once semantics
- Combine multiple deduplication strategies
- Handle failure scenarios gracefully

## Exactly-Once Semantics Overview

| Layer | Mechanism | Details |
|-------|-----------|---------|
| **Producer** | Idempotent Producer (`enable.idempotence=true`) | Producer ID + sequence number per partition; broker deduplicates based on sequence; handles network retries transparently |
| **Broker** | Transactional Writes | Atomic commit across multiple partitions; transaction markers for commit/abort; consumer isolation levels (`read_committed`) |
| **Consumer** | Idempotent Processing | Deduplication stores; idempotent operations; transactional offset commits |

End-to-End = all three layers working together.

## Transactional Outbox Pattern

```mermaid
graph TD
    subgraph Service["Service"]
        BL["Business Logic\n(Process Order, Validate Payment,\nCalculate Total)"] --> TX["BEGIN TRANSACTION\nUPDATE orders …\nINSERT INTO outbox (event_data)\nCOMMIT"]
    end
    subgraph Publisher["Outbox Publisher (separate process/thread)"]
        PUB["SELECT * FROM outbox WHERE published = false\n→ Produce to Surgewave topic\n→ UPDATE outbox SET published = true"]
    end
    TX --> PUB
```

**Outbox Table:**

| id | event_type | payload | published | ts |
|----|-----------|---------|-----------|-----|
| 1 | OrderCreated | `{"orderId": "123"…}` | false | … |
| 2 | PaymentReceived | `{"paymentId": "456"}` | false | … |

## Saga Pattern with Compensation

Order Saga: Create Order → Reserve Inventory → Charge Payment

```mermaid
graph LR
    subgraph Success["Success Path"]
        S1[Start Saga] --> S2["CreateOrder\n(step 1)"] --> S3["Reserve Inventory\n(step 2)"] --> S4["Charge Payment\n(step 3)"] --> S5[Complete Saga]
    end
    subgraph Failure["Failure Path (Payment fails)"]
        F1[Start Saga] --> F2["CreateOrder\n(step 1)"] --> F3["Reserve Inventory\n(step 2)"] --> F4["Charge Payment\nFAILED"]
        F4 --> F5["Release Inventory\n(compensate)"] --> F6["Cancel Order\n(compensate)"]
    end
```

**Saga State** (stored in database):

| saga_id | status | current_step | data | ts |
|---------|--------|-------------|------|----|
| saga-1 | in_progress | 3 | `{"orderId":…}` | … |
| saga-2 | compensating | 2 | `{"orderId":…}` | … |
| saga-3 | completed | 3 | `{"orderId":…}` | … |

## Read-Process-Write Pattern

```mermaid
graph TD
    A["BEGIN Surgewave TRANSACTION"] --> B["1. Read from Input Topic (orders)\nMessage: {orderId: '123', …}"]
    B --> C["2. Process Message\n(transform, business logic, generate output)"]
    C --> D["3. Write to Output Topic (results) transactionally\nMessage: {status: 'processed'…}"]
    D --> E["4. Commit Offsets transactionally\n(atomic with output — if fails, nothing committed)"]
    E --> F["COMMIT TRANSACTION"]
```

**On Failure/Restart:** transaction aborted (nothing committed); consumer resumes from last committed offset; message is reprocessed (exactly-once effective processing).

## Combined Strategy

```mermaid
graph LR
    subgraph Source["Source System"]
        DB1["Database + Outbox"] --> PUB["Outbox Publisher"]
    end
    subgraph SurgewaveLayer["Surgewave (EOS)"]
        T["Topic: orders"] --> EOS["Surgewave EOS"]
    end
    subgraph Sink["Sink System"]
        DB2["Database + Dedup Store"] --> IC["Idempotent Consumer"]
    end
    PUB --> T
    EOS --> DB2
```

**Guarantees at each layer:**

| Layer | Guarantees |
|-------|-----------|
| **Source (Outbox)** | Business operation + event write in same DB transaction; publisher retries until delivered; marks published only after successful produce |
| **Surgewave (EOS)** | Idempotent producer prevents duplicate messages; transactions ensure atomic multi-partition writes; `read_committed` isolation for consumers |
| **Sink (Idempotent Consumer)** | Deduplication store tracks processed message IDs; idempotent database operations (upserts); transactional dedup + business operation |

## Best Practices

| Practice | Description |
|----------|-------------|
| Use outbox for external events | Never publish directly from business logic |
| Store saga state durably | Enable recovery after crashes |
| Combine dedup with transactions | Defense in depth |
| Set appropriate timeouts | Handle stuck transactions |
| Monitor transaction metrics | Track commit/abort rates |

## Exercise

In this exercise, you will:

1. Implement the transactional outbox pattern
2. Create a simple saga coordinator
3. Use Surgewave transactions for exactly-once
4. Combine all deduplication strategies
5. Test failure and recovery scenarios

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
