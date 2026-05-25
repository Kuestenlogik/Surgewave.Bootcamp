# Knowledge Assessment: Lesson 11.5 - Microservices Communication

## Multiple Choice

### 1. What is the Saga pattern used for?
- [ ] A) Service discovery
- [ ] B) Managing distributed transactions across services
- [ ] C) Load balancing
- [ ] D) Message compression

<details>
<summary>Answer</summary>
**B)** The Saga pattern manages distributed transactions across multiple services without using two-phase commit. It coordinates a sequence of local transactions with compensating actions for failures.
</details>

### 2. What happens when a step in a saga fails?
- [ ] A) The entire saga is retried from the beginning
- [ ] B) Compensating transactions are executed to undo previous steps
- [ ] C) The failure is ignored and the saga continues
- [ ] D) The database is rolled back automatically

<details>
<summary>Answer</summary>
**B)** When a step fails, compensating transactions are executed in reverse order to undo the effects of previous steps. For example, if payment fails after inventory was reserved, a ReleaseInventory compensation is executed.
</details>

### 3. In the order saga, what is the compensating action for ReserveInventory?
- [ ] A) CancelOrder
- [ ] B) RefundPayment
- [ ] C) ReleaseInventory
- [ ] D) ProcessPayment

<details>
<summary>Answer</summary>
**C)** ReleaseInventory is the compensating action that undoes ReserveInventory. If a later step fails (e.g., payment), the reserved inventory must be released back to available stock.
</details>

## True/False

### 4. Sagas eliminate the need for distributed two-phase commit protocols.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Sagas provide eventual consistency without the complexity and performance overhead of two-phase commit. Each service commits locally, and compensations handle failures.
</details>

### 5. In the saga pattern, all services must use the same database for atomicity.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Each service is autonomous and owns its own data. Services communicate via events, not shared databases. This is a key benefit of sagas - services remain independently deployable.
</details>

## Code Challenge

### 6. Design the compensation flow for this failed saga:

```
Saga Steps Completed:
1. CreateOrder - SUCCESS
2. ReserveInventory - SUCCESS
3. ProcessPayment - FAILED

What compensations should execute?
```

<details>
<summary>Answer</summary>
When ProcessPayment fails, compensations execute in reverse order for all successful steps:

```
Compensation Flow:
1. ReleaseInventory - Undo step 2 (free the reserved stock)
2. CancelOrder - Undo step 1 (mark order as cancelled)

Note: No compensation needed for ProcessPayment since it failed (nothing to undo)
```

In code:
```csharp
public async Task CompensateSaga(string sagaId, SagaState state)
{
    // Compensate in reverse order
    if (state.InventoryReserved)
    {
        await _producer.ProduceAsync("inventory-commands",
            new ReleaseInventory(sagaId, state.OrderId));
    }

    if (state.OrderCreated)
    {
        await _producer.ProduceAsync("order-commands",
            new CancelOrder(sagaId, state.OrderId, "Payment failed"));
    }

    // Update saga state
    await UpdateSagaState(sagaId, SagaStatus.FAILED);
}
```
</details>

### 7. What's wrong with this saga orchestrator implementation?

```csharp
public async Task ExecuteOrderSaga(Order order)
{
    // Step 1: Create order
    await _orderService.CreateOrder(order);

    // Step 2: Reserve inventory
    await _inventoryService.Reserve(order.Items);

    // Step 3: Process payment
    await _paymentService.Process(order.Payment);

    // Step 4: Confirm
    await _orderService.Confirm(order.Id);
}
```

<details>
<summary>Answer</summary>
Multiple issues:

1. **Direct service calls instead of events** - Creates tight coupling and synchronous dependencies. If one service is slow, the entire saga blocks.

2. **No compensation handling** - If step 3 fails, steps 1 and 2 are not rolled back.

3. **No saga state tracking** - Can't resume or debug failed sagas.

4. **No idempotency** - Retrying could duplicate operations.

Fix using events and saga state:
```csharp
public class OrderSagaOrchestrator
{
    public async Task StartSaga(Order order)
    {
        var sagaId = Guid.NewGuid().ToString();

        // Initialize saga state
        var state = new SagaState
        {
            SagaId = sagaId,
            OrderId = order.Id,
            Status = SagaStatus.STARTED
        };
        await _sagaStore.SaveAsync(state);

        // Emit first command (async, event-driven)
        await _producer.ProduceAsync("order-commands",
            new CreateOrder(sagaId, order));
    }

    // Handle events from services
    public async Task HandleOrderCreated(OrderCreated evt)
    {
        var state = await _sagaStore.GetAsync(evt.SagaId);
        state.OrderCreated = true;

        // Next step
        await _producer.ProduceAsync("inventory-commands",
            new ReserveInventory(evt.SagaId, state.OrderId, state.Items));
    }

    public async Task HandlePaymentFailed(PaymentFailed evt)
    {
        var state = await _sagaStore.GetAsync(evt.SagaId);
        state.Status = SagaStatus.COMPENSATING;
        await _sagaStore.SaveAsync(state);

        // Start compensation
        await _producer.ProduceAsync("inventory-commands",
            new ReleaseInventory(evt.SagaId, state.OrderId));
    }
}
```
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
