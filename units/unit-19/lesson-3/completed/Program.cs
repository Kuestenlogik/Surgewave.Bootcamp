// Surgewave Bootcamp - Unit 23, Lesson 3: MCP Tools Implementation
// COMPLETED SOLUTION

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 23.3 - MCP Tools Implementation ===");
Console.WriteLine();

// JSON serialization options
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

// Step 1 - Start Surgewave runtime
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = $"localhost:{runtime.Port}";
Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

// Create producer and consumer
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

// Step 2 - Define tool schemas
Console.WriteLine("\n--- Registering Surgewave Tools ---");

var produceSchema = JsonSerializer.Deserialize<JsonElement>("""
{
    "type": "object",
    "properties": {
        "topic": { "type": "string", "description": "Target topic name" },
        "key": { "type": "string", "description": "Message key (optional, auto-generated if not provided)" },
        "value": { "type": "string", "description": "Message value (JSON string recommended)" }
    },
    "required": ["topic", "value"]
}
""");

var consumeSchema = JsonSerializer.Deserialize<JsonElement>("""
{
    "type": "object",
    "properties": {
        "topic": { "type": "string", "description": "Topic to consume from" },
        "count": { "type": "integer", "description": "Number of messages to consume", "default": 1 },
        "timeout_ms": { "type": "integer", "description": "Timeout in milliseconds", "default": 5000 }
    },
    "required": ["topic"]
}
""");

var listTopicsSchema = JsonSerializer.Deserialize<JsonElement>("""
{
    "type": "object",
    "properties": {
        "pattern": { "type": "string", "description": "Optional regex pattern to filter topics" }
    },
    "required": []
}
""");

var describeTopicSchema = JsonSerializer.Deserialize<JsonElement>("""
{
    "type": "object",
    "properties": {
        "topic": { "type": "string", "description": "Name of the topic to describe" }
    },
    "required": ["topic"]
}
""");

// Step 3 - Implement surgewave_produce tool
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

    if (string.IsNullOrEmpty(topic))
        return ToolResult.Error("Topic cannot be empty");

    if (string.IsNullOrEmpty(value))
        return ToolResult.Error("Value cannot be empty");

    var key = args.Value.TryGetProperty("key", out var keyElement) && keyElement.ValueKind == JsonValueKind.String
        ? keyElement.GetString() ?? Guid.NewGuid().ToString()
        : Guid.NewGuid().ToString();

    try
    {
        var result = await producer.ProduceAsync(topic, key, value);
        return ToolResult.Success(new
        {
            status = "sent",
            topic = topic,
            key = key,
            partition = result.Partition,
            offset = result.Offset
        });
    }
    catch (Exception ex)
    {
        return ToolResult.Error($"Failed to produce: {ex.Message}");
    }
}

Console.WriteLine("[Tool] surgewave_produce registered");

// Step 4 - Implement surgewave_consume tool
async Task<ToolResult> HandleSurgewaveConsume(JsonElement? args)
{
    if (!args.HasValue)
        return ToolResult.Error("Missing arguments");

    if (!args.Value.TryGetProperty("topic", out var topicElement))
        return ToolResult.Error("Missing required parameter: topic");

    var topic = topicElement.GetString();
    if (string.IsNullOrEmpty(topic))
        return ToolResult.Error("Topic cannot be empty");

    var count = args.Value.TryGetProperty("count", out var countElement) && countElement.ValueKind == JsonValueKind.Number
        ? countElement.GetInt32()
        : 1;

    var timeoutMs = args.Value.TryGetProperty("timeout_ms", out var timeoutElement) && timeoutElement.ValueKind == JsonValueKind.Number
        ? timeoutElement.GetInt32()
        : 5000;

    if (count <= 0) count = 1;
    if (count > 100) count = 100; // Limit to prevent excessive reads
    if (timeoutMs <= 0) timeoutMs = 1000;
    if (timeoutMs > 30000) timeoutMs = 30000;

    try
    {
        await consumer.SubscribeAsync(default, topic);

        var messages = new List<object>();
        using var cts = new CancellationTokenSource(timeoutMs);

        for (int i = 0; i < count; i++)
        {
            try
            {
                var result = await consumer.ConsumeAsync(cts.Token);
                if (result != null)
                {
                    messages.Add(new
                    {
                        key = result.Key,
                        value = result.Value,
                        partition = result.Partition,
                        offset = result.Offset
                    });
                    await consumer.CommitAsync(result);
                }
            }
            catch (OperationCanceledException)
            {
                break; // Timeout reached
            }
        }

        return ToolResult.Success(new
        {
            topic = topic,
            requested = count,
            received = messages.Count,
            messages = messages
        });
    }
    catch (Exception ex)
    {
        return ToolResult.Error($"Failed to consume: {ex.Message}");
    }
}

Console.WriteLine("[Tool] surgewave_consume registered");

// Step 5 - Implement surgewave_list_topics tool
async Task<ToolResult> HandleListTopics(JsonElement? args)
{
    try
    {
        var topics = new List<string> { "orders", "users", "events" }; // Simulated for demo

        string? pattern = null;
        if (args.HasValue && args.Value.TryGetProperty("pattern", out var patternElement) && patternElement.ValueKind == JsonValueKind.String)
        {
            pattern = patternElement.GetString();
        }

        var filteredTopics = topics;
        if (!string.IsNullOrEmpty(pattern))
        {
            try
            {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                filteredTopics = topics.Where(t => regex.IsMatch(t)).ToList();
            }
            catch (RegexParseException)
            {
                return ToolResult.Error($"Invalid regex pattern: {pattern}");
            }
        }

        await Task.CompletedTask;

        return ToolResult.Success(new
        {
            count = filteredTopics.Count,
            pattern = pattern,
            topics = filteredTopics
        });
    }
    catch (Exception ex)
    {
        return ToolResult.Error($"Failed to list topics: {ex.Message}");
    }
}

