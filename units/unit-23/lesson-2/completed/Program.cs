// Surgewave Bootcamp - Unit 14, Lesson 2: Docker Compose
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Client;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 14.2 - Docker Compose ===");
Console.WriteLine();

// Step 1 - Read multi-broker configuration from environment
var bootstrapServers = Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS")
    ?? "surgewave-broker-1:9092,surgewave-broker-2:9092";
var topic = Environment.GetEnvironmentVariable("Surgewave_TOPIC")
    ?? "compose-test";

// Step 2 - Configure retry settings
var maxRetries = int.TryParse(
    Environment.GetEnvironmentVariable("Surgewave_MAX_RETRIES"),
    out var retries) ? retries : 30;
var retryDelayMs = int.TryParse(
    Environment.GetEnvironmentVariable("Surgewave_RETRY_DELAY_MS"),
    out var delay) ? delay : 2000;

// Step 3 - Print configuration
Console.WriteLine("Configuration:");
Console.WriteLine($"  Bootstrap Servers: {bootstrapServers}");
Console.WriteLine($"  Topic: {topic}");
Console.WriteLine($"  Retry Count: {maxRetries}");
Console.WriteLine($"  Retry Delay: {retryDelayMs}ms");
Console.WriteLine();

// Step 4 - Set up graceful shutdown
var cts = new CancellationTokenSource();
var shutdownRequested = false;

Console.CancelKeyPress += (_, e) =>
{
    if (!shutdownRequested)
    {
        Console.WriteLine();
        Console.WriteLine("Shutting down gracefully...");
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

// Step 5 - Implement broker availability wait
async Task<bool> WaitForBrokersAsync()
{
    Console.WriteLine("Waiting for brokers...");

    for (int i = 0; i < maxRetries && !cts.Token.IsCancellationRequested; i++)
    {
        Console.WriteLine($"  Attempt {i + 1}/{maxRetries}: Connecting...");
        try
        {
            // Attempt to create producer (validates connection)
            await using var testProducer = new SurgewaveProducer<string, string>(options =>
            {
                options.BootstrapServers = bootstrapServers;
            });

            // If we get here, connection succeeded
            Console.WriteLine("Successfully connected to brokers!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    Connection failed: {ex.Message}");

            if (i < maxRetries - 1)
            {
                try
                {
                    await Task.Delay(retryDelayMs, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
            }
        }
    }

    return false;
}

// Step 6 - Wait for brokers before starting
if (!await WaitForBrokersAsync())
{
    if (!cts.Token.IsCancellationRequested)
    {
        Console.WriteLine($"Failed to connect to brokers after {maxRetries} attempts.");
        Console.WriteLine("Please ensure brokers are running and accessible.");
    }
    return;
}

// Step 7 - Create producer and start sending messages
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = bootstrapServers;
});

Console.WriteLine();
Console.WriteLine("Starting message producer...");
var messageCount = 0;

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        messageCount++;
        var key = $"key-{messageCount}";
        var value = $"Message {messageCount} from Docker Compose at {DateTime.UtcNow:O}";

        await producer.ProduceAsync(topic, key, value);
        Console.WriteLine($"Sent message {messageCount} to {topic}");

        await Task.Delay(1000, cts.Token);
    }
}
catch (OperationCanceledException)
{
    // Expected when shutdown is requested
}

Console.WriteLine($"Sent {messageCount} messages total.");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use service names for Docker DNS resolution");
Console.WriteLine("2. Implement retry logic for startup ordering");
Console.WriteLine("3. Use health checks for proper dependency ordering");
Console.WriteLine("4. Named volumes persist data across restarts");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
