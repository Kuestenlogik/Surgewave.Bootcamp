// Surgewave Bootcamp - Unit 10, Lesson 6: Live Schema Inference & AI-Assisted Evolution
//
// In this lesson, you'll learn:
// - JSON Schema auto-derivation from message data
// - Format detection heuristics (date-time, email, URI, UUID)
// - Configuring schema inference (sample size, refresh interval)
// - REST API: GET /schemas/infer/{topic}, POST /schemas/infer/{topic}/register
// - AI-Assisted Schema Evolution: automatic change detection
// - Breaking level classification (None, Minor, Major)
// - C# migration code generation
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 10.6 - Schema Inference & AI Evolution ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine("Surgewave running on localhost:9092");
        Console.WriteLine();

        // TODO 1: Produce 50 messages to 'orders' topic with these fields:
        //   id (integer), email (string), createdAt (ISO date-time),
        //   orderId (UUID), total (number), profileUrl (https:// URI)

        // TODO 2: Implement SimpleSchemaInferenceEngine with:
        //   - Field type detection (string/number/boolean)
        //   - Required field detection (≥ 95% threshold)
        //   - Format heuristics: date-time, email, uri, uuid

        // TODO 3: Collect 50 messages from the topic and run inference
        //         Print the inferred schema showing detected formats

        // TODO 4: Implement SchemaEvolutionAdvisor with:
        //   - DetectChanges (added / removed fields)
        //   - ClassifyBreakingLevel (None / Minor / Major)
        //   - GenerateMigrationCode (C# snippet)

        // TODO 5: Scenario A — add optional 'phone' field → expect Minor
        //         Print breaking level and generated migration code

        // TODO 6: Scenario B — remove required 'email' field → expect Major
        //         Print breaking level and generated migration code

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
