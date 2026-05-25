// Unit 11 Project: E-Commerce Backend - COMPLETED
// A complete Order -> Payment -> Shipping event flow

using System.Collections.Concurrent;
using System.Text.Json;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== E-Commerce Event Processing System ===\n");

// Analytics counters
var analyticsCounters = new ConcurrentDictionary<string, int>();
var processedOrders = new ConcurrentDictionary<string, OrderCreated>();
var approvedPayments = new ConcurrentDictionary<string, PaymentProcessed>();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;

// Step 1: Create producers and consumers
Console.WriteLine("[Setup] Creating producers and consumers...");

await using var orderProducer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
await using var paymentProducer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
await using var shipmentProducer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
await using var analyticsProducer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);

await using var orderConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "payment-service";
    opt.AutoOffsetReset = Kuestenlogik.Surgewave.Client.Consumer.AutoOffsetReset.Earliest;
});

await using var paymentConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "shipping-service";
    opt.AutoOffsetReset = Kuestenlogik.Surgewave.Client.Consumer.AutoOffsetReset.Earliest;
});

orderConsumer.Subscribe("orders-created");
paymentConsumer.Subscribe("payments-processed");

Console.WriteLine("[Setup] Producers and consumers created\n");

// Customer addresses for shipping
var customerAddresses = new Dictionary<string, string>
{
    ["C-123"] = "123 Main St, Seattle, WA 98101",
    ["C-456"] = "456 Oak Ave, Portland, OR 97201",
    ["C-789"] = "789 Pine Rd, San Francisco, CA 94102"
};

// Sample orders to process
var orders = new List<OrderCreated>
{
    new("ORD-001", "C-123", new() { new("P1", "Laptop Stand", 1, 299.99m) }, 299.99m, DateTime.UtcNow, Guid.NewGuid().ToString()),
    new("ORD-002", "C-456", new() { new("P2", "Gaming PC", 1, 1250.00m) }, 1250.00m, DateTime.UtcNow, Guid.NewGuid().ToString()),
    new("ORD-003", "C-789", new() { new("P3", "USB Cable", 2, 24.99m) }, 49.99m, DateTime.UtcNow, Guid.NewGuid().ToString()),
    new("ORD-004", "C-123", new() { new("P4", "Monitor", 1, 450.00m) }, 450.00m, DateTime.UtcNow, Guid.NewGuid().ToString()),
    new("ORD-005", "C-456", new() { new("P5", "Keyboard", 1, 89.99m) }, 89.99m, DateTime.UtcNow, Guid.NewGuid().ToString()),
};

// Step 2: Order Service - Produce OrderCreated events
Console.WriteLine("[Order Service] Starting...");

foreach (var order in orders)
{
    var json = JsonSerializer.Serialize(order);

    // Produce to orders-created topic
    await orderProducer.ProduceAsync("orders-created", order.OrderId, json);

    // Also send to analytics
    await analyticsProducer.ProduceAsync("events-analytics", order.CorrelationId,
        JsonSerializer.Serialize(new { Type = "OrderCreated", Data = order }));

    analyticsCounters.AddOrUpdate("orders", 1, (_, c) => c + 1);

    Console.WriteLine($"[Order Service] Order {order.OrderId} created for customer {order.CustomerId} (${order.TotalAmount:F2})");
}

Console.WriteLine($"[Order Service] Produced {orders.Count} orders\n");

// Step 3: Payment Service - Process orders and create payments
Console.WriteLine("[Payment Service] Starting...");

var paymentCounter = 0;
using var orderCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    while (!orderCts.IsCancellationRequested)
    {
        var result = await orderConsumer.ConsumeAsync(orderCts.Token);
        if (result is null) continue;

        var order = JsonSerializer.Deserialize<OrderCreated>(result.Value);
        if (order is null) continue;

        processedOrders[order.OrderId] = order;
        paymentCounter++;
        var paymentId = $"PAY-{paymentCounter:D3}";

        // Simulate payment processing delay
        await Task.Delay(100);

        Console.WriteLine($"[Payment Service] Processing payment for Order {order.OrderId}...");

        // Simulate: orders > $1000 get declined (insufficient funds)
        var paymentSuccess = order.TotalAmount <= 1000;
        var status = paymentSuccess ? PaymentStatus.Approved : PaymentStatus.Declined;

        var payment = new PaymentProcessed(
            paymentId,
            order.OrderId,
            order.TotalAmount,
            status,
            DateTime.UtcNow,
            order.CorrelationId
        );

        var paymentJson = JsonSerializer.Serialize(payment);

        if (paymentSuccess)
        {
            await paymentProducer.ProduceAsync("payments-processed", order.OrderId, paymentJson);

            approvedPayments[order.OrderId] = payment;
            analyticsCounters.AddOrUpdate("payments_approved", 1, (_, c) => c + 1);

            Console.WriteLine($"[Payment Service] Payment {paymentId} APPROVED for Order {order.OrderId} (${order.TotalAmount:F2})");
        }
        else
        {
            // Send to dead letter queue for retry
            await paymentProducer.ProduceAsync("payments-failed", order.OrderId, paymentJson);

            analyticsCounters.AddOrUpdate("payments_declined", 1, (_, c) => c + 1);

            Console.WriteLine($"[Payment Service] Payment {paymentId} DECLINED for Order {order.OrderId} - Insufficient funds");
            Console.WriteLine($"[Payment Service] Order {order.OrderId} sent to retry queue");
        }

        // Send to analytics
        await analyticsProducer.ProduceAsync("events-analytics", payment.CorrelationId,
            JsonSerializer.Serialize(new { Type = "PaymentProcessed", Data = payment }));

        if (paymentCounter >= orders.Count) break;
    }
}
catch (OperationCanceledException)
{
    // Expected timeout
}

