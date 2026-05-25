# Lesson 6.5: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Implement Capacity Calculator

```csharp
public class CapacityCalculator
{
    public record CapacityInput(
        long MessageSizeBytes,
        long MessagesPerSecond,
        TimeSpan RetentionPeriod,
        int ReplicationFactor,
        int PartitionCount
    );

    public record CapacityOutput(
        long TotalStorageBytes,
        long DailyIngestionBytes,
        long StoragePerPartitionBytes,
        double ThroughputMBPerSecond
    );

    public static CapacityOutput Calculate(CapacityInput input)
    {
        var retentionSeconds = (long)input.RetentionPeriod.TotalSeconds;

        // Total messages stored = rate × retention
        var totalMessages = input.MessagesPerSecond * retentionSeconds;

        // Raw storage = messages × size × replication
        var totalStorage = totalMessages * input.MessageSizeBytes * input.ReplicationFactor;

        // Daily ingestion
        var dailyIngestion = input.MessagesPerSecond * 86400 * input.MessageSizeBytes * input.ReplicationFactor;

        // Per partition
        var storagePerPartition = totalStorage / input.PartitionCount;

        // Throughput in MB/s
        var throughputMB = (input.MessagesPerSecond * input.MessageSizeBytes) / (1024.0 * 1024.0);

        return new CapacityOutput(
            totalStorage,
            dailyIngestion,
            storagePerPartition,
            throughputMB
        );
    }
}
```

## Step 2: Create Workload Simulator

```csharp
public class WorkloadSimulator
{
    private readonly IProducer<string, byte[]> _producer;
    private readonly string _topic;

    public record SimulationResult(
        long MessagesProduced,
        TimeSpan Duration,
        double ActualThroughputPerSecond,
        double P50LatencyMs,
        double P99LatencyMs
    );

    public async Task<SimulationResult> RunSimulation(
        int targetMessagesPerSecond,
        int messageSizeBytes,
        TimeSpan duration)
    {
        var message = new byte[messageSizeBytes];
        var latencies = new List<double>();
        var produced = 0L;
        var stopwatch = Stopwatch.StartNew();
        var targetInterval = TimeSpan.FromSeconds(1.0 / targetMessagesPerSecond);

        while (stopwatch.Elapsed < duration)
        {
            var sendStart = Stopwatch.GetTimestamp();

            await _producer.ProduceAsync(_topic, Guid.NewGuid().ToString(), message);

            var latency = Stopwatch.GetElapsedTime(sendStart).TotalMilliseconds;
            latencies.Add(latency);
            produced++;

            // Throttle to target rate
            var elapsed = Stopwatch.GetElapsedTime(sendStart);
            if (elapsed < targetInterval)
                await Task.Delay(targetInterval - elapsed);
        }

        stopwatch.Stop();
        latencies.Sort();

        return new SimulationResult(
            produced,
            stopwatch.Elapsed,
            produced / stopwatch.Elapsed.TotalSeconds,
            Percentile(latencies, 0.50),
            Percentile(latencies, 0.99)
        );
    }

    private static double Percentile(List<double> sorted, double p)
    {
        var index = (int)(sorted.Count * p);
        return sorted[Math.Min(index, sorted.Count - 1)];
    }
}
```

## Step 3: Monitor Resource Usage

