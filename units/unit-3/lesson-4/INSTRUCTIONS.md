# Lesson 2.4: Instructions

Follow these step-by-step instructions to complete the lesson.

## Note on Surgewave Docker Image

Surgewave doesn't have an official Docker image yet. This lesson demonstrates the Testcontainers pattern that you'll use once a Docker image is available. For now, we'll use the embedded broker but structure the code as if using containers.

## Step 1: Understand Testcontainers

Testcontainers starts real Docker containers for your tests:

```csharp
// When Surgewave has a Docker image:
var container = new ContainerBuilder()
    .WithImage("surgewave:latest")
    .WithPortBinding(9092, true)  // Random host port
    .WithWaitStrategy(Wait.ForUnixContainer()
        .UntilPortIsAvailable(9092))
    .Build();

await container.StartAsync();
var port = container.GetMappedPublicPort(9092);
```

## Step 2: Create Container-Style Fixture

For now, we simulate the container pattern:

```csharp
public class SurgewaveContainerFixture : IAsyncLifetime
{
    // In reality, this would be an IContainer
    // For now, we use embedded broker with same interface
    private ISurgewaveRuntime? _runtime;
    public int Port { get; private set; }

    public async Task InitializeAsync()
    {
        // Simulating container startup
        // When Docker image exists, replace with actual container
        _runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Port = _runtime.Port;
    }

    public async Task DisposeAsync()
    {
        if (_runtime != null)
            await _runtime.DisposeAsync();
    }
}
```

## Step 3: Use Collection Fixture for Shared Container

Share the container across multiple test classes:

```csharp
[CollectionDefinition("Surgewave Container")]
public class SurgewaveContainerCollection : ICollectionFixture<SurgewaveContainerFixture>
{
    // This class has no code, it just applies [CollectionDefinition]
}

[Collection("Surgewave Container")]
public class OrderServiceContainerTests
{
    private readonly SurgewaveContainerFixture _fixture;

    public OrderServiceContainerTests(SurgewaveContainerFixture fixture)
    {
        _fixture = fixture;
    }
}
```

## Step 4: Write Container-Based Tests

```csharp
[Collection("Surgewave Container")]
public class ContainerIntegrationTests
{
    private readonly SurgewaveContainerFixture _fixture;

    public ContainerIntegrationTests(SurgewaveContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Container_ProduceAndConsume_Works()
    {
        // Arrange
        await using var producer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = $"localhost:{_fixture.Port}");

        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{_fixture.Port}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        // Act
        var topic = $"container-test-{Guid.NewGuid():N}";
        await producer.ProduceAsync(topic, "key", "Container Test!");

        consumer.Subscribe(topic);
        var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(10));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Container Test!", result.Value);
    }
}
```

## Step 5: Configure for CI/CD

In CI, containers take longer to start. Use environment-based timeouts:

```csharp
public class SurgewaveContainerFixture : IAsyncLifetime
{
    private static TimeSpan StartupTimeout =>
        Environment.GetEnvironmentVariable("CI") != null
            ? TimeSpan.FromMinutes(2)
            : TimeSpan.FromSeconds(30);

    public async Task InitializeAsync()
    {
        // Add timeout for CI
        using var cts = new CancellationTokenSource(StartupTimeout);
        // ... start container
    }
}
```

## Complete Solution

See `completed/ContainerTests.cs` for the full implementation.

## Run and Verify

```bash
cd start
dotnet test --verbosity normal
```

## Future: Real Testcontainers

When Surgewave has a Docker image:

```csharp
public class SurgewaveContainerFixture : IAsyncLifetime
{
    private readonly IContainer _container;

    public SurgewaveContainerFixture()
    {
        _container = new ContainerBuilder()
            .WithImage("ghcr.io/kuestenlogik/surgewave:latest")
            .WithPortBinding(9092, true)
            .WithEnvironment("Surgewave_STORAGE_MODE", "memory")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilPortIsAvailable(9092)
                .UntilMessageIsLogged("Surgewave broker started"))
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

    public int Port { get; private set; }
}
```

---

**Ready for more?** → [Lesson 2.5: Benchmarking](../lesson-5/README.md)
