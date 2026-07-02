// Unit 4 Project: Cluster Monitor Dashboard
// Build a real-time monitoring dashboard for Surgewave brokers

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Cluster Monitor ===\n");

// =============================================================================
// DATA MODELS
// =============================================================================

public record BrokerMetrics(
    int BrokerId,
    string Host,
    int Port,
    bool IsHealthy,
    long MessagesPerSecond,
    int ActiveConnections,
    TimeSpan Latency,
    DateTime CollectedAt
);

public record TopicMetrics(
    string TopicName,
    int PartitionCount,
    long TotalMessages,
    DateTime CollectedAt
);

public record ConsumerGroupMetrics(
    string GroupId,
    string Topic,
    long CurrentOffset,
    long HighWatermark,
    long Lag,
    DateTime CollectedAt
);

public record ClusterHealth(
    int TotalBrokers,
    int HealthyBrokers,
    int TotalTopics,
    long TotalMessages,
    long TotalLag,
    double AverageLatencyMs,
    List<string> Alerts
);

// =============================================================================
// CLUSTER MONITOR
// =============================================================================

public class ClusterMonitor
{
    private readonly ConcurrentDictionary<int, BrokerMetrics> _brokerMetrics = new();
    private readonly ConcurrentDictionary<string, TopicMetrics> _topicMetrics = new();
    private readonly ConcurrentDictionary<string, ConsumerGroupMetrics> _consumerMetrics = new();
    private readonly ConcurrentDictionary<int, (string Host, int Port)> _brokerInfo = new();

    // Configuration
    private readonly int _lagWarningThreshold;
    private readonly int _lagCriticalThreshold;

    public ClusterMonitor(int lagWarningThreshold = 100, int lagCriticalThreshold = 1000)
    {
        _lagWarningThreshold = lagWarningThreshold;
        _lagCriticalThreshold = lagCriticalThreshold;
    }

    public void AddBroker(int brokerId, string host, int port)
    {
        // TODO: Store broker info
        _brokerInfo[brokerId] = (host, port);

        // Initialize broker metrics
        _brokerMetrics[brokerId] = new BrokerMetrics(
            brokerId, host, port, false, 0, 0, TimeSpan.Zero, DateTime.UtcNow);
    }

    public void AddTopic(string topic, int partitions)
    {
        // TODO: Store topic info
        _topicMetrics[topic] = new TopicMetrics(topic, partitions, 0, DateTime.UtcNow);
    }

    public void AddConsumerGroup(string groupId, string topic)
    {
        // TODO: Store consumer group info for lag monitoring
        _consumerMetrics[groupId] = new ConsumerGroupMetrics(
            groupId, topic, 0, 0, 0, DateTime.UtcNow);
    }

    public async Task CollectMetricsAsync()
    {
        // TODO: Step 1 - Collect broker health by checking connectivity
        await CollectBrokerMetricsAsync();

        // TODO: Step 2 - Update topic metrics
        await CollectTopicMetricsAsync();

        // TODO: Step 3 - Calculate consumer lag
        await CollectConsumerMetricsAsync();
    }

