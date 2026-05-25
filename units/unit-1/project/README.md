# Unit 1 Project: Order Notification System

Build a real-time order notification system that processes e-commerce orders and sends notifications.

## Scenario

You're building an order processing system for an online store. When a customer places an order, you need to:
1. Produce the order event to Surgewave
2. Consume and process orders
3. Route orders to different handlers based on priority
4. Track order statistics

## Requirements

### Functional Requirements

- [ ] Produce order events with order ID, customer, items, and total
- [ ] Consume orders and log processing
- [ ] High-priority orders (total > $500) go to `orders-priority` topic
- [ ] Track count of processed orders per partition

### Technical Requirements

- [ ] Use proper key selection for partitioning (customer ID)
- [ ] Implement consumer group for scalability
- [ ] Handle graceful shutdown
- [ ] Use batch producing for efficiency

## Order Event Schema

```csharp
public record OrderEvent(
    string OrderId,
    string CustomerId,
    List<OrderItem> Items,
    decimal Total,
    DateTime CreatedAt,
    OrderPriority Priority
);

public record OrderItem(string ProductId, string Name, int Quantity, decimal Price);

public enum OrderPriority { Normal, High, Express }
```

## Expected Output

```
=== Order Notification System ===

[Producer] Starting order generation...
[Producer] Order ORD-001 ($125.50) -> Partition 2
[Producer] Order ORD-002 ($750.00) -> Partition 0 [HIGH PRIORITY]
[Producer] Order ORD-003 ($45.00) -> Partition 1

[Consumer] Processing Order ORD-001 for customer C-123
[Consumer] Processing Order ORD-003 for customer C-456
[Priority] High-value order ORD-002 routed to priority queue

[Stats] Processed: 3 orders, Total: $920.50
[Stats] Partition 0: 1, Partition 1: 1, Partition 2: 1
```

## Getting Started

```bash
cd bootcamp/unit-1/project/start
dotnet run
```

## Hints

1. Use `CustomerId` as the message key for consistent partitioning
2. Check `Total > 500` for priority routing
3. Use `ConcurrentDictionary` for thread-safe statistics
4. Remember to flush the producer before shutdown

## Evaluation Criteria

| Criteria | Points |
|----------|--------|
| Orders produced correctly | 25 |
| Consumer processes all orders | 25 |
| Priority routing works | 20 |
| Statistics are accurate | 15 |
| Clean shutdown | 15 |

## Solution

Compare your implementation with [completed/](completed/README.md) when done.
