# Knowledge Assessment: Lesson 2.3 - Integration Testing

## Multiple Choice

### 1. What is the purpose of `IClassFixture<T>` in xUnit?
- [ ] A) To run tests in parallel
- [ ] B) To share a fixture instance across all tests in a single class
- [ ] C) To mock external dependencies
- [ ] D) To generate test data

<details>
<summary>Answer</summary>
**B)** To share a fixture instance across all tests in a single class. This allows expensive setup (like starting a broker) to happen once per class rather than per test.
</details>

### 2. What is the difference between `IClassFixture` and `ICollectionFixture`?
- [ ] A) IClassFixture is faster
- [ ] B) IClassFixture shares within one class, ICollectionFixture shares across multiple classes
- [ ] C) ICollectionFixture is deprecated
- [ ] D) IClassFixture only works with async tests

<details>
<summary>Answer</summary>
**B)** IClassFixture shares within one class, ICollectionFixture shares across multiple classes. Use ICollectionFixture when you need to share a broker across an entire test collection.
</details>

### 3. Why should you use unique topic names in integration tests?
- [ ] A) To improve performance
- [ ] B) To ensure test isolation even with a shared broker
- [ ] C) To avoid naming conflicts with production
- [ ] D) Because Surgewave requires unique topic names

<details>
<summary>Answer</summary>
**B)** To ensure test isolation even with a shared broker. Unique topic names prevent tests from interfering with each other when they share a broker instance.
</details>

### 4. What does `IAsyncLifetime` provide for test fixtures?
- [ ] A) Synchronous setup and teardown
- [ ] B) Async `InitializeAsync` and `DisposeAsync` methods for setup/cleanup
- [ ] C) Automatic retry on failure
- [ ] D) Parallel test execution

<details>
<summary>Answer</summary>
**B)** Async `InitializeAsync` and `DisposeAsync` methods for setup/cleanup. This is essential for Surgewave fixtures since broker startup and shutdown are async operations.
</details>

## True/False

### 5. A shared broker fixture starts once and is reused across all tests that use it.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - That's the main benefit of fixtures. The broker starts in `InitializeAsync` and shuts down in `DisposeAsync`, with all tests in between sharing the same instance.
</details>

### 6. Using `[CallerMemberName]` in a unique topic helper method automatically uses the test method name.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The `[CallerMemberName]` attribute automatically captures the calling method's name, making it easy to generate unique topic names based on the test method.
</details>

## Code Challenge

### 7. Complete this integration test fixture and test class:

```csharp
public class SurgewaveFixture : ____________
{
    public ISurgewaveRuntime Runtime { get; private set; } = null!;
    public int Port => Runtime.Port;

    public async Task ____________()
    {
        Runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();
    }

    public async Task ____________()
    {
        await Runtime.DisposeAsync();
    }
}

public class OrderTests : ____________<SurgewaveFixture>
{
    private readonly SurgewaveFixture _fixture;

    public OrderTests(SurgewaveFixture fixture)
    {
        _fixture = fixture;
    }

    private string UniqueTopic([CallerMemberName] string? name = null)
        => $"test-{name}-{Guid.NewGuid():N}";

    [Fact]
    public async Task CanProduceAndConsume()
    {
        var topic = ____________;  // Get unique topic name
        // ... test implementation
    }
}
```

<details>
<summary>Answer</summary>
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

public class OrderTests : IClassFixture<SurgewaveFixture>
{
    private readonly SurgewaveFixture _fixture;

    public OrderTests(SurgewaveFixture fixture)
    {
        _fixture = fixture;
    }

    private string UniqueTopic([CallerMemberName] string? name = null)
        => $"test-{name}-{Guid.NewGuid():N}";

    [Fact]
    public async Task CanProduceAndConsume()
    {
        var topic = UniqueTopic();  // Get unique topic name
        // ... test implementation
    }
}
```

Key points: Implement `IAsyncLifetime` for async setup/teardown, implement `IClassFixture<SurgewaveFixture>` to share the fixture, and call `UniqueTopic()` to get an isolated topic name.
</details>

## Score
- 7/7: Excellent! You understand integration test fixtures
- 5-6/7: Good grasp of shared fixture patterns
- 3-4/7: Review xUnit fixture types and lifecycle
- <3/7: Re-do the lesson with hands-on fixture creation
