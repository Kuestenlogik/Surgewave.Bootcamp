# Knowledge Assessment: Lesson 6.1 - Topic Design

## Multiple Choice

### 1. What is the recommended topic naming convention?
- [ ] A) `topic1`, `topic2`, `topic3`
- [ ] B) `<domain>.<entity>.<event-type>`
- [ ] C) `<date>_<sequence>`
- [ ] D) Any name that is unique

<details>
<summary>Answer</summary>
**B)** The recommended naming convention is `<domain>.<entity>.<event-type>`. For example: `orders.order.created`, `payments.payment.processed`. This provides clear organization and discoverability.
</details>

### 2. What is the problem with a single "all-events" topic (coarse granularity)?
- [ ] A) Too few partitions
- [ ] B) Mixed concerns, hard to filter, mixed retention requirements
- [ ] C) Too expensive
- [ ] D) Limited replication

<details>
<summary>Answer</summary>
**B)** A coarse-grained topic like "all-events" mixes different event types, making it hard to filter, requiring complex consumers, and forcing all events to share the same retention policy even when requirements differ.
</details>

### 3. What approach aligns topics with bounded contexts?
- [ ] A) Entity-level topics
- [ ] B) Instance-level topics
- [ ] C) Domain-driven topic design
- [ ] D) Random distribution

<details>
<summary>Answer</summary>
**C)** Domain-driven topic design aligns topics with bounded contexts. Each domain (orders, users, payments) has its own set of topics for commands and events, providing clear boundaries.
</details>

## True/False

### 4. Having one topic per entity instance (e.g., `user-123-events`) is a recommended approach for most systems.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Instance-level topics (e.g., `user-123-events`) cause an explosion of topics and management overhead. Domain-level or entity-type-level topics (e.g., `users.events`) are usually more appropriate.
</details>

### 5. Poor topic design decisions are inexpensive to fix later.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Poor topic design creates technical debt that's expensive to fix. The lesson explicitly states that wrong topic granularity has "High" fix cost and is a common source of problems.
</details>

## Code Challenge

### 6. Design a topic structure for an e-commerce platform with the following domains: Orders, Inventory, Shipping, and Notifications. Include both commands and events.

<details>
<summary>Answer</summary>
**Recommended topic structure:**

```
Orders Domain:
- orders.order.commands     (PlaceOrder, CancelOrder)
- orders.order.created
- orders.order.cancelled
- orders.order.shipped

Inventory Domain:
- inventory.stock.commands  (ReserveStock, ReleaseStock)
- inventory.stock.reserved
- inventory.stock.released
- inventory.stock.updated

Shipping Domain:
- shipping.shipment.commands (CreateShipment, UpdateTracking)
- shipping.shipment.created
- shipping.shipment.in-transit
- shipping.shipment.delivered

Notifications Domain:
- notifications.email.commands (SendEmail)
- notifications.email.sent
- notifications.sms.commands (SendSms)
- notifications.sms.sent
```

**Key design principles applied:**
1. Topics organized by domain (bounded context)
2. Clear naming: `<domain>.<entity>.<event-type>`
3. Commands separated from events
4. Each event type has its own topic for different retention/processing needs
</details>

### 7. A team proposes the following topic structure. Identify the problems:

```
topics:
- data
- events
- user-1-activity
- user-2-activity
- user-3-activity
... (10,000 user topics)
```

<details>
<summary>Answer</summary>
**Problems identified:**

1. **`data` - Too coarse**
   - No clear meaning or organization
   - Mixes all data types
   - Impossible to filter efficiently
   - Cannot have different retention per data type

2. **`events` - Too coarse**
   - Same problems as `data`
   - What kind of events? From which domain?
   - All consumers must process all events

3. **`user-*-activity` - Too fine (instance-level)**
   - 10,000+ topics is management nightmare
   - No shared ordering across users
   - Topic operations become expensive
   - Each topic needs partitions, replication = huge overhead

**Recommended alternative:**
```
users.activity         (partition by user_id)
orders.events          (partition by order_id or user_id)
inventory.events       (partition by product_id)
```

Use partitioning for per-entity ordering, not separate topics!
</details>

## Score
- 7/7: Excellent! You understand topic design
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
