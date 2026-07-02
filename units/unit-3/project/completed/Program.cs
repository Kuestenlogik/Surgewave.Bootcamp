// Unit 2 Project: Surgewave Test Suite - COMPLETED
// A comprehensive test suite for Surgewave Producer/Consumer

using Xunit;
using System.Collections.Concurrent;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Client.Abstractions;
using Kuestenlogik.Surgewave.Broker;

// =============================================================================
// SHARED TEST FIXTURE
// =============================================================================

[CollectionDefinition("Surgewave")]
public class SurgewaveCollection : ICollectionFixture<SurgewaveFixture> { }

public class SurgewaveFixture : IAsyncLifetime
{
    public SurgewaveRuntime Runtime { get; private set; } = null!;
    public int Port => Runtime.Port;

    public async Task InitializeAsync()
    {
        Runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine($"[Fixture] Surgewave broker started on port {Port}");
    }

    public async Task DisposeAsync()
    {
        if (Runtime != null)
        {
            await Runtime.DisposeAsync();
            Console.WriteLine("[Fixture] Surgewave broker stopped");
        }
    }

    public SurgewaveProducer<string, string> CreateProducer()
    {
        return new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{Port}";
        });
    }

    public SurgewaveConsumer<string, string> CreateConsumer(string? groupId = null)
    {
        return new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{Port}";
            opt.GroupId = groupId ?? Guid.NewGuid().ToString();
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
            opt.EnableAutoCommit = false;
        });
    }
}

// =============================================================================
// PRODUCER TESTS
// =============================================================================

[Collection("Surgewave")]
public class ProducerTests
{
    private readonly SurgewaveFixture _fixture;

    public ProducerTests(SurgewaveFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Producer_SendsMessage_ReturnsValidOffset()
    {
        // Arrange
        await using var producer = _fixture.CreateProducer();
        var topic = $"test-producer-{Guid.NewGuid():N}";

        // Act
        var result = await producer.ProduceAsync(topic, "test-key", "test-value");

        // Assert
        Assert.Equal(topic, result.Topic);
        Assert.True(result.Partition >= 0, "Partition should be non-negative");
        Assert.Equal(0, result.Offset); // First message should have offset 0
    }

    [Fact]
    public async Task Producer_SendsWithKey_PartitionsConsistently()
    {
        // Arrange
        await using var producer = _fixture.CreateProducer();
        var topic = $"test-partition-{Guid.NewGuid():N}";

        var key = "consistent-key";
        var partitions = new List<int>();

        // Act - Send 10 messages with the same key
        for (int i = 0; i < 10; i++)
        {
            var result = await producer.ProduceAsync(topic, key, $"value-{i}");
            partitions.Add(result.Partition);
        }

        // Assert - All messages should go to the same partition
        Assert.True(partitions.All(p => p == partitions[0]),
            "All messages with same key should go to same partition");
    }

    [Fact]
    public async Task Producer_BatchSend_AllMessagesDelivered()
    {
        // Arrange
        await using var producer = _fixture.CreateProducer();
        var topic = $"test-batch-{Guid.NewGuid():N}";
        const int messageCount = 100;
        var results = new List<ProduceResult>();

        // Act
        for (int i = 0; i < messageCount; i++)
        {
            var result = await producer.ProduceAsync(topic, $"key-{i}", $"value-{i}");
            results.Add(result);
        }
        await producer.FlushAsync();

        // Assert
        Assert.Equal(messageCount, results.Count);

        // Group by partition and verify sequential offsets within each partition
        var byPartition = results.GroupBy(r => r.Partition);
        foreach (var group in byPartition)
        {
            var offsets = group.Select(r => r.Offset).OrderBy(o => o).ToList();
            for (int i = 1; i < offsets.Count; i++)
            {
                Assert.Equal(offsets[i - 1] + 1, offsets[i]);
            }
        }
    }

    [Fact]
    public async Task Producer_FlushAsync_CompletesAllPending()
    {
        // Arrange
        await using var producer = _fixture.CreateProducer();
        await using var consumer = _fixture.CreateConsumer();
        var topic = $"test-flush-{Guid.NewGuid():N}";
        const int messageCount = 50;

        // Act - Send without awaiting individual results
        var tasks = new List<Task<ProduceResult>>();
        for (int i = 0; i < messageCount; i++)
        {
            tasks.Add(producer.ProduceAsync(topic, $"key-{i}", $"value-{i}"));
        }

        // Flush ensures all pending messages are sent
        await producer.FlushAsync();
        var results = await Task.WhenAll(tasks);

        // Assert - Verify by consuming
        consumer.Subscribe(topic);
        var consumed = 0;

        while (consumed < messageCount)
        {
            var msg = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
            if (msg == null) break;
            consumed++;
        }

        Assert.Equal(messageCount, consumed);
    }
}

// =============================================================================
// CONSUMER TESTS
// =============================================================================

[Collection("Surgewave")]
public class ConsumerTests
{
    private readonly SurgewaveFixture _fixture;

