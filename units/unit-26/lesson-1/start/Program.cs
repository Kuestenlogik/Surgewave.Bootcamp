// Surgewave Bootcamp - Unit 24, Lesson 1: Tiered Storage Concepts
//
// In this lesson, you'll learn:
// - The hot/warm/cold storage tier model
// - When and why data moves between tiers
// - Cost-performance tradeoffs for each tier
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using System.Text;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 24.1 - Tiered Storage Concepts ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var topic = "tiered-demo";

// TODO: Step 1 - Define storage tier configurations
// Create records to represent each tier with:
// - Name (Hot, Warm, Cold)
// - Retention period
// - Cost per GB per month
// - Expected latency
//
// public record TierConfig(string Name, TimeSpan Retention, decimal CostPerGbMonth, TimeSpan ExpectedLatency);
//
// var hotTier = new TierConfig("Hot", TimeSpan.FromHours(1), 0.50m, TimeSpan.FromMilliseconds(1));
// var warmTier = new TierConfig("Warm", TimeSpan.FromDays(7), 0.10m, TimeSpan.FromMilliseconds(10));
// var coldTier = new TierConfig("Cold", TimeSpan.FromDays(90), 0.01m, TimeSpan.FromSeconds(1));
//
// Console.WriteLine("\n--- Tier Configuration ---");
// Console.WriteLine($"Hot Tier:  {hotTier.Retention.TotalHours}h retention, ${hotTier.CostPerGbMonth}/GB/month");
// Console.WriteLine($"Warm Tier: {warmTier.Retention.TotalDays}d retention, ${warmTier.CostPerGbMonth}/GB/month");
// Console.WriteLine($"Cold Tier: {coldTier.Retention.TotalDays}d retention, ${coldTier.CostPerGbMonth}/GB/month");

// TODO: Step 2 - Create a StoredMessage record that tracks message age
// public record StoredMessage(string Id, string Topic, string Value, DateTime ProducedAt, long SizeBytes)
// {
//     public TimeSpan Age => DateTime.UtcNow - ProducedAt;
//     public string CurrentTier => Age switch
//     {
//         var a when a < TimeSpan.FromHours(1) => "Hot",
//         var a when a < TimeSpan.FromDays(7) => "Warm",
//         _ => "Cold"
//     };
// }

// TODO: Step 3 - Create a TierDistribution class to track bytes per tier
// public class TierDistribution
// {
//     public long HotBytes { get; set; }
//     public long WarmBytes { get; set; }
//     public long ColdBytes { get; set; }
//     public long TotalBytes => HotBytes + WarmBytes + ColdBytes;
//
//     public void AddMessage(StoredMessage message)
//     {
//         switch (message.CurrentTier)
//         {
//             case "Hot": HotBytes += message.SizeBytes; break;
//             case "Warm": WarmBytes += message.SizeBytes; break;
//             case "Cold": ColdBytes += message.SizeBytes; break;
//         }
//     }
// }

// TODO: Step 4 - Create a producer and generate messages with simulated ages
// await using var producer = new SurgewaveProducer<string, string>(opt =>
// {
//     opt.BootstrapServers = $"localhost:{runtime.Port}";
// });
//
// var messages = new List<StoredMessage>();
// Console.WriteLine("\n--- Producing Messages ---");
//
// for (int i = 0; i < 100; i++)
// {
//     var value = $"Order-{i:D4}: Sample data for tiered storage demo";
//     var key = $"order-{i:D4}";
//
//     await producer.ProduceAsync(topic, key, value);
//
//     // Simulate different ages (0h, 2h, 4h, ... up to 200h)
//     var age = TimeSpan.FromHours(i * 2);
//     var producedAt = DateTime.UtcNow - age;
//
//     messages.Add(new StoredMessage(
//         Id: Guid.NewGuid().ToString(),
//         Topic: topic,
//         Value: value,
//         ProducedAt: producedAt,
//         SizeBytes: Encoding.UTF8.GetByteCount(value)
//     ));
// }
//
// Console.WriteLine($"Produced {messages.Count} messages with simulated ages (0h to 200h)");

// TODO: Step 5 - Calculate tier distribution
// var distribution = new TierDistribution();
// foreach (var message in messages)
// {
//     distribution.AddMessage(message);
// }
//
// Console.WriteLine("\n--- Tier Distribution ---");
// Console.WriteLine($"Hot Tier:  {distribution.HotBytes:N0} bytes ({distribution.HotBytes * 100.0 / distribution.TotalBytes:F1}%)");
// Console.WriteLine($"Warm Tier: {distribution.WarmBytes:N0} bytes ({distribution.WarmBytes * 100.0 / distribution.TotalBytes:F1}%)");
// Console.WriteLine($"Cold Tier: {distribution.ColdBytes:N0} bytes ({distribution.ColdBytes * 100.0 / distribution.TotalBytes:F1}%)");
// Console.WriteLine($"Total:     {distribution.TotalBytes:N0} bytes");

// TODO: Step 6 - Calculate cost savings
// var totalGb = distribution.TotalBytes / (1024m * 1024m * 1024m);
// var hotGb = distribution.HotBytes / (1024m * 1024m * 1024m);
// var warmGb = distribution.WarmBytes / (1024m * 1024m * 1024m);
// var coldGb = distribution.ColdBytes / (1024m * 1024m * 1024m);
//
// var tieredCost = (hotGb * hotTier.CostPerGbMonth) + (warmGb * warmTier.CostPerGbMonth) + (coldGb * coldTier.CostPerGbMonth);
// var flatCost = totalGb * hotTier.CostPerGbMonth;
// var savings = flatCost - tieredCost;
// var savingsPercent = flatCost > 0 ? (savings / flatCost) * 100 : 0;
//
// Console.WriteLine("\n--- Cost Analysis ---");
// Console.WriteLine($"Without tiering: ${flatCost:F7}/month (all at hot tier price)");
// Console.WriteLine($"With tiering:    ${tieredCost:F7}/month");
// Console.WriteLine($"Monthly savings: ${savings:F7} ({savingsPercent:F1}%)");

// TODO: Step 7 - Simulate tier access latency
// async Task<TimeSpan> SimulateTierAccess(string tier)
// {
//     var sw = Stopwatch.StartNew();
//     var delay = tier switch
//     {
//         "Hot" => TimeSpan.FromMilliseconds(1),
//         "Warm" => TimeSpan.FromMilliseconds(10),
//         "Cold" => TimeSpan.FromMilliseconds(100),
//         _ => TimeSpan.Zero
//     };
//     await Task.Delay(delay);
//     sw.Stop();
//     return sw.Elapsed;
// }
//
// Console.WriteLine("\n--- Access Latency by Tier ---");
// foreach (var tier in new[] { "Hot", "Warm", "Cold" })
// {
//     var latency = await SimulateTierAccess(tier);
//     Console.WriteLine($"{tier} tier access: {latency.TotalMilliseconds:F1}ms");
// }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Hot tier is fastest but most expensive");
Console.WriteLine("2. Cold tier is cheapest but has higher latency");
Console.WriteLine("3. Proper tier configuration can save 50-90% on storage costs");
Console.WriteLine("4. Access patterns should guide tier retention settings");
Console.WriteLine("5. Monitor tier distribution to optimize configuration");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
