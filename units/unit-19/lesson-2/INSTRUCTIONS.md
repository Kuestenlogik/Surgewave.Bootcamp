# Lesson 23.2: Instructions

Follow these step-by-step instructions to set up a Surgewave MCP Server.

## Step 1: Start the Surgewave Runtime

Create an embedded Surgewave broker:

```csharp
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)  // Dynamic port
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");
var bootstrapServers = $"localhost:{runtime.Port}";
```

## Step 2: Create MCP Server Options

Define the server configuration:

```csharp
public class McpServerOptions
{
    public string ServerName { get; set; } = "surgewave-mcp";
    public string ServerVersion { get; set; } = "1.0.0";
    public string ProtocolVersion { get; set; } = "2024-11-05";
    public int MaxConcurrentRequests { get; set; } = 10;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
```

## Step 3: Create the MCP Server Class

Build a simple MCP server that handles the protocol:

```csharp
public class SimpleMcpServer
{
    private readonly McpServerOptions _options;
    private readonly Dictionary<string, Func<JsonElement?, Task<object>>> _tools = new();
    private string? _bootstrapServers;

    public SimpleMcpServer(McpServerOptions options)
    {
        _options = options;
    }

    public void ConnectToSurgewave(string bootstrapServers)
    {
        _bootstrapServers = bootstrapServers;
        Console.WriteLine($"[MCP] Connected to Surgewave at {bootstrapServers}");
    }

    public void RegisterTool(string name, string description, JsonElement schema,
        Func<JsonElement?, Task<object>> handler)
    {
        _tools[name] = handler;
        Console.WriteLine($"[MCP] Registered tool: {name}");
    }

    public string? BootstrapServers => _bootstrapServers;
    public IReadOnlyDictionary<string, Func<JsonElement?, Task<object>>> Tools => _tools;
}
```

## Step 4: Implement Message Handling

Add protocol handling to the server:

```csharp
public async Task<JsonRpcResponse?> HandleMessageAsync(string json)
{
    var request = JsonSerializer.Deserialize<JsonRpcRequest>(json, _jsonOptions);
    if (request == null) return CreateParseError();

    return request.Method switch
    {
        "initialize" => HandleInitialize(request),
        "tools/list" => HandleToolsList(request),
        "tools/call" => await HandleToolCallAsync(request),
        "notifications/initialized" => null,
        _ => CreateMethodNotFound(request)
    };
}

private JsonRpcResponse HandleInitialize(JsonRpcRequest request)
{
    var result = new
    {
        protocolVersion = _options.ProtocolVersion,
        capabilities = new { tools = new { } },
        serverInfo = new
        {
            name = _options.ServerName,
            version = _options.ServerVersion
        }
    };

    return CreateSuccess(request.Id, result);
}
```

## Step 5: Register Surgewave Tools

Add tools that interact with Surgewave:

```csharp
// Register a "get_cluster_info" tool
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
        return new
        {
            bootstrapServers = server.BootstrapServers,
            status = "running",
            connectedAt = DateTime.UtcNow.ToString("O")
        };
    }
);
```

## Step 6: Test the Server

Simulate MCP client interactions:

```csharp
Console.WriteLine("\n--- Testing MCP Server ---\n");

// Test initialize
var initResponse = await server.HandleMessageAsync(
    """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}"""
);
Console.WriteLine($"Initialize: {JsonSerializer.Serialize(initResponse)}");

// Test tools/list
var listResponse = await server.HandleMessageAsync(
    """{"jsonrpc":"2.0","id":2,"method":"tools/list"}"""
);
Console.WriteLine($"Tools list: {JsonSerializer.Serialize(listResponse)}");

// Test tool call
var callResponse = await server.HandleMessageAsync(
    """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"get_cluster_info","arguments":{}}}"""
);
Console.WriteLine($"Tool call: {JsonSerializer.Serialize(callResponse)}");
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Surgewave runtime integration
- MCP server with tool registration
- Protocol message handling
- Tool execution with Surgewave connection
- Error handling

## Sample Output

```
=== Surgewave Bootcamp: Lesson 23.2 - Surgewave MCP Server Setup ===

Surgewave broker started on port 12345

--- MCP Server Setup ---
[MCP] Server created: surgewave-mcp v1.0.0
[MCP] Connected to Surgewave at localhost:12345
[MCP] Registered tool: get_cluster_info
[MCP] Registered tool: ping

--- Testing MCP Server ---

Initialize: {"jsonrpc":"2.0","id":1,"result":{"protocolVersion":"2024-11-05",...}}
Tools list: {"jsonrpc":"2.0","id":2,"result":{"tools":[{"name":"get_cluster_info",...}]}}
Tool call: {"jsonrpc":"2.0","id":3,"result":{"content":[{"type":"text","text":"Cluster: localhost:12345, Status: running"}]}}

--- Key Takeaways ---
1. MCP Server wraps Surgewave client for AI access
2. Tools are registered with schemas and handlers
3. Server handles protocol lifecycle automatically
4. Surgewave connection is shared across tool invocations
5. Graceful shutdown ensures clean resource cleanup

Lesson complete!
```

## Knowledge Check

1. What are the two transport options for MCP servers?
2. Why do we use dynamic port (0) for the embedded Surgewave runtime?
3. What happens if a tool handler throws an exception?
4. How does the MCP server share the Surgewave connection across tools?
5. What should happen during graceful shutdown?

---

**Next:** [Lesson 23.3: MCP Tools Implementation](../lesson-3/README.md)
