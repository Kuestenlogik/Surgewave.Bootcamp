# Knowledge Assessment: Lesson 16.1 - Tenant Isolation Concepts

## Multiple Choice

### 1. What is a primary business driver for adopting multi-tenancy in a messaging system?
- [ ] A) Increased message latency
- [ ] B) Higher operational complexity
- [ ] C) Cost efficiency by sharing infrastructure across tenants
- [ ] D) Requiring separate clusters for each customer

<details>
<summary>Answer</summary>
**C)** Cost efficiency by sharing infrastructure across tenants. Multi-tenancy allows organizations to share infrastructure costs while serving multiple customers from a single cluster.
</details>

### 2. Which isolation level provides topic-level separation with shared broker resources?
- [ ] A) Level 1: Shared Topics
- [ ] B) Level 2: Topic-Level Isolation (Logical Isolation)
- [ ] C) Level 3: Broker-Level Isolation (Physical Isolation)
- [ ] D) Level 4: Network Isolation

<details>
<summary>Answer</summary>
**B)** Level 2: Topic-Level Isolation provides logical isolation where tenants have separate topics but share the same broker infrastructure. This offers a balance between cost and isolation.
</details>

### 3. In a dedicated model for multi-tenancy, what is the main trade-off compared to a shared model?
- [ ] A) Lower security
- [ ] B) Higher cost and complexity but maximum isolation
- [ ] C) Faster time to market
- [ ] D) Better resource utilization

<details>
<summary>Answer</summary>
**B)** The dedicated model provides maximum physical isolation but at significantly higher cost and operational complexity compared to the shared model.
</details>

## True/False

### 4. In the Tenant Context pattern, the `GetTopicName` method typically prefixes the base topic with the tenant ID.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The Tenant Context pattern encapsulates tenant-specific information and typically includes methods like `GetTopicName(baseTopic)` that return `{TenantId}.{baseTopic}` to ensure proper namespace separation.
</details>

### 5. Shared Topics (No Isolation) is the recommended approach for production SaaS environments with strict compliance requirements.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Shared Topics provide no isolation and are only suitable for development/testing. Production SaaS environments typically require at least Topic-Level Isolation (Logical Isolation) or Broker-Level Isolation for compliance requirements.
</details>

## Code Challenge

### 6. What is missing from this TenantContext implementation?
```csharp
public record TenantContext(string TenantId, string TenantName)
{
    public string GetTopicName(string baseTopic) => $".{baseTopic}";
}
```

<details>
<summary>Answer</summary>
The TenantId is missing from the topic name generation. The method should be:
```csharp
public string GetTopicName(string baseTopic) => $"{TenantId}.{baseTopic}";
```
Without the TenantId prefix, all tenants would write to the same topics, defeating the purpose of multi-tenancy isolation.
</details>

### 7. Which of the following best describes when to use Broker-Level Isolation?
- [ ] A) When cost is the primary concern
- [ ] B) For development and testing environments
- [ ] C) For large enterprise customers with compliance requirements needing full physical isolation
- [ ] D) When you want the simplest operational model

<details>
<summary>Answer</summary>
**C)** Broker-Level Isolation (Physical Isolation) is appropriate for large enterprise customers with strict compliance requirements who need complete isolation between tenants, even at the cost of higher infrastructure expenses and operational complexity.
</details>

## Score
- 7/7: Excellent! You fully understand tenant isolation concepts
- 5-6/7: Good understanding of multi-tenancy fundamentals
- 3-4/7: Review the isolation levels and their trade-offs
- <3/7: Re-do the lesson focusing on isolation concepts
