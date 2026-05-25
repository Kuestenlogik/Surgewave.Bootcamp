# Knowledge Assessment: Lesson 27.1 - Hybrid Cloud Concepts

## Multiple Choice

### 1. Which hybrid cloud pattern is best suited for disaster recovery with minimal complexity?
- [ ] A) Active-Active
- [ ] B) Active-Passive
- [ ] C) Hub-and-Spoke
- [ ] D) Mesh Topology

<details>
<summary>Answer</summary>
**B)** Active-Passive is the simplest pattern for disaster recovery. One cluster handles all traffic while the other remains on standby, ready for failover. It has clear ownership and lower operational complexity compared to Active-Active.
</details>

### 2. According to the CAP theorem, which two properties can a distributed system guarantee during a network partition?
- [ ] A) Consistency and Availability
- [ ] B) Consistency and Partition Tolerance
- [ ] C) Availability and Partition Tolerance
- [ ] D) Either B or C, but not A

<details>
<summary>Answer</summary>
**D)** During a network partition (which is inevitable in distributed systems), you must choose between Consistency (CP) or Availability (AP). You cannot have both CA during a partition. Systems choose CP for financial data or AP for user-facing availability.
</details>

### 3. What is the primary benefit of using regional topic prefixes like `us-east.orders.created`?
- [ ] A) Improved compression
- [ ] B) Better performance
- [ ] C) Clear data locality and ownership
- [ ] D) Reduced storage costs

<details>
<summary>Answer</summary>
**C)** Regional topic prefixes make data locality explicit, clarifying which region "owns" the data. This helps with routing decisions, compliance requirements, and debugging issues in multi-region deployments.
</details>

### 4. In a Hub-and-Spoke topology, what is the main weakness?
- [ ] A) High latency
- [ ] B) Complex conflict resolution
- [ ] C) Hub is a single point of failure
- [ ] D) Cannot scale beyond 3 regions

<details>
<summary>Answer</summary>
**C)** The central hub in a Hub-and-Spoke topology is a single point of failure. If the hub goes down, edge clusters cannot communicate with each other. This topology is best when central aggregation is required and hub reliability can be ensured.
</details>

### 5. Which cluster role should handle both reads and writes in a hybrid deployment?
- [ ] A) Secondary
- [ ] B) Edge
- [ ] C) Aggregator
- [ ] D) Primary

<details>
<summary>Answer</summary>
**D)** The Primary cluster handles both reads and writes. Secondary clusters are typically read-only replicas for failover, Edge clusters handle local processing, and Aggregators collect data from multiple sources.
</details>

## True/False

### 6. In Active-Active deployments, conflict resolution is simpler than in Active-Passive deployments.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Active-Active deployments are more complex because multiple clusters can receive writes simultaneously, requiring conflict resolution strategies. Active-Passive has clear ownership where only one cluster handles writes.
</details>

### 7. Regional topic prefixes help with GDPR compliance by making data residency explicit.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Regional prefixes like `eu-west.users.data` make it clear where data resides. This helps organizations comply with data residency requirements like GDPR (EU) or CCPA (California) by ensuring data stays in the required jurisdiction.
</details>

## Code Challenge

### 8. What's wrong with this cluster selection implementation?

```csharp
public class ClusterLocator
{
    private readonly List<string> _clusters = new();

    public string GetCluster(string clientRegion)
    {
        // Return first available cluster
        return _clusters.First();
    }
}
```

<details>
<summary>Answer</summary>
The implementation ignores the client's region entirely and always returns the first cluster. This defeats the purpose of having multiple regional clusters for low latency. The correct implementation should consider proximity:

```csharp
public class ClusterLocator
{
    private readonly Dictionary<string, (string BootstrapServers, string Region)> _clusters;

    public ClusterLocator(Dictionary<string, (string, string)> clusters)
    {
        _clusters = clusters;
    }

    public string GetCluster(string clientRegion)
    {
        // First, try to find a cluster in the same region
        var sameRegion = _clusters
            .FirstOrDefault(c => c.Value.Region == clientRegion);

        if (sameRegion.Key != null)
            return sameRegion.Value.BootstrapServers;

        // Fall back to any available cluster
        return _clusters.Values.First().BootstrapServers;
    }
}
```

A production implementation would also consider:
- Actual network latency measurements
- Cluster health and load
- Failover to next-nearest cluster
</details>

## Scenario Challenge

### 9. Your company needs to deploy Surgewave for a global e-commerce platform with these requirements:
- Users in US, EU, and APAC regions
- EU user data must stay in EU (GDPR)
- 99.99% availability requirement
- Analytics team needs aggregated data from all regions

Which topology would you recommend and why?

<details>
<summary>Answer</summary>
**Recommended: Hybrid approach combining Active-Active with Hub-and-Spoke**

**Architecture:**
1. **Three regional clusters** (Active-Active):
   - `us-east-1` (AWS) - US users
   - `eu-west-1` (Azure) - EU users (GDPR compliant)
   - `ap-southeast-1` (GCP) - APAC users

2. **Central analytics hub** (Hub-and-Spoke):
   - Aggregates anonymized/non-PII data from all regions
   - EU cluster sends only aggregated, GDPR-compliant data

**Topic Strategy:**
```
Regional topics (not replicated):
  - eu-west.users.pii          (stays in EU)
  - us-east.orders.raw         (stays in US)

Global topics (replicated everywhere):
  - global.products.catalog    (read-only reference data)
  - global.config.settings

Aggregate topics (to hub only):
  - agg.orders.summary         (anonymized for analytics)
  - agg.metrics.all
```

**Rationale:**
- Active-Active between regions provides low latency for each region
- Regional topics ensure GDPR compliance
- Hub receives only compliant aggregated data
- 99.99% availability through multi-region redundancy
</details>

## Score
- 9/9: Excellent!
- 7-8/9: Good understanding
- 5-6/9: Review the lesson
- <5/9: Re-do the lesson
