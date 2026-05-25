# Lesson 16.3: Instructions

Follow these step-by-step instructions to implement quota management.

## Step 1: Define Tenant Quota Configuration

Create a record to hold quota settings:

```csharp
public record TenantQuota(
    string TenantId,
    long ProducerBytesPerSecond,
    long ConsumerBytesPerSecond,
    int MaxTopics,
    int MaxPartitionsPerTopic,
    long MaxStorageBytes)
{
    public static TenantQuota Standard(string tenantId) => new(
        tenantId,
        ProducerBytesPerSecond: 10 * 1024 * 1024,  // 10 MB/s
        ConsumerBytesPerSecond: 50 * 1024 * 1024,  // 50 MB/s
        MaxTopics: 100,
        MaxPartitionsPerTopic: 12,
        MaxStorageBytes: 100L * 1024 * 1024 * 1024  // 100 GB
    );

    public static TenantQuota Premium(string tenantId) => new(
        tenantId,
        ProducerBytesPerSecond: 100 * 1024 * 1024,  // 100 MB/s
        ConsumerBytesPerSecond: 500 * 1024 * 1024,  // 500 MB/s
        MaxTopics: 1000,
        MaxPartitionsPerTopic: 64,
        MaxStorageBytes: 1024L * 1024 * 1024 * 1024  // 1 TB
    );
}
```

## Step 2: Implement Token Bucket Rate Limiter

Create a thread-safe token bucket implementation:

```csharp
public class TokenBucket
{
    private readonly long _capacity;
    private readonly double _tokensPerSecond;
    private double _tokens;
    private DateTime _lastRefill;
    private readonly object _lock = new();

    public TokenBucket(long capacity, double tokensPerSecond)
    {
        _capacity = capacity;
        _tokensPerSecond = tokensPerSecond;
        _tokens = capacity;
        _lastRefill = DateTime.UtcNow;
    }

    public bool TryConsume(long tokens)
    {
        lock (_lock)
        {
            Refill();

            if (_tokens >= tokens)
            {
                _tokens -= tokens;
                return true;
            }

            return false;
        }
    }

    public double AvailableTokens
    {
        get
        {
            lock (_lock)
            {
                Refill();
                return _tokens;
            }
        }
    }

    private void Refill()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastRefill).TotalSeconds;
        var tokensToAdd = elapsed * _tokensPerSecond;

        _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
        _lastRefill = now;
    }
}
```

## Step 3: Create Quota Manager

Build a manager that tracks and enforces quotas per tenant:

```csharp
public class QuotaManager
{
    private readonly Dictionary<string, TenantQuota> _quotas = new();
    private readonly Dictionary<string, TokenBucket> _producerBuckets = new();
    private readonly Dictionary<string, TokenBucket> _consumerBuckets = new();
    private readonly Dictionary<string, QuotaUsage> _usage = new();

    public void SetQuota(TenantQuota quota)
    {
        _quotas[quota.TenantId] = quota;
        _producerBuckets[quota.TenantId] = new TokenBucket(
            quota.ProducerBytesPerSecond * 2,  // Allow burst up to 2x
            quota.ProducerBytesPerSecond);
        _consumerBuckets[quota.TenantId] = new TokenBucket(
            quota.ConsumerBytesPerSecond * 2,
            quota.ConsumerBytesPerSecond);
        _usage[quota.TenantId] = new QuotaUsage(quota.TenantId);
    }

    public QuotaCheckResult CheckProducerQuota(string tenantId, long bytes)
    {
        if (!_producerBuckets.TryGetValue(tenantId, out var bucket))
            return QuotaCheckResult.TenantNotFound;

        if (bucket.TryConsume(bytes))
        {
            _usage[tenantId].RecordProduced(bytes);
            return QuotaCheckResult.Allowed;
        }

        return QuotaCheckResult.RateLimited;
    }

    public QuotaCheckResult CheckConsumerQuota(string tenantId, long bytes)
    {
        if (!_consumerBuckets.TryGetValue(tenantId, out var bucket))
            return QuotaCheckResult.TenantNotFound;

        if (bucket.TryConsume(bytes))
        {
            _usage[tenantId].RecordConsumed(bytes);
            return QuotaCheckResult.Allowed;
        }

        return QuotaCheckResult.RateLimited;
    }

    public QuotaUsage? GetUsage(string tenantId) =>
        _usage.TryGetValue(tenantId, out var usage) ? usage : null;
}

public enum QuotaCheckResult
{
    Allowed,
    RateLimited,
    TenantNotFound
}

public class QuotaUsage
{
    public string TenantId { get; }
    public long TotalBytesProduced { get; private set; }
    public long TotalBytesConsumed { get; private set; }
    public int ThrottleCount { get; private set; }

    public QuotaUsage(string tenantId) => TenantId = tenantId;

    public void RecordProduced(long bytes) => TotalBytesProduced += bytes;
    public void RecordConsumed(long bytes) => TotalBytesConsumed += bytes;
    public void RecordThrottle() => ThrottleCount++;
}
```

