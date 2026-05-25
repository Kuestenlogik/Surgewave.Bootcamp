// Surgewave Bootcamp - Unit 23 Project: Surgewave Agent
// COMPLETED SOLUTION
//
// A complete AI agent that connects to Surgewave via MCP, manages topics,
// produces/consumes messages, maintains conversation state, and provides
// cluster health insights.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

// Configuration
var bootstrapServers = Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS") ?? "localhost:9092";
var agentId = Environment.GetEnvironmentVariable("Surgewave_AGENT_ID") ?? $"surgewave-agent-{Guid.NewGuid():N[..8]}";
var rateLimitPerMinute = int.Parse(Environment.GetEnvironmentVariable("Surgewave_RATE_LIMIT") ?? "100");
var isMcpMode = args.Length > 0 && args[0] == "--mcp";

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

if (isMcpMode)
{
    await RunMcpServerMode();
}
else
{
    await RunInteractiveMode();
}

// ============================================================================
// Interactive Mode
// ============================================================================

async Task RunInteractiveMode()
{
    Console.WriteLine("=== Surgewave Agent ===");
    Console.WriteLine();

    // Start embedded Surgewave runtime
    await using var runtime = await SurgewaveRuntime.CreateBuilder()
        .WithPort(0)
        .WithStorageMode(StorageMode.Memory)
        .WithAutoCreateTopics(true)
        .Build()
        .StartAsync();

    bootstrapServers = $"localhost:{runtime.Port}";
    Console.WriteLine($"Surgewave cluster connected: {bootstrapServers}");
    Console.WriteLine($"Agent ID: {agentId}");

    // Create agent
    await using var agent = new SurgewaveAgentCore(agentId, bootstrapServers, jsonOptions);
    await agent.InitializeAsync();

    // Seed sample data
    await SeedSampleData(agent);

    Console.WriteLine("\nAgent ready. Type 'help' for commands, 'quit' to exit.\n");

    while (true)
    {
        Console.Write("> ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input)) continue;
        if (input.Equals("quit", StringComparison.OrdinalIgnoreCase)) break;

        try
        {
            var response = await agent.ProcessCommandAsync(input);
            Console.WriteLine($"Agent: {response}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] {ex.Message}\n");
        }
    }

    Console.WriteLine("Saving conversation state...");
    await agent.SaveStateAsync();
    Console.WriteLine("Goodbye!");
}

async Task SeedSampleData(SurgewaveAgentCore agent)
{
    Console.WriteLine("Seeding sample data...");
    await agent.ProduceAsync("orders", "ORD-001", JsonSerializer.Serialize(new { orderId = "ORD-001", amount = 99.99, customer = "Alice" }));
    await agent.ProduceAsync("orders", "ORD-002", JsonSerializer.Serialize(new { orderId = "ORD-002", amount = 249.50, customer = "Bob" }));
    await agent.ProduceAsync("users", "USR-001", JsonSerializer.Serialize(new { userId = "USR-001", name = "Alice", email = "alice@example.com" }));
}

// ============================================================================
// MCP Server Mode
// ============================================================================

async Task RunMcpServerMode()
{
    // Start embedded Surgewave runtime
    await using var runtime = await SurgewaveRuntime.CreateBuilder()
        .WithPort(0)
        .WithStorageMode(StorageMode.Memory)
        .WithAutoCreateTopics(true)
        .Build()
        .StartAsync();

    bootstrapServers = $"localhost:{runtime.Port}";

    await using var agent = new SurgewaveAgentCore(agentId, bootstrapServers, jsonOptions);
    await agent.InitializeAsync();

    var mcpServer = new McpServer("surgewave-agent", "1.0.0", jsonOptions, rateLimitPerMinute);
    RegisterMcpTools(mcpServer, agent);

    await mcpServer.RunAsync();
}

void RegisterMcpTools(McpServer server, SurgewaveAgentCore agent)
{
    server.RegisterTool("surgewave_produce", "Send a message to a Surgewave topic",
        JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{"topic":{"type":"string"},"key":{"type":"string"},"value":{"type":"string"}},"required":["topic","value"]}"""),
        async args =>
        {
            var topic = args?.GetProperty("topic").GetString() ?? throw new ArgumentException("Missing topic");
            var value = args?.GetProperty("value").GetString() ?? throw new ArgumentException("Missing value");
            var key = args.Value.TryGetProperty("key", out var k) ? k.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
            var result = await agent.ProduceAsync(topic, key, value);
            return $"Message sent to '{topic}' at partition {result.Partition}, offset {result.Offset}";
        });

    server.RegisterTool("surgewave_consume", "Read messages from a Surgewave topic",
        JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{"topic":{"type":"string"},"count":{"type":"integer","default":1},"timeout_ms":{"type":"integer","default":5000}},"required":["topic"]}"""),
        async args =>
        {
            var topic = args?.GetProperty("topic").GetString() ?? throw new ArgumentException("Missing topic");
            var count = args.Value.TryGetProperty("count", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetInt32() : 1;
            var messages = await agent.ConsumeAsync(topic, count);
            return new { topic, count = messages.Count, messages };
        });

    server.RegisterTool("surgewave_list_topics", "List all topics in the cluster",
        JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{"pattern":{"type":"string"}},"required":[]}"""),
        async args =>
        {
            var pattern = args.HasValue && args.Value.TryGetProperty("pattern", out var p) ? p.GetString() : null;
            var topics = await agent.ListTopicsAsync(pattern);
            return new { count = topics.Count, topics };
        });

    server.RegisterTool("surgewave_cluster_health", "Get cluster health and metrics",
        JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{},"required":[]}"""),
        async args =>
        {
            await Task.CompletedTask;
            return new { status = "healthy", brokers = 1, bootstrapServers = bootstrapServers, uptime = "running" };
        });
}

