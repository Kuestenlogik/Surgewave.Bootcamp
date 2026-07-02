// Surgewave Bootcamp - Unit 23, Lesson 2: Surgewave MCP Server Setup
// COMPLETED SOLUTION

using System.Text.Json;
using System.Text.Json.Serialization;
using Kuestenlogik.Surgewave.Core.Storage;
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

// Step 1 - Start the Surgewave runtime
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");
var bootstrapServers = $"localhost:{runtime.Port}";

// Step 2-4 - Create MCP server
Console.WriteLine("\n--- MCP Server Setup ---");
var options = new McpServerOptions();
var server = new SimpleMcpServer(options, jsonOptions);
Console.WriteLine($"[MCP] Server created: {options.ServerName} v{options.ServerVersion}");

// Step 5 - Connect to Surgewave
server.ConnectToSurgewave(bootstrapServers);

// Step 6 - Register tools
server.RegisterTool(
    name: "get_cluster_info",
    description: "Get information about the Surgewave cluster",
    schema: JsonSerializer.Deserialize<JsonElement>("""
    {
        "type": "object",
        "properties": {},
        "required": []
    }
    """),
    handler: async args =>
    {
        await Task.Delay(10); // Simulate async work
        return new
        {
            bootstrapServers = server.BootstrapServers,
            status = "running",
            connectedAt = DateTime.UtcNow.ToString("O")
        };
    }
);

server.RegisterTool(
    name: "ping",
    description: "Check if the MCP server is responsive",
    schema: JsonSerializer.Deserialize<JsonElement>("""
    {
        "type": "object",
        "properties": {
            "echo": {
                "type": "string",
                "description": "Optional message to echo back"
            }
        },
        "required": []
    }
    """),
    handler: async args =>
    {
        await Task.CompletedTask;
        var echo = "";
        if (args.HasValue && args.Value.TryGetProperty("echo", out var echoElement))
        {
            echo = echoElement.GetString() ?? "";
        }
        return new
        {
            pong = true,
            echo = echo,
            timestamp = DateTime.UtcNow.ToString("O")
        };
    }
);

// Step 7 - Test the server
Console.WriteLine("\n--- Testing MCP Server ---\n");

// Test initialize
var initResponse = await server.HandleMessageAsync(
    """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05"}}"""
);
Console.WriteLine($"[Test] Initialize:");
Console.WriteLine($"       Response: {FormatResponse(initResponse)}");

// Test initialized notification (no response expected)
var notifyResponse = await server.HandleMessageAsync(
    """{"jsonrpc":"2.0","method":"notifications/initialized"}"""
);
Console.WriteLine($"[Test] Initialized notification:");
Console.WriteLine($"       Response: {(notifyResponse == null ? "(none - as expected)" : "unexpected response")}");

// Test tools/list
var listResponse = await server.HandleMessageAsync(
    """{"jsonrpc":"2.0","id":2,"method":"tools/list"}"""
);
Console.WriteLine($"[Test] Tools list:");
Console.WriteLine($"       Response: {FormatResponse(listResponse)}");

// Test tool call - get_cluster_info
var clusterResponse = await server.HandleMessageAsync(
    """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"get_cluster_info","arguments":{}}}"""
);
Console.WriteLine($"[Test] Call get_cluster_info:");
Console.WriteLine($"       Response: {FormatResponse(clusterResponse)}");

// Test tool call - ping with echo
var pingResponse = await server.HandleMessageAsync(
    """{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"ping","arguments":{"echo":"Hello MCP!"}}}"""
);
Console.WriteLine($"[Test] Call ping:");
Console.WriteLine($"       Response: {FormatResponse(pingResponse)}");

// Test unknown tool
var unknownResponse = await server.HandleMessageAsync(
    """{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"unknown_tool","arguments":{}}}"""
);
Console.WriteLine($"[Test] Call unknown_tool:");
Console.WriteLine($"       Response: {FormatResponse(unknownResponse)}");

// Test unknown method
var unknownMethodResponse = await server.HandleMessageAsync(
    """{"jsonrpc":"2.0","id":6,"method":"unknown/method"}"""
);
Console.WriteLine($"[Test] Unknown method:");
Console.WriteLine($"       Response: {FormatResponse(unknownMethodResponse)}");

// Summary
Console.WriteLine("\n--- Server Statistics ---");
Console.WriteLine($"Server name: {options.ServerName}");
Console.WriteLine($"Server version: {options.ServerVersion}");
Console.WriteLine($"Registered tools: {server.ToolCount}");
Console.WriteLine($"Surgewave connection: {server.BootstrapServers}");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. MCP Server wraps Surgewave client for AI access");
Console.WriteLine("2. Tools are registered with schemas and handlers");
Console.WriteLine("3. Server handles protocol lifecycle automatically");
Console.WriteLine("4. Surgewave connection is shared across tool invocations");
Console.WriteLine("5. Graceful shutdown ensures clean resource cleanup");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

