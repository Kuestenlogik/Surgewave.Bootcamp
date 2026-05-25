# Knowledge Assessment: Lesson 3.4 - Transactions

## Multiple Choice

### 1. What does a transaction guarantee in Surgewave?
- [ ] A) Messages are delivered faster
- [ ] B) All messages in the transaction succeed or none do (atomicity)
- [ ] C) Messages are encrypted
- [ ] D) Messages are compressed

<details>
<summary>Answer</summary>
**B)** All messages in the transaction succeed or none do (atomicity). Transactions ensure that multiple produce operations are treated as a single unit - either all are committed or all are rolled back.
</details>

### 2. What is required to enable transactional producing?
- [ ] A) Setting `EnableTransactions = true`
- [ ] B) Setting a unique `TransactionalId` and calling `InitTransactionsAsync()`
- [ ] C) Using a special transaction topic
- [ ] D) Connecting to multiple brokers

<details>
<summary>Answer</summary>
**B)** Setting a unique `TransactionalId` and calling `InitTransactionsAsync()`. The TransactionalId identifies the producer for transaction recovery, and InitTransactionsAsync prepares the producer for transactions.
</details>

### 3. What happens if an exception occurs during a transaction?
- [ ] A) Partial messages are committed
- [ ] B) The transaction should be aborted and no messages are visible
- [ ] C) Messages are automatically retried
- [ ] D) The broker crashes

<details>
<summary>Answer</summary>
**B)** The transaction should be aborted and no messages are visible. When you call `AbortTransactionAsync()`, all messages produced in that transaction are discarded.
</details>

### 4. In the consume-transform-produce pattern, what must happen atomically?
- [ ] A) Only the produce operations
- [ ] B) The consume, transformation, and produce operations
- [ ] C) Only the offset commit
- [ ] D) The connection and disconnection

<details>
<summary>Answer</summary>
**B)** The consume, transformation, and produce operations. The entire flow must be atomic to ensure exactly-once semantics - if the produce fails, the offset shouldn't be committed.
</details>

## True/False

### 5. Each transactional producer must have a unique TransactionalId.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The TransactionalId uniquely identifies a producer for transaction coordination and recovery. Multiple producers with the same ID would cause conflicts.
</details>

### 6. If you don't call CommitTransactionAsync(), messages are automatically committed after a timeout.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Transactions must be explicitly committed or aborted. Without CommitTransactionAsync(), the transaction will eventually timeout and be aborted by the broker.
</details>

## Code Challenge

### 7. Complete this transactional producer code:

```csharp
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    opt.____________ = "order-processor-1";  // Required for transactions
});

// Initialize transaction support
await producer.____________();

// Start a transaction
await producer.____________();

try
{
    // Produce to multiple topics atomically
    await producer.ProduceAsync("orders-processed", "ord-1", "Order processed");
    await producer.ProduceAsync("inventory-updates", "inv-1", "Stock reduced");
    await producer.ProduceAsync("notifications", "notif-1", "Customer notified");

    // Commit all messages
    await producer.____________();
    Console.WriteLine("Transaction committed successfully!");
}
catch (Exception ex)
{
    // Rollback on failure
    await producer.____________();
    Console.WriteLine($"Transaction aborted: {ex.Message}");
    throw;
}
```

<details>
<summary>Answer</summary>
```csharp
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    opt.TransactionalId = "order-processor-1";  // Required for transactions
});

// Initialize transaction support
await producer.InitTransactionsAsync();

// Start a transaction
await producer.BeginTransactionAsync();

try
{
    // Produce to multiple topics atomically
    await producer.ProduceAsync("orders-processed", "ord-1", "Order processed");
    await producer.ProduceAsync("inventory-updates", "inv-1", "Stock reduced");
    await producer.ProduceAsync("notifications", "notif-1", "Customer notified");

    // Commit all messages
    await producer.CommitTransactionAsync();
    Console.WriteLine("Transaction committed successfully!");
}
catch (Exception ex)
{
    // Rollback on failure
    await producer.AbortTransactionAsync();
    Console.WriteLine($"Transaction aborted: {ex.Message}");
    throw;
}
```

Key transaction lifecycle:
1. Set `TransactionalId` - unique identifier for this producer
2. `InitTransactionsAsync()` - one-time initialization
3. `BeginTransactionAsync()` - start transaction scope
4. Produce messages - all within the transaction
5. `CommitTransactionAsync()` - make messages visible
6. `AbortTransactionAsync()` - discard messages on failure
</details>

## Score
- 7/7: Excellent! You understand transactional messaging
- 5-6/7: Good grasp of transaction concepts
- 3-4/7: Review the transaction lifecycle
- <3/7: Re-do the lesson focusing on commit/abort