Console.WriteLine();

// Step 4: Shipping Service - Create shipments for approved payments
Console.WriteLine("[Shipping Service] Starting...");

var shipmentCounter = 0;
using var paymentCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    while (!paymentCts.IsCancellationRequested)
    {
        var result = await paymentConsumer.ConsumeAsync(paymentCts.Token);
        if (result is null) continue;

        var payment = JsonSerializer.Deserialize<PaymentProcessed>(result.Value);
        if (payment is null || payment.Status != PaymentStatus.Approved) continue;

        shipmentCounter++;
        var shipmentId = $"SHIP-{shipmentCounter:D3}";

        // Get customer address
        var order = processedOrders.GetValueOrDefault(payment.OrderId);
        var address = order != null
            ? customerAddresses.GetValueOrDefault(order.CustomerId, "Unknown Address")
            : "Unknown Address";

        // Calculate estimated delivery (3-5 business days)
        var estimatedDelivery = DateTime.UtcNow.AddDays(Random.Shared.Next(3, 6));

        var shipment = new ShipmentCreated(
            shipmentId,
            payment.OrderId,
            address,
            estimatedDelivery,
            payment.CorrelationId
        );

        Console.WriteLine($"[Shipping Service] Creating shipment for Order {payment.OrderId}...");

        await shipmentProducer.ProduceAsync("shipments-created", payment.OrderId, JsonSerializer.Serialize(shipment));

        // Send to analytics
        await analyticsProducer.ProduceAsync("events-analytics", shipment.CorrelationId,
            JsonSerializer.Serialize(new { Type = "ShipmentCreated", Data = shipment }));

        analyticsCounters.AddOrUpdate("shipments", 1, (_, c) => c + 1);

        Console.WriteLine($"[Shipping Service] Shipment {shipmentId} created - ETA: {estimatedDelivery:yyyy-MM-dd}");
        Console.WriteLine($"[Shipping Service] Shipping to: {address}");

        if (shipmentCounter >= approvedPayments.Count) break;
    }
}
catch (OperationCanceledException)
{
    // Expected timeout
}

Console.WriteLine();

// Step 5: Analytics Dashboard
Console.WriteLine(new string('=', 50));
Console.WriteLine("[Analytics] E-Commerce Event Summary");
Console.WriteLine(new string('=', 50));
Console.WriteLine($"[Analytics] Total Orders Created:    {analyticsCounters.GetValueOrDefault("orders", 0)}");
Console.WriteLine($"[Analytics] Payments Approved:       {analyticsCounters.GetValueOrDefault("payments_approved", 0)}");
Console.WriteLine($"[Analytics] Payments Declined:       {analyticsCounters.GetValueOrDefault("payments_declined", 0)}");
Console.WriteLine($"[Analytics] Shipments Created:       {analyticsCounters.GetValueOrDefault("shipments", 0)}");
Console.WriteLine();

var totalRevenue = approvedPayments.Values.Sum(p => p.Amount);
Console.WriteLine($"[Analytics] Total Revenue:           ${totalRevenue:F2}");
Console.WriteLine($"[Analytics] Avg Order Value:         ${(totalRevenue / Math.Max(1, approvedPayments.Count)):F2}");
Console.WriteLine(new string('=', 50));

Console.WriteLine("\n=== E-Commerce Event Processing Complete ===");

// Event types
public record OrderCreated(
    string OrderId,
    string CustomerId,
    List<OrderItem> Items,
    decimal TotalAmount,
    DateTime CreatedAt,
    string CorrelationId
);

public record PaymentProcessed(
    string PaymentId,
    string OrderId,
    decimal Amount,
    PaymentStatus Status,
    DateTime ProcessedAt,
    string CorrelationId
);

public record ShipmentCreated(
    string ShipmentId,
    string OrderId,
    string Address,
    DateTime EstimatedDelivery,
    string CorrelationId
);

public record OrderItem(string ProductId, string Name, int Quantity, decimal Price);

public enum PaymentStatus { Approved, Declined, Pending }