# Unit 9 Project: Observability Stack

Build a fully observable Surgewave application with logging, metrics, and distributed tracing.

## Scenario

You're building a payment processing system that requires full observability:
1. Structured logging for audit trails
2. Metrics for monitoring payment volumes and latencies
3. Distributed tracing to follow payments through the pipeline
4. Health checks and diagnostics

## Requirements

### Functional Requirements

- [ ] Process payment requests through Surgewave
- [ ] Log all payment operations with correlation IDs
- [ ] Track metrics: payment count, total amount, processing time
- [ ] Propagate trace context through the message pipeline
- [ ] Display observability summary at the end

### Technical Requirements

- [ ] Use ILogger with structured logging
- [ ] Implement custom metrics (counters, histograms)
- [ ] Use Activity for distributed tracing
- [ ] Include timing measurements for latency metrics

## Data Schema

```csharp
public record PaymentRequest(
    string PaymentId,
    string CustomerId,
    decimal Amount,
    string Currency,
    DateTime RequestedAt
);

public record PaymentResult(
    string PaymentId,
    bool Success,
    string? ErrorMessage,
    TimeSpan ProcessingTime
);
```

## Expected Output

```
=== Observability Stack: Payment Processing ===

[Logger] Payment system initialized
[Trace] Starting trace: payment-pipeline

[Producer] Processing payment requests...
[Logger] PaymentId=PAY-001, CustomerId=C-100, Amount=150.00 USD
[Metric] payments.requested +1
[Trace] Span: produce-payment (TraceId: abc123)

[Consumer] Processing payments...
[Logger] Processing PAY-001 (TraceId: abc123, SpanId: def456)
[Metric] payments.processed +1
[Metric] payments.amount.total +150.00
[Metric] payments.latency.ms 45

[Logger] PaymentId=PAY-002, CustomerId=C-101, Amount=2500.00 USD
[Metric] payments.requested +1
[Logger] Processing PAY-002 (TraceId: abc123, SpanId: ghi789)
[Metric] payments.processed +1
[Metric] payments.amount.total +2500.00
[Metric] payments.latency.ms 38

=== Observability Summary ===
[Metrics]
  - Payments Requested: 5
  - Payments Processed: 5
  - Total Amount: $4,250.00
  - Avg Latency: 42ms
  - P99 Latency: 78ms

[Tracing]
  - Total Spans: 10
  - Root Trace: payment-pipeline

[Logging]
  - Info: 12 entries
  - Warning: 1 entries
  - Error: 0 entries

=== Observability Stack Complete ===
```

## Getting Started

```bash
cd bootcamp/unit-9/project/start
dotnet run
```

## Hints

1. Use `Activity.Current?.TraceId` to access trace context
2. Store metrics in a dictionary for easy aggregation
3. Use `Stopwatch` for accurate timing measurements
4. Structured logging: `logger.LogInformation("Processing {PaymentId}", id)`

## Evaluation Criteria

| Criteria | Points |
|----------|--------|
| Structured logging implemented | 20 |
| Metrics tracking works | 25 |
| Distributed tracing propagates | 25 |
| Summary statistics accurate | 20 |
| Code organization | 10 |

## Solution

Compare your implementation with [completed/](completed/README.md) when done.
