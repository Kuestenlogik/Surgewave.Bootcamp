// Surgewave Bootcamp - Unit 14, Lesson 5: Cloud Providers
//
// In this lesson, you'll learn:
// - Detect and configure for different cloud environments
// - Handle cloud-specific authentication patterns
// - Create cloud-agnostic Surgewave applications
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

Console.WriteLine("=== Surgewave Bootcamp: Lesson 14.5 - Cloud Providers ===");
Console.WriteLine();

// TODO: Step 1 - Define CloudProvider enum
// enum CloudProvider { Local, AWS, Azure, GCP }

// TODO: Step 2 - Implement cloud provider detection
// CloudProvider DetectCloudProvider()
// {
//     var cloudEnv = Environment.GetEnvironmentVariable("CLOUD_PROVIDER");
//     if (!string.IsNullOrEmpty(cloudEnv))
//     {
//         return cloudEnv.ToLower() switch
//         {
//             "aws" => CloudProvider.AWS,
//             "azure" => CloudProvider.Azure,
//             "gcp" => CloudProvider.GCP,
//             _ => CloudProvider.Local
//         };
//     }
//
//     // AWS detection
//     if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_REGION")))
//         return CloudProvider.AWS;
//
//     // Azure detection
//     if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")))
//         return CloudProvider.Azure;
//
//     // GCP detection
//     if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT")))
//         return CloudProvider.GCP;
//
//     return CloudProvider.Local;
// }

// TODO: Step 3 - Create configuration record
// record SurgewaveCloudConfig(
//     string BootstrapServers,
//     string Topic,
//     string? SecurityProtocol = null,
//     string? SaslMechanism = null
// );

// TODO: Step 4 - Implement cloud-specific configuration
// SurgewaveCloudConfig GetConfiguration(CloudProvider provider)
// {
//     return provider switch
//     {
//         CloudProvider.AWS => new SurgewaveCloudConfig(
//             Environment.GetEnvironmentVariable("MSK_BOOTSTRAP_SERVERS") ?? "msk-broker:9092",
//             Environment.GetEnvironmentVariable("Surgewave_TOPIC") ?? "aws-topic",
//             "SASL_SSL", "AWS_MSK_IAM"
//         ),
//         CloudProvider.Azure => new SurgewaveCloudConfig(
//             $"{Environment.GetEnvironmentVariable("EVENTHUB_NAMESPACE")}.servicebus.windows.net:9093",
//             Environment.GetEnvironmentVariable("Surgewave_TOPIC") ?? "azure-topic",
//             "SASL_SSL", "PLAIN"
//         ),
//         CloudProvider.GCP => new SurgewaveCloudConfig(
//             Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "gcp-kafka:9092",
//             Environment.GetEnvironmentVariable("Surgewave_TOPIC") ?? "gcp-topic",
//             "SASL_SSL", "PLAIN"
//         ),
//         _ => new SurgewaveCloudConfig(
//             Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS") ?? "localhost:9092",
//             Environment.GetEnvironmentVariable("Surgewave_TOPIC") ?? "local-topic"
//         )
//     };
// }

// TODO: Step 5 - Detect and display cloud environment
// Console.WriteLine("Detecting cloud environment...");
// var provider = DetectCloudProvider();
// Console.WriteLine($"Detected cloud provider: {provider}");
// Console.WriteLine();
//
// var config = GetConfiguration(provider);
// Console.WriteLine($"{provider} Configuration:");
// Console.WriteLine($"  Bootstrap Servers: {config.BootstrapServers}");
// Console.WriteLine($"  Topic: {config.Topic}");
// if (config.SecurityProtocol != null)
//     Console.WriteLine($"  Security Protocol: {config.SecurityProtocol}");
// if (config.SaslMechanism != null)
//     Console.WriteLine($"  SASL Mechanism: {config.SaslMechanism}");
// Console.WriteLine();

// TODO: Step 6 - Set up graceful shutdown
// var cts = new CancellationTokenSource();
// Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

// TODO: Step 7 - Create producer and send messages
// await using var producer = new SurgewaveProducer<string, string>(options =>
// {
//     options.BootstrapServers = config.BootstrapServers;
// });
//
// Console.WriteLine($"Connecting to {provider} cluster...");
// Console.WriteLine("Connected successfully!");
// Console.WriteLine();
// Console.WriteLine("Starting message producer...");
//
// var messageCount = 0;
// while (!cts.Token.IsCancellationRequested)
// {
//     messageCount++;
//     var key = $"{provider}-{messageCount}";
//     var value = $"Message {messageCount} from {provider}";
//
//     await producer.ProduceAsync(config.Topic, key, value);
//     Console.WriteLine($"[{provider}/{config.Topic}] Sent message {messageCount}");
//
//     try { await Task.Delay(1000, cts.Token); } catch (OperationCanceledException) { break; }
// }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Detect cloud environment from environment variables");
Console.WriteLine("2. Use cloud-native authentication (IAM, AAD, Workload Identity)");
Console.WriteLine("3. Configure security protocols for managed services");
Console.WriteLine("4. Use Private Link/Endpoints for secure connectivity");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
