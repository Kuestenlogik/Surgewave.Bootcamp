// Unit 4 Project: Cluster Monitor Dashboard - COMPLETED
// A real-time monitoring dashboard for Surgewave brokers

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Cluster Monitor ===\n");

// =============================================================================
// MAIN PROGRAM
// =============================================================================

// Start broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"[Setup] Broker started on port {runtime.Port}");

await using var producer = new SurgewaveProducer<string, string>(opt =>
    opt.BootstrapServers = $"localhost:{runtime.Port}");

// Produce sample messages
Console.WriteLine("[Setup] Producing 1000 sample messages...");
for (int i = 0; i < 1000; i++)
{
    var topic = i % 3 == 0 ? "orders" : i % 3 == 1 ? "payments" : "notifications";
    await producer.ProduceAsync(topic, $"key-{i % 100}", $"{{\"id\":{i},\"data\":\"message-{i}\"}}");
}
await producer.FlushAsync();
Console.WriteLine("[Setup] Sample data produced");

// Create consumer groups with different lag levels
Console.WriteLine("[Setup] Creating consumer groups...");

// Order processor - moderate consumption
{
    await using var consumer = new SurgewaveConsumer<string, string>(opt =>
    {
        opt.BootstrapServers = $"localhost:{runtime.Port}";
        opt.GroupId = "order-processor";
        opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        opt.EnableAutoCommit = false;
    });
    consumer.Subscribe("orders");
    for (int i = 0; i < 250; i++)
    {
        await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(10));
    }
    await consumer.CommitAsync();
    Console.WriteLine("[Setup] order-processor: consumed 250 messages");
}

// Payment service - low consumption (high lag)
{
    await using var consumer = new SurgewaveConsumer<string, string>(opt =>
    {
        opt.BootstrapServers = $"localhost:{runtime.Port}";
        opt.GroupId = "payment-service";
        opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        opt.EnableAutoCommit = false;
    });
    consumer.Subscribe("payments");
    for (int i = 0; i < 50; i++)
    {
        await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(10));
    }
    await consumer.CommitAsync();
    Console.WriteLine("[Setup] payment-service: consumed 50 messages");
}

// Notification handler - high consumption (low lag)
{
    await using var consumer = new SurgewaveConsumer<string, string>(opt =>
    {
        opt.BootstrapServers = $"localhost:{runtime.Port}";
        opt.GroupId = "notification-handler";
        opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        opt.EnableAutoCommit = false;
    });
    consumer.Subscribe("notifications");
    for (int i = 0; i < 300; i++)
    {
        await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(10));
    }
    await consumer.CommitAsync();
    Console.WriteLine("[Setup] notification-handler: consumed 300 messages");
}

Console.WriteLine("[Setup] Consumer groups configured\n");

// Create and configure monitor
await using var monitor = new ClusterMonitor(
    lagWarningThreshold: 50,
    lagCriticalThreshold: 200,
    latencyWarningMs: 100);

monitor.AddBroker(1, "localhost", runtime.Port);
monitor.AddTopic("orders", 1, 334);
monitor.AddTopic("payments", 1, 333);
monitor.AddTopic("notifications", 1, 333);
monitor.AddConsumerGroup("order-processor", "orders");
monitor.AddConsumerGroup("payment-service", "payments");
monitor.AddConsumerGroup("notification-handler", "notifications");

Console.WriteLine("[Monitor] Starting metric collection...\n");

// Run monitoring loop
for (int report = 1; report <= 3; report++)
{
    await monitor.CollectMetricsAsync();
    monitor.PrintDashboard(report);

    // On first report, show Prometheus export format
    if (report == 1)
    {
        Console.WriteLine();
        Console.WriteLine("  PROMETHEUS METRICS (export format):");
        Console.WriteLine("  " + new string('-', 50));
        var prometheusMetrics = monitor.ExportPrometheusMetrics();
        foreach (var line in prometheusMetrics.Split('\n').Take(12))
        {
            Console.WriteLine($"  {line}");
        }
        Console.WriteLine("  ...");
        Console.WriteLine();
    }

    if (report < 3)
    {
        Console.WriteLine($"\n[Monitor] Next collection in 2 seconds...\n");
        await Task.Delay(TimeSpan.FromSeconds(2));
    }
}

Console.WriteLine();
Console.WriteLine("=== Cluster Monitor Complete ===");

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

public class ClusterMonitor : IAsyncDisposable
{
    private readonly ConcurrentDictionary<int, BrokerMetrics> _brokerMetrics = new();
    private readonly ConcurrentDictionary<string, TopicMetrics> _topicMetrics = new();
    private readonly ConcurrentDictionary<string, ConsumerGroupMetrics> _consumerMetrics = new();
    private readonly ConcurrentDictionary<int, (string Host, int Port)> _brokerInfo = new();
    private readonly ConcurrentDictionary<string, string> _groupTopics = new();

