// Surgewave Bootcamp - Unit 14, Lesson 3: Kubernetes Fundamentals
// COMPLETED SOLUTION

using System.Net;
using Kuestenlogik.Surgewave.Client;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 14.3 - Kubernetes Fundamentals ===");
Console.WriteLine();

// Step 1 - Read pod information from Kubernetes downward API
var podName = Environment.GetEnvironmentVariable("POD_NAME") ?? "local-dev";
var podNamespace = Environment.GetEnvironmentVariable("POD_NAMESPACE") ?? "default";
var nodeName = Environment.GetEnvironmentVariable("NODE_NAME") ?? "unknown";

// Step 2 - Read configuration from ConfigMap (via environment variables)
var bootstrapServers = Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS")
    ?? "surgewave-broker-headless:9092";
var topic = Environment.GetEnvironmentVariable("Surgewave_TOPIC")
    ?? "k8s-topic";
var maxRetries = int.TryParse(
    Environment.GetEnvironmentVariable("Surgewave_MAX_RETRIES"),
    out var retries) ? retries : 30;

// Step 3 - Print pod and configuration info
Console.WriteLine("Pod Information:");
Console.WriteLine($"  Name: {podName}");
Console.WriteLine($"  Namespace: {podNamespace}");
Console.WriteLine($"  Node: {nodeName}");
Console.WriteLine();
Console.WriteLine("Configuration (from ConfigMap):");
Console.WriteLine($"  Bootstrap Servers: {bootstrapServers}");
Console.WriteLine($"  Topic: {topic}");
Console.WriteLine($"  Max Retries: {maxRetries}");
Console.WriteLine();

// Step 4 - Set up health server for Kubernetes probes
var healthPort = int.TryParse(
    Environment.GetEnvironmentVariable("HEALTH_PORT"),
    out var port) ? port : 8080;

var healthServer = new HealthServer(healthPort);
await healthServer.StartAsync();

Console.WriteLine($"Health server started on port {healthPort}");
Console.WriteLine("  Liveness: /health/live");
Console.WriteLine("  Readiness: /health/ready");
Console.WriteLine();

// Step 5 - Set up graceful shutdown for Kubernetes SIGTERM
var cts = new CancellationTokenSource();
var shutdownComplete = new TaskCompletionSource();
var shutdownRequested = false;

AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    if (!shutdownRequested)
    {
        shutdownRequested = true;
        Console.WriteLine($"[{podName}] Received SIGTERM, initiating graceful shutdown...");
        cts.Cancel();
        // Wait for shutdown to complete (up to 25 seconds for K8s default 30s grace period)
        shutdownComplete.Task.Wait(TimeSpan.FromSeconds(25));
    }
};

Console.CancelKeyPress += (_, e) =>
{
    if (!shutdownRequested)
    {
        shutdownRequested = true;
        Console.WriteLine();
        Console.WriteLine($"[{podName}] Received SIGINT, shutting down...");
        e.Cancel = true;
        cts.Cancel();
    }
};

// Step 6 - Connect to brokers with retry logic
Console.WriteLine("Connecting to brokers...");

SurgewaveProducer<string, string>? producer = null;
for (int i = 0; i < maxRetries && !cts.Token.IsCancellationRequested; i++)
{
    try
    {
        producer = new SurgewaveProducer<string, string>(options =>
        {
            options.BootstrapServers = bootstrapServers;
        });
        Console.WriteLine("Connected! Setting readiness to true.");
        healthServer.SetReady(true);
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
    Console.WriteLine("Failed to connect to brokers. Exiting.");
    healthServer.SetReady(false);
    await healthServer.StopAsync();
    return;
}

await using (producer)
{
    // Step 7 - Produce messages with pod identity
    Console.WriteLine();
    Console.WriteLine("Starting message producer...");
    var messageCount = 0;

    try
    {
        while (!cts.Token.IsCancellationRequested)
        {
            messageCount++;
            var key = $"{podName}-{messageCount}";
            var value = $"Message {messageCount} from {podName} on {nodeName} at {DateTime.UtcNow:O}";

            await producer.ProduceAsync(topic, key, value);
            Console.WriteLine($"[{podName}] Sent message {messageCount} to {topic}");

            await Task.Delay(1000, cts.Token);
        }
    }
    catch (OperationCanceledException)
    {
        // Expected during shutdown
    }

    // Step 8 - Clean up and signal shutdown complete
    Console.WriteLine($"[{podName}] Sent {messageCount} messages total.");
    Console.WriteLine("Flushing pending messages...");
}

healthServer.SetReady(false);
await healthServer.StopAsync();
Console.WriteLine("Shutdown complete.");
shutdownComplete.TrySetResult();

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use ConfigMaps for non-sensitive configuration");
Console.WriteLine("2. Use Secrets for sensitive data (passwords, certs)");
Console.WriteLine("3. Implement health probes for reliability");
Console.WriteLine("4. Handle SIGTERM for graceful shutdown");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// --- Health Server Implementation ---

public class HealthServer
{
    private readonly int _port;
    private readonly HttpListener _listener;
    private volatile bool _isReady;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;

    public HealthServer(int port)
    {
        _port = port;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{port}/");
    }

    public void SetReady(bool isReady) => _isReady = isReady;

    public Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        try
        {
            _listener.Start();
            _listenerTask = Task.Run(HandleRequestsAsync);
        }
        catch (HttpListenerException)
        {
            // May fail if not running with admin rights on Windows
            Console.WriteLine($"  Warning: Could not start health server on port {_port}");
            Console.WriteLine("  (Run as admin or use a different port on Windows)");
        }
        return Task.CompletedTask;
    }

    private async Task HandleRequestsAsync()
    {
        while (_listener.IsListening && !_cts!.Token.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                await HandleRequestAsync(context);
            }
            catch (HttpListenerException)
            {
                // Listener was stopped
                break;
            }
            catch (ObjectDisposedException)
            {
                // Listener was disposed
                break;
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var response = context.Response;
        var path = context.Request.Url?.AbsolutePath ?? "";

        var (statusCode, body) = path switch
        {
            "/health/live" => (200, "{\"status\":\"alive\"}"),
            "/health/ready" => _isReady
                ? (200, "{\"status\":\"ready\"}")
                : (503, "{\"status\":\"not ready\"}"),
            "/health" => (200, $"{{\"live\":true,\"ready\":{_isReady.ToString().ToLower()}}}"),
            _ => (404, "{\"error\":\"not found\"}")
        };

        response.StatusCode = statusCode;
        response.ContentType = "application/json";

        var buffer = System.Text.Encoding.UTF8.GetBytes(body);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.Close();
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        _listener.Stop();

        if (_listenerTask != null)
        {
            try
            {
                await _listenerTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException)
            {
                // Ignore timeout
            }
        }

        _listener.Close();
    }
}
