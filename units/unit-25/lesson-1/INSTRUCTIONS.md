# Lesson 16.1: Instructions

Follow these step-by-step instructions to implement tenant isolation concepts.

## Step 1: Create the Tenant Context Record

Define a record that holds tenant-specific information:

```csharp
public record TenantContext(
    string TenantId,
    string TenantName,
    IsolationLevel Isolation)
{
    public string GetTopicName(string baseTopic) => $"{TenantId}.{baseTopic}";
    public string GetConsumerGroupId(string baseGroup) => $"{TenantId}-{baseGroup}";
}

public enum IsolationLevel
{
    Shared,      // Topics shared, tenant ID in message headers
    Namespace,   // Separate topics per tenant
    Dedicated    // Separate brokers per tenant
}
```

## Step 2: Implement Tenant Identification

Extract tenant ID from various sources:

```csharp
public static class TenantIdentifier
{
    public static string? FromEnvironment() =>
        Environment.GetEnvironmentVariable("Surgewave_TENANT_ID");

    public static string? FromApiKey(string? apiKey) =>
        apiKey?.Split('-').FirstOrDefault();

    public static string? FromHeader(IDictionary<string, string>? headers) =>
        headers?.TryGetValue("X-Tenant-Id", out var tenantId) == true
            ? tenantId
            : null;
}
```

## Step 3: Build a Tenant Registry

Create a simple in-memory tenant registry:

```csharp
public class TenantRegistry
{
    private readonly Dictionary<string, TenantContext> _tenants = new();

    public void Register(TenantContext tenant) =>
        _tenants[tenant.TenantId] = tenant;

    public TenantContext? GetTenant(string tenantId) =>
        _tenants.TryGetValue(tenantId, out var tenant) ? tenant : null;

    public IEnumerable<TenantContext> GetAllTenants() =>
        _tenants.Values;
}
```

## Step 4: Demonstrate Topic Naming

Show how different tenants get isolated topics:

```csharp
var registry = new TenantRegistry();

// Register tenants with different isolation levels
registry.Register(new TenantContext("acme", "Acme Corporation", IsolationLevel.Namespace));
registry.Register(new TenantContext("globex", "Globex Industries", IsolationLevel.Namespace));
registry.Register(new TenantContext("initech", "Initech", IsolationLevel.Dedicated));

// Show topic naming
foreach (var tenant in registry.GetAllTenants())
{
    Console.WriteLine($"Tenant: {tenant.TenantName}");
    Console.WriteLine($"  Orders topic: {tenant.GetTopicName("orders")}");
    Console.WriteLine($"  Events topic: {tenant.GetTopicName("events")}");
    Console.WriteLine($"  Consumer group: {tenant.GetConsumerGroupId("processor")}");
}
```

## Step 5: Test with Surgewave Runtime

Create topics and produce messages with tenant context:

```csharp
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var tenantId = TenantIdentifier.FromEnvironment() ?? "demo-tenant";
var tenant = registry.GetTenant(tenantId)
    ?? new TenantContext(tenantId, $"Tenant {tenantId}", IsolationLevel.Namespace);

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

// Produce to tenant-specific topic
var topic = tenant.GetTopicName("orders");
await producer.ProduceAsync(topic, "order-1", "Test order");
Console.WriteLine($"Produced to tenant topic: {topic}");
```

## Complete Solution

See `completed/Program.cs` for the full implementation with:

- TenantContext record with naming helpers
- TenantIdentifier for multi-source tenant identification
- TenantRegistry for managing tenants
- Demonstration of tenant-scoped messaging

## Knowledge Check

1. What are the three main isolation levels for multi-tenancy?
2. Why would you choose namespace isolation over dedicated clusters?
3. How does the tenant context pattern help with topic naming?
4. What sources can be used to identify the current tenant?
5. What are the trade-offs between cost and isolation?

## Expected Output

```
=== Surgewave Bootcamp: Lesson 16.1 - Tenant Isolation Concepts ===

--- Isolation Levels ---
Shared: All tenants share topics, identified by headers
Namespace: Each tenant has dedicated topics (e.g., tenant-a.orders)
Dedicated: Each tenant has dedicated brokers

--- Registered Tenants ---
Tenant: Acme Corporation (acme)
  Isolation Level: Namespace
  Orders topic: acme.orders
  Events topic: acme.events
  Consumer group: acme-processor

Tenant: Globex Industries (globex)
  Isolation Level: Namespace
  Orders topic: globex.orders
  Events topic: globex.events
  Consumer group: globex-processor

Tenant: Initech (initech)
  Isolation Level: Dedicated
  Orders topic: initech.orders
  Events topic: initech.events
  Consumer group: initech-processor

--- Active Tenant Demo ---
Current tenant: demo-tenant
Starting Surgewave runtime on dynamic port...
Runtime started on port 12345
Created tenant topic: demo-tenant.orders
Produced test message to demo-tenant.orders
Successfully demonstrated tenant isolation!

--- Key Takeaways ---
1. Multi-tenancy enables cost-efficient resource sharing
2. Tenant context encapsulates naming and isolation logic
3. Topic namespacing provides logical data isolation
4. Choose isolation level based on compliance and cost needs

Lesson complete!
```

---

**Next:** [Lesson 16.2: Topic Namespacing](../lesson-2/README.md)
