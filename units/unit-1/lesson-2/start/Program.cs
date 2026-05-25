// Surgewave Bootcamp - Unit 1, Lesson 3: Consuming Messages
//
// In this lesson, you'll learn to:
// - Create a Surgewave consumer
// - Subscribe to topics
// - Consume messages with offset tracking
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

// TODO: Add the necessary using statements
// Hint: You need Kuestenlogik.Surgewave.Broker, Kuestenlogik.Surgewave.Runtime, Kuestenlogik.Surgewave.Client, and Kuestenlogik.Surgewave.Client.Consumer

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.3 - Consuming Messages ===");
Console.WriteLine();

// Step 1 - Start an embedded Surgewave broker
Console.WriteLine("Starting embedded Surgewave broker...");

// TODO: Start the embedded broker (same as previous lessons)

// Your code here...

Console.WriteLine("Broker started on localhost:9092");
Console.WriteLine();

// Step 2 - First, produce some messages
Console.WriteLine("Producing messages first...");

// TODO: Create a producer and produce 3 messages to "greetings" topic
// Hint: Use SurgewaveProducer<string, string>

// Your code here...

Console.WriteLine();

// TODO: Step 3 - Create a consumer
// Hint: Use new SurgewaveConsumer<string, string>(options => { ... })
// Set AutoOffsetReset = AutoOffsetReset.Earliest to read from beginning
Console.WriteLine("Creating consumer...");

// Your code here...

// TODO: Subscribe to the "greetings" topic
// Hint: consumer.Subscribe("greetings")

Console.WriteLine("Consumer ready!");
Console.WriteLine();

// TODO: Step 4 - Consume messages
// Hint: Use a while loop with consumer.ConsumeAsync(timeout)
Console.WriteLine("Consuming messages...");

// Your code here - consume the 3 messages and print them

Console.WriteLine();
Console.WriteLine("Consumed messages from 'greetings' topic");
Console.WriteLine();

// Step 5 - Clean shutdown
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");
