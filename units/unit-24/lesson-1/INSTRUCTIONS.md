# Lesson 15.1: Instructions

Follow these step-by-step instructions to implement a Surgewave topic backup service.

## Step 1: Create Backup Configuration

Define the backup configuration with retention policies:

```csharp
public class BackupConfiguration
{
    public string DestinationPath { get; set; } = "./backups";
    public List<string> Topics { get; set; } = new();
    public bool CompressionEnabled { get; set; } = true;
    public RetentionPolicy Retention { get; set; } = new();
}

public class RetentionPolicy
{
    public int HourlyBackups { get; set; } = 24;   // Keep 24 hourly backups
    public int DailyBackups { get; set; } = 7;     // Keep 7 daily backups
    public int WeeklyBackups { get; set; } = 4;    // Keep 4 weekly backups
    public int MonthlyBackups { get; set; } = 12;  // Keep 12 monthly backups
}
```

## Step 2: Implement Backup Manifest

Create a manifest to track backup metadata:

```csharp
public class BackupManifest
{
    public string BackupId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Topic { get; set; } = "";
    public List<PartitionBackup> Partitions { get; set; } = new();
    public long TotalMessages { get; set; }
    public long TotalBytes { get; set; }
    public string Checksum { get; set; } = "";
}

public class PartitionBackup
{
    public int PartitionId { get; set; }
    public long StartOffset { get; set; }
    public long EndOffset { get; set; }
    public string FileName { get; set; } = "";
    public string Checksum { get; set; } = "";
    public long SizeBytes { get; set; }
}
```

## Step 3: Create the Backup Service

Implement the core backup logic:

```csharp
public class TopicBackupService
{
    private readonly BackupConfiguration _config;

    public TopicBackupService(BackupConfiguration config)
    {
        _config = config;
    }

    public async Task<BackupManifest> CreateBackupAsync(
        string topic,
        SurgewaveConsumer<string, string> consumer,
        CancellationToken ct = default)
    {
        var manifest = new BackupManifest { Topic = topic };
        var backupDir = Path.Combine(
            _config.DestinationPath,
            topic,
            DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss"));

        Directory.CreateDirectory(backupDir);

        // Get partition information
        var partitions = await consumer.GetPartitionsAsync(topic);

        foreach (var partition in partitions)
        {
            var partitionBackup = await BackupPartitionAsync(
                consumer, topic, partition, backupDir, ct);
            manifest.Partitions.Add(partitionBackup);
            manifest.TotalMessages += partitionBackup.EndOffset - partitionBackup.StartOffset;
            manifest.TotalBytes += partitionBackup.SizeBytes;
        }

        // Calculate overall checksum
        manifest.Checksum = CalculateManifestChecksum(manifest);

        // Save manifest
        var manifestPath = Path.Combine(backupDir, "manifest.json");
        await File.WriteAllTextAsync(manifestPath,
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), ct);

        return manifest;
    }
}
```

## Step 4: Implement Partition Backup

Export individual partition data:

```csharp
private async Task<PartitionBackup> BackupPartitionAsync(
    SurgewaveConsumer<string, string> consumer,
    string topic,
    int partition,
    string backupDir,
    CancellationToken ct)
{
    var fileName = $"partition-{partition}.jsonl";
    if (_config.CompressionEnabled)
        fileName += ".gz";

    var filePath = Path.Combine(backupDir, fileName);
    var backup = new PartitionBackup
    {
        PartitionId = partition,
        FileName = fileName
    };

    // Get partition offsets
    var offsets = await consumer.GetOffsetsAsync(topic, partition);
    backup.StartOffset = offsets.Low;
    backup.EndOffset = offsets.High;

    // Seek to beginning
    consumer.Assign(new[] { new TopicPartitionOffset(topic, partition, offsets.Low) });

    using var hasher = SHA256.Create();
    await using var fileStream = File.Create(filePath);
    Stream writeStream = fileStream;

    if (_config.CompressionEnabled)
        writeStream = new GZipStream(fileStream, CompressionLevel.Optimal);

    await using var writer = new StreamWriter(writeStream);

    var currentOffset = offsets.Low;
    while (currentOffset < offsets.High && !ct.IsCancellationRequested)
    {
        var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5), ct);
        if (result == null) break;

        var record = JsonSerializer.Serialize(new
        {
            result.Offset,
            result.Key,
            result.Value,
            result.Timestamp
        });

        await writer.WriteLineAsync(record);
        hasher.TransformBlock(Encoding.UTF8.GetBytes(record), 0, record.Length, null, 0);

        currentOffset = result.Offset + 1;
    }

    hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
    backup.Checksum = Convert.ToHexString(hasher.Hash!);

    await writer.FlushAsync();
    await writeStream.FlushAsync();

    backup.SizeBytes = new FileInfo(filePath).Length;
    return backup;
}
```

## Step 5: Implement Retention Policy Enforcement

Clean up old backups based on retention rules:

