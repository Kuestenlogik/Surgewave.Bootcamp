# Knowledge Assessment: Lesson 2.1 - Unit Testing Basics

## Multiple Choice

### 1. Why is the embedded broker ideal for unit tests?
- [ ] A) It provides more features than the standalone broker
- [ ] B) It starts fast, runs in-memory, requires no external dependencies
- [ ] C) It has better error messages for debugging
- [ ] D) It supports more serialization formats

<details>
<summary>Answer</summary>
**B)** It starts fast, runs in-memory, requires no external dependencies. The embedded broker provides realistic testing without Docker or external services, making tests fast and self-contained.
</details>

### 2. What is the recommended naming convention for test methods?
- [ ] A) `Test1`, `Test2`, `Test3`
- [ ] B) `TestMethodName`
- [ ] C) `MethodName_Scenario_ExpectedBehavior`
- [ ] D) `Should_DoSomething_WhenCondition`

<details>
<summary>Answer</summary>
**C)** `MethodName_Scenario_ExpectedBehavior`. This convention clearly describes what is being tested, under what conditions, and what the expected result is.
</details>

### 3. Which port number should you use for test brokers to avoid conflicts?
- [ ] A) Always use 9092
- [ ] B) Use 0 for auto-assign
- [ ] C) Use 8080
- [ ] D) Use a random number between 1000-9000

<details>
<summary>Answer</summary>
**B)** Use 0 for auto-assign. When you use `.WithPort(0)`, the system automatically assigns an available port, preventing conflicts with other processes or parallel tests.
</details>

### 4. What is the correct order of the Arrange-Act-Assert pattern?
- [ ] A) Assert what you expect, Arrange the setup, Act on the system
- [ ] B) Act first, then Arrange, finally Assert
- [ ] C) Arrange the setup, Act on the system, Assert the results
- [ ] D) Assert expectations, Act to verify, Arrange cleanup

<details>
<summary>Answer</summary>
**C)** Arrange the setup, Act on the system, Assert the results. This is the standard testing pattern: set up preconditions, execute the code under test, verify the outcomes.
</details>

## True/False

### 5. Tests using the embedded broker are less realistic than tests using mocks because they don't test actual Surgewave behavior.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - The embedded broker tests actual Surgewave behavior, making it MORE realistic than mocks. It's the best of both worlds: fast startup with real behavior testing.
</details>

### 6. The `[Fact]` attribute in xUnit marks a method as a test that takes no parameters.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The `[Fact]` attribute indicates a test method that runs once with no parameters. For parameterized tests, you would use `[Theory]` with `[InlineData]`.
</details>

## Code Challenge

### 7. Complete this unit test for verifying producer delivery:

```csharp
[Fact]
public async Task Producer_DeliversMessage_ReturnsValidOffset()
{
    // Arrange
    await using var runtime = await SurgewaveRuntime.CreateBuilder()
        .WithPort(____)
        .WithStorageMode(StorageMode.Memory)
        .Build()
        .StartAsync();

    await using var producer = new SurgewaveProducer<string, string>(opt =>
        opt.BootstrapServers = $"localhost:{runtime.____}");

    // Act
    var result = await producer.ProduceAsync("test-topic", "key", "value");

    // Assert
    Assert.____(result.Offset >= 0);
    Assert.____("test-topic", result.Topic);
}
```

<details>
<summary>Answer</summary>
```csharp
[Fact]
public async Task Producer_DeliversMessage_ReturnsValidOffset()
{
    // Arrange
    await using var runtime = await SurgewaveRuntime.CreateBuilder()
        .WithPort(0)  // Auto-assign port
        .WithStorageMode(StorageMode.Memory)
        .Build()
        .StartAsync();

    await using var producer = new SurgewaveProducer<string, string>(opt =>
        opt.BootstrapServers = $"localhost:{runtime.Port}");

    // Act
    var result = await producer.ProduceAsync("test-topic", "key", "value");

    // Assert
    Assert.True(result.Offset >= 0);
    Assert.Equal("test-topic", result.Topic);
}
```

Key points: Use port 0 for auto-assign, access actual port via `runtime.Port`, use `Assert.True` for boolean conditions and `Assert.Equal` for value comparison.
</details>

## Score
- 7/7: Excellent! You're ready to write Surgewave unit tests
- 5-6/7: Good foundation for testing
- 3-4/7: Review the embedded broker test setup
- <3/7: Re-do the lesson and practice writing tests
