// Surgewave Bootcamp - Unit 2, Lesson 4: Testcontainers
//
// In this lesson, you'll learn to:
// - Use Testcontainers pattern for integration tests
// - Share containers across test classes with collection fixtures
// - Configure containers for CI/CD
//
// Note: Using embedded broker to simulate container pattern until Surgewave has Docker image.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

namespace Lesson2_4;

// TODO: Implement container fixture (using embedded broker for now)
public class SurgewaveContainerFixture : IAsyncLifetime
{
    private ISurgewaveRuntime? _runtime;
    public int Port { get; private set; }

    public async Task InitializeAsync()
    {
        // TODO: Start embedded broker (simulating container startup)
        throw new NotImplementedException();
    }

    public async Task DisposeAsync()
    {
        // TODO: Dispose runtime
        throw new NotImplementedException();
    }
}

// TODO: Define collection for shared container
[CollectionDefinition("Surgewave Container")]
public class SurgewaveContainerCollection : ICollectionFixture<SurgewaveContainerFixture>
{
}

// TODO: Implement tests using the container fixture
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
        // TODO: Create producer and consumer using _fixture.Port
        // TODO: Produce a message, consume it, verify

        throw new NotImplementedException();
    }

    [Fact]
    public async Task Container_MultipleClients_CanConnect()
    {
        // TODO: Create multiple producers/consumers
        // TODO: Verify they all work with the same "container"

        throw new NotImplementedException();
    }
}
