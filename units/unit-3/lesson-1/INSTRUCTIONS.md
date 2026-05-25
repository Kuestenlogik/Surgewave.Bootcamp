# Lesson 2.1: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Understand the Test Project Structure

For testing Surgewave applications, we use xUnit with the embedded broker. The project structure:

```
lesson-1/
├── start/
│   ├── Lesson2_1.csproj      # Test project
│   └── ProducerTests.cs       # Tests to complete
└── completed/
    ├── Lesson2_1.csproj
    └── ProducerTests.cs       # Reference solution
```

---

## Step 2: Set Up the Test Project

The `.csproj` file includes necessary references:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <ProjectReference Include="../../../../src/Kuestenlogik.Surgewave.Broker/Kuestenlogik.Surgewave.Broker.csproj" />
    <ProjectReference Include="../../../../src/Kuestenlogik.Surgewave.Runtime/Kuestenlogik.Surgewave.Runtime.csproj" />
    <ProjectReference Include="../../../../src/Kuestenlogik.Surgewave.Client/Kuestenlogik.Surgewave.Client.csproj" />
  </ItemGroup>
</Project>
```

---

## Step 3: Create Helper Methods

Add helper methods to reduce boilerplate:

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

public class ProducerTests
{
    private static async Task<ISurgewaveRuntime> CreateRuntimeAsync()
    {
        return await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)  // Auto-assign port
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();
    }

    private static SurgewaveProducer<string, string> CreateProducer(int port)
    {
        return new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{port}";
        });
    }

    private static SurgewaveConsumer<string, string> CreateConsumer(int port)
    {
        return new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{port}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });
    }
}
```

---

## Step 4: Write Your First Test - Producer Delivery

Test that a producer successfully delivers a message:

```csharp
[Fact]
public async Task Producer_SendsMessage_ReturnsValidOffset()
{
    // Arrange
    await using var runtime = await CreateRuntimeAsync();
    await using var producer = CreateProducer(runtime.Port);

    // Act
    var result = await producer.ProduceAsync("test-topic", "test-key", "test-value");

    // Assert
    Assert.Equal("test-topic", result.Topic);
    Assert.Equal(0, result.Offset);  // First message gets offset 0
    Assert.Equal(0, result.Partition);
}
```

**What this tests:**
- Producer can connect to broker
- Message is successfully written
- Correct metadata is returned

---

## Step 5: Write Consumer Test - Message Reception

Test that a consumer receives what was produced:

```csharp
[Fact]
public async Task Consumer_ReceivesMessage_WithCorrectContent()
{
    // Arrange
    await using var runtime = await CreateRuntimeAsync();
    await using var producer = CreateProducer(runtime.Port);
    await using var consumer = CreateConsumer(runtime.Port);

    // Produce a message
    await producer.ProduceAsync("greetings", "user-1", "Hello, Surgewave!");

    // Act - Subscribe and consume
    consumer.Subscribe("greetings");
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));

    // Assert
    Assert.NotNull(result);
    Assert.Equal("user-1", result.Key);
    Assert.Equal("Hello, Surgewave!", result.Value);
    Assert.Equal(0, result.Offset);
}
```

**What this tests:**
- End-to-end message flow
- Correct key/value deserialization
- Offset tracking

---

## Step 6: Write Multiple Messages Test

Test producing and consuming multiple messages:

```csharp
[Fact]
public async Task ProducerConsumer_MultipleMessages_AllReceived()
{
    // Arrange
    await using var runtime = await CreateRuntimeAsync();
    await using var producer = CreateProducer(runtime.Port);
    await using var consumer = CreateConsumer(runtime.Port);

    // Produce 5 messages
    for (int i = 0; i < 5; i++)
    {
        await producer.ProduceAsync("numbers", $"key-{i}", $"value-{i}");
    }

    // Act - Consume all messages
    consumer.Subscribe("numbers");
    var messages = new List<string>();

    for (int i = 0; i < 5; i++)
    {
        var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
        if (result != null)
            messages.Add(result.Value);
    }

    // Assert
    Assert.Equal(5, messages.Count);
    Assert.Contains("value-0", messages);
    Assert.Contains("value-4", messages);
}
```