```csharp
public async Task EnforceRetentionPolicyAsync(string topic)
{
    var topicBackupDir = Path.Combine(_config.DestinationPath, topic);
    if (!Directory.Exists(topicBackupDir))
        return;

    var backups = Directory.GetDirectories(topicBackupDir)
        .Select(d => new
        {
            Path = d,
            Timestamp = ParseBackupTimestamp(Path.GetFileName(d))
        })
        .Where(b => b.Timestamp.HasValue)
        .OrderByDescending(b => b.Timestamp)
        .ToList();

    var now = DateTime.UtcNow;
    var keepBackups = new HashSet<string>();

    // Keep hourly backups for last 24 hours
    keepBackups.UnionWith(backups
        .Where(b => b.Timestamp >= now.AddHours(-_config.Retention.HourlyBackups))
        .Select(b => b.Path));

    // Keep daily backups for last 7 days (one per day)
    for (int i = 0; i < _config.Retention.DailyBackups; i++)
    {
        var day = now.Date.AddDays(-i);
        var dailyBackup = backups
            .FirstOrDefault(b => b.Timestamp?.Date == day);
        if (dailyBackup != null)
            keepBackups.Add(dailyBackup.Path);
    }

    // Keep weekly backups (one per week)
    for (int i = 0; i < _config.Retention.WeeklyBackups; i++)
    {
        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek - (i * 7));
        var weeklyBackup = backups
            .FirstOrDefault(b => b.Timestamp?.Date >= weekStart &&
                                 b.Timestamp?.Date < weekStart.AddDays(7));
        if (weeklyBackup != null)
            keepBackups.Add(weeklyBackup.Path);
    }

    // Delete backups not in keep list
    foreach (var backup in backups.Where(b => !keepBackups.Contains(b.Path)))
    {
        Console.WriteLine($"Deleting old backup: {backup.Path}");
        Directory.Delete(backup.Path, recursive: true);
    }
}
```

## Step 6: Implement Backup Verification

Verify backup integrity:

```csharp
public async Task<bool> VerifyBackupAsync(string backupPath)
{
    var manifestPath = Path.Combine(backupPath, "manifest.json");
    if (!File.Exists(manifestPath))
    {
        Console.WriteLine("ERROR: Manifest not found");
        return false;
    }

    var manifest = JsonSerializer.Deserialize<BackupManifest>(
        await File.ReadAllTextAsync(manifestPath));

    if (manifest == null)
    {
        Console.WriteLine("ERROR: Failed to parse manifest");
        return false;
    }

    // Verify each partition file
    foreach (var partition in manifest.Partitions)
    {
        var filePath = Path.Combine(backupPath, partition.FileName);
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"ERROR: Missing partition file: {partition.FileName}");
            return false;
        }

        var actualChecksum = await CalculateFileChecksumAsync(filePath, partition.FileName.EndsWith(".gz"));
        if (actualChecksum != partition.Checksum)
        {
            Console.WriteLine($"ERROR: Checksum mismatch for {partition.FileName}");
            Console.WriteLine($"  Expected: {partition.Checksum}");
            Console.WriteLine($"  Actual:   {actualChecksum}");
            return false;
        }

        Console.WriteLine($"  Partition {partition.PartitionId}: OK ({partition.EndOffset - partition.StartOffset} messages)");
    }

    // Verify overall manifest checksum
    var expectedChecksum = CalculateManifestChecksum(manifest);
    if (expectedChecksum != manifest.Checksum)
    {
        Console.WriteLine("ERROR: Manifest checksum mismatch");
        return false;
    }

    Console.WriteLine($"Backup verified: {manifest.TotalMessages} total messages, {manifest.TotalBytes} bytes");
    return true;
}
```

## Step 7: List Available Backups

Display existing backups with metadata:

```csharp
public void ListBackups(string? topic = null)
{
    var topics = topic != null
        ? new[] { topic }
        : Directory.GetDirectories(_config.DestinationPath)
            .Select(Path.GetFileName)
            .Where(d => d != null);

    Console.WriteLine("Available Backups:");
    Console.WriteLine("==================");

    foreach (var t in topics)
    {
        var topicDir = Path.Combine(_config.DestinationPath, t!);
        if (!Directory.Exists(topicDir)) continue;

        var backups = Directory.GetDirectories(topicDir)
            .OrderByDescending(d => d)
            .ToList();

        Console.WriteLine($"\nTopic: {t}");
        Console.WriteLine($"  Backups: {backups.Count}");

        foreach (var backup in backups.Take(5))
        {
            var manifestPath = Path.Combine(backup, "manifest.json");
            if (File.Exists(manifestPath))
            {
                var manifest = JsonSerializer.Deserialize<BackupManifest>(
                    File.ReadAllText(manifestPath));
                Console.WriteLine($"    {Path.GetFileName(backup)}:");
                Console.WriteLine($"      Messages: {manifest?.TotalMessages:N0}");
                Console.WriteLine($"      Size: {manifest?.TotalBytes:N0} bytes");
            }
        }
    }
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation including:

- Complete backup service with all features
- Retention policy enforcement
- Backup verification
- Interactive demo with sample data

## Knowledge Check

1. What is the difference between full, incremental, and snapshot backups?
2. Why is the 3-2-1 backup rule important?
3. How do checksums help verify backup integrity?
4. When should you use compression for backups?
5. How do retention policies balance storage costs and recovery needs?

## Expected Output

```
=== Surgewave Bootcamp: Lesson 15.1 - Backup Strategies ===

Creating sample data in topic: backup-demo
  Produced 100 messages

Creating backup...
  Backing up partition 0: 100 messages
  Calculating checksum...
  Backup complete: ./backups/backup-demo/2024-01-15-10-30-45

Verifying backup...
  Partition 0: OK (100 messages)
Backup verified: 100 total messages, 15234 bytes

Listing backups...
Available Backups:
==================

Topic: backup-demo
  Backups: 1
    2024-01-15-10-30-45:
      Messages: 100
      Size: 15,234 bytes

Enforcing retention policy...
  No old backups to delete

Lesson complete!
```

---

**Next:** [Lesson 15.2: Cross-Datacenter Replication](../lesson-2/README.md)
