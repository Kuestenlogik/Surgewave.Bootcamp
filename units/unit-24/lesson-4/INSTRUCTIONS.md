# Lesson 15.4: Instructions

Follow these step-by-step instructions to implement data recovery.

## Step 1: Define Recovery Configuration

Create configuration for the recovery service:

```csharp
public class RecoveryConfig
{
    public string BackupPath { get; set; } = "./backups";
    public string TargetBootstrapServers { get; set; } = "";
    public string TopicSuffix { get; set; } = "_recovered";
    public bool VerifyChecksum { get; set; } = true;
    public int BatchSize { get; set; } = 1000;
}
```

## Step 2: Create the Recovery Service

Implement the main recovery logic:

```csharp
public class RecoveryService
{
    private readonly RecoveryConfig _config;

    public RecoveryService(RecoveryConfig config)
    {
        _config = config;
    }

    public async Task<RecoveryResult> RestoreTopicAsync(
        string topic,
        string backupId,
        CancellationToken ct = default)
    {
        var result = new RecoveryResult
        {
            StartTime = DateTime.UtcNow,
            SourceTopic = topic,
            BackupId = backupId
        };

        try
        {
            // Step 1: Load and verify manifest
            Console.WriteLine("Loading backup manifest...");
            var manifest = await LoadManifestAsync(topic, backupId);
            if (manifest == null)
            {
                result.Success = false;
                result.Error = "Manifest not found";
                return result;
            }
            Console.WriteLine($"  Backup date: {manifest.CreatedAt}");
            Console.WriteLine($"  Total messages: {manifest.TotalMessages}");

            // Step 2: Verify backup integrity
            if (_config.VerifyChecksum)
            {
                Console.WriteLine("Verifying backup integrity...");
                var isValid = await VerifyBackupAsync(topic, backupId, manifest);
                if (!isValid)
                {
                    result.Success = false;
                    result.Error = "Backup verification failed";
                    return result;
                }
                Console.WriteLine("  Backup integrity verified");
            }

            // Step 3: Restore messages to target topic
            var targetTopic = topic + _config.TopicSuffix;
            Console.WriteLine($"Restoring to topic: {targetTopic}");

            result.TargetTopic = targetTopic;
            result.MessagesRestored = await RestoreMessagesAsync(
                topic, backupId, manifest, targetTopic, ct);

            // Step 4: Verify restoration
            Console.WriteLine("Verifying restoration...");
            result.VerificationPassed = await VerifyRestorationAsync(
                targetTopic, manifest.TotalMessages);

            result.Success = true;
            result.EndTime = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            result.EndTime = DateTime.UtcNow;
        }

        return result;
    }
}
```

## Step 3: Implement Manifest Loading

Load backup metadata:

```csharp
private async Task<BackupManifest?> LoadManifestAsync(string topic, string backupId)
{
    var manifestPath = Path.Combine(_config.BackupPath, topic, backupId, "manifest.json");

    if (!File.Exists(manifestPath))
        return null;

    var json = await File.ReadAllTextAsync(manifestPath);
    return JsonSerializer.Deserialize<BackupManifest>(json);
}

public IEnumerable<BackupInfo> ListAvailableBackups(string topic)
{
    var topicDir = Path.Combine(_config.BackupPath, topic);
    if (!Directory.Exists(topicDir))
        yield break;

    foreach (var dir in Directory.GetDirectories(topicDir).OrderByDescending(d => d))
    {
        var manifestPath = Path.Combine(dir, "manifest.json");
        if (!File.Exists(manifestPath))
            continue;

        var manifest = JsonSerializer.Deserialize<BackupManifest>(
            File.ReadAllText(manifestPath));

        if (manifest != null)
        {
            yield return new BackupInfo
            {
                BackupId = Path.GetFileName(dir),
                CreatedAt = manifest.CreatedAt,
                MessageCount = manifest.TotalMessages,
                SizeBytes = manifest.TotalBytes
            };
        }
    }
}
```

## Step 4: Implement Message Restoration

Restore messages from backup files:

```csharp
private async Task<long> RestoreMessagesAsync(
    string topic,
    string backupId,
    BackupManifest manifest,
    string targetTopic,
    CancellationToken ct)
{
    await using var producer = new SurgewaveProducer<string, string>(options =>
    {
        options.BootstrapServers = _config.TargetBootstrapServers;
    });

    long totalRestored = 0;
    var backupDir = Path.Combine(_config.BackupPath, topic, backupId);

    foreach (var partition in manifest.Partitions)
    {
        var filePath = Path.Combine(backupDir, partition.FileName);
        Console.WriteLine($"  Restoring partition {partition.PartitionId}...");

        var count = await RestorePartitionAsync(
            filePath, partition, targetTopic, producer, ct);

        totalRestored += count;
        Console.WriteLine($"    Restored {count} messages");
    }

    return totalRestored;
}

private async Task<long> RestorePartitionAsync(
    string filePath,
    PartitionBackup partition,
    string targetTopic,
    SurgewaveProducer<string, string> producer,
    CancellationToken ct)
{
    await using var fileStream = File.OpenRead(filePath);
    Stream readStream = fileStream;

    if (partition.FileName.EndsWith(".gz"))
        readStream = new GZipStream(fileStream, CompressionMode.Decompress);

    using var reader = new StreamReader(readStream);
    long count = 0;

    string? line;
    while ((line = await reader.ReadLineAsync(ct)) != null)
    {
        if (ct.IsCancellationRequested) break;

        var record = JsonSerializer.Deserialize<BackupRecord>(line);
        if (record != null)
        {
            await producer.ProduceAsync(targetTopic, record.Key, record.Value);
            count++;

            if (count % 1000 == 0)
                Console.WriteLine($"    Progress: {count} messages");
        }
    }

    return count;
}
```

## Step 5: Implement Point-in-Time Recovery

Restore to a specific point in time:

```csharp
public async Task<RecoveryResult> RestoreToPointInTimeAsync(
    string topic,
    DateTime targetTime,
    CancellationToken ct = default)
{
    Console.WriteLine($"Point-in-time recovery for {topic}");
    Console.WriteLine($"  Target time: {targetTime:O}");

    // Find the most recent backup before the target time
    var backups = ListAvailableBackups(topic)
        .Where(b => b.CreatedAt <= targetTime)
        .OrderByDescending(b => b.CreatedAt)
        .ToList();

    if (backups.Count == 0)
    {
        return new RecoveryResult
        {
            Success = false,
            Error = "No backup found before target time"
        };
    }

    var selectedBackup = backups.First();
    Console.WriteLine($"  Selected backup: {selectedBackup.BackupId}");
    Console.WriteLine($"  Backup date: {selectedBackup.CreatedAt}");
    Console.WriteLine($"  Data gap: {targetTime - selectedBackup.CreatedAt}");

    // Restore from the selected backup
    var result = await RestoreTopicAsync(topic, selectedBackup.BackupId, ct);

    if (result.Success)
    {
        result.DataGap = targetTime - selectedBackup.CreatedAt;
        Console.WriteLine($"  Warning: {result.DataGap} of data may be missing");
    }

    return result;
}
```

## Step 6: Implement Consistency Verification

Verify restored data matches the backup:

```csharp
private async Task<bool> VerifyRestorationAsync(string targetTopic, long expectedCount)
{
    await using var consumer = new SurgewaveConsumer<string, string>(options =>
    {
        options.BootstrapServers = _config.TargetBootstrapServers;
        options.GroupId = $"recovery-verify-{Guid.NewGuid():N}";
        options.AutoOffsetReset = AutoOffsetReset.Earliest;
    });

    consumer.Subscribe(targetTopic);

    long actualCount = 0;
    var timeout = DateTime.UtcNow.AddSeconds(30);

    while (DateTime.UtcNow < timeout)
    {
        var msg = consumer.Consume(TimeSpan.FromMilliseconds(100));
        if (msg != null)
            actualCount++;
        else if (actualCount >= expectedCount)
            break;
    }

    Console.WriteLine($"  Expected: {expectedCount}, Actual: {actualCount}");

    var match = actualCount >= expectedCount;
    Console.WriteLine($"  Verification: {(match ? "PASSED" : "FAILED")}");

    return match;
}

public async Task<ConsistencyReport> GenerateConsistencyReportAsync(
    string originalTopic,
    string restoredTopic)
{
    var report = new ConsistencyReport
    {
        OriginalTopic = originalTopic,
        RestoredTopic = restoredTopic,
        CheckedAt = DateTime.UtcNow
    };

    // Count messages in both topics
    report.OriginalCount = await CountMessagesAsync(originalTopic);
    report.RestoredCount = await CountMessagesAsync(restoredTopic);
    report.CountMatch = report.OriginalCount == report.RestoredCount;

    // Calculate checksums
    report.OriginalChecksum = await CalculateTopicChecksumAsync(originalTopic);
    report.RestoredChecksum = await CalculateTopicChecksumAsync(restoredTopic);
    report.ChecksumMatch = report.OriginalChecksum == report.RestoredChecksum;

    report.IsConsistent = report.CountMatch && report.ChecksumMatch;

    return report;
}
```

## Step 7: Handle Partial Recovery

Restore specific partitions or offset ranges:

```csharp
public async Task<RecoveryResult> RestorePartialAsync(
    string topic,
    string backupId,
    PartialRecoveryOptions options,
    CancellationToken ct = default)
{
    Console.WriteLine($"Partial recovery for {topic}");
    Console.WriteLine($"  Partitions: {string.Join(", ", options.Partitions ?? new[] { "all" })}");
    Console.WriteLine($"  Start offset: {options.StartOffset}");
    Console.WriteLine($"  End offset: {options.EndOffset}");

    var manifest = await LoadManifestAsync(topic, backupId);
    if (manifest == null)
    {
        return new RecoveryResult
        {
            Success = false,
            Error = "Manifest not found"
        };
    }

    var targetTopic = topic + _config.TopicSuffix;
    long totalRestored = 0;

    await using var producer = new SurgewaveProducer<string, string>(opts =>
    {
        opts.BootstrapServers = _config.TargetBootstrapServers;
    });

    foreach (var partition in manifest.Partitions)
    {
        // Skip if not in requested partitions
        if (options.Partitions != null &&
            !options.Partitions.Contains(partition.PartitionId))
            continue;

        var count = await RestorePartitionWithFilterAsync(
            topic, backupId, partition, targetTopic, producer, options, ct);

        totalRestored += count;
    }

    return new RecoveryResult
    {
        Success = true,
        SourceTopic = topic,
        TargetTopic = targetTopic,
        MessagesRestored = totalRestored,
        EndTime = DateTime.UtcNow
    };
}

public class PartialRecoveryOptions
{
    public int[]? Partitions { get; set; }
    public long? StartOffset { get; set; }
    public long? EndOffset { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation including:

- Complete recovery service
- Point-in-time recovery
- Consistency verification
- Partial recovery support
- Interactive demo with sample data

## Knowledge Check

1. What is the difference between full restore and point-in-time recovery?
2. Why is checksum verification important during recovery?
3. When would you use partial recovery?
4. How do you calculate the data gap after recovery?
5. What validation steps should follow a recovery?

## Expected Output

```
=== Surgewave Bootcamp: Lesson 15.4 - Data Recovery ===

Creating sample data and backup...
  Produced 100 messages to orders topic
  Backup created: 2024-01-15-10-30-45

Listing available backups...
  orders:
    - 2024-01-15-10-30-45 (100 messages, 15234 bytes)

Restoring from backup...
Loading backup manifest...
  Backup date: 2024-01-15T10:30:45Z
  Total messages: 100
Verifying backup integrity...
  Backup integrity verified
Restoring to topic: orders_recovered
  Restoring partition 0...
    Progress: 100 messages
    Restored 100 messages
Verifying restoration...
  Expected: 100, Actual: 100
  Verification: PASSED

Recovery complete!
  Duration: 00:00:02.345
  Messages restored: 100
  Target topic: orders_recovered

Generating consistency report...
  Original count: 100
  Restored count: 100
  Count match: True
  Checksum match: True
  Overall: CONSISTENT

Lesson complete!
```

---

**Next:** [Lesson 15.5: DR Testing](../lesson-5/README.md)
