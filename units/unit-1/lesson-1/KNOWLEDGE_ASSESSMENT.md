# Quiz: Lesson 1.1 - Hello Surgewave

## Multiple Choice

### 1. What does the Surgewave broker do?
- [ ] A) Only stores messages temporarily in memory
- [ ] B) Stores messages in topics, handles connections, manages partitions and replication
- [ ] C) Converts messages between different formats
- [ ] D) Routes HTTP requests to microservices

<details>
<summary>Answer</summary>
**B)** Stores messages in topics, handles connections, manages partitions and replication. The broker is the server component that manages all message storage and client coordination.
</details>

### 2. What is the correct lifecycle for connecting to Surgewave?
- [ ] A) Create client -> Connect -> Disconnect
- [ ] B) Connect -> Create client -> Use -> Dispose
- [ ] C) Create client with config -> Use client -> Dispose client
- [ ] D) Start broker -> Create client -> Use client -> Dispose client -> Stop broker

<details>
<summary>Answer</summary>
**D)** Start broker -> Create client -> Use client -> Dispose client -> Stop broker. When using an embedded broker, you must start it first, then create clients, and when shutting down, dispose clients before stopping the broker.
</details>

### 3. Which code snippet correctly creates a Surgewave client connection?
- [ ] A) `var client = new SurgewaveClient("localhost:9092");`
- [ ] B) `var client = SurgewaveClient.Connect("localhost:9092");`
- [ ] C) `await using var client = new SurgewaveClientBuilder().WithBootstrapServers("localhost:9092").Build();`
- [ ] D) `var client = await SurgewaveClientBuilder.CreateAsync("localhost:9092");`

<details>
<summary>Answer</summary>
**C)** `await using var client = new SurgewaveClientBuilder().WithBootstrapServers("localhost:9092").Build();`. The builder pattern is used to configure and create clients, and `await using` ensures proper disposal.
</details>

## True/False

### 4. Running `dotnet run --project src/Kuestenlogik.Surgewave.Broker` starts a standalone Surgewave broker.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - This command starts the Surgewave broker as a standalone process, which is an alternative to embedded mode.
</details>

### 5. When the broker has no topics, the ListTopics operation will throw an exception.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - ListTopics returns an empty collection when no topics exist. This is normal behavior when starting fresh.
</details>

## Code Challenge

### 6. What's the issue with this code?

```csharp
var client = new SurgewaveClientBuilder()
    .WithBootstrapServers("localhost:9092")
    .Build();

var topics = await client.Topics.ListAsync();
Console.WriteLine($"Found {topics.Count} topics");
// Application exits here
```

<details>
<summary>Answer</summary>
The issue is that the **client is not being disposed**. The client should use `await using` or be explicitly disposed to ensure connections are properly closed.

Fix:
```csharp
await using var client = new SurgewaveClientBuilder()
    .WithBootstrapServers("localhost:9092")
    .Build();

var topics = await client.Topics.ListAsync();
Console.WriteLine($"Found {topics.Count} topics");
// Client is automatically disposed when scope ends
```
</details>

### 7. Complete the code to start an embedded broker on a dynamic port:

```csharp
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(_____)  // What value for auto-assign?
    .WithStorageEngine(StorageEngines.Memory)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker running on port {runtime._____}");  // How to get the port?
```

<details>
<summary>Answer</summary>
```csharp
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)  // 0 = auto-assign available port
    .WithStorageEngine(StorageEngines.Memory)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker running on port {runtime.Port}");  // Port property contains actual port
```

Using `WithPort(0)` lets the system find an available port, and `runtime.Port` retrieves the assigned port number.
</details>

## Score
- 7/7: Excellent! You understand the Surgewave connection lifecycle
- 5-6/7: Good understanding, ready to proceed
- 3-4/7: Review broker and client basics
- <3/7: Re-do the lesson with hands-on practice
