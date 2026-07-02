// Surgewave Bootcamp - Unit 2, Lesson 4: Testcontainers
// COMPLETED SOLUTION
//
// Note: Using embedded broker to simulate container pattern until Surgewave has Docker image.

using Xunit;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

namespace Lesson2_4;

public class SurgewaveContainerFixture : IAsyncLifetime
{
    private SurgewaveRuntime? _runtime;
    public int Port { get; private set; }

    public async Task InitializeAsync()
    {
        // Simulating container startup with embedded broker
        // When Surgewave has a Docker image, replace with:
        // _container = new ContainerBuilder()
        //     .WithImage("surgewave:latest")
        //     .WithPortBinding(9092, true)
        //     .Build();
        // await _container.StartAsync();
        // Port = _container.GetMappedPublicPort(9092);

        _runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Port = _runtime.Port;
    }

    public async Task DisposeAsync()
    {
        if (_runtime != null)
            await _runtime.DisposeAsync();
    }
}

[CollectionDefinition("Surgewave Container")]
public class SurgewaveContainerCollection : ICollectionFixture<SurgewaveContainerFixture>
{
}

[Collection("Surgewave Container")]
public class ContainerIntegrationTests
{
    private readonly SurgewaveContainerFixture _fixture;

    public ContainerIntegrationTests(SurgewaveContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Container_ProduceAndConsume_Works()
    {
        // Arrange
        await using var producer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = $"localhost:{_fixture.Port}");

        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{_fixture.Port}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        // Act
        var topic = $"container-test-{Guid.NewGuid():N}";
        await producer.ProduceAsync(topic, "key", "Container Test!");

        consumer.Subscribe(topic);
        var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(10));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Container Test!", result.Value);
    }

    [Fact]
    public async Task Container_MultipleClients_CanConnect()
    {
        // Arrange - Create multiple producers and consumers
        await using var producer1 = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = $"localhost:{_fixture.Port}");
        await using var producer2 = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = $"localhost:{_fixture.Port}");
        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{_fixture.Port}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        // Act
        var topic = $"multi-client-{Guid.NewGuid():N}";
        await producer1.ProduceAsync(topic, "k1", "From Producer 1");
        await producer2.ProduceAsync(topic, "k2", "From Producer 2");

        consumer.Subscribe(topic);
        var messages = new List<string>();
        for (int i = 0; i < 2; i++)
        {
            var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
            if (result != null)
                messages.Add(result.Value);
        }

        // Assert
        Assert.Equal(2, messages.Count);
        Assert.Contains("From Producer 1", messages);
        Assert.Contains("From Producer 2", messages);
    }
}
