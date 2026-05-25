// Surgewave Bootcamp - Unit 23, Lesson 2: Surgewave MCP Server Setup
//
// In this lesson, you'll learn:
// - How to set up a Surgewave MCP Server
// - Transport configuration (stdio, HTTP/SSE)
// - Server lifecycle management
// - Connecting MCP to Surgewave runtime
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text.Json;
using System.Text.Json.Serialization;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 23.2 - Surgewave MCP Server Setup ===");
Console.WriteLine();

// JSON serialization options
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

// TODO: Step 1 - Start the Surgewave runtime
// await using var runtime = await SurgewaveRuntime.CreateBuilder()
//     .WithPort(0)
//     .WithStorageMode(StorageMode.Memory)
//     .WithAutoCreateTopics(true)
//     .Build()
//     .StartAsync();
//
// Console.WriteLine($"Surgewave broker started on port {runtime.Port}");
// var bootstrapServers = $"localhost:{runtime.Port}";

// TODO: Step 2 - Create MCP server options class
// public class McpServerOptions
// {
//     public string ServerName { get; set; } = "surgewave-mcp";
//     public string ServerVersion { get; set; } = "1.0.0";
//     public string ProtocolVersion { get; set; } = "2024-11-05";
// }

// TODO: Step 3 - Create the MCP Server class
// See INSTRUCTIONS.md for the SimpleMcpServer implementation

// TODO: Step 4 - Create server instance
// var options = new McpServerOptions();
// var server = new SimpleMcpServer(options);
// Console.WriteLine($"[MCP] Server created: {options.ServerName} v{options.ServerVersion}");

// TODO: Step 5 - Connect to Surgewave
// server.ConnectToSurgewave(bootstrapServers);

// TODO: Step 6 - Register tools
// server.RegisterTool(
//     name: "get_cluster_info",
//     description: "Get information about the Surgewave cluster",
//     schema: JsonSerializer.Deserialize<JsonElement>("""
//     {
//         "type": "object",
//         "properties": {},
//         "required": []
//     }
//     """),
//     handler: async args =>
//     {
//         await Task.Delay(10); // Simulate async work
//         return new
//         {
//             bootstrapServers = server.BootstrapServers,
//             status = "running",
//             connectedAt = DateTime.UtcNow.ToString("O")
//         };
//     }
// );

// TODO: Step 7 - Test the server
// Console.WriteLine("\n--- Testing MCP Server ---\n");
//
// var initResponse = await server.HandleMessageAsync(
//     """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}"""
// );
// Console.WriteLine($"Initialize response received");
//
// var listResponse = await server.HandleMessageAsync(
//     """{"jsonrpc":"2.0","id":2,"method":"tools/list"}"""
// );
// Console.WriteLine($"Tools list response received");
//
// var callResponse = await server.HandleMessageAsync(
//     """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"get_cluster_info","arguments":{}}}"""
// );
// Console.WriteLine($"Tool call response received");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. MCP Server wraps Surgewave client for AI access");
Console.WriteLine("2. Tools are registered with schemas and handlers");
Console.WriteLine("3. Server handles protocol lifecycle automatically");
Console.WriteLine("4. Surgewave connection is shared across tool invocations");
Console.WriteLine("5. Graceful shutdown ensures clean resource cleanup");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Record definitions for JSON-RPC
public record JsonRpcRequest(
    [property: JsonPropertyName("jsonrpc")] string Jsonrpc,
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("params")] JsonElement? Params
);

public record JsonRpcResponse(
    [property: JsonPropertyName("jsonrpc")] string Jsonrpc,
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("result")] JsonElement? Result,
    [property: JsonPropertyName("error")] JsonRpcError? Error
);

public record JsonRpcError(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("data")] JsonElement? Data
);
