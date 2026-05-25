# Lesson 23.5: Instructions

Follow these step-by-step instructions to build a custom AI agent powered by Surgewave.

## Step 1: Define the Agent Structure

Create the core agent class:

```csharp
public class SurgewaveAgent
{
    private readonly string _agentId;
    private readonly SurgewaveProducer<string, string> _producer;
    private readonly SurgewaveConsumer<string, string> _consumer;
    private readonly Dictionary<string, Func<JsonElement, Task<string>>> _tools;
    private readonly List<ConversationMessage> _conversationHistory = new();

    public SurgewaveAgent(string agentId, string bootstrapServers)
    {
        _agentId = agentId;
        _tools = new Dictionary<string, Func<JsonElement, Task<string>>>();

        _producer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = bootstrapServers);

        _consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = bootstrapServers;
            opt.GroupId = $"agent-{agentId}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });
    }
}
```

## Step 2: Implement Conversation State Persistence

Save and load conversation history from Surgewave:

```csharp
public async Task SaveStateAsync()
{
    var state = new AgentState
    {
        AgentId = _agentId,
        Timestamp = DateTime.UtcNow,
        ConversationHistory = _conversationHistory,
        LastActivity = DateTime.UtcNow
    };

    var json = JsonSerializer.Serialize(state);
    await _producer.ProduceAsync("agent-state", _agentId, json);
}

public async Task LoadStateAsync()
{
    await _consumer.SubscribeAsync(default, "agent-state");

    // Read the latest state for this agent
    using var cts = new CancellationTokenSource(5000);
    while (true)
    {
        try
        {
            var result = await _consumer.ConsumeAsync(cts.Token);
            if (result?.Key == _agentId)
            {
                var state = JsonSerializer.Deserialize<AgentState>(result.Value);
                _conversationHistory.Clear();
                _conversationHistory.AddRange(state.ConversationHistory);
                break;
            }
        }
        catch (OperationCanceledException) { break; }
    }
}
```

## Step 3: Register Agent Tools

Add tools the agent can use:

```csharp
public void RegisterTool(string name, Func<JsonElement, Task<string>> handler)
{
    _tools[name] = handler;
    Console.WriteLine($"[Agent] Registered tool: {name}");
}

// Usage:
agent.RegisterTool("check_inventory", async args =>
{
    var productId = args.GetProperty("productId").GetString();
    // Check inventory logic
    return $"Product {productId} has 50 units in stock";
});

agent.RegisterTool("send_notification", async args =>
{
    var channel = args.GetProperty("channel").GetString();
    var message = args.GetProperty("message").GetString();
    await _producer.ProduceAsync("notifications", channel, message);
    return $"Notification sent to {channel}";
});
```

## Step 4: Implement the Think-Act Loop

Create the agent's decision-making loop:

```csharp
public async Task<string> ProcessInputAsync(string userInput)
{
    // Add user message to history
    _conversationHistory.Add(new ConversationMessage("user", userInput));

    // Build prompt with context
    var prompt = BuildPrompt(userInput);

    // Simulate LLM decision (in production, call actual LLM API)
    var decision = await SimulateThinkingAsync(prompt);

    // Execute tool if needed
    if (decision.RequiresTool)
    {
        var toolResult = await ExecuteToolAsync(decision.ToolName, decision.ToolArgs);
        _conversationHistory.Add(new ConversationMessage("tool", toolResult));
    }

    // Generate response
    var response = await GenerateResponseAsync(decision);
    _conversationHistory.Add(new ConversationMessage("assistant", response));

    // Save state
    await SaveStateAsync();

    return response;
}
```

## Step 5: Create an Event-Driven Agent Loop

Monitor topics and respond to events:

```csharp
public async Task RunEventLoopAsync(string[] topics, CancellationToken ct)
{
    await _consumer.SubscribeAsync(ct, topics);

    Console.WriteLine($"[Agent] Listening to topics: {string.Join(", ", topics)}");

    while (!ct.IsCancellationRequested)
    {
        try
        {
            var message = await _consumer.ConsumeAsync(ct);
            if (message == null) continue;

            Console.WriteLine($"[Agent] Received event from {message.Topic}");

            // Process the event
            var response = await HandleEventAsync(message);

            // Publish response if needed
            if (!string.IsNullOrEmpty(response))
            {
                await _producer.ProduceAsync(
                    $"{message.Topic}-responses",
                    message.Key,
                    response
                );
            }

            await _consumer.CommitAsync(message);
        }
        catch (OperationCanceledException) { break; }
        catch (Exception ex)
        {
            Console.WriteLine($"[Agent] Error: {ex.Message}");
        }
    }
}
```

## Step 6: Handle Specific Event Types

Process different event types:

```csharp
private async Task<string?> HandleEventAsync(ConsumeResult message)
{
    var eventData = JsonSerializer.Deserialize<JsonElement>(message.Value);
    var eventType = eventData.GetProperty("type").GetString();

    return eventType switch
    {
        "order_created" => await HandleOrderCreatedAsync(eventData),
        "inventory_low" => await HandleInventoryLowAsync(eventData),
        "customer_inquiry" => await HandleInquiryAsync(eventData),
        _ => null
    };
}

private async Task<string> HandleOrderCreatedAsync(JsonElement order)
{
    var orderId = order.GetProperty("orderId").GetString();
    var amount = order.GetProperty("amount").GetDecimal();

    // Agent decides based on order value
    if (amount > 1000)
    {
        return JsonSerializer.Serialize(new
        {
            action = "flag_for_review",
            orderId = orderId,
            reason = "High value order requires manual approval"
        });
    }

    return JsonSerializer.Serialize(new
    {
        action = "auto_approve",
        orderId = orderId
    });
}
```

## Step 7: Test the Agent

Run the complete agent:

```csharp
var agent = new SurgewaveAgent("order-processor", bootstrapServers);

// Register tools
agent.RegisterTool("check_inventory", CheckInventory);
agent.RegisterTool("send_notification", SendNotification);
agent.RegisterTool("approve_order", ApproveOrder);

// Load previous state
await agent.LoadStateAsync();

// Interactive mode
Console.WriteLine("Agent ready. Type your message (or 'quit' to exit):");
while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (input == "quit") break;

    var response = await agent.ProcessInputAsync(input);
    Console.WriteLine($"Agent: {response}");
}
```

## Complete Solution

See `completed/Program.cs` for a complete agent implementation with:

- State persistence via Surgewave
- Tool registration and execution
- Event-driven processing loop
- Multi-step reasoning simulation
- Error handling and recovery

## Sample Output

```
=== Surgewave Bootcamp: Lesson 23.5 - Building Custom AI Agents ===

Surgewave broker started on port 12345
[Agent] Initialized: order-processor

--- Interactive Agent Mode ---
Type your message (or 'quit' to exit):

> What orders do we have today?
[Agent] Checking orders topic...
Agent: I found 3 orders today:
  - ORD-001: $99.99 (approved)
  - ORD-002: $1,500.00 (pending review)
  - ORD-003: $45.00 (approved)

> Why is ORD-002 pending?
[Agent] Looking up order history...
Agent: ORD-002 is pending because it's a high-value order ($1,500).
Orders over $1,000 require manual approval per our policy.

> Approve it
[Agent] Executing approve_order tool...
Agent: Done! ORD-002 has been approved. I've sent a notification
to the customer and updated the inventory.

> quit
[Agent] Saving state...
Goodbye!
```

## Knowledge Check

1. Why use Surgewave topics for agent state instead of a database?
2. How does the event-driven loop handle multiple topics?
3. What happens if the agent crashes mid-conversation?
4. How can multiple agents coordinate through Surgewave?
5. What's the benefit of persisting conversation history?

---

**Next:** [Unit 19 Project: Surgewave Agent](../project/README.md)
