// Surgewave Bootcamp - Unit 23, Lesson 5: Building Custom AI Agents
// COMPLETED SOLUTION

using System.Text.Json;
using System.Text.Json.Serialization;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 23.5 - Building Custom AI Agents ===");
Console.WriteLine();

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

// Step 2 - Create the agent
var agent = new SurgewaveAgent("order-processor", bootstrapServers, jsonOptions);
Console.WriteLine($"[Agent] Initialized: {agent.AgentId}");

// Step 3 - Register agent tools
agent.RegisterTool("check_inventory", async args =>
{
    var productId = args.TryGetProperty("productId", out var p) ? p.GetString() : "unknown";
    await Task.Delay(100); // Simulate API call
    var stock = new Random().Next(0, 100);
    return $"Product '{productId}' has {stock} units in stock";
});

agent.RegisterTool("approve_order", async args =>
{
    var orderId = args.TryGetProperty("orderId", out var o) ? o.GetString() : "unknown";
    await Task.Delay(100); // Simulate processing

    // Publish approval event
    await agent.PublishEventAsync("order-decisions", orderId!, new
    {
        type = "order_approved",
        orderId = orderId,
        approvedAt = DateTime.UtcNow,
        approvedBy = agent.AgentId
    });

    return $"Order '{orderId}' has been approved and notification sent";
});

agent.RegisterTool("send_notification", async args =>
{
    var channel = args.TryGetProperty("channel", out var c) ? c.GetString() : "default";
    var message = args.TryGetProperty("message", out var m) ? m.GetString() : "";
    await Task.Delay(50);
    return $"Notification sent to '{channel}': {message}";
});

agent.RegisterTool("get_order_status", async args =>
{
    var orderId = args.TryGetProperty("orderId", out var o) ? o.GetString() : "unknown";
    await Task.Delay(100);
    var statuses = new[] { "pending", "approved", "shipped", "delivered" };
    return $"Order '{orderId}' status: {statuses[new Random().Next(statuses.Length)]}";
});

// Step 4 - Seed some sample orders for the demo
Console.WriteLine("\n--- Seeding Sample Data ---");
await agent.PublishEventAsync("orders", "ORD-001", new { orderId = "ORD-001", amount = 99.99m, customer = "Alice", status = "approved" });
await agent.PublishEventAsync("orders", "ORD-002", new { orderId = "ORD-002", amount = 1500.00m, customer = "Bob", status = "pending_review" });
await agent.PublishEventAsync("orders", "ORD-003", new { orderId = "ORD-003", amount = 45.00m, customer = "Carol", status = "approved" });
Console.WriteLine("Seeded 3 sample orders");

// Step 5 - Run interactive agent loop
Console.WriteLine("\n--- Interactive Agent Mode ---");
Console.WriteLine("Type your message (or 'quit' to exit):");
Console.WriteLine("Try: 'What orders do we have?', 'Check inventory for PROD-123', 'Approve order ORD-002'\n");

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Equals("quit", StringComparison.OrdinalIgnoreCase)) break;

    try
    {
        var response = await agent.ProcessInputAsync(input);
        Console.WriteLine($"Agent: {response}\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Error] {ex.Message}\n");
    }
}

Console.WriteLine("\n[Agent] Saving state...");
await agent.SaveStateAsync();
Console.WriteLine("Goodbye!");

// ============================================================================
// Agent Implementation
// ============================================================================

public record ConversationMessage(string Role, string Content, DateTime Timestamp);

public record AgentState(
    string AgentId,
    DateTime SavedAt,
    List<ConversationMessage> History
);

public class SurgewaveAgent : IAsyncDisposable
{
    private readonly string _agentId;
    private readonly SurgewaveProducer<string, string> _producer;
    private readonly SurgewaveConsumer<string, string> _stateConsumer;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly List<ConversationMessage> _history = new();
    private readonly Dictionary<string, Func<JsonElement, Task<string>>> _tools = new();

    public string AgentId => _agentId;

