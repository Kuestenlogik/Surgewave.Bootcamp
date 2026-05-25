# Knowledge Assessment: Lesson 6.5 - Capacity Planning

## Multiple Choice

### 1. What is the formula for estimating storage requirements?
- [ ] A) MessageSize x PartitionCount
- [ ] B) MessageSize x MessagesPerSecond x RetentionSeconds x ReplicationFactor
- [ ] C) MessagesPerSecond x ConsumerCount
- [ ] D) PartitionCount x ReplicationFactor

<details>
<summary>Answer</summary>
**B)** Storage = MessageSize x MessagesPerSecond x RetentionSeconds x ReplicationFactor. For example: 1KB x 10,000/sec x 7 days x 3 replicas = ~18 TB.
</details>

### 2. Which factor INCREASES throughput?
- [ ] A) Larger message size
- [ ] B) Higher replication factor
- [ ] C) Acks.All mode
- [ ] D) Larger batch size

<details>
<summary>Answer</summary>
**D)** Larger batch size increases throughput by reducing the number of network round trips. Larger messages, higher replication, and Acks.All all decrease throughput.
</details>

### 3. For a workload of 100,000-1,000,000 messages/second, how many partitions are recommended?
- [ ] A) 3-6
- [ ] B) 12-24
- [ ] C) 48-96
- [ ] D) 1-3

<details>
<summary>Answer</summary>
**C)** 48-96 partitions are recommended for large workloads (100,000 - 1,000,000 msg/sec). This provides sufficient parallelism for high throughput.
</details>

## True/False

### 4. Enabling compression decreases throughput because of CPU overhead.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Compression typically INCREASES throughput because the reduction in network and disk I/O outweighs the CPU cost. Compressed data transfers faster and stores more efficiently.
</details>

### 5. Retention planning should consider regulatory requirements.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The capacity planning checklist explicitly includes regulatory requirements under retention planning. Some industries require keeping data for specific periods for compliance.
</details>

## Code Challenge

### 6. Calculate the storage requirements for this scenario:
- Average message size: 2 KB
- Messages per second: 50,000
- Retention period: 14 days
- Replication factor: 3

<details>
<summary>Answer</summary>
**Storage Calculation:**

```
Storage = MessageSize x MessagesPerSecond x RetentionSeconds x ReplicationFactor

Step 1: Convert retention to seconds
14 days = 14 x 24 x 60 x 60 = 1,209,600 seconds

Step 2: Calculate raw data volume
2 KB x 50,000/sec x 1,209,600 sec = 120,960,000,000 KB
= 120,960,000 MB
= 120,960 GB
= ~121 TB (raw)

Step 3: Apply replication factor
121 TB x 3 = ~363 TB total storage needed
```

**Summary:**
- Raw data volume: ~121 TB
- With RF=3: ~363 TB total storage
- Per broker (3 brokers): ~121 TB each

**Additional considerations:**
- Add 20-30% headroom for growth and operations
- Plan for ~450 TB total to be safe
- Consider compression (might reduce by 50-80%)
</details>

### 7. Complete this capacity planning checklist for a new Surgewave deployment:

**Scenario:** E-commerce platform expecting:
- Black Friday peak: 500,000 orders/hour
- Normal: 50,000 orders/hour
- Order message size: 5 KB
- Must retain orders for audit: 2 years

<details>
<summary>Answer</summary>
**Capacity Planning Checklist:**

### 1. Data Volume Analysis
- [x] Average message size: **5 KB**
- [x] Peak message size: **10 KB** (assume 2x for complex orders)
- [x] Messages per second (average): **~14 msg/sec** (50,000/3600)
- [x] Messages per second (peak): **~139 msg/sec** (500,000/3600)
- [x] Expected growth rate: **30% per year** (estimate)

### 2. Retention Requirements
- [x] Retention period: **2 years (730 days)** - audit requirement
- [x] Cleanup policy: **delete** (or compact if order updates are common)
- [x] Regulatory requirements: **Financial audit compliance**

### 3. Availability Requirements
- [x] Replication factor: **3** (production standard)
- [x] Minimum in-sync replicas: **2**
- [x] Recovery time objective: **< 5 minutes**

### 4. Performance Requirements
- [x] End-to-end latency target: **< 100ms P99**
- [x] Consumer lag tolerance: **< 10,000 messages**
- [x] Batch processing windows: **N/A (real-time)**

### Storage Calculation:
```
Peak rate for sizing: 139 msg/sec
Storage = 5 KB x 139/sec x (730 x 86400 sec) x 3 RF
        = 5 KB x 139 x 63,072,000 x 3
        = ~131 TB
```

### Partition Recommendation:
- Peak: 139 msg/sec is low, but plan for growth
- Recommend: **12 partitions** (allows 12 parallel consumers, room for growth)

### Cluster Sizing:
- **3 brokers minimum** (for RF=3)
- **~50 TB storage per broker** (with headroom)
- Consider tiered storage for 2-year retention
</details>

## Score
- 7/7: Excellent! You understand capacity planning
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
