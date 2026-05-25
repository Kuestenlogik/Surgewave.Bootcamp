# Knowledge Assessment: Lesson 16.3 - Quota Management

## Multiple Choice

### 1. What problem does quota management solve in multi-tenant Surgewave clusters?
- [ ] A) Message encryption
- [ ] B) Noisy neighbors monopolizing shared resources
- [ ] C) Topic replication
- [ ] D) Consumer group coordination

<details>
<summary>Answer</summary>
**B)** Quota management prevents noisy neighbors - tenants that could monopolize shared cluster resources through excessive throughput, storage, or connections, affecting other tenants' performance.
</details>

### 2. In the token bucket algorithm, what happens when a request arrives but there aren't enough tokens available?
- [ ] A) Tokens are borrowed from other tenants
- [ ] B) The request proceeds with partial tokens
- [ ] C) The request is blocked (wait) or rejected until tokens refill
- [ ] D) The bucket capacity is automatically increased

<details>
<summary>Answer</summary>
**C)** When insufficient tokens are available, the request must either wait for tokens to refill or be rejected. This is how rate limiting enforces throughput quotas.
</details>

### 3. Which quota type limits the maximum data a tenant can store in the cluster?
- [ ] A) Producer Quotas (Bytes/Second)
- [ ] B) Consumer Quotas (Fetch Bytes/Sec)
- [ ] C) Storage Quotas (Total Storage)
- [ ] D) Request Rate Quotas

<details>
<summary>Answer</summary>
**C)** Storage Quotas limit the total storage a tenant can use, along with the number of topics and maximum retention period.
</details>

## True/False

### 4. In fair scheduling, a tenant with 50 pending requests will always be processed before a tenant with 20 pending requests.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Fair scheduling (round-robin or weighted fair queuing) ensures each tenant gets a fair share of processing capacity regardless of their demand. Requests are processed in a rotating fashion among tenants, so high-demand tenants don't starve others.
</details>

### 5. The token bucket algorithm allows burst traffic up to the bucket capacity, which can exceed the steady-state rate limit temporarily.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The token bucket allows bursting up to the bucket capacity. For example, if the bucket holds 100 tokens and refills at 10/second, a tenant can consume 100 tokens instantly (burst), but then must wait for refills for sustained traffic.
</details>

## Code Challenge

### 6. What configuration values are missing from this TenantQuota record?
```csharp
public record TenantQuota(
    long ProducerBytesPerSecond,
    long ConsumerBytesPerSecond);
```

<details>
<summary>Answer</summary>
The record is missing important quota limits. A complete TenantQuota should include:
```csharp
public record TenantQuota(
    long ProducerBytesPerSecond,    // Producer throughput limit
    long ConsumerBytesPerSecond,    // Consumer throughput limit
    int MaxPartitions,              // Partition allocation limit
    long MaxStorageBytes,           // Storage quota
    int MaxTopics,                  // Topic count limit
    int MaxConsumerGroups,          // Consumer group limit
    int MaxConnections);            // Connection limit
```
</details>

### 7. In the quota enforcement pipeline, what should happen after checking that tokens are available?
- [ ] A) Send an alert immediately
- [ ] B) Consume tokens and record usage, then process the request
- [ ] C) Skip the request entirely
- [ ] D) Increase the quota automatically

<details>
<summary>Answer</summary>
**B)** After confirming tokens are available, the enforcement pipeline should: 1) Consume the required tokens from the bucket, 2) Record the usage for monitoring/billing, and 3) Process the actual request. This ensures accurate tracking and proper rate limiting.
</details>

## Score
- 7/7: Excellent! You understand quota management thoroughly
- 5-6/7: Good grasp of rate limiting concepts
- 3-4/7: Review token bucket and fair scheduling
- <3/7: Re-do the lesson focusing on quota types and enforcement
