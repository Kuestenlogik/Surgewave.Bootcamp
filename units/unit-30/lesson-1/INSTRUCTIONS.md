# Lesson 17.1: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Understand the Starting Code

Open `start/Program.cs`. You'll see:

```csharp
// TODO: Add using statements

// TODO: Create a message size validator

// TODO: Start the Surgewave broker with size limits

// TODO: Create a producer with size limit configuration

// TODO: Test sending messages of various sizes

// TODO: Demonstrate size limit configuration
```

Your job is to replace the `// TODO` comments with working code.

---

## Step 2: Add Using Statements

At the top of the file, add:

```csharp
using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
```

---

## Step 3: Create the Message Size Validator

Create a helper class to validate message sizes:

```csharp
public static class MessageSizeValidator
{
    // Default limit: 1 MB
    public const int DefaultMaxMessageSize = 1 * 1024 * 1024;

    public static ValidationResult Validate(string key, string value, int maxSize = DefaultMaxMessageSize)
    {
        var keyBytes = Encoding.UTF8.GetByteCount(key ?? string.Empty);
        var valueBytes = Encoding.UTF8.GetByteCount(value ?? string.Empty);

        // Account for overhead (headers, length prefixes, etc.)
        const int overhead = 100;
        var totalSize = keyBytes + valueBytes + overhead;

        return new ValidationResult
        {
            IsValid = totalSize <= maxSize,
            TotalSize = totalSize,
            MaxSize = maxSize,
            KeySize = keyBytes,
            ValueSize = valueBytes
        };
    }

    public static string GeneratePayload(int sizeInBytes)
    {
        // Generate a string of approximately the specified size
        var sb = new StringBuilder(sizeInBytes);
        var pattern = "ABCDEFGHIJ"; // 10 chars
        while (sb.Length < sizeInBytes)
        {
            sb.Append(pattern);
        }
        return sb.ToString(0, Math.Min(sb.Length, sizeInBytes));
    }
}

public record ValidationResult
{
    public bool IsValid { get; init; }
    public int TotalSize { get; init; }
    public int MaxSize { get; init; }
    public int KeySize { get; init; }
    public int ValueSize { get; init; }

    public override string ToString() =>
        IsValid
            ? $"Size: {TotalSize:N0} bytes - Within limit"
            : $"Size: {TotalSize:N0} bytes - EXCEEDS limit of {MaxSize:N0} bytes";
}
```

**What this does:**
- Calculates total message size including overhead
- Generates test payloads of specific sizes
- Returns detailed validation results

---

## Step 4: Create the Main Demo Application

Set up the demo with broker and producer:

```csharp
Console.WriteLine("Message Size Limits Demo");
Console.WriteLine("========================");
Console.WriteLine();

// Start embedded Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");

// Default max message size
var maxMessageSize = MessageSizeValidator.DefaultMaxMessageSize;
Console.WriteLine($"Producer configured with MaxMessageSize: {maxMessageSize:N0} bytes (1 MB)");
Console.WriteLine();
```

---

## Step 5: Create Producer and Consumer

```csharp
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "size-demo-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

const string topic = "size-test-topic";
var sentCount = 0;
```

---

## Step 6: Implement Test 1 - Small Message

```csharp
// Test 1: Small message
Console.WriteLine("Test 1: Small message (100 bytes)");
var smallPayload = MessageSizeValidator.GeneratePayload(100);
var validation1 = MessageSizeValidator.Validate("key-1", smallPayload, maxMessageSize);
Console.WriteLine($"  {validation1}");

if (validation1.IsValid)
{
    await producer.ProduceAsync(topic, "key-1", smallPayload);
    Console.WriteLine("  Status: Sent successfully");
    sentCount++;
}
Console.WriteLine();
```

---

## Step 7: Implement Test 2 - Medium Message

```csharp
// Test 2: Medium message (500 KB)
Console.WriteLine("Test 2: Medium message (500 KB)");
var mediumPayload = MessageSizeValidator.GeneratePayload(500 * 1024);
var validation2 = MessageSizeValidator.Validate("key-2", mediumPayload, maxMessageSize);
Console.WriteLine($"  {validation2}");

if (validation2.IsValid)
{
    await producer.ProduceAsync(topic, "key-2", mediumPayload);
    Console.WriteLine("  Status: Sent successfully");
    sentCount++;
}
Console.WriteLine();
```

