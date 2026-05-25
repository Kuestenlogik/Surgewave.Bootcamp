// Surgewave Bootcamp - Unit 14, Lesson 4: Helm Charts
//
// In this lesson, you'll learn:
// - Configure Surgewave for Helm-managed deployments
// - Read Helm release information
// - Support Helm value overrides
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

Console.WriteLine("=== Surgewave Bootcamp: Lesson 14.4 - Helm Charts ===");
Console.WriteLine();

// TODO: Step 1 - Read Helm release information
// var releaseName = Environment.GetEnvironmentVariable("HELM_RELEASE_NAME") ?? "local";
// var releaseNamespace = Environment.GetEnvironmentVariable("HELM_RELEASE_NAMESPACE") ?? "default";
// var podName = Environment.GetEnvironmentVariable("POD_NAME") ?? "local-dev";

// TODO: Step 2 - Read configuration from Helm values (via ConfigMap)
// var bootstrapServers = Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS")
//     ?? "surgewave-broker:9092";
// var topic = Environment.GetEnvironmentVariable("Surgewave_TOPIC")
//     ?? "helm-topic";
// var groupId = Environment.GetEnvironmentVariable("Surgewave_GROUP_ID")
//     ?? "helm-consumers";

// TODO: Step 3 - Print release and configuration info
// Console.WriteLine("Helm Release Information:");
// Console.WriteLine($"  Release Name: {releaseName}");
// Console.WriteLine($"  Release Namespace: {releaseNamespace}");
// Console.WriteLine($"  Pod Name: {podName}");
// Console.WriteLine();
// Console.WriteLine("Configuration (from Helm values):");
// Console.WriteLine($"  Bootstrap Servers: {bootstrapServers}");
// Console.WriteLine($"  Topic: {topic}");
// Console.WriteLine($"  Group ID: {groupId}");
// Console.WriteLine();

// TODO: Step 4 - Set up graceful shutdown
// var cts = new CancellationTokenSource();
// Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
// AppDomain.CurrentDomain.ProcessExit += (_, _) => cts.Cancel();

// TODO: Step 5 - Create producer and send messages with Helm context
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
//     var key = $"{releaseName}-{podName}-{messageCount}";
//     var value = $"Message {messageCount} from Helm release {releaseName}";
//
//     await producer.ProduceAsync(topic, key, value);
//     Console.WriteLine($"[{releaseName}/{topic}] Sent message {messageCount}");
//
//     try { await Task.Delay(1000, cts.Token); } catch (OperationCanceledException) { break; }
// }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Helm charts package all K8s resources together");
Console.WriteLine("2. values.yaml provides default configuration");
Console.WriteLine("3. Environment-specific values files support multiple envs");
Console.WriteLine("4. Helm manages versioning and rollbacks");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
