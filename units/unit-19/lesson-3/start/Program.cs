// Surgewave Bootcamp - Unit 23, Lesson 3: MCP Tools Implementation
//
// In this lesson, you'll learn:
// - How to implement production-ready MCP tools
// - Producer and consumer tools for Surgewave
// - Topic management tools
// - Input validation and error handling
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text.Json;
using System.Text.Json.Serialization;
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
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

// TODO: Step 1 - Start Surgewave runtime and create producer/consumer
// await using var runtime = await SurgewaveRuntime.CreateBuilder()
//     .WithPort(0)
//     .WithStorageMode(StorageMode.Memory)
//     .WithAutoCreateTopics(true)
//     .Build()
//     .StartAsync();
//
// var bootstrapServers = $"localhost:{runtime.Port}";
// Console.WriteLine($"Surgewave broker started on port {runtime.Port}");
//
// await using var producer = new SurgewaveProducer<string, string>(opt =>
// {
//     opt.BootstrapServers = bootstrapServers;
// });
//
// await using var consumer = new SurgewaveConsumer<string, string>(opt =>
// {
//     opt.BootstrapServers = bootstrapServers;
//     opt.GroupId = "mcp-tools-consumer";
//     opt.AutoOffsetReset = AutoOffsetReset.Earliest;
//     opt.EnableAutoCommit = false;
// });

// TODO: Step 2 - Define tool schemas
// var produceSchema = JsonSerializer.Deserialize<JsonElement>("""
// {
//     "type": "object",
//     "properties": {
//         "topic": { "type": "string", "description": "Target topic name" },
//         "key": { "type": "string", "description": "Message key (optional)" },
//         "value": { "type": "string", "description": "Message value" }
//     },
//     "required": ["topic", "value"]
// }
// """);
//
// var consumeSchema = JsonSerializer.Deserialize<JsonElement>("""
// {
//     "type": "object",
//     "properties": {
//         "topic": { "type": "string", "description": "Topic to consume from" },
//         "count": { "type": "integer", "description": "Number of messages", "default": 1 },
//         "timeout_ms": { "type": "integer", "description": "Timeout in ms", "default": 5000 }
//     },
//     "required": ["topic"]
// }
// """);

// TODO: Step 3 - Implement surgewave_produce tool handler
// async Task<ToolResult> HandleSurgewaveProduce(JsonElement? args)
// {
//     if (!args.HasValue)
//         return ToolResult.Error("Missing arguments");
//
//     if (!args.Value.TryGetProperty("topic", out var topicElement))
//         return ToolResult.Error("Missing required parameter: topic");
//
//     if (!args.Value.TryGetProperty("value", out var valueElement))
//         return ToolResult.Error("Missing required parameter: value");
//
//     var topic = topicElement.GetString()!;
//     var value = valueElement.GetString()!;
//     var key = args.Value.TryGetProperty("key", out var keyElement)
//         ? keyElement.GetString() ?? Guid.NewGuid().ToString()
//         : Guid.NewGuid().ToString();
//
//     try
//     {
//         var result = await producer.ProduceAsync(topic, key, value);
//         return ToolResult.Success($"Message sent to '{topic}' at partition {result.Partition}, offset {result.Offset}");
//     }
//     catch (Exception ex)
//     {
//         return ToolResult.Error($"Failed to produce: {ex.Message}");
//     }
// }

// TODO: Step 4 - Implement surgewave_consume tool handler
// async Task<ToolResult> HandleSurgewaveConsume(JsonElement? args)
// {
//     // Implement consume logic here
//     return ToolResult.Success("Not implemented");
// }

// TODO: Step 5 - Implement surgewave_list_topics tool handler
// async Task<ToolResult> HandleListTopics(JsonElement? args)
// {
//     // Implement list topics logic here
//     return ToolResult.Success("Not implemented");
// }

// TODO: Step 6 - Register and test tools
// Console.WriteLine("\n--- Testing Surgewave Tools ---\n");
//
// // Test produce
// var produceResult = await HandleSurgewaveProduce(JsonSerializer.Deserialize<JsonElement>(
//     """{"topic":"orders","key":"ord-1","value":"{\"id\":\"ord-1\",\"amount\":99.99}"}"""
// ));
// Console.WriteLine($"Produce result: {produceResult}");

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
        new(true, message, null);

    public static ToolResult Success(object data) =>
        new(true, data, null);

    public static ToolResult Error(string message) =>
        new(false, null, message);

    public override string ToString() =>
        IsSuccess ? $"Success: {Data}" : $"Error: {ErrorMessage}";
}