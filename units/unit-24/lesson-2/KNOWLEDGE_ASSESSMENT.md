# Knowledge Assessment: Lesson 15.2 - Cross-Datacenter Replication

## Multiple Choice

### 1. What is MirrorMaker in the context of Surgewave/Kafka replication?
- [ ] A) A backup compression tool
- [ ] B) A replication tool that consumes from one cluster and produces to another
- [ ] C) A monitoring dashboard
- [ ] D) A schema validation tool

<details>
<summary>Answer</summary>
**B)** MirrorMaker is a replication tool that acts as both a consumer (reading from source cluster) and a producer (writing to target cluster). It enables asynchronous replication between datacenters.
</details>

### 2. What is the difference between Active-Passive and Active-Active replication?
- [ ] A) Active-Passive is faster
- [ ] B) In Active-Passive one DC handles writes; in Active-Active both DCs handle writes
- [ ] C) Active-Active requires less storage
- [ ] D) They are the same

<details>
<summary>Answer</summary>
**B)** In Active-Passive (Primary/Standby), only the primary DC handles writes while the secondary receives replicated data. In Active-Active, both datacenters handle writes with bidirectional replication, requiring conflict resolution.
</details>

### 3. How is replication lag typically measured?
- [ ] A) Number of consumers
- [ ] B) Time or offset difference between source and target
- [ ] C) Network bandwidth usage
- [ ] D) CPU utilization

<details>
<summary>Answer</summary>
**B)** Replication lag can be measured as time-based lag (how old is the latest replicated message) or offset-based lag (difference between source end offset and target offset). Both indicate how far behind the replica is.
</details>

## True/False

### 4. Identity topic mapping means topics keep the same name in both source and target clusters.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Identity mapping replicates topics with the same name (source: `orders` -> target: `orders`). This is simple but can cause confusion in Active-Active setups. Prefixed mapping (e.g., `dc-a.orders`) helps identify the origin.
</details>

### 5. Cross-datacenter replication provides synchronous writes to both datacenters.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Cross-datacenter replication is typically asynchronous. Synchronous replication across datacenters would introduce unacceptable latency due to network round-trip times. Async replication means there will always be some lag (RPO > 0).
</details>

## Code Challenge

### 6. What issues exist in this replication lag monitoring implementation?

```csharp
public class ReplicationMonitor
{
    public void CheckLag()
    {
        var sourceOffset = GetSourceEndOffset();
        var targetOffset = GetTargetEndOffset();
        var lag = sourceOffset - targetOffset;

        Console.WriteLine($"Lag: {lag}");
    }
}
```

<details>
<summary>Answer</summary>
Issues:

1. **Synchronous/blocking**: Should be async for better scalability
2. **No error handling**: Network/broker failures will crash the monitor
3. **No alerting**: Just prints to console, no alerts for critical lag
4. **No thresholds**: Doesn't evaluate if lag is acceptable
5. **No continuous monitoring**: One-time check instead of periodic
6. **Console output**: Should use proper logging

Improved version:
```csharp
public class ReplicationMonitor
{
    private readonly ILogger<ReplicationMonitor> _logger;
    private readonly IAlertService _alertService;
    private readonly ReplicationConfig _config;

    public ReplicationMonitor(
        ILogger<ReplicationMonitor> logger,
        IAlertService alertService,
        IOptions<ReplicationConfig> config)
    {
        _logger = logger;
        _alertService = alertService;
        _config = config.Value;
    }

    public async Task MonitorAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await CheckLagAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check replication lag");
            }

            await Task.Delay(_config.CheckInterval, ct);
        }
    }

    private async Task CheckLagAsync()
    {
        var sourceOffset = await GetSourceEndOffsetAsync();
        var targetOffset = await GetTargetEndOffsetAsync();
        var offsetLag = sourceOffset - targetOffset;

        var sourceTimestamp = await GetSourceLatestTimestampAsync();
        var targetTimestamp = await GetTargetLatestTimestampAsync();
        var timeLag = sourceTimestamp - targetTimestamp;

        _logger.LogInformation(
            "Replication lag - Offset: {OffsetLag}, Time: {TimeLag}",
            offsetLag, timeLag);

        // Check thresholds
        if (timeLag > _config.CriticalLagThreshold)
        {
            await _alertService.AlertAsync(
                AlertSeverity.Critical,
                $"Replication lag critical: {timeLag}");
        }
        else if (timeLag > _config.WarningLagThreshold)
        {
            await _alertService.AlertAsync(
                AlertSeverity.Warning,
                $"Replication lag elevated: {timeLag}");
        }

        // Record metrics
        Metrics.ReplicationOffsetLag.Set(offsetLag);
        Metrics.ReplicationTimeLagSeconds.Set(timeLag.TotalSeconds);
    }
}

public class ReplicationConfig
{
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan WarningLagThreshold { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan CriticalLagThreshold { get; set; } = TimeSpan.FromMinutes(5);
}
```
</details>

