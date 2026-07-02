// Surgewave Bootcamp - Unit 17, Lesson 1: Message Size Limits
// COMPLETED SOLUTION

using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
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
