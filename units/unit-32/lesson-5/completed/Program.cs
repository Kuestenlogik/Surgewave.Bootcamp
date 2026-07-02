// Surgewave Bootcamp - Unit 27, Lesson 5: Hybrid On-Premise and Cloud
// COMPLETED SOLUTION

using System.Diagnostics;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 27.5 - Hybrid On-Premise and Cloud ===");
Console.WriteLine();

// Step 1 - Set up on-premise and cloud clusters
Console.WriteLine("--- Cluster Setup ---");

var onPremise = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var cloud = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"On-Premise cluster: port {onPremise.Port}");
Console.WriteLine($"Cloud cluster: port {cloud.Port}");

// Step 2 - Set up data classification rules
Console.WriteLine("\n--- Data Classification ---");

var classificationRules = new List<DataClassification>
{
    new("orders.", DataSensitivity.Confidential, true, new[] { "on-premise" }),
    new("payments.", DataSensitivity.Restricted, true, new[] { "on-premise" }),
    new("metrics.", DataSensitivity.Public, false, new[] { "on-premise", "cloud" }),
    new("analytics.", DataSensitivity.Public, false, new[] { "on-premise", "cloud" }),
    new("users.", DataSensitivity.Internal, false, new[] { "on-premise", "cloud" }),
};

var classifier = new DataClassifier(classificationRules);

foreach (var rule in classificationRules)
{
    var destination = rule.Sensitivity <= DataSensitivity.Internal
        ? "Cloud + On-Prem" + (rule.Sensitivity == DataSensitivity.Internal ? " (Anonymized)" : "")
        : "On-Prem Only" + (rule.RequiresEncryption ? " (Encrypted)" : "");
    Console.WriteLine($"{rule.TopicPattern + "*",-18} {rule.Sensitivity,-15} -> {destination}");
}

// Step 3 - Create producers for both environments
await using var onPremProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = onPremise.BootstrapServers;
});

await using var cloudProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = cloud.BootstrapServers;
});

var forwardAgent = new StoreAndForwardAgent(maxBufferSize: 1000);
var router = new HybridRouter(onPremProducer, cloudProducer, classifier, forwardAgent);
var auditLog = new AuditLogger();

// Step 4 - Data routing simulation
Console.WriteLine("\n--- Data Routing ---");

var testMessages = new[]
{
    ("orders.created", "ord-001", "Order #12345 for customer John Doe, $299.99"),
    ("metrics.cpu", "srv-001", "cpu_usage: 75%, memory: 62%"),
    ("users.activity", "usr-001", "User john.doe@example.com logged in from 192.168.1.100"),
    ("payments.processed", "pay-001", "CC: 4111-1111-1111-1111, Amount: $299.99"),
    ("analytics.pageview", "page-001", "Page: /dashboard, Views: 1500, Unique: 890"),
};

foreach (var (topic, key, value) in testMessages)
{
    var classification = classifier.Classify(topic);
    var (onPremSent, cloudSent) = await router.RouteMessageAsync(topic, key, value);

    var destination = cloudSent ? "Cloud + On-Prem" : "On-Prem Only";
    Console.WriteLine($"[{classification.Sensitivity}] {topic} -> {destination}");
    auditLog.Log(topic, destination, classification.Sensitivity.ToString());
}

// Step 5 - Store-and-forward test
Console.WriteLine("\n--- Store-and-Forward Test ---");

Console.WriteLine("Simulating network partition...");
forwardAgent.SimulatePartition(true);

var partitionMessages = 50;
for (int i = 0; i < partitionMessages; i++)
{
    await forwardAgent.SendToCloudAsync(
        "metrics.buffered", $"key-{i}", $"value-{i}", cloudProducer);
}
Console.WriteLine($"Buffered {forwardAgent.BufferedCount} messages during partition");

Console.WriteLine("Connectivity restored - flushing buffer...");
forwardAgent.SimulatePartition(false);
var flushed = await forwardAgent.FlushBufferAsync(cloudProducer);
Console.WriteLine($"Flushed {flushed} messages to cloud");
Console.WriteLine($"Remaining in buffer: {forwardAgent.BufferedCount}");

