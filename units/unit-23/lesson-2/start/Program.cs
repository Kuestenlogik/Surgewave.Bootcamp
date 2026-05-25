// Surgewave Bootcamp - Unit 14, Lesson 2: Docker Compose
//
// In this lesson, you'll learn:
// - Configure Surgewave for multi-container environments
// - Handle service discovery via Docker DNS
// - Implement retry logic for startup ordering
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

Console.WriteLine("=== Surgewave Bootcamp: Lesson 14.2 - Docker Compose ===");
Console.WriteLine();

// TODO: Step 1 - Read multi-broker configuration from environment
// var bootstrapServers = Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS")
//     ?? "surgewave-broker-1:9092,surgewave-broker-2:9092";
// var topic = Environment.GetEnvironmentVariable("Surgewave_TOPIC")
//     ?? "compose-test";

// TODO: Step 2 - Configure retry settings
// var maxRetries = int.TryParse(
//     Environment.GetEnvironmentVariable("Surgewave_MAX_RETRIES"),
//     out var retries) ? retries : 30;
// var retryDelayMs = int.TryParse(
//     Environment.GetEnvironmentVariable("Surgewave_RETRY_DELAY_MS"),
//     out var delay) ? delay : 2000;

// TODO: Step 3 - Print configuration
// Console.WriteLine("Configuration:");
// Console.WriteLine($"  Bootstrap Servers: {bootstrapServers}");
// Console.WriteLine($"  Topic: {topic}");
// Console.WriteLine($"  Retry Count: {maxRetries}");
// Console.WriteLine($"  Retry Delay: {retryDelayMs}ms");
// Console.WriteLine();

// TODO: Step 4 - Set up graceful shutdown
// var cts = new CancellationTokenSource();
// Console.CancelKeyPress += (_, e) =>
// {
//     e.Cancel = true;
//     cts.Cancel();
// };

// TODO: Step 5 - Implement broker availability wait
// async Task<bool> WaitForBrokersAsync()
// {
//     Console.WriteLine("Waiting for brokers...");
//     for (int i = 0; i < maxRetries && !cts.Token.IsCancellationRequested; i++)
//     {
//         Console.WriteLine($"  Attempt {i + 1}/{maxRetries}: Connecting...");
//         try
//         {
//             // Attempt to create producer (validates connection)
//             await using var testProducer = new SurgewaveProducer<string, string>(options =>
//             {
//                 options.BootstrapServers = bootstrapServers;
//             });
//             Console.WriteLine("Successfully connected to brokers!");
//             return true;
//         }
//         catch
//         {
//             await Task.Delay(retryDelayMs, cts.Token);
//         }
//     }
//     return false;
// }

// TODO: Step 6 - Wait for brokers before starting
// if (!await WaitForBrokersAsync())
// {
//     Console.WriteLine("Failed to connect to brokers after {maxRetries} attempts.");
//     return;
// }

// TODO: Step 7 - Create producer and start sending messages
// await using var producer = new SurgewaveProducer<string, string>(options =>
// {
//     options.BootstrapServers = bootstrapServers;
// });
//
// Console.WriteLine("Starting message producer...");
// var messageCount = 0;
// while (!cts.Token.IsCancellationRequested)
// {
//     messageCount++;
//     await producer.ProduceAsync(topic, $"key-{messageCount}", $"Message {messageCount}");
//     Console.WriteLine($"Sent message {messageCount} to {topic}");
//     try { await Task.Delay(1000, cts.Token); } catch (OperationCanceledException) { break; }
// }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use service names for Docker DNS resolution");
Console.WriteLine("2. Implement retry logic for startup ordering");
Console.WriteLine("3. Use health checks for proper dependency ordering");
Console.WriteLine("4. Named volumes persist data across restarts");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
