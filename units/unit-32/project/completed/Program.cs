// Surgewave Bootcamp - Unit 27 Project: Hybrid Cloud Demo
// COMPLETED SOLUTION

using System.Diagnostics;
using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Unit 27 Project - Hybrid Cloud Demo ===");
Console.WriteLine();

// ========== STEP 1: MULTI-REGION CLUSTER SETUP ==========
Console.WriteLine("--- Setting Up Multi-Region Clusters ---");

var clusters = new Dictionary<string, RegionCluster>();

// US East (Primary Cloud)
var usEast = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
clusters["us-east-1"] = new RegionCluster("us-east-1", usEast, 10,
    new GeoLocation(39.0438, -77.4874), "US Cloud (Virginia)");
Console.WriteLine($"[us-east-1] Started on port {usEast.Port} - US Cloud (Virginia)");

// EU West (GDPR-compliant)
var euWest = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
clusters["eu-west-1"] = new RegionCluster("eu-west-1", euWest, 80,
    new GeoLocation(53.3498, -6.2603), "EU Cloud (Ireland)");
Console.WriteLine($"[eu-west-1] Started on port {euWest.Port} - EU Cloud (Ireland)");

// APAC (Singapore)
var apac = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
clusters["ap-southeast-1"] = new RegionCluster("ap-southeast-1", apac, 200,
    new GeoLocation(1.3521, 103.8198), "APAC Cloud (Singapore)");
Console.WriteLine($"[ap-southeast-1] Started on port {apac.Port} - APAC Cloud (Singapore)");

// On-Premise (Sensitive Data)
var onPrem = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();
clusters["on-premise"] = new RegionCluster("on-premise", onPrem, 5,
    new GeoLocation(40.7128, -74.0060), "On-Premise (New York DC)");
Console.WriteLine($"[on-premise] Started on port {onPrem.Port} - On-Premise (New York DC)");

// ========== STEP 2: DATA SOVEREIGNTY RULES ==========
Console.WriteLine("\n--- Data Sovereignty Configuration ---");

var sovereigntyRules = new List<SovereigntyRule>
{
    new("eu.users.*", DataSensitivity.Confidential, new[] { "eu-west-1" }, "GDPR - EU personal data"),
    new("eu.orders.*", DataSensitivity.Confidential, new[] { "eu-west-1", "on-premise" }, "GDPR - EU transactions"),
    new("payments.*", DataSensitivity.Restricted, new[] { "on-premise" }, "PCI DSS - Payment data"),
    new("health.*", DataSensitivity.Restricted, new[] { "on-premise" }, "HIPAA - Health records"),
    new("metrics.*", DataSensitivity.Public, new[] { "us-east-1", "eu-west-1", "ap-southeast-1" }, "Public metrics"),
    new("analytics.*", DataSensitivity.Internal, new[] { "us-east-1", "eu-west-1", "ap-southeast-1" }, "Internal analytics"),
    new("logs.*", DataSensitivity.Internal, new[] { "us-east-1", "eu-west-1" }, "System logs"),
};

var classifier = new SovereigntyClassifier(sovereigntyRules);

foreach (var rule in sovereigntyRules)
{
    Console.WriteLine($"  {rule.TopicPattern,-20} [{rule.Sensitivity,-14}] -> {string.Join(", ", rule.AllowedRegions),-40} ({rule.Reason})");
}

// ========== STEP 3: GEO-ROUTING SETUP ==========
Console.WriteLine("\n--- Geographic Routing ---");

var geoRouter = new GeoRouter(clusters.Values.Where(c => c.Region != "on-premise").ToList());

var clientLocations = new[]
{
    new SimulatedClient("US-Client", "US", new GeoLocation(40.7128, -74.0060)),
    new SimulatedClient("EU-Client", "DE", new GeoLocation(52.5200, 13.4050)),
    new SimulatedClient("APAC-Client", "SG", new GeoLocation(1.3521, 103.8198)),
};

foreach (var client in clientLocations)
{
    var bestRegion = geoRouter.GetNearestRegion(client.Location);
    Console.WriteLine($"  {client.Name} ({client.Country}) -> Best region: {bestRegion.Region} " +
        $"(simulated latency: {bestRegion.BaseLatencyMs}ms)");
}

