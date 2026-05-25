# Knowledge Assessment: Lesson 23.5 - Building Custom AI Agents

## Multiple Choice

### 1. Why would you use a Surgewave topic to store agent conversation history?
- [ ] A) It's faster than a database
- [ ] B) It enables replay, recovery, and multi-agent access to conversation context
- [ ] C) It's required by the MCP protocol
- [ ] D) Surgewave topics are easier to query than databases

<details>
<summary>Answer</summary>
**B)** Surgewave topics provide replay capability (recover after crash), enable multiple agents to access shared context, and naturally fit event-driven architectures. The append-only log also provides an audit trail.
</details>

### 2. In the event-driven agent pattern, what triggers agent actions?
- [ ] A) HTTP requests
- [ ] B) Timer events
- [ ] C) Messages consumed from Surgewave topics
- [ ] D) User keyboard input

<details>
<summary>Answer</summary>
**C)** Event-driven agents consume messages from Surgewave topics (like "orders" or "alerts") and react to each event by processing it and potentially producing responses to other topics.
</details>

### 3. How should agents handle state when they restart?
- [ ] A) Start fresh with no history
- [ ] B) Load the latest state from a Surgewave topic using the agent's ID as the key
- [ ] C) Request state from other agents
- [ ] D) Read from a configuration file

<details>
<summary>Answer</summary>
**B)** Agents should persist their state to a Surgewave topic (e.g., "agent-state") keyed by agent ID. On restart, they load the latest state to resume where they left off.
</details>

### 4. What pattern enables multiple agents to coordinate on a task?
- [ ] A) Shared memory
- [ ] B) Direct function calls
- [ ] C) Publishing to shared Surgewave topics that other agents consume
- [ ] D) Global variables

<details>
<summary>Answer</summary>
**C)** Agents coordinate by publishing requests to topics (e.g., "inventory-requests") and consuming responses from other topics (e.g., "inventory-responses"). This decouples agents and enables async coordination.
</details>

### 5. What should happen if an agent's tool execution fails?
- [ ] A) Crash the agent
- [ ] B) Silently ignore the error
- [ ] C) Log the error, add to conversation context, and let the agent decide next steps
- [ ] D) Retry indefinitely

<details>
<summary>Answer</summary>
**C)** The agent should handle the error gracefully - log it, include the error information in its context, and use its reasoning capability to decide on an alternative action or inform the user.
</details>

## True/False

### 6. An event-driven agent should commit offsets before processing the event.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - The agent should commit offsets AFTER successfully processing the event. This ensures that if processing fails, the event will be redelivered for retry (at-least-once semantics).
</details>

### 7. Using Surgewave topics for agent communication naturally provides a replay capability for debugging.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Surgewave topics retain messages (based on retention settings), allowing you to replay events to debug agent behavior, test changes, or recover from failures.
</details>

## Code Challenge

### 8. What's wrong with this agent state persistence code?

```csharp
public async Task SaveStateAsync()
{
    var state = JsonSerializer.Serialize(_conversationHistory);
    await _producer.ProduceAsync("agent-state", "shared", state);
}

public async Task LoadStateAsync()
{
    await _consumer.SubscribeAsync(default, "agent-state");
    var result = await _consumer.ConsumeAsync();
    if (result != null)
    {
        _conversationHistory = JsonSerializer.Deserialize<List<Message>>(result.Value);
    }
}
```

<details>
<summary>Answer</summary>
Several issues:

1. **Using "shared" as key**: All agents would overwrite each other's state. Use `_agentId` as the key.

2. **Loading only one message**: Should keep consuming until we find the message with our agent's key, or use a compacted topic where the latest value per key is retained.

3. **No timestamp or metadata**: State should include when it was saved and agent version.

4. **No timeout on consume**: Could block forever if no messages exist.

Corrected version:
```csharp
public async Task SaveStateAsync()
{
    var state = new {
        agentId = _agentId,
        savedAt = DateTime.UtcNow,
        history = _conversationHistory
    };
    var json = JsonSerializer.Serialize(state);
    await _producer.ProduceAsync("agent-state", _agentId, json);
}

public async Task LoadStateAsync()
{
    await _consumer.SubscribeAsync(default, "agent-state");
    using var cts = new CancellationTokenSource(5000);

    while (!cts.IsCancellationRequested)
    {
        try
        {
            var result = await _consumer.ConsumeAsync(cts.Token);
            if (result?.Key == _agentId)
            {
                var state = JsonSerializer.Deserialize<AgentState>(result.Value);
                _conversationHistory = state.History.ToList();
                return;
            }
        }
        catch (OperationCanceledException) { break; }
    }
    // No state found - starting fresh
}
```
</details>

### 9. Complete the event handler to process different event types:

```csharp
private async Task<string?> HandleEventAsync(ConsumeResult message)
{
    var eventData = JsonSerializer.Deserialize<JsonElement>(message.Value);
    var eventType = eventData._____("type")._____();

    return eventType switch
    {
        "order_created" => await HandleOrderAsync(eventData),
        "payment_received" => await HandlePaymentAsync(eventData),
        "inventory_alert" => await HandleAlertAsync(eventData),
        _____ => _____  // Default case
    };
}
```

<details>
<summary>Answer</summary>

```csharp
private async Task<string?> HandleEventAsync(ConsumeResult message)
{
    var eventData = JsonSerializer.Deserialize<JsonElement>(message.Value);
    var eventType = eventData.GetProperty("type").GetString();

    return eventType switch
    {
        "order_created" => await HandleOrderAsync(eventData),
        "payment_received" => await HandlePaymentAsync(eventData),
        "inventory_alert" => await HandleAlertAsync(eventData),
        _ => null  // Default case: unknown event type, no response
    };
}
```

Key points:
- `GetProperty("type")` retrieves the "type" property from the JSON
- `GetString()` converts it to a C# string
- `_` is the discard pattern for the default case
- Returning `null` for unknown events means no response will be published
</details>

## Score
- 9/9: Excellent!
- 7-8/9: Good understanding
- 5-6/9: Review the lesson
- <5/9: Re-do the lesson
