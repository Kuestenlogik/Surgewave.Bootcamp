# SurgewaveMart Requirements

Detailed functional and technical requirements for the capstone project.

## Domain Model

### Entities

```csharp
// Order Domain
record Order(
    string OrderId,
    string CustomerId,
    List<OrderItem> Items,
    decimal Total,
    OrderStatus Status,
    DateTime CreatedAt
);

record OrderItem(string ProductId, int Quantity, decimal UnitPrice);

enum OrderStatus { Pending, Confirmed, Paid, Shipped, Delivered, Cancelled }

// Payment Domain
record Payment(
    string PaymentId,
    string OrderId,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    DateTime ProcessedAt
);

enum PaymentMethod { CreditCard, DebitCard, BankTransfer, Wallet }
enum PaymentStatus { Pending, Processing, Completed, Failed, Refunded }

// Inventory Domain
record InventoryItem(
    string ProductId,
    string ProductName,
    int QuantityAvailable,
    int QuantityReserved,
    decimal Price
);

record StockReservation(
    string ReservationId,
    string OrderId,
    string ProductId,
    int Quantity,
    DateTime ExpiresAt
);

// Shipping Domain
record Shipment(
    string ShipmentId,
    string OrderId,
    string TrackingNumber,
    ShipmentStatus Status,
    Address DeliveryAddress,
    DateTime EstimatedDelivery
);

record Address(string Street, string City, string PostalCode, string Country);
enum ShipmentStatus { Created, PickedUp, InTransit, OutForDelivery, Delivered }
```

## Topics

| Topic | Key | Partitions | Retention | Purpose |
|-------|-----|------------|-----------|---------|
| `orders` | orderId | 6 | 7 days | New orders |
| `orders-confirmed` | orderId | 3 | 7 days | Validated orders |
| `payments` | orderId | 3 | 30 days | Payment events |
| `inventory-updates` | productId | 6 | 7 days | Stock changes |
| `shipments` | orderId | 3 | 30 days | Shipping events |
| `analytics` | - | 1 | 1 day | Aggregated metrics |
| `dlq` | original-topic | 1 | 30 days | Failed messages |

## Functional Requirements

### FR-1: Order Processing

**FR-1.1: Create Order**
- Accept order via REST API
- Validate order (non-empty items, valid quantities)
- Generate unique order ID (UUID)
- Produce to `orders` topic
- Return order ID to client

**FR-1.2: Validate Order**
- Consume from `orders` topic
- Check inventory availability
- Reserve stock if available
- Produce to `orders-confirmed` or reject

**FR-1.3: Order Status**
- Track order through lifecycle
- Update status on each event
- Expose status via API

### FR-2: Payment Processing

**FR-2.1: Process Payment**
- Consume from `orders-confirmed`
- Validate payment details
- Execute payment (simulated)
- Produce payment result

**FR-2.2: Handle Failures**
- Retry failed payments (3 attempts)
- Exponential backoff
- Move to DLQ after max retries

**FR-2.3: Idempotency**
- Deduplicate payment requests
- Use order ID as idempotency key

### FR-3: Inventory Management

**FR-3.1: Stock Reservation**
- Reserve stock on order confirmation
- Release on cancellation/timeout
- Track reserved vs available

**FR-3.2: Stock Updates**
- Consume inventory events
- Maintain current stock levels
- Emit low-stock alerts

**FR-3.3: Inventory Query**
- Expose current stock via API
- Support product filtering

### FR-4: Shipping

**FR-4.1: Create Shipment**
- Trigger on successful payment
- Generate tracking number
- Produce shipment event

**FR-4.2: Track Shipment**
- Update status on events
- Expose tracking via API

### FR-5: Analytics

**FR-5.1: Real-Time Metrics**
- Orders per minute
- Revenue per hour
- Average order value
- Top products

**FR-5.2: Windowed Aggregations**
- 1-minute tumbling windows
- 1-hour hopping windows (5-min advance)

**FR-5.3: Dashboard API**
- WebSocket for real-time updates
- REST for historical data

### FR-6: REST API

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/orders` | POST | Create order |
| `/api/orders/{id}` | GET | Get order status |
| `/api/orders/{id}/cancel` | POST | Cancel order |
| `/api/inventory` | GET | List inventory |
| `/api/inventory/{productId}` | GET | Get product stock |
| `/api/shipments/{orderId}` | GET | Get shipment status |
| `/api/analytics/summary` | GET | Current metrics |
| `/api/health` | GET | Health check |

## Technical Requirements

### TR-1: Reliability

- **TR-1.1**: Exactly-once semantics for payments
- **TR-1.2**: At-least-once for non-critical paths
- **TR-1.3**: Dead letter queue for all failures
- **TR-1.4**: Graceful shutdown (drain messages)

### TR-2: Performance

- **TR-2.1**: < 100ms API response time (p95)
- **TR-2.2**: < 500ms end-to-end order processing
- **TR-2.3**: Support 1000 orders/minute
- **TR-2.4**: Batch produce where possible

### TR-3: Observability

- **TR-3.1**: Structured logging (JSON)
- **TR-3.2**: Metrics (orders/sec, latency, errors)
- **TR-3.3**: Health checks for all components
- **TR-3.4**: Correlation IDs across services

### TR-4: Testing

- **TR-4.1**: Unit tests for business logic
- **TR-4.2**: Integration tests with embedded broker
- **TR-4.3**: > 70% code coverage

### TR-5: Configuration

- **TR-5.1**: Environment-based configuration
- **TR-5.2**: No hardcoded secrets
- **TR-5.3**: Configurable retry policies

## Event Schemas

### OrderCreated (v1)

```json
{
  "eventType": "OrderCreated",
  "version": 1,
  "orderId": "ORD-123",
  "customerId": "C-456",
  "items": [
    {"productId": "P-001", "quantity": 2, "unitPrice": 29.99}
  ],
  "total": 59.98,
  "createdAt": "2024-01-15T10:30:00Z"
}
```

### PaymentCompleted (v1)

```json
{
  "eventType": "PaymentCompleted",
  "version": 1,
  "paymentId": "PAY-789",
  "orderId": "ORD-123",
  "amount": 59.98,
  "method": "CreditCard",
  "transactionId": "TXN-ABC",
  "completedAt": "2024-01-15T10:30:05Z"
}
```

### InventoryReserved (v1)

```json
{
  "eventType": "InventoryReserved",
  "version": 1,
  "reservationId": "RES-001",
  "orderId": "ORD-123",
  "items": [
    {"productId": "P-001", "quantity": 2}
  ],
  "expiresAt": "2024-01-15T10:45:00Z"
}
```

## Acceptance Criteria

### Scenario 1: Happy Path Order

```gherkin
Given a customer with ID "C-001"
And product "P-001" has 10 units in stock
When the customer places an order for 2 units of "P-001"
Then an order is created with status "Pending"
And inventory is reserved
And the order status changes to "Confirmed"
And payment is processed
And the order status changes to "Paid"
And a shipment is created
And analytics are updated
```

### Scenario 2: Insufficient Stock

```gherkin
Given product "P-001" has 1 unit in stock
When a customer orders 5 units of "P-001"
Then the order is rejected with reason "Insufficient stock"
And no inventory is reserved
And no payment is attempted
```

### Scenario 3: Payment Failure

```gherkin
Given a confirmed order "ORD-001"
When payment processing fails
Then the payment is retried up to 3 times
And if all retries fail, the order is cancelled
And reserved inventory is released
And the failure is logged to DLQ
```
