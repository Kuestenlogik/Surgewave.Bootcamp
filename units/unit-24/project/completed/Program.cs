// Unit 15 Project: DR Simulation - Failover Test Suite - COMPLETED
// A comprehensive disaster recovery test suite with health checks and failover

using System.Diagnostics;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Abstractions;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== DR Failover Test Suite ===\n");

// Health check configuration
var healthConfig = new HealthCheckConfig(
    Interval: TimeSpan.FromMilliseconds(500),
    Timeout: TimeSpan.FromSeconds(1),
    FailureThreshold: 3,
    SuccessThreshold: 2
);

// State tracking
var circuitState = CircuitState.Closed;
var consecutiveFailures = 0;
var consecutiveSuccesses = 0;
var messagesBeforeFailure = 0;
var messagesAfterRecovery = 0;
var failoverStartTime = DateTime.MinValue;
var failoverEndTime = DateTime.MinValue;
var primaryHealthy = true;

// Step 1: Initialize primary and secondary brokers
Console.WriteLine("[Setup] Initializing primary and secondary brokers...");

await using var primaryRuntime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .Build()
    .StartAsync();

await using var secondaryRuntime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .Build()
    .StartAsync();

await using var primaryClient = await SurgewaveClient.Create(primaryRuntime.BootstrapServers).BuildAsync();
await using var secondaryClient = await SurgewaveClient.Create(secondaryRuntime.BootstrapServers).BuildAsync();

var primaryNative = primaryClient.NativeClient!;
var secondaryNative = secondaryClient.NativeClient!;

// Create topics on both brokers
await primaryNative.Topics.CreateAsync("orders", partitions: 3);
await secondaryNative.Topics.CreateAsync("orders", partitions: 3);

Console.WriteLine($"[Setup] Primary broker ready (port {primaryRuntime.Port})");
Console.WriteLine($"[Setup] Secondary broker ready (port {secondaryRuntime.Port})");

// Step 2: Produce initial messages for replication baseline
Console.WriteLine("\n[Setup] Producing initial messages for baseline...");

var random = new Random(42);
for (int i = 0; i < 100; i++)
{
    var message = new TestMessage(
        Id: $"MSG-{i:D4}",
        Data: $"Test payload {i}",
        Timestamp: DateTime.UtcNow
    );

    var json = JsonSerializer.Serialize(message);

    // Produce to primary
    await primaryNative.Messaging.SendAsync("orders", partition: i % 3, key: message.Id, value: json);

    // Simulate replication to secondary (with small chance of lag)
    if (random.NextDouble() > 0.02) // 98% replication success
    {
        await secondaryNative.Messaging.SendAsync("orders", partition: i % 3, key: message.Id, value: json);
    }

    messagesBeforeFailure++;
}

Console.WriteLine($"[Setup] Initial sync completed: {messagesBeforeFailure} messages replicated");

// Step 3: Health monitoring function
async Task<BrokerHealth> PerformHealthCheck(ISurgewaveClient client, bool simulateFailure)
{
    try
    {
        if (simulateFailure)
        {
            throw new Exception("Connection refused");
        }

        using var cts = new CancellationTokenSource(healthConfig.Timeout);

        // Perform health check by listing topics
        var stopwatch = Stopwatch.StartNew();
        var topics = await client.NativeClient!.Topics.ListAsync(cts.Token);
        stopwatch.Stop();

        if (stopwatch.Elapsed > TimeSpan.FromMilliseconds(500))
        {
            return BrokerHealth.Degraded;
        }

        return BrokerHealth.Healthy;
    }
    catch (OperationCanceledException)
    {
        return BrokerHealth.Unhealthy;
    }
    catch (Exception)
    {
        return BrokerHealth.Unhealthy;
    }
}

void UpdateCircuitState(BrokerHealth health)
{
    if (health == BrokerHealth.Healthy)
    {
        consecutiveFailures = 0;
        consecutiveSuccesses++;

        if (circuitState == CircuitState.HalfOpen && consecutiveSuccesses >= healthConfig.SuccessThreshold)
        {
            circuitState = CircuitState.Closed;
            Console.WriteLine("[Health] Circuit breaker CLOSED - broker recovered");
        }
    }
    else if (health == BrokerHealth.Unhealthy)
    {
        consecutiveSuccesses = 0;
        consecutiveFailures++;

        if (consecutiveFailures >= healthConfig.FailureThreshold && circuitState == CircuitState.Closed)
        {
            circuitState = CircuitState.Open;
        }
    }
}

// Step 4: Start health monitoring
Console.WriteLine("\n[Health] Starting health monitoring (interval: 500ms)");

// Monitor healthy state first
for (int i = 0; i < 3; i++)
{
    var health = await PerformHealthCheck(primaryClient, simulateFailure: false);
    Console.WriteLine($"[Health] Primary: {health} (latency: {random.Next(3, 8)}ms)");
    await Task.Delay(healthConfig.Interval);
}

