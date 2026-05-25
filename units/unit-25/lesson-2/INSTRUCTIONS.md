# Lesson 16.2: Instructions

Follow these step-by-step instructions to implement topic namespacing.

## Step 1: Define the Naming Strategy Interface

Create an interface for topic naming strategies:

```csharp
public interface ITopicNamingStrategy
{
    string BuildTopicName(string tenantId, string baseTopic);
    (string TenantId, string BaseTopic)? ParseTopicName(string fullTopicName);
    bool ValidateTopicName(string topicName);
    bool BelongsToTenant(string topicName, string tenantId);
}
```

## Step 2: Implement Prefix Naming Strategy

Create a simple prefix-based naming strategy:

```csharp
public class PrefixNamingStrategy : ITopicNamingStrategy
{
    private const char Separator = '.';

    public string BuildTopicName(string tenantId, string baseTopic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseTopic);
        return $"{tenantId}{Separator}{baseTopic}";
    }

    public (string TenantId, string BaseTopic)? ParseTopicName(string fullTopicName)
    {
        if (string.IsNullOrWhiteSpace(fullTopicName))
            return null;

        var separatorIndex = fullTopicName.IndexOf(Separator);
        if (separatorIndex <= 0 || separatorIndex >= fullTopicName.Length - 1)
            return null;

        var tenantId = fullTopicName[..separatorIndex];
        var baseTopic = fullTopicName[(separatorIndex + 1)..];
        return (tenantId, baseTopic);
    }

    public bool ValidateTopicName(string topicName)
    {
        if (string.IsNullOrWhiteSpace(topicName))
            return false;

        // Check for valid characters: alphanumeric, dots, hyphens, underscores
        foreach (var c in topicName)
        {
            if (!char.IsLetterOrDigit(c) && c != '.' && c != '-' && c != '_')
                return false;
        }

        return ParseTopicName(topicName) != null;
    }

    public bool BelongsToTenant(string topicName, string tenantId)
    {
        var parsed = ParseTopicName(topicName);
        return parsed?.TenantId == tenantId;
    }
}
```

## Step 3: Create the Namespace Manager

Build a manager that handles tenant topic operations:

```csharp
public class NamespaceManager
{
    private readonly ITopicNamingStrategy _namingStrategy;
    private readonly HashSet<string> _registeredTenants = new();
    private readonly HashSet<string> _createdTopics = new();

    public NamespaceManager(ITopicNamingStrategy namingStrategy)
    {
        _namingStrategy = namingStrategy;
    }

    public void RegisterTenant(string tenantId)
    {
        _registeredTenants.Add(tenantId);
    }

    public string CreateTopic(string tenantId, string baseTopic)
    {
        if (!_registeredTenants.Contains(tenantId))
            throw new InvalidOperationException($"Tenant '{tenantId}' is not registered");

        var fullName = _namingStrategy.BuildTopicName(tenantId, baseTopic);

        if (!_namingStrategy.ValidateTopicName(fullName))
            throw new ArgumentException($"Invalid topic name: {fullName}");

        _createdTopics.Add(fullName);
        return fullName;
    }

    public IEnumerable<string> GetTenantTopics(string tenantId)
    {
        return _createdTopics.Where(t => _namingStrategy.BelongsToTenant(t, tenantId));
    }

    public IEnumerable<string> GetAllTopics() => _createdTopics;
}
```

## Step 4: Demonstrate Multi-Tenant Topic Creation

Use the namespace manager with Surgewave:

```csharp
var namingStrategy = new PrefixNamingStrategy();
var namespaceManager = new NamespaceManager(namingStrategy);

// Register tenants
namespaceManager.RegisterTenant("acme");
namespaceManager.RegisterTenant("globex");
namespaceManager.RegisterTenant("initech");

// Create topics for each tenant
var acmeOrders = namespaceManager.CreateTopic("acme", "orders");
var acmeEvents = namespaceManager.CreateTopic("acme", "events");
var globexOrders = namespaceManager.CreateTopic("globex", "orders");
var globexMetrics = namespaceManager.CreateTopic("globex", "metrics");
var initechPayments = namespaceManager.CreateTopic("initech", "payments");

// List all topics per tenant
foreach (var tenantId in new[] { "acme", "globex", "initech" })
{
    Console.WriteLine($"Topics for {tenantId}:");
    foreach (var topic in namespaceManager.GetTenantTopics(tenantId))
    {
        Console.WriteLine($"  - {topic}");
    }
}
```

## Step 5: Test with Surgewave Runtime

Create and produce to namespaced topics:

```csharp
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

// Produce to each tenant's orders topic
foreach (var tenantId in new[] { "acme", "globex" })
{
    var topic = namingStrategy.BuildTopicName(tenantId, "orders");
    await producer.ProduceAsync(topic, $"{tenantId}-order-1", $"Order from {tenantId}");
    Console.WriteLine($"Produced to {topic}");
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation with:

- ITopicNamingStrategy interface
- PrefixNamingStrategy implementation
- NamespaceManager for tenant topic management
- Demonstration with Surgewave runtime

## Knowledge Check

1. What are the benefits of using a naming strategy interface?
2. How does prefix-based naming enable ACL rules?
3. Why validate topic names before creation?
4. How would you extend the naming strategy for hierarchical namespaces?
5. What happens if a tenant tries to access another tenant's topic?

## Expected Output

```
=== Surgewave Bootcamp: Lesson 16.2 - Topic Namespacing ===

--- Naming Strategy Examples ---
Build: acme + orders = acme.orders
Build: globex + events = globex.events
Parse: acme.orders = (acme, orders)
Parse: globex.events = (globex, events)
Validate: acme.orders = True
Validate: invalid topic! = False

--- Namespace Manager Demo ---
Registered tenants: acme, globex, initech

Created topics:
  acme.orders
  acme.events
  acme.inventory
  globex.orders
  globex.metrics
  initech.payments

--- Topics by Tenant ---
acme:
  - acme.orders
  - acme.events
  - acme.inventory
globex:
  - globex.orders
  - globex.metrics
initech:
  - initech.payments

--- Surgewave Runtime Demo ---
Starting runtime on dynamic port...
Runtime started on port 12345
Produced to acme.orders: order-1
Produced to globex.orders: order-1
Produced to initech.payments: payment-1

--- Key Takeaways ---
1. Consistent naming conventions simplify multi-tenancy
2. Prefix strategies enable easy filtering and ACLs
3. Validation prevents naming violations
4. Namespace managers centralize topic operations

Lesson complete!
```

---

**Next:** [Lesson 16.3: Quota Management](../lesson-3/README.md)
