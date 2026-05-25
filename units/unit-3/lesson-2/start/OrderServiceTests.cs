// Surgewave Bootcamp - Unit 2, Lesson 2: Mocking & Isolation
//
// In this lesson, you'll learn to:
// - Mock IProducer and IConsumer interfaces
// - Test business logic without running a broker
// - Verify mock interactions
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Abstractions;
using Kuestenlogik.Surgewave.Client.Consumer;
using Moq;

namespace Lesson2_2;

// Domain model
public record Order(string Id, string CustomerId, decimal Amount);

// TODO: Implement OrderService that uses IProducer<string, Order>
// Hint: Accept producer in constructor, use it in SubmitOrderAsync
public class OrderService
{
    // TODO: Add private field for IProducer<string, Order>

    public OrderService(IProducer<string, Order> producer)
    {
        // TODO: Store the producer
        throw new NotImplementedException();
    }

    public async Task<long> SubmitOrderAsync(Order order, CancellationToken ct = default)
    {
        // TODO: Call producer.ProduceAsync with topic "orders", order.Id as key
        // TODO: Return the offset from the result
        throw new NotImplementedException();
    }
}

// TODO: Implement OrderProcessor that uses IConsumer<string, Order>
public class OrderProcessor
{
    // TODO: Add private field for IConsumer<string, Order>

    public OrderProcessor(IConsumer<string, Order> consumer)
    {
        // TODO: Store the consumer
        throw new NotImplementedException();
    }

    public async Task<Order?> GetNextOrderAsync(CancellationToken ct = default)
    {
        // TODO: Call consumer.ConsumeAsync and return result?.Value
        throw new NotImplementedException();
    }
}

public class OrderServiceTests
{
    // TODO: Complete this test - SubmitOrder produces to correct topic
    [Fact]
    public async Task SubmitOrder_ProducesToCorrectTopic()
    {
        // Arrange
        // Hint: Create Mock<IProducer<string, Order>>
        // Hint: Setup ProduceAsync to return ProduceResult with Offset = 42

        // Act
        // Hint: Create OrderService with mock.Object and call SubmitOrderAsync

        // Assert
        // Hint: Verify offset equals 42

        throw new NotImplementedException();
    }

    // TODO: Complete this test - SubmitOrder uses order ID as key
    [Fact]
    public async Task SubmitOrder_UsesOrderIdAsKey()
    {
        // Arrange
        // Hint: Create mock producer

        // Act
        // Hint: Submit an order with ID "ORD-789"

        // Assert
        // Hint: Use mockProducer.Verify() to check ProduceAsync was called with "ORD-789" as key

        throw new NotImplementedException();
    }

    // TODO: Complete this test - Exception handling
    [Fact]
    public async Task SubmitOrder_WhenProducerFails_ThrowsException()
    {
        // Arrange
        // Hint: Setup mock to ThrowsAsync(new ProduceException("Broker unavailable"))

        // Act & Assert
        // Hint: Use Assert.ThrowsAsync<ProduceException>()

        throw new NotImplementedException();
    }

    // TODO: Complete this test - Consumer mock
    [Fact]
    public async Task GetNextOrder_ReturnsConsumedOrder()
    {
        // Arrange
        // Hint: Create Mock<IConsumer<string, Order>>
        // Hint: Setup ConsumeAsync to return ConsumeResult with an Order

        // Act
        // Hint: Create OrderProcessor and call GetNextOrderAsync

        // Assert
        // Hint: Verify the returned order matches

        throw new NotImplementedException();
    }
}
