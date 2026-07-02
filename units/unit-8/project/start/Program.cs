// Unit 12 Project: Reactive Dashboard
// Build a real-time dashboard using Rx.NET with Surgewave events

using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using System.Text.Json;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Reactive Trading Dashboard ===\n");

// Event types
public record TradeEvent(
    string Symbol,
    decimal Price,
    int Volume,
    DateTime Timestamp,
    TradeType Type
);

public record DashboardStats(
    string Symbol,
    int TradeCount,
    decimal TotalVolume,
    decimal AvgPrice,
    decimal MinPrice,
    decimal MaxPrice,
    decimal PriceChange
);

public record AnomalyAlert(
    string Symbol,
    string AlertType,
    decimal Value,
    decimal Threshold,
    DateTime DetectedAt
);

public enum TradeType { Buy, Sell }

// Disposables for cleanup
var disposables = new CompositeDisposable();

// Trade stream subject (acts as both observable and observer)
var tradeSubject = new Subject<TradeEvent>();
disposables.Add(tradeSubject);

// Statistics tracking
var symbolStats = new Dictionary<string, List<decimal>>();
var totalTrades = 0;
var totalErrors = 0;

Console.WriteLine("[Stream] Connecting to trade feed...");

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;

// Create producer for trades
await using var tradeProducer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);

Console.WriteLine("[Stream] Connected - receiving live trades\n");

// Sample symbols and price ranges
var symbols = new[] { "AAPL", "MSFT", "GOOGL", "TSLA", "AMZN" };
var basePrices = new Dictionary<string, decimal>
{
    ["AAPL"] = 185.00m,
    ["MSFT"] = 378.00m,
    ["GOOGL"] = 141.00m,
    ["TSLA"] = 248.00m,
    ["AMZN"] = 178.00m
};

// TODO: Step 1 - Create trade generator observable
// Use Observable.Interval to generate trades every 200ms
// Hint: Observable.Interval(TimeSpan.FromMilliseconds(200))
//           .Take(50) // Generate 50 trades
//           .Select(_ => GenerateRandomTrade())
//           .Do(trade => {
//               // Produce to Surgewave for persistence
//               var json = JsonSerializer.Serialize(trade);
//               tradeProducer.ProduceAsync("trades", trade.Symbol, json).GetAwaiter().GetResult();
//           })
//           .Subscribe(trade => tradeSubject.OnNext(trade))

Console.WriteLine("[Generator] Starting trade generation...\n");

// TODO: Implement trade generator here


// TODO: Step 2 - Subscribe to windowed statistics
// Use Buffer to collect trades in 3-second windows
// Calculate statistics per window
// Hint: tradeSubject
//           .Buffer(TimeSpan.FromSeconds(3))
//           .Subscribe(window => ProcessWindow(window))

Console.WriteLine("[Stats] Setting up windowed statistics...\n");

// TODO: Implement windowed statistics here


// TODO: Step 3 - Subscribe to anomaly detection
// Detect price changes > 5% compared to base price
// Hint: tradeSubject
//           .GroupBy(t => t.Symbol)
//           .SelectMany(group => group.Scan(...))
//           .Where(change => Math.Abs(change) > 0.05m)

Console.WriteLine("[Anomaly] Setting up anomaly detection...\n");

// TODO: Implement anomaly detection here


// TODO: Step 4 - Dashboard display subscription
// Format and display the dashboard output
// Hint: Use Console.SetCursorPosition for live updates

Console.WriteLine("[Dashboard] Setting up display...\n");

// TODO: Implement dashboard display here


// Wait for completion
await Task.Delay(TimeSpan.FromSeconds(15));

// Cleanup
Console.WriteLine("\n[Stream] Graceful shutdown initiated");
disposables.Dispose();

Console.WriteLine($"[Stream] Final stats: {totalTrades} trades processed, {totalErrors} errors");
Console.WriteLine("\n=== Reactive Dashboard Complete ===");

// Helper: Generate random trade
TradeEvent GenerateRandomTrade()
{
    var symbol = symbols[Random.Shared.Next(symbols.Length)];
    var basePrice = basePrices[symbol];

    // Random price variation (-3% to +8% for occasional spikes)
    var variation = (decimal)(Random.Shared.NextDouble() * 0.11 - 0.03);
    var price = basePrice * (1 + variation);

    var volume = Random.Shared.Next(100, 5000);
    var type = Random.Shared.Next(2) == 0 ? TradeType.Buy : TradeType.Sell;

    return new TradeEvent(symbol, Math.Round(price, 2), volume, DateTime.UtcNow, type);
}

// Helper: Print dashboard header
void PrintDashboardHeader()
{
    Console.WriteLine("+----------------------------------------------------------+");
    Console.WriteLine("|                  LIVE TRADING DASHBOARD                   |");
    Console.WriteLine("+----------------------------------------------------------+");
    Console.WriteLine("| Symbol | Trades | Volume   | Avg Price | Change          |");
    Console.WriteLine("+----------------------------------------------------------+");
}

// Helper: Print stats row
void PrintStatsRow(DashboardStats stats)
{
    var changeStr = stats.PriceChange >= 0
        ? $"+{stats.PriceChange:P1}"
        : $"{stats.PriceChange:P1}";

    Console.WriteLine($"| {stats.Symbol,-6} | {stats.TradeCount,6} | {stats.TotalVolume,8:N0} | {stats.AvgPrice,9:C2} | {changeStr,-15} |");
}
