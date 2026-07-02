// Surgewave Bootcamp - Unit 9, Lesson 4: Common Issues
// COMPLETED SOLUTION

using System.Diagnostics;
using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 9.4 - Common Issues ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// ============== Demo 1: Consumer Lag Monitoring ==============
Console.WriteLine("--- Demo 1: Consumer Lag Monitoring ---");

var lagMonitor = new LagMonitor();

// Create producer
await using var producer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

// Produce a batch of messages
Console.WriteLine("  Producing 20 messages...");
for (var i = 0; i < 20; i++)
{
    await producer.ProduceAsync("lag-demo",
        Encoding.UTF8.GetBytes($"key-{i}"),
        Encoding.UTF8.GetBytes($"message-{i}"));
    lagMonitor.RecordProduce(i);
}

Console.WriteLine($"  Initial lag: {lagMonitor.CurrentLag} messages");
Console.WriteLine();

// Create slow consumer
await using var slowConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "slow-consumer-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

slowConsumer.Subscribe("lag-demo");

// Consume with slow processing to show lag
Console.WriteLine("  Consuming with slow processing (50ms per message)...");
var lagThreshold = 10;
var consumed = 0;
var consumeStart = DateTime.UtcNow;

while (consumed < 20 && DateTime.UtcNow - consumeStart < TimeSpan.FromSeconds(5))
{
    var result = await slowConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
    if (result != null)
    {
        // Simulate slow processing
        await Task.Delay(50);

        consumed++;
        lagMonitor.RecordCommit(consumed - 1);

        var lag = lagMonitor.CurrentLag;
        var lagStatus = lag > lagThreshold ? "HIGH" : "OK";
        Console.WriteLine($"    Consumed {consumed}, Lag: {lag} [{lagStatus}]");
    }
}

Console.WriteLine($"  Final lag: {lagMonitor.CurrentLag}");
Console.WriteLine();

// ============== Demo 2: Rebalance Handling ==============
Console.WriteLine("--- Demo 2: Rebalance Handling ---");

var rebalanceTracker = new RebalanceTracker();

// Simulate partition assignments
Console.WriteLine("  Simulating consumer group rebalancing...");
Console.WriteLine();

// Consumer 1 joins - gets all partitions
Console.WriteLine("  Consumer-1 joins group:");
rebalanceTracker.OnPartitionsAssigned("consumer-1", new[] { 0, 1, 2 });

await Task.Delay(100);

// Consumer 2 joins - triggers rebalance
Console.WriteLine("  Consumer-2 joins group (rebalance triggered):");
rebalanceTracker.OnPartitionsRevoked("consumer-1", new[] { 1, 2 });
rebalanceTracker.OnPartitionsAssigned("consumer-2", new[] { 1, 2 });

await Task.Delay(100);

// Consumer 1 leaves - triggers rebalance
Console.WriteLine("  Consumer-1 leaves group (rebalance triggered):");
rebalanceTracker.OnPartitionsRevoked("consumer-1", new[] { 0 });
rebalanceTracker.OnPartitionsAssigned("consumer-2", new[] { 0 });

Console.WriteLine();
rebalanceTracker.PrintAssignments();
Console.WriteLine();

// ============== Demo 3: Timeout Scenarios ==============
Console.WriteLine("--- Demo 3: Timeout Scenarios ---");

var timeoutSimulator = new TimeoutSimulator();

// Scenario 1: Session timeout
Console.WriteLine("  Scenario 1: Session Timeout");
Console.WriteLine("    - Consumer stops sending heartbeats");
Console.WriteLine("    - Broker waits for session.timeout.ms (default 45s)");
Console.WriteLine("    - Consumer is removed from group, rebalance triggered");
await timeoutSimulator.SimulateSessionTimeout();

// Scenario 2: Max poll interval exceeded
Console.WriteLine("  Scenario 2: Max Poll Interval Exceeded");
Console.WriteLine("    - Consumer takes too long processing between polls");
Console.WriteLine("    - Exceeds max.poll.interval.ms (default 5min)");
Console.WriteLine("    - Consumer leaves group, partitions revoked");
await timeoutSimulator.SimulateMaxPollInterval();

