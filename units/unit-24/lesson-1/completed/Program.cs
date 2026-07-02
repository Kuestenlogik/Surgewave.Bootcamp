// Surgewave Bootcamp - Unit 15, Lesson 1: Backup Strategies
// COMPLETED SOLUTION

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 15.1 - Backup Strategies ===");
Console.WriteLine();

// Start a Surgewave runtime for testing
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave runtime started on port {runtime.Port}");
Console.WriteLine();

var topic = "backup-demo";
var backupDir = "./backups";

// Step 1: Create sample data to backup
Console.WriteLine($"Creating sample data in topic: {topic}");
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

for (int i = 0; i < 100; i++)
{
    var key = $"key-{i}";
    var value = $"Message {i} created at {DateTime.UtcNow:O}";
    await producer.ProduceAsync(topic, key, value);
}
Console.WriteLine("  Produced 100 messages");
Console.WriteLine();

// Step 2: Create backup configuration
var config = new BackupConfiguration
{
    DestinationPath = backupDir,
    Topics = new List<string> { topic },
    CompressionEnabled = true,
    Retention = new RetentionPolicy
    {
        HourlyBackups = 24,
        DailyBackups = 7,
        WeeklyBackups = 4,
        MonthlyBackups = 12
    }
};

// Step 3: Create the backup service
var backupService = new TopicBackupService(config);

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "backup-service";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

// Step 4: Create backup
Console.WriteLine("Creating backup...");
var manifest = await backupService.CreateBackupAsync(topic, consumer, runtime.Port);
var backupPath = Path.Combine(backupDir, topic, manifest.BackupId);
Console.WriteLine($"  Backup complete: {backupPath}");
Console.WriteLine($"  Messages: {manifest.TotalMessages}");
Console.WriteLine($"  Size: {manifest.TotalBytes:N0} bytes");
Console.WriteLine();

// Step 5: Verify the backup
Console.WriteLine("Verifying backup...");
var isValid = await backupService.VerifyBackupAsync(backupPath);
Console.WriteLine($"  Verification result: {(isValid ? "PASSED" : "FAILED")}");
Console.WriteLine();

// Step 6: List available backups
Console.WriteLine("Listing backups...");
backupService.ListBackups();
Console.WriteLine();

// Step 7: Enforce retention policy
Console.WriteLine("Enforcing retention policy...");
await backupService.EnforceRetentionPolicyAsync(topic);
Console.WriteLine();

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use checksums to verify backup integrity");
Console.WriteLine("2. Implement tiered retention policies (hot/warm/cold)");
Console.WriteLine("3. Follow the 3-2-1 backup rule");
Console.WriteLine("4. Regularly test backup restoration");
Console.WriteLine("5. Monitor backup job success/failure");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Configuration classes
public class BackupConfiguration
{
    public string DestinationPath { get; set; } = "./backups";
    public List<string> Topics { get; set; } = new();
    public bool CompressionEnabled { get; set; } = true;
    public RetentionPolicy Retention { get; set; } = new();
}

public class RetentionPolicy
{
    public int HourlyBackups { get; set; } = 24;
    public int DailyBackups { get; set; } = 7;
    public int WeeklyBackups { get; set; } = 4;
    public int MonthlyBackups { get; set; } = 12;
}