// ========== STEP 4: PRODUCERS PER REGION ==========
var producers = new Dictionary<string, SurgewaveProducer<string, string>>();
foreach (var (region, cluster) in clusters)
{
    producers[region] = new SurgewaveProducer<string, string>(opt =>
    {
        opt.BootstrapServers = cluster.Runtime.BootstrapServers;
    });
}

// ========== STEP 5: SIMULATION ==========
Console.WriteLine("\n--- Running Hybrid Cloud Simulation ---");

var auditLog = new AuditLogger();
var forwardAgent = new StoreAndForwardAgent(maxBufferSize: 500);
var burstController = new CloudBurstController(onPremCapacity: 100, burstThreshold: 0.8);
var regionMessageCount = new Dictionary<string, int>();

// Initialize counters
foreach (var region in clusters.Keys)
    regionMessageCount[region] = 0;

// Simulated workload
var workload = new[]
{
    // EU personal data (must stay in EU)
    ("eu.users.profile", "DE", "user-001", "Name: Hans Mueller, Email: hans@example.de"),
    ("eu.users.activity", "FR", "user-002", "User marie@example.fr viewed product #456"),
    ("eu.orders.created", "DE", "ord-001", "Order #789 by Hans Mueller, 149.99 EUR"),

    // Payment data (must stay on-premise)
    ("payments.processed", "US", "pay-001", "CC: 4111-xxxx-xxxx-1111, Amount: $499.99"),
    ("payments.refund", "US", "pay-002", "Refund #321 for order #654, $29.99"),

    // Health data (must stay on-premise)
    ("health.records", "US", "patient-001", "Patient: John Smith, Diagnosis: [REDACTED]"),

    // Public metrics (can go anywhere)
    ("metrics.system", "US", "srv-001", "CPU: 72%, Memory: 8.2GB, Disk: 45%"),
    ("metrics.system", "DE", "srv-002", "CPU: 65%, Memory: 6.1GB, Disk: 38%"),
    ("metrics.system", "SG", "srv-003", "CPU: 81%, Memory: 7.5GB, Disk: 52%"),

    // Analytics (internal, can be anonymized to cloud)
    ("analytics.events", "US", "evt-001", "Page: /checkout, User: user-123, Duration: 45s"),
    ("analytics.events", "DE", "evt-002", "Page: /products, User: user-456, Duration: 32s"),

    // Logs
    ("logs.application", "US", "log-001", "INFO: Order processing completed in 125ms"),
    ("logs.application", "DE", "log-002", "WARN: Slow query detected, took 2.3s"),
};

foreach (var (topic, clientCountry, key, value) in workload)
{
    var rule = classifier.Classify(topic);
    var targetRegions = rule.AllowedRegions;

    foreach (var region in targetRegions)
    {
        if (producers.ContainsKey(region))
        {
            var messageValue = rule.Sensitivity == DataSensitivity.Internal
                ? AnonymizeForCloud(value)
                : value;

            await producers[region].ProduceAsync(topic, key, messageValue);
            regionMessageCount[region] = regionMessageCount.GetValueOrDefault(region) + 1;
        }
    }

    auditLog.Log(topic, string.Join(", ", targetRegions), rule.Sensitivity.ToString(), clientCountry);
}

Console.WriteLine($"  Routed {workload.Length} messages across {clusters.Count} regions");

// ========== STEP 6: STORE-AND-FORWARD TEST ==========
Console.WriteLine("\n--- Store-and-Forward Resilience Test ---");

Console.WriteLine("  Simulating APAC network partition...");
forwardAgent.SimulatePartition(true);

for (int i = 0; i < 25; i++)
{
    await forwardAgent.SendToCloudAsync("metrics.buffered", $"key-{i}",
        $"Metric value {i}", producers["ap-southeast-1"]);
}
Console.WriteLine($"  Buffered {forwardAgent.BufferedCount} messages during partition");

Console.WriteLine("  Restoring connectivity...");
forwardAgent.SimulatePartition(false);
var flushed = await forwardAgent.FlushBufferAsync(producers["ap-southeast-1"]);
Console.WriteLine($"  Flushed {flushed} messages to APAC cluster");

