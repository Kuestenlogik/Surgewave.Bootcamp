// Unit 20 Project: REST Gateway Bridge
// Build an HTTP gateway that bridges REST clients to Surgewave messaging

using System.Net;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Broker;

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

// TODO: Step 1 - Set up HttpListener
var gatewayPort = 8080;
Console.WriteLine($"[Gateway] Starting HTTP listener on http://localhost:{gatewayPort}/");

// TODO: Create and configure HttpListener
// Hint:
// var listener = new HttpListener();
// listener.Prefixes.Add($"http://localhost:{gatewayPort}/");
// listener.Start();

Console.WriteLine("[Gateway] Ready to accept requests\n");

// TODO: Step 2 - Main request loop with cancellation support
using var cts = new CancellationTokenSource();

// Handle Ctrl+C for graceful shutdown
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\n[Gateway] Shutdown requested...");
};

// TODO: Implement request loop
// while (!cts.Token.IsCancellationRequested)
// {
//     try
//     {
//         var context = await listener.GetContextAsync();
//         _ = HandleRequestAsync(context);
//     }
//     catch (HttpListenerException) when (cts.Token.IsCancellationRequested)
//     {
//         break;
//     }
// }

// For demo purposes, let's process a few simulated requests
await SimulateRequestsAsync();

Console.WriteLine("\n[Gateway] Shutting down...");
Console.WriteLine("=== Gateway Complete ===");

// TODO: Step 3 - Request handler method
async Task HandleRequestAsync(HttpListenerContext context)
{
    var path = context.Request.Url?.AbsolutePath ?? "/";
    var method = context.Request.HttpMethod;

    Console.WriteLine($"[Request] {method} {path}");

    try
    {
        // TODO: Route to appropriate handler
        // if (path == "/topics" && method == "GET")
        //     await HandleListTopicsAsync(context);
        // else if (path.StartsWith("/topics/") && method == "POST")
        //     await HandleProduceAsync(context);
        // else if (path.StartsWith("/topics/") && method == "GET")
        //     await HandleConsumeAsync(context);
        // else
        //     await SendNotFoundAsync(context);
    }
    catch (Exception ex)
    {
        await SendErrorAsync(context, 500, ex.Message);
    }
}

// TODO: Step 4 - List topics endpoint
async Task HandleListTopicsAsync(HttpListenerContext context)
{
    // TODO: Get list of topics from Surgewave client
    // var topics = await nativeClient.Topics.ListAsync();

    // TODO: Build response JSON
    // var response = new
    // {
    //     topics = topics.Select(t => new { name = t.Name, partitions = t.PartitionCount })
    // };

    // TODO: Send response
    // await SendJsonAsync(context, 200, response);

    Console.WriteLine("[Response] 200 OK - ? topics");
}

// TODO: Step 5 - Produce message endpoint
async Task HandleProduceAsync(HttpListenerContext context)
{
    // TODO: Extract topic name from path
    // var path = context.Request.Url?.AbsolutePath ?? "";
    // var topic = path.Replace("/topics/", "");

    // TODO: Read request body
    // using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
    // var body = await reader.ReadToEndAsync();

    // TODO: Parse JSON request
    // var request = JsonSerializer.Deserialize<ProduceRequest>(body);

    // TODO: Produce message to Surgewave
    // var offset = await nativeClient.Messaging.SendAsync(topic, partition: 0, key: request.Key, value: request.Value);

    // TODO: Build and send response
    // var response = new
    // {
    //     topic = topic,
    //     partition = 0,
    //     offset = offset,
    //     timestamp = DateTime.UtcNow.ToString("o")
    // };

    // await SendJsonAsync(context, 201, response);

    Console.WriteLine("[Response] 201 Created");
}

// TODO: Step 6 - Consume messages endpoint
async Task HandleConsumeAsync(HttpListenerContext context)
{
    // TODO: Extract topic name from path
    // var path = context.Request.Url?.AbsolutePath ?? "";
    // var topic = path.Replace("/topics/", "");

    // TODO: Parse query parameters
    // var query = context.Request.QueryString;
    // var partition = int.Parse(query["partition"] ?? "0");
    // var offset = long.Parse(query["offset"] ?? "0");
    // var limit = int.Parse(query["limit"] ?? "10");

    // TODO: Consume messages from Surgewave
    // await using var consumer = client.CreateConsumer<string, string>();
    // consumer.Assign(topic, partition, offset);
    // var messages = new List<object>();
    // using var consumeCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
    // ...

    // TODO: Build and send response
    // var response = new
    // {
    //     messages = messages,
    //     nextOffset = messages.Any() ? ((dynamic)messages.Last()).offset + 1 : offset
    // };

    // await SendJsonAsync(context, 200, response);

    Console.WriteLine("[Response] 200 OK");
}

// Helper: Send JSON response
async Task SendJsonAsync(HttpListenerContext context, int statusCode, object data)
{
    var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    var buffer = Encoding.UTF8.GetBytes(json);

    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/json";
    context.Response.ContentLength64 = buffer.Length;

    await context.Response.OutputStream.WriteAsync(buffer);
    context.Response.Close();
}

// Helper: Send error response
async Task SendErrorAsync(HttpListenerContext context, int statusCode, string message)
{
    var response = new { error = message };
    await SendJsonAsync(context, statusCode, response);
    Console.WriteLine($"[Response] {statusCode} Error: {message}");
}

// Helper: Send 404 Not Found
async Task SendNotFoundAsync(HttpListenerContext context)
{
    await SendErrorAsync(context, 404, "Not Found");
}

// Simulate some requests for demo
async Task SimulateRequestsAsync()
{
    Console.WriteLine("--- Simulating API requests ---\n");

    // Simulate producing messages
    Console.WriteLine("[Demo] Producing test messages...");

    var offset1 = await nativeClient.Messaging.SendAsync("orders", partition: 0, key: "order-1", value: """{"amount": 99.95, "customer": "Alice"}""");
    Console.WriteLine($"[Surgewave] Produced message to orders:0 at offset {offset1}");

    var offset2 = await nativeClient.Messaging.SendAsync("orders", partition: 0, key: "order-2", value: """{"amount": 149.50, "customer": "Bob"}""");
    Console.WriteLine($"[Surgewave] Produced message to orders:0 at offset {offset2}");

    // Simulate consuming messages
    Console.WriteLine("\n[Demo] Consuming messages...");

    await using var consumer = client.CreateConsumer<string, string>();
    consumer.Assign("orders", partition: 0, offset: 0);

    using var consumeCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
    var count = 0;

    try
    {
        while (!consumeCts.Token.IsCancellationRequested && count < 2)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100), consumeCts.Token);
            if (result != null)
            {
                Console.WriteLine($"[Surgewave] Consumed: offset={result.Offset}, key={result.Key}, value={result.Value}");
                count++;
            }
            else
            {
                break;
            }
        }
    }
    catch (OperationCanceledException)
    {
        // Expected
    }

    Console.WriteLine($"\n[Demo] Processed {count} messages");
}

// Request/Response models
public record ProduceRequest(string? Key, string Value);