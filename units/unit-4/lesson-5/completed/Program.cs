// Surgewave Bootcamp - Unit 6, Lesson 5: Capacity Planning
// COMPLETED SOLUTION

using System.Diagnostics;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;

// ============== Capacity Calculator ==============
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

// ============== Capacity Monitor ==============
public class CapacityMonitor
{
    public record ResourceMetrics(
        long DiskUsedBytes,
        long DiskTotalBytes,
        double DiskUsagePercent,
        long MemoryUsedBytes
    );

    public static ResourceMetrics GetCurrentMetrics(string dataPath)
    {
        var driveInfo = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(dataPath)) ?? "C:");

        var process = Process.GetCurrentProcess();

        return new ResourceMetrics(
            driveInfo.TotalSize - driveInfo.AvailableFreeSpace,
            driveInfo.TotalSize,
            100.0 * (1 - (double)driveInfo.AvailableFreeSpace / driveInfo.TotalSize),
            process.WorkingSet64
        );
    }

    public static void PrintCapacityWarnings(ResourceMetrics metrics)
    {
        Console.WriteLine($"Disk Usage: {metrics.DiskUsagePercent:F1}%");
        Console.WriteLine($"Memory Usage: {FormatBytes(metrics.MemoryUsedBytes)}");

        if (metrics.DiskUsagePercent > 80)
            Console.WriteLine("WARNING: Disk usage above 80%");

        if (metrics.DiskUsagePercent > 90)
            Console.WriteLine("CRITICAL: Disk usage above 90% - expand storage!");
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB", "PB"];
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:F2} {sizes[order]}";
    }
}

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 6.5 - Capacity Planning ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        // ============== Capacity Report ==============
        Console.WriteLine("--- Capacity Planning Report ---");
        Console.WriteLine();

        // Scenario: E-commerce order events
        var input = new CapacityCalculator.CapacityInput(
            MessageSizeBytes: 1024,           // 1 KB average message
            MessagesPerSecond: 10_000,        // 10K events/sec
            RetentionPeriod: TimeSpan.FromDays(7),  // 7 day retention
            ReplicationFactor: 3,             // Triple replication
            PartitionCount: 24                // 24 partitions
        );

        GenerateCapacityReport(input);
        Console.WriteLine();

        // ============== Different Scenarios ==============
        Console.WriteLine("--- Scenario Comparison ---");
        Console.WriteLine();

        var scenarios = new[]
        {
            ("Small Startup", new CapacityCalculator.CapacityInput(512, 100, TimeSpan.FromDays(3), 2, 6)),
            ("Growing Company", new CapacityCalculator.CapacityInput(1024, 5_000, TimeSpan.FromDays(7), 3, 12)),
            ("Enterprise", new CapacityCalculator.CapacityInput(2048, 100_000, TimeSpan.FromDays(30), 3, 96)),
        };

        Console.WriteLine($"{"Scenario",-20} {"Storage",-15} {"Daily Ingest",-15} {"Throughput",-12}");
        Console.WriteLine(new string('-', 62));

        foreach (var (name, scenarioInput) in scenarios)
        {
            var output = CapacityCalculator.Calculate(scenarioInput);
            Console.WriteLine($"{name,-20} {FormatBytes(output.TotalStorageBytes),-15} {FormatBytes(output.DailyIngestionBytes),-15} {output.ThroughputMBPerSecond:F2} MB/s");
        }
        Console.WriteLine();

        // ============== Current Resource Usage ==============
        Console.WriteLine("--- Current Resource Usage ---");
        Console.WriteLine();

        var metrics = CapacityMonitor.GetCurrentMetrics(".");
        CapacityMonitor.PrintCapacityWarnings(metrics);
        Console.WriteLine();

        // ============== Planning Guidelines ==============
        Console.WriteLine("--- Capacity Planning Guidelines ---");
        Console.WriteLine(@"
+------------------------------------------------------------------+
|                    SIZING RECOMMENDATIONS                         |
+------------------------------------------------------------------+
| Throughput (msg/sec) | Partitions | Brokers | Disk per Broker    |
+----------------------+------------+---------+--------------------+
| < 10,000             | 3-6        | 1       | 100 GB             |
| 10,000 - 100,000     | 12-24      | 3       | 500 GB             |
| 100,000 - 1,000,000  | 48-96      | 5-7     | 2 TB               |
| > 1,000,000          | 100+       | 10+     | 4+ TB              |
+------------------------------------------------------------------+

Growth Planning Formula:
  Year N Capacity = Current * (1 + GrowthRate)^N * 1.5 (safety margin)

Monitoring Thresholds:
  - Disk Usage:     Warning at 70%, Critical at 85%
  - Consumer Lag:   Warning at 10K, Critical at 100K messages
  - Latency P99:    Warning at 100ms, Critical at 500ms
");

        // ============== Capacity Planning Checklist ==============
        Console.WriteLine("--- Pre-Production Checklist ---");
        Console.WriteLine(@"
[ ] Data Volume Analysis
    [ ] Average message size measured
    [ ] Peak throughput estimated
    [ ] Growth rate projected

[ ] Storage Planning
    [ ] Retention policy defined per topic
    [ ] Storage calculated with 2x headroom
    [ ] Disk I/O capacity verified

[ ] Availability Planning
    [ ] Replication factor set (3 recommended)
    [ ] Min ISR configured
    [ ] Failure scenarios tested

[ ] Performance Planning
    [ ] Latency targets defined
    [ ] Partition count optimized
    [ ] Consumer parallelism planned
");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }

    private static void GenerateCapacityReport(CapacityCalculator.CapacityInput input)
    {
        var output = CapacityCalculator.Calculate(input);

        Console.WriteLine("+==============================================================+");
        Console.WriteLine("|              CAPACITY PLANNING REPORT                        |");
        Console.WriteLine("+==============================================================+");
        Console.WriteLine($"|  Message Size:        {input.MessageSizeBytes,10:N0} bytes                    |");
        Console.WriteLine($"|  Messages/Second:     {input.MessagesPerSecond,10:N0}                          |");
        Console.WriteLine($"|  Retention:           {input.RetentionPeriod.TotalDays,10:N1} days                     |");
        Console.WriteLine($"|  Replication Factor:  {input.ReplicationFactor,10}                          |");
        Console.WriteLine($"|  Partitions:          {input.PartitionCount,10}                          |");
        Console.WriteLine("+--------------------------------------------------------------+");
        Console.WriteLine($"|  Total Storage:       {FormatBytes(output.TotalStorageBytes),15}                    |");
        Console.WriteLine($"|  Daily Ingestion:     {FormatBytes(output.DailyIngestionBytes),15}                    |");
        Console.WriteLine($"|  Storage/Partition:   {FormatBytes(output.StoragePerPartitionBytes),15}                    |");
        Console.WriteLine($"|  Throughput:          {output.ThroughputMBPerSecond,10:F2} MB/s                   |");
        Console.WriteLine("+==============================================================+");
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB", "PB"];
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:F2} {sizes[order]}";
    }
}
