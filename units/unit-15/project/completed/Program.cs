// Unit 20 Project: REST Gateway Bridge - COMPLETED
// An HTTP gateway that bridges REST clients to Surgewave messaging

using System.Net;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Broker;

var JsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

Console.WriteLine("=== REST Gateway Bridge ===\n");

// Start embedded Surgewave broker
Console.WriteLine("[Surgewave] Starting embedded broker...");

await using var surgewave = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .Build()
    .StartAsync();

await using var client = await SurgewaveClient.Create(surgewave.BootstrapServers).BuildAsync();
var nativeClient = client.NativeClient!;

// Create sample topics
await nativeClient.Topics.CreateAsync("orders", partitions: 3);
await nativeClient.Topics.CreateAsync("events", partitions: 1);
Console.WriteLine($"[Surgewave] Broker started on port {surgewave.Port}");
Console.WriteLine("[Surgewave] Created topics: orders, events\n");

// Step 1: Set up HttpListener
var gatewayPort = 8080;

// Find an available port if 8080 is in use
var listener = new HttpListener();
var portFound = false;

for (int port = gatewayPort; port < gatewayPort + 100 && !portFound; port++)
{
    try
    {
        listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        gatewayPort = port;
        portFound = true;
    }
    catch (HttpListenerException)
    {
        listener.Close();
    }
}

if (!portFound)
{
    Console.WriteLine("[Gateway] ERROR: Could not find available port");
    return;
}

Console.WriteLine($"[Gateway] HTTP listener started on http://localhost:{gatewayPort}/");
Console.WriteLine("[Gateway] Ready to accept requests");
Console.WriteLine();
Console.WriteLine("Endpoints:");
Console.WriteLine($"  GET  http://localhost:{gatewayPort}/topics          - List all topics");
Console.WriteLine($"  POST http://localhost:{gatewayPort}/topics/{{topic}}   - Produce message");
Console.WriteLine($"  GET  http://localhost:{gatewayPort}/topics/{{topic}}   - Consume messages");
Console.WriteLine();

// Step 2: Set up cancellation and graceful shutdown
using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\n[Gateway] Shutdown requested...");
    listener.Stop();
};

// Step 3: Run demo requests in background, then accept real requests
_ = Task.Run(async () =>
{
    await Task.Delay(500);
    await RunDemoRequestsAsync();
});

// Main request loop
Console.WriteLine("[Gateway] Accepting requests (press Ctrl+C to stop)...\n");

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        try
        {
            var context = await listener.GetContextAsync();
            _ = HandleRequestAsync(context);
        }
        catch (HttpListenerException) when (cts.Token.IsCancellationRequested)
        {
            break;
        }
        catch (ObjectDisposedException)
        {
            break;
        }
    }
}
catch (OperationCanceledException)
{
    // Expected on shutdown
}

listener.Close();
Console.WriteLine("[Gateway] HTTP listener stopped");
Console.WriteLine("=== Gateway Complete ===");

// Request handler
async Task HandleRequestAsync(HttpListenerContext context)
{
    var path = context.Request.Url?.AbsolutePath ?? "/";
    var method = context.Request.HttpMethod;

    Console.WriteLine($"[Request] {method} {path}");

    try
    {
        // Route to appropriate handler
        if (path == "/topics" && method == "GET")
        {
            await HandleListTopicsAsync(context);
        }
        else if (path.StartsWith("/topics/") && method == "POST")
        {
            await HandleProduceAsync(context);
        }
        else if (path.StartsWith("/topics/") && method == "GET")
        {
            await HandleConsumeAsync(context);
        }
        else if (path == "/" && method == "GET")
        {
            await HandleRootAsync(context);
        }
        else
        {
            await SendNotFoundAsync(context);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Error] {ex.Message}");
        await SendErrorAsync(context, 500, ex.Message);
    }
}

// Root endpoint - API info
async Task HandleRootAsync(HttpListenerContext context)
{
    var response = new
    {
        name = "Surgewave REST Gateway",
        version = "1.0.0",
        endpoints = new[]
        {
            new { method = "GET", path = "/topics", description = "List all topics" },
            new { method = "POST", path = "/topics/{topic}", description = "Produce a message" },
            new { method = "GET", path = "/topics/{topic}", description = "Consume messages" }
        }
    };

    await SendJsonAsync(context, 200, response);
    Console.WriteLine("[Response] 200 OK - API info");
}

// List topics endpoint
async Task HandleListTopicsAsync(HttpListenerContext context)
{
    var topics = await nativeClient.Topics.ListAsync();

    var response = new
    {
        topics = topics.Select(t => new
        {
            name = t.Name,
            partitions = t.PartitionCount
        }).ToArray()
    };

    await SendJsonAsync(context, 200, response);
    Console.WriteLine($"[Response] 200 OK - {topics.Count} topics");
}

