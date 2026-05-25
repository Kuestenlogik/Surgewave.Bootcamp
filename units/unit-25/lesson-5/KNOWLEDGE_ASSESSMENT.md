# Knowledge Assessment: Lesson 16.5 - Resource Management

## Multiple Choice

### 1. In the resource allocation model, what is the purpose of the "Reserved Pool"?
- [ ] A) Emergency storage for crash recovery
- [ ] B) Guaranteed minimum allocations for tenants
- [ ] C) Temporary overflow capacity
- [ ] D) Administrative operations only

<details>
<summary>Answer</summary>
**B)** The Reserved Pool provides guaranteed minimum allocations for tenants. This ensures each tenant has access to a baseline level of resources regardless of overall cluster demand.
</details>

### 2. In the storage management tiering model, which tier provides the fastest access?
- [ ] A) Cold Storage (Archived, > 30 days)
- [ ] B) Warm Storage (7-30 days)
- [ ] C) Hot Storage (SSD, < 7 days)
- [ ] D) All tiers have equal access speed

<details>
<summary>Answer</summary>
**C)** Hot Storage on SSD provides the fastest access for recent data (typically < 7 days). It costs more but offers quick retrieval for active workloads.
</details>

### 3. During tenant offboarding, what is the correct sequence of resource lifecycle steps?
- [ ] A) Delete topics -> Release quotas -> Archive data -> Final billing
- [ ] B) Archive data -> Delete topics -> Release quotas -> Final billing
- [ ] C) Final billing -> Delete topics -> Archive data -> Release quotas
- [ ] D) Release quotas -> Final billing -> Delete topics -> Archive data

<details>
<summary>Answer</summary>
**B)** The correct sequence is: Archive data (preserve for compliance), Delete topics (remove from cluster), Release quotas (free up allocations), Final billing (close out the account).
</details>

## True/False

### 4. A tenant using 85% of their partition quota should trigger a warning alert.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The monitoring dashboard shows that 85% partition usage triggers a warning. This gives tenants and administrators time to either clean up resources or request quota increases before hitting the hard limit.
</details>

### 5. In the resource allocation model, tenants can never exceed their "Limit" even temporarily.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The Limit represents the maximum allowed allocation for a tenant and serves as a hard cap. While tenants are guaranteed their Reserved minimum and can grow into the Shared Pool up to their Limit, they cannot exceed it.
</details>

## Code Challenge

### 6. What information is missing from this tenant resource allocation display?
```csharp
public class TenantAllocation
{
    public int CurrentPartitions { get; set; }
    public int MaxPartitions { get; set; }
}
```

<details>
<summary>Answer</summary>
The allocation is missing the Reserved (guaranteed minimum) field. A complete allocation should include:
```csharp
public class TenantAllocation
{
    public int ReservedPartitions { get; set; }  // Guaranteed minimum
    public int CurrentPartitions { get; set; }   // Actual usage
    public int MaxPartitions { get; set; }       // Hard limit

    // Useful derived properties
    public double UtilizationPercent =>
        (double)CurrentPartitions / MaxPartitions * 100;
    public bool IsOverReserved =>
        CurrentPartitions > ReservedPartitions;
}
```
</details>

### 7. In the monitoring dashboard, which metric would help identify a tenant that may soon be throttled?
- [ ] A) Total Topics count
- [ ] B) Average Latency
- [ ] C) Quota utilization percentage trending toward 100%
- [ ] D) Number of consumer groups

<details>
<summary>Answer</summary>
**C)** Quota utilization percentage trending toward 100% indicates a tenant is approaching their limits and may soon be throttled. The dashboard shows alerts like "[INFO] globex: Producer quota usage increasing (trending 95%)" to warn administrators before throttling occurs.
</details>

## Score
- 7/7: Excellent! You fully understand resource management
- 5-6/7: Good grasp of allocation and monitoring
- 3-4/7: Review storage tiers and lifecycle management
- <3/7: Re-do the lesson focusing on resource allocation model