// Manifest classes
public class BackupManifest
{
    public string BackupId { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");
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

// Backup Service
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
        int port,
        CancellationToken ct = default)
    {
        var manifest = new BackupManifest { Topic = topic };
        var backupDir = Path.Combine(_config.DestinationPath, topic, manifest.BackupId);

        Directory.CreateDirectory(backupDir);
        Console.WriteLine($"  Backup directory: {backupDir}");

        // For this demo, we'll consume all messages from partition 0
        var partitionBackup = await BackupPartitionAsync(topic, 0, backupDir, port, ct);
        manifest.Partitions.Add(partitionBackup);
        manifest.TotalMessages = partitionBackup.EndOffset - partitionBackup.StartOffset;
        manifest.TotalBytes = partitionBackup.SizeBytes;

        // Calculate overall checksum
        manifest.Checksum = CalculateManifestChecksum(manifest);

        // Save manifest
        var manifestPath = Path.Combine(backupDir, "manifest.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, options), ct);

        return manifest;
    }

    private async Task<PartitionBackup> BackupPartitionAsync(
        string topic,
        int partition,
        string backupDir,
        int port,
        CancellationToken ct)
    {
        var fileName = $"partition-{partition}.jsonl";
        if (_config.CompressionEnabled)
            fileName += ".gz";

        var filePath = Path.Combine(backupDir, fileName);
        var backup = new PartitionBackup
        {
            PartitionId = partition,
            FileName = fileName,
            StartOffset = 0
        };

        // Create a dedicated consumer for backup
        await using var backupConsumer = new SurgewaveConsumer<string, string>(options =>
        {
            options.BootstrapServers = $"localhost:{port}";
            options.GroupId = $"backup-{Guid.NewGuid():N}";
            options.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        backupConsumer.Subscribe(topic);

        using var hasher = SHA256.Create();
        await using var fileStream = File.Create(filePath);
        Stream writeStream = fileStream;

        if (_config.CompressionEnabled)
            writeStream = new GZipStream(fileStream, CompressionLevel.Optimal, leaveOpen: true);

        await using var writer = new StreamWriter(writeStream, leaveOpen: true);

        Console.WriteLine($"  Backing up partition {partition}...");
        long messageCount = 0;
        var emptyPolls = 0;

        while (emptyPolls < 3 && !ct.IsCancellationRequested)
        {
            var result = await backupConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
            if (result == null)
            {
                emptyPolls++;
                continue;
            }

            emptyPolls = 0;
            var record = JsonSerializer.Serialize(new
            {
                result.Offset,
                result.Key,
                result.Value,
                Timestamp = DateTime.UtcNow.ToString("O")
            });

            await writer.WriteLineAsync(record);
            var bytes = Encoding.UTF8.GetBytes(record + "\n");
            hasher.TransformBlock(bytes, 0, bytes.Length, null, 0);
            messageCount++;
            backup.EndOffset = result.Offset + 1;
        }

        hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        backup.Checksum = Convert.ToHexString(hasher.Hash!);

        await writer.FlushAsync();
        if (writeStream != fileStream)
        {
            await writeStream.FlushAsync();
            await writeStream.DisposeAsync();
        }

        backup.SizeBytes = new FileInfo(filePath).Length;
        Console.WriteLine($"  Backed up {messageCount} messages ({backup.SizeBytes:N0} bytes)");

        return backup;
    }

    private string CalculateManifestChecksum(BackupManifest manifest)
    {
        var content = $"{manifest.Topic}|{manifest.TotalMessages}|{manifest.TotalBytes}|" +
                      string.Join(",", manifest.Partitions.Select(p => p.Checksum));
        using var hasher = SHA256.Create();
        return Convert.ToHexString(hasher.ComputeHash(Encoding.UTF8.GetBytes(content)));
    }

    public async Task<bool> VerifyBackupAsync(string backupPath)
    {
        var manifestPath = Path.Combine(backupPath, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            Console.WriteLine("  ERROR: Manifest not found");
            return false;
        }

        var manifest = JsonSerializer.Deserialize<BackupManifest>(
            await File.ReadAllTextAsync(manifestPath));

        if (manifest == null)
        {
            Console.WriteLine("  ERROR: Failed to parse manifest");
            return false;
        }

        // Verify each partition file
        foreach (var partition in manifest.Partitions)
        {
            var filePath = Path.Combine(backupPath, partition.FileName);
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"  ERROR: Missing partition file: {partition.FileName}");
                return false;
            }

            var actualChecksum = await CalculateFileChecksumAsync(filePath, partition.FileName.EndsWith(".gz"));
            if (actualChecksum != partition.Checksum)
            {
                Console.WriteLine($"  ERROR: Checksum mismatch for {partition.FileName}");
                Console.WriteLine($"    Expected: {partition.Checksum}");
                Console.WriteLine($"    Actual:   {actualChecksum}");
                return false;
            }

            Console.WriteLine($"  Partition {partition.PartitionId}: OK ({partition.EndOffset - partition.StartOffset} messages)");
        }

        // Verify overall manifest checksum
        var expectedChecksum = CalculateManifestChecksum(manifest);
        if (expectedChecksum != manifest.Checksum)
        {
            Console.WriteLine("  ERROR: Manifest checksum mismatch");
            return false;
        }

        Console.WriteLine($"  Total: {manifest.TotalMessages} messages, {manifest.TotalBytes:N0} bytes");
        return true;
    }

    private async Task<string> CalculateFileChecksumAsync(string filePath, bool isCompressed)
    {
        using var hasher = SHA256.Create();
        await using var fileStream = File.OpenRead(filePath);
        Stream readStream = fileStream;

        if (isCompressed)
            readStream = new GZipStream(fileStream, CompressionMode.Decompress);

        using var reader = new StreamReader(readStream);
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            var bytes = Encoding.UTF8.GetBytes(line + "\n");
            hasher.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }

        hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(hasher.Hash!);
    }