// ============================================================================
// Surgewave Agent Core
// ============================================================================

public class SurgewaveAgentCore : IAsyncDisposable
{
    private readonly string _agentId;
    private readonly string _bootstrapServers;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SurgewaveProducer<string, string> _producer;
    private readonly SurgewaveConsumer<string, string> _consumer;
    private readonly List<ConversationEntry> _history = new();
    private readonly List<string> _knownTopics = new();
    private readonly DateTime _startTime = DateTime.UtcNow;

    public string AgentId => _agentId;

    public SurgewaveAgentCore(string agentId, string bootstrapServers, JsonSerializerOptions jsonOptions)
    {
        _agentId = agentId;
        _bootstrapServers = bootstrapServers;
        _jsonOptions = jsonOptions;

        _producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
        _consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = bootstrapServers;
            opt.GroupId = $"agent-{agentId}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
            opt.EnableAutoCommit = false;
        });
    }

    public async Task InitializeAsync()
    {
        await Task.CompletedTask;
        _knownTopics.AddRange(new[] { "orders", "users", "events", "surgewave-agent-state" });
    }

    public async Task<AgentProduceResult> ProduceAsync(string topic, string key, string value)
    {
        if (!_knownTopics.Contains(topic)) _knownTopics.Add(topic);
        var result = await _producer.ProduceAsync(topic, key, value);
        return new AgentProduceResult(result.Partition, result.Offset);
    }

    public async Task<List<MessageInfo>> ConsumeAsync(string topic, int count)
    {
        await _consumer.SubscribeAsync(default, topic);
        var messages = new List<MessageInfo>();
        using var cts = new CancellationTokenSource(5000);

        for (int i = 0; i < count; i++)
        {
            try
            {
                var result = await _consumer.ConsumeAsync(cts.Token);
                if (result != null)
                {
                    messages.Add(new MessageInfo(result.Key ?? "", result.Value, result.Partition, result.Offset));
                    await _consumer.CommitAsync(result);
                }
            }
            catch (OperationCanceledException) { break; }
        }

        return messages;
    }

    public Task<List<string>> ListTopicsAsync(string? pattern)
    {
        var topics = _knownTopics.ToList();
        if (!string.IsNullOrEmpty(pattern))
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            topics = topics.Where(t => regex.IsMatch(t)).ToList();
        }
        return Task.FromResult(topics);
    }

    public async Task<string> ProcessCommandAsync(string input)
    {
        _history.Add(new ConversationEntry("user", input, DateTime.UtcNow));
        var response = await HandleCommandAsync(input);
        _history.Add(new ConversationEntry("assistant", response, DateTime.UtcNow));
        return response;
    }

    private async Task<string> HandleCommandAsync(string input)
    {
        var lower = input.ToLowerInvariant().Trim();

        if (lower == "help")
        {
            return """
                Available commands:
                  - list topics [pattern]: Show all topics (optionally filtered)
                  - send <topic> <message>: Send a message to a topic
                  - read <topic> [count]: Read messages from a topic
                  - health: Show cluster health status
                  - history: Show conversation history
                  - help: Show this help
                  - quit: Exit the agent
                """;
        }

        if (lower.StartsWith("list topics") || lower == "topics" || lower.Contains("what topics"))
        {
            var pattern = lower.Replace("list topics", "").Replace("topics", "").Trim();
            var topics = await ListTopicsAsync(string.IsNullOrEmpty(pattern) ? null : pattern);
            return $"Found {topics.Count} topics:\n" + string.Join("\n", topics.Select(t => $"  - {t}"));
        }

        if (lower.StartsWith("send ") || lower.StartsWith("produce "))
        {
            var parts = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return "Usage: send <topic> <message>";
            var topic = parts[1];
            var message = parts[2];
            var result = await ProduceAsync(topic, Guid.NewGuid().ToString(), message);
            return $"Message sent to '{topic}' at partition {result.Partition}, offset {result.Offset}";
        }

        if (lower.StartsWith("read ") || lower.StartsWith("consume "))
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return "Usage: read <topic> [count]";
            var topic = parts[1];
            var count = parts.Length > 2 && int.TryParse(parts[2], out var c) ? c : 1;
            var messages = await ConsumeAsync(topic, count);
            if (messages.Count == 0) return $"No messages found in '{topic}'";
            return $"Read {messages.Count} message(s) from '{topic}':\n" +
                   string.Join("\n", messages.Select(m => $"  [{m.Partition}:{m.Offset}] {m.Key}: {m.Value}"));
        }

        if (lower == "health" || lower.Contains("cluster health") || lower.Contains("is healthy"))
        {
            var uptime = DateTime.UtcNow - _startTime;
            return $"""
                Cluster Status: HEALTHY
                  - Bootstrap: {_bootstrapServers}
                  - Topics: {_knownTopics.Count}
                  - Uptime: {uptime.TotalMinutes:F1} minutes
                  - Agent ID: {_agentId}
                """;
        }

        if (lower == "history")
        {
            if (_history.Count == 0) return "No conversation history.";
            return "Conversation history:\n" +
                   string.Join("\n", _history.TakeLast(10).Select(h => $"  [{h.Role}] {h.Content}"));
        }

        return $"I don't understand '{input}'. Type 'help' for available commands.";
    }

    public async Task SaveStateAsync()
    {
        var state = new AgentState(_agentId, DateTime.UtcNow, _history.ToList());
        var json = JsonSerializer.Serialize(state, _jsonOptions);
        await _producer.ProduceAsync("surgewave-agent-state", _agentId, json);
    }

    public async ValueTask DisposeAsync()
    {
        await _producer.DisposeAsync();
        await _consumer.DisposeAsync();
    }
}