```csharp
public class CapacityMonitor
{
    public record ResourceMetrics(
        long DiskUsedBytes,
        long DiskTotalBytes,
        double DiskUsagePercent,
        long MemoryUsedBytes,
        double CpuUsagePercent,
        long MessagesPerSecond,
        long BytesPerSecond
    );

    public static ResourceMetrics GetCurrentMetrics(string dataPath)
    {
        var driveInfo = new DriveInfo(Path.GetPathRoot(dataPath)!);

        var process = Process.GetCurrentProcess();

        return new ResourceMetrics(
            driveInfo.TotalSize - driveInfo.AvailableFreeSpace,
            driveInfo.TotalSize,
            100.0 * (1 - (double)driveInfo.AvailableFreeSpace / driveInfo.TotalSize),
            process.WorkingSet64,
            0, // Would need performance counters for real CPU
            0, // Would need metrics tracking
            0  // Would need metrics tracking
        );
    }

    public static void PrintCapacityWarnings(ResourceMetrics metrics)
    {
        if (metrics.DiskUsagePercent > 80)
            Console.WriteLine($"⚠️  WARNING: Disk usage at {metrics.DiskUsagePercent:F1}%");

        if (metrics.DiskUsagePercent > 90)
            Console.WriteLine($"🔴 CRITICAL: Disk usage at {metrics.DiskUsagePercent:F1}% - expand storage!");

        if (metrics.MemoryUsedBytes > 1_000_000_000)
            Console.WriteLine($"ℹ️  Memory usage: {metrics.MemoryUsedBytes / 1_000_000_000.0:F2} GB");
    }
}
```

## Step 4: Capacity Planning Report

```csharp
public static void GenerateCapacityReport(CapacityCalculator.CapacityInput input)
{
    var output = CapacityCalculator.Calculate(input);

    Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║              CAPACITY PLANNING REPORT                       ║");
    Console.WriteLine("╠════════════════════════════════════════════════════════════╣");
    Console.WriteLine($"║  Message Size:        {input.MessageSizeBytes,10:N0} bytes              ║");
    Console.WriteLine($"║  Messages/Second:     {input.MessagesPerSecond,10:N0}                    ║");
    Console.WriteLine($"║  Retention:           {input.RetentionPeriod.TotalDays,10:N1} days               ║");
    Console.WriteLine($"║  Replication Factor:  {input.ReplicationFactor,10}                    ║");
    Console.WriteLine($"║  Partitions:          {input.PartitionCount,10}                    ║");
    Console.WriteLine("╠════════════════════════════════════════════════════════════╣");
    Console.WriteLine($"║  Total Storage:       {FormatBytes(output.TotalStorageBytes),15}              ║");
    Console.WriteLine($"║  Daily Ingestion:     {FormatBytes(output.DailyIngestionBytes),15}              ║");
    Console.WriteLine($"║  Storage/Partition:   {FormatBytes(output.StoragePerPartitionBytes),15}              ║");
    Console.WriteLine($"║  Throughput:          {output.ThroughputMBPerSecond,10:F2} MB/s             ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
}

private static string FormatBytes(long bytes)
{
    string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB" };
    int order = 0;
    double size = bytes;
    while (size >= 1024 && order < sizes.Length - 1)
    {
        order++;
        size /= 1024;
    }
    return $"{size:F2} {sizes[order]}";
}
```

## Capacity Planning Best Practices

### Sizing Guidelines

| Component | Recommendation |
|-----------|----------------|
| **Disk** | 2x calculated storage (headroom) |
| **Memory** | 4-8 GB per broker minimum |
| **CPU** | 4+ cores for compression |
| **Network** | 1+ Gbps per broker |

### Growth Planning

```
Year 1 Capacity = Current × 1.5 (safety margin)
Year 2 Capacity = Year 1 × Growth Rate
Year 3 Capacity = Year 2 × Growth Rate

Example (50% YoY growth):
  Year 1: 10 TB × 1.5 = 15 TB
  Year 2: 15 TB × 1.5 = 22.5 TB
  Year 3: 22.5 TB × 1.5 = 33.75 TB
```

### Monitoring Thresholds

| Metric | Warning | Critical |
|--------|---------|----------|
| Disk Usage | 70% | 85% |
| Consumer Lag | 10,000 msgs | 100,000 msgs |
| Under-replicated | Any | Any |
| Request Latency P99 | 100ms | 500ms |

## Complete Solution

See `completed/Program.cs` for the full implementation.

---

**Previous:** [Lesson 6.4: Event-Driven Patterns](../lesson-4/README.md)
