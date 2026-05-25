# Knowledge Assessment: Lesson 15.3 - Failover Procedures

## Multiple Choice

### 1. What is the correct sequence for executing a failover?
- [ ] A) Switch DNS, verify secondary, notify team
- [ ] B) Detect failure, verify secondary is ready, switch DNS, validate health
- [ ] C) Restart primary, then switch to secondary
- [ ] D) Switch DNS immediately, then investigate

<details>
<summary>Answer</summary>
**B)** The correct sequence is: Detect failure -> Verify secondary is ready (replication caught up) -> Stop writes to primary if possible -> Switch DNS/LB -> Validate health. Skipping verification can lead to data loss or serving stale data.
</details>

### 2. What is the advantage of a hybrid (semi-automatic) failover approach?
- [ ] A) It's completely automatic with no human intervention
- [ ] B) It detects failures automatically but allows human approval before failover
- [ ] C) It requires manual detection and manual failover
- [ ] D) It only works during business hours

<details>
<summary>Answer</summary>
**B)** Hybrid failover automatically detects failures and alerts the team, but waits for human approval before executing. If no response within a timeout, it can auto-failover. This balances fast response with human judgment to avoid false positives.
</details>

### 3. Why should DNS TTL be kept low (e.g., 60 seconds) for services that may need failover?
- [ ] A) To reduce DNS server load
- [ ] B) To enable faster propagation of DNS changes during failover
- [ ] C) To improve security
- [ ] D) To save money

<details>
<summary>Answer</summary>
**B)** Lower DNS TTL means clients will re-query DNS more frequently, so DNS changes (pointing to the secondary DC) propagate faster. High TTL means clients may continue using cached entries pointing to the failed primary for longer.
</details>

## True/False

### 4. Automatic failover should be used for all failure scenarios to minimize RTO.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Automatic failover is best for clear, unambiguous failures. For complex scenarios (partial failures, network partitions), automatic failover risks split-brain situations or unnecessary failovers. Some scenarios require human judgment.
</details>

### 5. During failover, it's important to stop producers from writing to the primary before switching DNS.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - If possible, stopping writes to the primary prevents data from being written that won't be replicated. This minimizes data loss during the transition window. If the primary is completely unavailable, this step is skipped.
</details>

## Code Challenge

### 6. What issues exist in this failover detection implementation?

```csharp
public class FailoverDetector
{
    public bool ShouldFailover(string primaryEndpoint)
    {
        try
        {
            var client = new HttpClient();
            var response = client.GetAsync($"{primaryEndpoint}/health").Result;
            return !response.IsSuccessStatusCode;
        }
        catch
        {
            return true;
        }
    }
}
```

<details>
<summary>Answer</summary>
Issues:

1. **Single check**: One failure triggers failover; should require multiple consecutive failures
2. **No timeout**: GetAsync could hang indefinitely
3. **Blocking call**: .Result blocks the thread
4. **HttpClient creation**: Should reuse HttpClient instance
5. **Swallows exceptions**: Catches all exceptions without logging
6. **No threshold logic**: No configurable failure threshold
7. **No secondary verification**: Doesn't check if secondary is ready

Improved version:
```csharp
public class FailoverDetector
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FailoverDetector> _logger;
    private readonly FailoverConfig _config;
    private int _consecutiveFailures = 0;

    public FailoverDetector(
        HttpClient httpClient,
        ILogger<FailoverDetector> logger,
        IOptions<FailoverConfig> config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<FailoverDecision> CheckAndDecideAsync(
        string primaryEndpoint,
        string secondaryEndpoint)
    {
        // Check primary health
        var primaryHealthy = await CheckHealthAsync(primaryEndpoint, "primary");

        if (primaryHealthy)
        {
            _consecutiveFailures = 0;
            return new FailoverDecision { Action = FailoverAction.None };
        }

        _consecutiveFailures++;
        _logger.LogWarning(
            "Primary health check failed. Consecutive failures: {Count}",
            _consecutiveFailures);

        // Check if threshold reached
        if (_consecutiveFailures < _config.FailureThreshold)
        {
            return new FailoverDecision
            {
                Action = FailoverAction.Monitor,
                Message = $"Monitoring: {_consecutiveFailures}/{_config.FailureThreshold} failures"
            };
        }

        // Verify secondary is ready before recommending failover
        var secondaryHealthy = await CheckHealthAsync(secondaryEndpoint, "secondary");
        var replicationLag = await GetReplicationLagAsync();

        if (!secondaryHealthy)
        {
            return new FailoverDecision
            {
                Action = FailoverAction.AlertOnly,
                Message = "Primary down but secondary not healthy"
            };
        }

        if (replicationLag > _config.MaxAcceptableLag)
        {
            return new FailoverDecision
            {
                Action = FailoverAction.AlertOnly,
                Message = $"Primary down but replication lag too high: {replicationLag}"
            };
        }

        return new FailoverDecision
        {
            Action = FailoverAction.Failover,
            Message = "Primary down, secondary ready, recommending failover"
        };
    }

    private async Task<bool> CheckHealthAsync(string endpoint, string name)
    {
        try
        {
            using var cts = new CancellationTokenSource(_config.HealthCheckTimeout);
            var response = await _httpClient.GetAsync(
                $"{endpoint}/health",
                cts.Token);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for {Name} at {Endpoint}", name, endpoint);
            return false;
        }
    }
}

public class FailoverConfig
{
    public int FailureThreshold { get; set; } = 3;
    public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan MaxAcceptableLag { get; set; } = TimeSpan.FromMinutes(5);
}

public enum FailoverAction { None, Monitor, AlertOnly, Failover }
```
</details>

