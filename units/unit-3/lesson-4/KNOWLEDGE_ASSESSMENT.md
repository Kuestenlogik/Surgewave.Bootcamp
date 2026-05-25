# Knowledge Assessment: Lesson 2.4 - Testcontainers

## Multiple Choice

### 1. When should you prefer Testcontainers over the embedded broker?
- [ ] A) For unit tests requiring fast startup
- [ ] B) For E2E tests, CI/CD pipelines, and testing Docker deployment
- [ ] C) When you don't have Docker installed
- [ ] D) For mocking external services

<details>
<summary>Answer</summary>
**B)** For E2E tests, CI/CD pipelines, and testing Docker deployment. Testcontainers provides a realistic containerized environment, which is ideal for acceptance tests and verifying production-like behavior.
</details>

### 2. What does `WithPortBinding(9092, true)` do in a container configuration?
- [ ] A) Binds port 9092 to a fixed host port 9092
- [ ] B) Binds container port 9092 to a random available host port
- [ ] C) Disables port 9092
- [ ] D) Creates a virtual network on port 9092

<details>
<summary>Answer</summary>
**B)** Binds container port 9092 to a random available host port. The `true` parameter enables random port assignment, which prevents port conflicts in parallel test execution.
</details>

### 3. What is the purpose of `WithWaitStrategy` in Testcontainers?
- [ ] A) To set the container startup timeout
- [ ] B) To define how to determine when the container is ready
- [ ] C) To wait for other containers to start first
- [ ] D) To add a delay before running tests

<details>
<summary>Answer</summary>
**B)** To define how to determine when the container is ready. Wait strategies ensure the container's service is fully available before tests begin, such as waiting for a port to be open.
</details>

### 4. How do you get the actual mapped port after a container starts?
- [ ] A) `container.Port`
- [ ] B) `container.GetMappedPublicPort(9092)`
- [ ] C) `container.HostPort`
- [ ] D) `container.Ports[9092]`

<details>
<summary>Answer</summary>
**B)** `container.GetMappedPublicPort(9092)`. This method returns the host port that was dynamically mapped to the container's internal port 9092.
</details>

## True/False

### 5. Testcontainers requires Docker to be installed and running on the test machine.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Testcontainers uses Docker to run containers, so Docker Desktop (or Docker Engine on Linux) must be installed and running.
</details>

### 6. The embedded broker is better for E2E acceptance tests because it's faster.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - While the embedded broker is faster, Testcontainers is better for E2E tests because it tests the actual containerized deployment, which is closer to production conditions.
</details>

## Code Challenge

### 7. Complete this Testcontainers fixture:

```csharp
public class SurgewaveContainerFixture : IAsyncLifetime
{
    private readonly IContainer _container;
    public int Port { get; private set; }

    public SurgewaveContainerFixture()
    {
        _container = new ____________()
            .WithImage("surgewave:latest")
            .WithPortBinding(9092, ______)  // Random port mapping
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .____________(9092))    // Wait for port
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.____________();
        Port = _container.____________(9092);
    }

    public async Task DisposeAsync()
    {
        await _container.____________();
    }
}
```

<details>
<summary>Answer</summary>
```csharp
public class SurgewaveContainerFixture : IAsyncLifetime
{
    private readonly IContainer _container;
    public int Port { get; private set; }

    public SurgewaveContainerFixture()
    {
        _container = new ContainerBuilder()
            .WithImage("surgewave:latest")
            .WithPortBinding(9092, true)  // Random port mapping
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilPortIsAvailable(9092))  // Wait for port
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Port = _container.GetMappedPublicPort(9092);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
```

Key elements:
- `ContainerBuilder` creates the container configuration
- `true` in `WithPortBinding` enables random port assignment
- `UntilPortIsAvailable` waits for the service to be ready
- `StartAsync()` starts the container
- `GetMappedPublicPort()` retrieves the actual host port
- `DisposeAsync()` cleans up the container
</details>

## Score
- 7/7: Excellent! You understand containerized testing
- 5-6/7: Good grasp of Testcontainers basics
- 3-4/7: Review container configuration and wait strategies
- <3/7: Re-do the lesson and practice with Docker
