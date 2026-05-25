// Surgewave Bootcamp - Unit 14, Lesson 3: Kubernetes Fundamentals
//
// In this lesson, you'll learn:
// - Configure Surgewave for Kubernetes deployment
// - Implement health probe endpoints
// - Handle Kubernetes pod lifecycle
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

Console.WriteLine("=== Surgewave Bootcamp: Lesson 14.3 - Kubernetes Fundamentals ===");
Console.WriteLine();

// TODO: Step 1 - Read pod information from Kubernetes downward API
// var podName = Environment.GetEnvironmentVariable("POD_NAME") ?? "local-dev";
// var podNamespace = Environment.GetEnvironmentVariable("POD_NAMESPACE") ?? "default";

// TODO: Step 2 - Read configuration from ConfigMap (via environment variables)
// var bootstrapServers = Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS")
//     ?? "surgewave-broker-headless:9092";
// var topic = Environment.GetEnvironmentVariable("Surgewave_TOPIC")
//     ?? "k8s-topic";

// TODO: Step 3 - Print pod and configuration info
// Console.WriteLine("Pod Information:");
// Console.WriteLine($"  Name: {podName}");
// Console.WriteLine($"  Namespace: {podNamespace}");
// Console.WriteLine();
// Console.WriteLine("Configuration (from ConfigMap):");
// Console.WriteLine($"  Bootstrap Servers: {bootstrapServers}");
// Console.WriteLine($"  Topic: {topic}");
// Console.WriteLine();

// TODO: Step 4 - Set up health server for Kubernetes probes
// var healthPort = int.TryParse(
//     Environment.GetEnvironmentVariable("HEALTH_PORT"),
//     out var port) ? port : 8080;
//
// var healthServer = new HealthServer(healthPort);
// await healthServer.StartAsync();
//
// Console.WriteLine($"Health server started on port {healthPort}");
// Console.WriteLine("  Liveness: /health/live");
// Console.WriteLine("  Readiness: /health/ready");
// Console.WriteLine();

// TODO: Step 5 - Set up graceful shutdown for Kubernetes SIGTERM
// var cts = new CancellationTokenSource();
// var shutdownComplete = new TaskCompletionSource();
//
// AppDomain.CurrentDomain.ProcessExit += (_, _) =>
// {
//     Console.WriteLine($"[{podName}] Received SIGTERM, initiating graceful shutdown...");
//     cts.Cancel();
//     // Wait for shutdown to complete (up to 25 seconds for K8s default 30s grace period)
//     shutdownComplete.Task.Wait(TimeSpan.FromSeconds(25));
// };
//
// Console.CancelKeyPress += (_, e) =>
// {
//     e.Cancel = true;
//     cts.Cancel();
// };

// TODO: Step 6 - Connect to brokers and set readiness
// Console.WriteLine("Connecting to brokers...");
// await using var producer = new SurgewaveProducer<string, string>(options =>
// {
//     options.BootstrapServers = bootstrapServers;
// });
// Console.WriteLine("Connected! Setting readiness to true.");
// healthServer.SetReady(true);

// TODO: Step 7 - Produce messages with pod identity
// Console.WriteLine();
// Console.WriteLine("Starting message producer...");
// var messageCount = 0;
// while (!cts.Token.IsCancellationRequested)
// {
//     messageCount++;
//     var key = $"{podName}-{messageCount}";
//     var value = $"Message from {podName} at {DateTime.UtcNow:O}";
//
//     await producer.ProduceAsync(topic, key, value);
//     Console.WriteLine($"[{podName}] Sent message {messageCount} to {topic}");
//
//     try { await Task.Delay(1000, cts.Token); } catch (OperationCanceledException) { break; }
// }

// TODO: Step 8 - Clean up and signal shutdown complete
// Console.WriteLine("Flushing pending messages...");
// healthServer.SetReady(false);
// await healthServer.StopAsync();
// Console.WriteLine("Shutdown complete.");
// shutdownComplete.TrySetResult();

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use ConfigMaps for non-sensitive configuration");
Console.WriteLine("2. Use Secrets for sensitive data (passwords, certs)");
Console.WriteLine("3. Implement health probes for reliability");
Console.WriteLine("4. Handle SIGTERM for graceful shutdown");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// TODO: Add HealthServer class below
// public class HealthServer
// {
//     private readonly int _port;
//     private readonly HttpListener _listener;
//     private bool _isReady;
//     private CancellationTokenSource? _cts;
//
//     public HealthServer(int port)
//     {
//         _port = port;
//         _listener = new HttpListener();
//         _listener.Prefixes.Add($"http://+:{port}/");
//     }
//
//     public void SetReady(bool isReady) => _isReady = isReady;
//
//     public Task StartAsync()
//     {
//         _cts = new CancellationTokenSource();
//         _listener.Start();
//         Task.Run(HandleRequests);
//         return Task.CompletedTask;
//     }
//
//     private async Task HandleRequests()
//     {
//         while (_listener.IsListening && !_cts!.Token.IsCancellationRequested)
//         {
//             try
//             {
//                 var context = await _listener.GetContextAsync();
//                 var response = context.Response;
//                 var path = context.Request.Url?.AbsolutePath;
//
//                 var (statusCode, body) = path switch
//                 {
//                     "/health/live" => (200, "alive"),
//                     "/health/ready" => _isReady ? (200, "ready") : (503, "not ready"),
//                     _ => (404, "not found")
//                 };
//
//                 response.StatusCode = statusCode;
//                 var buffer = System.Text.Encoding.UTF8.GetBytes(body);
//                 await response.OutputStream.WriteAsync(buffer);
//                 response.Close();
//             }
//             catch { }
//         }
//     }
//
//     public Task StopAsync()
//     {
//         _cts?.Cancel();
//         _listener.Stop();
//         return Task.CompletedTask;
//     }
// }
