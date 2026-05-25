// Surgewave Bootcamp - Unit 16, Lesson 2: Topic Namespacing
//
// In this lesson, you'll learn:
// - Topic naming conventions for multi-tenancy
// - Prefix-based namespace strategies
// - Tenant topic management utilities
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

Console.WriteLine("=== Surgewave Bootcamp: Lesson 16.2 - Topic Namespacing ===");
Console.WriteLine();

// TODO: Step 1 - Define the ITopicNamingStrategy interface
// public interface ITopicNamingStrategy
// {
//     string BuildTopicName(string tenantId, string baseTopic);
//     (string TenantId, string BaseTopic)? ParseTopicName(string fullTopicName);
//     bool ValidateTopicName(string topicName);
//     bool BelongsToTenant(string topicName, string tenantId);
// }

// TODO: Step 2 - Implement the PrefixNamingStrategy class
// public class PrefixNamingStrategy : ITopicNamingStrategy
// {
//     private const char Separator = '.';
//
//     public string BuildTopicName(string tenantId, string baseTopic)
//     {
//         ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
//         ArgumentException.ThrowIfNullOrWhiteSpace(baseTopic);
//         return $"{tenantId}{Separator}{baseTopic}";
//     }
//
//     public (string TenantId, string BaseTopic)? ParseTopicName(string fullTopicName)
//     {
//         // Parse "tenant.topic" format
//     }
//
//     public bool ValidateTopicName(string topicName)
//     {
//         // Validate allowed characters
//     }
//
//     public bool BelongsToTenant(string topicName, string tenantId)
//     {
//         // Check if topic belongs to tenant
//     }
// }

// TODO: Step 3 - Demonstrate naming strategy
// Console.WriteLine("--- Naming Strategy Examples ---");
// var strategy = new PrefixNamingStrategy();
// Console.WriteLine($"Build: acme + orders = {strategy.BuildTopicName("acme", "orders")}");
// Console.WriteLine($"Build: globex + events = {strategy.BuildTopicName("globex", "events")}");
//
// var parsed = strategy.ParseTopicName("acme.orders");
// Console.WriteLine($"Parse: acme.orders = ({parsed?.TenantId}, {parsed?.BaseTopic})");
//
// Console.WriteLine($"Validate: acme.orders = {strategy.ValidateTopicName("acme.orders")}");
// Console.WriteLine($"Validate: invalid topic! = {strategy.ValidateTopicName("invalid topic!")}");
// Console.WriteLine();

// TODO: Step 4 - Create the NamespaceManager class
// public class NamespaceManager
// {
//     private readonly ITopicNamingStrategy _namingStrategy;
//     private readonly HashSet<string> _registeredTenants = new();
//     private readonly HashSet<string> _createdTopics = new();
//
//     public NamespaceManager(ITopicNamingStrategy namingStrategy)
//     {
//         _namingStrategy = namingStrategy;
//     }
//
//     public void RegisterTenant(string tenantId)
//     {
//         _registeredTenants.Add(tenantId);
//     }
//
//     public string CreateTopic(string tenantId, string baseTopic)
//     {
//         // Create and register tenant topic
//     }
//
//     public IEnumerable<string> GetTenantTopics(string tenantId)
//     {
//         // Get all topics for a tenant
//     }
// }

// TODO: Step 5 - Use the namespace manager
// Console.WriteLine("--- Namespace Manager Demo ---");
// var namespaceManager = new NamespaceManager(new PrefixNamingStrategy());
// namespaceManager.RegisterTenant("acme");
// namespaceManager.RegisterTenant("globex");
// namespaceManager.RegisterTenant("initech");
// Console.WriteLine("Registered tenants: acme, globex, initech");
// Console.WriteLine();
//
// // Create topics
// namespaceManager.CreateTopic("acme", "orders");
// namespaceManager.CreateTopic("acme", "events");
// namespaceManager.CreateTopic("acme", "inventory");
// namespaceManager.CreateTopic("globex", "orders");
// namespaceManager.CreateTopic("globex", "metrics");
// namespaceManager.CreateTopic("initech", "payments");
//
// Console.WriteLine("Created topics:");
// foreach (var topic in namespaceManager.GetAllTopics())
// {
//     Console.WriteLine($"  {topic}");
// }
// Console.WriteLine();

// TODO: Step 6 - Display topics by tenant
// Console.WriteLine("--- Topics by Tenant ---");
// foreach (var tenantId in new[] { "acme", "globex", "initech" })
// {
//     Console.WriteLine($"{tenantId}:");
//     foreach (var topic in namespaceManager.GetTenantTopics(tenantId))
//     {
//         Console.WriteLine($"  - {topic}");
//     }
// }
// Console.WriteLine();

// TODO: Step 7 - Test with Surgewave runtime
// Console.WriteLine("--- Surgewave Runtime Demo ---");
// Console.WriteLine("Starting runtime on dynamic port...");
// await using var runtime = await SurgewaveRuntime.CreateBuilder()
//     .WithPort(0)
//     .WithStorageMode(StorageMode.Memory)
//     .WithAutoCreateTopics(true)
//     .Build()
//     .StartAsync();
// Console.WriteLine($"Runtime started on port {runtime.Port}");
//
// await using var producer = new SurgewaveProducer<string, string>(options =>
// {
//     options.BootstrapServers = $"localhost:{runtime.Port}";
// });
//
// // Produce to namespaced topics
// var namingStrategy = new PrefixNamingStrategy();
// foreach (var tenantId in new[] { "acme", "globex", "initech" })
// {
//     var topic = namingStrategy.BuildTopicName(tenantId, "orders");
//     await producer.ProduceAsync(topic, "order-1", $"Order from {tenantId}");
//     Console.WriteLine($"Produced to {topic}: order-1");
// }
// Console.WriteLine();

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Consistent naming conventions simplify multi-tenancy");
Console.WriteLine("2. Prefix strategies enable easy filtering and ACLs");
Console.WriteLine("3. Validation prevents naming violations");
Console.WriteLine("4. Namespace managers centralize topic operations");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
