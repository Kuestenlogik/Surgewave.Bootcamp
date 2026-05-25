# Knowledge Assessment: Lesson 4.1 - Clustering

## Multiple Choice

### 1. What is the primary purpose of clustering in Surgewave?
- [ ] A) To reduce disk usage
- [ ] B) To provide high availability and horizontal scaling
- [ ] C) To encrypt data at rest
- [ ] D) To simplify configuration

<details>
<summary>Answer</summary>
**B)** Clustering provides high availability (continue operating if a broker fails) and horizontal scaling (distribute load across brokers). A single broker is a single point of failure with limited throughput.
</details>

### 2. How do brokers in a Surgewave cluster discover each other?
- [ ] A) Manual configuration only
- [ ] B) DNS discovery
- [ ] C) Configuration file with seed brokers and gossip protocol
- [ ] D) Broadcast messages on the network

<details>
<summary>Answer</summary>
**C)** Brokers discover each other through configuration files with seed brokers and use a gossip protocol for membership updates, along with leader election for partitions.
</details>

### 3. When configuring a client to connect to a Surgewave cluster, which approach is recommended?
- [ ] A) Connect to only one broker
- [ ] B) List multiple bootstrap servers
- [ ] C) Use a load balancer only
- [ ] D) Connect to the leader broker only

<details>
<summary>Answer</summary>
**B)** Clients should list multiple bootstrap servers (e.g., "localhost:9092,localhost:9093,localhost:9094"). The client connects to any broker and discovers the full cluster automatically.
</details>

## True/False

### 4. In a Surgewave cluster, all partitions are stored on a single broker.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - In a cluster, partitions are distributed across brokers. This enables horizontal scaling and data locality, unlike a single broker where all partitions are local.
</details>

### 5. The `WithClusterSeeds()` method is used to specify other brokers in the cluster during broker configuration.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - When configuring a broker with `SurgewaveRuntime.CreateBuilder()`, you use `WithClusterSeeds()` to specify other brokers that this broker should connect to for cluster membership.
</details>

## Code Challenge

### 6. What configuration is missing from this broker setup for a 3-broker cluster?

```csharp
var broker1 = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .Build()
    .StartAsync();
```

<details>
<summary>Answer</summary>
The configuration is missing:
1. `WithBrokerId(1)` - Each broker needs a unique ID
2. `WithClusterSeeds("localhost:9093", "localhost:9094")` - Seed brokers for cluster discovery

Correct configuration:
```csharp
var broker1 = await SurgewaveRuntime.CreateBuilder()
    .WithBrokerId(1)
    .WithPort(9092)
    .WithClusterSeeds("localhost:9093", "localhost:9094")
    .Build()
    .StartAsync();
```
</details>

### 7. A producer is configured with only one bootstrap server. What happens if that broker goes down?

```csharp
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
});
```

<details>
<summary>Answer</summary>
If localhost:9092 is down when the producer starts, it cannot connect to the cluster at all. However, if the producer has already connected and discovered other brokers, it may still work.

Best practice is to list multiple bootstrap servers:
```csharp
opt.BootstrapServers = "localhost:9092,localhost:9093,localhost:9094";
```
This ensures the producer can connect even if one broker is unavailable during startup.
</details>

## Score
- 7/7: Excellent! You understand clustering fundamentals
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
