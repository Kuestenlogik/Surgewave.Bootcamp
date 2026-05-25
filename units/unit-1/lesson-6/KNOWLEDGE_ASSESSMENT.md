# Quiz: Lesson 1.6 - Message Best Practices

## Multiple Choice

### 1. What is the key difference between Events and Commands in messaging?
- [ ] A) Events are faster, Commands are slower
- [ ] B) Events describe facts that happened (past tense), Commands request actions (imperative)
- [ ] C) Events are for internal use, Commands are for external APIs
- [ ] D) Events require acknowledgment, Commands don't

<details>
<summary>Answer</summary>
**B)** Events describe facts that happened (past tense), Commands request actions (imperative). Events like "OrderPlaced" are facts that cannot be rejected, while Commands like "PlaceOrder" are requests that can be accepted or rejected.
</details>

### 2. Why should messages be immutable?
- [ ] A) To reduce memory usage
- [ ] B) To enable safe concurrent processing and simplify debugging
- [ ] C) To improve serialization performance
- [ ] D) To comply with security requirements

<details>
<summary>Answer</summary>
**B)** To enable safe concurrent processing and simplify debugging. Immutable messages can be safely shared across threads, and since they never change, debugging is easier because you know the message state is constant.
</details>

### 3. What is the purpose of a Correlation ID?
- [ ] A) To identify the message producer
- [ ] B) To link related messages across services for tracing
- [ ] C) To determine which partition to use
- [ ] D) To encrypt message content

<details>
<summary>Answer</summary>
**B)** To link related messages across services for tracing. Correlation IDs allow you to trace a request through multiple services and events, enabling end-to-end debugging and monitoring.
</details>

### 4. Which naming convention is correct for events?
- [ ] A) `ProcessOrder` (imperative)
- [ ] B) `OrderProcessing` (present continuous)
- [ ] C) `OrderProcessed` (past tense)
- [ ] D) `ToProcessOrder` (infinitive)

<details>
<summary>Answer</summary>
**C)** `OrderProcessed` (past tense). Events use past tense because they describe something that has already happened. Commands use imperative form like `ProcessOrder`.
</details>

## True/False

### 5. C# records are a good choice for message types because they are immutable by default.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Records in C# are immutable by default, making them ideal for message types. Using `with` expressions creates new instances rather than modifying existing ones.
</details>

### 6. Events can have multiple subscribers, while Commands typically have a single handler.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Events are published without knowledge of subscribers (multiple services can react to them), while Commands are sent to a specific handler that can accept or reject them.
</details>

## Code Challenge

### 7. What's wrong with this event design?

```csharp
public class OrderPlaced
{
    public Guid OrderId { get; set; }
    public Customer Customer { get; set; }  // Full customer entity
    public PaymentDetails Payment { get; set; }  // Full payment info
    public List<Product> Products { get; set; }  // Full product entities
    public ShippingAddress Address { get; set; }
    public BillingAddress Billing { get; set; }
    // ... 50 more properties
}
```

<details>
<summary>Answer</summary>
Multiple issues with this event design:

1. **Mutable class instead of record** - Should use `record` for immutability
2. **Too much data** - Including full entities creates tight coupling
3. **Missing Correlation ID** - No way to trace this event
4. **Missing timestamp** - No record of when the event occurred

Better design:
```csharp
public record OrderPlaced(
    Guid OrderId,
    Guid CustomerId,           // Just the ID, not full entity
    string CustomerEmail,      // Only data consumers need
    decimal TotalAmount,
    List<OrderLineItem> Items, // Minimal item info
    DateTimeOffset PlacedAt,
    Guid CorrelationId);

public record OrderLineItem(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
```

Include only what consumers need, keep it immutable, and always include correlation ID and timestamp.
</details>

## Score
- 7/7: Excellent! You understand messaging best practices
- 5-6/7: Good grasp of event-driven design
- 3-4/7: Review events vs commands and payload design
- <3/7: Re-do the lesson focusing on the principles
