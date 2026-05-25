# Lesson 23.4: Instructions

Follow these step-by-step instructions to integrate Surgewave with Claude Desktop.

## Step 1: Create a Standalone MCP Server Application

Create a server that runs as a subprocess:

```csharp
using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

// Read configuration from environment
var bootstrapServers = Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS") ?? "localhost:9092";

// Initialize Surgewave connections
var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = $"mcp-agent-{Guid.NewGuid():N}";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

// Create MCP server
var server = new StdioMcpServer("surgewave-mcp", "1.0.0");

// Register tools
server.RegisterTools(producer, consumer);

// Run the server (blocks until stdin closes)
await server.RunAsync();
```

## Step 2: Implement Stdio Transport

Handle JSON-RPC over stdin/stdout:

```csharp
public class StdioMcpServer
{
    private readonly string _name;
    private readonly string _version;
    private readonly Dictionary<string, McpTool> _tools = new();

    public async Task RunAsync()
    {
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();
        using var reader = new StreamReader(stdin);
        using var writer = new StreamWriter(stdout) { AutoFlush = true };

        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) break; // EOF - Claude closed the connection

            var response = await HandleMessageAsync(line);
            if (response != null)
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(response));
            }
        }
    }
}
```

## Step 3: Configure Claude Desktop

Create the configuration file:

**macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
**Windows**: `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
    "mcpServers": {
        "surgewave": {
            "command": "dotnet",
            "args": [
                "run",
                "--project",
                "/path/to/bootcamp/unit-23/lesson-4/completed"
            ],
            "env": {
                "Surgewave_BOOTSTRAP_SERVERS": "localhost:9092"
            }
        }
    }
}
```

## Step 4: Add Tool Discovery

Implement the tools/list handler:

```csharp
private JsonRpcResponse HandleToolsList(JsonRpcRequest request)
{
    var tools = _tools.Values.Select(t => new
    {
        name = t.Name,
        description = t.Description,
        inputSchema = t.Schema
    });

    return CreateSuccess(request.Id, new { tools });
}
```

## Step 5: Add Rate Limiting

Protect against excessive requests:

```csharp
public class RateLimiter
{
    private readonly int _maxRequests;
    private readonly TimeSpan _window;
    private readonly Queue<DateTime> _requests = new();

    public RateLimiter(int maxRequests, TimeSpan window)
    {
        _maxRequests = maxRequests;
        _window = window;
    }

    public bool TryAcquire()
    {
        var now = DateTime.UtcNow;
        var windowStart = now - _window;

        // Remove old requests
        while (_requests.Count > 0 && _requests.Peek() < windowStart)
            _requests.Dequeue();

        if (_requests.Count >= _maxRequests)
            return false;

        _requests.Enqueue(now);
        return true;
    }
}
```

## Step 6: Add Logging for Debugging

Log all MCP interactions:

```csharp
private void LogRequest(string method, string? toolName = null)
{
    var timestamp = DateTime.UtcNow.ToString("O");
    var message = toolName != null
        ? $"[{timestamp}] MCP: {method} -> {toolName}"
        : $"[{timestamp}] MCP: {method}";

    File.AppendAllText("mcp-debug.log", message + Environment.NewLine);
}
```

## Step 7: Test the Integration

Restart Claude Desktop and try these prompts:

1. "What Surgewave tools are available?"
2. "List all topics in the cluster"
3. "Send a message to the test topic with content 'Hello from Claude'"
4. "Read the last 3 messages from the orders topic"
5. "What's the current state of consumer group mcp-agent?"

## Complete Solution

See `completed/Program.cs` for a production-ready MCP server with:

- Full stdio transport implementation
- All Surgewave tools registered
- Rate limiting
- Error handling
- Debug logging

## Sample Interaction

```
User: "Send a test message to the orders topic"

Claude: I'll send a test message to the orders topic.

[Calling surgewave_produce with topic="orders", value={"test": true}]

The message was successfully sent to the 'orders' topic at partition 0, offset 15.

User: "Now read it back"

Claude: Let me read the message from the orders topic.

[Calling surgewave_consume with topic="orders", count=1]

Here's the message I found:
- Key: auto-generated-uuid
- Value: {"test": true}
- Partition: 0
- Offset: 15
```

## Knowledge Check

1. Why does Claude Desktop use stdio transport instead of HTTP?
2. Where is the Claude Desktop config file located on your OS?
3. How do you pass Surgewave credentials to the MCP server?
4. What happens when the MCP server process crashes?
5. How can you debug MCP communication issues?

---

**Next:** [Lesson 23.5: Building Custom AI Agents](../lesson-5/README.md)