// ============================================================================
// MCP Server
// ============================================================================

public class McpServer
{
    private readonly string _name;
    private readonly string _version;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Dictionary<string, ToolDef> _tools = new();
    private readonly RateLimiter _rateLimiter;

    public McpServer(string name, string version, JsonSerializerOptions jsonOptions, int rateLimit)
    {
        _name = name;
        _version = version;
        _jsonOptions = jsonOptions;
        _rateLimiter = new RateLimiter(rateLimit, TimeSpan.FromMinutes(1));
    }

    public void RegisterTool(string name, string description, JsonElement schema, Func<JsonElement?, Task<object>> handler)
    {
        _tools[name] = new ToolDef(name, description, schema, handler);
    }

    public async Task RunAsync()
    {
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();
        using var reader = new StreamReader(stdin);
        using var writer = new StreamWriter(stdout) { AutoFlush = true };

        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) break;

            var response = await HandleMessageAsync(line);
            if (response != null)
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(response, _jsonOptions));
            }
        }
    }

    private async Task<JsonRpcResponse?> HandleMessageAsync(string json)
    {
        JsonRpcRequest? request;
        try { request = JsonSerializer.Deserialize<JsonRpcRequest>(json, _jsonOptions); }
        catch { return Error(null, -32700, "Parse error"); }

        if (request == null) return Error(null, -32700, "Parse error");
        if (!_rateLimiter.TryAcquire()) return Error(request.Id, -32603, "Rate limit exceeded");

        return request.Method switch
        {
            "initialize" => Success(request.Id, new { protocolVersion = "2024-11-05", capabilities = new { tools = new { } }, serverInfo = new { name = _name, version = _version } }),
            "tools/list" => Success(request.Id, new { tools = _tools.Values.Select(t => new { name = t.Name, description = t.Description, inputSchema = t.Schema }) }),
            "tools/call" => await HandleToolCallAsync(request),
            "notifications/initialized" => null,
            _ => Error(request.Id, -32601, $"Method not found: {request.Method}")
        };
    }

    private async Task<JsonRpcResponse> HandleToolCallAsync(JsonRpcRequest request)
    {
        try
        {
            var toolName = request.Params?.GetProperty("name").GetString();
            var arguments = request.Params?.TryGetProperty("arguments", out var args) == true ? args : (JsonElement?)null;

            if (string.IsNullOrEmpty(toolName) || !_tools.TryGetValue(toolName, out var tool))
                return Error(request.Id, -32601, $"Tool not found: {toolName}");

            var result = await tool.Handler(arguments);
            var text = result is string s ? s : JsonSerializer.Serialize(result, _jsonOptions);
            return Success(request.Id, new { content = new[] { new { type = "text", text } } });
        }
        catch (Exception ex)
        {
            return Error(request.Id, -32603, $"Tool error: {ex.Message}");
        }
    }

    private JsonRpcResponse Success(int? id, object result) => new("2.0", id, JsonSerializer.SerializeToElement(result, _jsonOptions), null);
    private JsonRpcResponse Error(int? id, int code, string message) => new("2.0", id, null, new JsonRpcError(code, message, null));
}

