# Knowledge Assessment: Lesson 15.1 - Backup Strategies

## Multiple Choice

### 1. What is the purpose of the 3-2-1 backup rule?
- [ ] A) 3 backups per day, 2 locations, 1 format
- [ ] B) 3 copies of data, 2 different media types, 1 copy offsite
- [ ] C) 3 retention tiers, 2 encryption keys, 1 admin
- [ ] D) 3 partitions, 2 replicas, 1 broker

<details>
<summary>Answer</summary>
**B)** The 3-2-1 rule recommends keeping 3 copies of data, on 2 different types of media/storage, with 1 copy stored offsite. This provides redundancy against various failure scenarios.
</details>

### 2. What is the difference between a full backup and an incremental backup?
- [ ] A) Full backups are faster
- [ ] B) Full backups contain all data; incremental backups contain only changes since the last backup
- [ ] C) Incremental backups require more storage
- [ ] D) They are the same thing

<details>
<summary>Answer</summary>
**B)** Full backups contain a complete copy of all data, while incremental backups contain only the data that has changed since the last backup. Incremental backups are faster and smaller but require the previous backups to restore.
</details>

### 3. Which backup tier is most appropriate for quick recovery from recent data loss?
- [ ] A) Cold/Glacier (monthly archives)
- [ ] B) Hot (hourly backups)
- [ ] C) Archive (yearly backups)
- [ ] D) All tiers are equally appropriate

<details>
<summary>Answer</summary>
**B)** Hot tier backups (hourly frequency, 24-hour retention) are designed for quick recovery scenarios. They use standard storage class for fast access and have the most recent data.
</details>

## True/False

### 4. Backup checksums should be calculated during backup creation and verified during restoration.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Calculating checksums (like SHA-256) during backup creation and verifying them during restoration ensures data integrity. This detects corruption that may have occurred during storage or transfer.
</details>

### 5. Backups should include all topics regardless of their importance to ensure complete recovery.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Backup strategies should prioritize critical topics. Including all topics may be unnecessary, expensive, and slow. A selective approach (IncludeTopics configuration) focuses resources on important data while excluding transient or reproducible data.
</details>

## Code Challenge

### 6. What issues exist in this backup configuration?

```csharp
var backupConfig = new BackupConfiguration
{
    DestinationPath = "C:\\backups",
    Schedule = "0 2 * * *",
    IncludeTopics = new[] { "*" },
    CompressionEnabled = false
};
```

<details>
<summary>Answer</summary>
Issues:

1. **Local path only**: `C:\backups` provides no offsite protection; should use cloud storage (S3, Azure Blob, GCS)
2. **No retention policy**: Backups will accumulate indefinitely, consuming disk space
3. **Wildcard topics**: `*` includes all topics, including temporary ones; should be selective
4. **Compression disabled**: Increases storage costs and backup time
5. **Single backup tier**: No differentiation between hot/warm/cold retention

Improved configuration:
```csharp
var backupConfig = new BackupConfiguration
{
    DestinationPath = "s3://company-surgewave-backups/production/",
    RetentionPolicy = new RetentionPolicy
    {
        HourlyBackups = 24,      // Keep 24 hourly backups
        DailyBackups = 7,        // Keep 7 daily backups
        WeeklyBackups = 4,       // Keep 4 weekly backups
        MonthlyBackups = 12      // Keep 12 monthly backups
    },
    Schedule = "0 * * * *",      // Hourly
    IncludeTopics = new[] { "orders", "payments", "customers", "inventory" },
    ExcludeTopics = new[] { "*.tmp", "*.test" },
    CompressionEnabled = true,
    CompressionLevel = CompressionLevel.Optimal,
    EncryptionEnabled = true,
    ChecksumAlgorithm = "SHA256",
    ParallelPartitionBackup = true
};
```
</details>

### 7. Implement a backup verification method that checks message counts and checksums:

```csharp
public class BackupVerifier
{
    public async Task<VerificationResult> VerifyBackupAsync(
        string backupPath,
        BackupManifest manifest)
    {
        // Implement verification logic
    }
}
```

<details>
<summary>Answer</summary>
```csharp
public class BackupVerifier
{
    private readonly ILogger<BackupVerifier> _logger;
    private readonly IStorageClient _storage;

    public BackupVerifier(ILogger<BackupVerifier> logger, IStorageClient storage)
    {
        _logger = logger;
        _storage = storage;
    }

    public async Task<VerificationResult> VerifyBackupAsync(
        string backupPath,
        BackupManifest manifest)
    {
        var result = new VerificationResult
        {
            BackupPath = backupPath,
            ManifestTimestamp = manifest.Timestamp,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // 1. Verify all partition files exist
            foreach (var partition in manifest.Partitions)
            {
                var filePath = Path.Combine(backupPath, partition.FileName);
                if (!await _storage.ExistsAsync(filePath))
                {
                    result.Errors.Add($"Missing file: {partition.FileName}");
                    continue;
                }

                // 2. Verify file checksum
                var actualChecksum = await CalculateChecksumAsync(filePath);
                if (actualChecksum != partition.Checksum)
                {
                    result.Errors.Add(
                        $"Checksum mismatch for {partition.FileName}: " +
                        $"expected {partition.Checksum}, got {actualChecksum}");
                    continue;
                }

                // 3. Verify message count
                var messageCount = await CountMessagesAsync(filePath);
                if (messageCount != partition.MessageCount)
                {
                    result.Warnings.Add(
                        $"Message count mismatch for {partition.FileName}: " +
                        $"expected {partition.MessageCount}, got {messageCount}");
                }

                result.PartitionsVerified++;
                result.MessagesVerified += messageCount;
            }

            // 4. Verify total counts
            if (result.MessagesVerified != manifest.TotalMessageCount)
            {
                result.Warnings.Add(
                    $"Total message count mismatch: " +
                    $"expected {manifest.TotalMessageCount}, got {result.MessagesVerified}");
            }

            result.EndTime = DateTime.UtcNow;
            result.Success = result.Errors.Count == 0;

            _logger.LogInformation(
                "Backup verification {Result}: {Partitions} partitions, {Messages} messages, {Errors} errors",
                result.Success ? "PASSED" : "FAILED",
                result.PartitionsVerified,
                result.MessagesVerified,
                result.Errors.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup verification failed");
            result.Errors.Add($"Verification exception: {ex.Message}");
            result.Success = false;
            result.EndTime = DateTime.UtcNow;
            return result;
        }
    }

    private async Task<string> CalculateChecksumAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        await using var stream = await _storage.OpenReadAsync(filePath);
        var hash = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private async Task<long> CountMessagesAsync(string filePath)
    {
        long count = 0;
        await using var stream = await _storage.OpenReadAsync(filePath);
        await using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);

        while (await reader.ReadLineAsync() != null)
            count++;

        return count;
    }
}

public class VerificationResult
{
    public string BackupPath { get; set; } = "";
    public DateTime ManifestTimestamp { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public int PartitionsVerified { get; set; }
    public long MessagesVerified { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
```
</details>

## Score
- 7/7: Excellent! You understand backup strategies for Surgewave
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding
