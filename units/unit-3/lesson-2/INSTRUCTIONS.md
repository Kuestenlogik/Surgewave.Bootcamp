# Lesson 2.2: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Understand Dependency Injection for Testing

To make code testable, inject dependencies rather than creating them directly:

**Hard to test (tightly coupled):**
```csharp
public class OrderService
{
    private readonly SurgewaveProducer<string, Order> _producer;

    public OrderService()
    {
        _producer = new SurgewaveProducer<string, Order>(...); // Can't mock!
    }
}
```

**Easy to test (loosely coupled):**
```csharp
public class OrderService
{
    private readonly IProducer<string, Order> _producer;

    public OrderService(IProducer<string, Order> producer)
    {
        _producer = producer; // Can inject mock!
    }
}
```

---

## Step 2: Add Moq Package

The test project includes Moq:

```xml
<ItemGroup>
  <PackageReference Include="Moq" Version="4.*" />
</ItemGroup>
```

---

## Step 3: Create a Testable Service

First, create a simple service that uses the producer:

```csharp
public record Order(string Id, string CustomerId, decimal Amount);

public class OrderService
{
    private readonly IProducer<string, Order> _producer;

    public OrderService(IProducer<string, Order> producer)
    {
        _producer = producer;
    }

    public async Task<long> SubmitOrderAsync(Order order, CancellationToken ct = default)
    {
        var result = await _producer.ProduceAsync("orders", order.Id, order, ct);
        return result.Offset;
    }
}
```

---

## Step 4: Mock the Producer

Set up a mock producer that returns successful results:

```csharp
[Fact]
public async Task SubmitOrder_ProducesToCorrectTopic()
{
    // Arrange
    var mockProducer = new Mock<IProducer<string, Order>>();
    mockProducer
        .Setup(p => p.ProduceAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Order>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new ProduceResult { Topic = "orders", Offset = 42 });

    var service = new OrderService(mockProducer.Object);
    var order = new Order("ORD-123", "CUST-456", 99.99m);

    // Act
    var offset = await service.SubmitOrderAsync(order);

    // Assert
    Assert.Equal(42, offset);
}
```

---

## Step 5: Verify Interactions

Check that the producer was called correctly:

```csharp
[Fact]
public async Task SubmitOrder_UsesOrderIdAsKey()
{
    // Arrange
    var mockProducer = new Mock<IProducer<string, Order>>();
    mockProducer
        .Setup(p => p.ProduceAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Order>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new ProduceResult { Offset = 1 });

    var service = new OrderService(mockProducer.Object);
    var order = new Order("ORD-789", "CUST-111", 50.00m);

    // Act
    await service.SubmitOrderAsync(order);

    // Assert - Verify the key used was the order ID
    mockProducer.Verify(
        p => p.ProduceAsync("orders", "ORD-789", It.IsAny<Order>(), default),
        Times.Once);
}
```

---

## Step 6: Test Error Scenarios

Mock exceptions to test error handling:

```csharp
[Fact]
public async Task SubmitOrder_WhenProducerFails_ThrowsException()
{
    // Arrange
    var mockProducer = new Mock<IProducer<string, Order>>();
    mockProducer
        .Setup(p => p.ProduceAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Order>(),
            It.IsAny<CancellationToken>()))
        .ThrowsAsync(new ProduceException("Broker unavailable"));

    var service = new OrderService(mockProducer.Object);

    // Act & Assert
    await Assert.ThrowsAsync<ProduceException>(
        () => service.SubmitOrderAsync(new Order("1", "C1", 10)));
}
```

---

## Step 7: Mock Consumer for Message Processing

```csharp
public class OrderProcessor
{
    private readonly IConsumer<string, Order> _consumer;

    public OrderProcessor(IConsumer<string, Order> consumer)
    {
        _consumer = consumer;
    }

    public async Task<Order?> GetNextOrderAsync(CancellationToken ct = default)
    {
        var result = await _consumer.ConsumeAsync(ct);
        return result?.Value;
    }
}

[Fact]
public async Task GetNextOrder_ReturnsConsumedOrder()
{
    // Arrange
    var expectedOrder = new Order("ORD-1", "CUST-1", 100);
    var mockConsumer = new Mock<IConsumer<string, Order>>();
    mockConsumer
        .Setup(c => c.ConsumeAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(new ConsumeResult<string, Order>
        {
            Topic = "orders",
            Partition = 0,
            Offset = 0,
            Key = "ORD-1",
            Value = expectedOrder
        });

    var processor = new OrderProcessor(mockConsumer.Object);

    // Act
    var order = await processor.GetNextOrderAsync();

    // Assert
    Assert.NotNull(order);
    Assert.Equal("ORD-1", order.Id);
    Assert.Equal(100, order.Amount);
}
```

