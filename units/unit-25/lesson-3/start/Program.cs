// Surgewave Bootcamp - Unit 16, Lesson 3: Quota Management
//
// In this lesson, you'll learn:
// - Define and enforce tenant quotas
// - Implement token bucket rate limiting
// - Create fair scheduling policies
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

Console.WriteLine("=== Surgewave Bootcamp: Lesson 16.3 - Quota Management ===");
Console.WriteLine();

// TODO: Step 1 - Define TenantQuota record
// public record TenantQuota(
//     string TenantId,
//     long ProducerBytesPerSecond,
//     long ConsumerBytesPerSecond,
//     int MaxTopics,
//     int MaxPartitionsPerTopic,
//     long MaxStorageBytes)
// {
//     public static TenantQuota Standard(string tenantId) => new(...);
//     public static TenantQuota Premium(string tenantId) => new(...);
// }

// TODO: Step 2 - Display quota tiers
// Console.WriteLine("--- Tenant Quota Tiers ---");
// var standardQuota = TenantQuota.Standard("example");
// Console.WriteLine("Standard Tier:");
// Console.WriteLine($"  Producer: {standardQuota.ProducerBytesPerSecond / (1024 * 1024)} MB/s");
// Console.WriteLine($"  Consumer: {standardQuota.ConsumerBytesPerSecond / (1024 * 1024)} MB/s");
// Console.WriteLine($"  Max Topics: {standardQuota.MaxTopics}");
// Console.WriteLine($"  Storage: {standardQuota.MaxStorageBytes / (1024 * 1024 * 1024)} GB");
// Console.WriteLine();
//
// var premiumQuota = TenantQuota.Premium("example");
// Console.WriteLine("Premium Tier:");
// Console.WriteLine($"  Producer: {premiumQuota.ProducerBytesPerSecond / (1024 * 1024)} MB/s");
// Console.WriteLine($"  Consumer: {premiumQuota.ConsumerBytesPerSecond / (1024 * 1024)} MB/s");
// Console.WriteLine($"  Max Topics: {premiumQuota.MaxTopics}");
// Console.WriteLine($"  Storage: {premiumQuota.MaxStorageBytes / (1024 * 1024 * 1024)} GB");
// Console.WriteLine();

// TODO: Step 3 - Implement TokenBucket class
// public class TokenBucket
// {
//     private readonly long _capacity;
//     private readonly double _tokensPerSecond;
//     private double _tokens;
//     private DateTime _lastRefill;
//     private readonly object _lock = new();
//
//     public TokenBucket(long capacity, double tokensPerSecond) { ... }
//     public bool TryConsume(long tokens) { ... }
//     public double AvailableTokens { get { ... } }
//     private void Refill() { ... }
// }

// TODO: Step 4 - Demonstrate token bucket
// Console.WriteLine("--- Token Bucket Demo ---");
// var bucket = new TokenBucket(20 * 1024 * 1024, 10 * 1024 * 1024); // 20MB capacity, 10MB/s refill
// Console.WriteLine($"Initial tokens: {bucket.AvailableTokens:N0}");
//
// bucket.TryConsume(5 * 1024 * 1024);
// Console.WriteLine($"After consuming 5MB: {bucket.AvailableTokens:N0} tokens");
//
// bucket.TryConsume(10 * 1024 * 1024);
// Console.WriteLine($"After consuming 10MB: {bucket.AvailableTokens:N0} tokens");
//
// var success = bucket.TryConsume(10 * 1024 * 1024);
// Console.WriteLine($"After consuming 10MB: {(success ? "Success" : "Failed (rate limited)")}");
// Console.WriteLine();

// TODO: Step 5 - Implement QuotaManager class
// public class QuotaManager
// {
//     private readonly Dictionary<string, TenantQuota> _quotas = new();
//     private readonly Dictionary<string, TokenBucket> _producerBuckets = new();
//     private readonly Dictionary<string, QuotaUsage> _usage = new();
//
//     public void SetQuota(TenantQuota quota) { ... }
//     public QuotaCheckResult CheckProducerQuota(string tenantId, long bytes) { ... }
//     public QuotaUsage? GetUsage(string tenantId) { ... }
// }

// TODO: Step 6 - Demonstrate quota manager
// Console.WriteLine("--- Quota Manager Demo ---");
// var quotaManager = new QuotaManager();
// quotaManager.SetQuota(TenantQuota.Standard("acme"));
// quotaManager.SetQuota(TenantQuota.Premium("globex"));
// Console.WriteLine("Registered quotas for: acme (Standard), globex (Premium)");
// Console.WriteLine();
//
// Console.WriteLine("Simulating high-volume traffic for tenant 'acme'...");
// var messageSize = 1024L;
// var allowed = 0;
// var throttled = 0;
//
// for (int i = 0; i < 15000; i++)
// {
//     var result = quotaManager.CheckProducerQuota("acme", messageSize);
//     if (result == QuotaCheckResult.Allowed)
//         allowed++;
//     else
//         throttled++;
// }
//
// Console.WriteLine($"Attempting 15000 messages of 1KB each...");
// Console.WriteLine($"Allowed: {allowed}, Throttled: {throttled}");
//
// var usage = quotaManager.GetUsage("acme");
// Console.WriteLine($"\nUsage for acme:");
// Console.WriteLine($"  Total Produced: {usage?.TotalBytesProduced:N0} bytes");
// Console.WriteLine();

// TODO: Step 7 - Test with Surgewave runtime (optional)
// Console.WriteLine("--- Surgewave Runtime Demo ---");
// await using var runtime = await SurgewaveRuntime.CreateBuilder()
//     .WithPort(0)
//     .WithStorageEngine(StorageEngines.Memory)
//     .WithAutoCreateTopics(true)
//     .Build()
//     .StartAsync();
// Console.WriteLine($"Runtime started on port {runtime.Port}");

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Token bucket allows smooth rate limiting with burst support");
Console.WriteLine("2. Per-tenant quotas prevent noisy neighbor problems");
Console.WriteLine("3. Usage tracking enables billing and capacity planning");
Console.WriteLine("4. Graceful degradation is better than hard failures");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
