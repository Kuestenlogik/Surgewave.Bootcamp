# Lesson 1.6: Message Best Practices

> **Difficulty:** Beginner | **Duration:** 30 min | **Prerequisites:** Lesson 1.5

## Overview

In this lesson, you'll learn fundamental messaging best practices for building robust event-driven systems. These patterns help ensure your systems are maintainable, traceable, and resilient.

## Learning Objectives

By the end of this lesson, you will:

1. Understand why messages should be immutable
2. Distinguish between Events and Commands
3. Apply proper message naming conventions
4. Use correlation IDs for distributed tracing
5. Understand idempotency basics
6. Design effective message payloads

## Key Concepts

### Message Immutability

Messages should never change after creation. Use C# records to enforce this:

```csharp
// Immutable event using record
public record OrderPlaced(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    DateTimeOffset PlacedAt,
    Guid CorrelationId);
```

**Why immutability matters:**
- Enables safe concurrent processing
- Simplifies debugging (message never changes)
- Supports event sourcing patterns
- Prevents accidental state corruption

### Events vs Commands

| Aspect | Event | Command |
|--------|-------|---------|
| **Tense** | Past tense (something happened) | Imperative (request action) |
| **Examples** | `OrderPlaced`, `UserCreated`, `PaymentProcessed` | `PlaceOrder`, `CreateUser`, `ProcessPayment` |
| **Semantics** | Fact - cannot be rejected | Request - can be accepted or rejected |
| **Audience** | Multiple subscribers | Single handler |
| **Coupling** | Publisher doesn't know subscribers | Sender knows the handler exists |

### Event Examples

```csharp
// Events describe facts that occurred
public record OrderPlaced(Guid OrderId, Guid CustomerId, decimal Total, DateTimeOffset PlacedAt, Guid CorrelationId);
public record UserCreated(Guid UserId, string Email, DateTimeOffset CreatedAt, Guid CorrelationId);
public record PaymentProcessed(Guid PaymentId, Guid OrderId, decimal Amount, DateTimeOffset ProcessedAt, Guid CorrelationId);
public record InventoryReserved(Guid ReservationId, Guid ProductId, int Quantity, Guid CorrelationId);
```

### Command Examples

```csharp
// Commands request that something happen
public record PlaceOrder(Guid CustomerId, List<OrderItem> Items, Guid CorrelationId);
public record CreateUser(string Email, string Name, Guid CorrelationId);
public record ProcessPayment(Guid OrderId, decimal Amount, string PaymentMethod, Guid CorrelationId);
public record ReserveInventory(Guid ProductId, int Quantity, Guid CorrelationId);
```

### Correlation IDs

A correlation ID links related messages across services:

```
Request Flow:
[PlaceOrder] -----> [OrderPlaced] -----> [PaymentProcessed]
   CorrelationId: abc-123          CorrelationId: abc-123
```

**Benefits:**
- Trace requests across distributed systems
- Debug production issues
- Build comprehensive audit trails
- Monitor end-to-end latency

### Message Naming Conventions

| Pattern | Example | Usage |
|---------|---------|-------|
| `{Entity}{Action}` (Event) | `OrderPlaced`, `UserCreated` | Past tense verb |
| `{Action}{Entity}` (Command) | `PlaceOrder`, `CreateUser` | Imperative verb |
| Version suffix | `OrderPlacedV2` | Schema evolution |
| Domain prefix | `Sales.OrderPlaced` | Large systems |

### Payload Design Principles

**Include what consumers need:**
```csharp
// Good: Contains all necessary data
public record OrderPlaced(
    Guid OrderId,
    Guid CustomerId,
    string CustomerEmail,     // Included for notifications
    decimal TotalAmount,
    List<OrderItem> Items,    // Included for fulfillment
    DateTimeOffset PlacedAt,
    Guid CorrelationId);
```

**Avoid including everything:**
```csharp
// Bad: Too much data, tight coupling
public record OrderPlaced(
    Guid OrderId,
    Customer FullCustomerObject,  // Don't include entire entities
    PaymentDetails PaymentInfo,   // Not relevant to all consumers
    // ... hundreds of fields
);
```

### Idempotency Preview

Idempotency ensures processing a message multiple times has the same effect as processing it once:

```csharp
// Include a unique message ID for deduplication
public record OrderPlaced(
    Guid MessageId,        // Unique per message instance
    Guid OrderId,
    Guid CorrelationId,
    // ... other fields
);
```

We'll explore idempotency in depth in Unit 3.

## Prerequisites

- Completed Lesson 1.5 (Partitioning)

## Getting Started

1. Navigate to the `start` folder
2. Open `Program.cs` and follow the TODO comments
3. Reference `INSTRUCTIONS.md` for detailed guidance

## Verification

Run the completed lesson:

```bash
cd completed
dotnet run
```

Expected output:
```
=== Surgewave Bootcamp: Lesson 1.6 - Message Best Practices ===

Starting embedded Surgewave broker...
Broker started on localhost:9092

--- Demonstrating Events vs Commands ---

Publishing command: PlaceOrder
  CorrelationId: abc-123...
  CustomerId: cust-456...

Order placed! Publishing event: OrderPlaced
  CorrelationId: abc-123... (same!)
  OrderId: order-789...

--- Demonstrating Immutability ---

Attempting to modify a record...
Records are immutable - you get a new instance with 'with'
  Original total: 99.99
  Modified total: 149.99 (new instance)

--- Demonstrating Correlation ID Tracing ---

Simulating order flow with CorrelationId: ...
  1. PlaceOrder command received
  2. OrderPlaced event published
  3. PaymentProcessed event published
  4. InventoryReserved event published
  All events share the same CorrelationId!

--- Demonstrating Idempotent Processing ---

Processing order order-123 (attempt 1)... Processed!
Processing order order-123 (attempt 2)... Already processed, skipped.
Processing order order-123 (attempt 3)... Already processed, skipped.
Idempotency prevents duplicate processing!

Shutting down...
Goodbye!
```

## Next Steps

You've completed the fundamentals of Unit 1! Continue to [Unit 3: Testing Surgewave](../../unit-3/README.md) to learn about:
- Message headers and metadata
- Transactions
- Schema Registry
- And more!
