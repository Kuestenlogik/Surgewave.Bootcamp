// Surgewave Bootcamp - Unit 14, Lesson 1: Docker Basics
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Client;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 14.1 - Docker Basics ===");
Console.WriteLine();

// Step 1 - Read configuration from environment variables
var bootstrapServers = Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS")
    ?? "localhost:9092";
var topic = Environment.GetEnvironmentVariable("Surgewave_TOPIC")
    ?? "docker-test";
var messageInterval = int.TryParse(
    Environment.GetEnvironmentVariable("Surgewave_MESSAGE_INTERVAL_MS"),
    out var interval) ? interval : 1000;

// Step 2 - Print configuration for debugging
Console.WriteLine("Configuration:");
Console.WriteLine($"  Bootstrap Servers: {bootstrapServers}");
Console.WriteLine($"  Topic: {topic}");
Console.WriteLine($"  Message Interval: {messageInterval}ms");
Console.WriteLine();

// Step 3 - Set up graceful shutdown handling
var cts = new CancellationTokenSource();
var shutdownRequested = false;

Console.CancelKeyPress += (_, e) =>
{
    if (!shutdownRequested)
    {
        Console.WriteLine();
        Console.WriteLine("Received Ctrl+C, shutting down...");
        shutdownRequested = true;
        e.Cancel = true;
        cts.Cancel();
    }
};

AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    if (!shutdownRequested)
    {
        Console.WriteLine("Received SIGTERM, shutting down...");
        shutdownRequested = true;
        cts.Cancel();
    }
};

// Step 4 - Create a producer with environment configuration
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = bootstrapServers;
});

// Step 5 - Send messages until shutdown is requested
Console.WriteLine("Sending test messages (press Ctrl+C to stop)...");
var messageCount = 0;

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        messageCount++;
        var key = $"key-{messageCount}";
        var value = $"Message {messageCount} at {DateTime.UtcNow:O}";

        await producer.ProduceAsync(topic, key, value);
        Console.WriteLine($"Sent message {messageCount} to {topic}");

        await Task.Delay(messageInterval, cts.Token);
    }
}
catch (OperationCanceledException)
{
    // Expected when shutdown is requested
}

// Step 6 - Clean up and exit gracefully
Console.WriteLine($"Sent {messageCount} messages total.");
Console.WriteLine("Producer disposed, exiting gracefully.");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use environment variables for configuration");
Console.WriteLine("2. Handle SIGTERM for graceful container shutdown");
Console.WriteLine("3. Multi-stage builds reduce image size");
Console.WriteLine("4. .dockerignore speeds up builds");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
