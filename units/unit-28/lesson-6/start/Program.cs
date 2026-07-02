// Surgewave Bootcamp - Unit 3, Lesson 6: Compacted Topics
//
// In this lesson, you'll learn to:
// - Create compacted topics for key-based retention
// - Produce multiple updates per key
// - Use tombstones (null values) for deletion
// - Observe compaction behavior
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Client.Native;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 3.6 - Compacted Topics ===");
        Console.WriteLine();

        // Start embedded broker
        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine($"Broker started on port {runtime.Port}");

        // Create native client for topic management
        await using var nativeClient = new SurgewaveNativeClient("localhost", runtime.Port);
        await nativeClient.ConnectAsync();

        // TODO: Create compacted topic using fluent builder
        // await nativeClient.Topics.Create("user-profiles")
        //     .WithPartitions(1)
        //     .WithCompaction()
        //     .ExecuteAsync();

        // Create producer
        await using var producer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
        });

        // Create consumer
        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            opt.GroupId = "compaction-demo";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        var users = new[] { "user-1", "user-2", "user-3" };

        // TODO: Produce multiple updates per key
        // - First round: status = "active"
        // - Second round: status = "premium"
        // - Third round: status = "vip"
        Console.WriteLine("Producing user profile updates...");

        // TODO: Trigger compaction using native client
        // var compactionResult = await nativeClient.Cluster.TriggerLogCompactionAsync();

        // TODO: Read the compacted log and verify only latest values remain
        // consumer.Subscribe("user-profiles");

        Console.WriteLine();
        Console.WriteLine("--- Before Tombstone ---");

        // TODO: Send tombstone to delete user-2
        // await producer.ProduceAsync("user-profiles", "user-2", null!);

        // TODO: Trigger compaction again and verify user-2 is removed

        Console.WriteLine();
        Console.WriteLine("--- After Tombstone ---");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
