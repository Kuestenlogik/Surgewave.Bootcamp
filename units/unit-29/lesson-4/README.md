# Lesson 11.4: Event Sourcing

> **Difficulty:** Advanced | **Duration:** 60 min | **Prerequisites:** Lesson 11.3

Implement event sourcing with an event store, projections, and replay capabilities.

## Objectives

- Understand event sourcing principles
- Build an event store using Surgewave topics
- Create projections from event streams
- Implement event replay for state reconstruction

## Architecture

```
                           EVENT SOURCING SYSTEM

    +-------------+                              +-------------+
    |   Command   | -----> Commands ------>      |   Command   |
    |   Service   |                              |   Handler   |
    +-------------+                              +------+------+
                                                        |
                                                        v
                                               +--------+--------+
                                               |   Event Store   |
                                               | (events topic)  |
                                               +--------+--------+
                                                        |
                   +------------------------------------+------------------------------------+
                   |                    |                    |                              |
                   v                    v                    v                              v
           +-------+-------+    +-------+-------+    +-------+-------+            +--------+--------+
           |  Projection   |    |  Projection   |    |  Projection   |            |     Replay      |
           |   (Balance)   |    |  (History)    |    |  (Analytics)  |            |    Service      |
           +-------+-------+    +-------+-------+    +-------+-------+            +-----------------+
                   |                    |                    |
                   v                    v                    v
           +-------+-------+    +-------+-------+    +-------+-------+
           |   Read Model  |    |   Read Model  |    |   Read Model  |
           |  (Balances)   |    |  (Statements) |    |   (Reports)   |
           +---------------+    +---------------+    +---------------+
```

## Concepts

### Event Sourcing Principles

| Principle | Description |
|-----------|-------------|
| Events as Truth | Events are the source of truth, not current state |
| Immutability | Events are never modified or deleted |
| Append-Only | New events are always appended |
| Temporal Query | State at any point in time can be reconstructed |

### Event Model

```csharp
public record Event(
    string EventId,
    string AggregateId,
    string EventType,
    string Payload,
    int Version,
    DateTime Timestamp);

public record AccountCreated(string AccountId, string Owner, decimal InitialBalance);
public record MoneyDeposited(string AccountId, decimal Amount, string Description);
public record MoneyWithdrawn(string AccountId, decimal Amount, string Description);
public record AccountClosed(string AccountId, string Reason);
```

### Projection Types

| Type | Description | Example |
|------|-------------|---------|
| Current State | Latest state of an aggregate | Account balance |
| History | Timeline of changes | Transaction history |
| Analytics | Aggregated insights | Total deposits per day |

### Topics Structure

- `commands` - Incoming commands to process
- `events` - Committed domain events (event store)
- `projections-balance` - Current balance projections
- `projections-history` - Transaction history

## Key Takeaways

- Events capture business facts that have occurred
- Projections transform events into read-optimized views
- Replay enables state reconstruction and new projections
- Surgewave topics make excellent event stores with retention
