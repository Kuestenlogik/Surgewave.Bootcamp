# Knowledge Assessment: Lesson 12.5 - Integration Patterns

## Multiple Choice

### 1. What is the purpose of using a Subject as a bridge?
- [ ] A) To improve performance of observables
- [ ] B) To connect imperative code that pushes values with reactive code that subscribes
- [ ] C) To automatically handle errors
- [ ] D) To compress data streams

<details>
<summary>Answer</summary>
**B)** A Subject acts as a bridge between imperative and reactive worlds. Imperative code can push values via OnNext(), while reactive code can subscribe to receive those values. This enables gradual adoption of reactive patterns.
</details>

### 2. Which pattern would you use to combine messages from multiple Surgewave topics into a single stream?
- [ ] A) Fork
- [ ] B) Subject Bridge
- [ ] C) Merge
- [ ] D) Request-Response

<details>
<summary>Answer</summary>
**C)** Merge combines multiple observable sources into a single stream. This is useful when you want to process messages from several topics with the same processing logic.
</details>

### 3. What does the Fork (One-to-Many) pattern enable?
- [ ] A) Combining multiple sources
- [ ] B) Multiple independent subscribers to the same source
- [ ] C) Converting request/response to observables
- [ ] D) Periodic polling

<details>
<summary>Answer</summary>
**B)** The Fork pattern allows multiple independent subscribers to receive messages from a single source. Each subscriber processes the messages independently.
</details>

## True/False

### 4. Observable.FromAsync can be used to convert async/await methods into observables.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Observable.FromAsync creates an observable from an async method, bridging async/await code with reactive streams. This is useful for integrating HTTP calls, database queries, or other async operations into reactive pipelines.
</details>

### 5. In a reactive pipeline, enrichment (adding data from external sources) should always be done synchronously.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Enrichment in reactive pipelines is typically done asynchronously using SelectMany or similar operators that can handle async operations. This prevents blocking the pipeline while waiting for external data sources.
</details>

## Code Challenge

### 6. What issue exists in this merge pattern implementation?

```csharp
var ordersObservable = GetSurgewaveObservable("orders");
var paymentsObservable = GetSurgewaveObservable("payments");

ordersObservable.Subscribe(msg => ProcessMessage(msg));
paymentsObservable.Subscribe(msg => ProcessMessage(msg));
```

<details>
<summary>Answer</summary>
While this code works, it has issues:

1. **Two separate subscriptions** - No unified handling, error propagation, or completion tracking
2. **No proper disposal management** - Each subscription returns an IDisposable that should be tracked
3. **Not using the Merge pattern** - Cannot apply operators to the combined stream

Better approach using Merge:
```csharp
var ordersObservable = GetSurgewaveObservable("orders");
var paymentsObservable = GetSurgewaveObservable("payments");

var subscription = ordersObservable
    .Merge(paymentsObservable)
    .Subscribe(
        msg => ProcessMessage(msg),
        ex => HandleError(ex),
        () => HandleCompletion()
    );

// Later: subscription.Dispose() cleans up both
```

This provides unified error handling, completion tracking, and allows applying operators to the combined stream.
</details>

### 7. Complete this reactive pipeline that enriches orders with customer data from an API:

```csharp
surgewaveObservable
    .Select(msg => JsonSerializer.Deserialize<Order>(msg.Value))
    // Add enrichment step here to call GetCustomerAsync(order.CustomerId)
    // and combine the order with customer data
    .Subscribe(enrichedOrder => ProcessEnrichedOrder(enrichedOrder));
```

<details>
<summary>Answer</summary>
```csharp
surgewaveObservable
    .Select(msg => JsonSerializer.Deserialize<Order>(msg.Value))
    .SelectMany(async order =>
    {
        var customer = await GetCustomerAsync(order.CustomerId);
        return new EnrichedOrder
        {
            Order = order,
            Customer = customer
        };
    })
    .Subscribe(enrichedOrder => ProcessEnrichedOrder(enrichedOrder));
```

SelectMany is used because:
1. It handles async operations (returns Observable from async lambda)
2. It flattens the result back into the main stream
3. It properly handles concurrent enrichment calls

Alternative using Observable.FromAsync:
```csharp
.SelectMany(order =>
    Observable.FromAsync(() => GetCustomerAsync(order.CustomerId))
        .Select(customer => new EnrichedOrder { Order = order, Customer = customer }))
```
</details>

## Score
- 7/7: Excellent! You have mastered reactive integration patterns
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding
