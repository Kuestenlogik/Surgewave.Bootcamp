# Lesson 15.3: Instructions

Follow these step-by-step instructions to implement failover procedures.

## Step 1: Define Failover Configuration

Create configuration for the failover system:

```csharp
public class FailoverConfig
{
    public string PrimaryBootstrapServers { get; set; } = "";
    public string SecondaryBootstrapServers { get; set; } = "";
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(10);
    public int FailureThreshold { get; set; } = 3; // Consecutive failures before failover
    public TimeSpan MaxReplicationLag { get; set; } = TimeSpan.FromMinutes(5);
    public bool AutoFailoverEnabled { get; set; } = false;
    public TimeSpan AutoFailoverDelay { get; set; } = TimeSpan.FromMinutes(5);
}
```

## Step 2: Implement Health Checker

Monitor cluster health:

```csharp
public class ClusterHealthChecker
{
    private readonly string _bootstrapServers;
    private int _consecutiveFailures;

    public ClusterHealthChecker(string bootstrapServers)
    {
        _bootstrapServers = bootstrapServers;
    }

    public async Task<HealthCheckResult> CheckHealthAsync()
    {
        var result = new HealthCheckResult
        {
            Timestamp = DateTime.UtcNow,
            BootstrapServers = _bootstrapServers
        };

        try
        {
            // Try to connect and fetch metadata
            using var client = new SurgewaveAdminClient(options =>
            {
                options.BootstrapServers = _bootstrapServers;
            });

            var metadata = await client.GetMetadataAsync(TimeSpan.FromSeconds(5));

            result.IsHealthy = true;
            result.BrokerCount = metadata.Brokers.Count;
            result.TopicCount = metadata.Topics.Count;
            _consecutiveFailures = 0;
        }
        catch (Exception ex)
        {
            result.IsHealthy = false;
            result.Error = ex.Message;
            _consecutiveFailures++;
        }

        result.ConsecutiveFailures = _consecutiveFailures;
        return result;
    }
}

public class HealthCheckResult
{
    public DateTime Timestamp { get; set; }
    public string BootstrapServers { get; set; } = "";
    public bool IsHealthy { get; set; }
    public int BrokerCount { get; set; }
    public int TopicCount { get; set; }
    public int ConsecutiveFailures { get; set; }
    public string? Error { get; set; }
}
```

## Step 3: Create the Failover Coordinator

Implement the main failover logic:

```csharp
public class FailoverCoordinator
{
    private readonly FailoverConfig _config;
    private readonly ClusterHealthChecker _primaryChecker;
    private readonly ClusterHealthChecker _secondaryChecker;
    private ClusterRole _currentRole = ClusterRole.Primary;
    private DateTime? _failureDetectedAt;

    public event EventHandler<FailoverEventArgs>? FailoverTriggered;
    public event EventHandler<FailoverEventArgs>? FailoverCompleted;

    public FailoverCoordinator(FailoverConfig config)
    {
        _config = config;
        _primaryChecker = new ClusterHealthChecker(config.PrimaryBootstrapServers);
        _secondaryChecker = new ClusterHealthChecker(config.SecondaryBootstrapServers);
    }

    public async Task MonitorAsync(CancellationToken ct)
    {
        Console.WriteLine("Failover coordinator started");
        Console.WriteLine($"  Primary: {_config.PrimaryBootstrapServers}");
        Console.WriteLine($"  Secondary: {_config.SecondaryBootstrapServers}");
        Console.WriteLine($"  Auto-failover: {_config.AutoFailoverEnabled}");

        while (!ct.IsCancellationRequested)
        {
            var primaryHealth = await _primaryChecker.CheckHealthAsync();
            var secondaryHealth = await _secondaryChecker.CheckHealthAsync();

            await EvaluateHealthAsync(primaryHealth, secondaryHealth);

            await Task.Delay(_config.HealthCheckInterval, ct);
        }
    }

    private async Task EvaluateHealthAsync(
        HealthCheckResult primary,
        HealthCheckResult secondary)
    {
        Console.WriteLine($"  Primary: {(primary.IsHealthy ? "Healthy" : "UNHEALTHY")} " +
                          $"(failures: {primary.ConsecutiveFailures})");
        Console.WriteLine($"  Secondary: {(secondary.IsHealthy ? "Healthy" : "UNHEALTHY")} " +
                          $"(failures: {secondary.ConsecutiveFailures})");

        // Check if failover is needed
        if (_currentRole == ClusterRole.Primary &&
            primary.ConsecutiveFailures >= _config.FailureThreshold)
        {
            if (_failureDetectedAt == null)
            {
                _failureDetectedAt = DateTime.UtcNow;
                Console.WriteLine($"  ALERT: Primary failure detected at {_failureDetectedAt}");

                FailoverTriggered?.Invoke(this, new FailoverEventArgs
                {
                    Timestamp = _failureDetectedAt.Value,
                    Reason = "Primary cluster health check failures exceeded threshold"
                });
            }

            // Check for auto-failover
            if (_config.AutoFailoverEnabled &&
                secondary.IsHealthy &&
                DateTime.UtcNow - _failureDetectedAt > _config.AutoFailoverDelay)
            {
                await ExecuteFailoverAsync(FailoverTrigger.Automatic);
            }
        }
        else if (primary.IsHealthy)
        {
            _failureDetectedAt = null;
        }
    }
}
```

