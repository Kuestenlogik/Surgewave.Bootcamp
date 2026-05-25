# Unit 3 Project: Payment Processing System

Build a robust payment processing system with transactions, retries, and dead letter queues.

## Scenario

You're building the payment processing backend for a fintech company. The system must:
1. Process payment transactions reliably
2. Implement retry logic for transient failures
3. Route failed payments to a dead letter queue (DLQ) for manual review
4. Track processing metrics and success rates
5. Support idempotent processing to prevent duplicate charges

## Requirements

### Functional Requirements

- [ ] Process payment events from `payments.pending` topic
- [ ] Validate payment data (amount, account, currency)
- [ ] Retry failed payments up to 3 times with exponential backoff
- [ ] Route permanently failed payments to `payments.dlq`
- [ ] Include metadata (retry count, failure reason, timestamps) in DLQ messages
- [ ] Track success/failure/retry metrics
- [ ] Support idempotent processing (detect duplicate payment IDs)

### Technical Requirements

- [ ] Use message headers for correlation and metadata
- [ ] Implement exponential backoff (100ms, 200ms, 400ms)
- [ ] Handle transient errors (network, timeout) vs permanent errors (validation)
- [ ] Log all processing steps for auditability
- [ ] Clean shutdown with in-flight payment completion

## Payment Event Schema

```csharp
public record PaymentEvent(
    string PaymentId,
    string AccountFrom,
    string AccountTo,
    decimal Amount,
    string Currency,
    DateTime CreatedAt,
    PaymentType Type
);

public enum PaymentType { Transfer, Withdrawal, Deposit, Refund }

public record PaymentResult(
    string PaymentId,
    PaymentStatus Status,
    string? ErrorMessage,
    int RetryCount,
    DateTime ProcessedAt
);

public enum PaymentStatus { Completed, Failed, PendingRetry }
```

## Processing Flow

```
                           ┌───────────────────┐
                           │ payments.pending  │
                           └─────────┬─────────┘
                                     │
                                     ▼
                           ┌───────────────────┐
                           │     Validate      │
                           └─────────┬─────────┘
                                     │
                    ┌────────────────┼────────────────┐
                    │                │                │
                    ▼                ▼                ▼
              ┌─────────┐      ┌─────────┐      ┌─────────┐
              │ Valid   │      │ Invalid │      │ Retry?  │
              └────┬────┘      └────┬────┘      └────┬────┘
                   │                │                │
                   ▼                │                │
           ┌───────────────┐       │                │
           │   Process     │       │                │
           └───────┬───────┘       │                │
                   │               │                │
          ┌────────┼────────┐      │                │
          │        │        │      │                │
          ▼        ▼        ▼      ▼                ▼
    ┌─────────┐ ┌──────┐ ┌──────────────┐  ┌───────────────┐
    │ Success │ │ Fail │ │    DLQ       │  │     DLQ       │
    └─────────┘ └──┬───┘ │ (permanent)  │  │ (max retries) │
                   │     └──────────────┘  └───────────────┘
                   ▼
             ┌─────────┐
             │  Retry  │
             │ (delay) │
             └─────────┘
```

## Expected Output

```
=== Payment Processing System ===

[Startup] Broker started on port 9092
[Startup] Topics created: payments.pending, payments.completed, payments.dlq

[Producer] Sending payment PAY-001 ($150.00 USD)
[Producer] Sending payment PAY-002 ($-50.00 USD) [INVALID]
[Producer] Sending payment PAY-003 ($500.00 USD)
[Producer] Sending payment PAY-004 ($100.00 USD) [WILL TIMEOUT]

[Processor] Processing PAY-001 ($150.00 USD)
[Processor] PAY-001 -> SUCCESS

[Processor] Processing PAY-002 ($-50.00 USD)
[Processor] PAY-002 -> VALIDATION_ERROR: Amount must be positive
[Processor] PAY-002 -> Sent to DLQ (permanent failure)

[Processor] Processing PAY-003 ($500.00 USD)
[Processor] PAY-003 -> RETRY (attempt 1): Temporary network error
[Processor] PAY-003 -> RETRY (attempt 2): Temporary network error
[Processor] PAY-003 -> SUCCESS (after 2 retries)

[Processor] Processing PAY-004 ($100.00 USD)
[Processor] PAY-004 -> RETRY (attempt 1): Timeout
[Processor] PAY-004 -> RETRY (attempt 2): Timeout
[Processor] PAY-004 -> RETRY (attempt 3): Timeout
[Processor] PAY-004 -> Sent to DLQ (max retries exceeded)

=== Processing Summary ===
Processed: 4 payments
Successful: 2 (50.0%)
Failed (DLQ): 2 (50.0%)
Total retries: 5
Average latency: 234ms

=== Dead Letter Queue ===
PAY-002: VALIDATION_ERROR - Amount must be positive
PAY-004: MAX_RETRIES - Timeout after 3 attempts
```

## Getting Started

```bash
cd bootcamp/unit-3/project/start
dotnet run
```

## Hints

1. Use `Headers` for metadata: `x-retry-count`, `x-error-type`, `x-original-timestamp`
2. Distinguish transient errors (retry) from permanent errors (DLQ immediately)
3. Use `Stopwatch` to track processing latency
4. Store processed payment IDs in a `HashSet` for idempotency
5. Use exponential backoff: `delay = baseDelay * Math.Pow(2, attempt)`

## Error Types

| Error Type | Retry? | Example |
|------------|--------|---------|
| ValidationError | No | Invalid amount, missing fields |
| NetworkError | Yes | Connection timeout |
| ServiceUnavailable | Yes | Downstream service down |
| InsufficientFunds | No | Account balance too low |
| DuplicatePayment | No | Already processed |

## Evaluation Criteria

| Criteria | Points |
|----------|--------|
| Payments processed correctly | 20 |
| Retry logic with backoff | 25 |
| DLQ routing works | 20 |
| Metadata in headers | 15 |
| Metrics tracking | 10 |
| Idempotency handling | 10 |

## Solution

Compare your implementation with [completed/](completed/README.md) when done.