    public ConsumerTests(SurgewaveFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Consumer_Subscribe_ReceivesMessages()
    {
        // Arrange
        await using var producer = _fixture.CreateProducer();
        await using var consumer = _fixture.CreateConsumer();
        var topic = $"test-subscribe-{Guid.NewGuid():N}";
        var sentMessages = new[] { "message-1", "message-2", "message-3" };

        foreach (var msg in sentMessages)
        {
            await producer.ProduceAsync(topic, "key", msg);
        }

        // Act
        consumer.Subscribe(topic);
        var receivedMessages = new List<string>();

        for (int i = 0; i < sentMessages.Length; i++)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
            if (result != null)
                receivedMessages.Add(result.Value);
        }

        // Assert
        Assert.Equal(sentMessages.Length, receivedMessages.Count);
        Assert.All(sentMessages, msg => Assert.Contains(msg, receivedMessages));
    }

    [Fact]
    public async Task Consumer_MultipleSubscriptions_ReceivesFromAll()
    {
        // Arrange
        await using var producer = _fixture.CreateProducer();
        await using var consumer = _fixture.CreateConsumer();
        var topic1 = $"test-multi-1-{Guid.NewGuid():N}";
        var topic2 = $"test-multi-2-{Guid.NewGuid():N}";

        await producer.ProduceAsync(topic1, "key", "from-topic-1");
        await producer.ProduceAsync(topic2, "key", "from-topic-2");

        // Act
        consumer.Subscribe(topic1, topic2);
        var receivedTopics = new HashSet<string>();

        for (int i = 0; i < 2; i++)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
            if (result != null)
                receivedTopics.Add(result.Topic);
        }

        // Assert
        Assert.Contains(topic1, receivedTopics);
        Assert.Contains(topic2, receivedTopics);
    }

    [Fact]
    public async Task Consumer_CommitOffset_ResumeFromCorrectPosition()
    {
        // Arrange
        await using var producer = _fixture.CreateProducer();
        var topic = $"test-commit-{Guid.NewGuid():N}";
        var groupId = $"test-group-{Guid.NewGuid():N}";

        // Send 10 messages
        for (int i = 0; i < 10; i++)
        {
            await producer.ProduceAsync(topic, "key", $"message-{i}");
        }

        // Act - First consumer: consume 5, commit, close
        var firstBatch = new List<string>();
        {
            await using var consumer1 = _fixture.CreateConsumer(groupId);
            await consumer1.SubscribeAsync(default, topic);

            for (int i = 0; i < 5; i++)
            {
                var msg = await consumer1.ConsumeAsync(TimeSpan.FromSeconds(5));
                if (msg != null)
                {
                    firstBatch.Add(msg.Value);
                }
            }
            await consumer1.CommitAsync();
        }

        // Act - Second consumer: should start from offset 5
        var secondBatch = new List<string>();
        {
            await using var consumer2 = _fixture.CreateConsumer(groupId);
            await consumer2.SubscribeAsync(default, topic);

            for (int i = 0; i < 5; i++)
            {
                var msg = await consumer2.ConsumeAsync(TimeSpan.FromSeconds(5));
                if (msg != null)
                    secondBatch.Add(msg.Value);
            }
        }

        // Assert
        Assert.Equal(5, firstBatch.Count);
        Assert.Equal(5, secondBatch.Count);
        Assert.Contains("message-0", firstBatch);
        Assert.Contains("message-4", firstBatch);
        Assert.Contains("message-5", secondBatch);
        Assert.Contains("message-9", secondBatch);
    }
}

// =============================================================================
// INTEGRATION TESTS
// =============================================================================

[Collection("Surgewave")]
public class IntegrationTests
{
    private readonly SurgewaveFixture _fixture;

