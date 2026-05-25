# Quiz: Lesson 0.1 - What is Surgewave?

## Multiple Choice

### 1. What is the fundamental pattern of message streaming?
- [ ] A) Direct service-to-service calls
- [ ] B) Producers send messages to topics, consumers read from topics
- [ ] C) Batch file transfers between systems
- [ ] D) Real-time database replication

<details>
<summary>Answer</summary>
**B)** Producers send messages to topics, consumers read from topics. This is the core pattern where messages are persisted and can be replayed, with multiple consumers reading independently.
</details>

### 2. What is NOT a benefit of message streaming over direct service calls?
- [ ] A) Services communicate via messages, reducing coupling
- [ ] B) Messages persist until explicitly deleted
- [ ] C) Synchronous request-response communication
- [ ] D) Consumers can scale independently

<details>
<summary>Answer</summary>
**C)** Synchronous request-response communication. Message streaming is asynchronous, which is a key benefit for decoupling services. Direct synchronous calls would defeat the purpose.
</details>

### 3. What problem does Surgewave solve compared to Apache Kafka?
- [ ] A) Surgewave provides more features than Kafka
- [ ] B) Surgewave requires ZooKeeper for coordination
- [ ] C) Surgewave is native .NET with simpler setup and no JVM required
- [ ] D) Surgewave has a larger community

<details>
<summary>Answer</summary>
**C)** Surgewave is native .NET with simpler setup and no JVM required. Surgewave provides Kafka-compatible functionality without the operational overhead of managing a Java ecosystem.
</details>

## True/False

### 4. Surgewave can run in embedded mode inside your application.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Surgewave supports embedded mode, which allows running the broker inside your app. This is particularly useful for development and testing scenarios.
</details>

### 5. Surgewave uses a completely different protocol than Kafka, so existing Kafka tools won't work.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Surgewave is Kafka-compatible, meaning it uses the same protocol and you can use existing Kafka tools and libraries with Surgewave.
</details>

## Short Answer

### 6. Name three use cases where Surgewave is a good fit.
<details>
<summary>Answer</summary>
Any three of the following:
- Microservices communicating asynchronously
- Event sourcing and CQRS patterns
- Log aggregation and metrics collection
- Real-time data pipelines
- Development and testing (embedded broker)
- .NET shops wanting to avoid JVM operations
</details>

### 7. What are the four key features of a message in message streaming?
<details>
<summary>Answer</summary>
1. **Key**: Optional identifier for routing
2. **Value**: The actual data payload
3. **Headers**: Optional metadata
4. **Timestamp**: When the message was created
</details>

## Score
- 7/7: Excellent! You understand Surgewave's purpose and benefits
- 5-6/7: Good understanding of the basics
- 3-4/7: Review the lesson, focusing on comparisons
- < 3/7: Re-do the lesson before continuing