## Step 4: Implement Failover Execution

Execute the actual failover:

```csharp
public async Task<FailoverResult> ExecuteFailoverAsync(FailoverTrigger trigger)
{
    var result = new FailoverResult
    {
        StartTime = DateTime.UtcNow,
        Trigger = trigger
    };

    Console.WriteLine($"\n=== EXECUTING FAILOVER ({trigger}) ===");

    try
    {
        // Step 1: Verify secondary is ready
        Console.WriteLine("Step 1: Verifying secondary cluster...");
        var secondaryHealth = await _secondaryChecker.CheckHealthAsync();
        if (!secondaryHealth.IsHealthy)
        {
            result.Success = false;
            result.Error = "Secondary cluster is not healthy";
            return result;
        }
        Console.WriteLine("  Secondary cluster is healthy");

        // Step 2: Check replication lag
        Console.WriteLine("Step 2: Checking replication lag...");
        var lag = await GetReplicationLagAsync();
        if (lag > _config.MaxReplicationLag)
        {
            Console.WriteLine($"  WARNING: Replication lag ({lag}) exceeds threshold");
            // Continue anyway for DR scenarios
        }
        else
        {
            Console.WriteLine($"  Replication lag acceptable: {lag}");
        }

        // Step 3: Update active cluster
        Console.WriteLine("Step 3: Switching active cluster...");
        _currentRole = ClusterRole.Secondary;
        result.NewActiveCluster = _config.SecondaryBootstrapServers;

        // Step 4: Simulate DNS update
        Console.WriteLine("Step 4: Updating DNS records...");
        await SimulateDnsUpdateAsync(result.NewActiveCluster);

        // Step 5: Validate new cluster
        Console.WriteLine("Step 5: Validating new active cluster...");
        var validation = await ValidateClusterAsync(result.NewActiveCluster);
        result.ValidationPassed = validation.IsValid;

        result.Success = true;
        result.EndTime = DateTime.UtcNow;

        Console.WriteLine($"\n=== FAILOVER COMPLETE ===");
        Console.WriteLine($"  Duration: {result.Duration}");
        Console.WriteLine($"  New active: {result.NewActiveCluster}");

        FailoverCompleted?.Invoke(this, new FailoverEventArgs
        {
            Timestamp = DateTime.UtcNow,
            Reason = $"Failover to secondary completed ({trigger})"
        });
    }
    catch (Exception ex)
    {
        result.Success = false;
        result.Error = ex.Message;
        Console.WriteLine($"  FAILOVER FAILED: {ex.Message}");
    }

    return result;
}
```

## Step 5: Implement DNS Simulation

Simulate DNS switching:

```csharp
public class DnsManager
{
    private readonly Dictionary<string, string> _records = new();

    public void SetRecord(string hostname, string endpoint)
    {
        _records[hostname] = endpoint;
        Console.WriteLine($"  DNS: {hostname} -> {endpoint}");
    }

    public string? ResolveRecord(string hostname)
    {
        return _records.TryGetValue(hostname, out var endpoint) ? endpoint : null;
    }

    public async Task UpdateWithPropagationAsync(string hostname, string endpoint)
    {
        SetRecord(hostname, endpoint);

        // Simulate DNS propagation delay
        Console.WriteLine("  Waiting for DNS propagation...");
        await Task.Delay(2000); // Simulate 2 second propagation

        Console.WriteLine("  DNS propagation complete");
    }
}

private async Task SimulateDnsUpdateAsync(string newEndpoint)
{
    var dns = new DnsManager();
    await dns.UpdateWithPropagationAsync("surgewave.example.com", newEndpoint);
}
```

## Step 6: Add Manual Failover Interface

Allow operators to trigger manual failover:

