# Unit 2 Project: Surgewave Test Suite

Build a comprehensive xUnit test suite for Surgewave's Producer and Consumer functionality.

## Scenario

You're the QA engineer responsible for ensuring the Surgewave messaging system works correctly. Your task is to create a complete test suite that validates:
1. Producer functionality (single message, batch, partitioning)
2. Consumer functionality (subscribe, consume, consumer groups)
3. Integration scenarios (produce-consume roundtrip)
4. Error handling (invalid topics, timeouts)

## Requirements

### Functional Requirements

- [ ] Test producer sends single messages correctly
- [ ] Test producer batch sending works efficiently
- [ ] Test key-based partitioning consistency
- [ ] Test consumer receives messages in order
- [ ] Test consumer groups distribute messages
- [ ] Test offset management and commits
- [ ] Test error scenarios (invalid topic, connection issues)

### Technical Requirements

- [ ] Use xUnit with embedded broker (no external dependencies)
- [ ] Implement test fixtures for shared broker instance
- [ ] Use async/await patterns correctly
- [ ] Include meaningful assertions with descriptive messages
- [ ] Clean up resources properly (IAsyncDisposable)

## Test Categories

```
TestSuite/
├── ProducerTests.cs      - Basic producer operations
├── ConsumerTests.cs      - Basic consumer operations
├── IntegrationTests.cs   - End-to-end scenarios
└── EdgeCaseTests.cs      - Error handling and edge cases
```

## Test Cases to Implement

### Producer Tests
- `Producer_SendsMessage_ReturnsValidOffset`
- `Producer_SendsWithKey_PartitionsConsistently`
- `Producer_BatchSend_AllMessagesDelivered`
- `Producer_FlushAsync_CompletesAllPending`

### Consumer Tests
- `Consumer_Subscribe_ReceivesMessages`
- `Consumer_MultipleSubscriptions_ReceivesFromAll`
- `Consumer_GroupConsumption_DistributesMessages`
- `Consumer_CommitOffset_ResumeFromCorrectPosition`

### Integration Tests
- `ProducerConsumer_RoundTrip_MessageIntact`
- `ProducerConsumer_HighVolume_NoMessageLoss`
- `ProducerConsumer_MultiPartition_OrderPerPartition`

### Edge Case Tests
- `Producer_InvalidTopic_ThrowsException`
- `Consumer_Timeout_ReturnsNull`
- `Consumer_EmptyTopic_NoMessages`

## Expected Output

```
=== Surgewave Test Suite ===

Running Producer Tests...
  [PASS] Producer_SendsMessage_ReturnsValidOffset
  [PASS] Producer_SendsWithKey_PartitionsConsistently
  [PASS] Producer_BatchSend_AllMessagesDelivered

Running Consumer Tests...
  [PASS] Consumer_Subscribe_ReceivesMessages
  [PASS] Consumer_CommitOffset_ResumeFromCorrectPosition

Running Integration Tests...
  [PASS] ProducerConsumer_RoundTrip_MessageIntact
  [PASS] ProducerConsumer_HighVolume_NoMessageLoss

Running Edge Case Tests...
  [PASS] Consumer_Timeout_ReturnsNull

=== Test Results ===
Total: 11 | Passed: 11 | Failed: 0
All tests passed!
```

## Getting Started

```bash
cd bootcamp/unit-2/project/start
dotnet test --verbosity normal
```

## Hints

1. Use `SurgewaveRuntime.CreateBuilder().WithPort(0)` for random available port
2. Use `StorageMode.Memory` for fast in-memory testing
3. Create a shared fixture for broker to speed up tests
4. Use `TimeSpan.FromSeconds(5)` as reasonable timeout
5. Remember to dispose producers and consumers

## Evaluation Criteria

| Criteria | Points |
|----------|--------|
| Producer tests pass | 20 |
| Consumer tests pass | 20 |
| Integration tests pass | 25 |
| Edge case tests pass | 15 |
| Code organization | 10 |
| Proper resource cleanup | 10 |

## Solution

Compare your implementation with [completed/](completed/README.md) when done.
