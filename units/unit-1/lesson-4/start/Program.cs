// Surgewave Bootcamp - Unit 1, Lesson 5: Partitioning
//
// In this lesson, you'll learn to:
// - Create topics with multiple partitions
// - Understand how keys determine partition assignment
// - Observe message distribution across partitions
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

// TODO: Add the necessary using statements
// Hint: You need Kuestenlogik.Surgewave.Broker, Kuestenlogik.Surgewave.Runtime, Kuestenlogik.Surgewave.Client, Kuestenlogik.Surgewave.Client.Consumer, and Kuestenlogik.Surgewave.Client.Native

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.5 - Partitioning ===");
Console.WriteLine();

// Step 1 - Start an embedded Surgewave broker
Console.WriteLine("Starting embedded Surgewave broker...");

// TODO: Start the embedded broker with WithAutoCreateTopics(false)
// We'll create the topic manually with specific partition count

// Your code here...

Console.WriteLine("Broker started on localhost:9092");
Console.WriteLine();

// TODO: Step 2 - Create a topic with multiple partitions
// Hint: Use SurgewaveNativeClient to create a topic with 3 partitions
Console.WriteLine("Creating topic 'user-events' with 3 partitions...");

// Your code here...

Console.WriteLine("Topic created!");
Console.WriteLine();

// TODO: Step 3 - Produce messages with keys
// Hint: Produce events for different users (user-1, user-2, user-3)
// Observe that same keys go to same partitions
Console.WriteLine("Producing user events...");

// Your code here - produce at least 10 events

Console.WriteLine();

// TODO: Step 4 - List topic details
// Hint: Use client.Topics.ListAsync() to see partition info
Console.WriteLine("Listing topic partitions...");

// Your code here...

Console.WriteLine();

// TODO: Step 5 - Consume from all partitions
// Hint: Subscribe and consume, tracking which partition each message came from
Console.WriteLine("Consuming from all partitions...");

// Your code here...

Console.WriteLine();
Console.WriteLine("Message distribution by partition:");
// Your code here - print partition counts

Console.WriteLine();

// Step 6 - Clean shutdown
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");
