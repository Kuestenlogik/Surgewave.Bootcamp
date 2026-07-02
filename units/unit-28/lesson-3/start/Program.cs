// Surgewave Bootcamp - Unit 3, Lesson 3: Error Handling
//
// In this lesson, you'll learn to:
// - Implement retry logic with exponential backoff
// - Use dead letter queues for failed messages
// - Build resilient message processing
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

// TODO: Implement RetryProcessor class
public class RetryProcessor
{
    private readonly SurgewaveProducer<string, string> _dlqProducer;
    private readonly int _maxRetries;

    public RetryProcessor(SurgewaveProducer<string, string> dlqProducer, int maxRetries = 3)
    {
        _dlqProducer = dlqProducer;
        _maxRetries = maxRetries;
    }

    public async Task<bool> ProcessAsync(ConsumeResult<string, string> message)
    {
        // TODO: Implement retry loop with exponential backoff
        // 1. Try to process the message
        // 2. On failure, retry with increasing delay
        // 3. After max retries, send to DLQ
        throw new NotImplementedException();
    }

    private Task ProcessMessage(string value)
    {
        // Simulate processing - fails if value contains "FAIL"
        if (value.Contains("FAIL"))
            throw new InvalidOperationException("Processing failed");

        Console.WriteLine($"    Processed successfully: {value}");
        return Task.CompletedTask;
    }

    private async Task SendToDeadLetterQueue(
        ConsumeResult<string, string> message,
        Exception ex)
    {
        // TODO: Send message to DLQ with error metadata
        // Include: original topic, error message, timestamp
        throw new NotImplementedException();
    }
}

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 3.3 - Error Handling ===");
        Console.WriteLine();

        // Start embedded broker
        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine($"Broker started on port {runtime.Port}");

        await using var producer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = $"localhost:{runtime.Port}");

        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        // TODO: Create processor with DLQ producer
        // var processor = new RetryProcessor(producer, maxRetries: 3);

        // Send test messages (some will fail)
        var messages = new[]
        {
            ("order-1", "Process order 1"),
            ("order-2", "FAIL - Invalid data"),
            ("order-3", "Process order 3"),
            ("order-4", "FAIL - Timeout"),
            ("order-5", "Process order 5")
        };

        Console.WriteLine("Sending messages...");
        foreach (var (key, value) in messages)
        {
            await producer.ProduceAsync("orders", key, value);
        }

        // TODO: Process messages with retry logic
        consumer.Subscribe("orders");

        Console.WriteLine();
        Console.WriteLine("Processing messages...");
        // for each message: await processor.ProcessAsync(message)

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