    public SurgewaveAgent(string agentId, string bootstrapServers, JsonSerializerOptions jsonOptions)
    {
        _agentId = agentId;
        _jsonOptions = jsonOptions;

        _producer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = bootstrapServers;
        });

        _stateConsumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = bootstrapServers;
            opt.GroupId = $"agent-state-{agentId}-{Guid.NewGuid():N}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });
    }

    public void RegisterTool(string name, Func<JsonElement, Task<string>> handler)
    {
        _tools[name] = handler;
        Console.WriteLine($"[Agent] Registered tool: {name}");
    }

    public async Task PublishEventAsync(string topic, string key, object data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        await _producer.ProduceAsync(topic, key, json);
    }

    public async Task SaveStateAsync()
    {
        var state = new AgentState(_agentId, DateTime.UtcNow, _history.ToList());
        var json = JsonSerializer.Serialize(state, _jsonOptions);
        await _producer.ProduceAsync("agent-state", _agentId, json);
    }

    public async Task LoadStateAsync()
    {
        await _stateConsumer.SubscribeAsync(default, "agent-state");
        using var cts = new CancellationTokenSource(3000);

        AgentState? latestState = null;
        while (!cts.IsCancellationRequested)
        {
            try
            {
                var result = await _stateConsumer.ConsumeAsync(cts.Token);
                if (result?.Key == _agentId)
                {
                    latestState = JsonSerializer.Deserialize<AgentState>(result.Value, _jsonOptions);
                }
            }
            catch (OperationCanceledException) { break; }
        }

        if (latestState != null)
        {
            _history.Clear();
            _history.AddRange(latestState.History);
            Console.WriteLine($"[Agent] Loaded {_history.Count} messages from state");
        }
    }

    public async Task<string> ProcessInputAsync(string input)
    {
        // Add user message to history
        _history.Add(new ConversationMessage("user", input, DateTime.UtcNow));

        // Simulate agent thinking (pattern matching for demo)
        var response = await ThinkAndActAsync(input);

        // Add agent response to history
        _history.Add(new ConversationMessage("assistant", response, DateTime.UtcNow));

        return response;
    }

    private async Task<string> ThinkAndActAsync(string input)
    {
        var lowerInput = input.ToLowerInvariant();

        // Pattern matching for different intents (simulating LLM understanding)
        if (lowerInput.Contains("order") && (lowerInput.Contains("what") || lowerInput.Contains("list") || lowerInput.Contains("show")))
        {
            return await HandleListOrdersAsync();
        }

        if (lowerInput.Contains("check inventory") || lowerInput.Contains("stock"))
        {
            var productId = ExtractProductId(input) ?? "PROD-001";
            return await ExecuteToolAsync("check_inventory", new { productId });
        }

        if (lowerInput.Contains("approve") && lowerInput.Contains("order"))
        {
            var orderId = ExtractOrderId(input) ?? "ORD-001";
            return await ExecuteToolAsync("approve_order", new { orderId });
        }

        if (lowerInput.Contains("status"))
        {
            var orderId = ExtractOrderId(input) ?? "ORD-001";
            return await ExecuteToolAsync("get_order_status", new { orderId });
        }

        if (lowerInput.Contains("why") && lowerInput.Contains("pending"))
        {
            return "Orders over $1,000 require manual review per our fraud prevention policy. " +
                   "This helps protect both the customer and the business from unauthorized transactions.";
        }

        if (lowerInput.Contains("help") || lowerInput.Contains("what can you do"))
        {
            return "I can help you with:\n" +
                   "  - List orders: 'What orders do we have?'\n" +
                   "  - Check inventory: 'Check inventory for PROD-123'\n" +
                   "  - Approve orders: 'Approve order ORD-002'\n" +
                   "  - Check status: 'What is the status of ORD-001?'\n" +
                   "  - Explain policies: 'Why is an order pending?'";
        }

        // Default response
        return "I'm not sure how to help with that. Try asking about orders, inventory, or say 'help' for options.";
    }

    private async Task<string> HandleListOrdersAsync()
    {
        // Simulate reading from orders topic
        await Task.Delay(100);
        return "I found 3 orders today:\n" +
               "  - ORD-001: $99.99 (approved) - Customer: Alice\n" +
               "  - ORD-002: $1,500.00 (pending review) - Customer: Bob\n" +
               "  - ORD-003: $45.00 (approved) - Customer: Carol";
    }

    private async Task<string> ExecuteToolAsync(string toolName, object args)
    {
        if (!_tools.TryGetValue(toolName, out var handler))
        {
            return $"Tool '{toolName}' not found.";
        }

        Console.WriteLine($"[Agent] Executing tool: {toolName}");
        var argsJson = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(args, _jsonOptions));
        var result = await handler(argsJson);
        _history.Add(new ConversationMessage("tool", $"{toolName}: {result}", DateTime.UtcNow));
        return result;
    }

    private static string? ExtractOrderId(string input)
    {
        // Simple pattern matching for order IDs
        var patterns = new[] { "ORD-001", "ORD-002", "ORD-003", "ORD-004", "ORD-005" };
        foreach (var pattern in patterns)
        {
            if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return pattern;
        }
        return null;
    }

    private static string? ExtractProductId(string input)
    {
        // Simple pattern matching for product IDs
        var match = System.Text.RegularExpressions.Regex.Match(input, @"PROD-\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    public async ValueTask DisposeAsync()
    {
        await _producer.DisposeAsync();
        await _stateConsumer.DisposeAsync();
    }
}