// ========== STEP 7: CLOUD BURSTING TEST ==========
Console.WriteLine("\n--- Cloud Bursting Simulation ---");

var loadPattern = new[] { 50, 70, 85, 110, 130, 90, 60, 40 };
foreach (var load in loadPattern)
{
    var shouldBurst = burstController.ShouldBurst(load);
    var status = shouldBurst ? "BURST (cloud active)" : "Normal (on-prem only)";
    Console.WriteLine($"  Load: {load,4} msg/s -> {status}");
}

// ========== STEP 8: COMPREHENSIVE REPORT ==========
Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("  HYBRID CLOUD DEPLOYMENT REPORT");
Console.WriteLine(new string('=', 60));

// Region distribution
Console.WriteLine("\n--- Message Distribution by Region ---");
Console.WriteLine($"  {"Region",-20} {"Messages",10} {"Type"}");
Console.WriteLine("  " + new string('-', 50));
foreach (var (region, count) in regionMessageCount.OrderByDescending(r => r.Value))
{
    var cluster = clusters[region];
    Console.WriteLine($"  {region,-20} {count,10} {cluster.Description}");
}

// Sovereignty compliance
Console.WriteLine("\n--- Data Sovereignty Compliance ---");
auditLog.PrintSovereigntyReport();

// Latency comparison
Console.WriteLine("\n--- Simulated Latency by Region ---");
Console.WriteLine($"  {"Region",-20} {"Base Latency",15} {"Location"}");
Console.WriteLine("  " + new string('-', 55));
foreach (var (_, cluster) in clusters)
{
    Console.WriteLine($"  {cluster.Region,-20} {cluster.BaseLatencyMs,12}ms  {cluster.Description}");
}

// Cleanup
foreach (var (_, prod) in producers)
    await prod.DisposeAsync();

foreach (var (_, cluster) in clusters)
    await cluster.Runtime.DisposeAsync();

Console.WriteLine();
Console.WriteLine("Project complete!");

// ========== HELPER FUNCTIONS ==========

static string AnonymizeForCloud(string data)
{
    var anonymized = System.Text.RegularExpressions.Regex.Replace(
        data, @"[Uu]ser[:\s-]*[\w.-]+", "User: [ANONYMIZED]");
    anonymized = System.Text.RegularExpressions.Regex.Replace(
        anonymized, @"[\w.-]+@[\w.-]+\.\w+", "[EMAIL_REDACTED]");
    return anonymized;
}

// ========== SUPPORTING TYPES ==========

public record GeoLocation(double Latitude, double Longitude);

public record RegionCluster(
    string Region,
    SurgewaveRuntime Runtime,
    int BaseLatencyMs,
    GeoLocation Location,
    string Description
);

public record SimulatedClient(string Name, string Country, GeoLocation Location);

public enum DataSensitivity { Public, Internal, Confidential, Restricted }

public record SovereigntyRule(
    string TopicPattern,
    DataSensitivity Sensitivity,
    string[] AllowedRegions,
    string Reason
);

public class SovereigntyClassifier
{
    private readonly List<SovereigntyRule> _rules;

    public SovereigntyClassifier(List<SovereigntyRule> rules) => _rules = rules;

    public SovereigntyRule Classify(string topic)
    {
        // Find best matching rule (most specific pattern first)
        return _rules
            .Where(r => topic.StartsWith(r.TopicPattern.TrimEnd('*')))
            .OrderByDescending(r => r.TopicPattern.Length)
            .FirstOrDefault()
            ?? new SovereigntyRule("*", DataSensitivity.Confidential, new[] { "on-premise" }, "Default: on-premise only");
    }
}

public class GeoRouter
{
    private readonly List<RegionCluster> _clusters;

    public GeoRouter(List<RegionCluster> clusters) => _clusters = clusters;

    public RegionCluster GetNearestRegion(GeoLocation clientLocation)
    {
        return _clusters
            .OrderBy(c => CalculateDistance(clientLocation, c.Location))
            .First();
    }