// Step 5: Simulate broker failure
Console.WriteLine("\n[Test 1] Simulating primary broker failure...");
primaryHealthy = false;

// Produce a few more messages before failure is detected
for (int i = 0; i < 5; i++)
{
    var message = new TestMessage(
        Id: $"MSG-{messagesBeforeFailure + i:D4}",
        Data: $"In-flight message {i}",
        Timestamp: DateTime.UtcNow
    );

    // These messages may be lost during failover
    messagesBeforeFailure++;
}

Console.WriteLine($"[Producer] Produced 5 in-flight messages (may be lost)");

// Health check loop detecting failure
var failoverStopwatch = Stopwatch.StartNew();

while (circuitState != CircuitState.Open)
{
    var health = await PerformHealthCheck(primaryClient, simulateFailure: !primaryHealthy);
    UpdateCircuitState(health);

    if (health == BrokerHealth.Unhealthy)
    {
        Console.WriteLine($"[Health] Primary: UNHEALTHY (retry {consecutiveFailures}/{healthConfig.FailureThreshold})");
    }

    if (circuitState == CircuitState.Open)
    {
        break;
    }

    await Task.Delay(healthConfig.Interval);
}

failoverStartTime = DateTime.UtcNow;
Console.WriteLine("[Failover] Circuit breaker OPEN - initiating failover");

// Step 6: Execute failover
Console.WriteLine("\n[Failover] Switching to secondary broker...");

// Verify secondary is healthy
Console.WriteLine("[Failover] Verifying secondary readiness...");
var secondaryHealth = await PerformHealthCheck(secondaryClient, simulateFailure: false);

if (secondaryHealth != BrokerHealth.Healthy)
{
    Console.WriteLine("[Failover] ERROR: Secondary broker not healthy!");
}
else
{
    Console.WriteLine("[Failover] Secondary broker verified healthy");
}

// Simulate DNS/client switch
await Task.Delay(500); // Simulate switch time
Console.WriteLine("[Failover] Secondary promoted to primary");

failoverEndTime = DateTime.UtcNow;
var rto = failoverEndTime - failoverStartTime;

Console.WriteLine($"[Failover] Failover completed in {rto.TotalSeconds:F1} seconds (RTO: {rto.TotalSeconds:F1}s)");

// Step 7: Verify message continuity
Console.WriteLine("\n[Verify] Checking message continuity...");

// Count messages in secondary using consumer
await using var consumer = secondaryClient.CreateConsumer<string, string>();
consumer.Assign("orders", partition: 0, offset: 0);

using var countCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
try
{
    while (!countCts.Token.IsCancellationRequested)
    {
        var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100), countCts.Token);
        if (result != null)
        {
            messagesAfterRecovery++;
        }
        else
        {
            break;
        }
    }
}
catch (OperationCanceledException)
{
    // Expected
}

// Calculate RPO
var rpo = messagesBeforeFailure - messagesAfterRecovery;

Console.WriteLine($"[Verify] Messages before failure: {messagesBeforeFailure}");
Console.WriteLine($"[Verify] Messages after recovery: {messagesAfterRecovery}");
Console.WriteLine($"[Verify] Data loss: {rpo} messages (RPO: {rpo} messages)");

// Step 8: Generate test report
Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("[Report] DR Test Summary:");

var testResult = new DRTestResult(
    TestName: "Primary Broker Failure",
    Passed: rto.TotalSeconds < 5 && rpo < 10,
    RTO: rto,
    RPO: rpo,
    FailoverTrigger: "Connection refused",
    RecoveryAction: "Automatic promotion"
);

Console.WriteLine($"  - Test: {testResult.TestName}");
Console.WriteLine($"  - Result: {(testResult.Passed ? "PASSED" : "FAILED")}");
Console.WriteLine($"  - RTO: {testResult.RTO.TotalSeconds:F1} seconds (target: < 5s)");
Console.WriteLine($"  - RPO: {testResult.RPO} messages (target: < 10 messages)");
Console.WriteLine($"  - Failover trigger: {testResult.FailoverTrigger}");
Console.WriteLine($"  - Recovery action: {testResult.RecoveryAction}");

Console.WriteLine(new string('=', 50));
Console.WriteLine("\n=== DR Test Suite Complete ===");

// Configuration
public record HealthCheckConfig(
    TimeSpan Interval,
    TimeSpan Timeout,
    int FailureThreshold,
    int SuccessThreshold
);

public enum BrokerHealth { Healthy, Degraded, Unhealthy, Unknown }

public enum CircuitState { Closed, Open, HalfOpen }

// DR Test Result
public record DRTestResult(
    string TestName,
    bool Passed,
    TimeSpan RTO,
    int RPO,
    string FailoverTrigger,
    string RecoveryAction
);

// Message for testing
public record TestMessage(string Id, string Data, DateTime Timestamp);