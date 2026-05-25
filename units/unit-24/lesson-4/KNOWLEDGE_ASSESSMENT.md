# Knowledge Assessment: Lesson 15.4 - Data Recovery

## Multiple Choice

### 1. What is point-in-time recovery?
- [ ] A) Recovering data from the most recent backup only
- [ ] B) Restoring data to a specific moment in time, potentially combining backups with incremental logs
- [ ] C) Recovering data in real-time as it's written
- [ ] D) Recovering only the latest message

<details>
<summary>Answer</summary>
**B)** Point-in-time recovery restores data to a specific timestamp by combining a base backup with incremental changes (from replication logs or incremental backups) up to the target time. This provides flexibility in choosing exactly when to recover to.
</details>

### 2. How is data consistency verified after restoration?
- [ ] A) By checking the file sizes only
- [ ] B) By comparing message counts and checksums between backup manifest and restored data
- [ ] C) By checking if the service starts
- [ ] D) By waiting 24 hours

<details>
<summary>Answer</summary>
**B)** Consistency verification compares the backup manifest (which records message counts and checksums) against the actual restored data. Both counts and checksums must match to confirm successful restoration without corruption or data loss.
</details>

### 3. When would you use partial recovery instead of full recovery?
- [ ] A) When you want to recover all topics
- [ ] B) When you only need to recover specific topics and want faster recovery time
- [ ] C) When the backup is corrupted
- [ ] D) Partial recovery is never recommended

<details>
<summary>Answer</summary>
**B)** Partial recovery is appropriate when you only need specific topics restored, reducing recovery time and resource usage. For example, if only one topic was corrupted, there's no need to restore all topics.
</details>

## True/False

### 4. Restored topics should use the same name as the original to maintain consumer compatibility.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - It's often better to restore to a differently named topic (e.g., `orders_recovered`) first, verify the restoration, then either swap names or reconfigure consumers. This prevents accidentally overwriting data or serving potentially incorrect restored data.
</details>

### 5. Testing backup restoration regularly is as important as creating backups.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - An untested backup may fail when you need it most. Regular restoration tests (quarterly or more often) verify that backups are valid, the restoration process works, and the team is trained on the procedures.
</details>

## Code Challenge

### 6. What issues exist in this recovery service?

```csharp
public class RecoveryService
{
    public async Task RestoreAsync(string backupPath, string targetTopic)
    {
        var files = Directory.GetFiles(backupPath, "*.gz");
        foreach (var file in files)
        {
            var messages = ReadMessages(file);
            foreach (var msg in messages)
            {
                await _producer.ProduceAsync(targetTopic, msg.Key, msg.Value);
            }
        }
    }
}
```

<details>
<summary>Answer</summary>
Issues:

1. **No manifest verification**: Doesn't check manifest for expected files/counts
2. **No checksum validation**: Doesn't verify data integrity before restoring
3. **Sequential, slow**: Processes files and messages one at a time
4. **No progress tracking**: No way to know restoration status
5. **No error handling**: Failures will leave partial restoration
6. **No transaction/atomicity**: Partial failure leaves inconsistent state
7. **Produces message by message**: Inefficient, should batch
8. **No partition preservation**: Original partition assignments are lost

