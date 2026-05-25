# Quiz: Lesson 11.1 - E-Commerce Pipeline

## Multiple Choice

### 1. What is the correct order of events in a typical order lifecycle?
- [ ] A) OrderPlaced -> OrderShipped -> PaymentProcessed -> OrderConfirmed
- [ ] B) OrderPlaced -> OrderValidated -> PaymentProcessed -> OrderConfirmed -> ShipmentCreated
- [ ] C) PaymentProcessed -> OrderPlaced -> OrderValidated -> OrderConfirmed
- [ ] D) OrderConfirmed -> OrderPlaced -> PaymentProcessed -> ShipmentCreated

<details>
<summary>Answer</summary>
**B)** The correct lifecycle is: OrderPlaced -> OrderValidated -> PaymentProcessed -> OrderConfirmed -> ShipmentCreated -> OrderShipped -> OrderDelivered. Each step must complete before the next can proceed.
</details>

### 2. In an event-driven e-commerce architecture, how are customer notifications triggered?
- [ ] A) Direct API calls from the order service
- [ ] B) By polling a notifications table
- [ ] C) By subscribing to domain events on a notifications topic
- [ ] D) By scheduled batch jobs

<details>
<summary>Answer</summary>
**C)** Notifications are triggered by domain events. The notification service subscribes to relevant events (like OrderConfirmed, OrderShipped) and sends notifications when those events occur, enabling loose coupling.
</details>

### 3. Which event indicates that stock has been allocated for an order?
- [ ] A) OrderPlaced
- [ ] B) InventoryLow
- [ ] C) InventoryReserved
- [ ] D) OrderValidated

<details>
<summary>Answer</summary>
**C)** InventoryReserved indicates that stock has been successfully allocated for an order. This event allows the order flow to continue, knowing the items are available.
</details>

## True/False

### 4. In event-driven architecture, each service owns its domain and communicates only via events.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Event-driven architecture promotes loose coupling where services don't call each other directly. Instead, they publish events when state changes and subscribe to events they're interested in.
</details>

### 5. Order state cannot be reconstructed from the event stream.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - A key benefit of event-driven architecture is that order state CAN be reconstructed by replaying the event stream. This enables event sourcing patterns, debugging, and audit trails.
</details>

## Code Challenge

### 6. Design the event flow for this scenario: A customer places an order, but payment fails.

What events should be emitted and in what order?

<details>
<summary>Answer</summary>
Event flow for a failed payment scenario:

1. **OrderPlaced** - Customer submits the order
2. **OrderValidated** - Order passes validation (items exist, address valid, etc.)
3. **InventoryReserved** - Stock is allocated for the order
4. **PaymentFailed** - Payment processing fails (insufficient funds, card declined, etc.)
5. **InventoryReleased** - Compensating action to release the reserved stock
6. **OrderCancelled** - Order is marked as cancelled
7. **NotificationSent** - Customer is notified about the payment failure

The key insight is that PaymentFailed triggers compensating events (InventoryReleased) to undo previous actions.
</details>

### 7. What's wrong with this e-commerce event handler?

```csharp
public async Task HandleOrderPlaced(OrderPlaced orderEvent)
{
    // Validate order
    var isValid = await ValidateOrder(orderEvent.Order);

    if (isValid)
    {
        // Reserve inventory (direct call!)
        await _inventoryService.ReserveStock(orderEvent.Order.Items);

        // Process payment (direct call!)
        await _paymentService.ProcessPayment(orderEvent.Order.PaymentInfo);

        // Confirm order (direct call!)
        await _orderService.ConfirmOrder(orderEvent.Order.Id);
    }
}
```

<details>
<summary>Answer</summary>
Multiple issues violate event-driven architecture principles:

1. **Direct service calls instead of events** - This creates tight coupling. If the inventory service is down, the entire order fails.

2. **No compensation handling** - If payment fails after inventory is reserved, the inventory stays reserved forever.

3. **Single point of failure** - One handler doing everything means one failure breaks the entire flow.

Fix using events:
```csharp
public async Task HandleOrderPlaced(OrderPlaced orderEvent)
{
    var isValid = await ValidateOrder(orderEvent.Order);

    if (isValid)
    {
        // Emit event - let inventory service handle it
        await _producer.ProduceAsync("order-events",
            new OrderValidated(orderEvent.Order.Id));
    }
    else
    {
        await _producer.ProduceAsync("order-events",
            new OrderValidationFailed(orderEvent.Order.Id, "Validation failed"));
    }
}

// Separate handlers in respective services:
// InventoryService listens to OrderValidated -> emits InventoryReserved
// PaymentService listens to InventoryReserved -> emits PaymentProcessed/Failed
// Each service handles its own compensating logic
```
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