```csharp
public class FailoverOperator
{
    private readonly FailoverCoordinator _coordinator;

    public FailoverOperator(FailoverCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public async Task<FailoverResult> TriggerManualFailoverAsync(string reason)
    {
        Console.WriteLine($"\nManual failover requested: {reason}");
        Console.WriteLine("Confirm failover? (y/n)");

        // In real implementation, wait for operator confirmation
        // For demo, auto-confirm
        Console.WriteLine("  Auto-confirming for demo...");

        return await _coordinator.ExecuteFailoverAsync(FailoverTrigger.Manual);
    }

    public void DisplayStatus()
    {
        var status = _coordinator.GetStatus();

        Console.WriteLine("\n=== Failover Status ===");
        Console.WriteLine($"  Current active: {status.ActiveCluster}");
        Console.WriteLine($"  Primary healthy: {status.PrimaryHealthy}");
        Console.WriteLine($"  Secondary healthy: {status.SecondaryHealthy}");
        Console.WriteLine($"  Last failover: {status.LastFailoverTime?.ToString() ?? "Never"}");
    }
}
```

## Step 7: Post-Failover Validation

Verify the cluster after failover:

```csharp
public async Task<ValidationResult> ValidateClusterAsync(string bootstrapServers)
{
    var result = new ValidationResult();

    // Check 1: Cluster connectivity
    Console.WriteLine("  Checking cluster connectivity...");
    var checker = new ClusterHealthChecker(bootstrapServers);
    var health = await checker.CheckHealthAsync();
    result.ConnectivityOk = health.IsHealthy;

    // Check 2: Topics accessible
    Console.WriteLine("  Checking topic accessibility...");
    result.TopicsAccessible = health.TopicCount > 0;

    // Check 3: Produce/consume test
    Console.WriteLine("  Running produce/consume test...");
    result.ProduceConsumeOk = await TestProduceConsumeAsync(bootstrapServers);

    result.IsValid = result.ConnectivityOk &&
                     result.TopicsAccessible &&
                     result.ProduceConsumeOk;

    Console.WriteLine($"  Validation result: {(result.IsValid ? "PASSED" : "FAILED")}");

    return result;
}

private async Task<bool> TestProduceConsumeAsync(string bootstrapServers)
{
    try
    {
        var testTopic = "_failover-test";
        var testMessage = $"test-{Guid.NewGuid():N}";

        await using var producer = new SurgewaveProducer<string, string>(options =>
        {
            options.BootstrapServers = bootstrapServers;
        });

        await producer.ProduceAsync(testTopic, "test", testMessage);

        await using var consumer = new SurgewaveConsumer<string, string>(options =>
        {
            options.BootstrapServers = bootstrapServers;
            options.GroupId = $"failover-test-{Guid.NewGuid():N}";
            options.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        consumer.Subscribe(testTopic);

        var received = consumer.Consume(TimeSpan.FromSeconds(5));
        return received?.Value == testMessage;
    }
    catch
    {
        return false;
    }
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation including:

- Complete failover coordinator
- Health checking with configurable thresholds
- Manual and automatic failover
- DNS simulation
- Post-failover validation

## Knowledge Check

1. What is the difference between manual and automatic failover?
2. How do health checks determine cluster status?
3. Why is replication lag important during failover?
4. What DNS TTL should you use for failover readiness?
5. What validation steps are needed after failover?

## Expected Output

```
=== Surgewave Bootcamp: Lesson 15.3 - Failover Procedures ===

Starting primary cluster...
  Primary started on port 9092
Starting secondary cluster...
  Secondary started on port 9093

Failover coordinator started
  Primary: localhost:9092
  Secondary: localhost:9093
  Auto-failover: false

Health monitoring...
  Primary: Healthy (failures: 0)
  Secondary: Healthy (failures: 0)

Simulating primary failure...
  Primary: UNHEALTHY (failures: 1)
  Primary: UNHEALTHY (failures: 2)
  Primary: UNHEALTHY (failures: 3)
  ALERT: Primary failure detected

Triggering manual failover...

=== EXECUTING FAILOVER (Manual) ===
Step 1: Verifying secondary cluster...
  Secondary cluster is healthy
Step 2: Checking replication lag...
  Replication lag acceptable: 00:00:02
Step 3: Switching active cluster...
Step 4: Updating DNS records...
  DNS: surgewave.example.com -> localhost:9093
  Waiting for DNS propagation...
  DNS propagation complete
Step 5: Validating new active cluster...
  Checking cluster connectivity...
  Checking topic accessibility...
  Running produce/consume test...
  Validation result: PASSED

=== FAILOVER COMPLETE ===
  Duration: 00:00:03.245
  New active: localhost:9093

Lesson complete!
```

---

**Next:** [Lesson 15.4: Data Recovery](../lesson-4/README.md)
