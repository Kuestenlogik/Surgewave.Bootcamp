// Surgewave Bootcamp - Unit 4, Lesson 4: Security
//
// In this lesson, you'll learn to:
// - Configure TLS encryption
// - Set up SASL authentication
// - Implement ACLs
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 4.4 - Security ===");
        Console.WriteLine();

        // For this demo, we'll simulate security configuration
        // In production, you would use actual certificates

        // TODO: Start broker with security enabled
        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            // TODO: Add TLS configuration
            // .WithTls(tls => { ... })
            // TODO: Add SASL configuration
            // .WithSasl(sasl => { ... })
            .Build()
            .StartAsync();

        Console.WriteLine($"Broker started on port {runtime.Port}");

        // TODO: Create secure producer
        await using var producer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            // TODO: Add security configuration
            // opt.SecurityProtocol = SecurityProtocol.SaslSsl;
        });

        // TODO: Configure ACLs using admin client

        // TODO: Test security by sending messages

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
