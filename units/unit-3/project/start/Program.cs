// Unit 2 Project: Surgewave Test Suite
// Build a comprehensive test suite for Surgewave Producer/Consumer

using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

// =============================================================================
// SHARED TEST FIXTURE
// =============================================================================
// TODO: Implement IAsyncLifetime to share a broker instance across all tests
// This fixture starts the broker once and shares it across multiple test classes

public class SurgewaveFixture // TODO: Add IAsyncLifetime interface
{
    public SurgewaveRuntime Runtime { get; private set; } = null!;
    public int Port => Runtime?.Port ?? 0;

    // TODO: Implement InitializeAsync
    // - Create a SurgewaveRuntime with Port 0 (auto-assign) and Memory storage
    // - Enable auto-create topics
    // - Start the runtime
    public async Task InitializeAsync()
    {
        // TODO: Create and start the runtime
        // Hint: await SurgewaveRuntime.CreateBuilder()
        //           .WithPort(0)
        //           .WithStorageMode(StorageMode.Memory)
        //           .WithAutoCreateTopics(true)
        //           .Build()
        //           .StartAsync();
        await Task.CompletedTask;
    }

    // TODO: Implement DisposeAsync to clean up runtime
    public async Task DisposeAsync()
    {
        // TODO: Dispose the runtime
        await Task.CompletedTask;
    }

    public SurgewaveProducer<string, string> CreateProducer()
    {
        // TODO: Create and return a producer connected to the runtime
        // Hint: new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = ...)
        throw new NotImplementedException();
    }

    public SurgewaveConsumer<string, string> CreateConsumer(string? groupId = null)
    {
        // TODO: Create and return a consumer connected to the runtime
        // Include AutoOffsetReset.Earliest for testing
        throw new NotImplementedException();
    }
}

// =============================================================================
// PRODUCER TESTS
// =============================================================================
// TODO: Add [Collection("Surgewave")] attribute to use the shared fixture

public class ProducerTests
{
    private readonly SurgewaveFixture _fixture;

    public ProducerTests(SurgewaveFixture fixture)
    {
        _fixture = fixture;
    }

