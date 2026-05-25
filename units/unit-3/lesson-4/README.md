# Lesson 2.4: Testcontainers

> **Difficulty:** Intermediate | **Duration:** 45 min | **Prerequisites:** Lesson 2.3

## Overview

In this lesson, you'll learn how to use Testcontainers for .NET to run Surgewave in Docker containers for realistic integration tests. This is especially useful for CI/CD pipelines and testing against real infrastructure.

## Learning Objectives

By the end of this lesson, you will:

1. Understand when to use Testcontainers vs embedded broker
2. Set up Testcontainers.Surgewave (or generic container)
3. Run tests with containerized Surgewave
4. Configure containers for different test scenarios
5. Integrate with CI/CD pipelines

## Key Concepts

### When to Use Testcontainers

| Embedded Broker | Testcontainers |
|----------------|----------------|
| Fast startup | Realistic environment |
| No Docker required | Tests Docker deployment |
| Unit/Integration tests | E2E/Acceptance tests |
| Local development | CI/CD pipelines |

### Testcontainers Setup

Add the package:
```xml
<PackageReference Include="Testcontainers" Version="3.*" />
```

### Basic Container Pattern

```csharp
public class SurgewaveContainerFixture : IAsyncLifetime
{
    private readonly IContainer _container;
    public int Port { get; private set; }

    public SurgewaveContainerFixture()
    {
        _container = new ContainerBuilder()
            .WithImage("surgewave:latest")
            .WithPortBinding(9092, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(9092))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Port = _container.GetMappedPublicPort(9092);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
```

## Prerequisites

- Completed Lesson 2.3: Integration Testing
- Docker Desktop installed

## Note

Since Surgewave doesn't have an official Docker image yet, this lesson demonstrates the Testcontainers pattern using a simulated approach. The same pattern applies when Surgewave has a Docker image.

## Getting Started

1. Navigate to the `start` folder
2. Open `ContainerTests.cs` and follow the TODO comments
3. Reference `INSTRUCTIONS.md` for detailed guidance

## Verification

Run the tests (requires Docker):

```bash
cd completed
dotnet test
```

## Next Steps

Continue to [Lesson 2.5: Benchmarking](../lesson-5/README.md) to learn about performance testing!
