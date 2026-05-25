# Knowledge Assessment: Lesson 12.2 - Surgewave as Observable

## Multiple Choice

### 1. What is the main benefit of converting a Surgewave consumer to an IObservable?
- [ ] A) It makes the consumer faster
- [ ] B) It enables push-based notification and operator chaining
- [ ] C) It reduces memory usage
- [ ] D) It eliminates the need for serialization

<details>
<summary>Answer</summary>
**B)** Converting to IObservable enables push-based notification instead of manual polling, and allows fluent operator chaining (Where, Select, etc.) for powerful data transformations.
</details>

### 2. In a SurgewaveObservable wrapper, what should be returned from the Subscribe method?
- [ ] A) The consumer instance
- [ ] B) A Task representing the consume loop
- [ ] C) An IDisposable that cancels consumption and disposes resources
- [ ] D) The CancellationTokenSource

<details>
<summary>Answer</summary>
**C)** Subscribe must return an IDisposable that, when disposed, cancels the consume loop (via CancellationTokenSource) and disposes the consumer to properly release resources.
</details>

### 3. Why does the SurgewaveObservable example use a consumer factory instead of accepting a consumer directly?
- [ ] A) Factories are faster than direct instances
- [ ] B) Each subscription should have its own consumer for proper isolation
- [ ] C) Consumers cannot be passed as constructor parameters
- [ ] D) It is required by the IObservable interface

<details>
<summary>Answer</summary>
**B)** Using a factory allows each subscription to create its own consumer instance, ensuring proper isolation. This supports multiple subscribers without them interfering with each other's consumption.
</details>

## True/False

### 4. When converting Surgewave consumers to observables, the consume loop should run on a background thread or task.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The consume loop should run on a background thread (via Task.Run or similar) to avoid blocking the Subscribe call. This allows the caller to continue executing while messages are consumed asynchronously.
</details>

### 5. Errors during message consumption should be silently swallowed to prevent the observable from terminating.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Errors should be propagated via OnError to properly notify subscribers. Silently swallowing errors can hide problems and lead to data loss. Subscribers can implement their own error handling strategies.
</details>

## Code Challenge

### 6. What resources are NOT properly managed in this SurgewaveObservable implementation?

```csharp
public class SurgewaveObservable<TKey, TValue> : IObservable<ConsumeResult<TKey, TValue>>
{
    private readonly SurgewaveConsumer<TKey, TValue> _consumer;

    public SurgewaveObservable(SurgewaveConsumer<TKey, TValue> consumer)
    {
        _consumer = consumer;
    }

    public IDisposable Subscribe(IObserver<ConsumeResult<TKey, TValue>> observer)
    {
        var cts = new CancellationTokenSource();

        Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var result = await _consumer.ConsumeAsync(TimeSpan.FromSeconds(1));
                if (result != null)
                    observer.OnNext(result);
            }
        });

        return new Subscription(() => cts.Cancel());
    }
}
```

<details>
<summary>Answer</summary>
Issues with resource management:
1. The consumer is shared across subscriptions instead of creating a new one per subscription
2. The consumer is not disposed when the subscription is disposed
3. Exceptions from ConsumeAsync are not caught and forwarded to OnError
4. OnCompleted is never called

Fix:
```csharp
public IDisposable Subscribe(IObserver<ConsumeResult<TKey, TValue>> observer)
{
    var cts = new CancellationTokenSource();
    var consumer = _consumerFactory(); // Use factory pattern

    Task.Run(async () =>
    {
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(1));
                if (result != null)
                    observer.OnNext(result);
            }
            observer.OnCompleted();
        }
        catch (Exception ex) when (!cts.Token.IsCancellationRequested)
        {
            observer.OnError(ex);
        }
    });

    return new CompositeDisposable(
        new Subscription(() => cts.Cancel()),
        consumer // Dispose consumer when subscription ends
    );
}
```
</details>

### 7. Why is it important to check `result != null` before calling OnNext?

<details>
<summary>Answer</summary>
The ConsumeAsync method may return null when:
1. The timeout expires without receiving a message
2. The topic/partition has no new messages

Calling OnNext with null could:
1. Violate the contract of some operators that expect non-null values
2. Cause NullReferenceException in downstream processing
3. Pollute the observable stream with meaningless null values

Only meaningful, non-null consume results should be pushed to observers.
</details>

## Score
- 7/7: Excellent! You understand how to create observable wrappers for Surgewave
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding
