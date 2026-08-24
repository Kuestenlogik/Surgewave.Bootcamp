# Lesson 17.4: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Understand the Starting Code

Open `start/Program.cs`. You'll see:

```csharp
// TODO: Add using statements

// TODO: Create ClaimCheck record

// TODO: Create ClaimCheckMessage record

// TODO: Create IExternalStorage interface

// TODO: Create LocalFileStorage implementation

// TODO: Demonstrate claim-check pattern
```

Your job is to replace the `// TODO` comments with working code.

---

## Step 2: Add Using Statements

At the top of the file, add:

```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
```

---

## Step 3: Create Storage Type Enum

```csharp
public enum StorageType
{
    LocalFile,
    S3,
    AzureBlob
}
```

---

## Step 4: Create the ClaimCheck Record

```csharp
public record ClaimCheck
{
    public Guid Id { get; init; }
    public StorageType StorageType { get; init; }
    public string Location { get; init; } = string.Empty;
    public long Size { get; init; }
    public string ContentType { get; init; } = "application/octet-stream";
    public string Checksum { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
}
```

---

## Step 5: Create the ClaimCheckMessage Record

```csharp
public record ClaimCheckMessage
{
    public ClaimCheck ClaimCheck { get; init; } = null!;
    public Dictionary<string, string> Metadata { get; init; } = new();

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static ClaimCheckMessage Deserialize(string json)
    {
        return JsonSerializer.Deserialize<ClaimCheckMessage>(json)!;
    }
}
```

---

## Step 6: Create the IExternalStorage Interface

```csharp
public interface IExternalStorage
{
    Task<ClaimCheck> StoreAsync(byte[] data, string contentType = "application/octet-stream");
    Task<byte[]> RetrieveAsync(ClaimCheck claimCheck);
    Task DeleteAsync(ClaimCheck claimCheck);
}
```

---

## Step 7: Create the LocalFileStorage Implementation

```csharp
public class LocalFileStorage : IExternalStorage
{
    private readonly string _basePath;
    private readonly TimeSpan _defaultExpiration;

    public LocalFileStorage(string basePath, TimeSpan? defaultExpiration = null)
    {
        _basePath = basePath;
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromDays(1);
        Directory.CreateDirectory(_basePath);
    }

    public async Task<ClaimCheck> StoreAsync(byte[] data, string contentType = "application/octet-stream")
    {
        var id = Guid.NewGuid();
        var filename = $"{id}.bin";
        var filePath = Path.Combine(_basePath, filename);

        await File.WriteAllBytesAsync(filePath, data);

        return new ClaimCheck
        {
            Id = id,
            StorageType = StorageType.LocalFile,
            Location = $"file://{filePath}",
            Size = data.Length,
            ContentType = contentType,
            Checksum = ComputeChecksum(data),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(_defaultExpiration)
        };
    }

    public async Task<byte[]> RetrieveAsync(ClaimCheck claimCheck)
    {
        var filePath = claimCheck.Location.Replace("file://", "");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Claim check file not found: {filePath}");
        }

        var data = await File.ReadAllBytesAsync(filePath);

        // Verify checksum
        var computedChecksum = ComputeChecksum(data);
        if (computedChecksum != claimCheck.Checksum)
        {
            throw new InvalidDataException("Checksum mismatch when retrieving claim check");
        }

        return data;
    }

    public Task DeleteAsync(ClaimCheck claimCheck)
    {
        var filePath = claimCheck.Location.Replace("file://", "");

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    private static string ComputeChecksum(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return $"sha256:{Convert.ToBase64String(hash)}";
    }
}
```

---

## Step 8: Create the Main Demo

```csharp
Console.WriteLine("Claim-Check Pattern Demo");
Console.WriteLine("========================");
Console.WriteLine();

// Start broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");

// Initialize external storage
var storagePath = Path.Combine(Path.GetTempPath(), "claim-check-demo");
var storage = new LocalFileStorage(storagePath);
Console.WriteLine($"External storage initialized at: {storagePath}");
Console.WriteLine();
```

---

## Step 9: Create and Store Large Payload

```csharp
// Create a large payload
Console.WriteLine("Creating large payload (10 MB)...");
var largePayload = new byte[10 * 1024 * 1024];
Random.Shared.NextBytes(largePayload);
Console.WriteLine();

// Store in external storage
Console.WriteLine("Storing payload in external storage...");
var claimCheck = await storage.StoreAsync(largePayload, "application/octet-stream");

var claimCheckMessage = new ClaimCheckMessage
{
    ClaimCheck = claimCheck,
    Metadata = new Dictionary<string, string>
    {
        ["producer"] = "demo-producer",
        ["correlationId"] = Guid.NewGuid().ToString()
    }
};

var serializedReference = claimCheckMessage.Serialize();
Console.WriteLine($"  Stored at: {claimCheck.Location}");
Console.WriteLine($"  Claim check reference size: {Encoding.UTF8.GetByteCount(serializedReference):N0} bytes");
Console.WriteLine($"  Size reduction: {(1 - (double)Encoding.UTF8.GetByteCount(serializedReference) / largePayload.Length) * 100:F3}%");
Console.WriteLine();
```

---

## Step 10: Send Claim Check to Surgewave

```csharp
Console.WriteLine("Sending claim check reference to Surgewave...");

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

const string topic = "claim-checks";
await producer.ProduceAsync(topic, claimCheck.Id.ToString(), serializedReference);
Console.WriteLine($"  Sent reference for payload {claimCheck.Id.ToString()[..8]}");
Console.WriteLine();
```

---

## Step 11: Consume and Retrieve

```csharp
Console.WriteLine("Consuming claim check reference...");

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "claim-check-demo-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe(topic);

ClaimCheckMessage? receivedMessage = null;
var timeout = DateTime.UtcNow.AddSeconds(5);

while (receivedMessage == null && DateTime.UtcNow < timeout)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
    if (result != null)
    {
        receivedMessage = ClaimCheckMessage.Deserialize(result.Value);
        Console.WriteLine($"  Received claim check for {receivedMessage.ClaimCheck.Id.ToString()[..8]}");
        Console.WriteLine($"  Storage type: {receivedMessage.ClaimCheck.StorageType}");
        Console.WriteLine($"  Location: {receivedMessage.ClaimCheck.Location}");
        Console.WriteLine($"  Size: {receivedMessage.ClaimCheck.Size:N0} bytes");
    }
}
Console.WriteLine();
```

---

## Step 12: Fetch Payload

```csharp
if (receivedMessage != null)
{
    Console.WriteLine("Fetching payload from external storage...");
    var retrievedPayload = await storage.RetrieveAsync(receivedMessage.ClaimCheck);
    Console.WriteLine($"  Retrieved {retrievedPayload.Length:N0} bytes");
    Console.WriteLine($"  Content matches original: {retrievedPayload.SequenceEqual(largePayload)}");
    Console.WriteLine();

    // Cleanup
    Console.WriteLine("Cleaning up external storage...");
    await storage.DeleteAsync(receivedMessage.ClaimCheck);
    Console.WriteLine($"  Deleted: {Path.GetFileName(receivedMessage.ClaimCheck.Location.Replace("file://", ""))}");
    Console.WriteLine();
}

Console.WriteLine("Demo completed!");
```

---

## Complete Solution

Your completed `Program.cs` should look like this:

