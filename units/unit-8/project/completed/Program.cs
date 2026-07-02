// Unit 12 Project: Reactive Dashboard - COMPLETED
// A real-time trading dashboard using Rx.NET with Surgewave events

using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using System.Collections.Concurrent;
using System.Text.Json;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Reactive Trading Dashboard ===\n");

// Disposables for cleanup
var disposables = new CompositeDisposable();

// Trade stream subject (acts as both observable and observer)
var tradeSubject = new Subject<TradeEvent>();
disposables.Add(tradeSubject);

// Alert subject for anomalies
var alertSubject = new Subject<AnomalyAlert>();
disposables.Add(alertSubject);

// Statistics tracking
var currentStats = new ConcurrentDictionary<string, DashboardStats>();
var totalTrades = 0;
var totalErrors = 0;
var windowCount = 0;

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

// Step 1: Create trade generator observable
Console.WriteLine("[Generator] Starting trade generation...\n");

var tradeGenerator = Observable.Interval(TimeSpan.FromMilliseconds(150))
    .Take(80) // Generate 80 trades
    .Select(_ => GenerateRandomTrade())
    .Do(trade =>
    {
        // Also produce to Surgewave for persistence
        var json = JsonSerializer.Serialize(trade);
        tradeProducer.ProduceAsync("trades", trade.Symbol, json)
            .GetAwaiter().GetResult();
    })
    .Subscribe(
        trade =>
        {
            Interlocked.Increment(ref totalTrades);
            tradeSubject.OnNext(trade);
        },
        ex =>
        {
            Interlocked.Increment(ref totalErrors);
            Console.WriteLine($"[Error] Trade generation error: {ex.Message}");
        }
    );

disposables.Add(tradeGenerator);

// Step 2: Subscribe to windowed statistics (3-second windows)
Console.WriteLine("[Stats] Setting up windowed statistics...\n");

var windowedStats = tradeSubject
    .Buffer(TimeSpan.FromSeconds(3))
    .Where(window => window.Count > 0)
    .Subscribe(window =>
    {
        Interlocked.Increment(ref windowCount);

        var bySymbol = window.GroupBy(t => t.Symbol);

        foreach (var group in bySymbol)
        {
            var trades = group.ToList();
            var symbol = group.Key;
            var basePrice = basePrices[symbol];

            var stats = new DashboardStats(
                Symbol: symbol,
                TradeCount: trades.Count,
                TotalVolume: trades.Sum(t => t.Volume),
                AvgPrice: trades.Average(t => t.Price),
                MinPrice: trades.Min(t => t.Price),
                MaxPrice: trades.Max(t => t.Price),
                PriceChange: (trades.Average(t => t.Price) - basePrice) / basePrice
            );

            currentStats[symbol] = stats;
        }

        var totalVolume = window.Sum(t => t.Volume);
        var tradesPerSecond = window.Count / 3.0;

        Console.WriteLine($"[Stats] Window {windowCount} closed: {window.Count} trades, {totalVolume:N0} total volume");
        Console.WriteLine($"[Stats] Processing rate: {tradesPerSecond:F1} trades/second\n");
    });

disposables.Add(windowedStats);

// Step 3: Subscribe to anomaly detection
Console.WriteLine("[Anomaly] Setting up anomaly detection...\n");

var lastPrices = new ConcurrentDictionary<string, decimal>();

var anomalyDetection = tradeSubject
    .GroupBy(t => t.Symbol)
    .SelectMany(symbolGroup =>
        symbolGroup
            .Scan(
                (PreviousPrice: 0m, CurrentTrade: (TradeEvent?)null, PriceChange: 0m),
                (acc, trade) =>
                {
                    var prevPrice = acc.PreviousPrice > 0 ? acc.PreviousPrice : trade.Price;
                    var change = (trade.Price - prevPrice) / prevPrice;
                    return (trade.Price, trade, change);
                })
            .Skip(1) // Skip the first (seed) value
            .Where(x => x.CurrentTrade != null && Math.Abs(x.PriceChange) > 0.05m)
            .Select(x => new AnomalyAlert(
                x.CurrentTrade!.Symbol,
                x.PriceChange > 0 ? "Price Spike" : "Price Drop",
                x.PriceChange,
                0.05m,
                DateTime.UtcNow
            ))
    )
    .DistinctUntilChanged(a => a.Symbol) // Avoid duplicate alerts for same symbol
    .Throttle(TimeSpan.FromSeconds(2)) // Rate limit alerts
    .Subscribe(alert =>
    {
        Console.ForegroundColor = alert.AlertType == "Price Spike" ? ConsoleColor.Red : ConsoleColor.Yellow;
        Console.WriteLine();
        Console.WriteLine("+----------------------------------------------------------+");
        Console.WriteLine("|                   ANOMALY DETECTED                        |");
        Console.WriteLine("+----------------------------------------------------------+");
        Console.WriteLine($"| Symbol: {alert.Symbol} | {alert.AlertType}: {alert.Value:+0.0%;-0.0%} | Threshold: {alert.Threshold:P1} |");
        Console.WriteLine("+----------------------------------------------------------+");
        Console.ResetColor();
        Console.WriteLine();

        alertSubject.OnNext(alert);
    });

disposables.Add(anomalyDetection);

// Step 4: Dashboard display subscription (updates every 2 seconds)
Console.WriteLine("[Dashboard] Setting up display...\n");

var dashboardDisplay = Observable.Interval(TimeSpan.FromSeconds(2))
    .Where(_ => currentStats.Count > 0)
    .Subscribe(_ =>
    {
        PrintDashboardHeader();

        foreach (var (symbol, stats) in currentStats.OrderBy(x => x.Key))
        {
            PrintStatsRow(stats);
        }

        Console.WriteLine("+----------------------------------------------------------+");
        Console.WriteLine();
    });

disposables.Add(dashboardDisplay);

// Wait for trade generation to complete plus a bit more for final window
await Task.Delay(TimeSpan.FromSeconds(15));

// Final dashboard display
Console.WriteLine("\n[Dashboard] Final Statistics:");
PrintDashboardHeader();

foreach (var (symbol, stats) in currentStats.OrderBy(x => x.Key))
{
    PrintStatsRow(stats);
}

Console.WriteLine("+----------------------------------------------------------+");

// Cleanup
Console.WriteLine("\n[Stream] Graceful shutdown initiated");
tradeSubject.OnCompleted();
alertSubject.OnCompleted();
disposables.Dispose();

Console.WriteLine($"[Stream] Final stats: {totalTrades} trades processed, {totalErrors} errors");
Console.WriteLine($"[Stream] Windows processed: {windowCount}");
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
    var changeColor = stats.PriceChange >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
    var changeStr = stats.PriceChange >= 0
        ? $"+{stats.PriceChange:P1}"
        : $"{stats.PriceChange:P1}";

    Console.Write($"| {stats.Symbol,-6} | {stats.TradeCount,6} | {stats.TotalVolume,8:N0} | {stats.AvgPrice,9:C2} | ");

    Console.ForegroundColor = changeColor;
    Console.Write($"{changeStr,-15}");
    Console.ResetColor();

    Console.WriteLine(" |");
}

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
