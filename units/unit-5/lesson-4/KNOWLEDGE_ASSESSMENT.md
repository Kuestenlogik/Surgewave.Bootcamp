# Knowledge Assessment: Lesson 7.4 - Joins

## Multiple Choice

### 1. Which join type requires a window?
- [ ] A) Stream-Table join
- [ ] B) Table-Table join
- [ ] C) Stream-Stream join
- [ ] D) All joins require windows

<details>
<summary>Answer</summary>
**C)** Stream-Stream joins require a window because streams are unbounded. The window defines how long to wait for matching records. Stream-Table and Table-Table joins don't require windows because tables represent current state.
</details>

### 2. What is a Stream-Table join best used for?
- [ ] A) Correlating two event streams
- [ ] B) Enriching events with lookup data
- [ ] C) Combining two state stores
- [ ] D) Aggregating events

<details>
<summary>Answer</summary>
**B)** Stream-Table joins are ideal for enriching events with lookup data. For example, joining an order event stream with a customer table to add customer details to each order.
</details>

### 3. In a Stream-Stream inner join with a 5-minute window, when does a join output occur?
- [ ] A) When any record arrives
- [ ] B) When records from both streams with matching keys arrive within the window
- [ ] C) At the end of each window
- [ ] D) Only for the first matching record

<details>
<summary>Answer</summary>
**B)** An inner Stream-Stream join outputs when records from BOTH streams with matching keys arrive within the join window. If only one side has a matching record within the window, no output is produced.
</details>

## True/False

### 4. Stream-Table joins emit a result for every event in the stream, even if there's no matching table entry.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - In an inner Stream-Table join, events without matching table entries don't produce output. You need a LEFT join to emit results for all stream events (with null for missing table data).
</details>

### 5. Table-Table joins produce output whenever either table changes.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Table-Table joins are stateful and emit updates whenever either side changes. If customer data updates, all joined records for that customer are re-emitted with the new data.
</details>

## Code Challenge

### 6. Build an order enrichment pipeline:
1. Join orders (stream) with customers (table) to add customer name
2. Output enriched orders to a topic

```csharp
var builder = new StreamsBuilder("order-enrichment");

// Orders arrive as events
var orders = builder.Stream<string, Order>("orders");  // keyed by customerId

// Customer data as a table
var customers = builder.Table<string, Customer>("customers");  // keyed by customerId

// TODO: Join and enrich
```

<details>
<summary>Answer</summary>
```csharp
var builder = new StreamsBuilder("order-enrichment");

// Orders arrive as events (keyed by customerId)
var orders = builder.Stream<string, Order>("orders");

// Customer data as a table (keyed by customerId)
var customers = builder.Table<string, Customer>("customers");

// Join orders with customers
var enrichedOrders = orders.Join(
    customers,
    (order, customer) => new EnrichedOrder
    {
        OrderId = order.OrderId,
        CustomerId = order.CustomerId,
        CustomerName = customer.Name,
        CustomerEmail = customer.Email,
        Amount = order.Amount,
        OrderDate = order.OrderDate
    }
);

// Output enriched orders
enrichedOrders.To("enriched-orders");

var app = builder.Build();
app.Start();
```

**For left join (include orders even without customer data):**
```csharp
var enrichedOrders = orders.LeftJoin(
    customers,
    (order, customer) => new EnrichedOrder
    {
        OrderId = order.OrderId,
        CustomerId = order.CustomerId,
        CustomerName = customer?.Name ?? "Unknown",
        CustomerEmail = customer?.Email ?? "N/A",
        Amount = order.Amount,
        OrderDate = order.OrderDate
    }
);
```
</details>

### 7. Design a Stream-Stream join to correlate orders with payments. Orders and payments arrive on separate topics, and a payment should match its order within 30 minutes.

```csharp
var orders = builder.Stream<string, Order>("orders");     // keyed by orderId
var payments = builder.Stream<string, Payment>("payments"); // keyed by orderId

// TODO: Correlate orders with payments
```

<details>
<summary>Answer</summary>
```csharp
var orders = builder.Stream<string, Order>("orders");     // keyed by orderId
var payments = builder.Stream<string, Payment>("payments"); // keyed by orderId

// Define join window: 30 minutes
var joinWindow = JoinWindows.Of(TimeSpan.FromMinutes(30));

// Inner join: only output when both order and payment match
var paidOrders = orders.Join(
    payments,
    (order, payment) => new PaidOrder
    {
        OrderId = order.OrderId,
        CustomerId = order.CustomerId,
        OrderAmount = order.Amount,
        PaymentAmount = payment.Amount,
        PaymentMethod = payment.Method,
        PaymentTime = payment.Timestamp,
        OrderTime = order.Timestamp
    },
    joinWindow
);

// Output successfully paid orders
paidOrders.To("paid-orders");

// For detecting unpaid orders (left join):
var allOrders = orders.LeftJoin(
    payments,
    (order, payment) => new OrderStatus
    {
        OrderId = order.OrderId,
        IsPaid = payment != null,
        PaymentTime = payment?.Timestamp
    },
    joinWindow
);

// Filter to find unpaid orders after window expires
// Note: This is simplified; real unpaid detection needs grace period handling
```

**How the join works:**
```
Orders:   --O1--------O2-----------O3----
Payments: ------P1---------P2-----------P4
Window:   |----30 min----|

Output:
- (O1, P1) if P1 arrives within 30 min of O1
- (O2, P2) if P2 arrives within 30 min of O2
- O3: waits for matching payment
- P4: if no matching order within window, no output (inner join)
```

**Key considerations:**
1. Both streams must be keyed by the same field (orderId)
2. Window size determines how long to wait for matches
3. Inner join requires both sides; use left join to detect missing payments
</details>

## Score
- 7/7: Excellent! You understand joins
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
