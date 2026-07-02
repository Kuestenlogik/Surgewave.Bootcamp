// Unit 3 Project: Payment Processing System - COMPLETED
// A robust payment processor with retries and dead letter queues

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
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
    .WithStorageEngine(StorageEngines.Memory)
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
    opt.EnableAutoCommit = false;
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
    new("PAY-006", "ACC-100", "ACC-200", 250.00m, "EUR", DateTime.UtcNow, PaymentType.Deposit),
    new("PAY-001", "ACC-100", "ACC-200", 150.00m, "USD", DateTime.UtcNow, PaymentType.Transfer),  // Duplicate!
};

// Step 1: Send payments to pending topic
Console.WriteLine("--- Sending Payments ---");
foreach (var payment in payments)
{
    var json = JsonSerializer.Serialize(payment);
    await producer.ProduceAsync("payments.pending", payment.PaymentId, json);

    var note = payment.Amount <= 0 ? " [INVALID]"
             : payment.PaymentId.Contains("TIMEOUT") ? " [WILL TIMEOUT]"
             : string.IsNullOrEmpty(payment.AccountTo) ? " [INVALID]"
             : "";
    Console.WriteLine($"[Producer] Sending payment {payment.PaymentId} (${payment.Amount:F2} {payment.Currency}){note}");
}
await producer.FlushAsync();
Console.WriteLine();

// Step 2: Process payments
Console.WriteLine("--- Processing Payments ---");
consumer.Subscribe("payments.pending");

var results = new List<PaymentResult>();
for (int i = 0; i < payments.Count; i++)
{
    var message = await consumer.ConsumeAsync(TimeSpan.FromSeconds(10));
    if (message != null)
    {
        try
        {
            var result = await processor.ProcessAsync(message);
            results.Add(result);
            await consumer.CommitAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Unexpected error: {ex.Message}");
        }
    }
    Console.WriteLine();
}

// Step 3: Print summary
Console.WriteLine(new string('=', 50));
Console.WriteLine("=== Processing Summary ===");
Console.WriteLine($"Total processed: {results.Count} payments");
Console.WriteLine($"Successful: {processor.SuccessCount} ({100.0 * processor.SuccessCount / Math.Max(1, results.Count):F1}%)");
Console.WriteLine($"Failed (DLQ): {processor.FailureCount} ({100.0 * processor.FailureCount / Math.Max(1, results.Count):F1}%)");
Console.WriteLine($"Duplicates: {processor.DuplicateCount}");
Console.WriteLine($"Total retries: {processor.TotalRetries}");
Console.WriteLine($"Average latency: {processor.AverageLatencyMs:F0}ms");
Console.WriteLine();

// Step 4: Read and display DLQ contents
Console.WriteLine("=== Dead Letter Queue ===");
dlqConsumer.Subscribe("payments.dlq");

var dlqCount = 0;
while (true)
{
    var dlqMessage = await dlqConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
    if (dlqMessage == null) break;

    dlqCount++;
    var errorType = dlqMessage.Headers != null && dlqMessage.Headers.TryGetValue("x-error-type", out var etBytes)
        ? Encoding.UTF8.GetString(etBytes) : "UNKNOWN";
    var errorMessage = dlqMessage.Headers != null && dlqMessage.Headers.TryGetValue("x-error-message", out var emBytes)
        ? Encoding.UTF8.GetString(emBytes) : "No message";
    var retryCount = dlqMessage.Headers != null && dlqMessage.Headers.TryGetValue("x-retry-count", out var rcBytes)
        ? Encoding.UTF8.GetString(rcBytes) : "0";
    var failedAt = dlqMessage.Headers != null && dlqMessage.Headers.TryGetValue("x-failed-at", out var faBytes)
        ? Encoding.UTF8.GetString(faBytes) : "Unknown";

    Console.WriteLine($"DLQ #{dlqCount}: {dlqMessage.Key}");
    Console.WriteLine($"  Error Type: {errorType}");
    Console.WriteLine($"  Message: {errorMessage}");
    Console.WriteLine($"  Retry Count: {retryCount}");
    Console.WriteLine($"  Failed At: {failedAt}");
    Console.WriteLine();
}

if (dlqCount == 0)
{
    Console.WriteLine("  (No messages in DLQ)");
}

Console.WriteLine(new string('=', 50));
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

public enum PaymentStatus { Completed, Failed, PendingRetry, Duplicate }

// Custom exception types for different error categories
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

public class TransientException : Exception
{
    public TransientException(string message) : base(message) { }
}

public class PermanentException : Exception
{
    public PermanentException(string message) : base(message) { }
}

// =============================================================================
// PAYMENT PROCESSOR
// =============================================================================

public class PaymentProcessor
{
    private readonly SurgewaveProducer<string, string> _producer;
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;

    // Idempotency tracking
    private readonly HashSet<string> _processedPayments = new();
    private readonly object _lock = new();

    // Metrics
    private int _successCount;
    private int _failureCount;
    private int _duplicateCount;
    private int _retryCount;
    private long _totalLatencyMs;

    public int SuccessCount => _successCount;
    public int FailureCount => _failureCount;
    public int DuplicateCount => _duplicateCount;
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

        // Step 1: Check for duplicate payment (idempotency)
        lock (_lock)
        {
            if (_processedPayments.Contains(payment.PaymentId))
            {
                Console.WriteLine($"[Processor] {payment.PaymentId} -> DUPLICATE (already processed)");
                Interlocked.Increment(ref _duplicateCount);
                return new PaymentResult(
                    payment.PaymentId,
                    PaymentStatus.Duplicate,
                    "Payment already processed",
                    0,
                    DateTime.UtcNow);
            }
        }

        // Step 2: Validate payment
        try
        {
            ValidatePayment(payment);
        }
        catch (ValidationException ex)
        {
            await SendToDlqAsync(payment, "VALIDATION_ERROR", ex.Message, 0);
            sw.Stop();
            Interlocked.Add(ref _totalLatencyMs, sw.ElapsedMilliseconds);
            return new PaymentResult(
                payment.PaymentId,
                PaymentStatus.Failed,
                ex.Message,
                0,
                DateTime.UtcNow);
        }

        // Step 3: Process with retries
        int currentRetry = 0;
        string? lastError = null;

        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                await ExecutePayment(payment);

                // Success!
                sw.Stop();
                lock (_lock)
                {
                    _processedPayments.Add(payment.PaymentId);
                }
                Interlocked.Increment(ref _successCount);
                Interlocked.Add(ref _totalLatencyMs, sw.ElapsedMilliseconds);

                // Send to completed topic
                await _producer.ProduceAsync(
                    "payments.completed",
                    payment.PaymentId,
                    JsonSerializer.Serialize(payment));

                var retryNote = currentRetry > 0 ? $" (after {currentRetry} retries)" : "";
                Console.WriteLine($"[Processor] {payment.PaymentId} -> SUCCESS{retryNote}");

                return new PaymentResult(
                    payment.PaymentId,
                    PaymentStatus.Completed,
                    null,
                    currentRetry,
                    DateTime.UtcNow);
            }
            catch (TransientException ex)
            {
                currentRetry++;
                lastError = ex.Message;
                Interlocked.Increment(ref _retryCount);

                Console.WriteLine($"[Processor] {payment.PaymentId} -> RETRY (attempt {attempt}): {ex.Message}");

                if (attempt < _maxRetries)
                {
                    // Exponential backoff
                    var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                    await Task.Delay(delay);
                }
            }
            catch (PermanentException ex)
            {
                // Permanent error - send to DLQ immediately
                await SendToDlqAsync(payment, "PERMANENT_ERROR", ex.Message, currentRetry);
                sw.Stop();
                Interlocked.Add(ref _totalLatencyMs, sw.ElapsedMilliseconds);
                return new PaymentResult(
                    payment.PaymentId,
                    PaymentStatus.Failed,
                    ex.Message,
                    currentRetry,
                    DateTime.UtcNow);
            }
        }

        // Max retries exceeded - send to DLQ
        await SendToDlqAsync(payment, "MAX_RETRIES", lastError ?? "Max retries exceeded", currentRetry);
        sw.Stop();
        Interlocked.Add(ref _totalLatencyMs, sw.ElapsedMilliseconds);

        return new PaymentResult(
            payment.PaymentId,
            PaymentStatus.Failed,
            $"Max retries exceeded: {lastError}",
            currentRetry,
            DateTime.UtcNow);
    }

    private void ValidatePayment(PaymentEvent payment)
    {
        if (payment.Amount <= 0)
            throw new ValidationException("Amount must be positive");

        if (string.IsNullOrWhiteSpace(payment.AccountFrom))
            throw new ValidationException("Source account is required");

        if (string.IsNullOrWhiteSpace(payment.AccountTo))
            throw new ValidationException("Destination account is required");

        if (string.IsNullOrWhiteSpace(payment.Currency) || payment.Currency.Length != 3)
            throw new ValidationException("Currency must be a 3-letter code");

        if (payment.AccountFrom == payment.AccountTo)
            throw new ValidationException("Source and destination accounts must be different");
    }

    private Task ExecutePayment(PaymentEvent payment)
    {
        // Simulate payment processing with deterministic failures based on PaymentId
        if (payment.PaymentId.Contains("TIMEOUT"))
            throw new TransientException("Connection timeout");

        if (payment.PaymentId.Contains("NETWORK"))
            throw new TransientException("Network error");

        if (payment.PaymentId.Contains("INSUFFICIENT"))
            throw new PermanentException("Insufficient funds");

        // Random transient failure for demonstration (based on payment amount)
        var random = new Random((int)(payment.Amount * 100) + DateTime.Now.Millisecond);
        if (random.NextDouble() < 0.15 && !payment.PaymentId.Contains("PAY-001"))
            throw new TransientException("Temporary service unavailable");

        return Task.CompletedTask;
    }

    private async Task SendToDlqAsync(
        PaymentEvent payment,
        string errorType,
        string errorMessage,
        int retryCount)
    {
        // Build headers with comprehensive metadata using Dictionary<string, byte[]>
        var headers = new Dictionary<string, byte[]>
        {
            ["x-error-type"] = Encoding.UTF8.GetBytes(errorType),
            ["x-error-message"] = Encoding.UTF8.GetBytes(errorMessage),
            ["x-retry-count"] = Encoding.UTF8.GetBytes(retryCount.ToString()),
            ["x-original-timestamp"] = Encoding.UTF8.GetBytes(payment.CreatedAt.ToString("O")),
            ["x-failed-at"] = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")),
            ["x-payment-type"] = Encoding.UTF8.GetBytes(payment.Type.ToString()),
            ["x-amount"] = Encoding.UTF8.GetBytes(payment.Amount.ToString("F2")),
            ["x-currency"] = Encoding.UTF8.GetBytes(payment.Currency)
        };

        // Send to DLQ
        await _producer.ProduceAsync(
            "payments.dlq",
            payment.PaymentId,
            JsonSerializer.Serialize(payment),
            headers);

        Interlocked.Increment(ref _failureCount);
        Console.WriteLine($"[Processor] {payment.PaymentId} -> Sent to DLQ ({errorType})");
    }
}
