// Unit 16 Project: SaaS Platform - Multi-Tenant Message Routing - COMPLETED
// A multi-tenant SaaS messaging platform with isolation and quotas

using System.Collections.Concurrent;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Abstractions;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Multi-Tenant SaaS Platform ===\n");

// Tenant registry and state
var tenantRegistry = new ConcurrentDictionary<string, TenantConfig>();
var tenantMetrics = new ConcurrentDictionary<string, TenantMetrics>();

// Start embedded broker
Console.WriteLine("[Surgewave] Starting embedded broker...");

await using var surgewave = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .Build()
    .StartAsync();

await using var client = await SurgewaveClient.Create(surgewave.BootstrapServers).BuildAsync();
var nativeClient = client.NativeClient!;

Console.WriteLine($"[Surgewave] Broker started on port {surgewave.Port}");

// Step 1: Register tenants
Console.WriteLine("\n[Setup] Registering tenants...");

var tenants = new[]
{
    new TenantConfig("acme", "ACME Corporation", TenantTier.Premium, 1000, 3, 10_000_000_000),
    new TenantConfig("globex", "Globex Inc", TenantTier.Standard, 500, 2, 5_000_000_000),
    new TenantConfig("initech", "Initech LLC", TenantTier.Basic, 100, 1, 1_000_000_000)
};

foreach (var tenant in tenants)
{
    tenantRegistry.TryAdd(tenant.TenantId, tenant);
    tenantMetrics.TryAdd(tenant.TenantId, new TenantMetrics());
    Console.WriteLine($"[Tenant] Registered: {tenant.TenantId} ({tenant.Tier} tier, {tenant.RateLimitPerSecond} msg/s limit)");
}

// Step 2: Create namespaced topics for each tenant
Console.WriteLine("\n[Namespace] Creating tenant topics...");

foreach (var tenant in tenantRegistry.Values)
{
    var context = new TenantContext(tenant.TenantId);
    var topicName = context.GetTopicName("orders");
    await nativeClient.Topics.CreateAsync(topicName, partitions: tenant.MaxPartitionsPerTopic);
    Console.WriteLine($"[Namespace] Created topic: {topicName} ({tenant.MaxPartitionsPerTopic} partition{(tenant.MaxPartitionsPerTopic > 1 ? "s" : "")})");
}

// Step 3: Rate limiting implementation
bool CheckRateLimit(string tenantId)
{
    if (!tenantRegistry.TryGetValue(tenantId, out var config))
        return false;

    var metrics = tenantMetrics.GetOrAdd(tenantId, _ => new TenantMetrics());

    if (metrics.CurrentSecondCount >= config.RateLimitPerSecond)
    {
        Interlocked.Increment(ref metrics.ThrottleCount);
        return false;
    }

    Interlocked.Increment(ref metrics.CurrentSecondCount);
    return true;
}

void ResetRateLimits()
{
    foreach (var metrics in tenantMetrics.Values)
    {
        Interlocked.Exchange(ref metrics.CurrentSecondCount, 0);
    }
}

// Step 4: Tenant-aware producer
async Task<bool> ProduceForTenant(string tenantId, OrderEvent order)
{
    if (!CheckRateLimit(tenantId))
    {
        Console.WriteLine($"[Quota] Tenant {tenantId}: Rate limit exceeded!");
        return false;
    }

    var context = new TenantContext(tenantId);
    var topicName = context.GetTopicName("orders");
    var json = JsonSerializer.Serialize(order);

    await nativeClient.Messaging.SendAsync(topicName, partition: 0, key: order.CustomerId, value: json);

    var metrics = tenantMetrics.GetOrAdd(tenantId, _ => new TenantMetrics());
    Interlocked.Increment(ref metrics.MessageCount);
    Interlocked.Add(ref metrics.ByteCount, json.Length);

    return true;
}

// Step 5: Produce messages for tenants
Console.WriteLine("\n[Producer] Tenant acme: Producing order messages...");

var acmeOrders = new[]
{
    new OrderEvent("ORD-001", "C-100", "Enterprise Widget", 1500.00m, DateTime.UtcNow),
    new OrderEvent("ORD-002", "C-101", "Premium Gadget", 750.00m, DateTime.UtcNow)
};

foreach (var order in acmeOrders)
{
    if (await ProduceForTenant("acme", order))
    {
        Console.WriteLine($"[Producer] acme.orders <- Order {order.OrderId} (customer: {order.CustomerId})");
    }
}

var acmeMetrics = tenantMetrics["acme"];
var acmeConfig = tenantRegistry["acme"];
Console.WriteLine($"[Quota] Tenant acme: {acmeMetrics.CurrentSecondCount}/{acmeConfig.RateLimitPerSecond} messages ({acmeMetrics.CurrentSecondCount * 100.0 / acmeConfig.RateLimitPerSecond:F1}% of limit)");

