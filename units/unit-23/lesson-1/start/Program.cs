// Surgewave Bootcamp - Unit 14, Lesson 1: Docker Basics
//
// In this lesson, you'll learn:
// - Configure Surgewave apps via environment variables
// - Handle graceful shutdown for containers
// - Understand Docker build and run patterns
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

Console.WriteLine("=== Surgewave Bootcamp: Lesson 14.1 - Docker Basics ===");
Console.WriteLine();

// TODO: Step 1 - Read configuration from environment variables
// var bootstrapServers = Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS")
//     ?? "localhost:9092";
// var topic = Environment.GetEnvironmentVariable("Surgewave_TOPIC")
//     ?? "docker-test";

// TODO: Step 2 - Print configuration for debugging
// Console.WriteLine("Configuration:");
// Console.WriteLine($"  Bootstrap Servers: {bootstrapServers}");
// Console.WriteLine($"  Topic: {topic}");
// Console.WriteLine();

// TODO: Step 3 - Set up graceful shutdown handling
// var cts = new CancellationTokenSource();
//
// Console.CancelKeyPress += (_, e) =>
// {
//     Console.WriteLine("Received Ctrl+C, shutting down...");
//     e.Cancel = true;
//     cts.Cancel();
// };
//
// AppDomain.CurrentDomain.ProcessExit += (_, _) =>
// {
//     Console.WriteLine("Received SIGTERM, shutting down...");
//     cts.Cancel();
// };

// TODO: Step 4 - Create a producer with environment configuration
// await using var producer = new SurgewaveProducer<string, string>(options =>
// {
//     options.BootstrapServers = bootstrapServers;
// });

// TODO: Step 5 - Send messages until shutdown is requested
// Console.WriteLine("Sending test messages (press Ctrl+C to stop)...");
// var messageCount = 0;
// while (!cts.Token.IsCancellationRequested)
// {
//     messageCount++;
//     var key = $"key-{messageCount}";
//     var value = $"Message {messageCount} at {DateTime.UtcNow:O}";
//
//     await producer.ProduceAsync(topic, key, value);
//     Console.WriteLine($"Sent message {messageCount} to {topic}");
//
//     try
//     {
//         await Task.Delay(1000, cts.Token);
//     }
//     catch (OperationCanceledException)
//     {
//         break;
//     }
// }

// TODO: Step 6 - Clean up and exit gracefully
// Console.WriteLine("Producer disposed, exiting gracefully.");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use environment variables for configuration");
Console.WriteLine("2. Handle SIGTERM for graceful container shutdown");
Console.WriteLine("3. Multi-stage builds reduce image size");
Console.WriteLine("4. .dockerignore speeds up builds");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
