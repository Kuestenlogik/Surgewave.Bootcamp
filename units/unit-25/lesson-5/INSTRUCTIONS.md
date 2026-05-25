# Lesson 16.5: Instructions

Follow these step-by-step instructions to implement resource management.

## Step 1: Define Resource Allocation Configuration

Create records to track resource allocation:

```csharp
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
```

## Step 2: Create Resource Usage Tracker

Track current resource usage per tenant:

```csharp
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
```

## Step 3: Implement Resource Manager

Build a manager that enforces allocations:

```csharp
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

    public ResourceUtilization GetUtilization(string tenantId)
    {
        if (!_allocations.TryGetValue(tenantId, out var allocation) ||
            !_usage.TryGetValue(tenantId, out var usage))
            return null!;

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
```

## Step 4: Create Monitoring Metrics

Build a metrics collector for multi-tenant monitoring:

```csharp
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
        Console.WriteLine("=== Multi-Tenant Dashboard ===");
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
```

## Step 5: Implement Alert System

Create alerts for resource thresholds:

```csharp
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
```

## Complete Solution

See `completed/Program.cs` for the full implementation with:

- ResourceAllocation configuration
- ResourceUsage tracking
- ResourceManager enforcement
- TenantMetrics and MetricsAggregator
- AlertManager for threshold monitoring

## Knowledge Check

1. Why use reserved partitions for tenants?
2. How does storage quota enforcement work?
3. What metrics are important for multi-tenant monitoring?
4. When should alerts be triggered?
5. How would you implement tenant offboarding?

## Expected Output

```
=== Surgewave Bootcamp: Lesson 16.5 - Resource Management ===

--- Resource Allocation Tiers ---
Premium: 100 reserved, 300 max partitions, 500 GB storage
Standard: 50 reserved, 100 max partitions, 100 GB storage
Basic: 20 reserved, 50 max partitions, 20 GB storage

--- Registering Tenants ---
Registered acme with Premium allocation
Registered globex with Standard allocation
Registered initech with Basic allocation

--- Creating Topics ---
acme: Creating orders (6 partitions)... Allowed
acme: Creating events (3 partitions)... Allowed
acme: Creating logs (3 partitions)... Allowed
globex: Creating analytics (12 partitions)... Allowed
initech: Creating data (24 partitions)... Allowed

--- Simulating Storage Growth ---
acme.orders: 30 GB
acme.events: 25 GB
globex.analytics: 75 GB
initech.data: 18 GB

--- Resource Utilization ---
Tenant     | Partitions    | Storage       | Topics
-----------|---------------|---------------|---------------
acme       | 4% (12/300)   | 11% (55 GB)   | 1% (3/500)
globex     | 12% (12/100)  | 75% (75 GB)   | 1% (1/100)
initech    | 48% (24/50)   | 90% (18 GB)   | 5% (1/20)

--- Alerts ---
[WARN] globex: Storage usage elevated: 75%
[CRIT] initech: Storage usage critical: 90%

--- Metrics Dashboard ---
Active Tenants: 3

Tenant          | Produced     | Consumed     | Last Activity
----------------|--------------|--------------|---------------
acme            |       10,000 |        8,000 | 12:30:45
globex          |        5,000 |        4,500 | 12:30:44
initech         |        2,000 |        1,800 | 12:30:43

--- Key Takeaways ---
1. Resource allocation ensures fair distribution
2. Usage tracking enables quota enforcement
3. Monitoring identifies issues before they escalate
4. Alerts enable proactive capacity management

Lesson complete!
```

---

**Congratulations!** You have completed Unit 16: Multi-Tenancy.
