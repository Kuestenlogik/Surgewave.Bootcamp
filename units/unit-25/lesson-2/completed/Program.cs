// Surgewave Bootcamp - Unit 16, Lesson 2: Topic Namespacing
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Runtime;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 16.2 - Topic Namespacing ===");
Console.WriteLine();

// Step 3 - Demonstrate naming strategy
Console.WriteLine("--- Naming Strategy Examples ---");
var strategy = new PrefixNamingStrategy();
Console.WriteLine($"Build: acme + orders = {strategy.BuildTopicName("acme", "orders")}");
Console.WriteLine($"Build: globex + events = {strategy.BuildTopicName("globex", "events")}");

var parsed = strategy.ParseTopicName("acme.orders");
Console.WriteLine($"Parse: acme.orders = ({parsed?.TenantId}, {parsed?.BaseTopic})");

var parsed2 = strategy.ParseTopicName("globex.events");
Console.WriteLine($"Parse: globex.events = ({parsed2?.TenantId}, {parsed2?.BaseTopic})");

Console.WriteLine($"Validate: acme.orders = {strategy.ValidateTopicName("acme.orders")}");
Console.WriteLine($"Validate: invalid topic! = {strategy.ValidateTopicName("invalid topic!")}");
Console.WriteLine();

// Step 5 - Use the namespace manager
Console.WriteLine("--- Namespace Manager Demo ---");
var namespaceManager = new NamespaceManager(new PrefixNamingStrategy());
namespaceManager.RegisterTenant("acme");
namespaceManager.RegisterTenant("globex");
namespaceManager.RegisterTenant("initech");
Console.WriteLine("Registered tenants: acme, globex, initech");
Console.WriteLine();

// Create topics
namespaceManager.CreateTopic("acme", "orders");
namespaceManager.CreateTopic("acme", "events");
namespaceManager.CreateTopic("acme", "inventory");
namespaceManager.CreateTopic("globex", "orders");
namespaceManager.CreateTopic("globex", "metrics");
namespaceManager.CreateTopic("initech", "payments");

Console.WriteLine("Created topics:");
foreach (var topic in namespaceManager.GetAllTopics())
{
    Console.WriteLine($"  {topic}");
}
Console.WriteLine();

// Step 6 - Display topics by tenant
Console.WriteLine("--- Topics by Tenant ---");
foreach (var tenantId in new[] { "acme", "globex", "initech" })
{
    Console.WriteLine($"{tenantId}:");
    foreach (var topic in namespaceManager.GetTenantTopics(tenantId))
    {
        Console.WriteLine($"  - {topic}");
    }
}
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

// Produce to namespaced topics
var namingStrategy = new PrefixNamingStrategy();
foreach (var tenantId in new[] { "acme", "globex", "initech" })
{
    var baseTopic = tenantId == "initech" ? "payments" : "orders";
    var topic = namingStrategy.BuildTopicName(tenantId, baseTopic);
    await producer.ProduceAsync(topic, "order-1", $"Order from {tenantId}");
    Console.WriteLine($"Produced to {topic}: order-1");
}
Console.WriteLine();

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Consistent naming conventions simplify multi-tenancy");
Console.WriteLine("2. Prefix strategies enable easy filtering and ACLs");
Console.WriteLine("3. Validation prevents naming violations");
Console.WriteLine("4. Namespace managers centralize topic operations");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// --- Supporting Types ---

// Step 1 - ITopicNamingStrategy interface
public interface ITopicNamingStrategy
{
    string BuildTopicName(string tenantId, string baseTopic);
    (string TenantId, string BaseTopic)? ParseTopicName(string fullTopicName);
    bool ValidateTopicName(string topicName);
    bool BelongsToTenant(string topicName, string tenantId);
}

// Step 2 - PrefixNamingStrategy implementation
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

// Step 4 - NamespaceManager class
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

    public bool IsTenantRegistered(string tenantId) =>
        _registeredTenants.Contains(tenantId);

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
