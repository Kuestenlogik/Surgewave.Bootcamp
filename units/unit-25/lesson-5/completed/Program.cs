// Surgewave Bootcamp - Unit 16, Lesson 5: Resource Management
// COMPLETED SOLUTION

Console.WriteLine("=== Surgewave Bootcamp: Lesson 16.5 - Resource Management ===");
Console.WriteLine();

Console.WriteLine("--- Resource Allocation Tiers ---");
var premiumExample = ResourceAllocation.Premium("example");
var standardExample = ResourceAllocation.Standard("example");
var basicExample = ResourceAllocation.Basic("example");
Console.WriteLine($"Premium: {premiumExample.ReservedPartitions} reserved, {premiumExample.MaxPartitions} max partitions, {premiumExample.MaxStorageBytes / (1024 * 1024 * 1024)} GB storage");
Console.WriteLine($"Standard: {standardExample.ReservedPartitions} reserved, {standardExample.MaxPartitions} max partitions, {standardExample.MaxStorageBytes / (1024 * 1024 * 1024)} GB storage");
Console.WriteLine($"Basic: {basicExample.ReservedPartitions} reserved, {basicExample.MaxPartitions} max partitions, {basicExample.MaxStorageBytes / (1024 * 1024 * 1024)} GB storage");
Console.WriteLine();

// Step 4 - Register tenants
Console.WriteLine("--- Registering Tenants ---");
var resourceManager = new ResourceManager();
resourceManager.RegisterTenant(ResourceAllocation.Premium("acme"));
Console.WriteLine("Registered acme with Premium allocation");
resourceManager.RegisterTenant(ResourceAllocation.Standard("globex"));
Console.WriteLine("Registered globex with Standard allocation");
resourceManager.RegisterTenant(ResourceAllocation.Basic("initech"));
Console.WriteLine("Registered initech with Basic allocation");
Console.WriteLine();

// Step 5 - Create topics with quota checks
Console.WriteLine("--- Creating Topics ---");
var topicsToCreate = new (string Tenant, string Topic, int Partitions)[]
{
    ("acme", "orders", 6),
    ("acme", "events", 3),
    ("acme", "logs", 3),
    ("globex", "analytics", 12),
    ("initech", "data", 24),
};

foreach (var (tenant, topic, partitions) in topicsToCreate)
{
    var result = resourceManager.CanCreateTopic(tenant, partitions);
    Console.Write($"{tenant}: Creating {topic} ({partitions} partitions)... ");
    if (result.IsAllowed)
    {
        resourceManager.RecordTopicCreated(tenant, $"{tenant}.{topic}", partitions);
        Console.WriteLine("Allowed");
    }
    else
    {
        Console.WriteLine($"Denied - {result.Reason}");
    }
}
Console.WriteLine();

// Step 6 - Simulate storage growth
Console.WriteLine("--- Simulating Storage Growth ---");
resourceManager.UpdateTopicStorage("acme", "acme.orders", 30L * 1024 * 1024 * 1024);
Console.WriteLine("acme.orders: 30 GB");
resourceManager.UpdateTopicStorage("acme", "acme.events", 25L * 1024 * 1024 * 1024);
Console.WriteLine("acme.events: 25 GB");
resourceManager.UpdateTopicStorage("globex", "globex.analytics", 75L * 1024 * 1024 * 1024);
Console.WriteLine("globex.analytics: 75 GB");
resourceManager.UpdateTopicStorage("initech", "initech.data", 18L * 1024 * 1024 * 1024);
Console.WriteLine("initech.data: 18 GB");
Console.WriteLine();

// Step 8 - Display utilization
Console.WriteLine("--- Resource Utilization ---");
Console.WriteLine("Tenant     | Partitions    | Storage       | Topics");
Console.WriteLine("-----------|---------------|---------------|---------------");
foreach (var tenantId in new[] { "acme", "globex", "initech" })
{
    var util = resourceManager.GetUtilization(tenantId);
    if (util != null)
    {
        Console.WriteLine($"{tenantId,-10} | {util.PartitionUtilization,12:P0} | {util.StorageUtilization,12:P0} | {util.TopicUtilization,12:P0}");
    }
}
Console.WriteLine();

// Step 7 - Check alerts
Console.WriteLine("--- Alerts ---");
var alertManager = new AlertManager();
foreach (var tenantId in new[] { "acme", "globex", "initech" })
{
    var util = resourceManager.GetUtilization(tenantId);
    if (util != null)
    {
        alertManager.CheckUtilization(util);
    }
}
Console.WriteLine();

// Metrics demonstration
Console.WriteLine("--- Metrics Dashboard ---");
var metricsAggregator = new MetricsAggregator();

// Simulate some activity
var acmeMetrics = metricsAggregator.GetOrCreate("acme");
acmeMetrics.RecordProduce(10000, 10L * 1024 * 1024);
acmeMetrics.RecordConsume(8000, 8L * 1024 * 1024);

