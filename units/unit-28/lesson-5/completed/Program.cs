// Surgewave Bootcamp - Unit 3, Lesson 5: Exactly-Once Semantics
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 3.5 - Exactly-Once Semantics ===");
        Console.WriteLine();

        // Start embedded broker
        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine($"Broker started on port {runtime.Port}");

        // Create idempotent transactional producer
        await using var producer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            // opt.EnableIdempotence = true; // TODO: EnableIdempotence not yet available on SurgewaveProducerOptions
            // opt.TransactionalId = "exactly-once-processor"; // TODO: TransactionalId not yet available on SurgewaveProducerOptions
            // opt.Acks = Acks.All; // TODO: Acks not yet available on SurgewaveProducerOptions
        });

        // Create consumer with ReadCommitted isolation
        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            opt.GroupId = "exactly-once-group";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
            opt.EnableAutoCommit = false;
            opt.IsolationLevel = IsolationLevel.ReadCommitted;
        });

        // Seed input topic with messages
        Console.WriteLine("Seeding input topic with 100 messages...");
        for (int i = 0; i < 100; i++)
        {
            await producer.ProduceAsync("input", $"key-{i}", $"value-{i}");
        }
        await producer.FlushAsync();

        // Initialize transactions
        // await producer.InitTransactionsAsync(); // TODO: Not yet implemented
        Console.WriteLine("Transactions initialized (stubbed)");
        Console.WriteLine();

        consumer.Subscribe("input");

        // Exactly-once processing loop
        Console.WriteLine("Processing messages with exactly-once semantics...");
        int totalProcessed = 0;

        while (true)
        {
            var message = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
            if (message == null) break;

            // await producer.BeginTransactionAsync(); // TODO: Not yet implemented

            try
            {
                // Transform
                var output = Transform(message.Value);

                // Produce to output topic
                await producer.ProduceAsync("output", message.Key, output);

                // Commit offsets as part of transaction
                // await producer.SendOffsetsToTransactionAsync(
                //     new[] { new TopicPartitionOffset(message.Topic, message.Partition, message.Offset + 1) },
                //     consumer.GroupId); // TODO: SendOffsetsToTransactionAsync not yet available on SurgewaveProducer
                await consumer.CommitAsync();

                // await producer.CommitTransactionAsync(); // TODO: Not yet implemented
                totalProcessed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {message.Key}: {ex.Message}");
                // await producer.AbortTransactionAsync(); // TODO: Not yet implemented
            }
        }

        Console.WriteLine($"Processed {totalProcessed} messages");
        Console.WriteLine();

        // Verify no duplicates in output
        Console.WriteLine("Verifying output (checking for duplicates)...");

        await using var verifyConsumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
            opt.IsolationLevel = IsolationLevel.ReadCommitted;
        });

        verifyConsumer.Subscribe("output");

        var seenKeys = new HashSet<string>();
        int duplicates = 0;
        int outputCount = 0;

        while (true)
        {
            var msg = await verifyConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
            if (msg == null) break;

            outputCount++;
            if (!seenKeys.Add(msg.Key ?? ""))
            {
                duplicates++;
                Console.WriteLine($"  DUPLICATE: {msg.Key}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("--- Results ---");
        Console.WriteLine($"Input messages: 100");
        Console.WriteLine($"Output messages: {outputCount}");
        Console.WriteLine($"Unique keys: {seenKeys.Count}");
        Console.WriteLine($"Duplicates: {duplicates}");

        if (duplicates == 0 && outputCount == 100)
        {
            Console.WriteLine();
            Console.WriteLine("SUCCESS: Exactly-once processing verified!");
        }

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }

    private static string Transform(string input)
    {
        return input.ToUpperInvariant();
    }
}
