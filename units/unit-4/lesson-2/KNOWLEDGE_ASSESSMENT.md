# Knowledge Assessment: Lesson 6.2 - Partitioning Strategies

## Multiple Choice

### 1. What determines message ordering in Surgewave?
- [ ] A) Topic name
- [ ] B) Message timestamp
- [ ] C) Partition - messages with the same key go to the same partition and are ordered
- [ ] D) Consumer group

<details>
<summary>Answer</summary>
**C)** Messages with the same key go to the same partition, and messages within a partition are ordered. This is fundamental to how Surgewave (and Kafka) provide ordering guarantees.
</details>

### 2. What is a "hot partition" problem?
- [ ] A) A partition with too much replication
- [ ] B) Uneven distribution where one partition receives most traffic
- [ ] C) A partition that is too large
- [ ] D) A partition with high temperature

<details>
<summary>Answer</summary>
**B)** A hot partition occurs when traffic is unevenly distributed, with one partition receiving significantly more data than others. Example: partitioning by country where 80% of users are in one country.
</details>

### 3. Which partition key strategy provides the best distribution but no ordering guarantees?
- [ ] A) Entity ID
- [ ] B) Tenant ID
- [ ] C) Random/null key
- [ ] D) Composite key

<details>
<summary>Answer</summary>
**C)** Random or null keys provide perfect distribution (round-robin across partitions) but no ordering guarantees since messages are spread evenly without regard to relationships. Best for logs and metrics where ordering doesn't matter.
</details>

## True/False

### 4. More partitions always means better performance.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - While more partitions enable more parallelism, they also increase overhead (memory, file handles, leader elections, rebalancing time). The right number depends on throughput requirements and consumer capacity.
</details>

### 5. Partitioning by entity ID (e.g., order_id) guarantees per-entity ordering.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - When you partition by entity ID, all messages for that entity go to the same partition. Since messages within a partition are ordered, you get per-entity ordering guarantees.
</details>

## Code Challenge

### 6. You have a multi-tenant SaaS application. One tenant (TenantA) has 10x the volume of all other tenants combined. How would you design the partitioning strategy?

<details>
<summary>Answer</summary>
**Problem:** Partitioning by tenant_id will create a hot partition for TenantA.

**Solution options:**

**Option 1: Composite Key**
```csharp
// For large tenants, add sub-key for distribution
var key = tenant.IsLarge
    ? $"{tenant.Id}:{message.EntityId.GetHashCode() % 10}"
    : tenant.Id;
await producer.ProduceAsync(topic, key, message);
```
- Large tenants spread across multiple partitions
- Ordering maintained per entity, not per tenant

**Option 2: Dedicated Topics**
```
topics:
- events.tenant-a     (many partitions, dedicated)
- events.standard     (shared by other tenants)
```
- Complete isolation for large tenant
- Different scaling and retention possible

**Option 3: Custom Partitioner**
```csharp
// Weight partitions by tenant volume
int partition = customPartitioner.Assign(tenantId, messageId);
```

**Recommendation:** Option 1 (composite key) is usually simplest. If TenantA needs isolation, Option 2 gives complete control.

Key insight: Accept that per-tenant ordering may not be possible for large tenants, but per-entity ordering within the tenant is usually sufficient.
</details>

### 7. Analyze this partitioning strategy and identify potential issues:

```csharp
// E-commerce order processing
await producer.ProduceAsync("orders", order.Country, orderJson);
```

<details>
<summary>Answer</summary>
**Issues with partitioning by country:**

1. **Hot partition problem**
   ```
   Distribution might look like:
   - USA: 60% of orders
   - UK:  15% of orders
   - DE:  10% of orders
   - Other: 15% of orders
   ```
   The USA partition will be overloaded while others are underutilized.

2. **Wrong ordering semantics**
   - Orders are ordered by country, not by customer or order
   - Customer A's orders might be out of order if they span countries
   - Order updates (created -> paid -> shipped) might not be ordered

3. **Consumer bottleneck**
   - One consumer handles 60% of all orders
   - Cannot scale USA processing independently

**Better approach:**
```csharp
// Partition by order_id for per-order ordering
await producer.ProduceAsync("orders", order.Id, orderJson);

// Or by customer_id for per-customer ordering
await producer.ProduceAsync("orders", order.CustomerId, orderJson);
```

This gives:
- Even distribution (order IDs are typically UUIDs or sequences)
- Meaningful ordering (all events for one order/customer are ordered)
- Scalable consumers
</details>

## Score
- 7/7: Excellent! You understand partitioning strategies
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