    public IntegrationTests(SurgewaveFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ProducerConsumer_RoundTrip_MessageIntact()
    {
        // Arrange
        await using var producer = _fixture.CreateProducer();
        await using var consumer = _fixture.CreateConsumer();
        var topic = $"test-roundtrip-{Guid.NewGuid():N}";

        var original = new TestMessage("ID-123", "Test payload with special chars: <>&\"'", DateTime.UtcNow);
        var json = JsonSerializer.Serialize(original);

        // Act
        await producer.ProduceAsync(topic, original.Id, json);

        consumer.Subscribe(topic);
        var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(result);
        var received = JsonSerializer.Deserialize<TestMessage>(result.Value);
        Assert.NotNull(received);
        Assert.Equal(original.Id, received.Id);
        Assert.Equal(original.Payload, received.Payload);
        Assert.Equal(original.Timestamp.ToString("O"), received.Timestamp.ToString("O"));
    }

    [Fact]
    public async Task ProducerConsumer_HighVolume_NoMessageLoss()
    {
        // Arrange
        await using var producer = _fixture.CreateProducer();
        await using var consumer = _fixture.CreateConsumer();
        var topic = $"test-volume-{Guid.NewGuid():N}";
        const int messageCount = 1000;
        var sentIds = new HashSet<int>();
        var receivedIds = new ConcurrentBag<int>();

        // Act - Send 1000 messages
        for (int i = 0; i < messageCount; i++)
        {
            await producer.ProduceAsync(topic, $"key-{i % 10}", i.ToString());
            sentIds.Add(i);
        }
        await producer.FlushAsync();

        // Consume all messages
        consumer.Subscribe(topic);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        while (receivedIds.Count < messageCount && !cts.Token.IsCancellationRequested)
        {
            var msg = await consumer.ConsumeAsync(TimeSpan.FromSeconds(1));
            if (msg != null && int.TryParse(msg.Value, out var id))
            {
                receivedIds.Add(id);
            }
        }

        // Assert
        Assert.Equal(messageCount, receivedIds.Count);
        var receivedSet = new HashSet<int>(receivedIds);
        Assert.True(sentIds.SetEquals(receivedSet), "All sent message IDs should be received");
    }

    [Fact]
    public async Task ProducerConsumer_MultiPartition_OrderPerPartition()
    {
        // Arrange
        await using var producer = _fixture.CreateProducer();
        await using var consumer = _fixture.CreateConsumer();
        var topic = $"test-order-{Guid.NewGuid():N}";

        // Send messages with keys that hash to different partitions
        var keys = new[] { "key-a", "key-b", "key-c" };
        var partitionMessages = new Dictionary<int, List<int>>();

        for (int i = 0; i < 30; i++)
        {
            var key = keys[i % 3];
            var result = await producer.ProduceAsync(topic, key, i.ToString());

            if (!partitionMessages.ContainsKey(result.Partition))
                partitionMessages[result.Partition] = new List<int>();
            partitionMessages[result.Partition].Add(i);
        }

        // Consume and track order
        consumer.Subscribe(topic);
        var receivedByPartition = new Dictionary<int, List<int>>();

        for (int i = 0; i < 30; i++)
        {
            var msg = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
            if (msg != null && int.TryParse(msg.Value, out var val))
            {
                if (!receivedByPartition.ContainsKey(msg.Partition))
                    receivedByPartition[msg.Partition] = new List<int>();
                receivedByPartition[msg.Partition].Add(val);
            }
        }

        // Assert - Order within each partition is preserved
        foreach (var (partition, messages) in receivedByPartition)
        {
            for (int i = 1; i < messages.Count; i++)
            {
                var expected = partitionMessages[partition];
                var expectedIndex1 = expected.IndexOf(messages[i - 1]);
                var expectedIndex2 = expected.IndexOf(messages[i]);
                Assert.True(expectedIndex1 < expectedIndex2,
                    $"Messages in partition {partition} should maintain relative order");
            }
        }
    }
}

// =============================================================================
// EDGE CASE TESTS
// =============================================================================

[Collection("Surgewave")]
public class EdgeCaseTests
{
    private readonly SurgewaveFixture _fixture;

