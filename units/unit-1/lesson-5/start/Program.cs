// Surgewave Bootcamp - Unit 1, Lesson 5: Batching & Message Sizing
//
// In this lesson, you'll learn to:
// - Configure producer batching for optimal throughput
// - Choose appropriate message sizes
// - Decide when to flush vs let batches fill
// - Configure consumer batch fetching
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

// TODO: Add the necessary using statements
// Hint: You need System.Diagnostics, Kuestenlogik.Surgewave.Broker, Kuestenlogik.Surgewave.Runtime, Kuestenlogik.Surgewave.Client, Kuestenlogik.Surgewave.Client.Consumer

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.5 - Batching & Message Sizing ===");
Console.WriteLine();

// Step 1 - Start an embedded Surgewave broker
Console.WriteLine("Starting embedded Surgewave broker...");

// TODO: Start the embedded broker with WithAutoCreateTopics(true)

// Your code here...

Console.WriteLine("Broker started on localhost:9092");
Console.WriteLine();

// TODO: Step 2 - Test 1: No batching (baseline)
// Hint: Create a producer with BatchSize = 1 and LingerMs = 0
// Produce 1000 messages and measure time with Stopwatch
Console.WriteLine("--- Test 1: No Batching (Baseline) ---");
Console.WriteLine("Producing 1000 messages without batching...");

// Your code here...

Console.WriteLine();

// TODO: Step 3 - Test 2: With batching
// Hint: Create a producer with BatchSize = 65536 and LingerMs = 5
// Produce 1000 messages and compare performance
Console.WriteLine("--- Test 2: With Batching ---");
Console.WriteLine("Producing 1000 messages with batching...");

// Your code here...

Console.WriteLine();

// TODO: Step 4 - Test 3: Message size impact
// Hint: Test with small (100 bytes), medium (10KB), and large (100KB) messages
Console.WriteLine("--- Test 3: Message Size Impact ---");

// Your code here...

Console.WriteLine();

// TODO: Step 5 - Test 4: Flush strategies
// Hint: Compare explicit flush vs waiting for linger timeout
Console.WriteLine("--- Test 4: Flush Strategies ---");

// Your code here...

Console.WriteLine();

// TODO: Step 6 - Test 5: Consumer batch fetching
// Hint: Configure consumer with FetchMinBytes and FetchMaxWaitMs
Console.WriteLine("--- Test 5: Consumer Batch Fetching ---");
Console.WriteLine("Consuming 1000 messages...");

// Your code here...

Console.WriteLine();

// Summary
Console.WriteLine("Summary:");
Console.WriteLine("  - Batching improved throughput by ~Xx");
Console.WriteLine("  - Optimal message size: 1KB-100KB");
Console.WriteLine("  - Use explicit flush for time-critical batches");
Console.WriteLine();

// Step 7 - Clean shutdown
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");
