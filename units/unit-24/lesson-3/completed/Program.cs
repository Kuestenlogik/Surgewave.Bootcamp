// Surgewave Bootcamp - Unit 15, Lesson 3: Failover Procedures
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 15.3 - Failover Procedures ===");
Console.WriteLine();

// Start primary and secondary clusters
Console.WriteLine("Starting primary cluster...");
await using var primaryRuntime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
Console.WriteLine($"  Primary started on port {primaryRuntime.Port}");

Console.WriteLine("Starting secondary cluster...");
await using var secondaryRuntime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
Console.WriteLine($"  Secondary started on port {secondaryRuntime.Port}");
Console.WriteLine();

// Step 1: Create failover configuration
var config = new FailoverConfig
{
    PrimaryBootstrapServers = $"localhost:{primaryRuntime.Port}",
    SecondaryBootstrapServers = $"localhost:{secondaryRuntime.Port}",
    HealthCheckInterval = TimeSpan.FromSeconds(1),
    FailureThreshold = 3,
    AutoFailoverEnabled = false
};

// Step 2: Create failover coordinator
var coordinator = new FailoverCoordinator(config);

// Subscribe to failover events
coordinator.FailoverTriggered += (sender, args) =>
{
    Console.WriteLine($"\n[EVENT] Failover triggered: {args.Reason}");
};

coordinator.FailoverCompleted += (sender, args) =>
{
    Console.WriteLine($"[EVENT] Failover completed: {args.Reason}");
};

// Step 3: Start health monitoring
using var cts = new CancellationTokenSource();
var monitorTask = Task.Run(() => coordinator.MonitorAsync(cts.Token));

Console.WriteLine("Failover coordinator started");
Console.WriteLine($"  Primary: {config.PrimaryBootstrapServers}");
Console.WriteLine($"  Secondary: {config.SecondaryBootstrapServers}");
Console.WriteLine($"  Auto-failover: {config.AutoFailoverEnabled}");
Console.WriteLine();

// Step 4: Initial health check
Console.WriteLine("Performing initial health checks...");
await Task.Delay(3000);
Console.WriteLine();

// Step 5: Display current status
coordinator.DisplayStatus();
Console.WriteLine();

// Step 6: Trigger manual failover
Console.WriteLine("Triggering manual failover...");
var result = await coordinator.ExecuteFailoverAsync(FailoverTrigger.Manual);

if (result.Success)
{
    Console.WriteLine($"\nFailover successful!");
    Console.WriteLine($"  New active cluster: {result.NewActiveCluster}");
    Console.WriteLine($"  Duration: {result.Duration}");
    Console.WriteLine($"  Validation: {(result.ValidationPassed ? "PASSED" : "FAILED")}");
}
else
{
    Console.WriteLine($"\nFailover failed: {result.Error}");
}
Console.WriteLine();

// Step 7: Display final status
coordinator.DisplayStatus();

// Step 8: Stop monitoring
cts.Cancel();
try { await monitorTask; } catch (OperationCanceledException) { }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Health checks detect cluster failures");
Console.WriteLine("2. Failure thresholds prevent false positives");
Console.WriteLine("3. Manual failover requires operator confirmation");
Console.WriteLine("4. Automatic failover reduces RTO but needs safeguards");
Console.WriteLine("5. Always validate after failover");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Configuration
public class FailoverConfig
{
    public string PrimaryBootstrapServers { get; set; } = "";
    public string SecondaryBootstrapServers { get; set; } = "";
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(10);
    public int FailureThreshold { get; set; } = 3;
    public TimeSpan MaxReplicationLag { get; set; } = TimeSpan.FromMinutes(5);
    public bool AutoFailoverEnabled { get; set; } = false;
    public TimeSpan AutoFailoverDelay { get; set; } = TimeSpan.FromMinutes(5);
}

// Health check
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

public class ClusterHealthChecker
{
    private readonly string _bootstrapServers;
    private int _consecutiveFailures;
    private HealthCheckResult? _lastResult;

    public ClusterHealthChecker(string bootstrapServers)
    {
        _bootstrapServers = bootstrapServers;
    }

    public HealthCheckResult? LastResult => _lastResult;

