# Quiz: Lesson 0.3 - Hello Surgewave

## Multiple Choice

### 1. What is the purpose of `await using` when creating a Surgewave client?
- [ ] A) It makes the code run faster
- [ ] B) It ensures proper resource disposal when the scope ends
- [ ] C) It enables parallel processing
- [ ] D) It converts synchronous code to asynchronous

<details>
<summary>Answer</summary>
**B)** It ensures proper resource disposal when the scope ends. Surgewave clients implement IAsyncDisposable, and `await using` ensures connections are properly closed and resources are released.
</details>

### 2. What are the two modes for running a Surgewave broker?
- [ ] A) Debug and Release
- [ ] B) Local and Remote
- [ ] C) Standalone and Embedded
- [ ] D) Single and Clustered

<details>
<summary>Answer</summary>
**C)** Standalone and Embedded. Standalone runs the broker as a separate process, while embedded runs the broker within your application.
</details>

### 3. What does the "Connection refused" error typically indicate?
- [ ] A) Invalid credentials
- [ ] B) The broker isn't running
- [ ] C) The topic doesn't exist
- [ ] D) Network firewall blocking

<details>
<summary>Answer</summary>
**B)** The broker isn't running. This error occurs when trying to connect to a broker that hasn't been started or is not accessible at the specified address.
</details>

## True/False

### 4. An embedded Surgewave broker must be started before creating client connections.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The broker must be running before clients can connect. Starting the embedded broker first ensures clients have something to connect to.
</details>

### 5. The SurgewaveClientBuilder requires you to specify all configuration options; there are no defaults.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Surgewave provides sensible defaults for most configuration options. You only need to specify the essential options like BootstrapServers.
</details>

## Code Challenge

### 6. Complete the missing code to properly create and connect a Surgewave client:

```csharp
// 1. Start the embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageEngine(StorageEngines.Memory)
    .Build()
    .StartAsync();

// 2. Create client - what goes here?
await using var client = ______________________
    .WithBootstrapServers("localhost:9092")
    .Build();

// 3. List topics
var topics = await client.Topics.ListAsync();
```

<details>
<summary>Answer</summary>
The missing code is:

```csharp
await using var client = new SurgewaveClientBuilder()
    .WithBootstrapServers("localhost:9092")
    .Build();
```

The `SurgewaveClientBuilder` is the entry point for creating Surgewave client connections.
</details>

### 7. What happens if you try to use port 9092 but another process is already using it?

<details>
<summary>Answer</summary>
You will get a "Port already in use" error. Solutions include:
1. Stop the other process using port 9092
2. Use a different port: `.WithPort(9093)` or another available port
3. Use `.WithPort(0)` to let the system auto-assign an available port

Using `.WithPort(0)` is recommended for testing as it avoids port conflicts.
</details>

## Score
- 7/7: Excellent! You're ready for Unit 1
- 5-6/7: Good foundation established
- 3-4/7: Review the connection lifecycle
- <3/7: Re-do the lesson with hands-on practice
