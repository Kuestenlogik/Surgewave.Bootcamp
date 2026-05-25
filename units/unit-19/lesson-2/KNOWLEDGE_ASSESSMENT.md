# Knowledge Assessment: Lesson 23.2 - Surgewave MCP Server Setup

## Multiple Choice

### 1. Which transport is best for integrating with Claude Desktop?
- [ ] A) HTTP/SSE
- [ ] B) WebSocket
- [ ] C) stdio
- [ ] D) gRPC

<details>
<summary>Answer</summary>
**C)** Claude Desktop uses stdio transport, launching the MCP server as a subprocess and communicating via standard input/output streams.
</details>

### 2. What does the MCP server return in the "initialize" response?
- [ ] A) List of available tools
- [ ] B) Server capabilities, protocol version, and server info
- [ ] C) Authentication token
- [ ] D) Surgewave cluster metadata

<details>
<summary>Answer</summary>
**B)** The initialize response includes the protocol version, server capabilities (what features it supports like tools, resources), and server info (name and version).
</details>

### 3. Why would you use HTTP/SSE transport instead of stdio?
- [ ] A) It's faster than stdio
- [ ] B) It supports remote access and multiple concurrent clients
- [ ] C) It's required for tool registration
- [ ] D) It's the only transport that works with Surgewave

<details>
<summary>Answer</summary>
**B)** HTTP/SSE transport allows remote clients to connect over the network and supports multiple concurrent clients, whereas stdio only works with a single local client.
</details>

### 4. What is the purpose of the RequestTimeout option?
- [ ] A) Limit how long clients can stay connected
- [ ] B) Set maximum time for tool execution before returning an error
- [ ] C) Define how often to ping the client
- [ ] D) Specify Surgewave connection timeout

<details>
<summary>Answer</summary>
**B)** RequestTimeout sets the maximum time allowed for a tool handler to complete execution. If exceeded, the server returns a timeout error to the client.
</details>

### 5. What happens when the MCP server receives a "notifications/initialized" message?
- [ ] A) It returns an error
- [ ] B) It returns the list of tools
- [ ] C) It does not send a response (it's a notification)
- [ ] D) It disconnects the client

<details>
<summary>Answer</summary>
**C)** Notifications in JSON-RPC do not expect a response. The "notifications/initialized" message signals the client is ready, but the server should not reply.
</details>

## True/False

### 6. The MCP server must be restarted to register new tools.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Tools can be registered dynamically at runtime. However, clients may need to call "tools/list" again to discover newly registered tools.
</details>

### 7. Each tool invocation creates a new Surgewave producer/consumer connection.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - A well-designed MCP server should share Surgewave connections (producer, consumer) across tool invocations for efficiency. Creating connections per-request would be wasteful and slow.
</details>

## Code Challenge

### 8. What's wrong with this tool registration?

```csharp
server.RegisterTool(
    name: "send_message",
    description: "Send a message to a topic",
    schema: JsonSerializer.Deserialize<JsonElement>("""
    {
        "type": "object",
        "properties": {
            "topic": { "type": "string" },
            "message": { "type": "string" }
        }
    }
    """),
    handler: args =>
    {
        var topic = args?.GetProperty("topic").GetString();
        var message = args?.GetProperty("message").GetString();
        producer.ProduceAsync(topic, "key", message);
        return Task.FromResult<object>(new { success = true });
    }
);
```

<details>
<summary>Answer</summary>
Two issues:

1. **Missing `required` array in schema**: The schema should specify which properties are required:
```json
{
    "type": "object",
    "properties": {
        "topic": { "type": "string" },
        "message": { "type": "string" }
    },
    "required": ["topic", "message"]
}
```

2. **Not awaiting the async operation**: `ProduceAsync` is not awaited, which means the method returns before the message is sent, and errors won't be caught:
```csharp
handler: async args =>
{
    var topic = args?.GetProperty("topic").GetString();
    var message = args?.GetProperty("message").GetString();
    await producer.ProduceAsync(topic, "key", message);  // await!
    return new { success = true };
}
```
</details>

### 9. Complete the graceful shutdown implementation:

```csharp
public async Task StopAsync()
{
    Console.WriteLine("[MCP] Stopping server...");

    // TODO: Cancel in-flight requests
    _____________________

    // TODO: Wait for pending operations to complete
    _____________________

    // TODO: Dispose Surgewave connections
    _____________________

    Console.WriteLine("[MCP] Server stopped");
}
```

<details>
<summary>Answer</summary>

```csharp
public async Task StopAsync()
{
    Console.WriteLine("[MCP] Stopping server...");

    // Cancel in-flight requests
    _cancellationTokenSource.Cancel();

    // Wait for pending operations to complete (with timeout)
    try
    {
        await Task.WhenAll(_pendingTasks).WaitAsync(TimeSpan.FromSeconds(5));
    }
    catch (TimeoutException)
    {
        Console.WriteLine("[MCP] Timeout waiting for pending tasks");
    }

    // Dispose Surgewave connections
    if (_producer != null) await _producer.DisposeAsync();
    if (_consumer != null) await _consumer.DisposeAsync();

    Console.WriteLine("[MCP] Server stopped");
}
```

Key points:
- Use CancellationToken to signal shutdown to pending operations
- Wait for pending tasks with a timeout to avoid hanging
- Properly dispose all Surgewave client resources
</details>

## Score
- 9/9: Excellent!
- 7-8/9: Good understanding
- 5-6/9: Review the lesson
- <5/9: Re-do the lesson
