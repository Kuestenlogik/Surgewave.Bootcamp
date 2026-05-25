// Surgewave Bootcamp - Unit 1, Lesson 6: Message Best Practices
//
// In this lesson, you'll learn to:
// - Design immutable messages using C# records
// - Distinguish between events and commands
// - Use correlation IDs for distributed tracing
// - Apply idempotency patterns
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

// TODO: Add the necessary using statements
// Hint: You need Kuestenlogik.Surgewave.Broker, Kuestenlogik.Surgewave.Runtime, Kuestenlogik.Surgewave.Client, Kuestenlogik.Surgewave.Client.Consumer, and System.Text.Json

// TODO: Define message types as records
// Commands (requests for action - imperative naming):
//   - PlaceOrder(CustomerId, Items, CorrelationId)
//   - OrderItem(ProductId, Quantity, UnitPrice)
//
// Events (facts about what happened - past tense naming):
//   - OrderPlaced(MessageId, OrderId, CustomerId, TotalAmount, Items, PlacedAt, CorrelationId)
//   - PaymentProcessed(MessageId, PaymentId, OrderId, Amount, ProcessedAt, CorrelationId)
//   - InventoryReserved(MessageId, ReservationId, ProductId, Quantity, ReservedAt, CorrelationId)

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.6 - Message Best Practices ===");
Console.WriteLine();

// Step 1 - Start an embedded Surgewave broker
Console.WriteLine("Starting embedded Surgewave broker...");

// TODO: Start the embedded broker with:
//   .WithPort(0)
//   .WithStorageMode(StorageMode.Memory)
//   .WithAutoCreateTopics(true)

// Your code here...

Console.WriteLine("Broker started on localhost:9092");
Console.WriteLine();

// Step 2 - Demonstrate Events vs Commands
Console.WriteLine("--- Demonstrating Events vs Commands ---");
Console.WriteLine();

// TODO: Create a producer for string keys and values
// TODO: Generate a correlationId (Guid.NewGuid())
// TODO: Create a PlaceOrder command and publish to "commands" topic
// TODO: Create an OrderPlaced event with the SAME correlationId and publish to "events" topic

Console.WriteLine("Publishing command: PlaceOrder");
// Your code here - print correlation ID and customer ID

Console.WriteLine();
Console.WriteLine("Order placed! Publishing event: OrderPlaced");
// Your code here - print correlation ID (should be same) and order ID

Console.WriteLine();

// Step 3 - Demonstrate Immutability
Console.WriteLine("--- Demonstrating Immutability ---");
Console.WriteLine();

// TODO: Create an OrderPlaced event
// TODO: Try to "modify" it using 'with' expression
// TODO: Print both totals to show they're different instances

Console.WriteLine("Attempting to modify a record...");
Console.WriteLine("Records are immutable - you get a new instance with 'with'");
// Your code here - print original and modified totals

Console.WriteLine();

// Step 4 - Demonstrate Correlation ID Tracing
Console.WriteLine("--- Demonstrating Correlation ID Tracing ---");
Console.WriteLine();

// TODO: Create a correlation ID that represents a single business transaction
// TODO: Create PaymentProcessed and InventoryReserved events with the same correlation ID
// TODO: Publish both events

Console.WriteLine("Simulating order flow with CorrelationId: ...");
Console.WriteLine("  1. PlaceOrder command received");
Console.WriteLine("  2. OrderPlaced event published");
Console.WriteLine("  3. PaymentProcessed event published");
Console.WriteLine("  4. InventoryReserved event published");
Console.WriteLine("  All events share the same CorrelationId!");
Console.WriteLine();

// Step 5 - Demonstrate Idempotent Processing
Console.WriteLine("--- Demonstrating Idempotent Processing ---");
Console.WriteLine();

// TODO: Create a HashSet<string> to track processed orders
// TODO: Create a method that checks if an order was already processed
// TODO: Call it multiple times with the same order ID to demonstrate idempotency

// Your code here...

Console.WriteLine("Idempotency prevents duplicate processing!");
Console.WriteLine();

// Step 6 - Clean shutdown
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");
