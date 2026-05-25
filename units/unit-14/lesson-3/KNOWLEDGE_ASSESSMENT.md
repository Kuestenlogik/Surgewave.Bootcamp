# Knowledge Assessment: Lesson 19.3 - Building a Minimal Client

## Multiple Choice

### 1. What is the correct order of operations when initializing a Kafka client?
- [ ] A) Connect -> Metadata -> Produce/Fetch
- [ ] B) Connect -> ApiVersions -> Metadata -> Produce/Fetch
- [ ] C) ApiVersions -> Connect -> Metadata -> Produce/Fetch
- [ ] D) Metadata -> Connect -> ApiVersions -> Produce/Fetch

<details>
<summary>Answer</summary>
**B)** The correct order is: Connect to bootstrap server, query ApiVersions to discover supported protocol versions, fetch Metadata to learn about cluster topology and topics, then perform Produce/Fetch operations.
</details>

### 2. In a Produce request, what does "Required Acks" value of -1 mean?
- [ ] A) No acknowledgment required (fire and forget)
- [ ] B) Leader acknowledgment only
- [ ] C) All in-sync replicas must acknowledge
- [ ] D) Invalid value, will cause an error

<details>
<summary>Answer</summary>
**C)** Required Acks of -1 means all in-sync replicas (ISRs) must acknowledge the write before the producer receives confirmation. This provides the strongest durability guarantee.
</details>

### 3. What value should Replica ID be set to in a Fetch request from a consumer?
- [ ] A) 0
- [ ] B) 1
- [ ] C) -1
- [ ] D) The consumer group ID

<details>
<summary>Answer</summary>
**C)** Replica ID should be -1 for consumer fetch requests. Positive values are reserved for broker-to-broker replication fetches.
</details>

### 4. What does error code 6 (NOT_LEADER_OR_FOLLOWER) indicate?
- [ ] A) The topic does not exist
- [ ] B) The broker is not the partition leader
- [ ] C) The offset is out of range
- [ ] D) The request timed out

<details>
<summary>Answer</summary>
**B)** Error code 6 indicates the broker receiving the request is not the leader for the requested partition. The client should refresh metadata and retry with the correct leader.
</details>

### 5. In a Metadata request with Topics Array set to -1, what does the broker return?
- [ ] A) An error indicating invalid request
- [ ] B) No topics, only broker information
- [ ] C) All topics in the cluster
- [ ] D) Only topics the client has produced to

<details>
<summary>Answer</summary>
**C)** A Topics Array value of -1 requests metadata for all topics in the cluster. A value of 0 (empty array) returns no topic metadata.
</details>

## True/False

### 6. In the legacy MessageSet format, a Magic byte value of 0 means the message includes a timestamp field.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Magic byte 0 indicates Message format v0, which does NOT include a timestamp. Magic byte 1 indicates v1, which includes a timestamp field.
</details>

## Code Challenge

### 7. What's wrong with this Fetch request construction?

```csharp
// Fetch request body
var fetchBody = new MemoryStream();
var writer = new BinaryWriter(fetchBody);

writer.Write(IPAddress.HostToNetworkOrder(1));        // Replica ID
writer.Write(IPAddress.HostToNetworkOrder(5000));     // Max Wait Ms
writer.Write(IPAddress.HostToNetworkOrder(1024));     // Min Bytes
writer.Write(IPAddress.HostToNetworkOrder(1));        // Topic count
// ... topic and partition data follows
```

<details>
<summary>Answer</summary>
The Replica ID should be -1 for consumer fetch requests, not 1. Using 1 would identify this as a follower replica request, which has different behavior:

```csharp
// Correct Fetch request body
var fetchBody = new MemoryStream();
var writer = new BinaryWriter(fetchBody);

writer.Write(IPAddress.HostToNetworkOrder(-1));       // Replica ID = -1 for consumers
writer.Write(IPAddress.HostToNetworkOrder(5000));     // Max Wait Ms
writer.Write(IPAddress.HostToNetworkOrder(1024));     // Min Bytes
writer.Write(IPAddress.HostToNetworkOrder(1));        // Topic count
```
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 3-4/7: Review the lesson
- <3/7: Re-do the lesson