    private async Task CollectBrokerMetricsAsync()
    {
        // TODO: For each broker, check if it's reachable by creating a producer
        // and measuring latency
        foreach (var (brokerId, (host, port)) in _brokerInfo)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                // TODO: Try to create a producer and send a health check message
                // Hint: Use SurgewaveProducer to check connectivity
                // await using var healthProducer = new SurgewaveProducer<string, string>(...);
                // await healthProducer.ProduceAsync("__health_check", "ping", "pong");

                await Task.Delay(10); // Placeholder
                sw.Stop();

                // TODO: Update broker metrics with health status
                _brokerMetrics[brokerId] = _brokerMetrics[brokerId] with
                {
                    IsHealthy = true,
                    Latency = sw.Elapsed,
                    CollectedAt = DateTime.UtcNow
                };
            }
            catch (Exception)
            {
                sw.Stop();
                _brokerMetrics[brokerId] = _brokerMetrics[brokerId] with
                {
                    IsHealthy = false,
                    Latency = sw.Elapsed,
                    CollectedAt = DateTime.UtcNow
                };
            }
        }
    }

    private async Task CollectTopicMetricsAsync()
    {
        // TODO: Update topic metrics (message counts, etc.)
        await Task.CompletedTask;
    }

    private async Task CollectConsumerMetricsAsync()
    {
        // TODO: For each consumer group, calculate lag using GetLagAsync
        // Hint: Use SurgewaveConsumer.GetLagAsync to get lag per partition
        await Task.CompletedTask;
    }

    public ClusterHealth GetClusterHealth()
    {
        var alerts = new List<string>();

        var totalBrokers = _brokerMetrics.Count;
        var healthyBrokers = _brokerMetrics.Values.Count(b => b.IsHealthy);
        var totalTopics = _topicMetrics.Count;
        var totalMessages = _topicMetrics.Values.Sum(t => t.TotalMessages);
        var avgLatency = _brokerMetrics.Values.Where(b => b.IsHealthy)
            .Select(b => b.Latency.TotalMilliseconds)
            .DefaultIfEmpty(0)
            .Average();

        var totalLag = _consumerMetrics.Values.Sum(m => m.Lag);

        // TODO: Generate alerts for offline brokers
        foreach (var broker in _brokerMetrics.Values.Where(b => !b.IsHealthy))
        {
            alerts.Add($"[CRITICAL] Broker {broker.BrokerId} ({broker.Host}:{broker.Port}) is OFFLINE");
        }

        // TODO: Generate alerts for high consumer lag
        foreach (var (groupId, metrics) in _consumerMetrics)
        {
            if (metrics.Lag > _lagCriticalThreshold)
            {
                alerts.Add($"[CRITICAL] Consumer group '{groupId}' lag is critical ({metrics.Lag:N0})");
            }
            else if (metrics.Lag > _lagWarningThreshold)
            {
                alerts.Add($"[WARNING] Consumer group '{groupId}' lag exceeds threshold ({metrics.Lag:N0})");
            }
        }

        return new ClusterHealth(totalBrokers, healthyBrokers, totalTopics,
            totalMessages, totalLag, avgLatency, alerts);
    }

    public void PrintDashboard(int reportNumber)
    {
        var health = GetClusterHealth();
        var status = health.HealthyBrokers == health.TotalBrokers ? "HEALTHY" : "DEGRADED";
        var statusColor = status == "HEALTHY" ? ConsoleColor.Green : ConsoleColor.Yellow;

        Console.WriteLine(new string('=', 50));
        Console.WriteLine($"  Cluster Health Report #{reportNumber}");
        Console.WriteLine($"  Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine(new string('=', 50));

        Console.Write("  Status: ");
        Console.ForegroundColor = statusColor;
        Console.WriteLine(status);
        Console.ResetColor();

        Console.WriteLine($"  Brokers: {health.HealthyBrokers}/{health.TotalBrokers} online");
        Console.WriteLine($"  Topics: {health.TotalTopics}");
        Console.WriteLine($"  Total Messages: {health.TotalMessages:N0}");
        Console.WriteLine($"  Total Consumer Lag: {health.TotalLag:N0}");
        Console.WriteLine($"  Average Latency: {health.AverageLatencyMs:F1}ms");
        Console.WriteLine();

        // Print broker status
        Console.WriteLine("  BROKER STATUS:");
        foreach (var broker in _brokerMetrics.Values.OrderBy(b => b.BrokerId))
        {
            var brokerStatus = broker.IsHealthy ? "ONLINE" : "OFFLINE";
            var brokerColor = broker.IsHealthy ? ConsoleColor.Green : ConsoleColor.Red;

            Console.Write($"    [{broker.BrokerId}] {broker.Host}:{broker.Port} - ");
            Console.ForegroundColor = brokerColor;
            Console.WriteLine(brokerStatus);
            Console.ResetColor();
        }
        Console.WriteLine();

        // Print consumer groups
        Console.WriteLine("  CONSUMER LAG:");
        foreach (var (groupId, metrics) in _consumerMetrics)
        {
            var lagStatus = metrics.Lag > _lagCriticalThreshold ? "CRITICAL"
                          : metrics.Lag > _lagWarningThreshold ? "WARNING"
                          : "OK";
            Console.WriteLine($"    {groupId}: {metrics.Lag:N0} messages behind ({lagStatus})");
        }
        Console.WriteLine();

        // Print alerts
        Console.WriteLine($"  ALERTS ({health.Alerts.Count}):");
        if (health.Alerts.Any())
        {
            foreach (var alert in health.Alerts)
            {
                var color = alert.Contains("CRITICAL") ? ConsoleColor.Red : ConsoleColor.Yellow;
                Console.ForegroundColor = color;
                Console.WriteLine($"    {alert}");
                Console.ResetColor();
            }
        }
        else
        {
            Console.WriteLine("    (none)");
        }
        Console.WriteLine(new string('=', 50));
    }
}

// =============================================================================
// MAIN PROGRAM
// =============================================================================

// Start broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"[Setup] Broker started on port {runtime.Port}");

await using var producer = new SurgewaveProducer<string, string>(opt =>
    opt.BootstrapServers = $"localhost:{runtime.Port}");

Console.WriteLine("[Setup] Producing sample messages...");
for (int i = 0; i < 500; i++)
{
    var topic = i % 3 == 0 ? "orders" : i % 3 == 1 ? "payments" : "notifications";
    await producer.ProduceAsync(topic, $"key-{i}", $"message-{i}");
}
await producer.FlushAsync();
Console.WriteLine("[Setup] 500 messages produced");

// Create consumer groups
Console.WriteLine("[Setup] Creating consumer groups...");
{
    await using var consumer = new SurgewaveConsumer<string, string>(opt =>
    {
        opt.BootstrapServers = $"localhost:{runtime.Port}";
        opt.GroupId = "order-processor";
        opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        opt.EnableAutoCommit = false;
    });
    consumer.Subscribe("orders");
    for (int i = 0; i < 50; i++)
    {
        await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
    }
    await consumer.CommitAsync();
}

{
    await using var consumer = new SurgewaveConsumer<string, string>(opt =>
    {
        opt.BootstrapServers = $"localhost:{runtime.Port}";
        opt.GroupId = "payment-service";
        opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        opt.EnableAutoCommit = false;
    });
    consumer.Subscribe("payments");
    for (int i = 0; i < 20; i++)
    {
        await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
    }
    await consumer.CommitAsync();
}
Console.WriteLine("[Setup] Consumer groups configured\n");

// Create monitor
var monitor = new ClusterMonitor(lagWarningThreshold: 50, lagCriticalThreshold: 200);
monitor.AddBroker(1, "localhost", runtime.Port);
monitor.AddTopic("orders", 1);
monitor.AddTopic("payments", 1);
monitor.AddTopic("notifications", 1);
monitor.AddConsumerGroup("order-processor", "orders");
monitor.AddConsumerGroup("payment-service", "payments");

// TODO: Run monitoring loop
Console.WriteLine("[Monitor] Starting metric collection...\n");

for (int report = 1; report <= 3; report++)
{
    await monitor.CollectMetricsAsync();
    monitor.PrintDashboard(report);

    if (report < 3)
    {
        Console.WriteLine("\n[Monitor] Next collection in 2 seconds...\n");
        await Task.Delay(TimeSpan.FromSeconds(2));
    }
}

Console.WriteLine("\n=== Cluster Monitor Complete ===");
