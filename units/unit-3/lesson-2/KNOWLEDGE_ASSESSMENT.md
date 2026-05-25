# Knowledge Assessment: Lesson 2.2 - Mocking & Isolation

## Multiple Choice

### 1. When should you prefer mocking over the embedded broker?
- [ ] A) When testing message serialization
- [ ] B) When testing business logic only, without needing broker behavior
- [ ] C) When testing consumer groups
- [ ] D) When testing end-to-end flows

<details>
<summary>Answer</summary>
**B)** When testing business logic only, without needing broker behavior. Mocking is ideal for isolating your code from Surgewave, allowing fast tests that focus on your application logic.
</details>

### 2. Which mocking framework is commonly used with Surgewave in .NET?
- [ ] A) JMock
- [ ] B) Mockito
- [ ] C) Moq
- [ ] D) EasyMock

<details>
<summary>Answer</summary>
**C)** Moq. Moq is a popular .NET mocking framework that works well with xUnit for creating mock implementations of Surgewave interfaces.
</details>

### 3. What does `It.IsAny<T>()` mean in a Moq setup?
- [ ] A) The parameter must be exactly type T
- [ ] B) Match any value of type T
- [ ] C) The parameter is optional
- [ ] D) The parameter must be null

<details>
<summary>Answer</summary>
**B)** Match any value of type T. `It.IsAny<T>()` is a matcher that accepts any argument of the specified type when setting up or verifying mock calls.
</details>

### 4. What does `Times.Once` verify in a mock assertion?
- [ ] A) The method was called at least once
- [ ] B) The method was called exactly one time
- [ ] C) The method was called at most once
- [ ] D) The method takes one parameter

<details>
<summary>Answer</summary>
**B)** The method was called exactly one time. `Times.Once` is a verification constraint that ensures the mocked method was invoked exactly once.
</details>

## True/False

### 5. You should use the embedded broker when testing actual message serialization behavior.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Testing serialization requires actual Surgewave behavior, which mocks don't provide. The embedded broker ensures your serialization configuration works correctly.
</details>

### 6. Mocked tests run slower than embedded broker tests because mocking has more overhead.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Mocked tests are typically faster because they don't start a broker at all. There's no network or I/O involved - just in-memory mock objects.
</details>

## Code Challenge

### 7. Complete the mock setup and verification for this order service test:

```csharp
[Fact]
public async Task OrderService_ProcessOrder_PublishesToCorrectTopic()
{
    // Arrange
    var mockProducer = new Mock<IProducer<string, Order>>();
    mockProducer
        .Setup(p => p.ProduceAsync(
            ____________,      // Any topic string
            ____________,      // Any key string
            ____________,      // Any Order object
            ____________))     // Any CancellationToken
        .ReturnsAsync(new ProduceResult { Topic = "orders", Offset = 1 });

    var service = new OrderService(mockProducer.Object);
    var order = new Order { Id = "ORD-123", Total = 99.99m };

    // Act
    await service.ProcessAsync(order);

    // Assert
    mockProducer.Verify(
        p => p.ProduceAsync("orders", "ORD-123", ____________, default),
        ____________);  // Verify called exactly once
}
```

<details>
<summary>Answer</summary>
```csharp
[Fact]
public async Task OrderService_ProcessOrder_PublishesToCorrectTopic()
{
    // Arrange
    var mockProducer = new Mock<IProducer<string, Order>>();
    mockProducer
        .Setup(p => p.ProduceAsync(
            It.IsAny<string>(),           // Any topic string
            It.IsAny<string>(),           // Any key string
            It.IsAny<Order>(),            // Any Order object
            It.IsAny<CancellationToken>())) // Any CancellationToken
        .ReturnsAsync(new ProduceResult { Topic = "orders", Offset = 1 });

    var service = new OrderService(mockProducer.Object);
    var order = new Order { Id = "ORD-123", Total = 99.99m };

    // Act
    await service.ProcessAsync(order);

    // Assert
    mockProducer.Verify(
        p => p.ProduceAsync("orders", "ORD-123", It.IsAny<Order>(), default),
        Times.Once);  // Verify called exactly once
}
```

The setup uses `It.IsAny<T>()` matchers for flexibility, while the verification uses specific values for topic and key to ensure correctness, with `Times.Once` to verify it was called exactly once.
</details>

## Score
- 7/7: Excellent! You understand mocking patterns for Surgewave
- 5-6/7: Good grasp of isolation testing
- 3-4/7: Review Moq setup and verification syntax
- <3/7: Re-do the lesson focusing on mock examples
