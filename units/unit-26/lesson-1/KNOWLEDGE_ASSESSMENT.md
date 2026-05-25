# Knowledge Assessment: Lesson 24.1 - Tiered Storage Concepts

## Multiple Choice

### 1. Which storage tier has the highest cost per GB but lowest latency?
- [ ] A) Cold tier
- [ ] B) Warm tier
- [ ] C) Hot tier
- [ ] D) Archive tier

<details>
<summary>Answer</summary>
**C)** Hot tier has the highest cost per GB (typically $0.10-0.50/GB/month) but provides sub-millisecond latency using memory and NVMe SSDs.
</details>

### 2. What is the most common trigger for moving data from hot to warm tier?
- [ ] A) Manual administrator action
- [ ] B) Time-based (age of data)
- [ ] C) Random selection
- [ ] D) Consumer request

<details>
<summary>Answer</summary>
**B)** Time-based triggers are the most common. Data moves from hot to warm tier after a configured retention period (e.g., 1 hour) regardless of access patterns.
</details>

### 3. What is the typical cost savings when implementing tiered storage with proper configuration?
- [ ] A) 10-20%
- [ ] B) 30-40%
- [ ] C) 50-90%
- [ ] D) 95-99%

<details>
<summary>Answer</summary>
**C)** Tiered storage typically saves 50-90% on storage costs by moving older, less-accessed data to cheaper storage while keeping hot data on fast, expensive storage.
</details>

### 4. Which compression strategy is recommended for cold tier storage?
- [ ] A) No compression
- [ ] B) Light compression (LZ4)
- [ ] C) Heavy compression (Zstd, high level)
- [ ] D) Encryption only

<details>
<summary>Answer</summary>
**C)** Heavy compression (like Zstd at high compression levels) is recommended for cold tier because: (1) data is accessed infrequently, (2) storage cost savings outweigh CPU cost of decompression, (3) cold tier often uses object storage where egress costs matter.
</details>

### 5. What is a key consideration when storing compliance data in tiered storage?
- [ ] A) Always use the cheapest tier
- [ ] B) Delete after 30 days
- [ ] C) Ensure data remains accessible for required retention period
- [ ] D) Store only in memory

<details>
<summary>Answer</summary>
**C)** Compliance data must remain accessible for legally required retention periods (often 5-7 years). This means configuring cold tier retention appropriately and ensuring reliable retrieval from archive storage.
</details>

## True/False

### 6. Data in the cold tier can only be accessed through special restore procedures that take hours.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - While cold tier has higher latency than hot/warm tiers (typically seconds to minutes), data is still directly accessible. Only "archive" or "glacier" tiers in cloud providers may require restore procedures. Surgewave's cold tier provides direct access, just with higher latency.
</details>

### 7. Tiered storage is only beneficial for systems with more than 1TB of data.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Tiered storage can provide benefits at any scale. The percentage savings (50-90%) apply regardless of total data volume. Even with 100GB of data, proper tiering can significantly reduce costs.
</details>

### 8. Access-pattern-based tiering can move data back from cold to warm tier if it becomes frequently accessed.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Adaptive or access-pattern-based tiering monitors read frequency and can promote frequently-accessed data to warmer tiers. This is especially useful for historical data that suddenly becomes relevant (e.g., during an investigation or analysis).
</details>

## Code Challenge

### 9. What's wrong with this tier configuration?

```csharp
var config = new TieredStorageConfig
{
    HotTierRetention = TimeSpan.FromDays(7),
    WarmTierRetention = TimeSpan.FromHours(1),
    ColdTierRetention = TimeSpan.FromDays(30)
};
```

<details>
<summary>Answer</summary>
The hot tier retention (7 days) is longer than warm tier retention (1 hour). This doesn't make sense because data should flow from hot -> warm -> cold. The correct configuration should have:

```csharp
var config = new TieredStorageConfig
{
    HotTierRetention = TimeSpan.FromHours(1),    // Shortest
    WarmTierRetention = TimeSpan.FromDays(7),    // Medium
    ColdTierRetention = TimeSpan.FromDays(30)    // Longest
};
```

Hot tier retention must be less than warm tier retention, which must be less than or equal to cold tier retention.
</details>

### 10. Calculate the monthly storage cost for the following scenario:

- Daily data ingestion: 50 GB
- Hot tier retention: 1 hour (cost: $0.50/GB)
- Warm tier retention: 7 days (cost: $0.10/GB)
- Cold tier retention: 30 days (cost: $0.01/GB)

<details>
<summary>Answer</summary>
Calculate data in each tier:

**Hot tier:** 50 GB/day / 24 hours * 1 hour = ~2.1 GB
Cost: 2.1 GB * $0.50 = $1.05

**Warm tier:** 50 GB/day * 7 days = 350 GB
Cost: 350 GB * $0.10 = $35.00

**Cold tier:** 50 GB/day * (30 - 7) days = 50 * 23 = 1,150 GB
Cost: 1,150 GB * $0.01 = $11.50

**Total monthly cost with tiering: $47.55**

**Without tiering (all at hot price):**
50 GB * 30 days * $0.50 = $750.00

**Savings: $702.45 (93.7%)**
</details>

## Score
- 10/10: Excellent! Ready for production tiered storage
- 8-9/10: Good understanding
- 6-7/10: Review the lesson
- <6/10: Re-do the lesson