---

## Step 8: Implement Test 3 - Large Message (Blocked)

```csharp
// Test 3: Large message (2 MB) - should be blocked
Console.WriteLine("Test 3: Large message (2 MB)");
var largePayload = MessageSizeValidator.GeneratePayload(2 * 1024 * 1024);
var validation3 = MessageSizeValidator.Validate("key-3", largePayload, maxMessageSize);
Console.WriteLine($"  {validation3}");

if (validation3.IsValid)
{
    await producer.ProduceAsync(topic, "key-3", largePayload);
    Console.WriteLine("  Status: Sent successfully");
    sentCount++;
}
else
{
    Console.WriteLine("  Status: Blocked by size validation");
}
Console.WriteLine();
```

---

## Step 9: Implement Test 4 - Increased Limit

```csharp
// Test 4: Increase the limit and retry
Console.WriteLine("Test 4: Configuring larger limit (5 MB)");
var newMaxSize = 5 * 1024 * 1024;
Console.WriteLine($"  New limit: {newMaxSize:N0} bytes");

var validation4 = MessageSizeValidator.Validate("key-4", largePayload, newMaxSize);
Console.WriteLine("  Retrying 2 MB message...");

if (validation4.IsValid)
{
    await producer.ProduceAsync(topic, "key-4", largePayload);
    Console.WriteLine("  Status: Sent successfully with increased limit");
    sentCount++;
}
Console.WriteLine();
```

---

## Step 10: Consume and Verify

```csharp
// Consume all sent messages
consumer.Subscribe(topic);
var receivedCount = 0;
var timeout = DateTime.UtcNow.AddSeconds(5);

while (receivedCount < sentCount && DateTime.UtcNow < timeout)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
    if (result != null)
    {
        receivedCount++;
    }
}

Console.WriteLine($"Consumed {receivedCount} messages successfully");
Console.WriteLine("Demo completed!");
```

---

## Complete Solution

Your completed `Program.cs` should look like this:

```csharp
// Surgewave Bootcamp - Unit 17, Lesson 1: Message Size Limits
// COMPLETED SOLUTION

using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("Message Size Limits Demo");
Console.WriteLine("========================");
Console.WriteLine();

// Start embedded Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");

// Default max message size
var maxMessageSize = MessageSizeValidator.DefaultMaxMessageSize;
Console.WriteLine($"Producer configured with MaxMessageSize: {maxMessageSize:N0} bytes (1 MB)");
Console.WriteLine();

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "size-demo-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

const string topic = "size-test-topic";
var sentCount = 0;

// Test 1: Small message
Console.WriteLine("Test 1: Small message (100 bytes)");
var smallPayload = MessageSizeValidator.GeneratePayload(100);
var validation1 = MessageSizeValidator.Validate("key-1", smallPayload, maxMessageSize);
Console.WriteLine($"  {validation1}");

if (validation1.IsValid)
{
    await producer.ProduceAsync(topic, "key-1", smallPayload);
    Console.WriteLine("  Status: Sent successfully");
    sentCount++;
}
Console.WriteLine();

// Test 2: Medium message (500 KB)
Console.WriteLine("Test 2: Medium message (500 KB)");
var mediumPayload = MessageSizeValidator.GeneratePayload(500 * 1024);
var validation2 = MessageSizeValidator.Validate("key-2", mediumPayload, maxMessageSize);
Console.WriteLine($"  {validation2}");

if (validation2.IsValid)
{
    await producer.ProduceAsync(topic, "key-2", mediumPayload);
    Console.WriteLine("  Status: Sent successfully");
    sentCount++;
}
Console.WriteLine();

// Test 3: Large message (2 MB) - should be blocked
Console.WriteLine("Test 3: Large message (2 MB)");
var largePayload = MessageSizeValidator.GeneratePayload(2 * 1024 * 1024);
var validation3 = MessageSizeValidator.Validate("key-3", largePayload, maxMessageSize);
Console.WriteLine($"  {validation3}");

if (validation3.IsValid)
{
    await producer.ProduceAsync(topic, "key-3", largePayload);
    Console.WriteLine("  Status: Sent successfully");
    sentCount++;
}
else
{
    Console.WriteLine("  Status: Blocked by size validation");
}
Console.WriteLine();

// Test 4: Increase the limit and retry
Console.WriteLine("Test 4: Configuring larger limit (5 MB)");
var newMaxSize = 5 * 1024 * 1024;
Console.WriteLine($"  New limit: {newMaxSize:N0} bytes");

var validation4 = MessageSizeValidator.Validate("key-4", largePayload, newMaxSize);
Console.WriteLine("  Retrying 2 MB message...");

if (validation4.IsValid)
{
    await producer.ProduceAsync(topic, "key-4", largePayload);
    Console.WriteLine("  Status: Sent successfully with increased limit");
    sentCount++;
}
Console.WriteLine();

// Consume all sent messages
consumer.Subscribe(topic);
var receivedCount = 0;
var timeout = DateTime.UtcNow.AddSeconds(5);

while (receivedCount < sentCount && DateTime.UtcNow < timeout)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
    if (result != null)
    {
        receivedCount++;
    }
}

Console.WriteLine($"Consumed {receivedCount} messages successfully");
Console.WriteLine("Demo completed!");

// Helper class for message size validation
public static class MessageSizeValidator
{
    public const int DefaultMaxMessageSize = 1 * 1024 * 1024; // 1 MB

    public static ValidationResult Validate(string key, string value, int maxSize = DefaultMaxMessageSize)
    {
        var keyBytes = Encoding.UTF8.GetByteCount(key ?? string.Empty);
        var valueBytes = Encoding.UTF8.GetByteCount(value ?? string.Empty);

        const int overhead = 100;
        var totalSize = keyBytes + valueBytes + overhead;

        return new ValidationResult
        {
            IsValid = totalSize <= maxSize,
            TotalSize = totalSize,
            MaxSize = maxSize,
            KeySize = keyBytes,
            ValueSize = valueBytes
        };
    }

    public static string GeneratePayload(int sizeInBytes)
    {
        var sb = new StringBuilder(sizeInBytes);
        var pattern = "ABCDEFGHIJ";
        while (sb.Length < sizeInBytes)
        {
            sb.Append(pattern);
        }
        return sb.ToString(0, Math.Min(sb.Length, sizeInBytes));
    }
}

public record ValidationResult
{
    public bool IsValid { get; init; }
    public int TotalSize { get; init; }
    public int MaxSize { get; init; }
    public int KeySize { get; init; }
    public int ValueSize { get; init; }

    public override string ToString() =>
        IsValid
            ? $"Size: {TotalSize:N0} bytes - Within limit"
            : $"Size: {TotalSize:N0} bytes - EXCEEDS limit of {MaxSize:N0} bytes";
}
```

---

## Run and Verify

```bash
cd start
dotnet run
```

Expected output:

```
Message Size Limits Demo
========================

Broker started on port 54321
Producer configured with MaxMessageSize: 1,048,576 bytes (1 MB)

Test 1: Small message (100 bytes)
  Size: 208 bytes - Within limit
  Status: Sent successfully

Test 2: Medium message (500 KB)
  Size: 512,100 bytes - Within limit
  Status: Sent successfully

Test 3: Large message (2 MB)
  Size: 2,097,252 bytes - EXCEEDS limit of 1,048,576 bytes
  Status: Blocked by size validation

Test 4: Configuring larger limit (5 MB)
  New limit: 5,242,880 bytes
  Retrying 2 MB message...
  Status: Sent successfully with increased limit

Consumed 3 messages successfully
Demo completed!
```

---

## Knowledge Check

Before moving on, make sure you can answer:

1. Why do message brokers enforce size limits?
2. What components contribute to total message size?
3. Where should size validation happen - producer or broker?
4. How can you handle messages that exceed size limits?

---

**Ready for more?** -> [Lesson 17.2: Compression](../lesson-2/README.md)
