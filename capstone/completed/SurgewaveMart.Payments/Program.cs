// SurgewaveMart Payment Service - COMPLETED
// Handles payment processing with exactly-once semantics

using System.Collections.Concurrent;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using SurgewaveMart.Shared;
using SurgewaveMart.Shared.Domain;
using SurgewaveMart.Shared.Events;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== SurgewaveMart Payment Service ===\n");

// Configuration
const int MaxRetries = 3;
const int BaseBackoffMs = 1000;
const double SuccessRate = 0.85; // 85% success rate for simulation

// Idempotency store
var processedPayments = new ConcurrentDictionary<string, PaymentCompleted>();
var failedPayments = new ConcurrentDictionary<string, PaymentFailed>();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;

Console.WriteLine("[Setup] Topics will be auto-created");

// Create producer and consumer
await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "payment-service";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe(Topics.OrdersConfirmed);

Console.WriteLine("[Consumer] Starting payment processor...");
Console.WriteLine($"[Config] Max retries: {MaxRetries}, Success rate: {SuccessRate:P0}\n");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var processedCount = 0;
var successCount = 0;
var failedCount = 0;
var duplicateCount = 0;

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        var result = await consumer.ConsumeAsync(cts.Token);
        if (result is null) continue;

        try
        {
            var orderConfirmed = JsonSerializer.Deserialize<OrderConfirmed>(result.Value);
            if (orderConfirmed is null)
            {
                Console.WriteLine("[Error] Invalid confirmed order message");
                continue;
            }

            var orderId = orderConfirmed.OrderId;
            Console.WriteLine($"[Processing] Payment for order {orderId}");

            // Idempotency check
            if (processedPayments.ContainsKey(orderId))
            {
                Console.WriteLine($"[Duplicate] Payment for {orderId} already processed, skipping");
                duplicateCount++;
                continue;
            }

            if (failedPayments.ContainsKey(orderId))
            {
                Console.WriteLine($"[Duplicate] Payment for {orderId} already failed, skipping");
                duplicateCount++;
                continue;
            }

            // Process payment with retries
            var amount = 100.00m; // In production, get from order state
            var paymentResult = await ProcessPaymentWithRetry(orderId, amount, MaxRetries);

            if (paymentResult.Success)
            {
                var paymentId = $"PAY-{Guid.NewGuid():N}"[..12];
                var transactionId = paymentResult.TransactionId!;

                var completed = new PaymentCompleted(
                    paymentId, orderId, amount,
                    PaymentMethod.CreditCard, transactionId, DateTime.UtcNow);

                // Store in idempotency cache
                processedPayments[orderId] = completed;

                // Produce to payments topic
                await producer.ProduceAsync(Topics.Payments, orderId, JsonSerializer.Serialize(completed));

                Console.WriteLine($"[Success] Payment {paymentId} completed for order {orderId} (TXN: {transactionId})");
                successCount++;
            }
            else
            {
                var paymentId = $"PAY-{Guid.NewGuid():N}"[..12];

                var failed = new PaymentFailed(
                    paymentId, orderId, amount,
                    paymentResult.Error!, MaxRetries, DateTime.UtcNow);

                // Store in idempotency cache
                failedPayments[orderId] = failed;

                // Send to DLQ
                await SendToDlq(producer, orderId, failed);

                // Produce failure event for order cancellation
                var cancelled = new OrderCancelled(orderId, $"Payment failed: {paymentResult.Error}", DateTime.UtcNow);
                await producer.ProduceAsync(Topics.Orders, orderId, JsonSerializer.Serialize(cancelled));

                Console.WriteLine($"[Failed] Payment for order {orderId} after {MaxRetries} retries: {paymentResult.Error}");
                failedCount++;
            }

            processedCount++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Unexpected payment error: {ex.Message}");
        }
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("\n[Shutdown] Payment service stopping...");
}

// Print summary
Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("[Stats] Payment Processing Summary:");
Console.WriteLine($"  Processed:  {processedCount}");
Console.WriteLine($"  Successful: {successCount}");
Console.WriteLine($"  Failed:     {failedCount}");
Console.WriteLine($"  Duplicates: {duplicateCount}");
Console.WriteLine(new string('=', 50));

Console.WriteLine("=== Payment Service Stopped ===");

// Payment processing with exponential backoff retry
async Task<PaymentResult> ProcessPaymentWithRetry(string orderId, decimal amount, int maxRetries)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            // Simulate payment processing
            await Task.Delay(50); // Simulate network latency

            var success = Random.Shared.NextDouble() < SuccessRate;
            if (success)
            {
                var transactionId = $"TXN-{Guid.NewGuid():N}"[..12];
                return new PaymentResult(true, transactionId, null);
            }

            throw new Exception("Payment declined by provider");
        }
        catch (Exception) when (attempt < maxRetries)
        {
            var backoff = BaseBackoffMs * (int)Math.Pow(2, attempt - 1);
            Console.WriteLine($"[Retry] Order {orderId}: Attempt {attempt} failed, retrying in {backoff}ms...");
            await Task.Delay(backoff);
        }
        catch (Exception ex) when (attempt == maxRetries)
        {
            return new PaymentResult(false, null, ex.Message);
        }
    }

    return new PaymentResult(false, null, "Max retries exceeded");
}

async Task SendToDlq(SurgewaveProducer<string, string> producer, string orderId, PaymentFailed failed)
{
    var dlqMessage = JsonSerializer.Serialize(new
    {
        OriginalTopic = Topics.OrdersConfirmed,
        OrderId = orderId,
        Error = failed.Reason,
        RetryCount = failed.RetryCount,
        FailedAt = failed.FailedAt
    });
    await producer.ProduceAsync(Topics.DeadLetterQueue, Topics.Payments, dlqMessage);
}

record PaymentResult(bool Success, string? TransactionId, string? Error);