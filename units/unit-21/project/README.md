# Unit 4 Project: Cluster Monitor Dashboard

Build a real-time multi-broker health monitoring dashboard with metrics collection.

## Scenario

You're the operations engineer responsible for monitoring a Surgewave cluster in production. Your task is to build a monitoring dashboard that:
1. Collects health metrics from multiple brokers
2. Tracks topic statistics and consumer lag
3. Displays real-time cluster status
4. Alerts on potential issues (high lag, broker offline)
5. Generates periodic health reports

## Requirements

### Functional Requirements

- [ ] Monitor multiple broker instances (simulated cluster)
- [ ] Collect broker metrics (message rate, bytes in/out, connections)
- [ ] Track topic statistics (partitions, replicas, message count)
- [ ] Calculate and display consumer lag per consumer group
- [ ] Detect offline/unhealthy brokers
- [ ] Generate health status summary
- [ ] Alert on critical conditions (lag > threshold, broker down)

### Technical Requirements

- [ ] Use Admin API for metadata queries
- [ ] Implement periodic metric collection (configurable interval)
- [ ] Use async/await for non-blocking operations
- [ ] Thread-safe metric storage
- [ ] Clean console output with formatted tables

## Cluster Metrics

```csharp
public record BrokerMetrics(
    int BrokerId,
    string Host,
    int Port,
    bool IsHealthy,
    long MessagesPerSecond,
    long BytesInPerSecond,
    long BytesOutPerSecond,
    int ActiveConnections,
    DateTime CollectedAt
);

public record TopicMetrics(
    string TopicName,
    int PartitionCount,
    int ReplicationFactor,
    long TotalMessages,
    long BytesOnDisk,
    DateTime CollectedAt
);

public record ConsumerGroupMetrics(
    string GroupId,
    string Topic,
    int Partition,
    long CurrentOffset,
    long HighWatermark,
    long Lag,
    DateTime CollectedAt
);

public record ClusterHealth(
    int TotalBrokers,
    int HealthyBrokers,
    int TotalTopics,
    int TotalPartitions,
    long TotalLag,
    List<string> Alerts
);
```

## Dashboard Layout

```
╔══════════════════════════════════════════════════════════════╗
║                 Surgewave CLUSTER MONITOR                        ║
║                 Last Update: 2024-01-15 10:30:45            ║
╠══════════════════════════════════════════════════════════════╣
║  CLUSTER STATUS: HEALTHY                                     ║
║  Brokers: 3/3 online | Topics: 5 | Partitions: 15           ║
╠══════════════════════════════════════════════════════════════╣
║  BROKER HEALTH                                               ║
║  ┌────────┬───────────────┬────────┬─────────┬────────────┐ ║
║  │ ID     │ Host:Port     │ Status │ Msg/sec │ Connections│ ║
║  ├────────┼───────────────┼────────┼─────────┼────────────┤ ║
║  │ 1      │ localhost:9092│ ONLINE │ 1,234   │ 15         │ ║
║  │ 2      │ localhost:9093│ ONLINE │ 987     │ 12         │ ║
║  │ 3      │ localhost:9094│ ONLINE │ 1,102   │ 18         │ ║
║  └────────┴───────────────┴────────┴─────────┴────────────┘ ║
╠══════════════════════════════════════════════════════════════╣
║  TOPIC STATISTICS                                            ║
║  ┌─────────────────┬────────────┬──────────┬───────────────┐ ║
║  │ Topic           │ Partitions │ Replicas │ Messages      │ ║
║  ├─────────────────┼────────────┼──────────┼───────────────┤ ║
║  │ orders          │ 3          │ 2        │ 15,432        │ ║
║  │ payments        │ 6          │ 2        │ 8,901         │ ║
║  │ notifications   │ 3          │ 2        │ 45,678        │ ║
║  └─────────────────┴────────────┴──────────┴───────────────┘ ║
╠══════════════════════════════════════════════════════════════╣
║  CONSUMER LAG                                                ║
║  ┌───────────────────┬─────────────┬──────────┬────────────┐ ║
║  │ Consumer Group    │ Topic       │ Lag      │ Status     │ ║
║  ├───────────────────┼─────────────┼──────────┼────────────┤ ║
║  │ order-processor   │ orders      │ 45       │ OK         │ ║
║  │ payment-service   │ payments    │ 1,234    │ WARNING    │ ║
║  │ notification-svc  │ notifications│ 12      │ OK         │ ║
║  └───────────────────┴─────────────┴──────────┴────────────┘ ║
╠══════════════════════════════════════════════════════════════╣
║  ALERTS (0 active)                                           ║
║  (none)                                                      ║
╚══════════════════════════════════════════════════════════════╝
```

## Expected Output

```
=== Surgewave Cluster Monitor ===

[Setup] Starting 3 broker instances...
[Setup] Broker 1 started on port 9092
[Setup] Broker 2 started on port 9093
[Setup] Broker 3 started on port 9094

[Setup] Creating sample topics and data...
[Setup] Topic 'orders' created (3 partitions)
[Setup] Topic 'payments' created (6 partitions)
[Setup] Producing 1000 sample messages...

[Monitor] Starting metric collection (interval: 5s)

--- Cluster Health Report #1 ---
Cluster Status: HEALTHY
  Brokers: 3/3 online
  Topics: 2
  Total Partitions: 9
  Total Consumer Lag: 850

Broker Status:
  [1] localhost:9092 - ONLINE - 234 msg/s - 8 connections
  [2] localhost:9093 - ONLINE - 189 msg/s - 6 connections
  [3] localhost:9094 - ONLINE - 201 msg/s - 7 connections

Consumer Groups:
  order-processor (orders): lag=450, status=WARNING
  payment-service (payments): lag=400, status=WARNING

Alerts:
  [WARNING] Consumer group 'order-processor' lag exceeds threshold (450 > 100)
  [WARNING] Consumer group 'payment-service' lag exceeds threshold (400 > 100)

--- Cluster Health Report #2 ---
...
```

## Getting Started

```bash
cd bootcamp/unit-4/project/start
dotnet run
```

## Hints

1. Use `SurgewaveAdminClient` for cluster metadata
2. `GetClusterMetadataAsync()` returns broker and topic info
3. `GetConsumerGroupOffsetsAsync(groupId)` returns committed offsets
4. `GetHighWatermarkAsync(topic, partition)` returns latest offset
5. Calculate lag: `highWatermark - committedOffset`
6. Use `Console.Clear()` for updating dashboard (or print incrementally)

## Thresholds

| Metric | Warning | Critical |
|--------|---------|----------|
| Consumer Lag | > 100 | > 1000 |
| Broker Latency | > 100ms | > 500ms |
| Connection Count | > 100 | > 500 |

## Evaluation Criteria

| Criteria | Points |
|----------|--------|
| Multi-broker monitoring | 20 |
| Topic statistics correct | 20 |
| Consumer lag calculation | 25 |
| Alert detection | 15 |
| Dashboard formatting | 10 |
| Code organization | 10 |

## Bonus Challenges

- [ ] Export metrics to Prometheus format
- [ ] Add historical trending (last N samples)
- [ ] Implement graceful degradation when a broker is unreachable
- [ ] Add partition leader distribution analysis

## Solution

Compare your implementation with [completed/](completed/README.md) when done.
