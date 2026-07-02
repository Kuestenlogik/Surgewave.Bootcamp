// Surgewave Bootcamp - Unit 23, Lesson 5: Building Custom AI Agents
//
// In this lesson, you'll learn:
// - How to build autonomous AI agents with Surgewave
// - Implementing conversation state persistence
// - Creating event-driven agent workflows
// - Agent coordination patterns
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

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
    WriteIndented = true
};

// TODO: Step 1 - Start Surgewave runtime
// await using var runtime = await SurgewaveRuntime.CreateBuilder()
//     .WithPort(0)
//     .WithStorageEngine(StorageEngines.Memory)
//     .WithAutoCreateTopics(true)
//     .Build()
//     .StartAsync();
//
// var bootstrapServers = $"localhost:{runtime.Port}";
// Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

// TODO: Step 2 - Define agent state records
// public record ConversationMessage(string Role, string Content, DateTime Timestamp);
// public record AgentState(
//     string AgentId,
//     DateTime SavedAt,
//     List<ConversationMessage> History
// );

// TODO: Step 3 - Create the SurgewaveAgent class
// public class SurgewaveAgent
// {
//     private readonly string _agentId;
//     private readonly SurgewaveProducer<string, string> _producer;
//     private readonly SurgewaveConsumer<string, string> _consumer;
//     private readonly List<ConversationMessage> _history = new();
//     private readonly Dictionary<string, Func<JsonElement, Task<string>>> _tools = new();
//
//     public SurgewaveAgent(string agentId, string bootstrapServers) { ... }
//     public async Task SaveStateAsync() { ... }
//     public async Task LoadStateAsync() { ... }
//     public void RegisterTool(string name, Func<JsonElement, Task<string>> handler) { ... }
//     public async Task<string> ProcessInputAsync(string input) { ... }
// }

// TODO: Step 4 - Create and configure the agent
// var agent = new SurgewaveAgent("order-processor", bootstrapServers);

// TODO: Step 5 - Register agent tools
// agent.RegisterTool("check_inventory", async args => { ... });
// agent.RegisterTool("approve_order", async args => { ... });

// TODO: Step 6 - Run interactive agent loop
// Console.WriteLine("\n--- Interactive Agent Mode ---");
// Console.WriteLine("Type your message (or 'quit' to exit):\n");
//
// while (true)
// {
//     Console.Write("> ");
//     var input = Console.ReadLine();
//     if (input == "quit") break;
//
//     var response = await agent.ProcessInputAsync(input);
//     Console.WriteLine($"Agent: {response}\n");
// }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use Surgewave topics to persist agent conversation state");
Console.WriteLine("2. Event-driven agents react to messages from business topics");
Console.WriteLine("3. Tools give agents capabilities to interact with systems");
Console.WriteLine("4. Multiple agents can coordinate via shared topics");
Console.WriteLine("5. State persistence enables agent recovery after crashes");

Console.WriteLine();
Console.WriteLine("Lesson complete!");