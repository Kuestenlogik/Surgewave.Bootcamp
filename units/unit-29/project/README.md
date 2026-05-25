# Unit 11 Project: E-Commerce Backend

Build a complete e-commerce event processing system with Order, Payment, and Shipping event flows.

## Scenario

You're building the event backbone for an e-commerce platform. When a customer places an order:
1. An `OrderCreated` event is published
2. The Payment service processes payment and emits `PaymentProcessed` or `PaymentFailed`
3. On successful payment, the Shipping service emits `ShipmentCreated`
4. All events are tracked for analytics

## Requirements

### Functional Requirements

- [ ] Produce `OrderCreated` events with order details
- [ ] Process orders and emit `PaymentProcessed` or `PaymentFailed` events
- [ ] On successful payment, emit `ShipmentCreated` events
- [ ] Track all events for real-time analytics dashboard
- [ ] Handle payment failures with retry logic

### Technical Requirements

- [ ] Use correlation IDs to track orders across services
- [ ] Implement proper error handling and dead letter queue
- [ ] Use consumer groups for scalable processing
- [ ] Ensure exactly-once semantics for payment processing

## Event Schemas

```csharp
public record OrderCreated(
    string OrderId,
    string CustomerId,
    List<OrderItem> Items,
    decimal TotalAmount,
    DateTime CreatedAt,
    string CorrelationId
);

public record PaymentProcessed(
    string PaymentId,
    string OrderId,
    decimal Amount,
    PaymentStatus Status,
    DateTime ProcessedAt,
    string CorrelationId
);

public record ShipmentCreated(
    string ShipmentId,
    string OrderId,
    string Address,
    DateTime EstimatedDelivery,
    string CorrelationId
);

public record OrderItem(string ProductId, string Name, int Quantity, decimal Price);

public enum PaymentStatus { Approved, Declined, Pending }
```

## Topic Structure

```
orders-created       -> OrderCreated events
payments-processed   -> PaymentProcessed events
payments-failed      -> PaymentFailed events (DLQ)
shipments-created    -> ShipmentCreated events
events-analytics     -> All events for analytics
```

## Expected Output

```
=== E-Commerce Event Processing System ===

[Order Service] Starting...
[Order Service] Order ORD-001 created for customer C-123 ($299.99)
[Order Service] Order ORD-002 created for customer C-456 ($1,250.00)
[Order Service] Order ORD-003 created for customer C-789 ($49.99)

[Payment Service] Processing payment for Order ORD-001...
[Payment Service] Payment PAY-001 APPROVED for Order ORD-001 ($299.99)
[Payment Service] Processing payment for Order ORD-002...
[Payment Service] Payment PAY-002 DECLINED for Order ORD-002 - Insufficient funds
[Payment Service] Order ORD-002 sent to retry queue
[Payment Service] Processing payment for Order ORD-003...
[Payment Service] Payment PAY-003 APPROVED for Order ORD-003 ($49.99)

[Shipping Service] Creating shipment for Order ORD-001...
[Shipping Service] Shipment SHIP-001 created - ETA: 2025-01-28
[Shipping Service] Creating shipment for Order ORD-003...
[Shipping Service] Shipment SHIP-002 created - ETA: 2025-01-29

[Analytics] Events processed: 7
[Analytics] Orders: 3, Payments: 3, Shipments: 2

=== Processing Complete ===
```

## Getting Started

```bash
cd bootcamp/unit-11/project/start
dotnet run
```

## Hints

1. Use `CorrelationId` to trace events across services
2. Simulate payment failures for orders > $1000
3. Use `Task.WhenAll` for parallel service processing
4. Dead letter queue pattern for failed payments

## Evaluation Criteria

| Criteria | Points |
|----------|--------|
| Order events produced correctly | 20 |
| Payment processing with success/failure | 25 |
| Shipment creation on payment success | 20 |
| Analytics tracking all events | 15 |
| Error handling and DLQ | 20 |

## Solution

Compare your implementation with [completed/](completed/README.md) when done.
