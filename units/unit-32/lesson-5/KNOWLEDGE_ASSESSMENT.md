# Knowledge Assessment: Lesson 27.5 - Hybrid On-Premise and Cloud

## Multiple Choice

### 1. What is data sovereignty?
- [ ] A) The right to own all data
- [ ] B) Legal requirements about where data can be stored and processed geographically
- [ ] C) Encrypting data at rest
- [ ] D) Using multiple cloud providers

<details>
<summary>Answer</summary>
**B)** Data sovereignty refers to laws and regulations that require data to be stored and processed within specific geographic boundaries. For example, GDPR requires EU personal data to remain within the EU/EEA unless adequate protection exists in the destination country.
</details>

### 2. When should a store-and-forward agent drop messages from its buffer?
- [ ] A) Never, all messages must be delivered
- [ ] B) When the buffer reaches its capacity limit
- [ ] C) After 1 second
- [ ] D) When the cloud reconnects

<details>
<summary>Answer</summary>
**B)** When the buffer is full and new messages arrive, the oldest messages should be dropped (or written to disk) to prevent memory exhaustion. The buffer size should be configured based on expected partition duration and available memory. Critical messages should use a separate, persistent buffer.
</details>

### 3. What is cloud bursting?
- [ ] A) Moving all workloads to the cloud
- [ ] B) Temporarily scaling to cloud resources when on-premise capacity is exceeded
- [ ] C) Destroying cloud resources
- [ ] D) Backing up data to the cloud

<details>
<summary>Answer</summary>
**B)** Cloud bursting is a pattern where normal workloads run on-premise, but when demand exceeds on-premise capacity, overflow is automatically routed to cloud resources. When demand subsides, cloud resources are released. This provides elastic scaling while keeping base workload costs predictable.
</details>

### 4. Which data classification level should NEVER be replicated to the cloud?
- [ ] A) Public
- [ ] B) Internal
- [ ] C) Confidential
- [ ] D) All levels can be replicated

<details>
<summary>Answer</summary>
**C)** Confidential and Restricted data must remain on-premise to comply with data sovereignty requirements. Public data can be freely replicated, and Internal data may be replicated if properly anonymized. The specific rules depend on the organization's compliance requirements.
</details>

### 5. What is the recommended connectivity between on-premise and cloud for Surgewave replication?
- [ ] A) Public internet
- [ ] B) VPN tunnel or dedicated connection (Direct Connect / ExpressRoute)
- [ ] C) SSH tunnel
- [ ] D) FTP

<details>
<summary>Answer</summary>
**B)** VPN tunnels or dedicated connections (AWS Direct Connect, Azure ExpressRoute, GCP Cloud Interconnect) provide encrypted, reliable, and low-latency connectivity between on-premise and cloud. Public internet is unreliable and insecure for production data replication.
</details>

## True/False

### 6. In a hybrid deployment, all data processing should happen in the cloud for cost efficiency.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Hybrid deployments keep sensitive and latency-critical processing on-premise while using cloud for elastic scaling, analytics, and non-sensitive workloads. Moving all processing to cloud may violate compliance requirements and increase latency for local operations.
</details>

### 7. Edge-to-cloud patterns require continuous internet connectivity.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Edge brokers should buffer data locally when cloud connectivity is unavailable (store-and-forward pattern). When connectivity is restored, the buffered data is synchronized to the cloud. This makes edge deployments resilient to intermittent connectivity.
</details>

### 8. Cloud bursting should include hysteresis to prevent rapid activation/deactivation cycling.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Without hysteresis, if load fluctuates around the threshold, the system would rapidly activate and deactivate cloud resources (thrashing). Using different thresholds for activation (e.g., 80%) and deactivation (e.g., 60%) prevents this oscillation and provides stable scaling behavior.
</details>

## Code Challenge

### 9. What issues exist in this hybrid routing implementation?

```csharp
public class HybridRouter
{
    public async Task RouteAsync(string topic, string key, string value)
    {
        // Always send to both on-premise and cloud
        await _onPremProducer.ProduceAsync(topic, key, value);
        await _cloudProducer.ProduceAsync(topic, key, value);
    }
}
```

<details>
<summary>Answer</summary>
**Issues:**

1. **No data classification** - Sensitive data (PII, financial) should not go to cloud
2. **No anonymization** - Internal data should be anonymized before cloud replication
3. **No error handling** - If cloud fails, on-premise also fails (sequential await)
4. **No store-and-forward** - Cloud failures cause data loss
5. **Same topic name** - Should use prefixed names to distinguish sources
6. **No audit logging** - Compliance requires logging cross-boundary transfers

**Corrected implementation:**