var globexMetrics = metricsAggregator.GetOrCreate("globex");
globexMetrics.RecordProduce(5000, 5L * 1024 * 1024);
globexMetrics.RecordConsume(4500, 4500L * 1024);

var initechMetrics = metricsAggregator.GetOrCreate("initech");
initechMetrics.RecordProduce(2000, 2L * 1024 * 1024);
initechMetrics.RecordConsume(1800, 1800L * 1024);

metricsAggregator.PrintDashboard();
Console.WriteLine();

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Resource allocation ensures fair distribution");
Console.WriteLine("2. Usage tracking enables quota enforcement");
Console.WriteLine("3. Monitoring identifies issues before they escalate");
Console.WriteLine("4. Alerts enable proactive capacity management");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// --- Supporting Types ---

// Step 1 - ResourceAllocation record
public record ResourceAllocation(
    string TenantId,
    int ReservedPartitions,
    int MaxPartitions,
    long MaxStorageBytes,
    int MaxTopics)
{
    public static ResourceAllocation Premium(string tenantId) => new(
        tenantId,
        ReservedPartitions: 100,
        MaxPartitions: 300,
        MaxStorageBytes: 500L * 1024 * 1024 * 1024,  // 500 GB
        MaxTopics: 500);

    public static ResourceAllocation Standard(string tenantId) => new(
        tenantId,
        ReservedPartitions: 50,
        MaxPartitions: 100,
        MaxStorageBytes: 100L * 1024 * 1024 * 1024,  // 100 GB
        MaxTopics: 100);

    public static ResourceAllocation Basic(string tenantId) => new(
        tenantId,
        ReservedPartitions: 20,
        MaxPartitions: 50,
        MaxStorageBytes: 20L * 1024 * 1024 * 1024,   // 20 GB
        MaxTopics: 20);
}

// Step 2 - ResourceUsage class
public class ResourceUsage
{
    public string TenantId { get; }
    public int CurrentPartitions { get; private set; }
    public long CurrentStorageBytes { get; private set; }
    public int CurrentTopics { get; private set; }
    public Dictionary<string, long> TopicStorageBytes { get; } = new();

    public ResourceUsage(string tenantId) => TenantId = tenantId;

    public void AddTopic(string topicName, int partitions, long initialBytes = 0)
    {
        CurrentTopics++;
        CurrentPartitions += partitions;
        TopicStorageBytes[topicName] = initialBytes;
        CurrentStorageBytes += initialBytes;
    }

    public void UpdateTopicStorage(string topicName, long bytes)
    {
        if (TopicStorageBytes.TryGetValue(topicName, out var oldBytes))
        {
            CurrentStorageBytes -= oldBytes;
        }
        TopicStorageBytes[topicName] = bytes;
        CurrentStorageBytes += bytes;
    }

    public void RemoveTopic(string topicName, int partitions)
    {
        if (TopicStorageBytes.TryGetValue(topicName, out var bytes))
        {
            CurrentStorageBytes -= bytes;
            TopicStorageBytes.Remove(topicName);
        }
        CurrentTopics--;
        CurrentPartitions -= partitions;
    }
}

// Step 3 - ResourceManager
public record ResourceCheckResult(bool IsAllowed, string? Reason)
{
    public static ResourceCheckResult Allowed() => new(true, null);
    public static ResourceCheckResult QuotaExceeded(string reason) => new(false, reason);
    public static ResourceCheckResult TenantNotFound(string reason) => new(false, reason);
}

public record ResourceUtilization(
    string TenantId,
    double PartitionUtilization,
    double StorageUtilization,
    double TopicUtilization);

public class ResourceManager
{
    private readonly Dictionary<string, ResourceAllocation> _allocations = new();
    private readonly Dictionary<string, ResourceUsage> _usage = new();

    public void RegisterTenant(ResourceAllocation allocation)
    {
        _allocations[allocation.TenantId] = allocation;
        _usage[allocation.TenantId] = new ResourceUsage(allocation.TenantId);
    }

    public ResourceCheckResult CanCreateTopic(string tenantId, int partitions)
    {
        if (!_allocations.TryGetValue(tenantId, out var allocation))
            return ResourceCheckResult.TenantNotFound("Tenant not registered");

        var usage = _usage[tenantId];

        if (usage.CurrentTopics >= allocation.MaxTopics)
            return ResourceCheckResult.QuotaExceeded($"Topic limit reached: {allocation.MaxTopics}");

        if (usage.CurrentPartitions + partitions > allocation.MaxPartitions)
            return ResourceCheckResult.QuotaExceeded(
                $"Partition limit would be exceeded: {usage.CurrentPartitions + partitions}/{allocation.MaxPartitions}");

        return ResourceCheckResult.Allowed();
    }

    public ResourceCheckResult CanWriteData(string tenantId, long bytes)
    {
        if (!_allocations.TryGetValue(tenantId, out var allocation))
            return ResourceCheckResult.TenantNotFound("Tenant not registered");

        var usage = _usage[tenantId];

        if (usage.CurrentStorageBytes + bytes > allocation.MaxStorageBytes)
            return ResourceCheckResult.QuotaExceeded(
                $"Storage limit would be exceeded: {FormatBytes(usage.CurrentStorageBytes + bytes)}/{FormatBytes(allocation.MaxStorageBytes)}");

        return ResourceCheckResult.Allowed();
    }

