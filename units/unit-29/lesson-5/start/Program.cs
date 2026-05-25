// Surgewave Bootcamp - Unit 11, Lesson 5: Microservices Communication
//
// In this lesson, you'll implement microservices patterns with:
// - Saga pattern for distributed transactions
// - Service coordination via events
// - Compensation for failure handling
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 11.5 - Microservices Communication ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// ============== State ==============
var sagaStates = new Dictionary<string, SagaState>();
var inventory = new Dictionary<string, int>
{
    ["PROD-001"] = 100,
    ["PROD-002"] = 50,
    ["PROD-003"] = 5 // Low stock
};
var orders = new Dictionary<string, Order>();

// ============== Step 1: Setup Producers ==============
Console.WriteLine("--- Step 1: Setting Up Services ---");

// TODO: Create producers for each topic
// Hint: await using var sagaEventProducer = new SurgewaveProducer<byte[], byte[]>(...);
// Topics needed: saga-events, inventory-commands, payment-commands, order-commands

Console.WriteLine("Services set up!");
Console.WriteLine();

// ============== Step 2: Saga Orchestrator ==============
Console.WriteLine("--- Step 2: Starting Sagas ---");

// TODO: Implement StartSaga function
// Hint: async Task StartSaga(Order order)
// - Create new saga state
// - Emit SagaStarted event
// - Send ReserveInventory command

// TODO: Create test orders and start sagas
// Hint: var testOrders = new[]
// {
//     new Order("ORD-001", "CUST-001", new List<OrderItem> { new("PROD-001", 2, 49.99m) }, 99.98m),
//     new Order("ORD-002", "CUST-002", new List<OrderItem> { new("PROD-002", 1, 29.99m) }, 29.99m),
//     new Order("ORD-003", "CUST-003", new List<OrderItem> { new("PROD-003", 10, 9.99m) }, 99.90m), // Will fail - not enough stock
// };

Console.WriteLine("Sagas started!");
Console.WriteLine();

// ============== Step 3: Inventory Service ==============
Console.WriteLine("--- Step 3: Processing Inventory Commands ---");

// TODO: Create consumer for inventory-commands
// TODO: Handle ReserveInventory and ReleaseInventory commands

Console.WriteLine("Inventory commands processed!");
Console.WriteLine();

// ============== Step 4: Payment Service ==============
Console.WriteLine("--- Step 4: Processing Payment Commands ---");

// TODO: Create consumer for payment-commands
// TODO: Handle ProcessPayment and RefundPayment commands

Console.WriteLine("Payment commands processed!");
Console.WriteLine();

// ============== Step 5: Saga Event Handler ==============
Console.WriteLine("--- Step 5: Processing Saga Events ---");

// TODO: Create consumer for saga-events
// TODO: Handle saga events and coordinate next steps

Console.WriteLine("Saga events processed!");
Console.WriteLine();

// ============== Step 6: Results ==============
Console.WriteLine("--- Step 6: Saga Results ---");

// TODO: Display final saga states

Console.WriteLine();
Console.WriteLine("Microservices Communication complete!");

// ============== Domain Models ==============

// Saga state
public record SagaState(
    string SagaId,
    string OrderId,
    string Status,
    List<string> CompletedSteps,
    string? FailureReason,
    DateTime StartedAt,
    DateTime? CompletedAt);

// Order types
public record OrderItem(string ProductId, int Quantity, decimal Price);
public record Order(string OrderId, string CustomerId, List<OrderItem> Items, decimal Total);

// Commands
public record CreateOrderSaga(string SagaId, Order Order);
public record ReserveInventory(string SagaId, string OrderId, List<OrderItem> Items);
public record ProcessPayment(string SagaId, string OrderId, string CustomerId, decimal Amount);
public record ConfirmOrder(string SagaId, string OrderId);
public record ReleaseInventory(string SagaId, string OrderId, List<OrderItem> Items);
public record RefundPayment(string SagaId, string OrderId, decimal Amount);
public record CancelOrder(string SagaId, string OrderId, string Reason);

// Events
public record SagaStarted(string SagaId, string OrderId, DateTime Timestamp);
public record InventoryReserved(string SagaId, string OrderId);
public record InventoryReservationFailed(string SagaId, string OrderId, string Reason);
public record PaymentProcessed(string SagaId, string OrderId);
public record PaymentFailed(string SagaId, string OrderId, string Reason);
public record OrderConfirmed(string SagaId, string OrderId);
public record SagaCompleted(string SagaId, string OrderId, bool Success, string? Reason);
