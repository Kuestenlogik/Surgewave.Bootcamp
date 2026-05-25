# Knowledge Assessment: Lesson 24.5 - Retention and Lifecycle Policies

## Multiple Choice

### 1. What is the primary purpose of a retention policy in tiered storage?
- [ ] A) To increase message throughput
- [ ] B) To define how long data stays in each tier and when it moves or gets deleted
- [ ] C) To compress data for faster reads
- [ ] D) To replicate data across regions

<details>
<summary>Answer</summary>
**B)** Retention policies define the lifecycle of data, including how long it remains in each storage tier, when it should be migrated to the next tier, and when it should be permanently deleted.
</details>

### 2. Which retention rule type should be used for regulatory compliance data?
- [ ] A) Age-based with short TTL
- [ ] B) Size-based with compaction
- [ ] C) Compliance rules with minimum retention and legal hold
- [ ] D) Access-based with automatic deletion

<details>
<summary>Answer</summary>
**C)** Compliance rules with minimum retention periods and legal hold capabilities are required for regulatory data. These rules prevent premature deletion and support legal discovery scenarios where data must be preserved regardless of age or access patterns.
</details>

### 3. What happens when a segment is under legal hold?
- [ ] A) It is immediately deleted
- [ ] B) It is moved to the archive tier
- [ ] C) It cannot be deleted or migrated regardless of retention policy
- [ ] D) It is compressed and stored in hot tier

<details>
<summary>Answer</summary>
**C)** Legal hold suspends all deletion and migration actions for the affected segments. The data must be preserved in its current state until the legal hold is explicitly removed, regardless of what the retention policy would normally dictate.
</details>

### 4. What cost savings can typically be achieved with tiered storage and retention policies?
- [ ] A) 5-10%
- [ ] B) 20-30%
- [ ] C) 60-80%
- [ ] D) 99%

<details>
<summary>Answer</summary>
**C)** Tiered storage with proper retention policies typically saves 60-80% in storage costs. This is achieved by moving aging data from expensive hot tiers (memory/NVMe) to progressively cheaper cold tiers (HDD/cloud archive) and deleting data that has exceeded its retention period.
</details>

### 5. Which approach is best for topics with unpredictable access patterns?
- [ ] A) Fixed age-based rules only
- [ ] B) Access-based rules that monitor actual read patterns
- [ ] C) No retention policy
- [ ] D) Delete all data after 24 hours

<details>
<summary>Answer</summary>
**B)** Access-based rules monitor actual read frequency and can promote frequently accessed cold data back to warmer tiers, or demote rarely accessed hot data to colder tiers. This provides the best cost-performance balance for unpredictable workloads.
</details>

## True/False

### 6. TTL can be configured at the global, topic, and partition level simultaneously.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - TTL follows a hierarchy: partition-level overrides topic-level, which overrides global defaults. This allows fine-grained control where specific partitions (e.g., high-value regions) can have longer retention than the default.
</details>

### 7. Compression during tier migration is always recommended.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Compressing segments during tier migration reduces storage costs in the destination tier. Since cold/archive tier data is rarely accessed, the decompression overhead on read is acceptable, while the ongoing storage savings are significant (typically 40-70% reduction).
</details>

### 8. Age-based retention rules are sufficient for all use cases.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Age-based rules alone miss important scenarios. Access-based rules handle data that is old but still frequently accessed (should stay warm). Compliance rules handle data that must be retained regardless of age. Size-based rules manage segment compaction and splitting.
</details>

## Code Challenge

### 9. What's wrong with this retention policy?

```csharp
var policy = new RetentionPolicy(
    Name: "aggressive-cleanup",
    TopicPattern: "audit-*",
    TierRules: new List<TierRule>
    {
        new(StorageTier.Hot, TimeSpan.FromHours(1), StorageTier.Cold),
        new(StorageTier.Cold, TimeSpan.FromDays(7), null)
    },
    TotalRetention: TimeSpan.FromDays(7),
    ComplianceEnabled: false
);
```

<details>
<summary>Answer</summary>
Several issues with this policy for audit data:

1. **Missing compliance flag** - Audit data typically requires compliance-enabled retention with minimum retention periods to meet regulatory requirements.

2. **Too short retention** - 7-day retention for audit data is almost certainly insufficient. Regulations like SOX require 7 years, HIPAA requires 6 years, GDPR requires reasonable retention.

3. **Skipped warm tier** - Jumping directly from Hot to Cold may cause latency issues if recent audit data is queried frequently.

4. **No legal hold consideration** - Audit data must support legal holds for discovery scenarios.

Corrected policy:
```csharp
var policy = new RetentionPolicy(
    Name: "audit-compliance",
    TopicPattern: "audit-*",
    TierRules: new List<TierRule>
    {
        new(StorageTier.Hot, TimeSpan.FromHours(1), StorageTier.Warm),
        new(StorageTier.Warm, TimeSpan.FromDays(30), StorageTier.Cold),
        new(StorageTier.Cold, TimeSpan.FromDays(365), StorageTier.Archive)
    },
    TotalRetention: TimeSpan.FromDays(2555), // ~7 years
    ComplianceEnabled: true,
    LegalHoldActive: false
);
```
</details>

### 10. Calculate the monthly cost savings for the following scenario:

- 1 TB total data
- Without tiering: all data in Hot tier ($0.50/GB/month)
- With tiering:
  - 50 GB in Hot ($0.50/GB/month)
  - 200 GB in Warm ($0.10/GB/month)
  - 500 GB in Cold ($0.02/GB/month)
  - 250 GB in Archive ($0.004/GB/month)

<details>
<summary>Answer</summary>
**Without tiering:**
1024 GB x $0.50 = $512.00/month

**With tiering:**
- Hot: 50 GB x $0.50 = $25.00
- Warm: 200 GB x $0.10 = $20.00
- Cold: 500 GB x $0.02 = $10.00
- Archive: 250 GB x $0.004 = $1.00

**Total with tiering: $56.00/month**

**Savings: $512.00 - $56.00 = $456.00/month (89.1%)**

This demonstrates the significant cost reduction possible with proper tiered storage policies. The key insight is that most data (750 GB out of 1024 GB) resides in cold or archive tiers where storage is extremely cheap.
</details>

## Score
- 10/10: Excellent! Ready for the Tiered Storage project
- 8-9/10: Good understanding
- 6-7/10: Review the lesson
- <6/10: Re-do the lesson
