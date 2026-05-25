# Knowledge Assessment: Lesson 5.5 - Hybrid Deployment Patterns

## Multiple Choice

### 1. What does Surgewave Cluster Linking provide?

- [ ] A) A load balancer between Kafka and Surgewave
- [ ] B) Continuous, low-latency replication of topics from a source broker into Surgewave
- [ ] C) A migration GUI in the Control UI
- [ ] D) Schema synchronization between registries

<details>
<summary>Answer</summary>
**B)** Cluster Linking provides continuous replication of topics from a source broker (Kafka or another Surgewave instance) into Surgewave. It keeps Surgewave in sync with the source so consumers can cut over when lag reaches zero.
</details>

### 2. In the dual-read consumer pattern, when does the consumer fall back to Kafka?

- [ ] A) Always, on every message
- [ ] B) Only during rollback
- [ ] C) When Surgewave does not return a message within the fast timeout window
- [ ] D) When schema validation fails

<details>
<summary>Answer</summary>
**C)** The dual-read consumer gives Surgewave a short window (e.g., 200 ms) to return a message. If nothing is available, it falls back to Kafka for the remaining timeout. This handles cases where Surgewave's mirror has not yet caught up.
</details>

### 3. Which topic should typically be migrated LAST?

- [ ] A) Logs (non-critical, high volume)
- [ ] B) Metrics (non-critical, medium volume)
- [ ] C) Payments (critical, lower volume)
- [ ] D) Feature flags (low volume, read-heavy)

<details>
<summary>Answer</summary>
**C)** Critical, business-sensitive topics like payments should migrate last. Start with non-critical, high-volume topics to build confidence in Surgewave at scale, then work toward the most sensitive workloads.
</details>

### 4. What is the correct rollback procedure when Surgewave has issues after promotion?

- [ ] A) Restart the Surgewave broker
- [ ] B) Delete the affected topics from Surgewave
- [ ] C) Demote Surgewave by restoring Kafka as primary reader and notify operations
- [ ] D) Rebuild the Cluster Link from scratch

<details>
<summary>Answer</summary>
**C)** Rolling back means demoting Surgewave so the consumer returns to Kafka as its primary source. Because Kafka was still running and receiving data (or the Cluster Link kept it in sync), no messages are lost. Alert operations and investigate before re-promoting.
</details>

## True/False

### 5. Consumer group offsets can be synchronised from Kafka to Surgewave via Cluster Link, enabling consumers to pick up from the correct position after cutover.

- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** — Cluster Linking with `ConsumerGroupSync: true` copies consumer group offsets alongside message data. This means consumers do not re-process messages or miss messages when they switch their bootstrap server from Kafka to Surgewave.
</details>

### 6. Running both Kafka and Surgewave simultaneously permanently doubles infrastructure costs with no migration benefit.

- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** — The hybrid phase is intentionally temporary (weeks, not months). The cost of running both systems is offset by the risk reduction: you can validate Surgewave under real production load and roll back instantly if problems occur. After a burn-in period, Kafka is decommissioned.
</details>

## Code Challenge

### 7. What is wrong with this dual-read implementation?

```csharp
public async Task<ConsumeResult<string, string>?> ConsumeAsync(TimeSpan timeout)
{
    var surgewaveResult = await _surgewaveConsumer.ConsumeAsync(timeout);
    if (surgewaveResult != null) return surgewaveResult;

    return await _kafkaConsumer.ConsumeAsync(timeout);
}
```

<details>
<summary>Answer</summary>
**The full `timeout` is spent waiting on Surgewave before Kafka is tried.**

If Surgewave has no messages and `timeout` is 5 seconds, the method waits 5 seconds on Surgewave and then potentially another 5 seconds on Kafka — total 10 seconds worst-case, which is 2x the expected latency.

Fix — use a short fast-path timeout for Surgewave:

```csharp
public async Task<ConsumeResult<string, string>?> ConsumeAsync(TimeSpan timeout)
{
    if (_surgewavePrimary)
    {
        var fastTimeout = TimeSpan.FromMilliseconds(200);
        var surgewaveResult = await _surgewaveConsumer.ConsumeAsync(fastTimeout);
        if (surgewaveResult != null) return surgewaveResult;

        // Remaining time goes to Kafka fallback
        var remaining = timeout - fastTimeout;
        if (remaining > TimeSpan.Zero)
            return await _kafkaConsumer.ConsumeAsync(remaining);

        return null;
    }

    return await _kafkaConsumer.ConsumeAsync(timeout);
}
```
</details>

### 8. Design a rollback trigger for a payment processing consumer. What metrics should trigger automatic rollback and what should remain manual?

<details>
<summary>Answer</summary>
**Automatic rollback triggers** (fast, objective):

- Error rate > 1% over 60 seconds
- Processing latency P99 > 2x baseline for 30 seconds
- Consumer lag on Surgewave growing while Kafka lag is stable (backpressure)
- More than 3 consecutive deserialization failures

**Manual rollback only** (require human judgment):

- Business logic producing unexpected results (data quality)
- Regulatory compliance concerns
- Suspected data loss (requires audit before action)
- Planned scheduled maintenance

```csharp
public async Task MonitorAsync(CancellationToken ct)
{
    int errorCount = 0;
    int successCount = 0;

    while (!ct.IsCancellationRequested)
    {
        var msg = await _consumer.ConsumeAsync(TimeSpan.FromSeconds(1));
        if (msg == null) continue;

        try
        {
            await ProcessPaymentAsync(msg);
            successCount++;
            errorCount = 0; // reset window on success
        }
        catch (Exception ex)
        {
            errorCount++;
            _logger.LogError(ex, "Payment error #{N}", errorCount);

            double errorRate = (double)errorCount / (errorCount + successCount);

            if (errorRate > 0.01 && errorCount >= 3)
            {
                // Automatic rollback: error rate threshold exceeded
                await _rollback.RollbackAsync($"Error rate {errorRate:P1} exceeded 1%");
                return;
            }
        }
    }
}
```

Key principle: err on the side of caution for financial data — rollback first, investigate second.
</details>

## Score

- 8/8: Excellent! You are ready for production hybrid deployments
- 6-7/8: Good — review the rollback and dual-read sections
- 5/8: Review the lesson and retry
- <5/8: Re-do the lesson before proceeding
