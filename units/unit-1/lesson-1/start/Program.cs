// Surgewave Bootcamp - Unit 1, Lesson 2: Producing Messages
//
// In this lesson, you'll learn to:
// - Create a Surgewave producer
// - Produce string messages with keys
// - Understand delivery confirmation via ProduceResult
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

// TODO: Add the necessary using statements
// Hint: You need Kuestenlogik.Surgewave.Broker, Kuestenlogik.Surgewave.Runtime, and Kuestenlogik.Surgewave.Client

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.2 - Producing Messages ===");
Console.WriteLine();

// Step 1 - Start an embedded Surgewave broker (same as Lesson 1.1)
Console.WriteLine("Starting embedded Surgewave broker...");

// TODO: Start the embedded broker
// Hint: Use SurgewaveRuntime.CreateBuilder() with WithPort(0), WithStorageEngine(StorageEngines.Memory), WithAutoCreateTopics(true)

// Your code here...

Console.WriteLine("Broker started on localhost:9092");
Console.WriteLine();

// TODO: Step 2 - Create a producer
// Hint: Use new SurgewaveProducer<string, string>(options => { options.BootstrapServers = ... })
Console.WriteLine("Creating producer...");

// Your code here...

Console.WriteLine("Producer ready!");
Console.WriteLine();

// TODO: Step 3 - Produce messages
// Hint: Use producer.ProduceAsync("greetings", key, value)
Console.WriteLine("Producing messages...");

// Your code here - produce at least 3 messages with different keys

Console.WriteLine();
Console.WriteLine("Produced messages to 'greetings' topic");
Console.WriteLine();

// Step 4 - Clean shutdown
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");