Improved version:
```csharp
public class RecoveryService
{
    private readonly ILogger<RecoveryService> _logger;
    private readonly IStorageClient _storage;
    private readonly SurgewaveProducer<string, string> _producer;

    public async Task<RecoveryResult> RestoreAsync(
        string backupPath,
        string targetTopic,
        RecoveryOptions options)
    {
        var result = new RecoveryResult { StartTime = DateTime.UtcNow };

        try
        {
            // 1. Load and verify manifest
            _logger.LogInformation("Loading manifest from {Path}", backupPath);
            var manifest = await LoadManifestAsync(backupPath);
            result.ExpectedMessageCount = manifest.TotalMessageCount;

            // 2. Verify backup integrity before restoring
            _logger.LogInformation("Verifying backup integrity...");
            foreach (var partition in manifest.Partitions)
            {
                var filePath = Path.Combine(backupPath, partition.FileName);
                var actualChecksum = await CalculateChecksumAsync(filePath);

                if (actualChecksum != partition.Checksum)
                {
                    throw new RecoveryException(
                        $"Checksum mismatch for {partition.FileName}");
                }
            }
            _logger.LogInformation("Backup integrity verified");

            // 3. Create temporary topic for restoration
            var tempTopic = $"{targetTopic}_recovery_{DateTime.UtcNow:yyyyMMddHHmmss}";
            await CreateTopicAsync(tempTopic, manifest.PartitionCount);
            result.RestoredToTopic = tempTopic;

            // 4. Restore partitions in parallel
            _logger.LogInformation("Restoring {Count} partitions...", manifest.Partitions.Count);
            var tasks = manifest.Partitions.Select(p =>
                RestorePartitionAsync(backupPath, p, tempTopic, result));
            await Task.WhenAll(tasks);

            // 5. Verify restoration
            _logger.LogInformation("Verifying restoration...");
            var restoredCount = await GetMessageCountAsync(tempTopic);
            if (restoredCount != manifest.TotalMessageCount)
            {
                throw new RecoveryException(
                    $"Message count mismatch: expected {manifest.TotalMessageCount}, got {restoredCount}");
            }

            // 6. Optionally rename to target topic
            if (options.ReplaceOriginal)
            {
                await DeleteTopicAsync(targetTopic);
                await RenameTopicAsync(tempTopic, targetTopic);
                result.RestoredToTopic = targetTopic;
            }

            result.Success = true;
            result.ActualMessageCount = restoredCount;
            result.EndTime = DateTime.UtcNow;

            _logger.LogInformation(
                "Recovery completed: {Count} messages restored in {Duration}",
                restoredCount, result.Duration);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recovery failed");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;
            return result;
        }
    }

    private async Task RestorePartitionAsync(
        string backupPath,
        PartitionManifest partition,
        string targetTopic,
        RecoveryResult result)
    {
        var filePath = Path.Combine(backupPath, partition.FileName);
        await using var stream = await _storage.OpenReadAsync(filePath);
        await using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);

        var batch = new List<(string Key, string Value)>();
        var batchSize = 1000;

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            var msg = JsonSerializer.Deserialize<BackupMessage>(line);
            batch.Add((msg!.Key, msg.Value));

            if (batch.Count >= batchSize)
            {
                await ProduceBatchAsync(targetTopic, partition.PartitionId, batch);
                Interlocked.Add(ref result._restoredCount, batch.Count);
                batch.Clear();
            }
        }

        // Final batch
        if (batch.Count > 0)
        {
            await ProduceBatchAsync(targetTopic, partition.PartitionId, batch);
            Interlocked.Add(ref result._restoredCount, batch.Count);
        }
    }
}
```
</details>

### 7. Design a recovery validation checklist that the system should verify after restoration:

