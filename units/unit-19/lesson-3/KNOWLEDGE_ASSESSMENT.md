# Knowledge Assessment: Lesson 23.3 - MCP Tools Implementation

## Multiple Choice

### 1. Which JSON-RPC error code should be returned for missing required parameters?
- [ ] A) -32600 (Invalid Request)
- [ ] B) -32601 (Method not found)
- [ ] C) -32602 (Invalid params)
- [ ] D) -32603 (Internal error)

<details>
<summary>Answer</summary>
**C)** -32602 (Invalid params) is used when the request parameters are invalid, missing required fields, or have incorrect types.
</details>

### 2. What format does MCP use for tool responses?
- [ ] A) Plain text
- [ ] B) A content array with typed items (e.g., { type: "text", text: "..." })
- [ ] C) Raw JSON
- [ ] D) Base64 encoded data

<details>
<summary>Answer</summary>
**B)** MCP tool responses use a content array where each item has a type (e.g., "text", "image") and the corresponding data. This allows for rich, multi-part responses.
</details>

### 3. Why is it important to handle OperationCanceledException in the consume tool?
- [ ] A) To prevent the application from crashing
- [ ] B) To gracefully handle timeout when no messages are available
- [ ] C) To retry the operation automatically
- [ ] D) To log the error for debugging

<details>
<summary>Answer</summary>
**B)** OperationCanceledException is thrown when the CancellationToken times out. Catching it allows the consume tool to return the messages received so far instead of failing completely.
</details>

### 4. What should the surgewave_produce tool return on success?
- [ ] A) Just "OK"
- [ ] B) The partition and offset where the message was written
- [ ] C) The entire message back to the caller
- [ ] D) Nothing

<details>
<summary>Answer</summary>
**B)** Returning the partition and offset provides confirmation of where the message was stored and allows the AI/user to reference or verify the produced message.
</details>

### 5. Which approach is best for sharing Surgewave connections across tools?
- [ ] A) Create new connections in each tool handler
- [ ] B) Use dependency injection to share producer/consumer instances
- [ ] C) Store connection strings in environment variables
- [ ] D) Use global static variables

<details>
<summary>Answer</summary>
**B)** Dependency injection (or a shared server context) is the cleanest way to share Surgewave connections. This avoids creating new connections per request (expensive) and maintains proper lifecycle management.
</details>

## True/False

### 6. Tool input schemas are optional in MCP.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Every MCP tool must have an inputSchema that describes its parameters. This allows AI assistants to understand what arguments the tool expects and generate correct tool calls.
</details>

### 7. The surgewave_consume tool should always commit offsets after reading messages.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - In the context of MCP tools, you should commit offsets after consuming to mark the messages as processed. This prevents the same messages from being returned on subsequent consume calls.
</details>

## Code Challenge

### 8. What's wrong with this tool implementation?

```csharp
async Task<ToolResult> HandleSurgewaveProduce(JsonElement? args)
{
    var topic = args.Value.GetProperty("topic").GetString();
    var value = args.Value.GetProperty("value").GetString();

    var result = await producer.ProduceAsync(topic, Guid.NewGuid().ToString(), value);

    return ToolResult.Success($"Sent to {topic}");
}
```

<details>
<summary>Answer</summary>
Several issues:

1. **No null check on args**: `args.Value` will throw NullReferenceException if args is null
2. **No TryGetProperty**: `GetProperty` throws if the property doesn't exist
3. **No try-catch**: Surgewave operations can fail and should be wrapped
4. **Missing details in response**: Should include partition and offset

Corrected version:
```csharp
async Task<ToolResult> HandleSurgewaveProduce(JsonElement? args)
{
    if (!args.HasValue)
        return ToolResult.Error("Missing arguments");

    if (!args.Value.TryGetProperty("topic", out var topicElement))
        return ToolResult.Error("Missing required parameter: topic");

    if (!args.Value.TryGetProperty("value", out var valueElement))
        return ToolResult.Error("Missing required parameter: value");

    var topic = topicElement.GetString();
    var value = valueElement.GetString();

    if (string.IsNullOrEmpty(topic) || string.IsNullOrEmpty(value))
        return ToolResult.Error("Topic and value cannot be empty");

    try
    {
        var result = await producer.ProduceAsync(topic, Guid.NewGuid().ToString(), value);
        return ToolResult.Success($"Sent to '{topic}' at partition {result.Partition}, offset {result.Offset}");
    }
    catch (Exception ex)
    {
        return ToolResult.Error($"Failed to produce: {ex.Message}");
    }
}
```
</details>

### 9. Complete the tool schema for surgewave_consume:

```csharp
var consumeSchema = JsonSerializer.Deserialize<JsonElement>("""
{
    "type": "object",
    "properties": {
        "topic": {
            "type": "string",
            "description": "_____________________"
        },
        "count": {
            "type": "_____________________",
            "description": "Number of messages to consume",
            "default": 1
        },
        "timeout_ms": {
            "type": "integer",
            "description": "_____________________",
            "_____________________": 5000
        }
    },
    "required": ["_____________________"]
}
""");
```

<details>
<summary>Answer</summary>

```csharp
var consumeSchema = JsonSerializer.Deserialize<JsonElement>("""
{
    "type": "object",
    "properties": {
        "topic": {
            "type": "string",
            "description": "Topic to consume messages from"
        },
        "count": {
            "type": "integer",
            "description": "Number of messages to consume",
            "default": 1
        },
        "timeout_ms": {
            "type": "integer",
            "description": "Maximum time to wait for messages in milliseconds",
            "default": 5000
        }
    },
    "required": ["topic"]
}
""");
```

Key points:
- topic description should explain what topic to consume from
- count type is "integer" for numeric values
- timeout_ms description explains the purpose
- "default" is used for optional parameters
- Only "topic" is required; count and timeout have defaults
</details>

## Score
- 9/9: Excellent!
- 7-8/9: Good understanding
- 5-6/9: Review the lesson
- <5/9: Re-do the lesson