string FormatResponse(JsonRpcResponse? response)
{
    if (response == null) return "(null)";
    var json = JsonSerializer.Serialize(response, jsonOptions);
    return json.Length > 100 ? json[..100] + "..." : json;
}

// MCP Server Options
public class McpServerOptions
{
    public string ServerName { get; set; } = "surgewave-mcp";
    public string ServerVersion { get; set; } = "1.0.0";
    public string ProtocolVersion { get; set; } = "2024-11-05";
    public int MaxConcurrentRequests { get; set; } = 10;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

// Tool definition
public record McpToolDefinition(
    string Name,
    string Description,
    JsonElement Schema,
    Func<JsonElement?, Task<object>> Handler
);

// Simple MCP Server implementation
public class SimpleMcpServer
{
    private readonly McpServerOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Dictionary<string, McpToolDefinition> _tools = new();
    private string? _bootstrapServers;

    public SimpleMcpServer(McpServerOptions options, JsonSerializerOptions jsonOptions)
    {
        _options = options;
        _jsonOptions = jsonOptions;
    }

    public string? BootstrapServers => _bootstrapServers;
    public int ToolCount => _tools.Count;

    public void ConnectToSurgewave(string bootstrapServers)
    {
        _bootstrapServers = bootstrapServers;
        Console.WriteLine($"[MCP] Connected to Surgewave at {bootstrapServers}");
    }

    public void RegisterTool(string name, string description, JsonElement schema,
        Func<JsonElement?, Task<object>> handler)
    {
        _tools[name] = new McpToolDefinition(name, description, schema, handler);
        Console.WriteLine($"[MCP] Registered tool: {name}");
    }

    public async Task<JsonRpcResponse?> HandleMessageAsync(string json)
    {
        JsonRpcRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<JsonRpcRequest>(json, _jsonOptions);
        }
        catch
        {
            return CreateError(null, -32700, "Parse error");
        }

        if (request == null) return CreateError(null, -32700, "Parse error");

        return request.Method switch
        {
            "initialize" => HandleInitialize(request),
            "tools/list" => HandleToolsList(request),
            "tools/call" => await HandleToolCallAsync(request),
            "notifications/initialized" => null, // No response for notifications
            _ => CreateError(request.Id, -32601, $"Method not found: {request.Method}")
        };
    }

    private JsonRpcResponse HandleInitialize(JsonRpcRequest request)
    {
        var result = new
        {
            protocolVersion = _options.ProtocolVersion,
            capabilities = new
            {
                tools = new { }
            },
            serverInfo = new
            {
                name = _options.ServerName,
                version = _options.ServerVersion
            }
        };

        return CreateSuccess(request.Id, result);
    }

    private JsonRpcResponse HandleToolsList(JsonRpcRequest request)
    {
        var result = new
        {
            tools = _tools.Values.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                inputSchema = t.Schema
            })
        };

        return CreateSuccess(request.Id, result);
    }

    private async Task<JsonRpcResponse> HandleToolCallAsync(JsonRpcRequest request)
    {
        string? toolName = null;
        JsonElement? arguments = null;

        try
        {
            if (request.Params.HasValue)
            {
                toolName = request.Params.Value.GetProperty("name").GetString();
                if (request.Params.Value.TryGetProperty("arguments", out var args))
                {
                    arguments = args;
                }
            }
        }
        catch
        {
            return CreateError(request.Id, -32602, "Invalid params");
        }

        if (string.IsNullOrEmpty(toolName))
        {
            return CreateError(request.Id, -32602, "Missing tool name");
        }

        if (!_tools.TryGetValue(toolName, out var tool))
        {
            return CreateError(request.Id, -32601, $"Tool not found: {toolName}");
        }

        try
        {
            var handlerResult = await tool.Handler(arguments);
            var resultText = JsonSerializer.Serialize(handlerResult, _jsonOptions);

            var result = new
            {
                content = new[]
                {
                    new { type = "text", text = resultText }
                }
            };

            return CreateSuccess(request.Id, result);
        }
        catch (Exception ex)
        {
            return CreateError(request.Id, -32603, $"Tool execution error: {ex.Message}");
        }
    }

    private JsonRpcResponse CreateSuccess(int? id, object result)
    {
        return new JsonRpcResponse(
            Jsonrpc: "2.0",
            Id: id,
            Result: JsonSerializer.SerializeToElement(result, _jsonOptions),
            Error: null
        );
    }

    private JsonRpcResponse CreateError(int? id, int code, string message)
    {
        return new JsonRpcResponse(
            Jsonrpc: "2.0",
            Id: id,
            Result: null,
            Error: new JsonRpcError(code, message, null)
        );
    }
}

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
