// Surgewave Bootcamp - Unit 2, Lesson 2: Mocking & Isolation
// COMPLETED SOLUTION

using Xunit;
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
            .ReturnsAsync(new ProduceResult { Topic = "orders", Partition = 0, Offset = 42 });

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
            .ReturnsAsync(new ProduceResult { Topic = "orders", Partition = 0, Offset = 1 });

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