### 7. Design a topic mapping strategy for a Hub-and-Spoke replication topology with 3 regional DCs (US, EU, APAC) and 1 central hub:

<details>
<summary>Answer</summary>
```csharp
// Hub-and-Spoke topology:
// - Regional DCs (US, EU, APAC) produce to local topics
// - MirrorMaker replicates to central hub with region prefix
// - Hub aggregates data from all regions

public class HubSpokeTopicMapper : ITopicMapper
{
    public string MapToHub(string region, string sourceTopic)
    {
        // Regional topics get prefixed when replicated to hub
        // us.orders, eu.orders, apac.orders
        return $"{region.ToLower()}.{sourceTopic}";
    }

    public string MapFromHub(string hubTopic, string targetRegion)
    {
        // Hub topics keep original name when replicated back
        // Only replicate if not from the target region
        return hubTopic;
    }

    public bool ShouldReplicate(string region, string topic)
    {
        // Don't replicate internal or temporary topics
        if (topic.StartsWith("_") || topic.EndsWith(".tmp"))
            return false;

        // Don't replicate already-replicated topics (prevent loops)
        if (topic.Contains('.'))
            return false;

        return true;
    }
}

// Replication configuration
public class HubSpokeReplicationConfig
{
    public Dictionary<string, RegionConfig> Regions { get; set; } = new()
    {
        ["us"] = new RegionConfig
        {
            BootstrapServers = "us-broker-1:9092,us-broker-2:9092",
            TopicsToReplicate = new[] { "orders", "payments", "users" }
        },
        ["eu"] = new RegionConfig
        {
            BootstrapServers = "eu-broker-1:9092,eu-broker-2:9092",
            TopicsToReplicate = new[] { "orders", "payments", "users" }
        },
        ["apac"] = new RegionConfig
        {
            BootstrapServers = "apac-broker-1:9092,apac-broker-2:9092",
            TopicsToReplicate = new[] { "orders", "payments", "users" }
        }
    };

    public string HubBootstrapServers { get; set; } = "hub-broker-1:9092,hub-broker-2:9092";
}

public class RegionConfig
{
    public string BootstrapServers { get; set; } = "";
    public string[] TopicsToReplicate { get; set; } = Array.Empty<string>();
}

// Resulting topology:
// +-------+          +---------+          +-------+
// |  US   | -------> |   HUB   | <------- |  EU   |
// | orders|  us.orders         eu.orders | orders|
// +-------+     |         ^         |     +-------+
//               |         |         |
//               v         |         v
//           +-------+     |     +-------+
//           | APAC  | ----+     | Global|
//           | orders|  apac.orders | Analytics
//           +-------+           +-------+
//
// Hub contains:
// - us.orders (from US)
// - eu.orders (from EU)
// - apac.orders (from APAC)
// - global.* (aggregated/processed data)
```

Key considerations:
1. Prefix with region to identify origin
2. Filter to prevent replication loops
3. Exclude internal/temporary topics
4. Support selective topic replication per region
</details>

## Score
- 7/7: Excellent! You understand cross-datacenter replication
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding
