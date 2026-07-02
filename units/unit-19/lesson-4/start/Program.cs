// Surgewave Bootcamp - Unit 23, Lesson 4: AI Agent Integration - Claude Desktop
//
// In this lesson, you'll learn:
// - How to create a standalone MCP server for Claude Desktop
// - Implementing stdio transport
// - Configuring Claude Desktop to use Surgewave MCP
// - Security and rate limiting
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text.Json;
using System.Text.Json.Serialization;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

// For demonstration, we'll simulate the Claude Desktop integration
// In production, this would run as a subprocess launched by Claude Desktop

Console.WriteLine("=== Surgewave Bootcamp: Lesson 23.4 - AI Agent Integration ===");
Console.WriteLine();

// TODO: Step 1 - Read configuration from environment
// var bootstrapServers = Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS") ?? "localhost:9092";
// Console.WriteLine($"[Config] Bootstrap servers: {bootstrapServers}");

// TODO: Step 2 - For demonstration, start embedded Surgewave runtime
// await using var runtime = await SurgewaveRuntime.CreateBuilder()
//     .WithPort(0)
//     .WithStorageEngine(StorageEngines.Memory)
//     .WithAutoCreateTopics(true)
//     .Build()
//     .StartAsync();
//
// var bootstrapServers = $"localhost:{runtime.Port}";
// Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

// TODO: Step 3 - Create Surgewave clients
// await using var producer = new SurgewaveProducer<string, string>(opt =>
// {
//     opt.BootstrapServers = bootstrapServers;
// });
//
// await using var consumer = new SurgewaveConsumer<string, string>(opt =>
// {
//     opt.BootstrapServers = bootstrapServers;
//     opt.GroupId = $"mcp-agent-{Guid.NewGuid():N}";
//     opt.AutoOffsetReset = AutoOffsetReset.Earliest;
// });

// TODO: Step 4 - Implement StdioMcpServer class
// See INSTRUCTIONS.md for the implementation

// TODO: Step 5 - Create and configure MCP server
// var server = new StdioMcpServer("surgewave-mcp", "1.0.0", jsonOptions);
// server.ConnectToSurgewave(bootstrapServers, producer, consumer);

// TODO: Step 6 - Register Surgewave tools
// RegisterSurgewaveTools(server);

// TODO: Step 7 - Simulate Claude Desktop interaction
// Console.WriteLine("\n--- Simulating Claude Desktop Interaction ---\n");
// await SimulateClaudeInteraction(server);

Console.WriteLine();
Console.WriteLine("--- Claude Desktop Configuration ---");
Console.WriteLine("To use this server with Claude Desktop, add to your config:");
Console.WriteLine();
Console.WriteLine("""
{
    "mcpServers": {
        "surgewave": {
            "command": "dotnet",
            "args": ["run", "--project", "/path/to/this/project"],
            "env": {
                "Surgewave_BOOTSTRAP_SERVERS": "localhost:9092"
            }
        }
    }
}
""");

Console.WriteLine();
Console.WriteLine("Config file locations:");
Console.WriteLine("  macOS: ~/Library/Application Support/Claude/claude_desktop_config.json");
Console.WriteLine("  Windows: %APPDATA%\\Claude\\claude_desktop_config.json");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Claude Desktop launches MCP servers as subprocesses");
Console.WriteLine("2. Communication happens over stdio (stdin/stdout)");
Console.WriteLine("3. Use environment variables for credentials");
Console.WriteLine("4. Implement rate limiting to protect Surgewave");
Console.WriteLine("5. Log all operations for debugging");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// JSON options
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};