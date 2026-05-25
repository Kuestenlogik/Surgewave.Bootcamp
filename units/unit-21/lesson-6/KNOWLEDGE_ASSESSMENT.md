# Knowledge Assessment: Lesson 4.6 - Admin API

## Multiple Choice

### 1. Which Admin API operation would you use to increase a topic's partition count?
- [ ] A) CreateTopic
- [ ] B) AlterConfigs
- [ ] C) AddPartitions
- [ ] D) DescribeTopic

<details>
<summary>Answer</summary>
**C)** `AddPartitions` is used to increase the partition count of an existing topic. This is useful for scaling a topic to handle more throughput.
</details>

### 2. What does the `retention.ms` topic configuration control?
- [ ] A) How long to keep messages
- [ ] B) Maximum message size
- [ ] C) Replication factor
- [ ] D) Compression type

<details>
<summary>Answer</summary>
**A)** `retention.ms` controls how long to retain messages in a topic. The default is 7 days (604800000 ms). After this period, messages are eligible for deletion.
</details>

### 3. What is an ACL entry composed of?
- [ ] A) Topic and partition
- [ ] B) Principal, Resource, Operation, and Permission
- [ ] C) Username and password
- [ ] D) Broker ID and port

<details>
<summary>Answer</summary>
**B)** An ACL entry consists of: Principal (e.g., User:alice), Resource (e.g., Topic:orders), Operation (e.g., Read), and Permission (Allow/Deny).
</details>

## True/False

### 4. Once a topic is created, its partition count cannot be increased.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Partition count can be increased using the `AddPartitions` operation. However, note that partition count cannot be decreased, and adding partitions may affect message ordering for existing keys.
</details>

### 5. Quotas can limit the bytes per second that a producer can send.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Producer quotas limit bytes/sec that producers can send. This prevents a single client from overwhelming the cluster. Consumer quotas similarly limit fetch throughput.
</details>

## Code Challenge

### 6. You need to create a topic with the following requirements:
- Name: `orders`
- 12 partitions
- Replication factor: 3
- Retention: 30 days
- Cleanup policy: delete

What Admin API operations would you use?

<details>
<summary>Answer</summary>
You would use two operations:

1. **CreateTopic** to create the topic with partitions and replication:
```csharp
await adminClient.CreateTopicAsync(new TopicSpecification
{
    Name = "orders",
    NumPartitions = 12,
    ReplicationFactor = 3
});
```

2. **AlterConfigs** to set retention and cleanup policy:
```csharp
await adminClient.AlterConfigsAsync(new Dictionary<ConfigResource, Config>
{
    {
        new ConfigResource(ResourceType.Topic, "orders"),
        new Config
        {
            { "retention.ms", "2592000000" },  // 30 days in ms
            { "cleanup.policy", "delete" }
        }
    }
});
```

Or combine during creation with configs parameter if supported.
</details>

### 7. Create an ACL that allows user "orderservice" to write to the topic "orders" but denies delete operations.

<details>
<summary>Answer</summary>
You would create two ACL entries:

**Allow Write:**
```
Principal: User:orderservice
Resource: Topic:orders
Operation: Write
Permission: Allow
```

**Deny Delete:**
```
Principal: User:orderservice
Resource: Topic:orders
Operation: Delete
Permission: Deny
```

In code:
```csharp
await adminClient.CreateAclsAsync(new[]
{
    new AclBinding
    {
        Principal = "User:orderservice",
        Resource = new ResourcePattern(ResourceType.Topic, "orders"),
        Operation = AclOperation.Write,
        PermissionType = AclPermissionType.Allow
    },
    new AclBinding
    {
        Principal = "User:orderservice",
        Resource = new ResourcePattern(ResourceType.Topic, "orders"),
        Operation = AclOperation.Delete,
        PermissionType = AclPermissionType.Deny
    }
});
```

Note: Deny rules typically take precedence over Allow rules.
</details>

## Score
- 7/7: Excellent! You understand the Admin API
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
