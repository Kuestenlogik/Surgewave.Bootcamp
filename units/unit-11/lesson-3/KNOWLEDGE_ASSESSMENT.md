# Knowledge Assessment: Lesson 5.3 - Hybrid Deployments

## Multiple Choice

### 1. What is the main advantage of a hybrid deployment during migration?
- [ ] A) Better performance
- [ ] B) Lower cost
- [ ] C) Reduced risk with easy rollback
- [ ] D) Simpler architecture

<details>
<summary>Answer</summary>
**C)** Hybrid deployments reduce migration risk by allowing gradual transition. If issues arise, rollback is easy since Kafka is still running. This contrasts with "big bang" migrations which have high risk and difficult rollback.
</details>

### 2. In the topic-by-topic migration pattern, what happens?
- [ ] A) All topics are migrated at once
- [ ] B) Topics are gradually moved from Kafka to Surgewave
- [ ] C) Topics are duplicated permanently
- [ ] D) Only new topics go to Surgewave

<details>
<summary>Answer</summary>
**B)** In topic-by-topic migration, topics are gradually moved from Kafka to Surgewave. Some topics remain on Kafka while migrated topics run on Surgewave, until all topics are eventually on Surgewave.
</details>

### 3. What is the dual-write pattern?
- [ ] A) Writing to two partitions
- [ ] B) Writing to both Kafka (primary) and Surgewave (shadow)
- [ ] C) Using two producers
- [ ] D) Replicating within a single cluster

<details>
<summary>Answer</summary>
**B)** Dual-write means producers write to both Kafka (as the primary system) and Surgewave (as a shadow system). This allows validating Surgewave behavior before cutting over.
</details>

## True/False

### 4. A "big bang" migration approach has lower risk than a hybrid deployment.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Big bang migrations have HIGH risk and difficult rollback. Hybrid/gradual deployments have LOW risk and easy rollback because you can fall back to the still-running Kafka system.
</details>

### 5. In a hybrid deployment, both Kafka and Surgewave can run simultaneously.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Hybrid deployments specifically involve running Surgewave alongside Kafka during the transition period. This enables gradual migration and validation before full cutover.
</details>

## Code Challenge

### 6. Design a hybrid deployment strategy for an e-commerce system with these topics:
- `orders` (critical, 10M messages/day)
- `logs` (non-critical, 100M messages/day)
- `payments` (critical, 5M messages/day)

What order would you migrate them and why?

<details>
<summary>Answer</summary>
Recommended migration order:

**Phase 1: `logs` (non-critical, high volume)**
- Start with non-critical topic to validate Surgewave at scale
- High volume tests Surgewave's throughput capabilities
- Low business risk if issues occur

**Phase 2: `orders` (critical, medium volume)**
- After logs validation, migrate orders
- Medium volume, easier to monitor
- Use dual-write initially for safety

**Phase 3: `payments` (critical, lowest volume)**
- Migrate last as it's most sensitive
- Lowest volume allows careful monitoring
- By now, confidence in Surgewave is established

Migration strategy:
```
Week 1-2: logs on Surgewave (shadow, then primary)
Week 3-4: orders with dual-write validation
Week 5:   orders cutover to Surgewave
Week 6-7: payments with dual-write validation
Week 8:   payments cutover to Surgewave
Week 9+:  Decommission Kafka after burn-in
```

Key principle: Start with low-risk, high-volume to prove the system, then gradually move critical workloads.
</details>

### 7. What are the risks of the dual-write pattern and how can they be mitigated?

<details>
<summary>Answer</summary>
**Risks of dual-write:**

1. **Consistency issues**
   - Risk: Messages might succeed on one system but fail on the other
   - Mitigation: Implement retry logic, compare message counts regularly

2. **Increased latency**
   - Risk: Waiting for both writes adds latency
   - Mitigation: Write to shadow asynchronously (fire-and-forget for Surgewave during validation)

3. **Double resource usage**
   - Risk: Costs for running both systems
   - Mitigation: Plan a definite cutover timeline, keep hybrid phase short

4. **Message ordering differences**
   - Risk: Different ordering between systems
   - Mitigation: Compare consumer behavior on both systems, not just message counts

5. **Divergence over time**
   - Risk: Systems drift apart making comparison difficult
   - Mitigation: Regular reconciliation, clear validation metrics

**Dual-write architecture for safety:**
```csharp
// Primary write (must succeed)
await kafkaProducer.ProduceAsync("orders", message);

// Shadow write (best effort during validation)
try {
    await surgewaveProducer.ProduceAsync("orders", message);
} catch (Exception ex) {
    // Log but don't fail the request
    logger.LogWarning("Shadow write failed: {Error}", ex.Message);
}
```
</details>

## Score
- 7/7: Excellent! You understand hybrid deployments
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
