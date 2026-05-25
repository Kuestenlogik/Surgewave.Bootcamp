# Quiz: Lesson 0.2 - Project Setup

## Multiple Choice

### 1. Which NuGet package is required for creating Surgewave producers and consumers?
- [ ] A) Kuestenlogik.Surgewave.Broker
- [ ] B) Kuestenlogik.Surgewave.Runtime
- [ ] C) Kuestenlogik.Surgewave.Client
- [ ] D) Kuestenlogik.Surgewave.Kafka.Compat

<details>
<summary>Answer</summary>
**C)** Kuestenlogik.Surgewave.Client. This package contains the Producer/Consumer APIs and is needed for all applications that interact with Surgewave.
</details>

### 2. What does the `StorageMode.Memory` setting do?
- [ ] A) Stores messages on disk with memory caching
- [ ] B) Stores messages only in memory (no persistence)
- [ ] C) Compresses messages in memory
- [ ] D) Uses shared memory between processes

<details>
<summary>Answer</summary>
**B)** Stores messages only in memory (no persistence). This is useful for development and testing where persistence isn't needed.
</details>

### 3. Which command creates a new Surgewave console application from template?
- [ ] A) `dotnet new surgewave -n MyApp`
- [ ] B) `dotnet new surgewave-console -n MyApp`
- [ ] C) `dotnet create surgewave-app MyApp`
- [ ] D) `surgewave init MyApp`

<details>
<summary>Answer</summary>
**B)** `dotnet new surgewave-console -n MyApp`. After installing Kuestenlogik.Surgewave.Templates, this command creates a basic console application with embedded broker.
</details>

### 4. What is the purpose of the `WithAutoCreateTopics(true)` configuration?
- [ ] A) Automatically creates backup copies of topics
- [ ] B) Creates topics automatically when a producer sends to a non-existent topic
- [ ] C) Generates topic names based on message content
- [ ] D) Creates test topics for benchmarking

<details>
<summary>Answer</summary>
**B)** Creates topics automatically when a producer sends to a non-existent topic. This simplifies development by not requiring explicit topic creation.
</details>

## True/False

### 5. The Kuestenlogik.Surgewave.Runtime package is only needed when you want to run an embedded broker.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Kuestenlogik.Surgewave.Runtime provides the embedded broker functionality. If you're connecting to a standalone broker, you only need Kuestenlogik.Surgewave.Client.
</details>

### 6. Surgewave can only be configured through code; configuration files like appsettings.json are not supported.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Surgewave supports configuration through appsettings.json and environment variables in addition to code-based configuration.
</details>

## Code Challenge

### 7. What's wrong with this code?

```csharp
await using var producer = new SurgewaveProducer<string, string>(opt =>
    opt.BootstrapServers = $"localhost:{runtime.Port}");

await producer.ProduceAsync("my-topic", "key", "Hello!");

await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .Build()
    .StartAsync();
```

<details>
<summary>Answer</summary>
The issue is that the **broker is started AFTER the producer is created**. The runtime (broker) must be started before creating the producer, otherwise the producer cannot connect.

Fix:
```csharp
// Start broker FIRST
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .Build()
    .StartAsync();

// THEN create producer
await using var producer = new SurgewaveProducer<string, string>(opt =>
    opt.BootstrapServers = $"localhost:{runtime.Port}");

await producer.ProduceAsync("my-topic", "key", "Hello!");
```
</details>

## Score
- 7/7: Excellent! Ready to start building
- 5-6/7: Good understanding, proceed with confidence
- 3-4/7: Review the setup steps before continuing
- <3/7: Re-do the lesson and try the exercises
