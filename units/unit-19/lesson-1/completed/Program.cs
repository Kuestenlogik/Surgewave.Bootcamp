// Surgewave Bootcamp - Unit 23, Lesson 1: MCP Protocol Basics
// COMPLETED SOLUTION

using System.Text.Json;
using System.Text.Json.Serialization;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 23.1 - MCP Protocol Basics ===");
Console.WriteLine();

// JSON serialization options
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

// Step 1 - JSON-RPC message types are defined at the bottom of the file

// Step 2 & 3 - Create tool definitions
var tools = new List<McpTool>
{
    new McpTool(
        Name: "hello_world",
        Description: "Returns a greeting message",
        InputSchema: JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "name": {
                    "type": "string",
                    "description": "Name to greet"
                }
            },
            "required": ["name"]
        }
        """)
    ),
    new McpTool(
        Name: "echo",
        Description: "Echoes back the provided message",
        InputSchema: JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "message": {
                    "type": "string",
                    "description": "Message to echo"
                }
            },
            "required": ["message"]
        }
        """)
    )
};

Console.WriteLine("--- Tool Definitions ---");
foreach (var tool in tools)
{
    Console.WriteLine($"Tool: {tool.Name}");
    Console.WriteLine($"  Description: {tool.Description}");
    Console.WriteLine($"  Schema: {tool.InputSchema}");
}

// Step 4 - Message handlers
JsonRpcResponse HandleInitialize(JsonRpcRequest request)
{
    Console.WriteLine("[Handler] Processing initialize request");

    var result = new
    {
        protocolVersion = "2024-11-05",
        capabilities = new
        {
            tools = new { },
            resources = new { }
        },
        serverInfo = new
        {
            name = "surgewave-mcp-demo",
            version = "1.0.0"
        }
    };

    return new JsonRpcResponse(
        Jsonrpc: "2.0",
        Id: request.Id,
        Result: JsonSerializer.SerializeToElement(result, jsonOptions),
        Error: null
    );
}

JsonRpcResponse HandleToolsList(JsonRpcRequest request)
{
    Console.WriteLine("[Handler] Listing available tools");

    var result = new
    {
        tools = tools.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            inputSchema = t.InputSchema
        })
    };

    return new JsonRpcResponse(
        Jsonrpc: "2.0",
        Id: request.Id,
        Result: JsonSerializer.SerializeToElement(result, jsonOptions),
        Error: null
    );
}

JsonRpcResponse HandleToolCall(JsonRpcRequest request)
{
    var toolName = request.Params?.GetProperty("name").GetString();
    var arguments = request.Params?.GetProperty("arguments");

    Console.WriteLine($"[Handler] Calling tool: {toolName}");

    if (toolName == "hello_world")
    {
        var name = arguments?.GetProperty("name").GetString() ?? "World";
        var greeting = $"Hello, {name}! Welcome to Surgewave MCP.";

        var result = new
        {
            content = new[] { new { type = "text", text = greeting } }
        };

        return new JsonRpcResponse(
            Jsonrpc: "2.0",
            Id: request.Id,
            Result: JsonSerializer.SerializeToElement(result, jsonOptions),
            Error: null
        );
    }

    if (toolName == "echo")
    {
        var message = arguments?.GetProperty("message").GetString() ?? "";

        var result = new
        {
            content = new[] { new { type = "text", text = $"Echo: {message}" } }
        };

        return new JsonRpcResponse(
            Jsonrpc: "2.0",
            Id: request.Id,
            Result: JsonSerializer.SerializeToElement(result, jsonOptions),
            Error: null
        );
    }

    // Tool not found
    Console.WriteLine($"[Handler] Tool not found: {toolName}");
    return new JsonRpcResponse(
        Jsonrpc: "2.0",
        Id: request.Id,
        Result: null,
        Error: new JsonRpcError(-32601, $"Tool not found: {toolName}", null)
    );
}

// Step 5 - Message router
JsonRpcResponse? HandleMessage(string json)
{
    var request = JsonSerializer.Deserialize<JsonRpcRequest>(json, jsonOptions);
    if (request == null)
    {
        return new JsonRpcResponse(
            Jsonrpc: "2.0",
            Id: null,
            Result: null,
            Error: new JsonRpcError(-32700, "Parse error", null)
        );
    }

    return request.Method switch
    {
        "initialize" => HandleInitialize(request),
        "tools/list" => HandleToolsList(request),
        "tools/call" => HandleToolCall(request),
        "notifications/initialized" => null, // Notifications don't get responses
        _ => new JsonRpcResponse(
            Jsonrpc: "2.0",
            Id: request.Id,
            Result: null,
            Error: new JsonRpcError(-32601, $"Method not found: {request.Method}", null)
        )
    };
}

// Step 6 - Simulate MCP session
Console.WriteLine("\n--- Simulating MCP Session ---\n");

var messages = new[]
{
    // 1. Client initiates connection
    """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{"tools":{}},"clientInfo":{"name":"demo-client","version":"1.0"}}}""",

    // 2. Client confirms initialization (notification - no response)
    """{"jsonrpc":"2.0","method":"notifications/initialized"}""",

    // 3. Client requests tool list
    """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""",

    // 4. Client calls hello_world tool
    """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"hello_world","arguments":{"name":"Surgewave Developer"}}}""",

    // 5. Client calls echo tool
    """{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"echo","arguments":{"message":"MCP is working!"}}}""",

    // 6. Client calls unknown tool (should return error)
    """{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"unknown_tool","arguments":{}}}""",

    // 7. Client calls unknown method (should return error)
    """{"jsonrpc":"2.0","id":6,"method":"unknown/method"}"""
};

foreach (var msg in messages)
{
    Console.WriteLine($"<-- {msg}");
    var response = HandleMessage(msg);
    if (response != null)
    {
        Console.WriteLine($"--> {JsonSerializer.Serialize(response, jsonOptions)}");
    }
    else
    {
        Console.WriteLine("(notification - no response)");
    }
    Console.WriteLine();
}

// Summary
Console.WriteLine("--- Protocol Summary ---");
Console.WriteLine($"Available tools: {tools.Count}");
Console.WriteLine($"Messages processed: {messages.Length}");
Console.WriteLine($"Notifications: 1");
Console.WriteLine($"Errors returned: 2 (unknown tool, unknown method)");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. MCP uses JSON-RPC 2.0 for all communication");
Console.WriteLine("2. Tools are functions the AI can invoke");
Console.WriteLine("3. The initialize handshake establishes protocol version and capabilities");
Console.WriteLine("4. Notifications don't require responses");
Console.WriteLine("5. Tool results use a content array with typed items");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Record definitions
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

public record McpTool(
    string Name,
    string Description,
    JsonElement InputSchema
);