    public EdgeCaseTests(SurgewaveFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Consumer_Timeout_ReturnsNull()
    {
        // Arrange
        await using var consumer = _fixture.CreateConsumer();
        var topic = $"test-timeout-{Guid.NewGuid():N}";

        // Act
        consumer.Subscribe(topic);
        var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Consumer_EmptyTopic_NoMessages()
    {
        // Arrange
        await using var consumer = _fixture.CreateConsumer();
        var topic = $"test-empty-{Guid.NewGuid():N}";

        // Act
        consumer.Subscribe(topic);
        var messages = new List<ConsumeResult<string, string>>();

        for (int i = 0; i < 3; i++)
        {
            var msg = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
            if (msg != null) messages.Add(msg);
        }

        // Assert
        Assert.Empty(messages);
    }

    [Fact]
    public async Task Producer_LargeMessage_DeliveredSuccessfully()
    {
        // Arrange
        await using var producer = _fixture.CreateProducer();
        await using var consumer = _fixture.CreateConsumer();
        var topic = $"test-large-{Guid.NewGuid():N}";

        // Create a ~1MB message
        var largePayload = new string('X', 1024 * 1024);

        // Act
        await producer.ProduceAsync(topic, "large-key", largePayload);

        consumer.Subscribe(topic);
        var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(10));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(largePayload.Length, result.Value.Length);
    }
}

// =============================================================================
// TEST DATA MODELS
// =============================================================================

public record TestMessage(string Id, string Payload, DateTime Timestamp);

// =============================================================================
// TEST RUNNER (for demonstration)
// =============================================================================

public class Program
{
#pragma warning disable CS8892 // Method will not be used as entry point because a synchronous entry point was found
    public static async Task Main()
#pragma warning restore CS8892
    {
        Console.WriteLine("=== Surgewave Test Suite ===");
        Console.WriteLine();
        Console.WriteLine("Run tests with: dotnet test --verbosity normal");
        Console.WriteLine();

        // Quick sanity check
        Console.WriteLine("Performing comprehensive sanity check...");
        Console.WriteLine();

        var fixture = new SurgewaveFixture();
        await fixture.InitializeAsync();

        try
        {
            var passedTests = 0;
            var failedTests = 0;

            // Run sample tests
            Console.WriteLine("Running Producer Tests...");
            var producerTests = new ProducerTests(fixture);

            if (await RunTest("Producer_SendsMessage_ReturnsValidOffset",
                () => producerTests.Producer_SendsMessage_ReturnsValidOffset())) passedTests++; else failedTests++;
            if (await RunTest("Producer_SendsWithKey_PartitionsConsistently",
                () => producerTests.Producer_SendsWithKey_PartitionsConsistently())) passedTests++; else failedTests++;
            if (await RunTest("Producer_BatchSend_AllMessagesDelivered",
                () => producerTests.Producer_BatchSend_AllMessagesDelivered())) passedTests++; else failedTests++;

            Console.WriteLine();
            Console.WriteLine("Running Consumer Tests...");
            var consumerTests = new ConsumerTests(fixture);

            if (await RunTest("Consumer_Subscribe_ReceivesMessages",
                () => consumerTests.Consumer_Subscribe_ReceivesMessages())) passedTests++; else failedTests++;
            if (await RunTest("Consumer_CommitOffset_ResumeFromCorrectPosition",
                () => consumerTests.Consumer_CommitOffset_ResumeFromCorrectPosition())) passedTests++; else failedTests++;

            Console.WriteLine();
            Console.WriteLine("Running Integration Tests...");
            var integrationTests = new IntegrationTests(fixture);

            if (await RunTest("ProducerConsumer_RoundTrip_MessageIntact",
                () => integrationTests.ProducerConsumer_RoundTrip_MessageIntact())) passedTests++; else failedTests++;
            if (await RunTest("ProducerConsumer_HighVolume_NoMessageLoss",
                () => integrationTests.ProducerConsumer_HighVolume_NoMessageLoss())) passedTests++; else failedTests++;

            Console.WriteLine();
            Console.WriteLine("Running Edge Case Tests...");
            var edgeCaseTests = new EdgeCaseTests(fixture);

            if (await RunTest("Consumer_Timeout_ReturnsNull",
                () => edgeCaseTests.Consumer_Timeout_ReturnsNull())) passedTests++; else failedTests++;

            // Summary
            Console.WriteLine();
            Console.WriteLine(new string('=', 50));
            Console.WriteLine($"=== Test Results ===");
            Console.WriteLine($"Total: {passedTests + failedTests} | Passed: {passedTests} | Failed: {failedTests}");

            if (failedTests == 0)
                Console.WriteLine("All tests passed!");
            else
                Console.WriteLine($"WARNING: {failedTests} test(s) failed!");
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    private static async Task<bool> RunTest(string name, Func<Task> test)
    {
        try
        {
            await test();
            Console.WriteLine($"  [PASS] {name}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] {name}: {ex.Message}");
            return false;
        }
    }
}

