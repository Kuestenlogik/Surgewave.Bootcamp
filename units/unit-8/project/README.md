# Unit 12 Project: Reactive Dashboard

Build a reactive real-time dashboard using Rx.NET (System.Reactive) that displays live-updating metrics from Surgewave events.

## Scenario

You're building a monitoring dashboard for a trading platform. The dashboard must:
1. Display live trade events as they arrive
2. Calculate running statistics (volume, average price, trade count)
3. Detect anomalies (price spikes, unusual volume)
4. Update the console display reactively

## Requirements

### Functional Requirements

- [ ] Subscribe to trade events as an Observable stream
- [ ] Calculate windowed statistics (5-second windows)
- [ ] Detect price changes > 5% within a window
- [ ] Display formatted dashboard output with live updates
- [ ] Handle backpressure when trades arrive faster than processing

### Technical Requirements

- [ ] Use System.Reactive for stream processing
- [ ] Implement custom operators for windowing
- [ ] Use proper subscription disposal
- [ ] Handle errors gracefully without crashing the stream

## Event Schema

```csharp
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

public enum TradeType { Buy, Sell }
```

## Expected Output

```
=== Reactive Trading Dashboard ===

[Stream] Connecting to trade feed...
[Stream] Connected - receiving live trades

+----------------------------------------------------------+
|                  LIVE TRADING DASHBOARD                   |
+----------------------------------------------------------+
| Symbol | Trades | Volume   | Avg Price | Change          |
+----------------------------------------------------------+
| AAPL   |     15 |   45,200 |   $185.42 | +2.3%          |
| MSFT   |     12 |   32,100 |   $378.15 | -0.8%          |
| GOOGL  |      8 |   12,400 |  $141.89  | +1.2%          |
+----------------------------------------------------------+

[Alert] AAPL price spike detected: $180.00 -> $192.50 (+6.9%)

[Stats] Window closed: 35 trades, 89,700 total volume
[Stats] Processing rate: 7 trades/second

+----------------------------------------------------------+
|                   ANOMALY DETECTED                        |
+----------------------------------------------------------+
| Symbol: TSLA | Price Change: +8.2% | Volume Spike: 3.5x  |
+----------------------------------------------------------+

[Stream] Graceful shutdown initiated
[Stream] Final stats: 156 trades processed, 0 errors
```

## Getting Started

```bash
cd bootcamp/unit-12/project/start
dotnet run
```

## Hints

1. Use `Observable.Interval` to simulate trade generation
2. Use `Buffer(TimeSpan.FromSeconds(5))` for windowed aggregation
3. Use `GroupBy` to partition by symbol
4. Use `Scan` for running calculations
5. Use `DistinctUntilChanged` to avoid duplicate alerts

## Rx.NET Operators Used

| Operator | Purpose |
|----------|---------|
| `Buffer` | Collect events into time windows |
| `GroupBy` | Partition stream by key |
| `Scan` | Running aggregation |
| `Where` | Filter events |
| `Select` | Transform events |
| `Throttle` | Rate limiting |
| `DistinctUntilChanged` | Deduplicate |

## Evaluation Criteria

| Criteria | Points |
|----------|--------|
| Reactive stream setup | 20 |
| Windowed statistics | 25 |
| Anomaly detection | 20 |
| Dashboard formatting | 15 |
| Error handling & cleanup | 20 |

## Solution

Compare your implementation with [completed/](completed/README.md) when done.