### 7. Implement a failover coordinator that executes the failover steps in order:

```csharp
public class FailoverCoordinator
{
    public async Task<FailoverResult> ExecuteFailoverAsync(
        string primaryEndpoint,
        string secondaryEndpoint)
    {
        // Implement the failover steps
    }
}
```

<details>
<summary>Answer</summary>
```csharp
public class FailoverCoordinator
{
    private readonly ILogger<FailoverCoordinator> _logger;
    private readonly IDnsService _dnsService;
    private readonly IReplicationService _replicationService;
    private readonly IHealthChecker _healthChecker;
    private readonly INotificationService _notifications;

    public FailoverCoordinator(
        ILogger<FailoverCoordinator> logger,
        IDnsService dnsService,
        IReplicationService replicationService,
        IHealthChecker healthChecker,
        INotificationService notifications)
    {
        _logger = logger;
        _dnsService = dnsService;
        _replicationService = replicationService;
        _healthChecker = healthChecker;
        _notifications = notifications;
    }

    public async Task<FailoverResult> ExecuteFailoverAsync(
        string primaryEndpoint,
        string secondaryEndpoint,
        string dnsRecord)
    {
        var result = new FailoverResult
        {
            StartTime = DateTime.UtcNow,
            Steps = new List<FailoverStep>()
        };

        try
        {
            // Step 1: Log and notify start
            _logger.LogWarning("FAILOVER INITIATED from {Primary} to {Secondary}",
                primaryEndpoint, secondaryEndpoint);
            await _notifications.NotifyAsync("Failover initiated", AlertSeverity.Critical);
            result.Steps.Add(new FailoverStep("Failover initiated", true));

            // Step 2: Verify secondary is healthy
            _logger.LogInformation("Step 2: Verifying secondary health...");
            var secondaryHealthy = await _healthChecker.CheckHealthAsync(secondaryEndpoint);
            if (!secondaryHealthy)
            {
                throw new FailoverException("Secondary is not healthy, aborting failover");
            }
            result.Steps.Add(new FailoverStep("Secondary health verified", true));

            // Step 3: Check replication status
            _logger.LogInformation("Step 3: Checking replication lag...");
            var lag = await _replicationService.GetLagAsync();
            result.ReplicationLagAtFailover = lag;
            if (lag > TimeSpan.FromMinutes(10))
            {
                _logger.LogWarning("Replication lag is high: {Lag}", lag);
            }
            result.Steps.Add(new FailoverStep($"Replication lag: {lag}", true));

            // Step 4: Try to stop writes to primary (best effort)
            _logger.LogInformation("Step 4: Attempting to stop primary writes...");
            try
            {
                await StopPrimaryWritesAsync(primaryEndpoint);
                result.Steps.Add(new FailoverStep("Primary writes stopped", true));

                // Step 5: Wait for replication to catch up
                _logger.LogInformation("Step 5: Waiting for replication catch-up...");
                await _replicationService.WaitForCatchupAsync(TimeSpan.FromSeconds(30));
                result.Steps.Add(new FailoverStep("Replication caught up", true));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not stop primary writes (may be unavailable)");
                result.Steps.Add(new FailoverStep("Primary writes stop failed (expected if unavailable)", false));
            }

            // Step 6: Update DNS
            _logger.LogInformation("Step 6: Updating DNS to secondary...");
            await _dnsService.UpdateRecordAsync(dnsRecord, secondaryEndpoint);
            result.Steps.Add(new FailoverStep("DNS updated to secondary", true));

            // Step 7: Verify DNS propagation
            _logger.LogInformation("Step 7: Verifying DNS propagation...");
            await Task.Delay(TimeSpan.FromSeconds(5)); // Wait for TTL
            var resolvedEndpoint = await _dnsService.ResolveAsync(dnsRecord);
            if (!resolvedEndpoint.Contains(secondaryEndpoint))
            {
                _logger.LogWarning("DNS may not have propagated yet");
            }
            result.Steps.Add(new FailoverStep("DNS propagation verified", true));

            // Step 8: Final health check
            _logger.LogInformation("Step 8: Final health verification...");
            var finalHealth = await _healthChecker.CheckHealthAsync(secondaryEndpoint);
            result.Steps.Add(new FailoverStep("Final health check", finalHealth));

            // Complete
            result.Success = true;
            result.EndTime = DateTime.UtcNow;
            result.NewActiveEndpoint = secondaryEndpoint;

            _logger.LogWarning(
                "FAILOVER COMPLETED in {Duration}. New active: {Endpoint}",
                result.Duration,
                secondaryEndpoint);

            await _notifications.NotifyAsync(
                $"Failover completed successfully in {result.Duration}",
                AlertSeverity.Warning);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FAILOVER FAILED");
            result.Success = false;
            result.EndTime = DateTime.UtcNow;
            result.ErrorMessage = ex.Message;

            await _notifications.NotifyAsync(
                $"Failover FAILED: {ex.Message}",
                AlertSeverity.Critical);

            return result;
        }
    }
}

public class FailoverResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public bool Success { get; set; }
    public string? NewActiveEndpoint { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ReplicationLagAtFailover { get; set; }
    public List<FailoverStep> Steps { get; set; } = new();
}

public record FailoverStep(string Description, bool Success);
```
</details>

## Score
- 7/7: Excellent! You understand failover procedures
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding
