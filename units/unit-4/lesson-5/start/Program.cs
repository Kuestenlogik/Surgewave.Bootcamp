// Surgewave Bootcamp - Unit 6, Lesson 5: Capacity Planning
//
// In this lesson, you'll learn to:
// - Calculate storage requirements
// - Estimate throughput capacity
// - Plan cluster sizing
// - Monitor capacity metrics
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;

// TODO: Implement CapacityCalculator
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
        // TODO: Calculate total storage
        // TODO: Calculate daily ingestion
        // TODO: Calculate storage per partition
        // TODO: Calculate throughput

        return new CapacityOutput(0, 0, 0, 0);
    }
}

// TODO: Implement CapacityMonitor
public class CapacityMonitor
{
    public record ResourceMetrics(
        long DiskUsedBytes,
        long DiskTotalBytes,
        double DiskUsagePercent,
        long MemoryUsedBytes
    );

    // TODO: Implement GetCurrentMetrics
    // TODO: Implement PrintCapacityWarnings
}

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 6.5 - Capacity Planning ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        // TODO: Generate capacity report

        // TODO: Monitor current resources

        // TODO: Create capacity planning checklist

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
