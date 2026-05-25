// Surgewave Bootcamp - Unit 3, Lesson 7: Ephemeral Topics
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Client.Native;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 3.7 - Ephemeral Topics ===");
        Console.WriteLine();

        // Step 1: Start embedded broker
        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(false)
            .Build()
            .StartAsync();

        Console.WriteLine($"Broker started on port {runtime.Port}");

        // Create native client for topic management
        await using var nativeClient = new SurgewaveNativeClient("localhost", runtime.Port);
        await nativeClient.ConnectAsync();

        // Step 2: Create ephemeral topic with 1 MB ring buffer
        await nativeClient.Topics.Create("sensor-telemetry")
            .WithPartitions(1)
            .WithConfig("cleanup.policy", "ephemeral")
            .WithConfig("ephemeral.buffer.bytes", "1048576")
            .ExecuteAsync();
        Console.WriteLine("Created ephemeral topic: sensor-telemetry (1 MB buffer)");

        // Step 3: Create persistent topic for comparison
        await nativeClient.Topics.Create("sensor-telemetry-persistent")
            .WithPartitions(1)
            .ExecuteAsync();
        Console.WriteLine("Created persistent topic: sensor-telemetry-persistent");
        Console.WriteLine();

        // Step 4: Start consumer for ephemeral topic (background task)
        Console.WriteLine("--- Step 4: Real-Time Consumption ---");
        var receivedMessages = new List<string>();
        var cts = new CancellationTokenSource();

        var consumerTask = Task.Run(async () =>
        {
            await using var consumer = new SurgewaveConsumer<string, string>(opt =>
            {
                opt.BootstrapServers = $"localhost:{runtime.Port}";
                opt.GroupId = "ephemeral-demo";
                opt.AutoOffsetReset = AutoOffsetReset.Latest;
            });

            consumer.Subscribe("sensor-telemetry");

            while (!cts.Token.IsCancellationRequested)
            {
                var msg = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(200));
                if (msg != null)
                {
                    receivedMessages.Add(msg.Value!);
                    Console.WriteLine($"  [Consumer] Received: {msg.Value}");
                }
            }
        });

        await Task.Delay(500); // Allow consumer to connect
        Console.WriteLine("Consumer started and listening...");

        // Step 5: Produce messages - consumer receives live
        Console.WriteLine();
        Console.WriteLine("--- Step 5: Producing Sensor Readings ---");

        await using var producer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
        });

        for (int i = 1; i <= 5; i++)
        {
            var reading = $"{{\"sensor\": \"temp-1\", \"value\": {20.0 + i}, \"ts\": \"{DateTime.UtcNow:O}\"}}";
            await producer.ProduceAsync("sensor-telemetry", "temp-1", reading);
            await Task.Delay(100);
        }
        await producer.FlushAsync();
        await Task.Delay(500); // Allow consumer to process

        Console.WriteLine();

        // Step 6: Demonstrate messages lost without active consumer
        Console.WriteLine("--- Step 6: Messages Without Active Consumer ---");
        cts.Cancel();
        await consumerTask;
        int receivedBeforeStop = receivedMessages.Count;
        Console.WriteLine($"Consumer received {receivedBeforeStop} messages while active");
        Console.WriteLine();

        Console.WriteLine("Producing 5 messages with NO active consumer...");
        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync("sensor-telemetry", "temp-1", $"lost-message-{i}");
        }
        await producer.FlushAsync();

        // Start new consumer with Latest - won't see old messages
        await using var lateConsumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            opt.AutoOffsetReset = AutoOffsetReset.Latest;
        });

        lateConsumer.Subscribe("sensor-telemetry");
        int lateCount = 0;
        while (true)
        {
            var msg = await lateConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(300));
            if (msg == null) break;
            lateCount++;
        }
        Console.WriteLine($"Late consumer (Latest offset) received: {lateCount} messages");
        Console.WriteLine("Messages produced without an active consumer are effectively lost with Latest offset!");
        Console.WriteLine();

        // Step 7: Demonstrate buffer wrap-around
        Console.WriteLine("--- Step 7: Buffer Wrap-Around ---");
        Console.WriteLine("Flooding ephemeral topic to trigger ring-buffer wrap-around...");

        // Use pseudo-random data to prevent compression from defeating the demo
        var random = new Random(42);
        var payloadChars = new char[50_000];
        for (int c = 0; c < payloadChars.Length; c++)
            payloadChars[c] = (char)('A' + random.Next(26));
        var largePayload = new string(payloadChars); // ~50 KB incompressible

        for (int i = 0; i < 50; i++) // ~2.5 MB total >> 1 MB buffer
        {
            await producer.ProduceAsync("sensor-telemetry", $"sensor-{i % 10}", $"{largePayload}-{i}");
            if (i % 10 == 9) await producer.FlushAsync(); // Flush every 10 messages
        }
        await producer.FlushAsync();

        // Check LogStartOffset via topic description
        var topicInfo = await nativeClient.Topics.DescribeAsync("sensor-telemetry");
        foreach (var partition in topicInfo.Partitions)
        {
            Console.WriteLine($"  Partition {partition.PartitionId}: LogStartOffset={partition.LogStartOffset}, HighWatermark={partition.HighWatermark}");
        }

        if (topicInfo.Partitions.Any(p => p.LogStartOffset > 0))
            Console.WriteLine("LogStartOffset > 0 confirms ring-buffer wrap-around (old data evicted)!");
        else
            Console.WriteLine("Buffer held all messages - try a smaller buffer size to observe wrap-around.");
        Console.WriteLine();

        // Step 8: Compare persistent vs ephemeral
        Console.WriteLine("--- Step 8: Persistent vs Ephemeral ---");
        Console.WriteLine("Producing 10 messages to both topics...");

        for (int i = 1; i <= 10; i++)
        {
            var value = $"reading-{i}";
            await producer.ProduceAsync("sensor-telemetry", "compare", value);
            await producer.ProduceAsync("sensor-telemetry-persistent", "compare", value);
        }
        await producer.FlushAsync();

        // Read persistent topic from beginning
        await using var persistentConsumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });
        persistentConsumer.Subscribe("sensor-telemetry-persistent");

        int persistentCount = 0;
        while (true)
        {
            var msg = await persistentConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(300));
            if (msg == null) break;
            persistentCount++;
        }

        Console.WriteLine($"Persistent topic: {persistentCount} messages (full history preserved)");
        Console.WriteLine("Ephemeral topic: only recent messages within ring-buffer window");
        Console.WriteLine();

        // Summary
        Console.WriteLine("--- Summary ---");
        Console.WriteLine("Ephemeral topics use a fixed-size ring buffer for real-time event distribution.");
        Console.WriteLine("Messages are NOT persisted - data is lost on broker restart.");
        Console.WriteLine("The ring buffer evicts oldest messages when full (LogStartOffset advances).");
        Console.WriteLine("Ideal for: live dashboards, sensor telemetry, cache invalidation, heartbeats.");
        Console.WriteLine("NOT suitable for: audit logs, order processing, event sourcing.");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
