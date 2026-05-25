// Surgewave Bootcamp - Unit 14, Lesson 5: Cloud Providers
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Client;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 14.5 - Cloud Providers ===");
Console.WriteLine();

// Step 1 - Detect cloud environment
Console.WriteLine("Detecting cloud environment...");
var provider = DetectCloudProvider();
Console.WriteLine($"Detected cloud provider: {provider}");
Console.WriteLine();

// Step 2 - Get cloud-specific configuration
var config = GetConfiguration(provider);
PrintConfiguration(provider, config);

// Step 3 - Set up graceful shutdown
var cts = new CancellationTokenSource();
var shutdownRequested = false;

Console.CancelKeyPress += (_, e) =>
{
    if (!shutdownRequested)
    {
        shutdownRequested = true;
        Console.WriteLine();
        Console.WriteLine($"[{provider}] Received SIGINT, shutting down...");
        e.Cancel = true;
        cts.Cancel();
    }
};

AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    if (!shutdownRequested)
    {
        shutdownRequested = true;
        Console.WriteLine($"[{provider}] Received SIGTERM, shutting down...");
        cts.Cancel();
    }
};

// Step 4 - Connect to broker cluster
Console.WriteLine($"Connecting to {provider} cluster...");

SurgewaveProducer<string, string>? producer = null;
var maxRetries = 30;

for (int i = 0; i < maxRetries && !cts.Token.IsCancellationRequested; i++)
{
    try
    {
        producer = new SurgewaveProducer<string, string>(options =>
        {
            options.BootstrapServers = config.BootstrapServers;
            // In a real implementation, you would also set:
            // options.SecurityProtocol = config.SecurityProtocol;
            // options.SaslMechanism = config.SaslMechanism;
            // options.SaslUsername = config.SaslUsername;
            // options.SaslPassword = config.SaslPassword;
        });
        Console.WriteLine("Connected successfully!");
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Attempt {i + 1}/{maxRetries}: {ex.Message}");
        if (i < maxRetries - 1)
        {
            try
            {
                await Task.Delay(2000, cts.Token);
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
    Console.WriteLine($"Failed to connect to {provider} cluster. Exiting.");
    return;
}

// Step 5 - Produce messages with cloud context
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

            // Include cloud context in message
            var key = $"{provider}-{Environment.MachineName}-{messageCount}";
            var messageData = new
            {
                MessageId = messageCount,
                CloudProvider = provider.ToString(),
                Timestamp = DateTime.UtcNow,
                Region = GetCloudRegion(provider),
                Topic = config.Topic
            };

            var value = System.Text.Json.JsonSerializer.Serialize(messageData);
            await producer.ProduceAsync(config.Topic, key, value);
            Console.WriteLine($"[{provider}/{config.Topic}] Sent message {messageCount}");

            await Task.Delay(1000, cts.Token);
        }
    }
    catch (OperationCanceledException)
    {
        // Expected during shutdown
    }

    Console.WriteLine($"[{provider}] Sent {messageCount} messages total.");
}

Console.WriteLine("Shutdown complete.");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Detect cloud environment from environment variables");
Console.WriteLine("2. Use cloud-native authentication (IAM, AAD, Workload Identity)");
Console.WriteLine("3. Configure security protocols for managed services");
Console.WriteLine("4. Use Private Link/Endpoints for secure connectivity");
Console.WriteLine("5. Include cloud context in messages for observability");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// --- Helper Methods ---

static CloudProvider DetectCloudProvider()
{
    // Check explicit environment variable first
    var cloudEnv = Environment.GetEnvironmentVariable("CLOUD_PROVIDER");
    if (!string.IsNullOrEmpty(cloudEnv))
    {
        return cloudEnv.ToLower() switch
        {
            "aws" => CloudProvider.AWS,
            "azure" => CloudProvider.Azure,
            "gcp" => CloudProvider.GCP,
            _ => CloudProvider.Local
        };
    }

    // AWS detection
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_REGION")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_EXECUTION_ENV")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ECS_CONTAINER_METADATA_URI")))
    {
        return CloudProvider.AWS;
    }

    // Azure detection
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")))
    {
        return CloudProvider.Azure;
    }

    // GCP detection
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GCE_METADATA_HOST")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("K_SERVICE")))
    {
        return CloudProvider.GCP;
    }

    return CloudProvider.Local;
}

