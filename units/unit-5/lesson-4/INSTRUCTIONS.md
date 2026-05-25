# Lesson 7.4: Joins - Instructions

## Step 1: Stream-Stream Join (Windowed)

Join two streams within a time window:

```csharp
var orders = builder.Stream<string, Order>("orders");
var payments = builder.Stream<string, Payment>("payments");

var enriched = orders.Join(
    payments,
    (order, payment) => new OrderWithPayment(order, payment),
    JoinWindows.Of(TimeSpan.FromMinutes(5))  // 5-minute window
);
```

**Join types:**
- `Join` - Inner join (both must exist)
- `LeftJoin` - Left outer (right may be null)
- `OuterJoin` - Full outer (either may be null)

## Step 2: Stream-Table Join

Enrich stream events with table lookup:

```csharp
var orders = builder.Stream<string, Order>("orders");        // Key: customerId
var customers = builder.Table<string, Customer>("customers"); // Key: customerId

var enriched = orders.Join(
    customers,
    (order, customer) => new EnrichedOrder(order, customer.Name, customer.Email)
);
```

No window needed - table always has current state.

## Step 3: Table-Table Join

Join two stateful tables:

```csharp
var customers = builder.Table<string, Customer>("customers");
var addresses = builder.Table<string, Address>("addresses");

var joined = customers.Join(
    addresses,
    (customer, address) => new CustomerWithAddress(customer, address)
);
```

## Step 4: Global Table Join

Join stream with fully-replicated global table:

```csharp
var orders = builder.Stream<string, Order>("orders");
var products = builder.GlobalTable<string, Product>("products");

var enriched = orders.Join(
    products,
    (orderId, order) => order.ProductId,  // Key extractor
    (order, product) => new OrderWithProduct(order, product)
);
```

## Complete Example

```csharp
var builder = new StreamsBuilder("order-enrichment");

// Streams
var orders = builder.Stream<string, string>("orders");
var payments = builder.Stream<string, string>("payments");

// Tables
var customers = builder.Table<string, string>("customers");

// Stream-Table join: Enrich orders with customer name
var ordersWithCustomer = orders.Join(
    customers,
    (order, customerName) => $"{order} (customer: {customerName})"
);

// Stream-Stream join: Correlate orders with payments
var ordersWithPayments = orders.Join(
    payments,
    (order, payment) => $"Order: {order}, Payment: {payment}",
    JoinWindows.Of(TimeSpan.FromMinutes(10))
);

ordersWithCustomer.To("enriched-orders");
ordersWithPayments.To("order-payments");
```

## Co-partitioning Requirement

For joins to work, streams/tables must be **co-partitioned**:
- Same number of partitions
- Same partitioning strategy (key hashing)

If not co-partitioned, use `Repartition()` first.

## Step 5: Suppression Basics

By default, aggregations emit an update for every input record. Use suppression
to control output frequency:

```csharp
// Without suppression - emits on every update
var counts = events
    .GroupByKey()
    .WindowedBy(TumblingWindows.Of(TimeSpan.FromMinutes(5)))
    .Count()
    .ToStream()
    .Peek((w, c) => Console.WriteLine($"{w.Key}: {c}"));  // Many outputs!

// With suppression - emit only when window closes
var finalCounts = events
    .GroupByKey()
    .WindowedBy(TumblingWindows.Of(TimeSpan.FromMinutes(5)))
    .Count()
    .Suppress(Suppressed<Windowed<string>>.UntilWindowClose(
        TimeSpan.FromMinutes(1)
    ))
    .ToStream()
    .Peek((w, c) => Console.WriteLine($"FINAL {w.Key}: {c}"));  // One output
```

## Step 6: UntilTimeLimit Suppression

For non-windowed aggregations or rate limiting:

```csharp
var throttledCounts = events
    .GroupByKey()
    .Count()
    .Suppress(Suppressed<string>.UntilTimeLimit(
        duration: TimeSpan.FromSeconds(5),
        maxBytes: 1024 * 1024  // 1 MB buffer limit
    ))
    .ToStream()
    .Peek((key, count) => Console.WriteLine($"{key}: {count}"));
```

**Parameters:**
- `duration`: Emit after this time even if no new records arrive
- `maxBytes`: If buffer exceeds this, emit oldest records to make room

## Step 7: Suppression with Grace Period

Combine grace period and suppression for accurate final results:

```csharp
var accurateFinalCounts = events
    .GroupByKey()
    .WindowedBy(
        TumblingWindows
            .Of(TimeSpan.FromHours(1))
            .Grace(TimeSpan.FromMinutes(5))  // Accept 5 min late
    )
    .Count()
    .Suppress(Suppressed<Windowed<string>>.UntilWindowClose(
        TimeSpan.FromMinutes(5)  // Wait for grace to expire
    ));
```

**Timeline for 10:00-11:00 window:**
```
10:00 - Window opens
11:00 - Window logically ends
11:05 - Grace period expires (last chance for late records)
11:05 - Suppression emits final count
```

## Complete Example with Suppression

```csharp
var builder = new StreamsBuilder("revenue-reporting");

var sales = builder.Stream<string, int>("sales");

// Real-time running totals (frequent updates for dashboard)
var runningTotals = sales
    .GroupByKey()
    .Reduce((a, b) => a + b);

runningTotals.ToStream()
    .Peek((region, total) =>
        Console.WriteLine($"[LIVE] {region} revenue: ${total}"));

// Hourly final totals (one update per hour for reporting)
var hourlyTotals = sales
    .GroupByKey()
    .WindowedBy(
        TumblingWindows
            .Of(TimeSpan.FromHours(1))
            .Grace(TimeSpan.FromMinutes(5))
    )
    .Reduce((a, b) => a + b)
    .Suppress(Suppressed<Windowed<string>>.UntilWindowClose(
        TimeSpan.FromMinutes(5)
    ));

hourlyTotals.ToStream()
    .Peek((windowed, total) =>
    {
        var hour = DateTimeOffset.FromUnixTimeMilliseconds(windowed.Window.StartMs);
        Console.WriteLine($"[FINAL] {windowed.Key} hour {hour:HH:00}: ${total}");
    });
```

## Exercises

1. **Order Enrichment**: Join orders with customer data
2. **Payment Correlation**: Correlate orders and payments
3. **Multi-way Join**: Chain multiple joins
4. **Suppressed Revenue**: Create hourly revenue reports with suppression
