# Unit 6 Project: E-Commerce Event Store Design

Design and implement an event store for an e-commerce domain with proper schema design, topic structure, and event versioning.

## Scenario

You're the lead architect designing the event infrastructure for an e-commerce platform. You need to:

1. Design domain events with proper schema evolution support
2. Implement topic naming conventions and partitioning strategies
3. Handle multiple event versions gracefully
4. Build projections from the event stream

## Requirements

### Functional Requirements

- [ ] Define domain events for: Order, Customer, Product, and Inventory
- [ ] Implement schema versioning with backward compatibility
- [ ] Create appropriate topic structure (one topic per aggregate type)
- [ ] Build a read model (projection) from order events
- [ ] Handle events of different versions in the same consumer

### Technical Requirements

- [ ] Use JSON serialization with tolerant reader pattern
- [ ] Include schema version in message headers
- [ ] Use entity ID as partition key for ordering guarantees
- [ ] Implement event upcasting (V1 -> V2 transformation)
- [ ] Track aggregate state from event history

## Domain Events

```csharp
// Order Aggregate Events
public record OrderCreated(string OrderId, string CustomerId, DateTime CreatedAt);
public record OrderItemAdded(string OrderId, string ProductId, int Quantity, decimal Price);
public record OrderSubmitted(string OrderId, DateTime SubmittedAt);
public record OrderShipped(string OrderId, string TrackingNumber, DateTime ShippedAt);

// Customer Aggregate Events
public record CustomerRegistered(string CustomerId, string Email, string Name);
public record CustomerAddressUpdated(string CustomerId, Address Address);

// Product Aggregate Events
public record ProductCreated(string ProductId, string Name, decimal Price);
public record ProductPriceChanged(string ProductId, decimal OldPrice, decimal NewPrice);

// Inventory Aggregate Events
public record InventoryReplenished(string ProductId, int Quantity);
public record InventoryReserved(string ProductId, string OrderId, int Quantity);
```

## Expected Output

```
=== E-Commerce Event Store ===

[Setup] Creating topic structure...
  Created: ecommerce.orders (partitions: 6)
  Created: ecommerce.customers (partitions: 3)
  Created: ecommerce.products (partitions: 3)
  Created: ecommerce.inventory (partitions: 6)

[Events] Publishing domain events...

  Order ORD-001:
    -> OrderCreated (v1)
    -> OrderItemAdded (v1) - Widget x2
    -> OrderItemAdded (v1) - Gadget x1
    -> OrderSubmitted (v1)

  Order ORD-002:
    -> OrderCreated (v2) - includes loyalty tier
    -> OrderItemAdded (v2) - includes discount
    -> OrderSubmitted (v2)

[Projection] Building Order Summary...

  Processing events for ORD-001...
    Event 1: OrderCreated -> new order
    Event 2: OrderItemAdded -> 2 items, $100.00
    Event 3: OrderItemAdded -> 3 items, $175.00
    Event 4: OrderSubmitted -> status=submitted

  Processing events for ORD-002 (with v2 events)...
    Event 1: OrderCreated (v2 -> v2) -> new order, tier=Gold
    Event 2: OrderItemAdded (v2 -> v2) -> 1 item, $85.50 (10% discount)
    Event 3: OrderSubmitted -> status=submitted

[Summary] Order Projections:

  Order ORD-001:
    Customer:    CUST-123
    Items:       3
    Total:       $175.00
    Status:      Submitted
    Version:     4

  Order ORD-002:
    Customer:    CUST-456
    Loyalty:     Gold
    Items:       1
    Total:       $85.50
    Status:      Submitted
    Version:     3

=== Event Store Design Complete ===
```

## Getting Started

```bash
cd bootcamp/unit-6/project/start
dotnet run
```

## Hints

1. Use `EventType` enum or string discriminator for event routing
2. Store schema version in headers: `schema-version: "2"`
3. Partition by aggregate ID (OrderId, CustomerId, etc.)
4. Implement upcasters as functions: `UpcastV1ToV2(eventV1) -> eventV2`
5. Build projections by replaying events in order

## Topic Naming Convention

```
{domain}.{aggregate-type}

Examples:
  ecommerce.orders
  ecommerce.customers
  ecommerce.products
  ecommerce.inventory
```

## Evaluation Criteria

| Criteria | Points |
|----------|--------|
| Proper event design | 20 |
| Schema versioning works | 25 |
| Topic structure is correct | 15 |
| Projections build correctly | 25 |
| Code organization | 15 |

## Solution

Compare your implementation with [completed/](completed/README.md) when done.