// Scenario 3: Request timeout
Console.WriteLine("  Scenario 3: Request Timeout");
Console.WriteLine("    - Network request takes too long");
Console.WriteLine("    - Exceeds request.timeout.ms (default 30s)");
Console.WriteLine("    - Operation fails, client retries or throws");
await timeoutSimulator.SimulateRequestTimeout();

Console.WriteLine();

// ============== Demo 4: Debugging Checklist ==============
Console.WriteLine("--- Demo 4: Debugging Checklist ---");

var debugger = new IssueDebugger();

// Check for common issues
Console.WriteLine("  Running diagnostic checks...");
Console.WriteLine();

debugger.CheckConsumerLag(lagMonitor);
debugger.CheckRebalanceFrequency(rebalanceTracker);
debugger.CheckTimeoutSettings();
debugger.CheckProcessingTime(50, 100);

Console.WriteLine();

// ============== Demo 5: Solutions ==============
Console.WriteLine("--- Demo 5: Solutions ---");

Console.WriteLine();
Console.WriteLine("  Issue: High Consumer Lag");
Console.WriteLine("    Solution 1: Add more consumers to the group");
Console.WriteLine("    Solution 2: Optimize message processing");
Console.WriteLine("    Solution 3: Increase consumer throughput (batch processing)");
Console.WriteLine();

Console.WriteLine("  Issue: Frequent Rebalancing");
Console.WriteLine("    Solution 1: Increase session.timeout.ms");
Console.WriteLine("    Solution 2: Reduce heartbeat.interval.ms");
Console.WriteLine("    Solution 3: Ensure stable consumer connections");
Console.WriteLine();

Console.WriteLine("  Issue: Timeout Errors");
Console.WriteLine("    Solution 1: Match timeouts to workload (slow processing = higher timeout)");
Console.WriteLine("    Solution 2: Process in smaller batches");
Console.WriteLine("    Solution 3: Use async processing with offset tracking");
Console.WriteLine();

// Summary
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Monitor consumer lag to detect processing bottlenecks");
Console.WriteLine("2. Handle rebalancing gracefully - commit offsets, cleanup state");
Console.WriteLine("3. Configure timeouts based on your processing requirements");
Console.WriteLine("4. Use batching to improve throughput while staying within timeouts");
Console.WriteLine("5. Log and alert on lag/rebalance/timeout events");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Helper Classes ==============

public class LagMonitor
{
    private long _latestOffset = -1;
    private long _committedOffset = -1;
    private readonly List<(DateTime Time, long Lag)> _history = new();

    public long CurrentLag => Math.Max(0, _latestOffset - _committedOffset);

    public void RecordProduce(long offset)
    {
        Interlocked.Exchange(ref _latestOffset, offset);
        RecordHistory();
    }

    public void RecordCommit(long offset)
    {
        Interlocked.Exchange(ref _committedOffset, offset);
        RecordHistory();
    }

    private void RecordHistory()
    {
        lock (_history)
        {
            _history.Add((DateTime.UtcNow, CurrentLag));
            // Keep only last 100 entries
            if (_history.Count > 100)
            {
                _history.RemoveAt(0);
            }
        }
    }

    public double AverageLag
    {
        get
        {
            lock (_history)
            {
                return _history.Count > 0 ? _history.Average(h => h.Lag) : 0;
            }
        }
    }

    public long MaxLag
    {
        get
        {
            lock (_history)
            {
                return _history.Count > 0 ? _history.Max(h => h.Lag) : 0;
            }
        }
    }
}

public class RebalanceTracker
{
    private readonly Dictionary<string, List<int>> _assignments = new();
    private readonly List<(DateTime Time, string Event, string Consumer)> _events = new();