    public async Task<HealthCheckResult> CheckHealthAsync()
    {
        var result = new HealthCheckResult
        {
            Timestamp = DateTime.UtcNow,
            BootstrapServers = _bootstrapServers
        };

        try
        {
            await using var producer = new SurgewaveProducer<string, string>(options =>
            {
                options.BootstrapServers = _bootstrapServers;
            });

            // Try to produce a test message
            await producer.ProduceAsync("_health-check", "ping", DateTime.UtcNow.ToString("O"));

            result.IsHealthy = true;
            result.BrokerCount = 1; // Simplified for demo
            result.TopicCount = 1;
            _consecutiveFailures = 0;
        }
        catch (Exception ex)
        {
            result.IsHealthy = false;
            result.Error = ex.Message;
            _consecutiveFailures++;
        }

        result.ConsecutiveFailures = _consecutiveFailures;
        _lastResult = result;
        return result;
    }
}

// Failover events
public class FailoverEventArgs : EventArgs
{
    public DateTime Timestamp { get; set; }
    public string Reason { get; set; } = "";
}

// Enums
public enum FailoverTrigger { Manual, Automatic }
public enum ClusterRole { Primary, Secondary }

// Results
public class FailoverResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public FailoverTrigger Trigger { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string NewActiveCluster { get; set; } = "";
    public bool ValidationPassed { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
}

public class ValidationResult
{
    public bool ConnectivityOk { get; set; }
    public bool TopicsAccessible { get; set; }
    public bool ProduceConsumeOk { get; set; }
    public bool IsValid { get; set; }
}

public class FailoverStatus
{
    public string ActiveCluster { get; set; } = "";
    public bool PrimaryHealthy { get; set; }
    public bool SecondaryHealthy { get; set; }
    public DateTime? LastFailoverTime { get; set; }
}

// DNS Manager
public class DnsManager
{
    private readonly Dictionary<string, string> _records = new();

    public void SetRecord(string hostname, string endpoint)
    {
        _records[hostname] = endpoint;
    }

    public string? ResolveRecord(string hostname)
    {
        return _records.TryGetValue(hostname, out var endpoint) ? endpoint : null;
    }

    public async Task UpdateWithPropagationAsync(string hostname, string endpoint)
    {
        SetRecord(hostname, endpoint);
        Console.WriteLine($"    DNS: {hostname} -> {endpoint}");
        Console.WriteLine("    Waiting for DNS propagation...");
        await Task.Delay(1000);
        Console.WriteLine("    DNS propagation complete");
    }
}

// Failover Coordinator
public class FailoverCoordinator
{
    private readonly FailoverConfig _config;
    private readonly ClusterHealthChecker _primaryChecker;
    private readonly ClusterHealthChecker _secondaryChecker;
    private readonly DnsManager _dnsManager = new();
    private ClusterRole _currentRole = ClusterRole.Primary;
    private DateTime? _failureDetectedAt;
    private DateTime? _lastFailoverTime;

    public event EventHandler<FailoverEventArgs>? FailoverTriggered;
    public event EventHandler<FailoverEventArgs>? FailoverCompleted;

    public FailoverCoordinator(FailoverConfig config)
    {
        _config = config;
        _primaryChecker = new ClusterHealthChecker(config.PrimaryBootstrapServers);
        _secondaryChecker = new ClusterHealthChecker(config.SecondaryBootstrapServers);

        // Initialize DNS
        _dnsManager.SetRecord("surgewave.example.com", config.PrimaryBootstrapServers);
    }

    public async Task MonitorAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var primaryHealth = await _primaryChecker.CheckHealthAsync();
                var secondaryHealth = await _secondaryChecker.CheckHealthAsync();

                Console.WriteLine($"  Primary: {(primaryHealth.IsHealthy ? "Healthy" : "UNHEALTHY")} " +
                                  $"(failures: {primaryHealth.ConsecutiveFailures})");
                Console.WriteLine($"  Secondary: {(secondaryHealth.IsHealthy ? "Healthy" : "UNHEALTHY")} " +
                                  $"(failures: {secondaryHealth.ConsecutiveFailures})");

                await EvaluateHealthAsync(primaryHealth, secondaryHealth);

