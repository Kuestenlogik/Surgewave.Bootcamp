# Lesson 11.1: E-Commerce Pipeline

> **Difficulty:** Advanced | **Duration:** 60 min | **Prerequisites:** Unit 10 completion

Build a complete e-commerce event pipeline with order processing, inventory management, and customer notifications.

## Objectives

- Design an event-driven e-commerce architecture
- Implement order lifecycle management
- Handle inventory updates in real-time
- Send customer notifications based on order events

## Architecture

```
                           E-COMMERCE PIPELINE

    +-------------+         +------------------+         +-------------+
    |   Order     | ------> |  orders topic    | ------> |   Order     |
    |   Service   |         +------------------+         |  Processor  |
    +-------------+                                      +------+------+
                                                                |
                            +-----------------------------------+
                            |                |                  |
                            v                v                  v
                    +-------+------+ +-------+------+  +--------+-------+
                    | order-events | | inventory-   |  | notifications  |
                    |    topic     | | updates topic|  |     topic      |
                    +-------+------+ +-------+------+  +--------+-------+
                            |                |                  |
                            v                v                  v
                    +-------+------+ +-------+------+  +--------+-------+
                    |   Order      | |  Inventory   |  | Notification   |
                    |   History    | |   Service    |  |    Service     |
                    +--------------+ +--------------+  +----------------+
```

## Concepts

### Order Lifecycle Events

```
OrderPlaced -> OrderValidated -> PaymentProcessed ->
OrderConfirmed -> ShipmentCreated -> OrderShipped -> OrderDelivered
```

### Event Types

| Event | Description | Downstream Actions |
|-------|-------------|-------------------|
| OrderPlaced | New order submitted | Validate, reserve inventory |
| OrderValidated | Order passes validation | Process payment |
| PaymentProcessed | Payment successful | Confirm order |
| OrderConfirmed | Order ready for fulfillment | Update inventory, notify customer |
| InventoryReserved | Stock allocated | Continue order flow |
| InventoryLow | Stock below threshold | Alert operations |
| NotificationSent | Customer notified | Audit trail |

### Topics Structure

- `orders` - Raw incoming orders
- `order-events` - Order lifecycle events
- `inventory-updates` - Stock changes
- `notifications` - Customer notification requests

## Key Takeaways

- Event-driven architecture enables loose coupling between services
- Each service owns its domain and communicates via events
- Order state can be reconstructed from the event stream
- Notifications are triggered by domain events, not direct calls