Console.WriteLine("\n[Producer] Tenant globex: Producing order messages...");

var globexOrder = new OrderEvent("ORD-001", "G-200", "Standard Service", 250.00m, DateTime.UtcNow);
if (await ProduceForTenant("globex", globexOrder))
{
    Console.WriteLine($"[Producer] globex.orders <- Order {globexOrder.OrderId} (customer: {globexOrder.CustomerId})");
}

var globexMetrics = tenantMetrics["globex"];
var globexConfig = tenantRegistry["globex"];
Console.WriteLine($"[Quota] Tenant globex: {globexMetrics.CurrentSecondCount}/{globexConfig.RateLimitPerSecond} messages ({globexMetrics.CurrentSecondCount * 100.0 / globexConfig.RateLimitPerSecond:F1}% of limit)");

Console.WriteLine("\n[Producer] Tenant initech: Testing rate limit...");

var initechConfig = tenantRegistry["initech"];
var throttledCount = 0;

for (int i = 0; i < 120; i++)
{
    var order = new OrderEvent($"ORD-{i:D4}", $"I-{300 + i}", "Basic Item", 25.00m, DateTime.UtcNow);
    if (!await ProduceForTenant("initech", order))
    {
        throttledCount++;
    }
}

var initechMetrics = tenantMetrics["initech"];
Console.WriteLine($"[Quota] Tenant initech: {initechMetrics.CurrentSecondCount}/{initechConfig.RateLimitPerSecond} messages produced");
Console.WriteLine($"[Quota] Throttled {throttledCount} messages for tenant initech");

// Step 6: Consume messages for a tenant
Console.WriteLine("\n[Consumer] Tenant acme: Consuming from acme.orders...");

var acmeContext = new TenantContext("acme");
await using var consumer = client.CreateConsumer<string, string>();
consumer.Assign(acmeContext.GetTopicName("orders"), partition: 0, offset: 0);

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100), cts.Token);
        if (result != null)
        {
            var order = JsonSerializer.Deserialize<OrderEvent>(result.Value!);
            if (order != null)
            {
                Console.WriteLine($"[Consumer] Received: Order {order.OrderId} for customer {order.CustomerId}");
            }
        }
        else
        {
            break;
        }
    }
}
catch (OperationCanceledException)
{
    // Expected
}

// Step 7: Verify tenant isolation
Console.WriteLine("\n[Isolation] Testing cross-tenant access...");

// Attempt cross-tenant access (globex trying to access acme's topic)
var globexContext = new TenantContext("globex");
var acmeTopic = acmeContext.GetTopicName("orders"); // acme.orders

// In a real system, ACLs would block this. Here we simulate the check.
var isAuthorized = acmeTopic.StartsWith(globexContext.TenantId + ".");

if (!isAuthorized)
{
    Console.WriteLine($"[Isolation] BLOCKED: Tenant globex cannot access {acmeTopic}");
}

// Also verify globex can access their own topic
var globexTopic = globexContext.GetTopicName("orders");
var globexAuthorized = globexTopic.StartsWith(globexContext.TenantId + ".");

if (globexAuthorized)
{
    Console.WriteLine($"[Isolation] ALLOWED: Tenant globex can access {globexTopic}");
}

Console.WriteLine("[Isolation] Tenant isolation verified!");

// Step 8: Display per-tenant metrics
Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("[Metrics] Per-Tenant Usage Report:");

foreach (var (tenantId, config) in tenantRegistry.OrderBy(x => x.Key))
{
    var metrics = tenantMetrics.GetOrAdd(tenantId, _ => new TenantMetrics());
    var throttled = metrics.ThrottleCount > 0 ? " [THROTTLED]" : "";

    Console.WriteLine($"  {tenantId}: {metrics.MessageCount} messages, " +
                      $"{metrics.ByteCount / 1024.0:F1} KB ({config.Tier}){throttled}");
}

Console.WriteLine(new string('=', 50));
Console.WriteLine("\n=== Multi-Tenant Platform Complete ===");

// Tenant configuration
public record TenantConfig(
    string TenantId,
    string TenantName,
    TenantTier Tier,
    int RateLimitPerSecond,
    int MaxPartitionsPerTopic,
    long MaxStorageBytes
);

public enum TenantTier { Basic, Standard, Premium, Enterprise }

// Tenant context for scoped operations
public record TenantContext(string TenantId)
{
    public string GetTopicName(string baseTopic) => $"{TenantId}.{baseTopic}";
    public string GetConsumerGroup(string baseGroup) => $"{TenantId}-{baseGroup}";
}

// Tenant metrics
public class TenantMetrics
{
    public long MessageCount;
    public long ByteCount;
    public long ThrottleCount;
    public int CurrentSecondCount;
}

// Order event
public record OrderEvent(
    string OrderId,
    string CustomerId,
    string Product,
    decimal Amount,
    DateTime CreatedAt
);