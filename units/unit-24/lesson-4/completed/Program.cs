// Surgewave Bootcamp - Unit 15, Lesson 4: Data Recovery
// COMPLETED SOLUTION

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 15.4 - Data Recovery ===");
Console.WriteLine();

// Start a Surgewave runtime
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave runtime started on port {runtime.Port}");
Console.WriteLine();

var topic = "orders";
var backupDir = "./backups-recovery";

// Step 1: Create sample data and backup
Console.WriteLine("Creating sample data and backup...");
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

for (int i = 0; i < 100; i++)
{
    await producer.ProduceAsync(topic, $"order-{i}", $"Order {i} data at {DateTime.UtcNow:O}");
}
Console.WriteLine("  Produced 100 messages to orders topic");

// Create backup
var backupId = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");
var backupPath = Path.Combine(backupDir, topic, backupId);
Directory.CreateDirectory(backupPath);

await using var backupConsumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = $"backup-{Guid.NewGuid():N}";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

backupConsumer.Subscribe(topic);

var records = new List<BackupRecord>();
var emptyPolls = 0;
while (emptyPolls < 3)
{
    var msg = await backupConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
    if (msg != null)
    {
        records.Add(new BackupRecord
        {
            Offset = msg.Offset,
            Key = msg.Key ?? "",
            Value = msg.Value ?? "",
            Timestamp = DateTime.UtcNow.ToString("O")
        });
        emptyPolls = 0;
    }
    else
    {
        emptyPolls++;
    }
}

// Write backup file
var partitionFile = Path.Combine(backupPath, "partition-0.jsonl.gz");
await using (var fs = File.Create(partitionFile))
await using (var gz = new GZipStream(fs, CompressionLevel.Optimal))
await using (var writer = new StreamWriter(gz))
{
    foreach (var record in records)
    {
        await writer.WriteLineAsync(JsonSerializer.Serialize(record));
    }
}

// Write manifest
var manifest = new BackupManifest
{
    BackupId = backupId,
    Topic = topic,
    TotalMessages = records.Count,
    TotalBytes = new FileInfo(partitionFile).Length,
    Partitions = new List<PartitionBackup>
    {
        new()
        {
            PartitionId = 0,
            StartOffset = 0,
            EndOffset = records.Count,
            FileName = "partition-0.jsonl.gz",
            SizeBytes = new FileInfo(partitionFile).Length
        }
    }
};

