// Surgewave Bootcamp - Unit 2, Lesson 3: Integration Testing
// COMPLETED SOLUTION

using Xunit;
using System.Runtime.CompilerServices;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

namespace Lesson2_3;

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
    }

    public async Task DisposeAsync()
    {
        await Runtime.DisposeAsync();
    }
}

public class ProducerConsumerIntegrationTests : IClassFixture<SurgewaveFixture>
{
    private readonly SurgewaveFixture _fixture;

    public ProducerConsumerIntegrationTests(SurgewaveFixture fixture)
    {
        _fixture = fixture;
    }

    private static string UniqueTopic([CallerMemberName] string? name = null)
        => $"test-{name}-{Guid.NewGuid():N}";

    private SurgewaveProducer<string, string> CreateProducer()
        => new(opt => opt.BootstrapServers = $"localhost:{_fixture.Port}");

    private SurgewaveConsumer<string, string> CreateConsumer()
        => new(opt =>
        {
            opt.BootstrapServers = $"localhost:{_fixture.Port}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

    [Fact]
    public async Task EndToEnd_ProduceAndConsume_MessageReceived()
    {
        // Arrange
        var topic = UniqueTopic();
        await using var producer = CreateProducer();
        await using var consumer = CreateConsumer();

        // Act - Produce
        await producer.ProduceAsync(topic, "key", "Hello Integration Test!");

        // Act - Consume
        consumer.Subscribe(topic);
        var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello Integration Test!", result.Value);
    }

    [Fact]
    public async Task EndToEnd_MultipleMessages_AllReceived()
    {
        // Arrange
        var topic = UniqueTopic();
        await using var producer = CreateProducer();
        await using var consumer = CreateConsumer();
        var messages = new[] { "First", "Second", "Third" };

        // Act - Produce all
        foreach (var msg in messages)
        {
            await producer.ProduceAsync(topic, "key", msg);
        }

        // Act - Consume all
        consumer.Subscribe(topic);
        var received = new List<string>();
        for (int i = 0; i < messages.Length; i++)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
            if (result != null)
                received.Add(result.Value);
        }

        // Assert
        Assert.Equal(messages.Length, received.Count);
        Assert.Equal(messages, received);
    }

    [Fact]
    public async Task EndToEnd_SameKey_PreservesOrder()
    {
        // Arrange
        var topic = UniqueTopic();
        await using var producer = CreateProducer();
        await using var consumer = CreateConsumer();

        // Act - Produce with same key
        for (int i = 0; i < 5; i++)
        {
            await producer.ProduceAsync(topic, "same-key", $"Message-{i}");
        }

        // Consume
        consumer.Subscribe(topic);
        var received = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
            if (result != null)
                received.Add(result.Value);
        }

        // Assert - Order preserved
        Assert.Equal(5, received.Count);
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal($"Message-{i}", received[i]);
        }
    }

    [Fact]
    public async Task EndToEnd_TwoConsumers_EachGetsAllMessages()
    {
        // Arrange
        var topic = UniqueTopic();
        await using var producer = CreateProducer();
        await using var consumer1 = CreateConsumer();
        await using var consumer2 = CreateConsumer();

        // Produce
        await producer.ProduceAsync(topic, "k1", "Shared Message");

        // Both consumers subscribe (no group - each gets all)
        consumer1.Subscribe(topic);
        consumer2.Subscribe(topic);

        var result1 = await consumer1.ConsumeAsync(TimeSpan.FromSeconds(5));
        var result2 = await consumer2.ConsumeAsync(TimeSpan.FromSeconds(5));

        // Assert - Both got the message
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal("Shared Message", result1.Value);
        Assert.Equal("Shared Message", result2.Value);
    }
}