                await Task.Delay(_config.HealthCheckInterval, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task EvaluateHealthAsync(HealthCheckResult primary, HealthCheckResult secondary)
    {
        if (_currentRole == ClusterRole.Primary &&
            primary.ConsecutiveFailures >= _config.FailureThreshold)
        {
            if (_failureDetectedAt == null)
            {
                _failureDetectedAt = DateTime.UtcNow;

                FailoverTriggered?.Invoke(this, new FailoverEventArgs
                {
                    Timestamp = _failureDetectedAt.Value,
                    Reason = "Primary cluster health check failures exceeded threshold"
                });
            }

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
            // Step 1: Verify secondary
            Console.WriteLine("  Step 1: Verifying secondary cluster...");
            var secondaryHealth = await _secondaryChecker.CheckHealthAsync();
            if (!secondaryHealth.IsHealthy)
            {
                result.Success = false;
                result.Error = "Secondary cluster is not healthy";
                return result;
            }
            Console.WriteLine("    Secondary cluster is healthy");

            // Step 2: Check replication lag (simulated)
            Console.WriteLine("  Step 2: Checking replication lag...");
            var lag = TimeSpan.FromSeconds(2);
            Console.WriteLine($"    Replication lag: {lag}");

            // Step 3: Switch active cluster
            Console.WriteLine("  Step 3: Switching active cluster...");
            _currentRole = ClusterRole.Secondary;
            result.NewActiveCluster = _config.SecondaryBootstrapServers;

            // Step 4: Update DNS
            Console.WriteLine("  Step 4: Updating DNS records...");
            await _dnsManager.UpdateWithPropagationAsync("surgewave.example.com", result.NewActiveCluster);

            // Step 5: Validate
            Console.WriteLine("  Step 5: Validating new active cluster...");
            var validation = await ValidateClusterAsync(result.NewActiveCluster);
            result.ValidationPassed = validation.IsValid;

            result.Success = true;
            result.EndTime = DateTime.UtcNow;
            _lastFailoverTime = result.EndTime;
            _failureDetectedAt = null;

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
            result.EndTime = DateTime.UtcNow;
            Console.WriteLine($"  FAILOVER FAILED: {ex.Message}");
        }

        return result;
    }

    public async Task<ValidationResult> ValidateClusterAsync(string bootstrapServers)
    {
        var result = new ValidationResult();

        Console.WriteLine("    Checking cluster connectivity...");
        var checker = new ClusterHealthChecker(bootstrapServers);
        var health = await checker.CheckHealthAsync();
        result.ConnectivityOk = health.IsHealthy;

        Console.WriteLine("    Checking topic accessibility...");
        result.TopicsAccessible = health.IsHealthy;

        Console.WriteLine("    Running produce/consume test...");
        result.ProduceConsumeOk = await TestProduceConsumeAsync(bootstrapServers);

        result.IsValid = result.ConnectivityOk && result.TopicsAccessible && result.ProduceConsumeOk;
        Console.WriteLine($"    Validation result: {(result.IsValid ? "PASSED" : "FAILED")}");

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

            var received = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
            return received?.Value == testMessage;
        }
        catch
        {
            return false;
        }
    }

    public void DisplayStatus()
    {
        var primaryHealth = _primaryChecker.LastResult;
        var secondaryHealth = _secondaryChecker.LastResult;

        Console.WriteLine("=== Failover Status ===");
        Console.WriteLine($"  Current active: {(_currentRole == ClusterRole.Primary ? _config.PrimaryBootstrapServers : _config.SecondaryBootstrapServers)}");
        Console.WriteLine($"  Primary healthy: {primaryHealth?.IsHealthy ?? false}");
        Console.WriteLine($"  Secondary healthy: {secondaryHealth?.IsHealthy ?? false}");
        Console.WriteLine($"  Last failover: {_lastFailoverTime?.ToString("O") ?? "Never"}");
    }

    public FailoverStatus GetStatus()
    {
        return new FailoverStatus
        {
            ActiveCluster = _currentRole == ClusterRole.Primary
                ? _config.PrimaryBootstrapServers
                : _config.SecondaryBootstrapServers,
            PrimaryHealthy = _primaryChecker.LastResult?.IsHealthy ?? false,
            SecondaryHealthy = _secondaryChecker.LastResult?.IsHealthy ?? false,
            LastFailoverTime = _lastFailoverTime
        };
    }
}