    public void RecordTopicCreated(string tenantId, string topicName, int partitions)
    {
        if (_usage.TryGetValue(tenantId, out var usage))
        {
            usage.AddTopic(topicName, partitions);
        }
    }

    public void UpdateTopicStorage(string tenantId, string topicName, long bytes)
    {
        if (_usage.TryGetValue(tenantId, out var usage))
        {
            usage.UpdateTopicStorage(topicName, bytes);
        }
    }

    public ResourceUtilization? GetUtilization(string tenantId)
    {
        if (!_allocations.TryGetValue(tenantId, out var allocation) ||
            !_usage.TryGetValue(tenantId, out var usage))
            return null;

        return new ResourceUtilization(
            tenantId,
            (double)usage.CurrentPartitions / allocation.MaxPartitions,
            (double)usage.CurrentStorageBytes / allocation.MaxStorageBytes,
            (double)usage.CurrentTopics / allocation.MaxTopics);
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}

// Step 6 - Metrics
public class TenantMetrics
{
    public string TenantId { get; }
    public long MessagesProduced { get; private set; }
    public long MessagesConsumed { get; private set; }
    public long BytesProduced { get; private set; }
    public long BytesConsumed { get; private set; }
    public DateTime LastActivity { get; private set; }

    public TenantMetrics(string tenantId) => TenantId = tenantId;

    public void RecordProduce(long messages, long bytes)
    {
        MessagesProduced += messages;
        BytesProduced += bytes;
        LastActivity = DateTime.UtcNow;
    }

    public void RecordConsume(long messages, long bytes)
    {
        MessagesConsumed += messages;
        BytesConsumed += bytes;
        LastActivity = DateTime.UtcNow;
    }
}

public class MetricsAggregator
{
    private readonly Dictionary<string, TenantMetrics> _metrics = new();

    public TenantMetrics GetOrCreate(string tenantId)
    {
        if (!_metrics.TryGetValue(tenantId, out var metrics))
        {
            metrics = new TenantMetrics(tenantId);
            _metrics[tenantId] = metrics;
        }
        return metrics;
    }

    public IEnumerable<TenantMetrics> GetAllMetrics() => _metrics.Values;

    public void PrintDashboard()
    {
        Console.WriteLine($"Active Tenants: {_metrics.Count}");
        Console.WriteLine();
        Console.WriteLine("Tenant          | Produced     | Consumed     | Last Activity");
        Console.WriteLine("----------------|--------------|--------------|---------------");
        foreach (var m in _metrics.Values.OrderByDescending(m => m.MessagesProduced))
        {
            Console.WriteLine($"{m.TenantId,-15} | {m.MessagesProduced,12:N0} | {m.MessagesConsumed,12:N0} | {m.LastActivity:HH:mm:ss}");
        }
    }
}

// Step 7 - Alerts
public enum AlertSeverity { Info, Warning, Critical }

public record ResourceAlert(
    DateTime Timestamp,
    string TenantId,
    AlertSeverity Severity,
    string Resource,
    string Message);

public class AlertManager
{
    private readonly List<ResourceAlert> _alerts = new();
    private readonly double _warningThreshold;
    private readonly double _criticalThreshold;

    public AlertManager(double warningThreshold = 0.7, double criticalThreshold = 0.9)
    {
        _warningThreshold = warningThreshold;
        _criticalThreshold = criticalThreshold;
    }

    public void CheckUtilization(ResourceUtilization utilization)
    {
        CheckResource(utilization.TenantId, "Partitions", utilization.PartitionUtilization);
        CheckResource(utilization.TenantId, "Storage", utilization.StorageUtilization);
        CheckResource(utilization.TenantId, "Topics", utilization.TopicUtilization);
    }

    private void CheckResource(string tenantId, string resource, double utilization)
    {
        if (utilization >= _criticalThreshold)
        {
            AddAlert(tenantId, AlertSeverity.Critical, resource,
                $"{resource} usage critical: {utilization:P0}");
        }
        else if (utilization >= _warningThreshold)
        {
            AddAlert(tenantId, AlertSeverity.Warning, resource,
                $"{resource} usage elevated: {utilization:P0}");
        }
    }

    private void AddAlert(string tenantId, AlertSeverity severity, string resource, string message)
    {
        var alert = new ResourceAlert(DateTime.UtcNow, tenantId, severity, resource, message);
        _alerts.Add(alert);
        var prefix = severity == AlertSeverity.Critical ? "[CRIT]" :
                     severity == AlertSeverity.Warning ? "[WARN]" : "[INFO]";
        Console.WriteLine($"{prefix} {tenantId}: {message}");
    }

    public IEnumerable<ResourceAlert> GetRecentAlerts(int count) =>
        _alerts.TakeLast(count);
}
