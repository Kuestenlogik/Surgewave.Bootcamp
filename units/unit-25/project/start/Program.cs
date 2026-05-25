// Unit 16 Project: SaaS Platform - Multi-Tenant Message Routing
// Build a multi-tenant SaaS messaging platform with isolation and quotas

using System.Collections.Concurrent;
using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Abstractions;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Multi-Tenant SaaS Platform ===\n");

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
public record TenantMetrics
{
    public long MessageCount;
    public long ByteCount;
    public long ThrottleCount;
}

// Order event
public record OrderEvent(
    string OrderId,
    string CustomerId,
    string Product,
    decimal Amount,
    DateTime CreatedAt
);

// Tenant registry
var tenantRegistry = new ConcurrentDictionary<string, TenantConfig>();
var tenantMetrics = new ConcurrentDictionary<string, TenantMetrics>();
var tenantRateLimits = new ConcurrentDictionary<string, int>(); // Current count this second

// Start embedded broker
Console.WriteLine("[Surgewave] Starting embedded broker...");

await using var surgewave = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .Build()
    .StartAsync();

await using var client = await SurgewaveClient.Create(surgewave.BootstrapServers).BuildAsync();
var nativeClient = client.NativeClient!;

Console.WriteLine($"[Surgewave] Broker started on port {surgewave.Port}");

// TODO: Step 1 - Register tenants
Console.WriteLine("\n[Setup] Registering tenants...");

// TODO: Register three tenants with different tiers:
// - acme: Premium tier, 1000 msg/s, 3 partitions
// - globex: Standard tier, 500 msg/s, 2 partitions
// - initech: Basic tier, 100 msg/s, 1 partition

// Hint: Use tenantRegistry.TryAdd(tenantId, config)
// Also initialize tenantMetrics[tenantId] = new TenantMetrics()

// TODO: Step 2 - Create namespaced topics for each tenant
Console.WriteLine("\n[Namespace] Creating tenant topics...");

// TODO: For each registered tenant, create "{tenantId}.orders" topic
// Use the tenant's MaxPartitionsPerTopic configuration
// Hint: foreach (var tenant in tenantRegistry.Values)
//       {
//           var context = new TenantContext(tenant.TenantId);
//           var topicName = context.GetTopicName("orders");
//           await nativeClient.Topics.CreateAsync(topicName, partitions: tenant.MaxPartitionsPerTopic);
//       }

// TODO: Step 3 - Implement rate limiting
Console.WriteLine("\n[Quota] Setting up rate limiting...");

// TODO: Implement CheckRateLimit method that:
// - Checks if tenant has exceeded their limit
// - Returns true if allowed, false if throttled
// - Updates tenant metrics on throttle

bool CheckRateLimit(string tenantId)
{
    // TODO: Get tenant config
    // TODO: Check current message count against limit
    // TODO: Return false and increment throttle count if exceeded
    // TODO: Return true and increment message count if allowed

    return true; // Placeholder
}

// TODO: Implement ResetRateLimits method (called every second)
void ResetRateLimits()
{
    // TODO: Reset all tenant rate limit counters to 0
}

// TODO: Step 4 - Tenant-aware producer
Console.WriteLine("\n[Producer] Starting tenant message production...");

// TODO: Implement tenant-aware produce function
async Task ProduceForTenant(string tenantId, OrderEvent order)
{
    // TODO: Check rate limit first
    // TODO: If throttled, log and return
    // TODO: Get tenant context
    // TODO: Produce to namespaced topic
    // TODO: Update metrics

    var context = new TenantContext(tenantId);
    var topicName = context.GetTopicName("orders");

    // TODO: Serialize and produce
    // var json = JsonSerializer.Serialize(order);
    // await nativeClient.Messaging.SendAsync(topicName, partition: 0, key: order.CustomerId, value: json);
}

// TODO: Produce sample orders for each tenant
// Tenant acme: 2 orders
// Tenant globex: 1 order
// Tenant initech: Test rate limit by trying to produce 150 messages

// TODO: Step 5 - Tenant-aware consumer with isolation
Console.WriteLine("\n[Consumer] Starting tenant message consumption...");

// TODO: Implement tenant-aware consume function
async Task ConsumeForTenant(string tenantId, CancellationToken ct)
{
    var context = new TenantContext(tenantId);
    var topicName = context.GetTopicName("orders");

    // TODO: Consume from tenant's namespaced topic using CreateConsumer<K,V>
    // TODO: Log received messages
}

// TODO: Consume messages for tenant "acme"

// TODO: Step 6 - Verify tenant isolation
Console.WriteLine("\n[Isolation] Testing cross-tenant access...");

// TODO: Attempt to access another tenant's topic
// This should fail or return no messages
// Example: Tenant "globex" tries to consume from "acme.orders"

// TODO: Log isolation verification result
Console.WriteLine("[Isolation] Tenant isolation verified!");

// TODO: Step 7 - Display per-tenant metrics
Console.WriteLine("\n[Metrics] Per-Tenant Usage Report:");

foreach (var (tenantId, config) in tenantRegistry.OrderBy(x => x.Key))
{
    var metrics = tenantMetrics.GetOrAdd(tenantId, _ => new TenantMetrics());
    var throttled = metrics.ThrottleCount > 0 ? " [THROTTLED]" : "";

    Console.WriteLine($"  {tenantId}: {metrics.MessageCount} messages, " +
                      $"{metrics.ByteCount / 1024.0:F1} KB ({config.Tier}){throttled}");
}

Console.WriteLine("\n=== Multi-Tenant Platform Complete ===");