<details>
<summary>Answer</summary>
```csharp
public class RecoveryValidator
{
    public async Task<ValidationReport> ValidateRestorationAsync(
        BackupManifest manifest,
        string restoredTopic)
    {
        var report = new ValidationReport
        {
            Topic = restoredTopic,
            Timestamp = DateTime.UtcNow,
            Checks = new List<ValidationCheck>()
        };

        // 1. Topic exists
        var topicExists = await CheckTopicExistsAsync(restoredTopic);
        report.Checks.Add(new ValidationCheck
        {
            Name = "Topic Exists",
            Passed = topicExists,
            Details = topicExists ? "Topic found" : "Topic not found"
        });

        if (!topicExists)
        {
            report.OverallResult = ValidationResult.Failed;
            return report;
        }

        // 2. Partition count matches
        var partitionCount = await GetPartitionCountAsync(restoredTopic);
        var partitionsMatch = partitionCount == manifest.PartitionCount;
        report.Checks.Add(new ValidationCheck
        {
            Name = "Partition Count",
            Passed = partitionsMatch,
            Expected = manifest.PartitionCount.ToString(),
            Actual = partitionCount.ToString()
        });

        // 3. Total message count matches
        var messageCount = await GetTotalMessageCountAsync(restoredTopic);
        var countMatches = messageCount == manifest.TotalMessageCount;
        report.Checks.Add(new ValidationCheck
        {
            Name = "Total Message Count",
            Passed = countMatches,
            Expected = manifest.TotalMessageCount.ToString(),
            Actual = messageCount.ToString()
        });

        // 4. Per-partition message counts
        foreach (var partition in manifest.Partitions)
        {
            var partitionCount = await GetPartitionMessageCountAsync(
                restoredTopic, partition.PartitionId);
            var matches = partitionCount == partition.MessageCount;

            report.Checks.Add(new ValidationCheck
            {
                Name = $"Partition {partition.PartitionId} Count",
                Passed = matches,
                Expected = partition.MessageCount.ToString(),
                Actual = partitionCount.ToString()
            });
        }

        // 5. Sample message validation (spot check)
        var sampleValid = await ValidateSampleMessagesAsync(restoredTopic, manifest);
        report.Checks.Add(new ValidationCheck
        {
            Name = "Sample Message Validation",
            Passed = sampleValid,
            Details = sampleValid ? "Random samples validated" : "Sample mismatch detected"
        });

        // 6. Consumer can read from start
        var readable = await TestConsumerReadAsync(restoredTopic);
        report.Checks.Add(new ValidationCheck
        {
            Name = "Consumer Readability",
            Passed = readable,
            Details = readable ? "Consumer can read from beginning" : "Consumer read failed"
        });

        // 7. Timestamp range validation
        var (minTs, maxTs) = await GetTimestampRangeAsync(restoredTopic);
        var timestampValid = minTs >= manifest.StartTimestamp &&
                            maxTs <= manifest.EndTimestamp;
        report.Checks.Add(new ValidationCheck
        {
            Name = "Timestamp Range",
            Passed = timestampValid,
            Expected = $"{manifest.StartTimestamp} - {manifest.EndTimestamp}",
            Actual = $"{minTs} - {maxTs}"
        });

        // Overall result
        report.OverallResult = report.Checks.All(c => c.Passed)
            ? ValidationResult.Passed
            : report.Checks.Any(c => !c.Passed && c.Name.Contains("Count"))
                ? ValidationResult.Failed
                : ValidationResult.PassedWithWarnings;

        return report;
    }
}

public class ValidationReport
{
    public string Topic { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public List<ValidationCheck> Checks { get; set; } = new();
    public ValidationResult OverallResult { get; set; }

    public void PrintReport()
    {
        Console.WriteLine($"Recovery Validation Report for {Topic}");
        Console.WriteLine($"Timestamp: {Timestamp}");
        Console.WriteLine(new string('-', 60));

        foreach (var check in Checks)
        {
            var status = check.Passed ? "PASS" : "FAIL";
            Console.WriteLine($"[{status}] {check.Name}");
            if (check.Expected != null)
                Console.WriteLine($"       Expected: {check.Expected}");
            if (check.Actual != null)
                Console.WriteLine($"       Actual:   {check.Actual}");
            if (check.Details != null)
                Console.WriteLine($"       {check.Details}");
        }

        Console.WriteLine(new string('-', 60));
        Console.WriteLine($"Overall: {OverallResult}");
    }
}

public class ValidationCheck
{
    public string Name { get; set; } = "";
    public bool Passed { get; set; }
    public string? Expected { get; set; }
    public string? Actual { get; set; }
    public string? Details { get; set; }
}

public enum ValidationResult { Passed, PassedWithWarnings, Failed }
```
</details>

## Score
- 7/7: Excellent! You understand data recovery procedures
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding
