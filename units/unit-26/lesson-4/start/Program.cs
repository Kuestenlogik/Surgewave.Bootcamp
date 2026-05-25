// Surgewave Bootcamp - Unit 24, Lesson 4: Azure Blob Storage
//
// In this lesson, you'll learn:
// - How to configure Azure Blob Storage as cold tier
// - Azure access tiers (Hot, Cool, Cold, Archive)
// - Lifecycle management policies
// - Archive tier rehydration
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 24.4 - Azure Blob Storage ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var topic = "orders";
var azureSimPath = Path.Combine(Path.GetTempPath(), "surgewave-azure-sim", Guid.NewGuid().ToString());

// TODO: Step 1 - Define Azure configuration
// public record AzureBlobStorageConfig(
//     string AccountName,
//     string ContainerName,
//     string BlobPrefix,
//     AzureAccessTier AccessTier = AzureAccessTier.Cool
// );
//
// public enum AzureAccessTier { Hot, Cool, Cold, Archive }
//
// var config = new AzureBlobStorageConfig(
//     AccountName: "surgewavearchive",
//     ContainerName: "surgewave-segments",
//     BlobPrefix: "topics",
//     AccessTier: AzureAccessTier.Cold
// );
//
// Console.WriteLine("\n--- Azure Configuration ---");
// Console.WriteLine($"Account: {config.AccountName} (simulated)");
// Console.WriteLine($"Container: {config.ContainerName}");
// Console.WriteLine($"Blob Prefix: {config.BlobPrefix}");
// Console.WriteLine($"Access Tier: {config.AccessTier}");

// TODO: Step 2 - Create simulated Azure Blob client
// See INSTRUCTIONS.md for the SimulatedAzureBlobClient implementation

// TODO: Step 3 - Create segment data and upload to Azure
// await using var producer = new SurgewaveProducer<string, string>(opt =>
// {
//     opt.BootstrapServers = $"localhost:{runtime.Port}";
// });
//
// Console.WriteLine("\n--- Uploading Segments to Azure ---");
//
// var uploader = new AzureBlobUploader(azureClient, config);
//
// for (int seg = 0; seg < 2; seg++)
// {
//     // Create and upload segment similar to S3 lesson
// }

// TODO: Step 4 - List blobs with their access tiers
// Console.WriteLine("\n--- Blobs Listed ---");
// foreach (var blob in azureClient.ListBlobs(config.ContainerName, config.BlobPrefix))
// {
//     Console.WriteLine($"{blob.Name} ({blob.Size:N0} bytes) [{blob.AccessTier}]");
// }

// TODO: Step 5 - Simulate lifecycle transition to Archive
// Console.WriteLine("\n--- Simulating Lifecycle Transition to Archive ---");
// var blobName = $"{config.BlobPrefix}/{topic}/0/00000000000000000000.log.zst";
// await azureClient.SetBlobTierAsync(config.ContainerName, blobName, AzureAccessTier.Archive);
// Console.WriteLine($"Blob {blobName} moved to Archive tier");

// TODO: Step 6 - Retrieve from Archive tier (with rehydration)
// Console.WriteLine("\n--- Retrieving from Archive Tier ---");
// var retriever = new AzureBlobRetriever(azureClient, config);
// var retrieved = await retriever.RetrieveSegmentAsync(topic, 0, 0);
// Console.WriteLine($"Retrieved: {retrieved.BlobName}");
// Console.WriteLine($"  Decompressed: {retrieved.DecompressedSize:N0} bytes");
// Console.WriteLine($"  Retrieval time: {retrieved.RetrievalTime.TotalMilliseconds:F1}ms");

// TODO: Step 7 - Calculate cost comparison
// Console.WriteLine("\n--- Cost Comparison (100 GB, 10k reads, 1k writes/month) ---");
// var storageBytes = 100L * 1024 * 1024 * 1024;
// var reads = 10000;
// var writes = 1000;
//
// foreach (var tier in Enum.GetValues<AzureAccessTier>())
// {
//     var cost = AzureCostCalculator.CalculateMonthlyCost(storageBytes, reads, writes, tier);
//     Console.WriteLine($"{tier,-8}: ${cost:F2}/month");
// }

// Cleanup
try
{
    if (Directory.Exists(azureSimPath))
    {
        Directory.Delete(azureSimPath, recursive: true);
    }
}
catch { /* Ignore cleanup errors */ }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Azure provides four access tiers with different cost profiles");
Console.WriteLine("2. Archive tier requires rehydration before reading");
Console.WriteLine("3. Lifecycle policies automate tier transitions");
Console.WriteLine("4. Cold tier offers good balance of cost and instant access");
Console.WriteLine("5. Use managed identity for secure Azure access");

Console.WriteLine();
Console.WriteLine("Lesson complete!");