    public void ListBackups(string? topic = null)
    {
        if (!Directory.Exists(_config.DestinationPath))
        {
            Console.WriteLine("  No backups found");
            return;
        }

        var topics = topic != null
            ? new[] { topic }
            : Directory.GetDirectories(_config.DestinationPath)
                .Select(Path.GetFileName)
                .Where(d => d != null)
                .Cast<string>();

        Console.WriteLine("  Available Backups:");
        Console.WriteLine("  ==================");

        foreach (var t in topics)
        {
            var topicDir = Path.Combine(_config.DestinationPath, t);
            if (!Directory.Exists(topicDir)) continue;

            var backups = Directory.GetDirectories(topicDir)
                .OrderByDescending(d => d)
                .ToList();

            Console.WriteLine($"\n  Topic: {t}");
            Console.WriteLine($"    Backups: {backups.Count}");

            foreach (var backup in backups.Take(5))
            {
                var manifestPath = Path.Combine(backup, "manifest.json");
                if (File.Exists(manifestPath))
                {
                    var manifest = JsonSerializer.Deserialize<BackupManifest>(
                        File.ReadAllText(manifestPath));
                    Console.WriteLine($"      {Path.GetFileName(backup)}:");
                    Console.WriteLine($"        Messages: {manifest?.TotalMessages:N0}");
                    Console.WriteLine($"        Size: {manifest?.TotalBytes:N0} bytes");
                }
            }
        }
    }

    public Task EnforceRetentionPolicyAsync(string topic)
    {
        var topicBackupDir = Path.Combine(_config.DestinationPath, topic);
        if (!Directory.Exists(topicBackupDir))
        {
            Console.WriteLine("  No backups to clean up");
            return Task.CompletedTask;
        }

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

        // Keep hourly backups
        keepBackups.UnionWith(backups
            .Where(b => b.Timestamp >= now.AddHours(-_config.Retention.HourlyBackups))
            .Select(b => b.Path));

        // Keep daily backups
        for (int i = 0; i < _config.Retention.DailyBackups; i++)
        {
            var day = now.Date.AddDays(-i);
            var dailyBackup = backups.FirstOrDefault(b => b.Timestamp?.Date == day);
            if (dailyBackup != null)
                keepBackups.Add(dailyBackup.Path);
        }

        // Keep weekly backups
        for (int i = 0; i < _config.Retention.WeeklyBackups; i++)
        {
            var weekStart = now.Date.AddDays(-(int)now.DayOfWeek - (i * 7));
            var weeklyBackup = backups.FirstOrDefault(b =>
                b.Timestamp?.Date >= weekStart &&
                b.Timestamp?.Date < weekStart.AddDays(7));
            if (weeklyBackup != null)
                keepBackups.Add(weeklyBackup.Path);
        }

        var toDelete = backups.Where(b => !keepBackups.Contains(b.Path)).ToList();
        if (toDelete.Count == 0)
        {
            Console.WriteLine("  No old backups to delete");
            return Task.CompletedTask;
        }

        foreach (var backup in toDelete)
        {
            Console.WriteLine($"  Deleting old backup: {Path.GetFileName(backup.Path)}");
            Directory.Delete(backup.Path, recursive: true);
        }

        return Task.CompletedTask;
    }

    private DateTime? ParseBackupTimestamp(string? dirName)
    {
        if (string.IsNullOrEmpty(dirName)) return null;

        if (DateTime.TryParseExact(dirName, "yyyy-MM-dd-HH-mm-ss",
            null, System.Globalization.DateTimeStyles.AssumeUniversal, out var timestamp))
        {
            return timestamp;
        }

        return null;
    }
}
