// Surgewave Bootcamp - Unit 14, Lesson 4: Helm Charts
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Client;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 14.4 - Helm Charts ===");
Console.WriteLine();

// Step 1 - Read Helm release information
var releaseName = Environment.GetEnvironmentVariable("HELM_RELEASE_NAME") ?? "local";
var releaseNamespace = Environment.GetEnvironmentVariable("HELM_RELEASE_NAMESPACE") ?? "default";
var chartVersion = Environment.GetEnvironmentVariable("HELM_CHART_VERSION") ?? "0.0.0";
var podName = Environment.GetEnvironmentVariable("POD_NAME") ?? "local-dev";

// Step 2 - Read configuration from Helm values (via ConfigMap)
var bootstrapServers = Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS")
    ?? "surgewave-broker:9092";
var topic = Environment.GetEnvironmentVariable("Surgewave_TOPIC")
    ?? "helm-topic";
var groupId = Environment.GetEnvironmentVariable("Surgewave_GROUP_ID")
    ?? "helm-consumers";
var maxRetries = int.TryParse(
    Environment.GetEnvironmentVariable("Surgewave_MAX_RETRIES"),
    out var retries) ? retries : 30;
var retryDelayMs = int.TryParse(
    Environment.GetEnvironmentVariable("Surgewave_RETRY_DELAY_MS"),
    out var delay) ? delay : 2000;

// Step 3 - Print release and configuration info
Console.WriteLine("Helm Release Information:");
Console.WriteLine($"  Release Name: {releaseName}");
Console.WriteLine($"  Release Namespace: {releaseNamespace}");
Console.WriteLine($"  Chart Version: {chartVersion}");
Console.WriteLine($"  Pod Name: {podName}");
Console.WriteLine();
Console.WriteLine("Configuration (from Helm values):");
Console.WriteLine($"  Bootstrap Servers: {bootstrapServers}");
Console.WriteLine($"  Topic: {topic}");
Console.WriteLine($"  Group ID: {groupId}");
Console.WriteLine($"  Max Retries: {maxRetries}");
Console.WriteLine($"  Retry Delay: {retryDelayMs}ms");
Console.WriteLine();

// Step 4 - Set up graceful shutdown
var cts = new CancellationTokenSource();
var shutdownRequested = false;

Console.CancelKeyPress += (_, e) =>
{
    if (!shutdownRequested)
    {
        shutdownRequested = true;
        Console.WriteLine();
        Console.WriteLine($"[{releaseName}] Received SIGINT, shutting down...");
        e.Cancel = true;
        cts.Cancel();
    }
};

AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    if (!shutdownRequested)
    {
        shutdownRequested = true;
        Console.WriteLine($"[{releaseName}] Received SIGTERM, shutting down...");
        cts.Cancel();
    }
};

// Step 5 - Connect to brokers with retry logic
Console.WriteLine("Connecting to Surgewave brokers...");

SurgewaveProducer<string, string>? producer = null;
for (int i = 0; i < maxRetries && !cts.Token.IsCancellationRequested; i++)
{
    try
    {
        producer = new SurgewaveProducer<string, string>(options =>
        {
            options.BootstrapServers = bootstrapServers;
        });
        Console.WriteLine("Connected to brokers!");
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Attempt {i + 1}/{maxRetries}: {ex.Message}");
        if (i < maxRetries - 1)
        {
            try
            {
                await Task.Delay(retryDelayMs, cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}

if (producer == null)
{
    Console.WriteLine("Failed to connect to brokers. Exiting.");
    return;
}

// Step 6 - Send messages with Helm context
await using (producer)
{
    Console.WriteLine();
    Console.WriteLine("Starting message producer...");
    var messageCount = 0;

    try
    {
        while (!cts.Token.IsCancellationRequested)
        {
            messageCount++;

            // Include release context in message key for traceability
            var key = $"{releaseName}-{podName}-{messageCount}";
            var value = new
            {
                MessageId = messageCount,
                HelmRelease = releaseName,
                Namespace = releaseNamespace,
                ChartVersion = chartVersion,
                Pod = podName,
                Timestamp = DateTime.UtcNow
            };

            await producer.ProduceAsync(topic, key, System.Text.Json.JsonSerializer.Serialize(value));
            Console.WriteLine($"[{releaseName}/{topic}] Sent message {messageCount}");

            await Task.Delay(1000, cts.Token);
        }
    }
    catch (OperationCanceledException)
    {
        // Expected during shutdown
    }

    Console.WriteLine($"[{releaseName}] Sent {messageCount} messages total.");
}

Console.WriteLine("Producer disposed, shutdown complete.");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Helm charts package all K8s resources together");
Console.WriteLine("2. values.yaml provides default configuration");
Console.WriteLine("3. Environment-specific values files support multiple envs");
Console.WriteLine("4. Helm manages versioning and rollbacks");
Console.WriteLine("5. Include release context in logs/messages for traceability");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
