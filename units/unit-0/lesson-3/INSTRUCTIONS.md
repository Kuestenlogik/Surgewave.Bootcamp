# Lesson 1.1: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Understand the Starting Code

Open `start/Program.cs`. You'll see:

```csharp
// TODO: Add the necessary using statements

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.1 - Hello Surgewave ===");
Console.WriteLine();

// TODO: Step 1 - Start an embedded Surgewave broker
Console.WriteLine("Starting embedded Surgewave broker...");

// TODO: Step 2 - Create a Surgewave client connection
Console.WriteLine("Connecting to Surgewave...");

// TODO: Step 3 - List available topics
Console.WriteLine("Available topics:");

// TODO: Step 4 - Clean shutdown
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");
```

Your job is to replace the `// TODO` comments with working code.

---

## Step 2: Add Using Statements

At the top of the file, add:

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client.Native;
```

These namespaces provide:
- `Kuestenlogik.Surgewave.Runtime` - The embedded SurgewaveRuntime
- `Kuestenlogik.Surgewave.Broker` - Storage mode enum
- `Kuestenlogik.Surgewave.Client.Native` - The Surgewave native client for connecting

---

## Step 3: Start the Embedded Broker

Replace the first TODO with code to start an embedded broker:

```csharp
// Step 1 - Start an embedded Surgewave broker
Console.WriteLine("Starting embedded Surgewave broker...");

await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)  // Auto-assign an available port
    .WithStorageMode(StorageMode.Memory)  // Use in-memory storage for this lesson
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on localhost:{runtime.Port}");
Console.WriteLine();
```

**What this does:**
- `SurgewaveRuntime.CreateBuilder()` creates a broker configuration
- `WithPort(0)` auto-assigns an available port
- `WithStorageMode(StorageMode.Memory)` uses RAM for fast testing
- `WithAutoCreateTopics(true)` creates topics on first use
- `Build().StartAsync()` starts the broker
- `await using` ensures proper cleanup when we're done

---

## Step 4: Create a Client Connection

Replace the second TODO with code to connect:

```csharp
// Step 2 - Create a Surgewave client connection
Console.WriteLine("Connecting to Surgewave...");

await using var client = new SurgewaveNativeClient("localhost", runtime.Port);
await client.ConnectAsync();

Console.WriteLine("Connected successfully!");
Console.WriteLine();
```

**What this does:**
- `SurgewaveNativeClient` creates a native protocol client
- Uses the port from the running broker (`runtime.Port`)
- `ConnectAsync()` establishes the connection
- `await using` ensures proper disconnection

---

## Step 5: List Available Topics

Replace the third TODO to list topics:

```csharp
// Step 3 - List available topics
Console.WriteLine("Available topics:");

var topics = await client.Topics.ListAsync();

if (topics.Count == 0)
{
    Console.WriteLine("  (no topics yet - we'll create some in the next lesson)");
}
else
{
    foreach (var topic in topics)
    {
        Console.WriteLine($"  - {topic.Name} ({topic.PartitionCount} partitions)");
    }
}
Console.WriteLine();
```

**What this does:**
- `client.Topics.ListAsync()` retrieves all topics
- We check if the list is empty
- If topics exist, we print each one with its partition count

---

## Step 6: Clean Shutdown

The final TODO is already handled by `await using`, but we can add a nice message:

```csharp
// Step 4 - Clean shutdown (handled by await using)
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");
```

The `await using` statements automatically:
1. Disconnect the client
2. Stop the broker
3. Release all resources

---

## Complete Solution

Your completed `Program.cs` should look like this:

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client.Native;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.1 - Hello Surgewave ===");
Console.WriteLine();

// Step 1 - Start an embedded Surgewave broker
Console.WriteLine("Starting embedded Surgewave broker...");

await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on localhost:{runtime.Port}");
Console.WriteLine();

// Step 2 - Create a Surgewave client connection
Console.WriteLine("Connecting to Surgewave...");

await using var client = new SurgewaveNativeClient("localhost", runtime.Port);
await client.ConnectAsync();

Console.WriteLine("Connected successfully!");
Console.WriteLine();

// Step 3 - List available topics
Console.WriteLine("Available topics:");

var topics = await client.Topics.ListAsync();

if (topics.Count == 0)
{
    Console.WriteLine("  (no topics yet - we'll create some in the next lesson)");
}
else
{
    foreach (var topic in topics)
    {
        Console.WriteLine($"  - {topic.Name} ({topic.PartitionCount} partitions)");
    }
}
Console.WriteLine();

// Step 4 - Clean shutdown
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");
```

---

## Run and Verify

```bash
cd start
dotnet run
```

Expected output:

```
=== Surgewave Bootcamp: Lesson 1.1 - Hello Surgewave ===

Starting embedded Surgewave broker...
Broker started on localhost:9092

Connecting to Surgewave...
Connected successfully!

Available topics:
  (no topics yet - we'll create some in the next lesson)

Shutting down...
Goodbye!
```

---

## Experiment (Optional)

Try these variations to deepen your understanding:

### 1. Use File Storage

Change the storage type to see data persist:

```csharp
.WithStorage(StorageType.FileSystem)
.WithDataDirectory("./surgewave-data")
```

### 2. Change the Port

Use a different port:

```csharp
.WithPort(19092)
// ...
.WithBootstrapServers("localhost:19092")
```

### 3. Add Error Handling

Wrap the connection in try-catch:

```csharp
try
{
    await using var client = new SurgewaveClientBuilder()
        .WithBootstrapServers("localhost:9092")
        .Build();
    // ...
}
catch (Exception ex)
{
    Console.WriteLine($"Connection failed: {ex.Message}");
}
```

---

## Knowledge Check

Before moving on, make sure you can answer:

1. What's the difference between embedded and standalone broker?
2. Why do we use `await using` for the runtime and client?
3. What happens if you try to connect before starting the broker?
4. What does `StorageType.Memory` mean for data persistence?

---

**Ready for more?** → [Lesson 1.2: Producing Messages](../lesson-2/README.md)