var manifestPath = Path.Combine(backupPath, "manifest.json");
await File.WriteAllTextAsync(manifestPath,
    JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine($"  Backup created: {backupId}");
Console.WriteLine();

// Step 2: Create recovery configuration
var config = new RecoveryConfig
{
    BackupPath = backupDir,
    TargetBootstrapServers = $"localhost:{runtime.Port}",
    TopicSuffix = "_recovered",
    VerifyChecksum = true
};

// Step 3: Create recovery service
var recoveryService = new RecoveryService(config);

// Step 4: List available backups
Console.WriteLine("Listing available backups...");
foreach (var backup in recoveryService.ListAvailableBackups(topic))
{
    Console.WriteLine($"  {topic}:");
    Console.WriteLine($"    - {backup.BackupId} ({backup.MessageCount} messages, {backup.SizeBytes:N0} bytes)");
}
Console.WriteLine();

// Step 5: Restore from backup
Console.WriteLine("Restoring from backup...");
var result = await recoveryService.RestoreTopicAsync(topic, backupId);

if (result.Success)
{
    Console.WriteLine($"\nRecovery complete!");
    Console.WriteLine($"  Duration: {result.Duration}");
    Console.WriteLine($"  Messages restored: {result.MessagesRestored}");
    Console.WriteLine($"  Target topic: {result.TargetTopic}");
    Console.WriteLine($"  Verification: {(result.VerificationPassed ? "PASSED" : "FAILED")}");
}
else
{
    Console.WriteLine($"Recovery failed: {result.Error}");
}
Console.WriteLine();

// Step 6: Generate consistency report
Console.WriteLine("Generating consistency report...");
var report = await recoveryService.GenerateConsistencyReportAsync(
    topic, topic + config.TopicSuffix, runtime.Port);
Console.WriteLine($"  Original count: {report.OriginalCount}");
Console.WriteLine($"  Restored count: {report.RestoredCount}");
Console.WriteLine($"  Count match: {report.CountMatch}");
Console.WriteLine($"  Overall: {(report.IsConsistent ? "CONSISTENT" : "INCONSISTENT")}");
Console.WriteLine();

// Cleanup
try { Directory.Delete(backupDir, true); } catch { }

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Verify backup integrity before restoration");
Console.WriteLine("2. Point-in-time recovery minimizes data gap");
Console.WriteLine("3. Always validate consistency after recovery");
Console.WriteLine("4. Partial recovery is useful for specific topics");
Console.WriteLine("5. Document and test recovery procedures");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Configuration
public class RecoveryConfig
{
    public string BackupPath { get; set; } = "./backups";
    public string TargetBootstrapServers { get; set; } = "";
    public string TopicSuffix { get; set; } = "_recovered";
    public bool VerifyChecksum { get; set; } = true;
    public int BatchSize { get; set; } = 1000;
}

// Backup classes
public class BackupManifest
{
    public string BackupId { get; set; } = "";
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

public class BackupRecord
{
    public long Offset { get; set; }
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string Timestamp { get; set; } = "";
}

// Result classes
public class RecoveryResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string SourceTopic { get; set; } = "";
    public string TargetTopic { get; set; } = "";
    public string BackupId { get; set; } = "";
    public bool Success { get; set; }
    public string? Error { get; set; }
    public long MessagesRestored { get; set; }
    public bool VerificationPassed { get; set; }
    public TimeSpan? DataGap { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
}

public class BackupInfo
{
    public string BackupId { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public long MessageCount { get; set; }
    public long SizeBytes { get; set; }
}

public class ConsistencyReport
{
    public string OriginalTopic { get; set; } = "";
    public string RestoredTopic { get; set; } = "";
    public DateTime CheckedAt { get; set; }
    public long OriginalCount { get; set; }
    public long RestoredCount { get; set; }
    public bool CountMatch { get; set; }
    public string OriginalChecksum { get; set; } = "";
    public string RestoredChecksum { get; set; } = "";
    public bool ChecksumMatch { get; set; }
    public bool IsConsistent { get; set; }
}

// Recovery Service
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
            // Step 1: Load manifest
            Console.WriteLine("Loading backup manifest...");
            var manifest = await LoadManifestAsync(topic, backupId);
            if (manifest == null)
            {
                result.Success = false;
                result.Error = "Manifest not found";
                return result;
            }
            Console.WriteLine($"  Backup date: {manifest.CreatedAt:O}");
            Console.WriteLine($"  Total messages: {manifest.TotalMessages}");

            // Step 2: Restore messages
            var targetTopic = topic + _config.TopicSuffix;
            Console.WriteLine($"Restoring to topic: {targetTopic}");
            result.TargetTopic = targetTopic;

            result.MessagesRestored = await RestoreMessagesAsync(
                topic, backupId, manifest, targetTopic, ct);

            // Step 3: Verify
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

    private async Task<BackupManifest?> LoadManifestAsync(string topic, string backupId)
    {
        var manifestPath = Path.Combine(_config.BackupPath, topic, backupId, "manifest.json");
        if (!File.Exists(manifestPath))
            return null;

        var json = await File.ReadAllTextAsync(manifestPath);
        return JsonSerializer.Deserialize<BackupManifest>(json);
    }

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
        var backupPath = Path.Combine(_config.BackupPath, topic, backupId);

        foreach (var partition in manifest.Partitions)
        {
            var filePath = Path.Combine(backupPath, partition.FileName);
            Console.WriteLine($"  Restoring partition {partition.PartitionId}...");

            await using var fs = File.OpenRead(filePath);
            Stream readStream = fs;

            if (partition.FileName.EndsWith(".gz"))
                readStream = new GZipStream(fs, CompressionMode.Decompress);

            using var reader = new StreamReader(readStream);
            string? line;
            long count = 0;

            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                if (ct.IsCancellationRequested) break;

                var record = JsonSerializer.Deserialize<BackupRecord>(line);
                if (record != null)
                {
                    await producer.ProduceAsync(targetTopic, record.Key, record.Value);
                    count++;
                }
            }

            Console.WriteLine($"    Restored {count} messages");
            totalRestored += count;
        }

        return totalRestored;
    }

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
        var emptyPolls = 0;

        while (emptyPolls < 3)
        {
            var msg = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
            if (msg != null)
            {
                actualCount++;
                emptyPolls = 0;
            }
            else
            {
                emptyPolls++;
            }
        }

        Console.WriteLine($"  Expected: {expectedCount}, Actual: {actualCount}");
        var match = actualCount >= expectedCount;
        Console.WriteLine($"  Verification: {(match ? "PASSED" : "FAILED")}");
        return match;
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

    public async Task<ConsistencyReport> GenerateConsistencyReportAsync(
        string originalTopic,
        string restoredTopic,
        int port)
    {
        var report = new ConsistencyReport
        {
            OriginalTopic = originalTopic,
            RestoredTopic = restoredTopic,
            CheckedAt = DateTime.UtcNow
        };

        report.OriginalCount = await CountMessagesAsync(originalTopic, port);
        report.RestoredCount = await CountMessagesAsync(restoredTopic, port);
        report.CountMatch = report.OriginalCount == report.RestoredCount;
        report.IsConsistent = report.CountMatch;

        return report;
    }

    private async Task<long> CountMessagesAsync(string topic, int port)
    {
        await using var consumer = new SurgewaveConsumer<string, string>(options =>
        {
            options.BootstrapServers = $"localhost:{port}";
            options.GroupId = $"count-{Guid.NewGuid():N}";
            options.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        consumer.Subscribe(topic);

        long count = 0;
        var emptyPolls = 0;

        while (emptyPolls < 3)
        {
            var msg = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
            if (msg != null)
            {
                count++;
                emptyPolls = 0;
            }
            else
            {
                emptyPolls++;
            }
        }

        return count;
    }
}
