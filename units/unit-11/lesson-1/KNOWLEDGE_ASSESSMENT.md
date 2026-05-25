# Knowledge Assessment: Lesson 5.1 - Kafka Compatibility

## Multiple Choice

### 1. How does Surgewave achieve Kafka compatibility?
- [ ] A) By wrapping Kafka binaries
- [ ] B) By implementing the Kafka wire protocol
- [ ] C) By requiring a Kafka-to-Surgewave proxy
- [ ] D) By using Kafka Connect

<details>
<summary>Answer</summary>
**B)** Surgewave implements the Kafka wire protocol, allowing existing Kafka clients (like Confluent.Kafka) to connect directly to Surgewave brokers without any code changes.
</details>

### 2. Which Kafka feature has partial support in Surgewave?
- [ ] A) Produce
- [ ] B) Consume
- [ ] C) Consumer Groups
- [ ] D) Kafka Streams

<details>
<summary>Answer</summary>
**D)** Kafka Streams has partial support. Produce, Consume, Consumer Groups, Transactions, and Admin API all have full support in Surgewave.
</details>

### 3. What is the minimum change required to migrate a Kafka client to Surgewave?
- [ ] A) Rewrite all producer code
- [ ] B) Change the bootstrap servers address
- [ ] C) Install a new NuGet package
- [ ] D) Update the serialization format

<details>
<summary>Answer</summary>
**B)** Migration can be as simple as changing the bootstrap servers from "kafka-broker:9092" to "surgewave-broker:9092". The protocol compatibility means no code changes are needed for basic functionality.
</details>

## True/False

### 4. Existing Confluent.Kafka code requires modifications to work with Surgewave.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Surgewave speaks the same wire protocol as Kafka. Existing Confluent.Kafka code can work with Surgewave by simply changing the bootstrap servers address.
</details>

### 5. Surgewave has full support for Kafka transactions.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - According to the compatibility table, Surgewave has full support for Kafka transactions, allowing exactly-once semantics for applications that require it.
</details>

## Code Challenge

### 6. What change is needed to migrate this Kafka producer to Surgewave?

```csharp
var config = new ProducerConfig
{
    BootstrapServers = "kafka-broker-1:9092,kafka-broker-2:9092",
    Acks = Acks.All
};

using var producer = new ProducerBuilder<string, string>(config).Build();
await producer.ProduceAsync("orders", new Message<string, string>
{
    Key = "order-123",
    Value = "{\"status\": \"created\"}"
});
```

<details>
<summary>Answer</summary>
Only the BootstrapServers needs to change:

```csharp
var config = new ProducerConfig
{
    BootstrapServers = "surgewave-broker-1:9092,surgewave-broker-2:9092",  // Changed!
    Acks = Acks.All
};

using var producer = new ProducerBuilder<string, string>(config).Build();
await producer.ProduceAsync("orders", new Message<string, string>
{
    Key = "order-123",
    Value = "{\"status\": \"created\"}"
});
```

No other changes are required because Surgewave implements the Kafka protocol. The ProducerConfig, ProducerBuilder, and ProduceAsync calls all work identically.
</details>

### 7. A team wants to migrate from Kafka to Surgewave but is concerned about application compatibility. What steps should they take to verify compatibility?

<details>
<summary>Answer</summary>
Steps to verify compatibility:

1. **Start a Surgewave broker** in a test environment

2. **Point existing Kafka clients at Surgewave** by changing bootstrap servers:
   ```csharp
   // Test configuration
   BootstrapServers = "surgewave-broker:9092"
   ```

3. **Test produce/consume compatibility**:
   - Verify messages can be produced
   - Verify messages can be consumed
   - Check message ordering
   - Validate consumer group behavior

4. **Test advanced features** used by the application:
   - Transactions (if used)
   - Specific consumer configurations
   - Admin API operations

5. **Identify unsupported features**:
   - Check if Kafka Streams is used (partial support)
   - Check if Schema Registry is used (planned, not yet available)

6. **Performance testing**:
   - Compare throughput and latency
   - Verify under expected load

This methodical approach ensures compatibility before production migration.
</details>

## Score
- 7/7: Excellent! You understand Kafka compatibility
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
