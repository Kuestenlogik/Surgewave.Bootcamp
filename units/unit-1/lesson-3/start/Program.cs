// Surgewave Bootcamp - Unit 1, Lesson 4: Consumer Groups
//
// In this lesson, you'll learn to:
// - Configure a consumer with a group ID
// - Join a consumer group and receive partition assignments
// - Commit offsets for reliable processing
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

// TODO: Add the necessary using statements
// Hint: You need Kuestenlogik.Surgewave.Broker, Kuestenlogik.Surgewave.Runtime, Kuestenlogik.Surgewave.Client, and Kuestenlogik.Surgewave.Client.Consumer

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.4 - Consumer Groups ===");
Console.WriteLine();

// Step 1 - Start an embedded Surgewave broker
Console.WriteLine("Starting embedded Surgewave broker...");

// TODO: Start the embedded broker (same as previous lessons)

// Your code here...

Console.WriteLine("Broker started on localhost:9092");
Console.WriteLine();

// Step 2 - Produce some orders
Console.WriteLine("Producing messages to 'orders' topic...");

// TODO: Create a producer and produce 10 orders to the "orders" topic
// Hint: Loop from 0 to 9, create order IDs like "ORD-0", "ORD-1", etc.

// Your code here...

Console.WriteLine("  Produced 10 orders");
Console.WriteLine();

// TODO: Step 3 - Create a consumer with group ID
// Hint: Set options.GroupId = "order-processors"
// Also set EnableAutoCommit = false for manual commits
Console.WriteLine("Creating consumer with group 'order-processors'...");

// Your code here...

// TODO: Add event handlers for PartitionsAssigned and PartitionsRevoked

// TODO: Join the consumer group using SubscribeAsync
Console.WriteLine("Joining consumer group...");

// Your code here...

Console.WriteLine("Consumer ready!");
Console.WriteLine();

// TODO: Step 4 - Process messages
// Hint: Use a while loop to consume 10 messages
Console.WriteLine("Processing orders...");

// Your code here...

Console.WriteLine();

// TODO: Step 5 - Commit offsets
// Hint: Use consumer.CommitAsync()
Console.WriteLine("Committing offsets...");

// Your code here...

Console.WriteLine("Offsets committed successfully!");
Console.WriteLine();

// Step 6 - Clean shutdown
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");