    private double CalculateDistance(GeoLocation a, GeoLocation b)
    {
        var R = 6371;
        var dLat = ToRadians(b.Latitude - a.Latitude);
        var dLon = ToRadians(b.Longitude - a.Longitude);
        var lat1 = ToRadians(a.Latitude);
        var lat2 = ToRadians(b.Latitude);
        var x = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
        return R * 2 * Math.Atan2(Math.Sqrt(x), Math.Sqrt(1 - x));
    }

    private double ToRadians(double deg) => deg * Math.PI / 180;
}

public record PendingMessage(string Topic, string Key, string Value, DateTime BufferedAt);

public class StoreAndForwardAgent
{
    private readonly Queue<PendingMessage> _buffer = new();
    private readonly int _maxBufferSize;
    private bool _simulatedPartition;

    public StoreAndForwardAgent(int maxBufferSize = 10_000) => _maxBufferSize = maxBufferSize;

    public int BufferedCount => _buffer.Count;

    public void SimulatePartition(bool partitioned) => _simulatedPartition = partitioned;

    public async Task SendToCloudAsync(string topic, string key, string value,
        SurgewaveProducer<string, string> producer)
    {
        if (!_simulatedPartition)
        {
            try
            {
                await producer.ProduceAsync(topic, key, value);
                return;
            }
            catch { /* Fall through to buffer */ }
        }

        if (_buffer.Count >= _maxBufferSize) _buffer.Dequeue();
        _buffer.Enqueue(new PendingMessage(topic, key, value, DateTime.UtcNow));
    }

    public async Task<int> FlushBufferAsync(SurgewaveProducer<string, string> producer)
    {
        var flushed = 0;
        while (_buffer.Count > 0 && !_simulatedPartition)
        {
            var msg = _buffer.Dequeue();
            try
            {
                await producer.ProduceAsync(msg.Topic, msg.Key, msg.Value);
                flushed++;
            }
            catch
            {
                _buffer.Enqueue(msg);
                break;
            }
        }
        return flushed;
    }
}

public class CloudBurstController
{
    private readonly int _onPremCapacity;
    private readonly double _burstThreshold;
    private bool _burstActive;

    public CloudBurstController(int onPremCapacity, double burstThreshold = 0.8)
    {
        _onPremCapacity = onPremCapacity;
        _burstThreshold = burstThreshold;
    }

    public bool ShouldBurst(double currentRate)
    {
        if (currentRate > _onPremCapacity * _burstThreshold && !_burstActive)
        {
            _burstActive = true;
        }
        else if (_burstActive && currentRate < _onPremCapacity * (_burstThreshold - 0.2))
        {
            _burstActive = false;
        }
        return _burstActive;
    }
}

public class AuditLogger
{
    private readonly List<AuditEntry> _entries = new();

    public void Log(string topic, string regions, string classification, string clientCountry)
    {
        _entries.Add(new AuditEntry(DateTime.UtcNow, topic, regions, classification, clientCountry));
    }

    public void PrintSovereigntyReport()
    {
        var byClassification = _entries.GroupBy(e => e.Classification);
        Console.WriteLine($"  Total messages audited: {_entries.Count}");
        Console.WriteLine();
        Console.WriteLine($"  {"Classification",-15} {"Count",6} {"Regions"}");
        Console.WriteLine("  " + new string('-', 55));

        foreach (var group in byClassification.OrderBy(g => g.Key))
        {
            var regions = string.Join("; ", group.Select(e => e.Regions).Distinct());
            Console.WriteLine($"  {group.Key,-15} {group.Count(),6} {regions}");
        }

        // Check for violations
        var violations = _entries.Where(e =>
            e.Classification is "Restricted" or "Confidential" &&
            e.Regions.Contains("us-east-1")).ToList();

        Console.WriteLine();
        Console.WriteLine($"  Compliance violations: {violations.Count}");
        if (violations.Count == 0)
            Console.WriteLine("  Status: COMPLIANT - All sovereignty rules respected");
        else
            Console.WriteLine("  Status: NON-COMPLIANT - Review violations above");
    }
}

public record AuditEntry(DateTime Timestamp, string Topic, string Regions,
    string Classification, string ClientCountry);
