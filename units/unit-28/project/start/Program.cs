// Unit 3 Project: Payment Processing System
// Build a robust payment processor with retries and dead letter queues

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Payment Processing System ===\n");

// =============================================================================
// MAIN PROGRAM
// =============================================================================

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"[Startup] Broker started on port {runtime.Port}");

await using var producer = new SurgewaveProducer<string, string>(opt =>
    opt.BootstrapServers = $"localhost:{runtime.Port}");

await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "payment-processor";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await using var dlqConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "dlq-reader";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

Console.WriteLine("[Startup] Topics: payments.pending, payments.completed, payments.dlq\n");

// Create processor
var processor = new PaymentProcessor(producer, maxRetries: 3);

// Sample payments to process
var payments = new List<PaymentEvent>
{
    new("PAY-001", "ACC-100", "ACC-200", 150.00m, "USD", DateTime.UtcNow, PaymentType.Transfer),
    new("PAY-002", "ACC-100", "ACC-200", -50.00m, "USD", DateTime.UtcNow, PaymentType.Transfer),  // Invalid: negative
    new("PAY-003", "ACC-100", "ACC-200", 500.00m, "USD", DateTime.UtcNow, PaymentType.Transfer),
    new("PAY-TIMEOUT-004", "ACC-100", "ACC-200", 100.00m, "USD", DateTime.UtcNow, PaymentType.Transfer),  // Will timeout
    new("PAY-005", "ACC-100", "", 75.00m, "USD", DateTime.UtcNow, PaymentType.Transfer),  // Invalid: no recipient
};

// TODO: Step 1 - Send payments to pending topic
Console.WriteLine("--- Sending Payments ---");
foreach (var payment in payments)
{
    var json = JsonSerializer.Serialize(payment);
    // TODO: Send to payments.pending topic
    // await producer.ProduceAsync("payments.pending", payment.PaymentId, json);

    var note = payment.Amount <= 0 ? " [INVALID]"
             : payment.PaymentId.Contains("TIMEOUT") ? " [WILL TIMEOUT]"
             : string.IsNullOrEmpty(payment.AccountTo) ? " [INVALID]"
             : "";
    Console.WriteLine($"[Producer] Sending payment {payment.PaymentId} (${payment.Amount:F2} {payment.Currency}){note}");
}
await producer.FlushAsync();
Console.WriteLine();

// TODO: Step 2 - Process payments
Console.WriteLine("--- Processing Payments ---");
consumer.Subscribe("payments.pending");

for (int i = 0; i < payments.Count; i++)
{
    var message = await consumer.ConsumeAsync(TimeSpan.FromSeconds(10));
    if (message != null)
    {
        try
        {
            // TODO: Process the payment
            // var result = await processor.ProcessAsync(message);
            Console.WriteLine($"[TODO] Process message: {message.Key}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to process: {ex.Message}");
        }
    }
}

// TODO: Step 3 - Print summary
Console.WriteLine();
Console.WriteLine("=== Processing Summary ===");
Console.WriteLine($"Processed: {processor.SuccessCount + processor.FailureCount} payments");
Console.WriteLine($"Successful: {processor.SuccessCount}");
Console.WriteLine($"Failed (DLQ): {processor.FailureCount}");
Console.WriteLine($"Total retries: {processor.TotalRetries}");
Console.WriteLine($"Average latency: {processor.AverageLatencyMs:F0}ms");

// TODO: Step 4 - Read and display DLQ contents
Console.WriteLine();
Console.WriteLine("=== Dead Letter Queue ===");
dlqConsumer.Subscribe("payments.dlq");

while (true)
{
    var dlqMessage = await dlqConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
    if (dlqMessage == null) break;

    // TODO: Display DLQ message with headers
    // Extract x-error-type and x-error-message from headers
    Console.WriteLine($"[TODO] DLQ message: {dlqMessage.Key}");
}

Console.WriteLine();
Console.WriteLine("=== Payment Processing Complete ===");

// =============================================================================
// DATA MODELS
// =============================================================================

public record PaymentEvent(
    string PaymentId,
    string AccountFrom,
    string AccountTo,
    decimal Amount,
    string Currency,
    DateTime CreatedAt,
    PaymentType Type
);

public enum PaymentType { Transfer, Withdrawal, Deposit, Refund }

public record PaymentResult(
    string PaymentId,
    PaymentStatus Status,
    string? ErrorMessage,
    int RetryCount,
    DateTime ProcessedAt
);

public enum PaymentStatus { Completed, Failed, PendingRetry }

// Custom exception types for different error categories
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

public class TransientException : Exception
{
    public TransientException(string message) : base(message) { }
}

// =============================================================================
// PAYMENT PROCESSOR
// =============================================================================

public class PaymentProcessor
{
    private readonly SurgewaveProducer<string, string> _producer;
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;

    // TODO: Add tracking for processed payments (idempotency)
    // Hint: private readonly HashSet<string> _processedPayments = new();

    // Metrics
    private int _successCount;
    private int _failureCount;
    private int _retryCount;
    private long _totalLatencyMs;

    public int SuccessCount => _successCount;
    public int FailureCount => _failureCount;
    public int TotalRetries => _retryCount;
    public double AverageLatencyMs => _successCount + _failureCount > 0
        ? (double)_totalLatencyMs / (_successCount + _failureCount) : 0;

    public PaymentProcessor(SurgewaveProducer<string, string> producer, int maxRetries = 3)
    {
        _producer = producer;
        _maxRetries = maxRetries;
        _baseDelay = TimeSpan.FromMilliseconds(100);
    }

    public async Task<PaymentResult> ProcessAsync(ConsumeResult<string, string> message)
    {
        var sw = Stopwatch.StartNew();
        var payment = JsonSerializer.Deserialize<PaymentEvent>(message.Value)!;

        Console.WriteLine($"[Processor] Processing {payment.PaymentId} (${payment.Amount:F2} {payment.Currency})");

        // TODO: Step 1 - Check for duplicate payment (idempotency)
        // If PaymentId already processed, return early with Completed status
        // Hint: if (_processedPayments.Contains(payment.PaymentId)) { ... }

        // TODO: Step 2 - Validate payment
        // Check: Amount > 0, AccountFrom not empty, AccountTo not empty, Currency valid
        // Throw ValidationException for invalid payments
        try
        {
            ValidatePayment(payment);
        }
        catch (ValidationException ex)
        {
            // TODO: Send to DLQ immediately (permanent failure)
            // Call SendToDlqAsync with error type "VALIDATION_ERROR"
            Console.WriteLine($"[Processor] {payment.PaymentId} -> VALIDATION_ERROR: {ex.Message}");
            throw new NotImplementedException("Implement DLQ routing for validation errors");
        }

        // TODO: Step 3 - Process with retries
        // Loop up to _maxRetries times
        // On TransientException: log retry, wait with exponential backoff
        // On success: mark as processed, update metrics, return Completed
        // After max retries: send to DLQ
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                // TODO: Call ExecutePayment(payment)
                // On success: update _successCount, _totalLatencyMs
                // Add to _processedPayments for idempotency
                // Return PaymentResult with Completed status

                throw new NotImplementedException("Implement payment execution with retries");
            }
            catch (TransientException ex)
            {
                // TODO: Log retry attempt
                // If not last attempt: calculate delay and wait
                // delay = _baseDelay * Math.Pow(2, attempt - 1)
                Console.WriteLine($"[Processor] {payment.PaymentId} -> RETRY (attempt {attempt}): {ex.Message}");
                _retryCount++;

                if (attempt < _maxRetries)
                {
                    // TODO: Calculate and apply exponential backoff delay
                }
            }
        }

        // TODO: Max retries exceeded - send to DLQ
        // Call SendToDlqAsync with error type "MAX_RETRIES"
        throw new NotImplementedException("Implement DLQ routing for max retries");
    }

    private void ValidatePayment(PaymentEvent payment)
    {
        // TODO: Implement validation
        // - Amount must be positive
        // - AccountFrom must not be empty
        // - AccountTo must not be empty
        // - Currency must be 3 characters (e.g., USD, EUR)
        // Throw ValidationException with descriptive message

        if (payment.Amount <= 0)
            throw new ValidationException("Amount must be positive");

        // TODO: Add more validations
    }

    private Task ExecutePayment(PaymentEvent payment)
    {
        // Simulate payment processing with random failures
        var random = new Random(payment.PaymentId.GetHashCode() + DateTime.Now.Millisecond);

        // Simulate different scenarios based on payment properties
        if (payment.PaymentId.Contains("TIMEOUT"))
            throw new TransientException("Connection timeout");

        if (payment.PaymentId.Contains("NETWORK"))
            throw new TransientException("Network error");

        // 20% chance of transient failure
        if (random.NextDouble() < 0.2)
            throw new TransientException("Temporary service unavailable");

        Console.WriteLine($"[Processor] {payment.PaymentId} -> SUCCESS");
        return Task.CompletedTask;
    }

    private async Task SendToDlqAsync(
        PaymentEvent payment,
        string errorType,
        string errorMessage,
        int retryCount)
    {
        // TODO: Build headers with metadata
        // Use Dictionary<string, byte[]> for headers
        // - x-error-type: errorType
        // - x-error-message: errorMessage
        // - x-retry-count: retryCount
        // - x-original-timestamp: payment.CreatedAt
        // - x-failed-at: DateTime.UtcNow
        var headers = new Dictionary<string, byte[]>();
        // TODO: Add headers
        // Hint: headers["x-error-type"] = Encoding.UTF8.GetBytes(errorType);

        // TODO: Send to payments.dlq topic
        // await _producer.ProduceAsync("payments.dlq", payment.PaymentId, JsonSerializer.Serialize(payment), headers);

        _failureCount++;
        Console.WriteLine($"[Processor] {payment.PaymentId} -> Sent to DLQ ({errorType})");

        await Task.CompletedTask;
        throw new NotImplementedException("Implement DLQ message sending");
    }
}