public class RateLimiter
{
    private readonly int _max;
    private readonly TimeSpan _window;
    private readonly Queue<DateTime> _requests = new();

    public RateLimiter(int max, TimeSpan window) { _max = max; _window = window; }

    public bool TryAcquire()
    {
        var now = DateTime.UtcNow;
        while (_requests.Count > 0 && _requests.Peek() < now - _window) _requests.Dequeue();
        if (_requests.Count >= _max) return false;
        _requests.Enqueue(now);
        return true;
    }
}

// ============================================================================
// Supporting Types
// ============================================================================

public record ConversationEntry(string Role, string Content, DateTime Timestamp);
public record AgentState(string AgentId, DateTime SavedAt, List<ConversationEntry> History);
public record MessageInfo(string Key, string Value, int Partition, long Offset);
public record AgentProduceResult(int Partition, long Offset);
public record ToolDef(string Name, string Description, JsonElement Schema, Func<JsonElement?, Task<object>> Handler);

public record JsonRpcRequest(
    [property: JsonPropertyName("jsonrpc")] string Jsonrpc,
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("params")] JsonElement? Params);

public record JsonRpcResponse(
    [property: JsonPropertyName("jsonrpc")] string Jsonrpc,
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("result")] JsonElement? Result,
    [property: JsonPropertyName("error")] JsonRpcError? Error);

public record JsonRpcError(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("data")] JsonElement? Data);