    // TODO: Add [Fact] attribute
    public async Task Producer_SendsMessage_ReturnsValidOffset()
    {
        // Arrange
        // TODO: Create a producer using _fixture.CreateProducer()

        // Act
        // TODO: Send a message to "test-producer" topic

        // Assert
        // TODO: Verify result has valid topic, partition, and offset >= 0

        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    // TODO: Add [Fact] attribute
    public async Task Producer_SendsWithKey_PartitionsConsistently()
    {
        // Arrange
        // TODO: Create a producer and topic with multiple partitions

        // Act
        // TODO: Send 10 messages with the same key

        // Assert
        // TODO: All messages should go to the same partition

        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    // TODO: Add [Fact] attribute
    public async Task Producer_BatchSend_AllMessagesDelivered()
    {
        // Arrange
        // TODO: Create producer

        // Act
        // TODO: Send 100 messages in a batch, then flush

        // Assert
        // TODO: Verify all messages have sequential offsets

        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    // TODO: Add [Fact] attribute
    public async Task Producer_FlushAsync_CompletesAllPending()
    {
        // Arrange
        // TODO: Create producer

        // Act
        // TODO: Send messages without awaiting, then flush

        // Assert
        // TODO: After flush, all messages should be persisted

        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}

// =============================================================================
// CONSUMER TESTS
// =============================================================================

public class ConsumerTests
{
    private readonly SurgewaveFixture _fixture;

    public ConsumerTests(SurgewaveFixture fixture)
    {
        _fixture = fixture;
    }

    // TODO: Add [Fact] attribute
    public async Task Consumer_Subscribe_ReceivesMessages()
    {
        // Arrange
        // TODO: Create producer and consumer
        // Send test messages to a topic

        // Act
        // TODO: Subscribe consumer and consume messages

        // Assert
        // TODO: Verify messages received match what was sent

        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    // TODO: Add [Fact] attribute
    public async Task Consumer_MultipleSubscriptions_ReceivesFromAll()
    {
        // Arrange
        // TODO: Create producer and consumer
        // Send messages to multiple topics

        // Act
        // TODO: Subscribe to multiple topics and consume

        // Assert
        // TODO: Verify messages from all topics received

        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    // TODO: Add [Fact] attribute
    public async Task Consumer_CommitOffset_ResumeFromCorrectPosition()
    {
        // Arrange
        // TODO: Create producer and send 10 messages

        // Act
        // TODO: Consume 5 messages, commit, dispose consumer
        // Create new consumer with same group, consume remaining

        // Assert
        // TODO: New consumer starts from offset 5

        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}

// =============================================================================
// INTEGRATION TESTS
// =============================================================================

public class IntegrationTests
{
    private readonly SurgewaveFixture _fixture;

    public IntegrationTests(SurgewaveFixture fixture)
    {
        _fixture = fixture;
    }

    // TODO: Add [Fact] attribute
    public async Task ProducerConsumer_RoundTrip_MessageIntact()
    {
        // Arrange
        var testData = new TestMessage("ID-123", "Test payload", DateTime.UtcNow);
        // TODO: Create producer and consumer

        // Act
        // TODO: Serialize and send message, then consume and deserialize

        // Assert
        // TODO: Verify received message equals original

        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    // TODO: Add [Fact] attribute
    public async Task ProducerConsumer_HighVolume_NoMessageLoss()
    {
        // Arrange
        const int messageCount = 1000;
        // TODO: Create producer and consumer

        // Act
        // TODO: Send 1000 messages, consume all

        // Assert
        // TODO: Verify all 1000 messages received

        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    // TODO: Add [Fact] attribute
    public async Task ProducerConsumer_MultiPartition_OrderPerPartition()
    {
        // Arrange
        // TODO: Create topic with 3 partitions

        // Act
        // TODO: Send messages to different partitions
        // Consume and track order per partition

        // Assert
        // TODO: Within each partition, messages are in order

        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}

// =============================================================================
// EDGE CASE TESTS
// =============================================================================

public class EdgeCaseTests
{
    private readonly SurgewaveFixture _fixture;

    public EdgeCaseTests(SurgewaveFixture fixture)
    {
        _fixture = fixture;
    }

    // TODO: Add [Fact] attribute
    public async Task Consumer_Timeout_ReturnsNull()
    {
        // Arrange
        // TODO: Create consumer, subscribe to empty topic

        // Act
        // TODO: Try to consume with short timeout

        // Assert
        // TODO: Verify null returned (no exception)

        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    // TODO: Add [Fact] attribute
    public async Task Consumer_EmptyTopic_NoMessages()
    {
        // Arrange
        // TODO: Create consumer for fresh topic

        // Act
        // TODO: Try to consume

        // Assert
        // TODO: Verify no messages returned

        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}

// =============================================================================
// TEST DATA MODELS
// =============================================================================

public record TestMessage(string Id, string Payload, DateTime Timestamp);

// =============================================================================
// SIMPLE TEST RUNNER (for demonstration)
// =============================================================================
// Note: In real usage, run with: dotnet test

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Test Suite ===");
        Console.WriteLine();
        Console.WriteLine("Run tests with: dotnet test --verbosity normal");
        Console.WriteLine();

        // Quick sanity check
        Console.WriteLine("Performing sanity check...");

        try
        {
            await using var runtime = await SurgewaveRuntime.CreateBuilder()
                .WithPort(0)
                .WithStorageMode(StorageMode.Memory)
                .WithAutoCreateTopics(true)
                .Build()
                .StartAsync();

            Console.WriteLine($"[OK] Broker started on port {runtime.Port}");

            await using var producer = new SurgewaveProducer<string, string>(opt =>
                opt.BootstrapServers = $"localhost:{runtime.Port}");

            var result = await producer.ProduceAsync("sanity-check", "key", "value");
            Console.WriteLine($"[OK] Message sent to partition {result.Partition}, offset {result.Offset}");

            await using var consumer = new SurgewaveConsumer<string, string>(opt =>
            {
                opt.BootstrapServers = $"localhost:{runtime.Port}";
                opt.AutoOffsetReset = AutoOffsetReset.Earliest;
            });

            consumer.Subscribe("sanity-check");
            var msg = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));

            if (msg != null)
                Console.WriteLine($"[OK] Message received: {msg.Value}");
            else
                Console.WriteLine("[FAIL] No message received");

            Console.WriteLine();
            Console.WriteLine("Sanity check passed! Now implement the test cases.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FAIL] Sanity check failed: {ex.Message}");
        }
    }
}
