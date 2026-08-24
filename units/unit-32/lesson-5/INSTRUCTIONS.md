# Lesson 27.5: Instructions

Follow these step-by-step instructions to implement hybrid on-premise and cloud patterns.

## Step 1: Set Up On-Premise and Cloud Clusters

Simulate on-premise and cloud environments:

```csharp
// On-premise cluster (primary, holds sensitive data)
var onPremise = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

// Cloud cluster (secondary, holds aggregated/non-sensitive data)
var cloud = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"On-Premise cluster: port {onPremise.Port}");
Console.WriteLine($"Cloud cluster: port {cloud.Port}");
```

## Step 2: Implement Data Classification

Create a system to classify and route data:

```csharp
public enum DataSensitivity
{
    Public,         // Can go to cloud freely
    Internal,       // Can go to cloud with anonymization
    Confidential,   // Must stay on-premise
    Restricted      // Must stay on-premise, encrypted at rest
}

public record DataClassification(
    string TopicPattern,
    DataSensitivity Sensitivity,
    bool RequiresEncryption,
    string[] AllowedRegions
);

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
```

## Step 3: Implement Data Routing

Route messages based on classification:

```csharp
public class HybridRouter
{
    private readonly SurgewaveProducer<string, string> _onPremProducer;
    private readonly SurgewaveProducer<string, string> _cloudProducer;
    private readonly DataClassifier _classifier;

    public async Task RouteMessageAsync(string topic, string key, string value)
    {
        var classification = _classifier.Classify(topic);

        switch (classification.Sensitivity)
        {
            case DataSensitivity.Public:
            case DataSensitivity.Internal:
                // Can replicate to cloud
                await _onPremProducer.ProduceAsync(topic, key, value);
                var cloudValue = classification.Sensitivity == DataSensitivity.Internal
                    ? AnonymizeData(value)
                    : value;
                await _cloudProducer.ProduceAsync($"cloud-{topic}", key, cloudValue);
                break;

            case DataSensitivity.Confidential:
            case DataSensitivity.Restricted:
                // On-premise only
                await _onPremProducer.ProduceAsync(topic, key, value);
                break;
        }
    }

    private string AnonymizeData(string data) => "[ANONYMIZED]";
}
```

## Step 4: Implement Store-and-Forward for Network Partitions

Handle connectivity loss between on-premise and cloud:

```csharp
public class StoreAndForwardAgent
{
    private readonly Queue<PendingMessage> _buffer = new();
    private readonly int _maxBufferSize;
    private bool _cloudAvailable = true;

    public async Task SendToCloudAsync(string topic, string key, string value,
        SurgewaveProducer<string, string> cloudProducer)
    {
        if (_cloudAvailable)
        {
            try
            {
                await cloudProducer.ProduceAsync(topic, key, value);
                // Also flush any buffered messages
                await FlushBufferAsync(cloudProducer);
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
            // Periodically try to reconnect
            await TryReconnectAsync(cloudProducer);
        }
    }

    private void BufferMessage(string topic, string key, string value)
    {
        if (_buffer.Count >= _maxBufferSize)
            _buffer.Dequeue(); // Drop oldest if buffer full
        _buffer.Enqueue(new PendingMessage(topic, key, value, DateTime.UtcNow));
    }
}
```

## Step 5: Implement Cloud Bursting

Scale to cloud when on-premise capacity is exceeded:

```csharp
public class CloudBurstController
{
    private readonly double _burstThreshold; // e.g., 0.8 = 80% capacity
    private readonly int _onPremCapacity;
    private bool _burstActive;

    public CloudBurstController(int onPremCapacity, double burstThreshold = 0.8)
    {
        _onPremCapacity = onPremCapacity;
        _burstThreshold = burstThreshold;
    }

    public bool ShouldBurst(double currentRate)
    {
        if (currentRate > _onPremCapacity * _burstThreshold)
        {
            if (!_burstActive)
            {
                _burstActive = true;
                Console.WriteLine($"[BURST] Activating cloud burst (rate: {currentRate:N0} msg/s)");
            }
            return true;
        }

        if (_burstActive && currentRate < _onPremCapacity * 0.6)
        {
            _burstActive = false;
            Console.WriteLine($"[BURST] Deactivating cloud burst (rate: {currentRate:N0} msg/s)");
        }

        return _burstActive;
    }
}
```

## Step 6: Run End-to-End Hybrid Simulation

Test the complete hybrid pipeline:

```csharp
// Simulate mixed workload
var messages = new[]
{
    ("orders.created", DataSensitivity.Confidential, "Order #12345 for customer John Doe"),
    ("metrics.cpu", DataSensitivity.Public, "cpu_usage: 75%"),
    ("users.activity", DataSensitivity.Internal, "User john@example.com logged in"),
    ("payments.processed", DataSensitivity.Restricted, "CC: 4111-1111-1111-1111"),
    ("analytics.pageview", DataSensitivity.Public, "Page: /dashboard, Views: 1500"),
};

foreach (var (topic, sensitivity, data) in messages)
{
    await router.RouteMessageAsync(topic, "key", data);
    var classification = classifier.Classify(topic);
    Console.WriteLine($"[{classification.Sensitivity}] {topic} -> " +
        $"{(classification.Sensitivity <= DataSensitivity.Internal ? "Cloud + On-Prem" : "On-Prem Only")}");
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- On-premise and cloud cluster setup
- Data classification and routing
- Store-and-forward for network partitions
- Cloud bursting under load
- Compliance audit logging

## Sample Output

```
=== Surgewave Bootcamp: Lesson 27.5 - Hybrid On-Premise and Cloud ===

--- Cluster Setup ---
On-Premise cluster: port 12345
Cloud cluster: port 12346

--- Data Classification ---
orders.*:      Confidential  -> On-Premise Only
payments.*:    Restricted    -> On-Premise Only (Encrypted)
metrics.*:     Public        -> Cloud + On-Premise
analytics.*:   Public        -> Cloud + On-Premise
users.*:       Internal      -> Cloud (Anonymized) + On-Premise

--- Data Routing ---
[Confidential] orders.created -> On-Prem Only
[Public] metrics.cpu -> Cloud + On-Prem
[Internal] users.activity -> Cloud (Anonymized) + On-Prem
[Restricted] payments.processed -> On-Prem Only
[Public] analytics.pageview -> Cloud + On-Prem

--- Store-and-Forward Test ---
Simulating network partition...
Buffered 50 messages during partition
Connectivity restored - flushing buffer
Flushed 50 messages to cloud

--- Cloud Bursting ---
Load: 6000 msg/s (60%) - On-Premise handles
Load: 8500 msg/s (85%) - [BURST] Activating cloud burst
Load: 12000 msg/s - Cloud handling overflow (2000 msg/s)
Load: 5000 msg/s (50%) - [BURST] Deactivating cloud burst

Lesson complete!
```

## Knowledge Check

1. What is data sovereignty and why does it matter for hybrid deployments?
2. How does store-and-forward handle network partitions?
3. When should cloud bursting activate and deactivate?
4. What data should never leave the on-premise environment?
5. How do you ensure compliance with GDPR in a hybrid architecture?

---

**Next:** [Unit 27 Project: Hybrid Cloud Demo](../project/README.md)