static SurgewaveCloudConfig GetConfiguration(CloudProvider provider)
{
    return provider switch
    {
        CloudProvider.AWS => new SurgewaveCloudConfig(
            BootstrapServers: Environment.GetEnvironmentVariable("MSK_BOOTSTRAP_SERVERS")
                ?? "localhost:9092",
            Topic: Environment.GetEnvironmentVariable("Surgewave_TOPIC") ?? "aws-orders",
            SecurityProtocol: "SASL_SSL",
            SaslMechanism: "AWS_MSK_IAM",
            SaslUsername: null,
            SaslPassword: null
        ),
        CloudProvider.Azure => new SurgewaveCloudConfig(
            BootstrapServers: !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EVENTHUB_NAMESPACE"))
                ? $"{Environment.GetEnvironmentVariable("EVENTHUB_NAMESPACE")}.servicebus.windows.net:9093"
                : "localhost:9092",
            Topic: Environment.GetEnvironmentVariable("Surgewave_TOPIC") ?? "azure-orders",
            SecurityProtocol: "SASL_SSL",
            SaslMechanism: "PLAIN",
            SaslUsername: "$ConnectionString",
            SaslPassword: Environment.GetEnvironmentVariable("EVENTHUB_CONNECTION_STRING")
        ),
        CloudProvider.GCP => new SurgewaveCloudConfig(
            BootstrapServers: Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
                ?? "localhost:9092",
            Topic: Environment.GetEnvironmentVariable("Surgewave_TOPIC") ?? "gcp-orders",
            SecurityProtocol: "SASL_SSL",
            SaslMechanism: "PLAIN",
            SaslUsername: Environment.GetEnvironmentVariable("KAFKA_USERNAME"),
            SaslPassword: Environment.GetEnvironmentVariable("KAFKA_PASSWORD")
        ),
        _ => new SurgewaveCloudConfig(
            BootstrapServers: Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS")
                ?? "localhost:9092",
            Topic: Environment.GetEnvironmentVariable("Surgewave_TOPIC") ?? "local-topic",
            SecurityProtocol: null,
            SaslMechanism: null,
            SaslUsername: null,
            SaslPassword: null
        )
    };
}

static void PrintConfiguration(CloudProvider provider, SurgewaveCloudConfig config)
{
    Console.WriteLine($"{provider} Configuration:");
    Console.WriteLine($"  Bootstrap Servers: {config.BootstrapServers}");
    Console.WriteLine($"  Topic: {config.Topic}");

    if (!string.IsNullOrEmpty(config.SecurityProtocol))
        Console.WriteLine($"  Security Protocol: {config.SecurityProtocol}");

    if (!string.IsNullOrEmpty(config.SaslMechanism))
        Console.WriteLine($"  SASL Mechanism: {config.SaslMechanism}");

    if (!string.IsNullOrEmpty(config.SaslUsername))
        Console.WriteLine($"  SASL Username: {config.SaslUsername}");

    var region = GetCloudRegion(provider);
    if (!string.IsNullOrEmpty(region))
        Console.WriteLine($"  Region: {region}");

    Console.WriteLine();
}

static string? GetCloudRegion(CloudProvider provider)
{
    return provider switch
    {
        CloudProvider.AWS => Environment.GetEnvironmentVariable("AWS_REGION")
            ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION"),
        CloudProvider.Azure => Environment.GetEnvironmentVariable("AZURE_REGION")
            ?? Environment.GetEnvironmentVariable("WEBSITE_REGION"),
        CloudProvider.GCP => Environment.GetEnvironmentVariable("GOOGLE_CLOUD_REGION")
            ?? Environment.GetEnvironmentVariable("FUNCTION_REGION"),
        _ => null
    };
}

// --- Type Definitions ---

enum CloudProvider
{
    Local,
    AWS,
    Azure,
    GCP
}

record SurgewaveCloudConfig(
    string BootstrapServers,
    string Topic,
    string? SecurityProtocol,
    string? SaslMechanism,
    string? SaslUsername,
    string? SaslPassword
);
