// Surgewave Bootcamp - Unit 2, Lesson 1: Unit Testing Basics
// COMPLETED SOLUTION

using Xunit;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

namespace Lesson2_1;

public class ProducerTests
{
    private static async Task<SurgewaveRuntime> CreateRuntimeAsync()
    {
        return await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();
    }

    private static SurgewaveProducer<string, string> CreateProducer(int port)
    {
        return new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{port}";
        });
    }

    private static SurgewaveConsumer<string, string> CreateConsumer(int port)
    {
        return new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{port}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });
    }

    [Fact]
    public async Task Producer_SendsMessage_ReturnsValidOffset()
    {
        // Arrange
        await using var runtime = await CreateRuntimeAsync();
        await using var producer = CreateProducer(runtime.Port);

        // Act
        var result = await producer.ProduceAsync("test-topic", "test-key", "test-value");

        // Assert
        Assert.Equal("test-topic", result.Topic);
        Assert.Equal(0, result.Offset);
        Assert.Equal(0, result.Partition);
    }

    [Fact]
    public async Task Consumer_ReceivesMessage_WithCorrectContent()
    {
        // Arrange
        await using var runtime = await CreateRuntimeAsync();
        await using var producer = CreateProducer(runtime.Port);
        await using var consumer = CreateConsumer(runtime.Port);

        await producer.ProduceAsync("greetings", "user-1", "Hello, Surgewave!");

        // Act
        consumer.Subscribe("greetings");
        var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user-1", result.Key);
        Assert.Equal("Hello, Surgewave!", result.Value);
        Assert.Equal(0, result.Offset);
    }

    [Fact]
    public async Task ProducerConsumer_MultipleMessages_AllReceived()
    {
        // Arrange
        await using var runtime = await CreateRuntimeAsync();
        await using var producer = CreateProducer(runtime.Port);
        await using var consumer = CreateConsumer(runtime.Port);

        for (int i = 0; i < 5; i++)
        {
            await producer.ProduceAsync("numbers", $"key-{i}", $"value-{i}");
        }

        // Act
        consumer.Subscribe("numbers");
        var messages = new List<string>();

        for (int i = 0; i < 5; i++)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
            if (result != null)
                messages.Add(result.Value);
        }

        // Assert
        Assert.Equal(5, messages.Count);
        Assert.Contains("value-0", messages);
        Assert.Contains("value-4", messages);
    }
}
