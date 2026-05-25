// Unit 8 Project: Data Pipeline
// Build a CSV -> Surgewave -> In-Memory Database pipeline

using System.Collections.Concurrent;
using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Client.Producer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Data Pipeline: CSV -> Surgewave -> In-Memory DB ===\n");

// Data models
public record ProductInventory(
    string ProductId,
    string Name,
    string Category,
    int StockQuantity,
    decimal UnitPrice,
    DateTime LastUpdated
);

public record CategoryStats(
    string Category,
    int TotalProducts,
    int TotalStock,
    decimal AveragePrice
);

// Sample CSV data (simulating file content)
var csvData = """
ProductId,Name,Category,StockQuantity,UnitPrice
P001,Laptop Pro,Electronics,50,1299.99
P002,Wireless Mouse,Electronics,200,29.99
P003,Office Chair,Furniture,75,249.99
P004,Standing Desk,Furniture,30,599.99
P005,USB-C Cable,Electronics,500,12.99
P006,Monitor Arm,Furniture,100,89.99
""";

// In-memory database
var database = new ConcurrentDictionary<string, ProductInventory>();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;

// TODO: Create producer and consumer
Console.WriteLine("[Setup] Surgewave runtime started...\n");

// TODO: Create SurgewaveProducer<string, string> with bootstrapServers
// TODO: Create SurgewaveConsumer<string, string> with:
//   - bootstrapServers
//   - GroupId = "inventory-consumer"
//   - AutoOffsetReset = AutoOffsetReset.Earliest

// Step 1: Parse CSV data
Console.WriteLine("[CSV Reader] Loading product inventory...");

// TODO: Parse the CSV data into a list of ProductInventory objects
// - Skip the header line
// - Split each line by comma
// - Create ProductInventory with current DateTime for LastUpdated
// - Handle parsing errors gracefully
// - Print the count of parsed products

var products = new List<ProductInventory>();

// TODO: Implement CSV parsing here
// Hint: Use csvData.Split('\n') to get lines
//       Skip the first line (header)
//       Use line.Split(',') to get fields

Console.WriteLine($"[CSV Reader] Parsed {products.Count} products from CSV\n");

// Step 2: Produce inventory events to Surgewave
Console.WriteLine("[Producer] Streaming to Surgewave...");

// TODO: Produce each product to the "inventory" topic
// - Use Category as the key for partitioning
// - Serialize the ProductInventory to JSON
// - Print each product with its assigned partition
// Hint: var result = await producer.ProduceAsync("inventory", product.Category, JsonSerializer.Serialize(product));

foreach (var product in products)
{
    // TODO: Produce to Surgewave and print partition assignment
    Console.WriteLine($"[Producer] {product.ProductId} ({product.Category}) -> Partition ?");
}

Console.WriteLine($"[Producer] Published {products.Count} inventory events\n");

// Step 3: Consume and store in database
Console.WriteLine("[Consumer] Processing inventory stream...");

// TODO: Subscribe consumer to "inventory" topic
// TODO: Consume from "inventory" topic and store in database
// - Deserialize JSON to ProductInventory
// - Store in the concurrent dictionary (key = ProductId)
// - Print each stored product
// Hint: while (!ct.IsCancellationRequested)
//       {
//           var result = await consumer.ConsumeAsync(ct);
//           if (result is null) continue;
//           // use result.Value
//       }

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

// TODO: Implement consumer and database storage here

Console.WriteLine();

// Step 4: Query and display aggregations
Console.WriteLine("[Query] In-Memory Database Contents:");

// TODO: Calculate and display category statistics
// - Group products by category
// - Calculate: TotalProducts, TotalStock, AveragePrice
// - Print formatted results
// Hint: Use LINQ GroupBy and aggregate functions

// TODO: Implement category aggregation query here

Console.WriteLine("\n=== Data Pipeline Complete ===");