    // Configuration
    private readonly int _lagWarningThreshold;
    private readonly int _lagCriticalThreshold;
    private readonly int _latencyWarningMs;

    public ClusterMonitor(int lagWarningThreshold = 100, int lagCriticalThreshold = 1000, int latencyWarningMs = 100)
    {
        _lagWarningThreshold = lagWarningThreshold;
        _lagCriticalThreshold = lagCriticalThreshold;
        _latencyWarningMs = latencyWarningMs;
    }

    public void AddBroker(int brokerId, string host, int port)
    {
        _brokerInfo[brokerId] = (host, port);
        _brokerMetrics[brokerId] = new BrokerMetrics(
            brokerId, host, port, false, 0, 0, TimeSpan.Zero, DateTime.UtcNow);
    }

    public void AddTopic(string topic, int partitions, long messageCount = 0)
    {
        _topicMetrics[topic] = new TopicMetrics(topic, partitions, messageCount, DateTime.UtcNow);
    }

    public void AddConsumerGroup(string groupId, string topic)
    {
        _groupTopics[groupId] = topic;
        _consumerMetrics[groupId] = new ConsumerGroupMetrics(
            groupId, topic, 0, 0, 0, DateTime.UtcNow);
    }

    public async Task CollectMetricsAsync()
    {
        await CollectBrokerMetricsAsync();
        await CollectConsumerMetricsAsync();
    }