Console.WriteLine("[Tool] surgewave_list_topics registered");

// Implement surgewave_describe_topic tool
async Task<ToolResult> HandleDescribeTopic(JsonElement? args)
{
    if (!args.HasValue)
        return ToolResult.Error("Missing arguments");

    if (!args.Value.TryGetProperty("topic", out var topicElement))
        return ToolResult.Error("Missing required parameter: topic");

    var topic = topicElement.GetString();
    if (string.IsNullOrEmpty(topic))
        return ToolResult.Error("Topic cannot be empty");

    await Task.CompletedTask;

    // Simulated topic metadata
    return ToolResult.Success(new
    {
        name = topic,
        partitions = 1,
        replicationFactor = 1,
        retentionMs = 604800000, // 7 days
        cleanupPolicy = "delete"
    });
}

Console.WriteLine("[Tool] surgewave_describe_topic registered");

// Step 6 - Test all tools
Console.WriteLine("\n--- Testing Surgewave Tools ---\n");

// Test 1: Produce a message
Console.WriteLine("[Test 1] surgewave_produce");
var produceInput = JsonSerializer.Deserialize<JsonElement>(
    """{"topic":"orders","key":"ord-001","value":"{\"id\":\"ord-001\",\"amount\":99.99,\"customer\":\"Alice\"}"}"""
);
var produceResult = await HandleSurgewaveProduce(produceInput);
Console.WriteLine($"  Input: {produceInput}");
Console.WriteLine($"  Result: {JsonSerializer.Serialize(produceResult, jsonOptions)}");

// Produce more messages for consume test
await HandleSurgewaveProduce(JsonSerializer.Deserialize<JsonElement>(
    """{"topic":"orders","key":"ord-002","value":"{\"id\":\"ord-002\",\"amount\":149.99,\"customer\":\"Bob\"}"}"""
));
await HandleSurgewaveProduce(JsonSerializer.Deserialize<JsonElement>(
    """{"topic":"orders","key":"ord-003","value":"{\"id\":\"ord-003\",\"amount\":49.99,\"customer\":\"Carol\"}"}"""
));
Console.WriteLine("  (Produced 2 more messages for consume test)");

// Test 2: Consume messages
Console.WriteLine("\n[Test 2] surgewave_consume");
var consumeInput = JsonSerializer.Deserialize<JsonElement>(
    """{"topic":"orders","count":3,"timeout_ms":2000}"""
);
var consumeResult = await HandleSurgewaveConsume(consumeInput);
Console.WriteLine($"  Input: {consumeInput}");
Console.WriteLine($"  Result: {JsonSerializer.Serialize(consumeResult, jsonOptions)}");

// Test 3: List topics
Console.WriteLine("\n[Test 3] surgewave_list_topics (no filter)");
var listInput1 = JsonSerializer.Deserialize<JsonElement>("""{}""");
var listResult1 = await HandleListTopics(listInput1);
Console.WriteLine($"  Input: {listInput1}");
Console.WriteLine($"  Result: {JsonSerializer.Serialize(listResult1, jsonOptions)}");

// Test 4: List topics with pattern
Console.WriteLine("\n[Test 4] surgewave_list_topics (with pattern)");
var listInput2 = JsonSerializer.Deserialize<JsonElement>("""{"pattern":"^o.*"}""");
var listResult2 = await HandleListTopics(listInput2);
Console.WriteLine($"  Input: {listInput2}");
Console.WriteLine($"  Result: {JsonSerializer.Serialize(listResult2, jsonOptions)}");

// Test 5: Describe topic
Console.WriteLine("\n[Test 5] surgewave_describe_topic");
var describeInput = JsonSerializer.Deserialize<JsonElement>("""{"topic":"orders"}""");
var describeResult = await HandleDescribeTopic(describeInput);
Console.WriteLine($"  Input: {describeInput}");
Console.WriteLine($"  Result: {JsonSerializer.Serialize(describeResult, jsonOptions)}");

// Test 6: Error handling - missing parameter
Console.WriteLine("\n[Test 6] surgewave_produce (missing topic - error case)");
var errorInput = JsonSerializer.Deserialize<JsonElement>("""{"value":"test"}""");
var errorResult = await HandleSurgewaveProduce(errorInput);
Console.WriteLine($"  Input: {errorInput}");
Console.WriteLine($"  Result: {JsonSerializer.Serialize(errorResult, jsonOptions)}");

// Summary
Console.WriteLine("\n--- Tool Summary ---");
Console.WriteLine("Registered tools: 4");
Console.WriteLine("  - surgewave_produce: Send messages to topics");
Console.WriteLine("  - surgewave_consume: Read messages from topics");
Console.WriteLine("  - surgewave_list_topics: List available topics");
Console.WriteLine("  - surgewave_describe_topic: Get topic details");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Validate all tool inputs before calling Surgewave operations");
Console.WriteLine("2. Return meaningful error messages for invalid inputs");
Console.WriteLine("3. Include partition and offset in produce results");
Console.WriteLine("4. Handle timeouts gracefully in consume operations");
Console.WriteLine("5. Use structured responses for complex data");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Helper types
public record ToolResult(bool IsSuccess, object? Data, string? ErrorMessage)
{
    public static ToolResult Success(string message) =>
        new(true, new { message = message }, null);

    public static ToolResult Success(object data) =>
        new(true, data, null);

    public static ToolResult Error(string message) =>
        new(false, null, message);
}