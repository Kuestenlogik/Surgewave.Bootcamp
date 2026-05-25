# Lesson 23.2: Surgewave MCP Server Setup

## Overview

Learn how to set up and configure Surgewave's MCP Server to expose Surgewave functionality to AI assistants. Understand the server lifecycle, transport configuration, and how to integrate with a running Surgewave cluster.

## Learning Objectives

- Set up a Surgewave MCP Server with stdio transport
- Configure HTTP/SSE transport for remote access
- Manage server lifecycle and graceful shutdown
- Connect the MCP server to a Surgewave runtime
- Handle server events and logging

## Surgewave MCP Server Architecture

```mermaid
graph TD
    Client["AI Client\n(Claude Desktop, VS Code, etc.)"] -->|JSON-RPC\n(stdio or HTTP/SSE)| MCP["SurgewaveMcpServer\n(Tool Registry, Transport, Surgewave Client)"]
    MCP --> Producer["SurgewaveProducer"]
    MCP --> Consumer["SurgewaveConsumer"]
    Producer & Consumer --> Runtime["Surgewave Runtime (Broker)"]
```

## Transport Options

**STDIO Transport** — run as a subprocess from the AI client:

```csharp
var server = new SurgewaveMcpServer(options =>
{
    options.Transport = McpTransport.Stdio;
    options.ServerName = "surgewave-mcp";
    options.ServerVersion = "1.0.0";
});
```

Claude Desktop config: `{ "mcpServers": { "surgewave": { "command": "dotnet", "args": ["run", "--project", "path/to/server"] } } }`

**HTTP/SSE Transport** — connect via HTTP from any client (endpoint: `http://localhost:8080/mcp`):

```csharp
var server = new SurgewaveMcpServer(options =>
{
    options.Transport = McpTransport.HttpSse;
    options.HttpPort = 8080;
    options.EnableCors = true;
});
```

## Server Lifecycle

1. **Create server:** `new SurgewaveMcpServer(options => { ... })`
2. **Register tools:** `server.RegisterTool("surgewave_produce", ProduceHandler)`
3. **Connect to Surgewave:** `await server.ConnectToSurgewaveAsync(bootstrapServers)`
4. **Start server:** `await server.StartAsync()`
5. **Handle requests (automatic):** `initialize` → capabilities; `tools/list` → registered tools; `tools/call` → tool handlers
6. **Graceful shutdown:** `await server.StopAsync()` — completes in-flight requests, closes Surgewave connections, releases resources

## Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| Transport | stdio or HttpSse | stdio |
| ServerName | Name reported to clients | "surgewave-mcp" |
| ServerVersion | Version reported to clients | "1.0.0" |
| HttpPort | Port for HTTP transport | 8080 |
| EnableCors | Allow cross-origin requests | false |
| MaxConcurrentRequests | Parallel request limit | 10 |
| RequestTimeout | Timeout for tool execution | 30s |

## Exercise

In this exercise, you will:

1. Create a Surgewave MCP server with stdio transport
2. Start an embedded Surgewave runtime
3. Connect the MCP server to Surgewave
4. Register a simple tool
5. Simulate the MCP initialization and tool call

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
