# Lesson 12.2: Instructions

Follow these step-by-step instructions to convert Surgewave consumers to observables.

## Step 1: Create Helper Classes

First, set up the reactive infrastructure:

```csharp
public class Subscription : IDisposable
{
    private readonly Action _onDispose;
    private bool _disposed;

    public Subscription(Action onDispose) => _onDispose = onDispose;

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _onDispose?.Invoke();
        }
    }
}

public class CompositeDisposable : IDisposable
{
    private readonly IDisposable[] _disposables;

    public CompositeDisposable(params IDisposable[] disposables)
        => _disposables = disposables;

    public void Dispose()
    {
        foreach (var d in _disposables)
            d?.Dispose();
    }
}
```

## Step 2: Create the Consumer Observable Wrapper

Build the core wrapper that converts a consumer to IObservable:

```csharp
public class SurgewaveObservable<TKey, TValue> : IObservable<ConsumeResult<TKey, TValue>>
{
    private readonly string _bootstrapServers;
    private readonly string _topic;
    private readonly string _groupId;

    public SurgewaveObservable(string bootstrapServers, string topic, string groupId)
    {
        _bootstrapServers = bootstrapServers;
        _topic = topic;
        _groupId = groupId;
    }

    public IDisposable Subscribe(IObserver<ConsumeResult<TKey, TValue>> observer)
    {
        var cts = new CancellationTokenSource();

        var consumer = new SurgewaveConsumer<TKey, TValue>(options =>
        {
            options.BootstrapServers = _bootstrapServers;
            options.GroupId = _groupId;
            options.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        consumer.Subscribe(_topic);

        Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(1));
                    if (result != null && !cts.Token.IsCancellationRequested)
                    {
                        observer.OnNext(result);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during disposal
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        }, cts.Token);

        return new CompositeDisposable(
            new Subscription(() => cts.Cancel()),
            consumer
        );
    }
}
```

## Step 3: Add a Factory Method

Create a convenient extension method:

```csharp
public static class SurgewaveReactiveExtensions
{
    public static IObservable<ConsumeResult<TKey, TValue>> ToObservable<TKey, TValue>(
        this string topic,
        string bootstrapServers,
        string groupId)
    {
        return new SurgewaveObservable<TKey, TValue>(bootstrapServers, topic, groupId);
    }
}
```

## Step 4: Create an Observer for Testing

Build an observer that logs messages:

```csharp
public class MessageObserver<TKey, TValue> : IObserver<ConsumeResult<TKey, TValue>>
{
    private readonly string _name;
    private int _count;

    public MessageObserver(string name) => _name = name;
    public int Count => _count;

    public void OnNext(ConsumeResult<TKey, TValue> result)
    {
        _count++;
        Console.WriteLine($"  [{_name}] Message {_count}: Key={result.Key}, Value={result.Value}");
    }

    public void OnError(Exception error)
    {
        Console.WriteLine($"  [{_name}] Error: {error.Message}");
    }

    public void OnCompleted()
    {
        Console.WriteLine($"  [{_name}] Completed after {_count} messages");
    }
}
```

## Step 5: Produce Test Messages

Set up a producer to generate test data:

```csharp
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{port}";
});

for (int i = 1; i <= 10; i++)
{
    await producer.ProduceAsync("test-topic", $"key-{i}", $"message-{i}");
}
```

## Step 6: Subscribe and Consume Reactively

```csharp
var observable = new SurgewaveObservable<string, string>(
    $"localhost:{port}",
    "test-topic",
    "reactive-group"
);

var observer = new MessageObserver<string, string>("Consumer1");
using var subscription = observable.Subscribe(observer);

await Task.Delay(2000); // Let it consume

subscription.Dispose(); // Clean shutdown
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. Why does each subscription create its own consumer?
2. What happens if we don't dispose the subscription?
3. How could you add support for OnCompleted (finite streams)?
4. What's the benefit of using CompositeDisposable?

---

**Next:** [Lesson 12.3: Rx Operators](../lesson-3/README.md)
