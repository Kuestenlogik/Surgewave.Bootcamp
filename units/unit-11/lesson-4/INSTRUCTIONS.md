# Lesson 5.4: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Pre-Cutover Validation

```csharp
public class MigrationValidator
{
    public async Task<bool> ValidateReadyForCutover()
    {
        var checks = new List<(string Name, bool Passed)>();

        // Check Surgewave cluster health
        checks.Add(("Surgewave cluster healthy", await CheckClusterHealth()));

        // Check data consistency
        checks.Add(("Data mirrored", await CheckDataMirrored()));

        // Check consumer group readiness
        checks.Add(("Consumer groups ready", await CheckConsumerGroups()));

        // Print results
        foreach (var (name, passed) in checks)
        {
            var status = passed ? "PASS" : "FAIL";
            Console.WriteLine($"  [{status}] {name}");
        }

        return checks.All(c => c.Passed);
    }
}
```

## Step 2: Execute Cutover

```csharp
public async Task ExecuteCutover()
{
    Console.WriteLine("Starting cutover...");

    // Step 1: Stop dual-writes to Kafka
    Console.WriteLine("1. Stopping writes to Kafka...");
    await StopKafkaWrites();

    // Step 2: Wait for Kafka consumers to drain
    Console.WriteLine("2. Draining Kafka consumers...");
    await WaitForKafkaDrain();

    // Step 3: Switch consumer bootstrap servers
    Console.WriteLine("3. Switching consumers to Surgewave...");
    await SwitchConsumers();

    // Step 4: Verify message flow
    Console.WriteLine("4. Verifying message flow...");
    await VerifyMessageFlow();

    Console.WriteLine("Cutover complete!");
}
```

## Step 3: Verify Success

```csharp
public async Task<bool> VerifyMigrationSuccess()
{
    // Check message flow
    var produceOk = await VerifyProducers();
    var consumeOk = await VerifyConsumers();
    var lagOk = await VerifyConsumerLag();

    Console.WriteLine($"Producers working: {produceOk}");
    Console.WriteLine($"Consumers working: {consumeOk}");
    Console.WriteLine($"Consumer lag acceptable: {lagOk}");

    return produceOk && consumeOk && lagOk;
}
```

## Step 4: Rollback Plan

```csharp
public async Task ExecuteRollback()
{
    Console.WriteLine("ROLLBACK: Reverting to Kafka...");

    // Step 1: Stop Surgewave writes
    await StopSurgewaveWrites();

    // Step 2: Switch consumers back to Kafka
    await SwitchConsumersToKafka();

    // Step 3: Resume Kafka writes
    await ResumeKafkaWrites();

    // Step 4: Verify Kafka message flow
    await VerifyKafkaMessageFlow();

    Console.WriteLine("Rollback complete. System running on Kafka.");
}
```

## Step 5: Decommission Kafka

After burn-in period (typically 1-2 weeks):

```csharp
public async Task DecommissionKafka()
{
    // Final validation
    var success = await VerifyMigrationSuccess();
    if (!success)
    {
        Console.WriteLine("Cannot decommission: validation failed");
        return;
    }

    Console.WriteLine("Safe to decommission Kafka:");
    Console.WriteLine("1. Stop all Kafka brokers");
    Console.WriteLine("2. Archive Kafka data (optional)");
    Console.WriteLine("3. Release Kafka infrastructure");
    Console.WriteLine("4. Update documentation");
}
```

## Complete Migration Checklist

```
PRE-CUTOVER
───────────
[ ] Surgewave cluster deployed and sized
[ ] Topics created with correct configuration
[ ] Data mirrored from Kafka
[ ] Applications tested against Surgewave
[ ] Monitoring and alerting configured
[ ] Rollback procedure documented
[ ] Cutover window scheduled
[ ] Stakeholders notified

CUTOVER
───────
[ ] Enter maintenance window
[ ] Stop dual-writes (Kafka side)
[ ] Drain Kafka consumers
[ ] Update application configs
[ ] Deploy applications with Surgewave bootstrap
[ ] Verify message flow
[ ] Monitor for errors

POST-CUTOVER
────────────
[ ] Validate message counts match
[ ] Check consumer lag < threshold
[ ] Verify application functionality
[ ] Notify stakeholders of completion
[ ] Begin burn-in period

DECOMMISSION (after burn-in)
────────────────────────────
[ ] Confirm no Kafka dependencies
[ ] Stop Kafka brokers
[ ] Archive data if needed
[ ] Release infrastructure
[ ] Update architecture docs
[ ] Close migration project
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

---

**Congratulations!** You've completed the Surgewave Bootcamp!

You now know how to:
- Build Surgewave applications from scratch
- Test with multiple patterns
- Implement advanced messaging features
- Deploy and operate in production
- Migrate from Kafka to Surgewave
