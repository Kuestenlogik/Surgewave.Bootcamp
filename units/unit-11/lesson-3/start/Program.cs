// Surgewave Bootcamp - Unit 5, Lesson 3: Hybrid Deployments
//
// In this lesson, you'll learn:
// - Running Surgewave alongside Kafka
// - Dual-write patterns
// - Topic-by-topic migration
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 5.3 - Hybrid Deployments ===");
        Console.WriteLine();

        // Simulate two systems: "Kafka" (port 9093) and Surgewave (port 9092)
        // In real migration, Kafka would be your existing system

        Console.WriteLine("Setting up hybrid environment...");

        await using var surgewave = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        await using var kafka = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9093)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine($"Surgewave: localhost:9092");
        Console.WriteLine($"Kafka (simulated): localhost:9093");
        Console.WriteLine();

        // TODO: Create dual-write producer
        // Write messages to both systems simultaneously

        // TODO: Implement topic router
        // Route different topics to different systems

        // TODO: Demonstrate data mirroring
        // Copy data from Kafka to Surgewave

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
