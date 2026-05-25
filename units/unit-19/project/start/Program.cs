// Surgewave Bootcamp - Unit 23 Project: Surgewave Agent
//
// Build a complete AI agent that:
// 1. Connects to Surgewave via MCP protocol
// 2. Provides tools for topic management and messaging
// 3. Maintains conversation state via Surgewave topics
// 4. Monitors cluster health
//
// See README.md for full requirements.

using System.Text.Json;
using System.Text.Json.Serialization;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Agent ===");
Console.WriteLine();

// Configuration
var bootstrapServers = Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS") ?? "localhost:9092";
var agentId = Environment.GetEnvironmentVariable("Surgewave_AGENT_ID") ?? $"surgewave-agent-{Guid.NewGuid():N}";
var isMcpMode = args.Length > 0 && args[0] == "--mcp";

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

if (isMcpMode)
{
    Console.WriteLine("[Mode] Running as MCP server...");
    // TODO: Implement MCP server mode
    // See Lesson 4 for reference
}
else
{
    Console.WriteLine("[Mode] Running in interactive mode");
    await RunInteractiveMode();
}

async Task RunInteractiveMode()
{
    // TODO: Step 1 - Start Surgewave runtime (or connect to existing cluster)
    // await using var runtime = await SurgewaveRuntime.CreateBuilder()
    //     .WithPort(0)
    //     .WithStorageMode(StorageMode.Memory)
    //     .WithAutoCreateTopics(true)
    //     .Build()
    //     .StartAsync();
    // bootstrapServers = $"localhost:{runtime.Port}";

    Console.WriteLine($"Surgewave cluster: {bootstrapServers}");
    Console.WriteLine($"Agent ID: {agentId}");
    Console.WriteLine();

    // TODO: Step 2 - Create Surgewave clients
    // await using var producer = new SurgewaveProducer<string, string>(...);
    // await using var consumer = new SurgewaveConsumer<string, string>(...);

    // TODO: Step 3 - Load conversation state from Surgewave topic
    // await LoadStateAsync();

    // TODO: Step 4 - Run interactive loop
    Console.WriteLine("Agent ready. Type 'help' for commands, 'quit' to exit.");
    Console.WriteLine();

    while (true)
    {
        Console.Write("> ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input)) continue;
        if (input.Equals("quit", StringComparison.OrdinalIgnoreCase)) break;

        // TODO: Process input and generate response
        var response = await ProcessInputAsync(input);
        Console.WriteLine($"Agent: {response}");
        Console.WriteLine();
    }

    // TODO: Step 5 - Save state before exiting
    Console.WriteLine("Saving state...");
    // await SaveStateAsync();
    Console.WriteLine("Goodbye!");
}

async Task<string> ProcessInputAsync(string input)
{
    // TODO: Implement natural language processing and tool execution
    // For now, just echo back
    await Task.CompletedTask;

    if (input.Equals("help", StringComparison.OrdinalIgnoreCase))
    {
        return """
            Available commands:
              - list topics: Show all topics in the cluster
              - send <topic> <message>: Send a message to a topic
              - read <topic> [count]: Read messages from a topic
              - health: Show cluster health
              - help: Show this help
              - quit: Exit the agent
            """;
    }

    return $"You said: {input}\n(Implement tool execution to handle this request)";
}

// TODO: Implement these helper classes and methods:
// - SurgewaveAgentState (conversation history, session info)
// - LoadStateAsync / SaveStateAsync
// - Tool handlers for each Surgewave operation
// - MCP protocol handling for server mode