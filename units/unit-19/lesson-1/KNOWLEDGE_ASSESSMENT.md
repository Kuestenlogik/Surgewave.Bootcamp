# Knowledge Assessment: Lesson 23.1 - MCP Protocol Basics

## Multiple Choice

### 1. What protocol does MCP use for communication?
- [ ] A) REST over HTTP
- [ ] B) gRPC
- [ ] C) JSON-RPC 2.0
- [ ] D) WebSocket binary frames

<details>
<summary>Answer</summary>
**C)** MCP uses JSON-RPC 2.0 for all communication between AI assistants and MCP servers. This provides a simple, lightweight protocol with request/response patterns and notifications.
</details>

### 2. What distinguishes a JSON-RPC notification from a request?
- [ ] A) Notifications use a different method prefix
- [ ] B) Notifications have no "id" field and expect no response
- [ ] C) Notifications must be sent over WebSocket
- [ ] D) Notifications have a "notification": true field

<details>
<summary>Answer</summary>
**B)** A JSON-RPC notification has no "id" field and the server should not send a response. This is used for one-way messages like the "initialized" notification after the handshake completes.
</details>

### 3. Which MCP primitive allows an AI to read data from an external system?
- [ ] A) Tools
- [ ] B) Resources
- [ ] C) Prompts
- [ ] D) Transports

<details>
<summary>Answer</summary>
**B)** Resources are data that the AI can read, similar to REST GET endpoints. Tools are for actions (write operations), and Prompts are templates for common operations.
</details>

### 4. What transport is best for local development with a single AI client?
- [ ] A) HTTP/SSE
- [ ] B) WebSocket
- [ ] C) stdio
- [ ] D) gRPC

<details>
<summary>Answer</summary>
**C)** stdio (standard input/output) transport is best for local development because it's simple, requires no network configuration, and works directly with process streams.
</details>

### 5. In the MCP initialization handshake, what does the client send after receiving the server's capabilities?
- [ ] A) An "acknowledge" request
- [ ] B) A "ready" request
- [ ] C) A "notifications/initialized" notification
- [ ] D) Nothing - the handshake is complete

<details>
<summary>Answer</summary>
**C)** After the server responds to "initialize" with its capabilities, the client sends a "notifications/initialized" notification to signal it's ready for normal operation.
</details>

## True/False

### 6. MCP Tools require an inputSchema that defines the expected arguments in JSON Schema format.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Every MCP tool must have an inputSchema that describes its parameters using JSON Schema. This allows AI assistants to understand what arguments the tool expects and validate inputs before calling.
</details>

### 7. HTTP/SSE transport uses Server-Sent Events for the server to push messages to the client.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - In HTTP/SSE transport, the client sends requests via HTTP POST, and the server uses Server-Sent Events (SSE) to push responses and notifications back to the client.
</details>

## Code Challenge

### 8. What's wrong with this JSON-RPC response?

```json
{
    "jsonrpc": "2.0",
    "id": 5,
    "result": { "success": true },
    "error": { "code": -1, "message": "Something went wrong" }
}
```

<details>
<summary>Answer</summary>
A JSON-RPC response cannot contain both `result` and `error` fields. It must have one or the other:

**Success response:**
```json
{
    "jsonrpc": "2.0",
    "id": 5,
    "result": { "success": true }
}
```

**Error response:**
```json
{
    "jsonrpc": "2.0",
    "id": 5,
    "error": { "code": -1, "message": "Something went wrong" }
}
```

The JSON-RPC specification requires that a response object must contain either `result` (for success) OR `error` (for failure), but never both.
</details>

### 9. Complete the MCP tool definition for a "get_topic_info" tool:

```csharp
var tool = new McpTool(
    Name: "_______________",
    Description: "_______________",
    InputSchema: JsonSerializer.Deserialize<JsonElement>("""
    {
        "type": "object",
        "properties": {
            "topicName": {
                "type": "_______________",
                "description": "_______________"
            }
        },
        "required": ["_______________"]
    }
    """)
);
```

<details>
<summary>Answer</summary>

```csharp
var tool = new McpTool(
    Name: "get_topic_info",
    Description: "Get information about a Surgewave topic including partitions, offsets, and configuration",
    InputSchema: JsonSerializer.Deserialize<JsonElement>("""
    {
        "type": "object",
        "properties": {
            "topicName": {
                "type": "string",
                "description": "Name of the topic to get information about"
            }
        },
        "required": ["topicName"]
    }
    """)
);
```

Key points:
- Tool name should be descriptive and use snake_case
- Description should explain what the tool does
- Property type is "string" for text values
- Required array contains the names of mandatory parameters
</details>

## Score
- 9/9: Excellent!
- 7-8/9: Good understanding
- 5-6/9: Review the lesson
- <5/9: Re-do the lesson
