# Lesson 23.3: Instructions

Follow these step-by-step instructions to implement MCP tools for Surgewave operations.

## Step 1: Set Up Surgewave Runtime and Producer/Consumer

Create the Surgewave infrastructure:

```csharp
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = $"localhost:{runtime.Port}";

await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
});

await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "mcp-tools-consumer";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
    opt.EnableAutoCommit = false;
});
```

## Step 2: Implement the surgewave_produce Tool

Create a tool that sends messages to Surgewave:

```csharp
async Task<ToolResult> HandleSurgewaveProduce(JsonElement? args)
{
    // Validate required parameters
    if (!args.HasValue)
        return ToolResult.Error("Missing arguments");

    if (!args.Value.TryGetProperty("topic", out var topicElement))
        return ToolResult.Error("Missing required parameter: topic");

    if (!args.Value.TryGetProperty("value", out var valueElement))
        return ToolResult.Error("Missing required parameter: value");

    var topic = topicElement.GetString()!;
    var value = valueElement.GetString()!;

    // Optional key
    var key = args.Value.TryGetProperty("key", out var keyElement)
        ? keyElement.GetString() ?? Guid.NewGuid().ToString()
        : Guid.NewGuid().ToString();

    try
    {
        var result = await producer.ProduceAsync(topic, key, value);
        return ToolResult.Success($"Message sent to '{topic}' at partition {result.Partition}, offset {result.Offset}");
    }
    catch (Exception ex)
    {
        return ToolResult.Error($"Failed to produce: {ex.Message}");
    }
}
```

## Step 3: Implement the surgewave_consume Tool

Create a tool that reads messages from Surgewave:

```csharp
async Task<ToolResult> HandleSurgewaveConsume(JsonElement? args)
{
    if (!args.HasValue)
        return ToolResult.Error("Missing arguments");

    if (!args.Value.TryGetProperty("topic", out var topicElement))
        return ToolResult.Error("Missing required parameter: topic");

    var topic = topicElement.GetString()!;

    var count = args.Value.TryGetProperty("count", out var countElement)
        ? countElement.GetInt32()
        : 1;

    var timeoutMs = args.Value.TryGetProperty("timeout_ms", out var timeoutElement)
        ? timeoutElement.GetInt32()
        : 5000;

    // Subscribe to the topic
    await consumer.SubscribeAsync(default, topic);

    var messages = new List<object>();
    var cts = new CancellationTokenSource(timeoutMs);

    try
    {
        for (int i = 0; i < count; i++)
        {
            var result = await consumer.ConsumeAsync(cts.Token);
            if (result != null)
            {
                messages.Add(new
                {
                    key = result.Key,
                    value = result.Value,
                    partition = result.Partition,
                    offset = result.Offset,
                    timestamp = result.Timestamp
                });
                await consumer.CommitAsync(result);
            }
        }
    }
    catch (OperationCanceledException)
    {
        // Timeout reached
    }

    return ToolResult.Success(new
    {
        topic = topic,
        messagesReceived = messages.Count,
        messages = messages
    });
}
```

## Step 4: Implement Topic Management Tools

Create tools for topic operations:

```csharp
async Task<ToolResult> HandleListTopics(JsonElement? args)
{
    var topics = await runtime.GetTopicsAsync();

    string? pattern = null;
    if (args.HasValue && args.Value.TryGetProperty("pattern", out var patternElement))
    {
        pattern = patternElement.GetString();
    }

    var filteredTopics = pattern != null
        ? topics.Where(t => Regex.IsMatch(t, pattern)).ToList()
        : topics.ToList();

    return ToolResult.Success(new
    {
        count = filteredTopics.Count,
        topics = filteredTopics
    });
}

async Task<ToolResult> HandleDescribeTopic(JsonElement? args)
{
    if (!args.HasValue || !args.Value.TryGetProperty("topic", out var topicElement))
        return ToolResult.Error("Missing required parameter: topic");

    var topic = topicElement.GetString()!;

    try
    {
        var metadata = await runtime.GetTopicMetadataAsync(topic);
        return ToolResult.Success(new
        {
            name = topic,
            partitions = metadata.PartitionCount,
            replicationFactor = metadata.ReplicationFactor,
            config = metadata.Configuration
        });
    }
    catch (Exception ex)
    {
        return ToolResult.Error($"Topic not found: {ex.Message}");
    }
}
```

