// Unit 8 Project: Data Pipeline - COMPLETED
// A CSV -> Surgewave -> In-Memory Database pipeline

using System.Collections.Concurrent;
using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Client.Producer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Data Pipeline: CSV -> Surgewave -> In-Memory DB ===\n");

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

// Create producer and consumer
await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "inventory-consumer";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

Console.WriteLine("[Setup] Created producer and consumer\n");

// Step 1: Parse CSV data
Console.WriteLine("[CSV Reader] Loading product inventory...");

var products = new List<ProductInventory>();
var lines = csvData.Split('\n', StringSplitOptions.RemoveEmptyEntries);

// Skip header, parse each data line
for (int i = 1; i < lines.Length; i++)
{
    var line = lines[i].Trim();
    if (string.IsNullOrEmpty(line)) continue;

    var fields = line.Split(',');
    if (fields.Length < 5)
    {
        Console.WriteLine($"[CSV Reader] Warning: Skipping malformed line {i + 1}");
        continue;
    }

    try
    {
        var product = new ProductInventory(
            ProductId: fields[0].Trim(),
            Name: fields[1].Trim(),
            Category: fields[2].Trim(),
            StockQuantity: int.Parse(fields[3].Trim()),
            UnitPrice: decimal.Parse(fields[4].Trim()),
            LastUpdated: DateTime.UtcNow
        );

        // Validate data
        if (product.StockQuantity < 0)
        {
            Console.WriteLine($"[CSV Reader] Warning: Negative stock for {product.ProductId}, skipping");
            continue;
        }

        if (product.UnitPrice <= 0)
        {
            Console.WriteLine($"[CSV Reader] Warning: Invalid price for {product.ProductId}, skipping");
            continue;
        }

        products.Add(product);
    }
    catch (FormatException ex)
    {
        Console.WriteLine($"[CSV Reader] Warning: Parse error on line {i + 1}: {ex.Message}");
    }
}

Console.WriteLine($"[CSV Reader] Parsed {products.Count} products from CSV\n");

// Step 2: Produce inventory events to Surgewave
Console.WriteLine("[Producer] Streaming to Surgewave...");

foreach (var product in products)
{
    var json = JsonSerializer.Serialize(product);
    var result = await producer.ProduceAsync("inventory", product.Category, json);

    Console.WriteLine($"[Producer] {product.ProductId} ({product.Category}) -> Partition {result.Partition}");
}

Console.WriteLine($"[Producer] Published {products.Count} inventory events\n");

// Step 3: Consume and store in database
Console.WriteLine("[Consumer] Processing inventory stream...");

consumer.Subscribe("inventory");

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var processedCount = 0;

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        var message = await consumer.ConsumeAsync(cts.Token);
        if (message is null) continue;

        var product = JsonSerializer.Deserialize<ProductInventory>(message.Value);
        if (product is null) continue;

        // Store in database (idempotent - same ID will update)
        database[product.ProductId] = product;
        processedCount++;

        Console.WriteLine($"[Database] Stored: {product.ProductId} - {product.Name} ({product.StockQuantity} units)");

        // Stop after processing expected number
        if (processedCount >= products.Count) break;
    }
}
catch (OperationCanceledException)
{
    // Expected timeout if not all messages received
}

Console.WriteLine();

// Step 4: Query and display aggregations
Console.WriteLine("[Query] In-Memory Database Contents:");

var categoryStats = database.Values
    .GroupBy(p => p.Category)
    .Select(g => new CategoryStats(
        Category: g.Key,
        TotalProducts: g.Count(),
        TotalStock: g.Sum(p => p.StockQuantity),
        AveragePrice: g.Average(p => p.UnitPrice)
    ))
    .OrderBy(s => s.Category)
    .ToList();

foreach (var stats in categoryStats)
{
    Console.WriteLine($"  {stats.Category}: {stats.TotalProducts} products, " +
                      $"{stats.TotalStock} total stock, avg price ${stats.AveragePrice:F2}");
}

Console.WriteLine($"\n[Query] Total: {database.Count} products in database");
Console.WriteLine($"[Query] Total inventory value: ${database.Values.Sum(p => p.StockQuantity * p.UnitPrice):F2}");

Console.WriteLine("\n=== Data Pipeline Complete ===");

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