// Step 6 - Cloud bursting simulation
Console.WriteLine("\n--- Cloud Bursting ---");

var burstController = new CloudBurstController(onPremCapacity: 10_000, burstThreshold: 0.8);

var loadLevels = new[] { 6_000, 8_500, 12_000, 15_000, 9_000, 5_000 };
foreach (var load in loadLevels)
{
    var shouldBurst = burstController.ShouldBurst(load);
    var capacityPct = (double)load / 10_000 * 100;

    if (shouldBurst)
    {
        var overflow = Math.Max(0, load - 10_000);
        Console.WriteLine($"Load: {load,6:N0} msg/s ({capacityPct:F0}%) - " +
            $"Cloud handling overflow ({overflow:N0} msg/s)");
    }
    else
    {
        Console.WriteLine($"Load: {load,6:N0} msg/s ({capacityPct:F0}%) - " +
            $"On-Premise handles");
    }
}

// Step 7 - Audit report
Console.WriteLine("\n--- Compliance Audit Report ---");
auditLog.PrintReport();

// Cleanup
await onPremise.DisposeAsync();
await cloud.DisposeAsync();

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Classify data by sensitivity before routing");
Console.WriteLine("2. Keep confidential/restricted data on-premise");
Console.WriteLine("3. Use store-and-forward for network partition resilience");
Console.WriteLine("4. Cloud bursting provides elastic scaling for peak loads");
Console.WriteLine("5. Always audit cross-boundary data movement");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ========== SUPPORTING TYPES ==========

public enum DataSensitivity
{
    Public,
    Internal,
    Confidential,
    Restricted
}

public record DataClassification(
    string TopicPattern,
    DataSensitivity Sensitivity,
    bool RequiresEncryption,
    string[] AllowedRegions
);

public record PendingMessage(string Topic, string Key, string Value, DateTime BufferedAt);

public class DataClassifier
{
    private readonly List<DataClassification> _rules;

    public DataClassifier(List<DataClassification> rules) => _rules = rules;

    public DataClassification Classify(string topic)
    {
        return _rules.FirstOrDefault(r => topic.StartsWith(r.TopicPattern))
            ?? new DataClassification("*", DataSensitivity.Confidential, true, new[] { "on-premise" });
    }
}

public class HybridRouter
{
    private readonly SurgewaveProducer<string, string> _onPremProducer;
    private readonly SurgewaveProducer<string, string> _cloudProducer;
    private readonly DataClassifier _classifier;
    private readonly StoreAndForwardAgent _forwardAgent;

    public HybridRouter(
        SurgewaveProducer<string, string> onPremProducer,
        SurgewaveProducer<string, string> cloudProducer,
        DataClassifier classifier,
        StoreAndForwardAgent forwardAgent)
    {
        _onPremProducer = onPremProducer;
        _cloudProducer = cloudProducer;
        _classifier = classifier;
        _forwardAgent = forwardAgent;
    }

    public async Task<(bool OnPremSent, bool CloudSent)> RouteMessageAsync(
        string topic, string key, string value)
    {
        var classification = _classifier.Classify(topic);

        // Always store on-premise
        await _onPremProducer.ProduceAsync(topic, key, value);

        // Route to cloud based on classification
        switch (classification.Sensitivity)
        {
            case DataSensitivity.Public:
                await _forwardAgent.SendToCloudAsync(
                    $"cloud.{topic}", key, value, _cloudProducer);
                return (true, true);

            case DataSensitivity.Internal:
                var anonymized = AnonymizeData(value);
                await _forwardAgent.SendToCloudAsync(
                    $"cloud.{topic}", key, anonymized, _cloudProducer);
                return (true, true);

            case DataSensitivity.Confidential:
            case DataSensitivity.Restricted:
            default:
                return (true, false);
        }
    }

    private string AnonymizeData(string data)
    {
        // Replace email addresses
        var anonymized = System.Text.RegularExpressions.Regex.Replace(
            data, @"[\w.-]+@[\w.-]+\.\w+", "[EMAIL_REDACTED]");
        // Replace IP addresses
        anonymized = System.Text.RegularExpressions.Regex.Replace(
            anonymized, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}", "[IP_REDACTED]");
        return anonymized;
    }
}

