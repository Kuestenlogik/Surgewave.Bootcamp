// Surgewave Bootcamp - Unit 1, Lesson 1: Hello Surgewave
//
// In this lesson, you'll learn to:
// - Start an embedded Surgewave broker
// - Connect with a Surgewave client
// - List available topics
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

// TODO: Add the necessary using statements
// Hint: You need Kuestenlogik.Surgewave.Broker, Kuestenlogik.Surgewave.Runtime, and Kuestenlogik.Surgewave.Client.Native

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.1 - Hello Surgewave ===");
Console.WriteLine();

// TODO: Step 1 - Start an embedded Surgewave broker
// Hint: Use SurgewaveRuntime.CreateBuilder() with WithPort(0) and WithStorageMode(StorageMode.Memory)
// Don't forget await using for proper disposal!
Console.WriteLine("Starting embedded Surgewave broker...");

// Your code here...

Console.WriteLine("Broker started on localhost:9092");
Console.WriteLine();

// TODO: Step 2 - Create a Surgewave client connection
// Hint: Use new SurgewaveNativeClient("localhost", runtime.Port) and ConnectAsync()
// Don't forget await using for proper disposal!
Console.WriteLine("Connecting to Surgewave...");

// Your code here...

Console.WriteLine("Connected successfully!");
Console.WriteLine();

// TODO: Step 3 - List available topics
// Hint: Use client.Topics.ListAsync()
Console.WriteLine("Available topics:");

// Your code here...

Console.WriteLine();

// TODO: Step 4 - Clean shutdown
// The await using statements handle cleanup automatically
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");