```csharp
// Surgewave Bootcamp - Unit 17, Lesson 4: Claim-Check Pattern
// COMPLETED SOLUTION

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("Claim-Check Pattern Demo");
Console.WriteLine("========================");
Console.WriteLine();

// Start broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");

// Initialize external storage
var storagePath = Path.Combine(Path.GetTempPath(), "claim-check-demo");
var storage = new LocalFileStorage(storagePath);
Console.WriteLine($"External storage initialized at: {storagePath}");
Console.WriteLine();

// Create a large payload
Console.WriteLine("Creating large payload (10 MB)...");
var largePayload = new byte[10 * 1024 * 1024];
Random.Shared.NextBytes(largePayload);
Console.WriteLine();

// Store in external storage
Console.WriteLine("Storing payload in external storage...");
var claimCheck = await storage.StoreAsync(largePayload, "application/octet-stream");

var claimCheckMessage = new ClaimCheckMessage
{
    ClaimCheck = claimCheck,
    Metadata = new Dictionary<string, string>
    {
        ["producer"] = "demo-producer",
        ["correlationId"] = Guid.NewGuid().ToString()
    }
};

var serializedReference = claimCheckMessage.Serialize();
Console.WriteLine($"  Stored at: {claimCheck.Location}");
Console.WriteLine($"  Claim check reference size: {Encoding.UTF8.GetByteCount(serializedReference):N0} bytes");
Console.WriteLine($"  Size reduction: {(1 - (double)Encoding.UTF8.GetByteCount(serializedReference) / largePayload.Length) * 100:F3}%");
Console.WriteLine();

Console.WriteLine("Sending claim check reference to Surgewave...");

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

const string topic = "claim-checks";
await producer.ProduceAsync(topic, claimCheck.Id.ToString(), serializedReference);
Console.WriteLine($"  Sent reference for payload {claimCheck.Id.ToString()[..8]}");
Console.WriteLine();

Console.WriteLine("Consuming claim check reference...");

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "claim-check-demo-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe(topic);

ClaimCheckMessage? receivedMessage = null;
var timeout = DateTime.UtcNow.AddSeconds(5);

while (receivedMessage == null && DateTime.UtcNow < timeout)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
    if (result != null)
    {
        receivedMessage = ClaimCheckMessage.Deserialize(result.Value);
        Console.WriteLine($"  Received claim check for {receivedMessage.ClaimCheck.Id.ToString()[..8]}");
        Console.WriteLine($"  Storage type: {receivedMessage.ClaimCheck.StorageType}");
        Console.WriteLine($"  Location: {receivedMessage.ClaimCheck.Location}");
        Console.WriteLine($"  Size: {receivedMessage.ClaimCheck.Size:N0} bytes");
    }
}
Console.WriteLine();

if (receivedMessage != null)
{
    Console.WriteLine("Fetching payload from external storage...");
    var retrievedPayload = await storage.RetrieveAsync(receivedMessage.ClaimCheck);
    Console.WriteLine($"  Retrieved {retrievedPayload.Length:N0} bytes");
    Console.WriteLine($"  Content matches original: {retrievedPayload.SequenceEqual(largePayload)}");
    Console.WriteLine();

    Console.WriteLine("Cleaning up external storage...");
    await storage.DeleteAsync(receivedMessage.ClaimCheck);
    Console.WriteLine($"  Deleted: {Path.GetFileName(receivedMessage.ClaimCheck.Location.Replace("file://", ""))}");
    Console.WriteLine();
}

Console.WriteLine("Demo completed!");

// Types

public enum StorageType
{
    LocalFile,
    S3,
    AzureBlob
}

public record ClaimCheck
{
    public Guid Id { get; init; }
    public StorageType StorageType { get; init; }
    public string Location { get; init; } = string.Empty;
    public long Size { get; init; }
    public string ContentType { get; init; } = "application/octet-stream";
    public string Checksum { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
}

public record ClaimCheckMessage
{
    public ClaimCheck ClaimCheck { get; init; } = null!;
    public Dictionary<string, string> Metadata { get; init; } = new();

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static ClaimCheckMessage Deserialize(string json)
    {
        return JsonSerializer.Deserialize<ClaimCheckMessage>(json)!;
    }
}

public interface IExternalStorage
{
    Task<ClaimCheck> StoreAsync(byte[] data, string contentType = "application/octet-stream");
    Task<byte[]> RetrieveAsync(ClaimCheck claimCheck);
    Task DeleteAsync(ClaimCheck claimCheck);
}

public class LocalFileStorage : IExternalStorage
{
    private readonly string _basePath;
    private readonly TimeSpan _defaultExpiration;

    public LocalFileStorage(string basePath, TimeSpan? defaultExpiration = null)
    {
        _basePath = basePath;
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromDays(1);
        Directory.CreateDirectory(_basePath);
    }

    public async Task<ClaimCheck> StoreAsync(byte[] data, string contentType = "application/octet-stream")
    {
        var id = Guid.NewGuid();
        var filename = $"{id}.bin";
        var filePath = Path.Combine(_basePath, filename);

        await File.WriteAllBytesAsync(filePath, data);

        return new ClaimCheck
        {
            Id = id,
            StorageType = StorageType.LocalFile,
            Location = $"file://{filePath}",
            Size = data.Length,
            ContentType = contentType,
            Checksum = ComputeChecksum(data),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(_defaultExpiration)
        };
    }

    public async Task<byte[]> RetrieveAsync(ClaimCheck claimCheck)
    {
        var filePath = claimCheck.Location.Replace("file://", "");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Claim check file not found: {filePath}");
        }

        var data = await File.ReadAllBytesAsync(filePath);

        var computedChecksum = ComputeChecksum(data);
        if (computedChecksum != claimCheck.Checksum)
        {
            throw new InvalidDataException("Checksum mismatch when retrieving claim check");
        }

        return data;
    }

    public Task DeleteAsync(ClaimCheck claimCheck)
    {
        var filePath = claimCheck.Location.Replace("file://", "");

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    private static string ComputeChecksum(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return $"sha256:{Convert.ToBase64String(hash)}";
    }
}
```

---

## Run and Verify

```bash
cd start
dotnet run
```

---

## Knowledge Check

Before moving on, make sure you can answer:

1. What information should a claim check reference contain?
2. Why is the claim check pattern better than chunking for very large payloads?
3. How do you handle cleanup of external storage?
4. What happens if external storage is unavailable when consumer tries to retrieve?

---

**Ready for more?** -> [Lesson 17.5: Streaming Large Data](../lesson-5/README.md)