## Step 4: Demonstrate Rate Limiting

Test the quota manager with simulated traffic:

```csharp
var quotaManager = new QuotaManager();
quotaManager.SetQuota(TenantQuota.Standard("acme"));
quotaManager.SetQuota(TenantQuota.Premium("globex"));

// Simulate high-volume producer
var tenantId = "acme";
var messageSize = 1024; // 1 KB per message
var allowed = 0;
var throttled = 0;

for (int i = 0; i < 15000; i++)  // Try to send 15K messages quickly
{
    var result = quotaManager.CheckProducerQuota(tenantId, messageSize);
    if (result == QuotaCheckResult.Allowed)
        allowed++;
    else
        throttled++;
}

Console.WriteLine($"Allowed: {allowed}, Throttled: {throttled}");
```

## Step 5: Integrate with Surgewave Producer

Create a quota-aware producer wrapper:

```csharp
public class QuotaAwareProducer<TKey, TValue>
{
    private readonly SurgewaveProducer<TKey, TValue> _producer;
    private readonly QuotaManager _quotaManager;
    private readonly string _tenantId;

    public QuotaAwareProducer(
        SurgewaveProducer<TKey, TValue> producer,
        QuotaManager quotaManager,
        string tenantId)
    {
        _producer = producer;
        _quotaManager = quotaManager;
        _tenantId = tenantId;
    }

    public async Task<bool> TryProduceAsync(string topic, TKey key, TValue value)
    {
        // Estimate message size (simplified)
        var estimatedSize = 1024L; // In real implementation, calculate actual size

        var result = _quotaManager.CheckProducerQuota(_tenantId, estimatedSize);
        if (result != QuotaCheckResult.Allowed)
            return false;

        await _producer.ProduceAsync(topic, key, value);
        return true;
    }
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation with:

- TenantQuota configuration
- TokenBucket rate limiter
- QuotaManager with usage tracking
- Simulated traffic demonstration

## Knowledge Check

1. What is the token bucket algorithm and why is it useful for rate limiting?
2. How does burst capacity help with traffic patterns?
3. Why track quota usage per tenant?
4. What happens when a tenant exceeds their quota?
5. How would you implement priority queuing for premium tenants?

## Expected Output

```
=== Surgewave Bootcamp: Lesson 16.3 - Quota Management ===

--- Tenant Quota Tiers ---
Standard Tier:
  Producer: 10 MB/s
  Consumer: 50 MB/s
  Max Topics: 100
  Storage: 100 GB

Premium Tier:
  Producer: 100 MB/s
  Consumer: 500 MB/s
  Max Topics: 1000
  Storage: 1 TB

--- Token Bucket Demo ---
Initial tokens: 20971520
After consuming 5MB: 15728640 tokens
After consuming 10MB: 5242880 tokens
After consuming 10MB: Failed (rate limited)

--- Quota Manager Demo ---
Registered quotas for: acme (Standard), globex (Premium)

Simulating high-volume traffic for tenant 'acme'...
Attempting 15000 messages of 1KB each...
Allowed: 10240, Throttled: 4760

Usage for acme:
  Total Produced: 10,485,760 bytes (10 MB)
  Throttle Events: 0

--- Surgewave Runtime Demo ---
Starting runtime on dynamic port...
Runtime started on port 12345
Producing messages with quota enforcement...
Message 1: Allowed
Message 2: Allowed
...
Message 100: Rate limited - backing off

--- Key Takeaways ---
1. Token bucket allows smooth rate limiting with burst support
2. Per-tenant quotas prevent noisy neighbor problems
3. Usage tracking enables billing and capacity planning
4. Graceful degradation is better than hard failures

Lesson complete!
```

---

**Next:** [Lesson 16.4: Access Control](../lesson-4/README.md)