---

## Complete Solution

Your completed test file should look like this:

```csharp
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Abstractions;
using Kuestenlogik.Surgewave.Client.Consumer;
using Moq;

namespace Lesson2_2;

public record Order(string Id, string CustomerId, decimal Amount);

public class OrderService
{
    private readonly IProducer<string, Order> _producer;

    public OrderService(IProducer<string, Order> producer)
    {
        _producer = producer;
    }

    public async Task<long> SubmitOrderAsync(Order order, CancellationToken ct = default)
    {
        var result = await _producer.ProduceAsync("orders", order.Id, order, ct);
        return result.Offset;
    }
}

public class OrderProcessor
{
    private readonly IConsumer<string, Order> _consumer;

    public OrderProcessor(IConsumer<string, Order> consumer)
    {
        _consumer = consumer;
    }

    public async Task<Order?> GetNextOrderAsync(CancellationToken ct = default)
    {
        var result = await _consumer.ConsumeAsync(ct);
        return result?.Value;
    }
}

public class OrderServiceTests
{
    [Fact]
    public async Task SubmitOrder_ProducesToCorrectTopic()
    {
        // Arrange
        var mockProducer = new Mock<IProducer<string, Order>>();
        mockProducer
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProduceResult { Topic = "orders", Offset = 42 });

        var service = new OrderService(mockProducer.Object);
        var order = new Order("ORD-123", "CUST-456", 99.99m);

        // Act
        var offset = await service.SubmitOrderAsync(order);

        // Assert
        Assert.Equal(42, offset);
    }

    [Fact]
    public async Task SubmitOrder_UsesOrderIdAsKey()
    {
        // Arrange
        var mockProducer = new Mock<IProducer<string, Order>>();
        mockProducer
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProduceResult { Offset = 1 });

        var service = new OrderService(mockProducer.Object);
        var order = new Order("ORD-789", "CUST-111", 50.00m);

        // Act
        await service.SubmitOrderAsync(order);

        // Assert
        mockProducer.Verify(
            p => p.ProduceAsync("orders", "ORD-789", It.IsAny<Order>(), default),
            Times.Once);
    }

    [Fact]
    public async Task SubmitOrder_WhenProducerFails_ThrowsException()
    {
        // Arrange
        var mockProducer = new Mock<IProducer<string, Order>>();
        mockProducer
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProduceException("Broker unavailable"));

        var service = new OrderService(mockProducer.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ProduceException>(
            () => service.SubmitOrderAsync(new Order("1", "C1", 10)));
    }

    [Fact]
    public async Task GetNextOrder_ReturnsConsumedOrder()
    {
        // Arrange
        var expectedOrder = new Order("ORD-1", "CUST-1", 100);
        var mockConsumer = new Mock<IConsumer<string, Order>>();
        mockConsumer
            .Setup(c => c.ConsumeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConsumeResult<string, Order>
            {
                Topic = "orders",
                Partition = 0,
                Offset = 0,
                Key = "ORD-1",
                Value = expectedOrder
            });

        var processor = new OrderProcessor(mockConsumer.Object);

        // Act
        var order = await processor.GetNextOrderAsync();

        // Assert
        Assert.NotNull(order);
        Assert.Equal("ORD-1", order.Id);
        Assert.Equal(100, order.Amount);
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
Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4
```

---

## Knowledge Check

Before moving on, make sure you can answer:

1. When would you choose mocking over an embedded broker?
2. Why is it important to verify mock interactions?
3. How do you test exception handling with mocks?
4. What makes code "mockable"?

---

**Ready for more?** → [Lesson 2.3: Integration Testing](../lesson-3/README.md)
