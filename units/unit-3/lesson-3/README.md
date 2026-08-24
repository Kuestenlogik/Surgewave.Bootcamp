# Lesson 2.3: Integration Testing

> **Difficulty:** Intermediate | **Duration:** 45 min | **Prerequisites:** Lesson 2.2

## Overview

In this lesson, you'll learn how to write integration tests that share a single broker instance across multiple tests using xUnit fixtures. This approach balances test isolation with execution speed.

## Learning Objectives

By the end of this lesson, you will:

1. Understand xUnit class and collection fixtures
2. Create a shared broker fixture for integration tests
3. Use unique topic names for test isolation
4. Write multi-component integration tests
5. Handle test cleanup properly

## Key Concepts

### Test Fixtures in xUnit

xUnit provides two levels of sharing:

| Fixture Type | Scope | Use Case |
|--------------|-------|----------|
| `IClassFixture<T>` | Per test class | Share within one class |
| `ICollectionFixture<T>` | Per collection | Share across multiple classes |

### SurgewaveFixture Pattern

Create a fixture that manages the broker lifecycle:

```csharp
public class SurgewaveFixture : IAsyncLifetime
{
    public ISurgewaveRuntime Runtime { get; private set; } = null!;
    public int Port => Runtime.Port;

    public async Task InitializeAsync()
    {
        Runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Runtime.DisposeAsync();
    }
}
```

### Using Class Fixtures

```csharp
public class OrderTests : IClassFixture<SurgewaveFixture>
{
    private readonly SurgewaveFixture _fixture;

    public OrderTests(SurgewaveFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Test1()
    {
        // All tests in this class share the same broker
        var producer = CreateProducer(_fixture.Port);
    }
}
```

### Test Isolation with Unique Topics

Even with a shared broker, use unique topic names:

```csharp
private string UniqueTopic([CallerMemberName] string? name = null)
    => $"test-{name}-{Guid.NewGuid():N}";
```

## Prerequisites

- Completed Lesson 2.2: Mocking & Isolation

## Getting Started

1. Navigate to the `start` folder
2. Open `IntegrationTests.cs` and follow the TODO comments
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

Continue to [Lesson 2.4: Testcontainers](../lesson-4/README.md) to learn about Docker-based testing!