// Produce message endpoint
async Task HandleProduceAsync(HttpListenerContext context)
{
    // Extract topic name from path
    var path = context.Request.Url?.AbsolutePath ?? "";
    var topic = path.Replace("/topics/", "");

    if (string.IsNullOrWhiteSpace(topic))
    {
        await SendErrorAsync(context, 400, "Topic name is required");
        return;
    }

    // Read request body
    using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
    var body = await reader.ReadToEndAsync();

    if (string.IsNullOrWhiteSpace(body))
    {
        await SendErrorAsync(context, 400, "Request body is required");
        return;
    }

    // Parse JSON request
    ProduceRequest? request;
    try
    {
        request = JsonSerializer.Deserialize<ProduceRequest>(body, JsonOptions);
    }
    catch (JsonException ex)
    {
        await SendErrorAsync(context, 400, $"Invalid JSON: {ex.Message}");
        return;
    }

    if (request is null || string.IsNullOrWhiteSpace(request.Value))
    {
        await SendErrorAsync(context, 400, "Message value is required");
        return;
    }

    // Produce message to Surgewave
    var offset = await nativeClient.Messaging.SendAsync(topic, partition: 0, key: request.Key, value: request.Value);

    // Build response
    var response = new
    {
        topic,
        partition = 0,
        offset,
        timestamp = DateTime.UtcNow.ToString("o")
    };

    await SendJsonAsync(context, 201, response);
    Console.WriteLine($"[Surgewave] Produced message to {topic}:0 at offset {offset}");
    Console.WriteLine("[Response] 201 Created");
}

// Consume messages endpoint
async Task HandleConsumeAsync(HttpListenerContext context)
{
    // Extract topic name from path
    var path = context.Request.Url?.AbsolutePath ?? "";
    var topic = path.Replace("/topics/", "");

    if (string.IsNullOrWhiteSpace(topic))
    {
        await SendErrorAsync(context, 400, "Topic name is required");
        return;
    }

    // Parse query parameters
    var query = context.Request.QueryString;
    var partition = int.TryParse(query["partition"], out var p) ? p : 0;
    var offset = long.TryParse(query["offset"], out var o) ? o : 0L;
    var limit = int.TryParse(query["limit"], out var l) ? l : 10;

    // Consume messages from Surgewave
    var messages = new List<object>();
    long lastOffset = offset;

    await using var consumer = client.CreateConsumer<string, string>();
    consumer.Assign(topic, partition, offset);

    using var consumeCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

    try
    {
        while (!consumeCts.Token.IsCancellationRequested && messages.Count < limit)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100), consumeCts.Token);
            if (result != null)
            {
                messages.Add(new
                {
                    offset = result.Offset,
                    partition = result.Partition,
                    key = result.Key,
                    value = result.Value,
                    timestamp = result.Timestamp.ToString("o")
                });

                lastOffset = result.Offset;
            }
            else
            {
                break;
            }
        }
    }
    catch (OperationCanceledException)
    {
        // Timeout - return what we have
    }

    // Build response
    var response = new
    {
        topic,
        messages,
        count = messages.Count,
        nextOffset = messages.Count > 0 ? lastOffset + 1 : offset
    };

    await SendJsonAsync(context, 200, response);
    Console.WriteLine($"[Surgewave] Consumed {messages.Count} message(s) from {topic}");
    Console.WriteLine("[Response] 200 OK");
}

// Helper: Send JSON response
async Task SendJsonAsync(HttpListenerContext context, int statusCode, object data)
{
    var json = JsonSerializer.Serialize(data, JsonOptions);
    var buffer = Encoding.UTF8.GetBytes(json);

    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/json";
    context.Response.ContentLength64 = buffer.Length;

    // Add CORS headers for browser access
    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
    context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
    context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

    await context.Response.OutputStream.WriteAsync(buffer);
    context.Response.Close();
}

// Helper: Send error response
async Task SendErrorAsync(HttpListenerContext context, int statusCode, string message)
{
    var response = new { error = message, status = statusCode };
    await SendJsonAsync(context, statusCode, response);
    Console.WriteLine($"[Response] {statusCode} Error: {message}");
}

// Helper: Send 404 Not Found
async Task SendNotFoundAsync(HttpListenerContext context)
{
    await SendErrorAsync(context, 404, "Endpoint not found");
}

// Demo requests using HttpClient
async Task RunDemoRequestsAsync()
{
    Console.WriteLine("--- Running Demo Requests ---\n");

    using var httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri($"http://localhost:{gatewayPort}/");

    try
    {
        // 1. List topics
        Console.WriteLine("[Demo] GET /topics");
        var topicsResponse = await httpClient.GetStringAsync("topics");
        Console.WriteLine($"[Demo] Response: {topicsResponse}\n");

        // 2. Produce some messages
        Console.WriteLine("[Demo] POST /topics/orders");
        var produceContent = new StringContent(
            """{"key":"order-1","value":"{\"amount\":99.95,\"customer\":\"Alice\"}"}""",
            Encoding.UTF8,
            "application/json");
        var produceResponse = await httpClient.PostAsync("topics/orders", produceContent);
        var produceResult = await produceResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"[Demo] Response: {produceResult}\n");

        Console.WriteLine("[Demo] POST /topics/orders");
        produceContent = new StringContent(
            """{"key":"order-2","value":"{\"amount\":149.50,\"customer\":\"Bob\"}"}""",
            Encoding.UTF8,
            "application/json");
        produceResponse = await httpClient.PostAsync("topics/orders", produceContent);
        produceResult = await produceResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"[Demo] Response: {produceResult}\n");

        // 3. Consume messages
        Console.WriteLine("[Demo] GET /topics/orders?offset=0&limit=10");
        var consumeResponse = await httpClient.GetStringAsync("topics/orders?offset=0&limit=10");
        Console.WriteLine($"[Demo] Response: {consumeResponse}\n");

        Console.WriteLine("--- Demo Complete ---\n");
        Console.WriteLine("Gateway is running. Try your own requests or press Ctrl+C to stop.\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Demo] Error: {ex.Message}");
    }
}

// JSON serializer options (declared at top of file)

// Request model
public record ProduceRequest(string? Key, string Value);