// Surgewave Bootcamp - Unit 3, Lesson 6: Compacted Topics
// COMPLETED SOLUTION

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

        // Create native client for topic management and cluster operations
        await using var nativeClient = new SurgewaveNativeClient("localhost", runtime.Port);
        await nativeClient.ConnectAsync();

        // Create compacted topic using fluent builder
        await nativeClient.Topics.Create("user-profiles")
            .WithPartitions(1)
            .WithCompaction()
            .ExecuteAsync();
        Console.WriteLine("Created compacted topic: user-profiles");
        Console.WriteLine();

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

        // Produce multiple updates per key (simulating state changes)
        Console.WriteLine("Producing user profile updates...");

        // First round - initial state
        foreach (var userId in users)
        {
            var profile = $"{{\"name\": \"{userId}\", \"status\": \"active\"}}";
            await producer.ProduceAsync("user-profiles", userId, profile);
            Console.WriteLine($"  Produced: {userId} -> active");
        }

        // Second round - status upgrade
        foreach (var userId in users)
        {
            var profile = $"{{\"name\": \"{userId}\", \"status\": \"premium\"}}";
            await producer.ProduceAsync("user-profiles", userId, profile);
            Console.WriteLine($"  Produced: {userId} -> premium");
        }

        // Third round - another status change
        foreach (var userId in users)
        {
            var profile = $"{{\"name\": \"{userId}\", \"status\": \"vip\"}}";
            await producer.ProduceAsync("user-profiles", userId, profile);
            Console.WriteLine($"  Produced: {userId} -> vip");
        }

        await producer.FlushAsync();
        Console.WriteLine("Produced 9 messages total (3 updates per user)");
        Console.WriteLine();

        // Trigger compaction
        Console.WriteLine("Triggering compaction...");
        var compactionResult = await nativeClient.Cluster.TriggerLogCompactionAsync();
        Console.WriteLine($"Compaction completed: {compactionResult.RecordsRemoved} records removed, {compactionResult.SegmentsCompacted} segments compacted");
        await Task.Delay(500); // Allow compaction to complete
        Console.WriteLine();

        // Read the compacted log
        Console.WriteLine("--- Reading Compacted Log ---");
        consumer.Subscribe("user-profiles");

        var latestValues = new Dictionary<string, string>();
        int messageCount = 0;

        while (true)
        {
            var message = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
            if (message == null) break;

            messageCount++;
            latestValues[message.Key!] = message.Value ?? "(tombstone)";
            Console.WriteLine($"  Offset {message.Offset}: {message.Key} = {message.Value}");
        }

        Console.WriteLine();
        Console.WriteLine($"Messages read: {messageCount}");
        Console.WriteLine($"Unique keys: {latestValues.Count}");
        Console.WriteLine();

        // Display final state
        Console.WriteLine("Final state after compaction:");
        foreach (var (key, value) in latestValues)
        {
            Console.WriteLine($"  {key}: {value}");
        }
        Console.WriteLine();

        // Demonstrate tombstones
        Console.WriteLine("--- Demonstrating Tombstones ---");
        Console.WriteLine("Sending tombstone (null value) for user-2...");
        await producer.ProduceAsync("user-profiles", "user-2", null!);
        await producer.FlushAsync();

        // Trigger compaction again
        Console.WriteLine("Triggering compaction again...");
        await nativeClient.Cluster.TriggerLogCompactionAsync();

        // Wait for tombstone retention to expire
        await Task.Delay(1500);
        Console.WriteLine();

        // Verify user-2 is removed
        Console.WriteLine("--- Verifying Deletion ---");
        await using var verifyConsumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        verifyConsumer.Subscribe("user-profiles");

        var remainingUsers = new Dictionary<string, string>();
        while (true)
        {
            var msg = await verifyConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
            if (msg == null) break;

            // Only track non-tombstone messages
            if (msg.Value != null)
            {
                remainingUsers[msg.Key!] = msg.Value;
            }
            else
            {
                // Tombstone - remove from tracking if present
                remainingUsers.Remove(msg.Key!);
            }
        }

        Console.WriteLine("Remaining users after tombstone compaction:");
        foreach (var (key, value) in remainingUsers)
        {
            Console.WriteLine($"  {key}: {value}");
        }

        var user2Exists = remainingUsers.ContainsKey("user-2");
        Console.WriteLine();
        Console.WriteLine($"user-2 exists: {user2Exists}");

        if (!user2Exists && remainingUsers.Count == 2)
        {
            Console.WriteLine();
            Console.WriteLine("SUCCESS: Tombstone deletion verified!");
        }

        Console.WriteLine();
        Console.WriteLine("--- Summary ---");
        Console.WriteLine("Log compaction keeps only the latest value per key.");
        Console.WriteLine("Tombstones (null values) delete keys after retention period.");
        Console.WriteLine("This is ideal for changelog topics and state stores.");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
