// Surgewave Bootcamp - Unit 5, Lesson 5: Hybrid Deployment Patterns
//
// In this lesson, you'll learn:
// - Running Surgewave alongside Kafka (side-by-side migration)
// - MirrorMaker-style topic mirroring with Surgewave Cluster Linking
// - Gradual consumer migration (dual-read pattern)
// - Rollback strategy
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
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 5.5 - Hybrid Deployment Patterns ===");
        Console.WriteLine();

        // Start two brokers: Surgewave (9092) and simulated Kafka (9093)
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

        Console.WriteLine("Surgewave:           localhost:9092");
        Console.WriteLine("Kafka (sim):     localhost:9093");
        Console.WriteLine();

        // TODO 1: Seed 20 messages into the "Kafka" broker (port 9093) for the orders topic

        // TODO 2: Implement and start a ClusterLinkMirror that copies orders from Kafka to Surgewave

        // TODO 3: Implement a DualReadConsumer
        //         - Kafka primary initially
        //         - PromoteSurgewave() method to switch to Surgewave as primary
        //         - DemoteSurgewave() method for rollback

        // TODO 4: Consume 5 messages with Kafka as primary, print source
        //         Then promote Surgewave, consume 5 more messages, print source

        // TODO 5: Simulate a rollback — demote Surgewave, consume one more message
        //         Confirm it comes from Kafka

        // TODO 6: Validate message counts on both brokers match

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
