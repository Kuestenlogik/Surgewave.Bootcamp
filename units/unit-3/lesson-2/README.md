# Lesson 2.2: Mocking & Isolation

> **Difficulty:** Intermediate | **Duration:** 30 min | **Prerequisites:** Lesson 2.1

## Overview

In this lesson, you'll learn how to test Surgewave-dependent code without running an actual broker. Using mocking frameworks like Moq, you can isolate your business logic and write fast, focused unit tests.

## Learning Objectives

By the end of this lesson, you will:

1. Understand when to mock vs use embedded broker
2. Set up Moq for testing
3. Mock `IProducer<TKey, TValue>` interface
4. Mock `IConsumer<TKey, TValue>` interface
5. Verify interactions with mocks

## Key Concepts

### When to Mock vs Embedded Broker

| Use Mocking When | Use Embedded Broker When |
|------------------|--------------------------|
| Testing business logic only | Testing message serialization |
| Verifying interactions | Testing end-to-end flow |
| Need very fast tests | Testing actual Surgewave behavior |
| Testing error handling | Testing partition logic |
| CI/CD time constraints | Testing consumer groups |

### The IProducer Interface

Surgewave provides a mockable interface:

```csharp
public interface IProducer<TKey, TValue> : IAsyncDisposable
{
    Task<ProduceResult> ProduceAsync(
        string topic, TKey? key, TValue value,
        CancellationToken cancellationToken = default);

    Task FlushAsync(CancellationToken cancellationToken = default);
    ProtocolType Protocol { get; }
}
```

### The IConsumer Interface

```csharp
public interface IConsumer<TKey, TValue> : IAsyncDisposable
{
    void Subscribe(params string[] topics);
    Task<ConsumeResult<TKey, TValue>?> ConsumeAsync(
        CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    // ...
}
```

### Basic Mocking Pattern

```csharp
// Arrange - Set up mock
var mockProducer = new Mock<IProducer<string, Order>>();
mockProducer
    .Setup(p => p.ProduceAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<Order>(),
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(new ProduceResult { Topic = "orders", Offset = 1 });

// Act - Use the mock
var service = new OrderService(mockProducer.Object);
await service.ProcessOrderAsync(new Order { Id = "123" });

// Assert - Verify interactions
mockProducer.Verify(
    p => p.ProduceAsync("orders", "123", It.IsAny<Order>(), default),
    Times.Once);
```

## Prerequisites

- Completed Lesson 2.1: Unit Testing Basics
- Basic understanding of dependency injection

## Getting Started

1. Navigate to the `start` folder
2. Open `OrderServiceTests.cs` and follow the TODO comments
3. Reference `INSTRUCTIONS.md` for detailed guidance

## Verification

Run the tests:

```bash
cd completed
dotnet test
```

Expected output:
```
Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4
```

## Next Steps

Continue to [Lesson 2.3: Integration Testing](../lesson-3/README.md) to learn about test fixtures and sharing broker instances!
