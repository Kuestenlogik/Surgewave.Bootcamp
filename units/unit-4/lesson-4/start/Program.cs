// Surgewave Bootcamp - Unit 6, Lesson 4: Event-Driven Patterns
//
// In this lesson, you'll learn to:
// - Implement CQRS pattern
// - Apply Event Sourcing
// - Design Saga for distributed transactions
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

// Events
public record OrderCreated(string OrderId, string CustomerId, decimal Amount, DateTime CreatedAt);
public record OrderShipped(string OrderId, DateTime ShippedAt);

// TODO: Implement CQRS read model
public class OrderReadModel
{
    public string OrderId { get; set; } = "";
    public string Status { get; set; } = "";
    // TODO: Add more fields
}

// TODO: Implement event-sourced aggregate
public class BankAccount
{
    public string AccountId { get; private set; } = "";
    public decimal Balance { get; private set; }

    // TODO: Implement Apply method
    // TODO: Implement Deposit method
    // TODO: Implement Withdraw method
}

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 6.4 - Event-Driven Patterns ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        // TODO: Demonstrate CQRS pattern
        Console.WriteLine("--- CQRS Demo ---");

        // TODO: Demonstrate Event Sourcing
        Console.WriteLine("--- Event Sourcing Demo ---");

        // TODO: Demonstrate Saga pattern
        Console.WriteLine("--- Saga Demo ---");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