    public void OnPartitionsAssigned(string consumerId, int[] partitions)
    {
        _assignments[consumerId] = partitions.ToList();
        _events.Add((DateTime.UtcNow, "Assigned", consumerId));
        Console.WriteLine($"    {consumerId} assigned partitions: [{string.Join(", ", partitions)}]");
    }

    public void OnPartitionsRevoked(string consumerId, int[] partitions)
    {
        if (_assignments.TryGetValue(consumerId, out var current))
        {
            foreach (var p in partitions)
            {
                current.Remove(p);
            }
            if (current.Count == 0)
            {
                _assignments.Remove(consumerId);
            }
        }
        _events.Add((DateTime.UtcNow, "Revoked", consumerId));
        Console.WriteLine($"    {consumerId} revoked partitions: [{string.Join(", ", partitions)}]");
    }

    public int RebalanceCount => _events.Count(e => e.Event == "Revoked");

    public void PrintAssignments()
    {
        Console.WriteLine("  Current partition assignments:");
        foreach (var (consumer, partitions) in _assignments)
        {
            Console.WriteLine($"    {consumer}: [{string.Join(", ", partitions)}]");
        }
    }
}

public class TimeoutSimulator
{
    public async Task SimulateSessionTimeout()
    {
        Console.WriteLine("    [Simulated] Consumer heartbeat stopped");
        await Task.Delay(50);
        Console.WriteLine("    [Simulated] Session timeout detected after 45s");
        Console.WriteLine("    [Simulated] Consumer removed from group");
        Console.WriteLine();
    }

    public async Task SimulateMaxPollInterval()
    {
        Console.WriteLine("    [Simulated] Long-running message processing started");
        await Task.Delay(50);
        Console.WriteLine("    [Simulated] max.poll.interval.ms exceeded");
        Console.WriteLine("    [Simulated] Consumer left group automatically");
        Console.WriteLine();
    }

    public async Task SimulateRequestTimeout()
    {
        Console.WriteLine("    [Simulated] Network request sent to broker");
        await Task.Delay(50);
        Console.WriteLine("    [Simulated] request.timeout.ms exceeded");
        Console.WriteLine("    [Simulated] TimeoutException thrown");
        Console.WriteLine();
    }
}

public class IssueDebugger
{
    public void CheckConsumerLag(LagMonitor lagMonitor)
    {
        var lag = lagMonitor.CurrentLag;
        var status = lag switch
        {
            0 => "OK - Fully caught up",
            < 10 => "OK - Low lag",
            < 100 => "WARNING - Moderate lag",
            _ => "CRITICAL - High lag"
        };
        Console.WriteLine($"  [LAG] Current: {lag}, Max: {lagMonitor.MaxLag}, Avg: {lagMonitor.AverageLag:F1} - {status}");
    }

    public void CheckRebalanceFrequency(RebalanceTracker tracker)
    {
        var count = tracker.RebalanceCount;
        var status = count switch
        {
            0 => "OK - Stable",
            < 3 => "OK - Expected rebalances",
            < 10 => "WARNING - Frequent rebalances",
            _ => "CRITICAL - Too many rebalances"
        };
        Console.WriteLine($"  [REBALANCE] Count: {count} - {status}");
    }

    public void CheckTimeoutSettings()
    {
        Console.WriteLine("  [TIMEOUTS] Recommended settings:");
        Console.WriteLine("    session.timeout.ms: 45000 (45s)");
        Console.WriteLine("    heartbeat.interval.ms: 3000 (3s) - should be 1/3 of session timeout");
        Console.WriteLine("    max.poll.interval.ms: 300000 (5min) - adjust based on processing time");
    }

    public void CheckProcessingTime(int avgProcessingMs, int maxProcessingMs)
    {
        var recommendedPollInterval = maxProcessingMs * 10;
        var status = avgProcessingMs < 100 ? "OK" : "WARNING - Consider async processing";
        Console.WriteLine($"  [PROCESSING] Avg: {avgProcessingMs}ms, Max: {maxProcessingMs}ms - {status}");
        Console.WriteLine($"    Recommended max.poll.interval.ms: {recommendedPollInterval}ms");
    }
}
