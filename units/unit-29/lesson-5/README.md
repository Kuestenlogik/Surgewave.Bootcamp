# Lesson 11.5: Microservices Communication

> **Difficulty:** Advanced | **Duration:** 60 min | **Prerequisites:** Lesson 11.4

Implement microservices communication patterns including service mesh concepts and the saga pattern.

## Objectives

- Design event-driven microservices architecture
- Implement the saga pattern for distributed transactions
- Build service discovery and routing
- Handle failures with compensation

## Architecture

```
                     MICROSERVICES COMMUNICATION

    +----------------+     +----------------+     +----------------+
    |     Order      |     |   Inventory    |     |    Payment     |
    |    Service     |     |    Service     |     |    Service     |
    +-------+--------+     +-------+--------+     +-------+--------+
            |                      |                      |
            v                      v                      v
    +-------+----------------------+----------------------+-------+
    |                        Surgewave Topics                         |
    |  (commands, events, saga-state, dead-letter)               |
    +-------+----------------------+----------------------+-------+
            |                      |                      |
            v                      v                      v
    +-------+--------+     +-------+--------+     +-------+--------+
    |     Saga       |     |   Service      |     |    Dead        |
    |  Orchestrator  |     |   Registry     |     |   Letter       |
    +----------------+     +----------------+     +----------------+
```

## Concepts

### Saga Pattern

The saga pattern manages distributed transactions across services:

```
Order Saga:
1. CreateOrder -> Order Service
2. ReserveInventory -> Inventory Service
3. ProcessPayment -> Payment Service
4. ConfirmOrder -> Order Service

If any step fails, execute compensating transactions:
- ReleaseInventory (compensates ReserveInventory)
- RefundPayment (compensates ProcessPayment)
- CancelOrder (compensates CreateOrder)
```

### Saga States

| State | Description | Next Steps |
|-------|-------------|------------|
| STARTED | Saga initiated | Execute first step |
| INVENTORY_RESERVED | Inventory allocated | Process payment |
| PAYMENT_PROCESSED | Payment successful | Confirm order |
| COMPLETED | All steps done | None |
| COMPENSATING | Rollback in progress | Execute compensations |
| FAILED | Saga failed | None |

### Message Types

```csharp
// Commands
public record ReserveInventory(string SagaId, string OrderId, List<OrderItem> Items);
public record ProcessPayment(string SagaId, string OrderId, decimal Amount);
public record ConfirmOrder(string SagaId, string OrderId);

// Events
public record InventoryReserved(string SagaId, string OrderId);
public record InventoryReservationFailed(string SagaId, string OrderId, string Reason);
public record PaymentProcessed(string SagaId, string OrderId);
public record PaymentFailed(string SagaId, string OrderId, string Reason);

// Compensation commands
public record ReleaseInventory(string SagaId, string OrderId);
public record RefundPayment(string SagaId, string OrderId);
```

### Topics Structure

- `order-commands` - Order service commands
- `inventory-commands` - Inventory service commands
- `payment-commands` - Payment service commands
- `saga-events` - Saga state change events
- `dead-letter` - Failed messages for investigation

## Key Takeaways

- Sagas enable distributed transactions without two-phase commit
- Each service is autonomous and communicates via events
- Compensation logic handles failures gracefully
- Surgewave topics act as the communication backbone
