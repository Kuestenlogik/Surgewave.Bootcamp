// Surgewave Bootcamp - Unit 3, Lesson 3: Error Handling
// COMPLETED SOLUTION

using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

public class RetryProcessor
{
    private readonly SurgewaveProducer<string, string> _dlqProducer;
    private readonly int _maxRetries;
    private int _successCount;
    private int _failureCount;

    public RetryProcessor(SurgewaveProducer<string, string> dlqProducer, int maxRetries = 3)
    {
        _dlqProducer = dlqProducer;
        _maxRetries = maxRetries;
    }

    public int SuccessCount => _successCount;
    public int FailureCount => _failureCount;

    public async Task<bool> ProcessAsync(ConsumeResult<string, string> message)
    {
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                await ProcessMessage(message.Value);
                _successCount++;
                return true;
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                Console.WriteLine($"    Attempt {attempt} failed: {ex.Message}");
                var delay = TimeSpan.FromMilliseconds(50 * Math.Pow(2, attempt));
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    All {_maxRetries} attempts failed. Sending to DLQ.");
                await SendToDeadLetterQueue(message, ex);
                _failureCount++;
                return false;
            }
        }
        return false;
    }

    private Task ProcessMessage(string value)
    {
        // Simulate processing - fails if value contains "FAIL"
        if (value.Contains("FAIL"))
            throw new InvalidOperationException("Processing failed: invalid data");

        Console.WriteLine($"    Processed successfully");
        return Task.CompletedTask;
    }

    private async Task SendToDeadLetterQueue(
        ConsumeResult<string, string> message,
        Exception ex)
    {
        var headers = new Dictionary<string, byte[]>
        {
            { "x-original-topic", Encoding.UTF8.GetBytes(message.Topic) },
            { "x-original-partition", Encoding.UTF8.GetBytes(message.Partition.ToString()) },
            { "x-original-offset", Encoding.UTF8.GetBytes(message.Offset.ToString()) },
            { "x-error-message", Encoding.UTF8.GetBytes(ex.Message) },
            { "x-failed-at", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) },
            { "x-retry-count", Encoding.UTF8.GetBytes(_maxRetries.ToString()) }
        };

        await _dlqProducer.ProduceAsync(
            $"{message.Topic}.dlq",
            message.Key,
            message.Value,
            headers);
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
            .WithStorageMode(StorageMode.Memory)
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

        await using var dlqConsumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        var processor = new RetryProcessor(producer, maxRetries: 3);

        // Send test messages (some will fail)
        var messages = new[]
        {
            ("order-1", "Process order 1"),
            ("order-2", "FAIL - Invalid data"),
            ("order-3", "Process order 3"),
            ("order-4", "FAIL - Timeout error"),
            ("order-5", "Process order 5")
        };

        Console.WriteLine("Sending messages...");
        foreach (var (key, value) in messages)
        {
            await producer.ProduceAsync("orders", key, value);
            Console.WriteLine($"  Sent: {key}");
        }

        // Process messages with retry logic
        consumer.Subscribe("orders");

        Console.WriteLine();
        Console.WriteLine("Processing messages with retry logic...");
        Console.WriteLine();

        for (int i = 0; i < messages.Length; i++)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
            if (result != null)
            {
                Console.WriteLine($"Processing: {result.Key} - {result.Value}");
                var success = await processor.ProcessAsync(result);
                Console.WriteLine($"  Result: {(success ? "SUCCESS" : "FAILED -> DLQ")}");
                Console.WriteLine();
            }
        }

        // Check DLQ
        Console.WriteLine("--- Dead Letter Queue Contents ---");
        dlqConsumer.Subscribe("orders.dlq");

        while (true)
        {
            var dlqMessage = await dlqConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
            if (dlqMessage == null) break;

            Console.WriteLine($"DLQ Message: {dlqMessage.Key}");
            Console.WriteLine($"  Value: {dlqMessage.Value}");

            if (dlqMessage.Headers?.TryGetValue("x-error-message", out var errorBytes) == true)
                Console.WriteLine($"  Error: {Encoding.UTF8.GetString(errorBytes)}");

            if (dlqMessage.Headers?.TryGetValue("x-failed-at", out var failedAtBytes) == true)
                Console.WriteLine($"  Failed at: {Encoding.UTF8.GetString(failedAtBytes)}");

            Console.WriteLine();
        }

        Console.WriteLine("--- Summary ---");
        Console.WriteLine($"Successful: {processor.SuccessCount}");
        Console.WriteLine($"Failed (sent to DLQ): {processor.FailureCount}");
        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