    private async Task CollectBrokerMetricsAsync()
    {
        foreach (var (brokerId, (host, port)) in _brokerInfo)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                // Check broker health by trying to create a producer and send a message
                await using var healthProducer = new SurgewaveProducer<string, string>(opt =>
                    opt.BootstrapServers = $"{host}:{port}");

                await healthProducer.ProduceAsync("__health_check", "ping", DateTime.UtcNow.ToString("O"));
                await healthProducer.FlushAsync();
                sw.Stop();

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

    private async Task CollectConsumerMetricsAsync()
    {
        // For each consumer group, create a consumer and measure position/lag
        foreach (var (groupId, topic) in _groupTopics)
        {
            try
            {
                var (host, port) = _brokerInfo.Values.FirstOrDefault();
                if (host == null) continue;

                // Create a consumer with the same group to check position
                await using var consumer = new SurgewaveConsumer<string, string>(opt =>
                {
                    opt.BootstrapServers = $"{host}:{port}";
                    opt.GroupId = groupId;
                    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
                    opt.EnableAutoCommit = false;
                });

                consumer.Subscribe(topic);

                // Try to get current position by consuming without commit
                long messagesConsumed = 0;
                var startTime = DateTime.UtcNow;
                while ((DateTime.UtcNow - startTime).TotalSeconds < 1)
                {
                    var msg = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
                    if (msg == null) break;
                    messagesConsumed++;
                }

                // Estimate lag based on unconsumed messages
                var existingMetrics = _consumerMetrics[groupId];
                _consumerMetrics[groupId] = existingMetrics with
                {
                    Lag = messagesConsumed,  // Messages remaining unconsumed
                    CollectedAt = DateTime.UtcNow
                };
            }
            catch
            {
                // Unable to measure lag for this group
            }
        }
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

        // Generate alerts for offline brokers
        foreach (var broker in _brokerMetrics.Values.Where(b => !b.IsHealthy))
        {
            alerts.Add($"[CRITICAL] Broker {broker.BrokerId} ({broker.Host}:{broker.Port}) is OFFLINE");
        }

        // Generate alerts for high latency
        foreach (var broker in _brokerMetrics.Values.Where(b => b.IsHealthy && b.Latency.TotalMilliseconds > _latencyWarningMs))
        {
            alerts.Add($"[WARNING] Broker {broker.BrokerId} latency is high ({broker.Latency.TotalMilliseconds:F0}ms)");
        }

        // Generate alerts for consumer lag
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

        if (healthyBrokers == 0 && totalBrokers > 0)
        {
            alerts.Insert(0, "[CRITICAL] No healthy brokers available!");
        }

        return new ClusterHealth(totalBrokers, healthyBrokers, totalTopics,
            totalMessages, totalLag, avgLatency, alerts);
    }

    public void PrintDashboard(int reportNumber)
    {
        var health = GetClusterHealth();
        var status = health.HealthyBrokers == 0 ? "OFFLINE"
                   : health.HealthyBrokers < health.TotalBrokers ? "DEGRADED"
                   : "HEALTHY";

        var statusColor = status == "HEALTHY" ? ConsoleColor.Green
                        : status == "DEGRADED" ? ConsoleColor.Yellow
                        : ConsoleColor.Red;

        var headerLine = new string('=', 60);
        Console.WriteLine(headerLine);
        Console.WriteLine($"  Surgewave CLUSTER MONITOR - Report #{reportNumber}");
        Console.WriteLine($"  Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine(headerLine);

        Console.Write("  Cluster Status: ");
        Console.ForegroundColor = statusColor;
        Console.WriteLine(status);
        Console.ResetColor();

        Console.WriteLine($"  Brokers: {health.HealthyBrokers}/{health.TotalBrokers} online");
        Console.WriteLine($"  Topics: {health.TotalTopics}");
        Console.WriteLine($"  Total Messages: {health.TotalMessages:N0}");
        Console.WriteLine($"  Total Consumer Lag: {health.TotalLag:N0}");
        Console.WriteLine($"  Average Latency: {health.AverageLatencyMs:F1}ms");
        Console.WriteLine(new string('-', 60));

        // Broker Health
        Console.WriteLine("  BROKER HEALTH");
        Console.WriteLine("  " + new string('-', 50));
        foreach (var broker in _brokerMetrics.Values.OrderBy(b => b.BrokerId))
        {
            var brokerStatus = broker.IsHealthy ? "ONLINE" : "OFFLINE";
            var brokerColor = broker.IsHealthy ? ConsoleColor.Green : ConsoleColor.Red;

            Console.Write($"    [{broker.BrokerId}] {broker.Host}:{broker.Port} - ");
            Console.ForegroundColor = brokerColor;
            Console.Write(brokerStatus);
            Console.ResetColor();
            Console.WriteLine($" ({broker.Latency.TotalMilliseconds:F0}ms)");
        }
        Console.WriteLine();

        // Topic Statistics
        Console.WriteLine("  TOPIC STATISTICS");
        Console.WriteLine("  " + new string('-', 50));
        foreach (var topic in _topicMetrics.Values.OrderBy(t => t.TopicName))
        {
            Console.WriteLine($"    {topic.TopicName}: {topic.PartitionCount} partitions, {topic.TotalMessages:N0} messages");
        }
        Console.WriteLine();

        // Consumer Lag
        Console.WriteLine("  CONSUMER LAG");
        Console.WriteLine("  " + new string('-', 50));
        foreach (var (groupId, metrics) in _consumerMetrics.OrderBy(g => g.Key))
        {
            var lagStatus = metrics.Lag > _lagCriticalThreshold ? "CRITICAL"
                          : metrics.Lag > _lagWarningThreshold ? "WARNING"
                          : "OK";
            var lagColor = lagStatus == "OK" ? ConsoleColor.Green
                         : lagStatus == "WARNING" ? ConsoleColor.Yellow
                         : ConsoleColor.Red;

            Console.Write($"    {groupId} ({metrics.Topic}): {metrics.Lag:N0} behind - ");
            Console.ForegroundColor = lagColor;
            Console.WriteLine(lagStatus);
            Console.ResetColor();
        }
        Console.WriteLine();

        // Alerts
        Console.WriteLine($"  ALERTS ({health.Alerts.Count} active)");
        Console.WriteLine("  " + new string('-', 50));
        if (health.Alerts.Any())
        {
            foreach (var alert in health.Alerts)
            {
                var alertColor = alert.Contains("CRITICAL") ? ConsoleColor.Red : ConsoleColor.Yellow;
                Console.ForegroundColor = alertColor;
                Console.WriteLine($"    {alert}");
                Console.ResetColor();
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("    (No active alerts - system healthy)");
            Console.ResetColor();
        }
        Console.WriteLine(headerLine);
    }

    public string ExportPrometheusMetrics()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# HELP surgewave_broker_healthy Broker health status (1=healthy, 0=unhealthy)");
        sb.AppendLine("# TYPE surgewave_broker_healthy gauge");
        foreach (var broker in _brokerMetrics.Values)
        {
            sb.AppendLine($"surgewave_broker_healthy{{broker_id=\"{broker.BrokerId}\",host=\"{broker.Host}\"}} {(broker.IsHealthy ? 1 : 0)}");
        }

        sb.AppendLine();
        sb.AppendLine("# HELP surgewave_broker_latency_ms Broker response latency in milliseconds");
        sb.AppendLine("# TYPE surgewave_broker_latency_ms gauge");
        foreach (var broker in _brokerMetrics.Values)
        {
            sb.AppendLine($"surgewave_broker_latency_ms{{broker_id=\"{broker.BrokerId}\"}} {broker.Latency.TotalMilliseconds:F2}");
        }

        sb.AppendLine();
        sb.AppendLine("# HELP surgewave_consumer_lag Consumer group lag");
        sb.AppendLine("# TYPE surgewave_consumer_lag gauge");
        foreach (var (groupId, metrics) in _consumerMetrics)
        {
            sb.AppendLine($"surgewave_consumer_lag{{group=\"{groupId}\"}} {metrics.Lag}");
        }

        return sb.ToString();
    }

    public async ValueTask DisposeAsync()
    {
        // Cleanup if needed
        await Task.CompletedTask;
    }
}
