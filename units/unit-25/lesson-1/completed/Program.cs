// Surgewave Bootcamp - Unit 16, Lesson 1: Tenant Isolation Concepts
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 16.1 - Tenant Isolation Concepts ===");
Console.WriteLine();

// --- Isolation Levels Overview ---
Console.WriteLine("--- Isolation Levels ---");
Console.WriteLine("Shared: All tenants share topics, identified by headers");
Console.WriteLine("Namespace: Each tenant has dedicated topics (e.g., tenant-a.orders)");
Console.WriteLine("Dedicated: Each tenant has dedicated brokers");
Console.WriteLine();

// Step 5 - Register sample tenants
var registry = new TenantRegistry();
registry.Register(new TenantContext("acme", "Acme Corporation", IsolationLevel.Namespace));
registry.Register(new TenantContext("globex", "Globex Industries", IsolationLevel.Namespace));
registry.Register(new TenantContext("initech", "Initech", IsolationLevel.Dedicated));

// Step 6 - Display registered tenants
Console.WriteLine("--- Registered Tenants ---");
foreach (var tenant in registry.GetAllTenants())
{
    Console.WriteLine($"Tenant: {tenant.TenantName} ({tenant.TenantId})");
    Console.WriteLine($"  Isolation Level: {tenant.Isolation}");
    Console.WriteLine($"  Orders topic: {tenant.GetTopicName("orders")}");
    Console.WriteLine($"  Events topic: {tenant.GetTopicName("events")}");
    Console.WriteLine($"  Consumer group: {tenant.GetConsumerGroupId("processor")}");
    Console.WriteLine();
}

// Step 7 - Demonstrate active tenant with Surgewave runtime
Console.WriteLine("--- Active Tenant Demo ---");
var tenantId = TenantIdentifier.FromEnvironment() ?? "demo-tenant";
var currentTenant = registry.GetTenant(tenantId)
    ?? new TenantContext(tenantId, $"Tenant {tenantId}", IsolationLevel.Namespace);
Console.WriteLine($"Current tenant: {currentTenant.TenantId}");

// Step 8 - Create Surgewave runtime and produce to tenant topic
Console.WriteLine("Starting Surgewave runtime on dynamic port...");
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
Console.WriteLine($"Runtime started on port {runtime.Port}");

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var topic = currentTenant.GetTopicName("orders");
Console.WriteLine($"Created tenant topic: {topic}");
await producer.ProduceAsync(topic, "order-1", "Test order from tenant");
Console.WriteLine($"Produced test message to {topic}");
Console.WriteLine("Successfully demonstrated tenant isolation!");
Console.WriteLine();

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Multi-tenancy enables cost-efficient resource sharing");
Console.WriteLine("2. Tenant context encapsulates naming and isolation logic");
Console.WriteLine("3. Topic namespacing provides logical data isolation");
Console.WriteLine("4. Choose isolation level based on compliance and cost needs");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// --- Supporting Types ---

// Step 1 - IsolationLevel enum
public enum IsolationLevel
{
    Shared,      // Topics shared, tenant ID in message headers
    Namespace,   // Separate topics per tenant
    Dedicated    // Separate brokers per tenant
}

// Step 2 - TenantContext record
public record TenantContext(
    string TenantId,
    string TenantName,
    IsolationLevel Isolation)
{
    public string GetTopicName(string baseTopic) => $"{TenantId}.{baseTopic}";
    public string GetConsumerGroupId(string baseGroup) => $"{TenantId}-{baseGroup}";
}

// Step 3 - TenantIdentifier static class
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

// Step 4 - TenantRegistry class
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
