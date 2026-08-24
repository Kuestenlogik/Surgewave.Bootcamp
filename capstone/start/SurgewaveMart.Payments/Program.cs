// SurgewaveMart Payment Service
// Handles payment processing with exactly-once semantics

using System.Collections.Concurrent;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client.Producer;
using Kuestenlogik.Surgewave.Client.Consumer;
using SurgewaveMart.Shared;
using SurgewaveMart.Shared.Domain;
using SurgewaveMart.Shared.Events;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== SurgewaveMart Payment Service ===\n");

// Configuration
const int MaxRetries = 3;
const int BaseBackoffMs = 1000;

// Idempotency store (in production, use persistent state store)
var processedPayments = new ConcurrentDictionary<string, PaymentCompleted>();

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

// TODO: Implement idempotency check
// - Use orderId as idempotency key
// - Check if payment already processed
// - Return existing result if duplicate

// TODO: Implement payment processing with retries
// - Attempt payment (simulated)
// - On failure, retry with exponential backoff
// - Max 3 retries before DLQ

// TODO: Implement exactly-once semantics
// - Use transactions for read-process-write
// - Commit only after successful processing

Console.WriteLine("[Consumer] Starting payment processor...");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

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

            // TODO: Idempotency check
            // if (processedPayments.TryGetValue(orderId, out var existingPayment))
            // {
            //     Console.WriteLine($"[Duplicate] Payment for {orderId} already processed");
            //     continue;
            // }

            // TODO: Process payment with retries
            // var paymentResult = await ProcessPaymentWithRetry(orderId, amount, MaxRetries);

            // Simulated payment processing
            var paymentId = $"PAY-{Guid.NewGuid():N}"[..12];
            var transactionId = $"TXN-{Guid.NewGuid():N}"[..12];

            // TODO: Create PaymentCompleted event
            // var completed = new PaymentCompleted(
            //     paymentId, orderId, amount,
            //     PaymentMethod.CreditCard, transactionId, DateTime.UtcNow);

            // TODO: Store in idempotency cache
            // processedPayments[orderId] = completed;

            // TODO: Produce to payments topic
            // await producer.ProduceAsync(Topics.Payments, orderId, JsonSerializer.Serialize(completed));

            Console.WriteLine($"[Processed] Payment {paymentId} completed for order {orderId}");
        }
        catch (Exception ex)
        {
            // TODO: Send to DLQ after max retries
            Console.WriteLine($"[Error] Payment processing failed: {ex.Message}");
        }
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("\n[Shutdown] Payment service stopping...");
}

Console.WriteLine("=== Payment Service Stopped ===");

// TODO: Implement this method
// async Task<PaymentResult> ProcessPaymentWithRetry(string orderId, decimal amount, int maxRetries)
// {
//     for (int attempt = 1; attempt <= maxRetries; attempt++)
//     {
//         try
//         {
//             // Simulate payment processing
//             var success = Random.Shared.NextDouble() > 0.2; // 80% success rate
//             if (success)
//             {
//                 return new PaymentResult(true, null);
//             }
//             throw new Exception("Payment declined");
//         }
//         catch (Exception ex) when (attempt < maxRetries)
//         {
//             var backoff = BaseBackoffMs * (int)Math.Pow(2, attempt - 1);
//             Console.WriteLine($"[Retry] Attempt {attempt} failed, retrying in {backoff}ms");
//             await Task.Delay(backoff);
//         }
//     }
//     throw new Exception("Max retries exceeded");
// }