public class StoreAndForwardAgent
{
    private readonly Queue<PendingMessage> _buffer = new();
    private readonly int _maxBufferSize;
    private bool _cloudAvailable = true;
    private bool _simulatedPartition;

    public StoreAndForwardAgent(int maxBufferSize = 10_000)
    {
        _maxBufferSize = maxBufferSize;
    }

    public int BufferedCount => _buffer.Count;

    public void SimulatePartition(bool partitioned)
    {
        _simulatedPartition = partitioned;
        _cloudAvailable = !partitioned;
    }

    public async Task SendToCloudAsync(string topic, string key, string value,
        SurgewaveProducer<string, string> cloudProducer)
    {
        if (_cloudAvailable && !_simulatedPartition)
        {
            try
            {
                await cloudProducer.ProduceAsync(topic, key, value);
            }
            catch
            {
                _cloudAvailable = false;
                BufferMessage(topic, key, value);
            }
        }
        else
        {
            BufferMessage(topic, key, value);
        }
    }

    private void BufferMessage(string topic, string key, string value)
    {
        if (_buffer.Count >= _maxBufferSize)
        {
            _buffer.Dequeue(); // Drop oldest
        }
        _buffer.Enqueue(new PendingMessage(topic, key, value, DateTime.UtcNow));
    }

    public async Task<int> FlushBufferAsync(SurgewaveProducer<string, string> cloudProducer)
    {
        var flushed = 0;

        while (_buffer.Count > 0 && _cloudAvailable && !_simulatedPartition)
        {
            var msg = _buffer.Dequeue();
            try
            {
                await cloudProducer.ProduceAsync(msg.Topic, msg.Key, msg.Value);
                flushed++;
            }
            catch
            {
                // Re-buffer and stop flushing
                _buffer.Enqueue(msg);
                _cloudAvailable = false;
                break;
            }
        }

        return flushed;
    }
}

public class CloudBurstController
{
    private readonly double _burstThreshold;
    private readonly double _deactivateThreshold;
    private readonly int _onPremCapacity;
    private bool _burstActive;

    public CloudBurstController(int onPremCapacity, double burstThreshold = 0.8)
    {
        _onPremCapacity = onPremCapacity;
        _burstThreshold = burstThreshold;
        _deactivateThreshold = burstThreshold - 0.2; // Hysteresis
    }

    public bool ShouldBurst(double currentRate)
    {
        if (currentRate > _onPremCapacity * _burstThreshold)
        {
            if (!_burstActive)
            {
                _burstActive = true;
                Console.WriteLine($"  [BURST] Activating cloud burst (rate: {currentRate:N0} msg/s, " +
                    $"threshold: {_onPremCapacity * _burstThreshold:N0})");
            }
            return true;
        }

        if (_burstActive && currentRate < _onPremCapacity * _deactivateThreshold)
        {
            _burstActive = false;
            Console.WriteLine($"  [BURST] Deactivating cloud burst (rate: {currentRate:N0} msg/s)");
        }

        return _burstActive;
    }

    public bool IsBurstActive => _burstActive;
}

public class AuditLogger
{
    private readonly List<AuditEntry> _entries = new();

    public void Log(string topic, string destination, string classification)
    {
        _entries.Add(new AuditEntry(DateTime.UtcNow, topic, destination, classification));
    }

    public void PrintReport()
    {
        var byClassification = _entries.GroupBy(e => e.Classification);
        Console.WriteLine($"Total messages audited: {_entries.Count}");
        Console.WriteLine();
        Console.WriteLine($"{"Classification",-15} {"Count",6} {"Destinations"}");
        Console.WriteLine(new string('-', 50));

        foreach (var group in byClassification)
        {
            var destinations = string.Join(", ", group.Select(e => e.Destination).Distinct());
            Console.WriteLine($"{group.Key,-15} {group.Count(),6} {destinations}");
        }
    }
}

public record AuditEntry(DateTime Timestamp, string Topic, string Destination, string Classification);
