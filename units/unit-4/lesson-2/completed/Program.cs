// Surgewave Bootcamp - Unit 6, Lesson 2: Partitioning Strategies
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Native;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 6.2 - Partitioning Strategies ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        await using var client = new SurgewaveNativeClient("localhost", runtime.Port);
        await client.ConnectAsync();

        await using var producer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = $"localhost:{runtime.Port}");

        // Demo 1: Good distribution with entity ID
        Console.WriteLine("--- Demo 1: Entity ID Key (Good Distribution) ---");

        await client.Topics.Create("users-good")
            .WithPartitions(8)
            .WithReplicationFactor(1)
            .ExecuteAsync();

        // Produce with unique user IDs
        for (int i = 0; i < 1000; i++)
        {
            await producer.ProduceAsync("users-good", $"user-{i}", $"data-{i}");
        }
        await producer.FlushAsync();

        await AnalyzeDistribution(client, "users-good");
        Console.WriteLine();

        // Demo 2: Hot partition with skewed key
        Console.WriteLine("--- Demo 2: Country Code Key (Hot Partition) ---");

        await client.Topics.Create("users-skewed")
            .WithPartitions(8)
            .WithReplicationFactor(1)
            .ExecuteAsync();

        // Simulate real-world distribution: 80% US, 10% UK, 5% DE, 5% other
        var countries = new[] { "US", "UK", "DE", "FR", "JP", "AU", "CA", "BR" };
        var weights = new[] { 80, 10, 5, 1, 1, 1, 1, 1 }; // Heavily skewed to US

        for (int i = 0; i < 1000; i++)
        {
            var country = GetWeightedRandom(countries, weights);
            await producer.ProduceAsync("users-skewed", country, $"data-{i}");
        }
        await producer.FlushAsync();

        await AnalyzeDistribution(client, "users-skewed");
        Console.WriteLine();

        // Demo 3: Null key (round-robin, perfect distribution)
        Console.WriteLine("--- Demo 3: Null Key (Round-Robin) ---");

        await client.Topics.Create("logs-roundrobin")
            .WithPartitions(8)
            .WithReplicationFactor(1)
            .ExecuteAsync();

        for (int i = 0; i < 1000; i++)
        {
            // Null key = round-robin distribution
            await producer.ProduceAsync("logs-roundrobin", null!, $"log-{i}");
        }
        await producer.FlushAsync();

        await AnalyzeDistribution(client, "logs-roundrobin");
        Console.WriteLine();

        // Partition count recommendations
        Console.WriteLine("--- Partition Count Guidelines ---");
        PrintPartitionRecommendations();

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }

    private static async Task AnalyzeDistribution(SurgewaveNativeClient client, string topic)
    {
        var description = await client.Topics.DescribeAsync(topic);
        var partitionCounts = description.Partitions
            .OrderBy(p => p.PartitionId)
            .Select(p => p.HighWatermark - p.LogStartOffset)
            .ToList();

        var total = partitionCounts.Sum();
        var avg = total / (double)partitionCounts.Count;

        Console.WriteLine($"Topic: {topic} ({partitionCounts.Count} partitions, {total} messages)");

        for (int i = 0; i < partitionCounts.Count; i++)
        {
            var count = partitionCounts[i];
            var pct = total > 0 ? (double)count / total * 100 : 0;
            var bar = new string('#', Math.Max(1, (int)(pct / 3)));

            Console.WriteLine($"  P{i}: {bar,-30} {pct,5:F1}% ({count})");
        }

        // Calculate skew
        var maxCount = partitionCounts.Max();
        var minCount = partitionCounts.Min();
        var skewRatio = minCount > 0 ? (double)maxCount / minCount : maxCount;

        if (skewRatio > 2)
        {
            Console.WriteLine($"  WARNING: HIGH SKEW: {skewRatio:F1}x difference between max/min");
        }
        else
        {
            Console.WriteLine($"  OK: Good distribution (skew ratio: {skewRatio:F1}x)");
        }
    }

    private static string GetWeightedRandom(string[] items, int[] weights)
    {
        var totalWeight = weights.Sum();
        var random = Random.Shared.Next(totalWeight);
        var cumulative = 0;

        for (int i = 0; i < items.Length; i++)
        {
            cumulative += weights[i];
            if (random < cumulative)
                return items[i];
        }

        return items[^1];
    }

    private static void PrintPartitionRecommendations()
    {
        Console.WriteLine(@"
Partition Count Formula:
  partitions = max(consumerCount, throughput / 10000) * 2

Examples:
  | Scenario                    | Consumers | Throughput | Recommended |
  |-----------------------------|-----------|------------|-------------|
  | Small app                   | 2         | 1K/sec     | 6           |
  | Medium service              | 6         | 50K/sec    | 12          |
  | High-throughput pipeline    | 20        | 500K/sec   | 100         |

Key Selection Guide:
  | Requirement          | Key Strategy      |
  |----------------------|-------------------|
  | Order per customer   | customer_id       |
  | Order per session    | session_id        |
  | Order per tenant     | tenant_id         |
  | No ordering needed   | null (round-robin)|
  | Global ordering      | constant key      |
");
    }
}
