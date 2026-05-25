# Knowledge Assessment: Lesson 4.2 - Replication

## Multiple Choice

### 1. What does a replication factor of 3 mean?
- [ ] A) Data is stored on 3 partitions
- [ ] B) There are 3 copies of each partition across brokers
- [ ] C) The topic has 3 partitions
- [ ] D) Messages are sent 3 times

<details>
<summary>Answer</summary>
**B)** Replication factor (RF) is the number of copies of each partition. RF=3 means each partition has 3 copies stored on different brokers, allowing the cluster to survive up to 2 broker failures.
</details>

### 2. What is the In-Sync Replica (ISR) set?
- [ ] A) All replicas in the cluster
- [ ] B) Replicas that are caught up with the leader
- [ ] C) Replicas that have never failed
- [ ] D) The minimum number of replicas required

<details>
<summary>Answer</summary>
**B)** ISR is the set of replicas that are caught up with the leader. All replicas start in ISR, but slow replicas are removed. Writes only acknowledge when all ISR replicas have the data.
</details>

### 3. With `min.insync.replicas=2` and `RF=3`, what happens if only 1 replica is available?
- [ ] A) Writes succeed normally
- [ ] B) Writes are queued until more replicas are available
- [ ] C) Writes fail
- [ ] D) Data is written to disk but not replicated

<details>
<summary>Answer</summary>
**C)** Writes fail when ISR drops below `min.insync.replicas`. With min.insync.replicas=2, at least 2 replicas must be available for writes to succeed, preventing data loss even with Acks.All.
</details>

## True/False

### 4. RF=1 is recommended for production environments.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - RF=1 provides no redundancy and should only be used for development. RF=3 is recommended for production because it can tolerate 2 broker failures while maintaining data durability.
</details>

### 5. When a replica falls behind the leader, it is automatically removed from the ISR set.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Slow replicas that fall behind the leader are automatically removed from the ISR set. They can rejoin once they catch up with the leader again.
</details>

## Code Challenge

### 6. You have a topic with RF=3 and min.insync.replicas=2. Currently ISR has all 3 replicas. If one broker fails, what happens to:
- a) Read operations
- b) Write operations with Acks.All

<details>
<summary>Answer</summary>
When one broker fails with RF=3 and min.insync.replicas=2:

a) **Read operations**: Continue normally. Reads can be served by any in-sync replica, and 2 replicas remain available.

b) **Write operations with Acks.All**: Continue normally. With 2 replicas still in ISR, writes meet the min.insync.replicas=2 requirement. The write is acknowledged when both remaining replicas have the data.

If a second broker fails (only 1 replica left), writes would fail because ISR (1) < min.insync.replicas (2).
</details>

### 7. What is wrong with this replication configuration for a production environment?

```
Topic: orders
Replication Factor: 2
min.insync.replicas: 2
Number of brokers: 2
```

<details>
<summary>Answer</summary>
The issues are:

1. **No fault tolerance for writes**: With RF=2 and min.insync.replicas=2, if ANY broker fails, writes will fail because ISR will drop to 1, which is less than min.insync.replicas.

2. **RF=3 recommended**: Production should use RF=3 with at least 3 brokers to tolerate 1 broker failure while still accepting writes (if min.insync.replicas=2).

Recommended configuration:
```
Topic: orders
Replication Factor: 3
min.insync.replicas: 2
Number of brokers: 3+
```

This allows 1 broker to fail while writes continue (ISR=2 >= min.insync.replicas=2).
</details>

## Score
- 7/7: Excellent! You understand replication fundamentals
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