---

## Complete Solution

Your completed `ProducerTests.cs` should look like this:

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

namespace Lesson2_1;

public class ProducerTests
{
    private static async Task<ISurgewaveRuntime> CreateRuntimeAsync()
    {
        return await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();
    }

    private static SurgewaveProducer<string, string> CreateProducer(int port)
    {
        return new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{port}";
        });
    }

    private static SurgewaveConsumer<string, string> CreateConsumer(int port)
    {
        return new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{port}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });
    }

    [Fact]
    public async Task Producer_SendsMessage_ReturnsValidOffset()
    {
        // Arrange
        await using var runtime = await CreateRuntimeAsync();
        await using var producer = CreateProducer(runtime.Port);

        // Act
        var result = await producer.ProduceAsync("test-topic", "test-key", "test-value");

        // Assert
        Assert.Equal("test-topic", result.Topic);
        Assert.Equal(0, result.Offset);
        Assert.Equal(0, result.Partition);
    }

    [Fact]
    public async Task Consumer_ReceivesMessage_WithCorrectContent()
    {
        // Arrange
        await using var runtime = await CreateRuntimeAsync();
        await using var producer = CreateProducer(runtime.Port);
        await using var consumer = CreateConsumer(runtime.Port);

        await producer.ProduceAsync("greetings", "user-1", "Hello, Surgewave!");

        // Act
        consumer.Subscribe("greetings");
        var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user-1", result.Key);
        Assert.Equal("Hello, Surgewave!", result.Value);
        Assert.Equal(0, result.Offset);
    }

    [Fact]
    public async Task ProducerConsumer_MultipleMessages_AllReceived()
    {
        // Arrange
        await using var runtime = await CreateRuntimeAsync();
        await using var producer = CreateProducer(runtime.Port);
        await using var consumer = CreateConsumer(runtime.Port);

        for (int i = 0; i < 5; i++)
        {
            await producer.ProduceAsync("numbers", $"key-{i}", $"value-{i}");
        }

        // Act
        consumer.Subscribe("numbers");
        var messages = new List<string>();

        for (int i = 0; i < 5; i++)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
            if (result != null)
                messages.Add(result.Value);
        }

        // Assert
        Assert.Equal(5, messages.Count);
        Assert.Contains("value-0", messages);
        Assert.Contains("value-4", messages);
    }
}
```

---

## Run and Verify

```bash
cd start
dotnet test --verbosity normal
```

Expected output:

```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3
```

---

## Experiment (Optional)

### 1. Test Error Scenarios

```csharp
[Fact]
public async Task Consumer_NoMessages_ReturnsNull()
{
    await using var runtime = await CreateRuntimeAsync();
    await using var consumer = CreateConsumer(runtime.Port);

    consumer.Subscribe("empty-topic");
    var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));

    Assert.Null(result);
}
```

### 2. Test Message Order

```csharp
[Fact]
public async Task Consumer_ReceivesMessages_InOrder()
{
    // Produce messages 0, 1, 2, 3, 4
    // Verify they're received in the same order
}
```

### 3. Test Different Value Types

```csharp
[Fact]
public async Task Producer_JsonSerialization_Works()
{
    // Test with a complex object that serializes to JSON
}
```

---

## Knowledge Check

Before moving on, make sure you can answer:

1. Why do we use `WithPort(0)` in tests?
2. What does `await using` ensure in our tests?
3. Why is the embedded broker better than mocking for some tests?
4. When would you prefer mocking over the embedded broker?

---

**Ready for more?** → [Lesson 2.2: Mocking & Isolation](../lesson-2/README.md)
