// Surgewave Bootcamp - Unit 2, Lesson 3: Integration Testing
//
// In this lesson, you'll learn to:
// - Create test fixtures for shared broker instances
// - Write integration tests with isolated topics
// - Test end-to-end message flow
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Runtime.CompilerServices;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

namespace Lesson2_3;

// TODO: Implement SurgewaveFixture that manages broker lifecycle
// Hint: Implement IAsyncLifetime with InitializeAsync and DisposeAsync
public class SurgewaveFixture : IAsyncLifetime
{
    public ISurgewaveRuntime Runtime { get; private set; } = null!;
    public int Port => Runtime.Port;

    public async ValueTask InitializeAsync()
    {
        // TODO: Start the embedded broker
        throw new NotImplementedException();
    }

    public async ValueTask DisposeAsync()
    {
        // TODO: Dispose the runtime
        throw new NotImplementedException();
    }
}

// TODO: Implement test class using IClassFixture<SurgewaveFixture>
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

    // TODO: Implement helper to create producer
    private SurgewaveProducer<string, string> CreateProducer()
    {
        throw new NotImplementedException();
    }

    // TODO: Implement helper to create consumer
    private SurgewaveConsumer<string, string> CreateConsumer()
    {
        throw new NotImplementedException();
    }

    // TODO: Complete this test - End-to-end produce and consume
    [Fact]
    public async Task EndToEnd_ProduceAndConsume_MessageReceived()
    {
        // Arrange
        // Hint: Create unique topic, producer, and consumer

        // Act - Produce then Consume

        // Assert
        // Hint: Verify message was received correctly

        throw new NotImplementedException();
    }

    // TODO: Complete this test - Multiple messages
    [Fact]
    public async Task EndToEnd_MultipleMessages_AllReceived()
    {
        // Hint: Produce 3 messages, consume all 3, verify order

        throw new NotImplementedException();
    }

    // TODO: Complete this test - Same key preserves order
    [Fact]
    public async Task EndToEnd_SameKey_PreservesOrder()
    {
        // Hint: Produce 5 messages with same key, verify order preserved

        throw new NotImplementedException();
    }

    // TODO: Complete this test - Two consumers each get all messages
    [Fact]
    public async Task EndToEnd_TwoConsumers_EachGetsAllMessages()
    {
        // Hint: Two consumers without group ID each get all messages

        throw new NotImplementedException();
    }
}
