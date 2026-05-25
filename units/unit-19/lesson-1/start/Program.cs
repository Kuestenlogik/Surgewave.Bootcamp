// Surgewave Bootcamp - Unit 23, Lesson 1: MCP Protocol Basics
//
// In this lesson, you'll learn:
// - JSON-RPC 2.0 message format
// - MCP primitives: tools, resources, prompts
// - MCP initialization handshake
// - Basic message routing
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text.Json;
using System.Text.Json.Serialization;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 23.1 - MCP Protocol Basics ===");
Console.WriteLine();

// JSON serialization options
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
};

// TODO: Step 1 - Define JSON-RPC message types
// public record JsonRpcRequest(
//     string Jsonrpc,
//     int? Id,
//     string Method,
//     JsonElement? Params
// );
//
// public record JsonRpcResponse(
//     string Jsonrpc,
//     int? Id,
//     JsonElement? Result,
//     JsonRpcError? Error
// );
//
// public record JsonRpcError(
//     int Code,
//     string Message,
//     JsonElement? Data
// );

// TODO: Step 2 - Define MCP Tool record
// public record McpTool(
//     string Name,
//     string Description,
//     JsonElement InputSchema
// );

// TODO: Step 3 - Create a tool list
// var tools = new List<McpTool>
// {
//     new McpTool(
//         Name: "hello_world",
//         Description: "Returns a greeting message",
//         InputSchema: JsonSerializer.Deserialize<JsonElement>("""
//         {
//             "type": "object",
//             "properties": {
//                 "name": {
//                     "type": "string",
//                     "description": "Name to greet"
//                 }
//             },
//             "required": ["name"]
//         }
//         """)
//     )
// };
//
// Console.WriteLine("--- Tool Definitions ---");
// foreach (var tool in tools)
// {
//     Console.WriteLine($"Tool: {tool.Name}");
//     Console.WriteLine($"  Description: {tool.Description}");
//     Console.WriteLine($"  Schema: {tool.InputSchema}");
// }

// TODO: Step 4 - Implement message handlers
// JsonRpcResponse HandleInitialize(JsonRpcRequest request)
// {
//     var result = new
//     {
//         protocolVersion = "2024-11-05",
//         capabilities = new { tools = new { } },
//         serverInfo = new { name = "surgewave-mcp-demo", version = "1.0.0" }
//     };
//
//     return new JsonRpcResponse(
//         Jsonrpc: "2.0",
//         Id: request.Id,
//         Result: JsonSerializer.SerializeToElement(result, jsonOptions),
//         Error: null
//     );
// }
//
// JsonRpcResponse HandleToolsList(JsonRpcRequest request)
// {
//     var result = new
//     {
//         tools = tools.Select(t => new
//         {
//             name = t.Name,
//             description = t.Description,
//             inputSchema = t.InputSchema
//         })
//     };
//
//     return new JsonRpcResponse(
//         Jsonrpc: "2.0",
//         Id: request.Id,
//         Result: JsonSerializer.SerializeToElement(result, jsonOptions),
//         Error: null
//     );
// }
//
// JsonRpcResponse HandleToolCall(JsonRpcRequest request)
// {
//     var toolName = request.Params?.GetProperty("name").GetString();
//     var arguments = request.Params?.GetProperty("arguments");
//
//     if (toolName == "hello_world")
//     {
//         var name = arguments?.GetProperty("name").GetString() ?? "World";
//         var greeting = $"Hello, {name}! Welcome to Surgewave MCP.";
//
//         var result = new
//         {
//             content = new[] { new { type = "text", text = greeting } }
//         };
//
//         return new JsonRpcResponse(
//             Jsonrpc: "2.0",
//             Id: request.Id,
//             Result: JsonSerializer.SerializeToElement(result, jsonOptions),
//             Error: null
//         );
//     }
//
//     return new JsonRpcResponse(
//         Jsonrpc: "2.0",
//         Id: request.Id,
//         Result: null,
//         Error: new JsonRpcError(-32601, $"Tool not found: {toolName}", null)
//     );
// }

// TODO: Step 5 - Create message router
// JsonRpcResponse? HandleMessage(string json)
// {
//     var request = JsonSerializer.Deserialize<JsonRpcRequest>(json, jsonOptions);
//     if (request == null) return null;
//
//     return request.Method switch
//     {
//         "initialize" => HandleInitialize(request),
//         "tools/list" => HandleToolsList(request),
//         "tools/call" => HandleToolCall(request),
//         "notifications/initialized" => null,
//         _ => new JsonRpcResponse(
//             Jsonrpc: "2.0",
//             Id: request.Id,
//             Result: null,
//             Error: new JsonRpcError(-32601, $"Method not found: {request.Method}", null)
//         )
//     };
// }

// TODO: Step 6 - Simulate MCP session
// Console.WriteLine("\n--- Simulating MCP Session ---\n");
//
// var messages = new[]
// {
//     """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05"}}""",
//     """{"jsonrpc":"2.0","method":"notifications/initialized"}""",
//     """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""",
//     """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"hello_world","arguments":{"name":"Surgewave Developer"}}}""",
//     """{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"unknown_tool","arguments":{}}}"""
// };
//
// foreach (var msg in messages)
// {
//     Console.WriteLine($"<-- {msg}");
//     var response = HandleMessage(msg);
//     if (response != null)
//     {
//         Console.WriteLine($"--> {JsonSerializer.Serialize(response, jsonOptions)}");
//     }
//     else
//     {
//         Console.WriteLine("(notification - no response)");
//     }
//     Console.WriteLine();
// }

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. MCP uses JSON-RPC 2.0 for all communication");
Console.WriteLine("2. Tools are functions the AI can invoke");
Console.WriteLine("3. The initialize handshake establishes protocol version and capabilities");
Console.WriteLine("4. Notifications don't require responses");
Console.WriteLine("5. Tool results use a content array with typed items");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Record definitions go here after uncommenting
