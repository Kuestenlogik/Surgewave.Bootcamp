// Surgewave Bootcamp - Unit 11, Lesson 1: E-Commerce Pipeline
//
// In this lesson, you'll build a complete e-commerce event pipeline with:
// - Order processing
// - Inventory management
// - Customer notifications
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 11.1 - E-Commerce Pipeline ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// ============== Domain Models ==============

// TODO: Define domain models
// Hint: public record Order(string OrderId, string CustomerId, List<OrderItem> Items, decimal Total);
// Hint: public record OrderItem(string ProductId, string Name, int Quantity, decimal Price);
// Hint: public record OrderEvent(string OrderId, string EventType, string Details, DateTime Timestamp);
// Hint: public record InventoryUpdate(string ProductId, int QuantityChange, string Reason);
// Hint: public record Notification(string CustomerId, string Type, string Message);

// ============== Shared State ==============
var orderEvents = new List<OrderEvent>();
var inventory = new Dictionary<string, int>
{
    ["PROD-001"] = 100,
    ["PROD-002"] = 50,
    ["PROD-003"] = 25
};
var notifications = new List<Notification>();

// ============== Step 1: Order Service (Producer) ==============
Console.WriteLine("--- Step 1: Placing Orders ---");

// TODO: Create a producer for orders
// Hint: await using var orderProducer = new SurgewaveProducer<byte[], byte[]>(options =>
// {
//     options.BootstrapServers = $"localhost:{runtime.Port}";
// });

// TODO: Create sample orders and produce them
// Hint: var order1 = new Order("ORD-001", "CUST-123",
//     new List<OrderItem> { new("PROD-001", "Widget", 2, 29.99m) }, 59.98m);
// var orderJson = JsonSerializer.Serialize(order1);
// await orderProducer.ProduceAsync("orders",
//     Encoding.UTF8.GetBytes(order1.OrderId),
//     Encoding.UTF8.GetBytes(orderJson));

Console.WriteLine("Orders placed!");
Console.WriteLine();

// ============== Step 2: Order Processor ==============
Console.WriteLine("--- Step 2: Processing Orders ---");

// TODO: Create producers for order-events, inventory-updates, and notifications topics
// TODO: Create a consumer for orders topic

// TODO: Process each order:
// 1. Consume from "orders" topic
// 2. Emit OrderPlaced event to "order-events"
// 3. Emit OrderValidated event
// 4. Emit InventoryUpdate for each item (negative quantity)
// 5. Emit Notification for customer

Console.WriteLine("Orders processed!");
Console.WriteLine();

// ============== Step 3: Inventory Service ==============
Console.WriteLine("--- Step 3: Updating Inventory ---");

// TODO: Create a consumer for inventory-updates topic
// TODO: Process each inventory update and adjust stock levels
// TODO: Check for low inventory and emit alerts if needed

Console.WriteLine("Inventory updated!");
Console.WriteLine();

// ============== Step 4: Notification Service ==============
Console.WriteLine("--- Step 4: Sending Notifications ---");

// TODO: Create a consumer for notifications topic
// TODO: Process each notification and "send" it (print to console)

Console.WriteLine("Notifications sent!");
Console.WriteLine();

// ============== Step 5: Order History ==============
Console.WriteLine("--- Step 5: Order Event History ---");

// TODO: Create a consumer for order-events topic
// TODO: Display all order events

Console.WriteLine();
Console.WriteLine("E-Commerce Pipeline complete!");

// ============== Domain Models (define at bottom of file) ==============
// TODO: Move the record definitions here

public record Order(string OrderId, string CustomerId, List<OrderItem> Items, decimal Total);
public record OrderItem(string ProductId, string Name, int Quantity, decimal Price);
public record OrderEvent(string OrderId, string EventType, string Details, DateTime Timestamp);
public record InventoryUpdate(string ProductId, int QuantityChange, string Reason);
public record Notification(string CustomerId, string Type, string Message);
