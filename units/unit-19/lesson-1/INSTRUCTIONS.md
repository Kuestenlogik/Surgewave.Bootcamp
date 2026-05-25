# Lesson 23.1: Instructions

Follow these step-by-step instructions to understand MCP protocol basics.

## Step 1: Define JSON-RPC Message Types

Create record types to represent JSON-RPC messages:

```csharp
// JSON-RPC request message
public record JsonRpcRequest(
    string Jsonrpc,
    int? Id,
    string Method,
    JsonElement? Params
);

// JSON-RPC response message
public record JsonRpcResponse(
    string Jsonrpc,
    int? Id,
    JsonElement? Result,
    JsonRpcError? Error
);

// JSON-RPC error object
public record JsonRpcError(
    int Code,
    string Message,
    JsonElement? Data
);
```

## Step 2: Create an MCP Tool Definition

Define a tool that the AI can invoke:

```csharp
public record McpTool(
    string Name,
    string Description,
    JsonElement InputSchema
);

// Create a "hello_world" tool
var helloTool = new McpTool(
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
);
```

## Step 3: Implement the Initialization Handshake

Handle the MCP initialization sequence:

```csharp
JsonRpcResponse HandleInitialize(JsonRpcRequest request)
{
    var result = new
    {
        protocolVersion = "2024-11-05",
        capabilities = new
        {
            tools = new { }  // We support tools
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
        Result: JsonSerializer.SerializeToElement(result),
        Error: null
    );
}
```

## Step 4: Handle Tools List Request

Return available tools when the client asks:

```csharp
JsonRpcResponse HandleToolsList(JsonRpcRequest request, List<McpTool> tools)
{
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
        Result: JsonSerializer.SerializeToElement(result),
        Error: null
    );
}
```

## Step 5: Handle Tool Invocation

Process a tool call and return results:

```csharp
JsonRpcResponse HandleToolCall(JsonRpcRequest request)
{
    // Extract tool name and arguments
    var toolName = request.Params?.GetProperty("name").GetString();
    var arguments = request.Params?.GetProperty("arguments");

    if (toolName == "hello_world")
    {
        var name = arguments?.GetProperty("name").GetString() ?? "World";
        var greeting = $"Hello, {name}! Welcome to Surgewave MCP.";

        var result = new
        {
            content = new[]
            {
                new { type = "text", text = greeting }
            }
        };

        return new JsonRpcResponse(
            Jsonrpc: "2.0",
            Id: request.Id,
            Result: JsonSerializer.SerializeToElement(result),
            Error: null
        );
    }

    // Tool not found
    return new JsonRpcResponse(
        Jsonrpc: "2.0",
        Id: request.Id,
        Result: null,
        Error: new JsonRpcError(-32601, $"Tool not found: {toolName}", null)
    );
}
```

## Step 6: Create a Message Router

Route incoming messages to the appropriate handler:

```csharp
JsonRpcResponse? HandleMessage(string json, List<McpTool> tools)
{
    var request = JsonSerializer.Deserialize<JsonRpcRequest>(json, jsonOptions);

    return request.Method switch
    {
        "initialize" => HandleInitialize(request),
        "tools/list" => HandleToolsList(request, tools),
        "tools/call" => HandleToolCall(request),
        "notifications/initialized" => null,  // No response for notifications
        _ => new JsonRpcResponse(
            Jsonrpc: "2.0",
            Id: request.Id,
            Result: null,
            Error: new JsonRpcError(-32601, $"Method not found: {request.Method}", null)
        )
    };
}
```

## Step 7: Simulate an MCP Session

Test the protocol flow:

```csharp
// Simulate MCP session
var messages = new[]
{
    // 1. Initialize
    """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05"}}""",

    // 2. Initialized notification (no response expected)
    """{"jsonrpc":"2.0","method":"notifications/initialized"}""",

    // 3. List tools
    """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""",

    // 4. Call tool
    """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"hello_world","arguments":{"name":"Surgewave Developer"}}}"""
};

foreach (var msg in messages)
{
    Console.WriteLine($"<-- {msg}");
    var response = HandleMessage(msg, tools);
    if (response != null)
    {
        Console.WriteLine($"--> {JsonSerializer.Serialize(response)}");
    }
    Console.WriteLine();
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- JSON-RPC message parsing and serialization
- MCP initialization handshake
- Tool definition and registration
- Tool invocation handling
- Error handling for unknown methods

## Sample Output

```
=== Surgewave Bootcamp: Lesson 23.1 - MCP Protocol Basics ===

--- Tool Definitions ---
Tool: hello_world
  Description: Returns a greeting message
  Schema: {"type":"object","properties":{"name":{"type":"string","description":"Name to greet"}},"required":["name"]}

--- Simulating MCP Session ---

<-- {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05"}}
--> {"Jsonrpc":"2.0","Id":1,"Result":{"protocolVersion":"2024-11-05","capabilities":{"tools":{}},"serverInfo":{"name":"surgewave-mcp-demo","version":"1.0.0"}},"Error":null}

<-- {"jsonrpc":"2.0","method":"notifications/initialized"}
(notification - no response)

<-- {"jsonrpc":"2.0","id":2,"method":"tools/list"}
--> {"Jsonrpc":"2.0","Id":2,"Result":{"tools":[{"name":"hello_world","description":"Returns a greeting message",...}]},"Error":null}

<-- {"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"hello_world","arguments":{"name":"Surgewave Developer"}}}
--> {"Jsonrpc":"2.0","Id":3,"Result":{"content":[{"type":"text","text":"Hello, Surgewave Developer! Welcome to Surgewave MCP."}]},"Error":null}

--- Key Takeaways ---
1. MCP uses JSON-RPC 2.0 for all communication
2. Tools are functions the AI can invoke
3. The initialize handshake establishes protocol version and capabilities
4. Notifications don't require responses
5. Tool results use a content array with typed items

Lesson complete!
```

## Knowledge Check

1. What is the difference between a JSON-RPC request and a notification?
2. What are the three main MCP primitives?
3. Why does the MCP initialization use a two-step handshake?
4. What should a server return if a tool is not found?
5. How does the content array format support different response types?

---

**Next:** [Lesson 23.2: Surgewave MCP Server Setup](../lesson-2/README.md)
