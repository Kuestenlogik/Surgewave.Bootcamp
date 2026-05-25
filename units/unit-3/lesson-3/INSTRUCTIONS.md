# Lesson 2.3: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Create the Surgewave Fixture

Create a fixture class that manages broker lifecycle:

```csharp
public class SurgewaveFixture : IAsyncLifetime
{
    public ISurgewaveRuntime Runtime { get; private set; } = null!;
    public int Port => Runtime.Port;

    public async Task InitializeAsync()
    {
        Runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageMode(StorageMode.Memory)
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

**What this does:**
- `IAsyncLifetime` provides async setup/teardown
- `InitializeAsync` starts the broker once
- `DisposeAsync` cleans up after all tests

---

## Step 2: Create Test Class Using the Fixture

```csharp
public class ProducerConsumerIntegrationTests : IClassFixture<SurgewaveFixture>
{
    private readonly SurgewaveFixture _fixture;

    public ProducerConsumerIntegrationTests(SurgewaveFixture fixture)
    {
        _fixture = fixture;
    }

    // Helper to create unique topic names
    private static string UniqueTopic([CallerMemberName] string? name = null)
        => $"test-{name}-{Guid.NewGuid():N}";

    // Helper to create producer
    private SurgewaveProducer<string, string> CreateProducer()
        => new(opt => opt.BootstrapServers = $"localhost:{_fixture.Port}");

    // Helper to create consumer
    private SurgewaveConsumer<string, string> CreateConsumer()
        => new(opt =>
        {
            opt.BootstrapServers = $"localhost:{_fixture.Port}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });
}
```

---

## Step 3: Write End-to-End Test

Test the full produce-consume cycle:

```csharp
[Fact]
public async Task EndToEnd_ProduceAndConsume_MessageReceived()
{
    // Arrange
    var topic = UniqueTopic();
    await using var producer = CreateProducer();
    await using var consumer = CreateConsumer();

    // Act - Produce
    await producer.ProduceAsync(topic, "key", "Hello Integration Test!");

    // Act - Consume
    consumer.Subscribe(topic);
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Hello Integration Test!", result.Value);
}
```

---

## Step 4: Test Multiple Messages

```csharp
[Fact]
public async Task EndToEnd_MultipleMessages_AllReceived()
{
    // Arrange
    var topic = UniqueTopic();
    await using var producer = CreateProducer();
    await using var consumer = CreateConsumer();
    var messages = new[] { "First", "Second", "Third" };

    // Act - Produce all
    foreach (var msg in messages)
    {
        await producer.ProduceAsync(topic, "key", msg);
    }

    // Act - Consume all
    consumer.Subscribe(topic);
    var received = new List<string>();
    for (int i = 0; i < messages.Length; i++)
    {
        var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
        if (result != null)
            received.Add(result.Value);
    }

    // Assert
    Assert.Equal(messages.Length, received.Count);
    Assert.Equal(messages, received);
}
```

---

## Step 5: Test Key-Based Routing

```csharp
[Fact]
public async Task EndToEnd_SameKey_PreservesOrder()
{
    // Arrange
    var topic = UniqueTopic();
    await using var producer = CreateProducer();
    await using var consumer = CreateConsumer();

    // Act - Produce with same key (guarantees order)
    for (int i = 0; i < 5; i++)
    {
        await producer.ProduceAsync(topic, "same-key", $"Message-{i}");
    }

    // Consume
    consumer.Subscribe(topic);
    var received = new List<string>();
    for (int i = 0; i < 5; i++)
    {
        var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
        if (result != null)
            received.Add(result.Value);
    }

    // Assert - Order preserved
    Assert.Equal(5, received.Count);
    for (int i = 0; i < 5; i++)
    {
        Assert.Equal($"Message-{i}", received[i]);
    }
}
```

---

## Step 6: Test Multiple Consumers (Same Topic)

```csharp
[Fact]
public async Task EndToEnd_TwoConsumers_EachGetsAllMessages()
{
    // Arrange
    var topic = UniqueTopic();
    await using var producer = CreateProducer();
    await using var consumer1 = CreateConsumer();
    await using var consumer2 = CreateConsumer();

    // Produce
    await producer.ProduceAsync(topic, "k1", "Shared Message");

    // Both consumers subscribe to same topic (no group - each gets all)
    consumer1.Subscribe(topic);
    consumer2.Subscribe(topic);

    var result1 = await consumer1.ConsumeAsync(TimeSpan.FromSeconds(5));
    var result2 = await consumer2.ConsumeAsync(TimeSpan.FromSeconds(5));

    // Assert - Both got the message
    Assert.NotNull(result1);
    Assert.NotNull(result2);
    Assert.Equal("Shared Message", result1.Value);
    Assert.Equal("Shared Message", result2.Value);
}
```

---

## Complete Solution

Your completed test file should include the fixture and all tests.

---

## Run and Verify

```bash
cd start
dotnet test --verbosity normal
```

---

## Knowledge Check

Before moving on, make sure you can answer:

1. Why use fixtures instead of creating a broker per test?
2. How do unique topic names provide isolation?
3. What's the difference between `IClassFixture` and `ICollectionFixture`?
4. When would you need collection fixtures?

---

**Ready for more?** → [Lesson 2.4: Testcontainers](../lesson-4/README.md)
