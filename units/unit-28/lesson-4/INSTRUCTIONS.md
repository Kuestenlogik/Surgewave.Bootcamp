# Lesson 3.4: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Create Transactional Producer

```csharp
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.TransactionalId = "order-processor";
});

// Initialize transactions (required once)
await producer.InitTransactionsAsync();
```

## Step 2: Implement Atomic Multi-Topic Produce

```csharp
async Task ProcessOrderTransactionally(Order order)
{
    await producer.BeginTransactionAsync();

    try
    {
        // All these writes are atomic
        await producer.ProduceAsync("orders", order.Id, order.ToJson());
        await producer.ProduceAsync("inventory", order.ProductId, $"-{order.Quantity}");
        await producer.ProduceAsync("notifications", order.CustomerId, "Order placed");

        await producer.CommitTransactionAsync();
        Console.WriteLine($"Transaction committed for order {order.Id}");
    }
    catch (Exception ex)
    {
        await producer.AbortTransactionAsync();
        Console.WriteLine($"Transaction aborted: {ex.Message}");
        throw;
    }
}
```

## Step 3: Consume-Transform-Produce Pattern

```csharp
// Read from input topic
var input = await consumer.ConsumeAsync(timeout);

await producer.BeginTransactionAsync();
try
{
    // Transform
    var output = Transform(input.Value);

    // Produce to output topic
    await producer.ProduceAsync("output-topic", input.Key, output);

    // Commit consumer offsets as part of transaction
    await producer.SendOffsetsToTransactionAsync(
        consumer.GetOffsets(),
        consumer.GroupId);

    await producer.CommitTransactionAsync();
}
catch
{
    await producer.AbortTransactionAsync();
    throw;
}
```

## Step 4: Handle Failures

```csharp
public async Task ProcessWithTransactionRetry(string key, string value)
{
    const int maxRetries = 3;

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await producer.BeginTransactionAsync();
            await producer.ProduceAsync("topic1", key, value);
            await producer.ProduceAsync("topic2", key, value);
            await producer.CommitTransactionAsync();
            return;
        }
        catch (Exception ex)
        {
            await producer.AbortTransactionAsync();

            if (attempt == maxRetries)
                throw;

            Console.WriteLine($"Attempt {attempt} failed, retrying...");
            await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt));
        }
    }
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. What happens to messages if a transaction is aborted?
2. When should you use transactions vs regular produces?
3. How do you include consumer offset commits in a transaction?

---

**Next:** → [Lesson 3.5: Exactly-Once Semantics](../lesson-5/README.md)