```csharp
public class HybridRouter
{
    private readonly DataClassifier _classifier;
    private readonly StoreAndForwardAgent _forwardAgent;
    private readonly IAuditLogger _auditLogger;

    public async Task RouteAsync(string topic, string key, string value)
    {
        var classification = _classifier.Classify(topic);

        // Always store on-premise
        await _onPremProducer.ProduceAsync(topic, key, value);

        // Cloud routing based on classification
        switch (classification.Sensitivity)
        {
            case DataSensitivity.Public:
                await _forwardAgent.SendToCloudAsync(
                    $"cloud.{topic}", key, value, _cloudProducer);
                _auditLogger.LogTransfer(topic, "cloud", "public");
                break;

            case DataSensitivity.Internal:
                var anonymized = Anonymize(value);
                await _forwardAgent.SendToCloudAsync(
                    $"cloud.{topic}", key, anonymized, _cloudProducer);
                _auditLogger.LogTransfer(topic, "cloud", "anonymized");
                break;

            case DataSensitivity.Confidential:
            case DataSensitivity.Restricted:
                // On-premise only - do not replicate
                _auditLogger.LogRetention(topic, "on-premise", classification.Sensitivity.ToString());
                break;
        }
    }
}
```
</details>

### 10. Design a compliance monitoring system for a hybrid Surgewave deployment.

**Scenario**: A healthcare company uses Surgewave for patient data processing. Requirements:
- HIPAA compliance for patient health information (PHI)
- Real-time anomaly detection can use anonymized data in the cloud
- Audit trail for all data access and movement
- Automatic alerts if PHI leaves the on-premise boundary

<details>
<summary>Answer</summary>
**Compliance Monitoring System:**

```csharp
public class ComplianceMonitor
{
    private readonly List<ComplianceViolation> _violations = new();
    private readonly DataClassifier _classifier;

    public async Task MonitorMessage(string topic, string key, string value,
        string destination)
    {
        var classification = _classifier.Classify(topic);

        // Check for PHI in cloud-bound messages
        if (destination == "cloud" && ContainsPHI(value))
        {
            var violation = new ComplianceViolation(
                Timestamp: DateTime.UtcNow,
                Topic: topic,
                Destination: destination,
                Rule: "HIPAA-PHI-BOUNDARY",
                Severity: "CRITICAL",
                Message: "PHI detected in cloud-bound message"
            );

            _violations.Add(violation);
            await AlertAsync(violation);
            throw new ComplianceException("PHI cannot leave on-premise boundary");
        }

        // Audit trail
        await AuditLogAsync(new AuditEntry(
            Timestamp: DateTime.UtcNow,
            Action: "MESSAGE_ROUTED",
            Topic: topic,
            Destination: destination,
            Classification: classification.Sensitivity.ToString(),
            Anonymized: classification.Sensitivity == DataSensitivity.Internal
        ));
    }

    private bool ContainsPHI(string data)
    {
        // Check for patterns indicating PHI
        // - SSN patterns
        // - Date of birth
        // - Medical record numbers
        // - Patient names (NER)
        return Regex.IsMatch(data, @"\d{3}-\d{2}-\d{4}") || // SSN
               Regex.IsMatch(data, @"patient[_\s]?id", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(data, @"diagnosis|treatment|prescription",
                   RegexOptions.IgnoreCase);
    }
}

// Topic classification rules
var rules = new List<DataClassification>
{
    new("patient.", DataSensitivity.Restricted, true, new[] { "on-premise" }),
    new("clinical.", DataSensitivity.Confidential, true, new[] { "on-premise" }),
    new("analytics.", DataSensitivity.Internal, false, new[] { "on-premise", "cloud" }),
    new("metrics.", DataSensitivity.Public, false, new[] { "on-premise", "cloud" }),
};

// Monitoring dashboard output
Console.WriteLine("=== Compliance Dashboard ===");
Console.WriteLine($"Total messages processed: {totalProcessed:N0}");
Console.WriteLine($"PHI messages (on-prem only): {phiCount:N0}");
Console.WriteLine($"Anonymized to cloud: {anonymizedCount:N0}");
Console.WriteLine($"Violations detected: {violations.Count}");
Console.WriteLine($"Compliance score: {(1 - violations.Count / (double)totalProcessed) * 100:F2}%");
```

**Architecture:**
```
On-Premise:
  patient.records -> Surgewave (on-prem only)
  clinical.notes -> Surgewave (on-prem only)

Cloud (anonymized):
  analytics.trends -> Surgewave (anonymized patient data)
  metrics.system -> Surgewave (infrastructure metrics)

Monitoring:
  compliance.audit -> Surgewave (on-prem, immutable log)
  compliance.alerts -> Alert system (PagerDuty/Slack)
```
</details>

## Score
- 10/10: Excellent! Ready for the unit project
- 8-9/10: Good understanding, review missed concepts
- 5-7/10: Review the lesson before continuing
- <5/10: Re-do the lesson and exercises