## Step 5: Create a ToolResult Helper

Simplify tool response creation:

```csharp
public record ToolResult(bool IsSuccess, object? Data, string? ErrorMessage)
{
    public static ToolResult Success(string message) =>
        new(true, new { content = new[] { new { type = "text", text = message } } }, null);

    public static ToolResult Success(object data) =>
        new(true, new { content = new[] { new { type = "text", text = JsonSerializer.Serialize(data) } } }, null);

    public static ToolResult Error(string message) =>
        new(false, null, message);
}
```

## Step 6: Register All Tools

Add all tools to the MCP server:

```csharp
server.RegisterTool("surgewave_produce", "Send a message to a Surgewave topic", produceSchema, HandleSurgewaveProduce);
server.RegisterTool("surgewave_consume", "Read messages from a Surgewave topic", consumeSchema, HandleSurgewaveConsume);
server.RegisterTool("surgewave_list_topics", "List all topics in the Surgewave cluster", listSchema, HandleListTopics);
server.RegisterTool("surgewave_describe_topic", "Get detailed information about a topic", describeSchema, HandleDescribeTopic);
```

## Step 7: Test the Tools

Verify all tools work correctly:

```csharp
// Test produce
await server.CallTool("surgewave_produce", new { topic = "orders", key = "ord-1", value = "{\"amount\": 99.99}" });

// Test consume
await server.CallTool("surgewave_consume", new { topic = "orders", count = 1 });

// Test list topics
await server.CallTool("surgewave_list_topics", new { });

// Test describe topic
await server.CallTool("surgewave_describe_topic", new { topic = "orders" });
```

## Complete Solution

See `completed/Program.cs` for the full implementation with:

- All Surgewave operation tools
- Comprehensive input validation
- Error handling and timeout management
- Structured response formatting

## Sample Output

```
=== Surgewave Bootcamp: Lesson 23.3 - MCP Tools Implementation ===

Surgewave broker started on port 12345

--- Registering Surgewave Tools ---
[MCP] Registered tool: surgewave_produce
[MCP] Registered tool: surgewave_consume
[MCP] Registered tool: surgewave_list_topics
[MCP] Registered tool: surgewave_describe_topic

--- Testing Tools ---

[Test] surgewave_produce:
  Input: {"topic":"orders","key":"ord-1","value":"{\"id\":\"ord-1\",\"amount\":99.99}"}
  Result: Message sent to 'orders' at partition 0, offset 0

[Test] surgewave_consume:
  Input: {"topic":"orders","count":1}
  Result: {"topic":"orders","messagesReceived":1,"messages":[{"key":"ord-1","value":"{\"id\":\"ord-1\",\"amount\":99.99}","partition":0,"offset":0}]}

[Test] surgewave_list_topics:
  Input: {}
  Result: {"count":1,"topics":["orders"]}

[Test] surgewave_describe_topic:
  Input: {"topic":"orders"}
  Result: {"name":"orders","partitions":1,"replicationFactor":1}

Lesson complete!
```

## Knowledge Check

1. Why should you validate tool inputs before calling Surgewave operations?
2. How do you handle optional parameters with default values?
3. What's the benefit of using a ToolResult helper class?
4. How should you handle timeouts in the consume tool?
5. Why is it important to commit offsets after consuming messages?

---

**Next:** [Lesson 23.4: AI Agent Integration](../lesson-4/README.md)
