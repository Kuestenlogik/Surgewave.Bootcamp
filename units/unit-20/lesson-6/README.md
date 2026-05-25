# Lesson 20.6: Agent Memory

### Why Agents Need Long-Term Memory

Without memory, every agent session starts from scratch. Agents cannot learn user preferences, recall past interactions, or build up knowledge over time. Surgewave's agent memory system provides persistent storage scoped per agent.

### IAgentMemoryStore

The core interface with InMemory and File backends:

```csharp
public interface IAgentMemoryStore
{
    Task SaveMemoryAsync(string agentId, MemoryEntry entry, CancellationToken ct = default);
    Task<MemoryEntry?> GetMemoryAsync(string agentId, string memoryId, CancellationToken ct = default);
    Task DeleteMemoryAsync(string agentId, string memoryId, CancellationToken ct = default);
    IAsyncEnumerable<MemoryEntry> SearchMemoriesAsync(string agentId, MemoryQuery query, CancellationToken ct = default);
    IAsyncEnumerable<MemoryEntry> ListMemoriesAsync(string agentId, MemoryType? type = null, CancellationToken ct = default);
    Task<MemorySummary> GetSummaryAsync(string agentId, CancellationToken ct = default);
}
```

**Backends:**
- `InMemoryAgentMemoryStore` -- Fast, non-persistent, good for testing
- `FileAgentMemoryStore` -- Persists to disk as JSON files

### The 6 Memory Types

| Memory Type | Description | Default Importance |
|-------------|-------------|-------------------|
| `ConversationSummary` | Summary of a previous conversation | 0.5 |
| `LearnedFact` | A fact learned during interactions | 0.5 |
| `UserPreference` | A discovered user preference | 0.7 |
| `ToolResult` | The result of a tool invocation | 0.5 |
| `Episodic` | Memory tied to a specific session or event | 0.5 |
| `Procedural` | How-to knowledge for performing tasks | 0.5 |

### MemoryEntry

```csharp
public sealed record MemoryEntry
{
    public required string Id { get; init; }
    public required string AgentId { get; init; }
    public required MemoryType Type { get; init; }
    public required string Content { get; init; }
    public string? SessionId { get; init; }
    public float Importance { get; init; } = 0.5f;       // 0.0 to 1.0
    public IReadOnlyDictionary<string, string>? Tags { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public int AccessCount { get; init; }
    public DateTimeOffset? LastAccessedAt { get; init; }
}
```

### AgentMemoryContext

A convenience wrapper scoped to a specific agent:

```csharp
var store = new InMemoryAgentMemoryStore();
var options = new MemoryOptions();
var memory = new AgentMemoryContext(store, agentId: "assistant-1", options);

// Save facts and preferences
await memory.SaveFactAsync("The user's favorite language is C#", importance: 0.8f);
await memory.SavePreferenceAsync("User prefers dark mode");
await memory.SaveEpisodeAsync("Helped user debug a null reference exception", sessionId: "session-42");

// Recall memories by text query
var results = await memory.RecallAsync("programming language", maxResults: 5);

// Recall by type
var preferences = await memory.RecallByTypeAsync(MemoryType.UserPreference, maxResults: 10);

// Delete a memory
await memory.ForgetAsync(memoryId: results[0].Id);

// Get summary statistics
var summary = await memory.GetSummaryAsync();
```

### ConversationSummarizer

Produces extractive summaries of conversation history without requiring an LLM. Uses topic extraction, question counting, and assistant action analysis:

```csharp
var history = new List<AgentMessage>
{
    new("user", "How do I configure Surgewave for high throughput?"),
    new("assistant", "You can tune batch size, linger.ms, and compression..."),
    new("user", "What about consumer parallelism?"),
    new("assistant", "Use multiple partitions and consumer instances...")
};

var summary = ConversationSummarizer.Summarize(history);
// "Conversation with 4 messages (2 user, 2 assistant). User asked 2 question(s).
//  Topics: configure, surgewave, throughput, consumer, parallelism.
//  Actions: provided explanations."
```

---

[Back to Unit 20](../README.md)
