// Surgewave Bootcamp - Unit 16, Lesson 3: Quota Management
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Runtime;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 16.3 - Quota Management ===");
Console.WriteLine();

// Step 2 - Display quota tiers
Console.WriteLine("--- Tenant Quota Tiers ---");
var standardQuota = TenantQuota.Standard("example");
Console.WriteLine("Standard Tier:");
Console.WriteLine($"  Producer: {standardQuota.ProducerBytesPerSecond / (1024 * 1024)} MB/s");
Console.WriteLine($"  Consumer: {standardQuota.ConsumerBytesPerSecond / (1024 * 1024)} MB/s");
Console.WriteLine($"  Max Topics: {standardQuota.MaxTopics}");
Console.WriteLine($"  Storage: {standardQuota.MaxStorageBytes / (1024 * 1024 * 1024)} GB");
Console.WriteLine();

var premiumQuota = TenantQuota.Premium("example");
Console.WriteLine("Premium Tier:");
Console.WriteLine($"  Producer: {premiumQuota.ProducerBytesPerSecond / (1024 * 1024)} MB/s");
Console.WriteLine($"  Consumer: {premiumQuota.ConsumerBytesPerSecond / (1024 * 1024)} MB/s");
Console.WriteLine($"  Max Topics: {premiumQuota.MaxTopics}");
Console.WriteLine($"  Storage: {premiumQuota.MaxStorageBytes / (1024 * 1024 * 1024)} GB");
Console.WriteLine();

// Step 4 - Demonstrate token bucket
Console.WriteLine("--- Token Bucket Demo ---");
var bucket = new TokenBucket(20 * 1024 * 1024, 10 * 1024 * 1024); // 20MB capacity, 10MB/s refill
Console.WriteLine($"Initial tokens: {bucket.AvailableTokens:N0}");

bucket.TryConsume(5 * 1024 * 1024);
Console.WriteLine($"After consuming 5MB: {bucket.AvailableTokens:N0} tokens");

bucket.TryConsume(10 * 1024 * 1024);
Console.WriteLine($"After consuming 10MB: {bucket.AvailableTokens:N0} tokens");

var success = bucket.TryConsume(10 * 1024 * 1024);
Console.WriteLine($"After consuming 10MB: {(success ? "Success" : "Failed (rate limited)")}");
Console.WriteLine();

// Step 6 - Demonstrate quota manager
Console.WriteLine("--- Quota Manager Demo ---");
var quotaManager = new QuotaManager();
quotaManager.SetQuota(TenantQuota.Standard("acme"));
quotaManager.SetQuota(TenantQuota.Premium("globex"));
Console.WriteLine("Registered quotas for: acme (Standard), globex (Premium)");
Console.WriteLine();

Console.WriteLine("Simulating high-volume traffic for tenant 'acme'...");
var messageSize = 1024L;
var allowed = 0;
var throttled = 0;

for (int i = 0; i < 15000; i++)
{
    var result = quotaManager.CheckProducerQuota("acme", messageSize);
    if (result == QuotaCheckResult.Allowed)
        allowed++;
    else
        throttled++;
}

Console.WriteLine($"Attempting 15000 messages of 1KB each...");
Console.WriteLine($"Allowed: {allowed}, Throttled: {throttled}");

var usage = quotaManager.GetUsage("acme");
Console.WriteLine($"\nUsage for acme:");
Console.WriteLine($"  Total Produced: {usage?.TotalBytesProduced:N0} bytes ({usage?.TotalBytesProduced / (1024 * 1024)} MB)");
Console.WriteLine();

// Step 7 - Test with Surgewave runtime
Console.WriteLine("--- Surgewave Runtime Demo ---");
Console.WriteLine("Starting runtime on dynamic port...");
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
Console.WriteLine($"Runtime started on port {runtime.Port}");

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

Console.WriteLine("Producing messages with quota enforcement...");
var quotaAwareProducer = new QuotaAwareProducer<string, string>(producer, quotaManager, "acme");

var producedCount = 0;
var rejectedCount = 0;
for (int i = 0; i < 100; i++)
{
    var produced = await quotaAwareProducer.TryProduceAsync("acme.orders", $"key-{i}", $"Order {i}");
    if (produced)
    {
        producedCount++;
        Console.WriteLine($"Message {i + 1}: Allowed");
    }
    else
    {
        rejectedCount++;
        Console.WriteLine($"Message {i + 1}: Rate limited - backing off");
        await Task.Delay(100); // Back off when rate limited
    }
}
Console.WriteLine($"\nTotal: {producedCount} produced, {rejectedCount} rate limited");
Console.WriteLine();

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Token bucket allows smooth rate limiting with burst support");
Console.WriteLine("2. Per-tenant quotas prevent noisy neighbor problems");
Console.WriteLine("3. Usage tracking enables billing and capacity planning");
Console.WriteLine("4. Graceful degradation is better than hard failures");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// --- Supporting Types ---

// Step 1 - TenantQuota record
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

// Step 3 - TokenBucket class
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

// Step 5 - QuotaManager and supporting types
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

        _usage[tenantId].RecordThrottle();
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

        _usage[tenantId].RecordThrottle();
        return QuotaCheckResult.RateLimited;
    }

    public QuotaUsage? GetUsage(string tenantId) =>
        _usage.TryGetValue(tenantId, out var usage) ? usage : null;

    public TenantQuota? GetQuota(string tenantId) =>
        _quotas.TryGetValue(tenantId, out var quota) ? quota : null;
}

// Quota-aware producer wrapper
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
        // Estimate message size (simplified - in real implementation, calculate actual size)
        var estimatedSize = 1024L;

        var result = _quotaManager.CheckProducerQuota(_tenantId, estimatedSize);
        if (result != QuotaCheckResult.Allowed)
            return false;

        await _producer.ProduceAsync(topic, key, value);
        return true;
    }
}
