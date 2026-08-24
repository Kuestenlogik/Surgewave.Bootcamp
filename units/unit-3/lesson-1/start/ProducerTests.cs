// Surgewave Bootcamp - Unit 2, Lesson 1: Unit Testing Basics
//
// In this lesson, you'll learn to:
// - Set up unit tests for Surgewave using xUnit
// - Test producers and consumers with embedded broker
// - Write async tests with proper resource cleanup
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

// TODO: Add the necessary using statements
// Hint: You need Kuestenlogik.Surgewave.Broker, Kuestenlogik.Surgewave.Runtime, Kuestenlogik.Surgewave.Client, Kuestenlogik.Surgewave.Client.Consumer

namespace Lesson2_1;

public class ProducerTests
{
    // TODO: Implement helper method to create embedded runtime
    // Hint: Use SurgewaveRuntime.CreateBuilder() with WithPort(0), WithStorageEngine(StorageEngines.Memory)
    private static async Task<ISurgewaveRuntime> CreateRuntimeAsync()
    {
        // Your code here...
        throw new NotImplementedException();
    }

    // TODO: Implement helper method to create a producer
    // Hint: Use new SurgewaveProducer<string, string>(opt => { opt.BootstrapServers = ... })
    private static SurgewaveProducer<string, string> CreateProducer(int port)
    {
        // Your code here...
        throw new NotImplementedException();
    }

    // TODO: Implement helper method to create a consumer
    // Hint: Use new SurgewaveConsumer<string, string>(opt => { ... })
    // Don't forget AutoOffsetReset = AutoOffsetReset.Earliest
    private static SurgewaveConsumer<string, string> CreateConsumer(int port)
    {
        // Your code here...
        throw new NotImplementedException();
    }

    // TODO: Complete this test - Producer sends message and returns valid offset
    [Fact]
    public async Task Producer_SendsMessage_ReturnsValidOffset()
    {
        // Arrange
        // Hint: Create runtime and producer using helper methods

        // Act
        // Hint: Call producer.ProduceAsync("test-topic", "test-key", "test-value")

        // Assert
        // Hint: Verify result.Topic, result.Offset, result.Partition

        throw new NotImplementedException();
    }

    // TODO: Complete this test - Consumer receives message with correct content
    [Fact]
    public async Task Consumer_ReceivesMessage_WithCorrectContent()
    {
        // Arrange
        // Hint: Create runtime, producer, and consumer

        // Produce a message
        // Hint: await producer.ProduceAsync("greetings", "user-1", "Hello, Surgewave!")

        // Act - Subscribe and consume
        // Hint: consumer.Subscribe("greetings")
        // Hint: await consumer.ConsumeAsync(TimeSpan.FromSeconds(5))

        // Assert
        // Hint: Verify result is not null, key, value, and offset

        throw new NotImplementedException();
    }

    // TODO: Complete this test - Multiple messages are all received
    [Fact]
    public async Task ProducerConsumer_MultipleMessages_AllReceived()
    {
        // Arrange
        // Hint: Create runtime, producer, and consumer

        // Produce 5 messages
        // Hint: Loop and produce with keys "key-0" through "key-4"

        // Act - Consume all messages
        // Hint: Subscribe and consume 5 times, collecting values

        // Assert
        // Hint: Verify 5 messages received, check for "value-0" and "value-4"

        throw new NotImplementedException();
    }
}
