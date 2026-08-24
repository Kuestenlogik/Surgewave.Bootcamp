# Lesson 2.1: Unit Testing Basics

> **Difficulty:** Intermediate | **Duration:** 30 min | **Prerequisites:** Unit 1 completion

## Overview

In this lesson, you'll learn how to write unit tests for Surgewave producers and consumers using xUnit and the embedded broker. This is the foundation for testing all Surgewave applications.

## Learning Objectives

By the end of this lesson, you will:

1. Set up an xUnit test project for Surgewave
2. Write tests using the embedded broker
3. Test producer message delivery
4. Test consumer message reception
5. Use async test patterns effectively

## Key Concepts

### Why Embedded Broker Testing?

The embedded broker is perfect for unit tests because:
- **Fast**: Starts in milliseconds, runs in-memory
- **Isolated**: Each test gets its own broker instance
- **Realistic**: Tests actual Surgewave behavior, not mocks
- **No Dependencies**: No Docker or external services needed

### Test Project Setup

Your test project needs these references:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
  <PackageReference Include="xunit" Version="2.*" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
  <ProjectReference Include="../../../src/Kuestenlogik.Surgewave.Runtime/Kuestenlogik.Surgewave.Runtime.csproj" />
  <ProjectReference Include="../../../src/Kuestenlogik.Surgewave.Client/Kuestenlogik.Surgewave.Client.csproj" />
</ItemGroup>
```

### Basic Test Pattern

```csharp
[Fact]
public async Task TestName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up broker, producer, consumer
    await using var runtime = await SurgewaveRuntime.CreateBuilder()
        .WithPort(0)
        .WithStorageEngine(StorageEngines.Memory)
        .Build()
        .StartAsync();

    // Act - Perform the operation
    // ...

    // Assert - Verify the result
    // ...
}
```

### Testing Producers

```csharp
[Fact]
public async Task Producer_DeliversMessage_ReturnsOffset()
{
    await using var runtime = await CreateRuntime();
    await using var producer = CreateProducer(runtime.Port);

    var result = await producer.ProduceAsync("test", "key", "value");

    Assert.True(result.Offset >= 0);
    Assert.Equal("test", result.Topic);
}
```

### Testing Consumers

```csharp
[Fact]
public async Task Consumer_ReceivesProducedMessage()
{
    await using var runtime = await CreateRuntime();
    await using var producer = CreateProducer(runtime.Port);
    await using var consumer = CreateConsumer(runtime.Port);

    // Produce
    await producer.ProduceAsync("test", "key", "hello");

    // Consume
    consumer.Subscribe("test");
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));

    Assert.NotNull(result);
    Assert.Equal("hello", result.Value);
}
```

## Prerequisites

- Completed Unit 1: Surgewave Fundamentals
- Basic xUnit knowledge

## Getting Started

1. Navigate to the `start` folder
2. Open `ProducerTests.cs` and follow the TODO comments
3. Reference `INSTRUCTIONS.md` for detailed guidance

## Verification

Run the tests:

```bash
cd completed
dotnet test
```

Expected output:
```
Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3
```

## Next Steps

Continue to [Lesson 2.2: Mocking & Isolation](../lesson-2/README.md) to learn how to test without a broker!
