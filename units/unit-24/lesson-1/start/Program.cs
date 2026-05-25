// Surgewave Bootcamp - Unit 15, Lesson 1: Backup Strategies
//
// In this lesson, you'll learn:
// - Create topic snapshots and full backups
// - Configure retention policies for different backup tiers
// - Verify backup integrity with checksums
// - Manage backup storage lifecycle
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 15.1 - Backup Strategies ===");
Console.WriteLine();

// TODO: Step 1 - Define backup configuration classes
// public class BackupConfiguration { ... }
// public class RetentionPolicy { ... }

// TODO: Step 2 - Define backup manifest classes
// public class BackupManifest { ... }
// public class PartitionBackup { ... }

// Start a Surgewave runtime for testing
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave runtime started on port {runtime.Port}");
Console.WriteLine();

var _topic = "backup-demo";
var _backupDir = "./backups";
_ = _topic; // Used in completed solution
_ = _backupDir; // Used in completed solution

// TODO: Step 3 - Create sample data to backup
// Console.WriteLine($"Creating sample data in topic: {topic}");
// await using var producer = new SurgewaveProducer<string, string>(options =>
// {
//     options.BootstrapServers = $"localhost:{runtime.Port}";
// });
//
// for (int i = 0; i < 100; i++)
// {
//     var key = $"key-{i}";
//     var value = $"Message {i} created at {DateTime.UtcNow:O}";
//     await producer.ProduceAsync(topic, key, value);
// }
// Console.WriteLine("  Produced 100 messages");
// Console.WriteLine();

// TODO: Step 4 - Create backup configuration
// var config = new BackupConfiguration
// {
//     DestinationPath = backupDir,
//     Topics = new List<string> { topic },
//     CompressionEnabled = true,
//     Retention = new RetentionPolicy
//     {
//         HourlyBackups = 24,
//         DailyBackups = 7,
//         WeeklyBackups = 4,
//         MonthlyBackups = 12
//     }
// };

// TODO: Step 5 - Create the backup service and perform backup
// var backupService = new TopicBackupService(config);
//
// await using var consumer = new SurgewaveConsumer<string, string>(options =>
// {
//     options.BootstrapServers = $"localhost:{runtime.Port}";
//     options.GroupId = "backup-service";
//     options.AutoOffsetReset = AutoOffsetReset.Earliest;
// });
//
// Console.WriteLine("Creating backup...");
// var manifest = await backupService.CreateBackupAsync(topic, consumer);
// Console.WriteLine($"  Backup complete: {manifest.BackupId}");
// Console.WriteLine();

// TODO: Step 6 - Verify the backup
// Console.WriteLine("Verifying backup...");
// var backupPath = Path.Combine(backupDir, topic, /* timestamp directory */);
// var isValid = await backupService.VerifyBackupAsync(backupPath);
// Console.WriteLine($"  Verification result: {(isValid ? "PASSED" : "FAILED")}");
// Console.WriteLine();

// TODO: Step 7 - List available backups
// Console.WriteLine("Listing backups...");
// backupService.ListBackups();
// Console.WriteLine();

// TODO: Step 8 - Enforce retention policy
// Console.WriteLine("Enforcing retention policy...");
// await backupService.EnforceRetentionPolicyAsync(topic);
// Console.WriteLine();

Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use checksums to verify backup integrity");
Console.WriteLine("2. Implement tiered retention policies (hot/warm/cold)");
Console.WriteLine("3. Follow the 3-2-1 backup rule");
Console.WriteLine("4. Regularly test backup restoration");
Console.WriteLine("5. Monitor backup job success/failure");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// TODO: Implement TopicBackupService class
// public class TopicBackupService
// {
//     private readonly BackupConfiguration _config;
//
//     public TopicBackupService(BackupConfiguration config)
//     {
//         _config = config;
//     }
//
//     public async Task<BackupManifest> CreateBackupAsync(...) { ... }
//     public async Task<bool> VerifyBackupAsync(...) { ... }
//     public void ListBackups(...) { ... }
//     public async Task EnforceRetentionPolicyAsync(...) { ... }
// }
