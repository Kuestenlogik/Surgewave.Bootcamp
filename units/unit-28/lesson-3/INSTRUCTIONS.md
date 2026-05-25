# Lesson 3.3: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Create a Message Processor with Retry Logic

```csharp
public class RetryProcessor
{
    private readonly int _maxRetries;
    private readonly IProducer<string, string> _dlqProducer;

    public RetryProcessor(IProducer<string, string> dlqProducer, int maxRetries = 3)
    {
        _dlqProducer = dlqProducer;
        _maxRetries = maxRetries;
    }

    public async Task<bool> ProcessAsync(ConsumeResult<string, string> message)
    {
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                await ProcessMessage(message.Value);
                return true;
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                Console.WriteLine($"  Attempt {attempt} failed: {ex.Message}");
                var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt));
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  All retries exhausted. Sending to DLQ.");
                await SendToDeadLetterQueue(message, ex);
                return false;
            }
        }
        return false;
    }

    private async Task ProcessMessage(string value)
    {
        // Simulate processing that might fail
        if (value.Contains("FAIL"))
            throw new InvalidOperationException("Processing failed");

        Console.WriteLine($"  Processed: {value}");
    }

    private async Task SendToDeadLetterQueue(
        ConsumeResult<string, string> message,
        Exception ex)
    {
        var headers = new Headers
        {
            { "x-original-topic", Encoding.UTF8.GetBytes(message.Topic) },
            { "x-error-message", Encoding.UTF8.GetBytes(ex.Message) },
            { "x-failed-at", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
        };

        await _dlqProducer.ProduceAsync(
            $"{message.Topic}.dlq",
            message.Key,
            message.Value,
            headers);
    }
}
```

## Step 2: Set Up Topics

```csharp
// Main topic for orders
const string OrdersTopic = "orders";

// Dead letter queue for failed orders
const string OrdersDlqTopic = "orders.dlq";
```

## Step 3: Create Processing Loop

```csharp
var processor = new RetryProcessor(producer, maxRetries: 3);

consumer.Subscribe(OrdersTopic);

while (true)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(1));
    if (result == null) break;

    Console.WriteLine($"Processing: {result.Key}");
    var success = await processor.ProcessAsync(result);
    Console.WriteLine($"  Result: {(success ? "Success" : "Failed -> DLQ")}");
}
```

## Step 4: Monitor the Dead Letter Queue

```csharp
// Create DLQ consumer
var dlqConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "dlq-monitor";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

dlqConsumer.Subscribe(OrdersDlqTopic);

// Process DLQ messages (alert, manual review, etc.)
var dlqMessage = await dlqConsumer.ConsumeAsync(timeout);
if (dlqMessage != null)
{
    var errorMessage = dlqMessage.Headers.GetValueOrDefault("x-error-message");
    Console.WriteLine($"DLQ: {dlqMessage.Key} - Error: {errorMessage}");
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. When should you retry vs send to DLQ immediately?
2. Why use exponential backoff instead of fixed delays?
3. What metadata should you include in DLQ messages?

---

**Next:** → [Lesson 3.4: Transactions](../lesson-4/README.md)
