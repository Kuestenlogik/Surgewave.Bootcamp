// Surgewave Bootcamp - Unit 16, Lesson 5: Resource Management
//
// In this lesson, you'll learn:
// - Partition allocation strategies per tenant
// - Storage limits and enforcement
// - Per-tenant monitoring and alerting
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

Console.WriteLine("=== Surgewave Bootcamp: Lesson 16.5 - Resource Management ===");
Console.WriteLine();

// TODO: Step 1 - Define ResourceAllocation record
// public record ResourceAllocation(
//     string TenantId,
//     int ReservedPartitions,
//     int MaxPartitions,
//     long MaxStorageBytes,
//     int MaxTopics)
// {
//     public static ResourceAllocation Premium(string tenantId) => new(...);
//     public static ResourceAllocation Standard(string tenantId) => new(...);
//     public static ResourceAllocation Basic(string tenantId) => new(...);
// }

Console.WriteLine("--- Resource Allocation Tiers ---");
Console.WriteLine("Premium: 100 reserved, 300 max partitions, 500 GB storage");
Console.WriteLine("Standard: 50 reserved, 100 max partitions, 100 GB storage");
Console.WriteLine("Basic: 20 reserved, 50 max partitions, 20 GB storage");
Console.WriteLine();

// TODO: Step 2 - Create ResourceUsage class
// public class ResourceUsage
// {
//     public string TenantId { get; }
//     public int CurrentPartitions { get; private set; }
//     public long CurrentStorageBytes { get; private set; }
//     public int CurrentTopics { get; private set; }
//
//     public void AddTopic(string topicName, int partitions, long initialBytes = 0) { ... }
//     public void UpdateTopicStorage(string topicName, long bytes) { ... }
// }

// TODO: Step 3 - Implement ResourceManager
// public class ResourceManager
// {
//     public void RegisterTenant(ResourceAllocation allocation) { ... }
//     public ResourceCheckResult CanCreateTopic(string tenantId, int partitions) { ... }
//     public ResourceCheckResult CanWriteData(string tenantId, long bytes) { ... }
//     public void RecordTopicCreated(string tenantId, string topicName, int partitions) { ... }
//     public ResourceUtilization GetUtilization(string tenantId) { ... }
// }

// TODO: Step 4 - Register tenants
// Console.WriteLine("--- Registering Tenants ---");
// var resourceManager = new ResourceManager();
// resourceManager.RegisterTenant(ResourceAllocation.Premium("acme"));
// resourceManager.RegisterTenant(ResourceAllocation.Standard("globex"));
// resourceManager.RegisterTenant(ResourceAllocation.Basic("initech"));
// Console.WriteLine("Registered acme with Premium allocation");
// Console.WriteLine("Registered globex with Standard allocation");
// Console.WriteLine("Registered initech with Basic allocation");
// Console.WriteLine();

// TODO: Step 5 - Create topics with quota checks
// Console.WriteLine("--- Creating Topics ---");
// var topicsToCreate = new[]
// {
//     ("acme", "orders", 6),
//     ("acme", "events", 3),
//     ("acme", "logs", 3),
//     ("globex", "analytics", 12),
//     ("initech", "data", 24),
// };
//
// foreach (var (tenant, topic, partitions) in topicsToCreate)
// {
//     var result = resourceManager.CanCreateTopic(tenant, partitions);
//     Console.Write($"{tenant}: Creating {topic} ({partitions} partitions)... ");
//     if (result.IsAllowed)
//     {
//         resourceManager.RecordTopicCreated(tenant, $"{tenant}.{topic}", partitions);
//         Console.WriteLine("Allowed");
//     }
//     else
//     {
//         Console.WriteLine($"Denied - {result.Reason}");
//     }
// }

// TODO: Step 6 - Create metrics aggregator
// public class TenantMetrics { ... }
// public class MetricsAggregator { ... }

// TODO: Step 7 - Create alert manager
// public class AlertManager
// {
//     public void CheckUtilization(ResourceUtilization utilization) { ... }
// }

// TODO: Step 8 - Display utilization and alerts
// Console.WriteLine("\n--- Resource Utilization ---");
// Console.WriteLine("Tenant     | Partitions    | Storage       | Topics");
// Console.WriteLine("-----------|---------------|---------------|---------------");
// foreach (var tenantId in new[] { "acme", "globex", "initech" })
// {
//     var util = resourceManager.GetUtilization(tenantId);
//     Console.WriteLine($"{tenantId,-10} | {util.PartitionUtilization:P0}          | {util.StorageUtilization:P0}          | {util.TopicUtilization:P0}");
// }

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Resource allocation ensures fair distribution");
Console.WriteLine("2. Usage tracking enables quota enforcement");
Console.WriteLine("3. Monitoring identifies issues before they escalate");
Console.WriteLine("4. Alerts enable proactive capacity management");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
