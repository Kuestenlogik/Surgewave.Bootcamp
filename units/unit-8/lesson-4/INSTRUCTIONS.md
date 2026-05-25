# Lesson 12.4: Instructions

Follow these step-by-step instructions to implement backpressure handling.

## Step 1: Define Overflow Policies

Create an enum for different overflow behaviors:

```csharp
public enum OverflowPolicy
{
    DropOldest,    // Remove oldest item when full
    DropNewest,    // Ignore new items when full
    Block,         // Block producer until space available
    ThrowException // Throw when buffer is full
}
```

## Step 2: Create a Bounded Buffer

Implement a buffer with configurable overflow handling:

```csharp
public class BoundedBuffer<T>
{
    private readonly Queue<T> _queue = new();
    private readonly int _capacity;
    private readonly OverflowPolicy _policy;
    private readonly object _lock = new();

    public BoundedBuffer(int capacity, OverflowPolicy policy = OverflowPolicy.DropOldest)
    {
        _capacity = capacity;
        _policy = policy;
    }

    public int Count
    {
        get { lock (_lock) return _queue.Count; }
    }

    public bool TryAdd(T item)
    {
        lock (_lock)
        {
            if (_queue.Count >= _capacity)
            {
                switch (_policy)
                {
                    case OverflowPolicy.DropOldest:
                        _queue.Dequeue();
                        break;
                    case OverflowPolicy.DropNewest:
                        return false;
                    case OverflowPolicy.ThrowException:
                        throw new InvalidOperationException("Buffer overflow");
                }
            }
            _queue.Enqueue(item);
            return true;
        }
    }

    public bool TryTake(out T? item)
    {
        lock (_lock)
        {
            if (_queue.Count > 0)
            {
                item = _queue.Dequeue();
                return true;
            }
            item = default;
            return false;
        }
    }
}
```

## Step 3: Create a Buffered Observable

Wrap an observable with bounded buffering:

```csharp
public class BufferedObservable<T> : IObservable<T>
{
    private readonly IObservable<T> _source;
    private readonly int _bufferSize;
    private readonly OverflowPolicy _policy;

    public BufferedObservable(IObservable<T> source, int bufferSize, OverflowPolicy policy)
    {
        _source = source;
        _bufferSize = bufferSize;
        _policy = policy;
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        var buffer = new BoundedBuffer<T>(_bufferSize, _policy);
        var cts = new CancellationTokenSource();

        // Consumer task - drains buffer at controlled rate
        var consumerTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (buffer.TryTake(out var item))
                {
                    observer.OnNext(item!);
                }
                else
                {
                    await Task.Delay(10, cts.Token);
                }
            }
        });

        // Subscribe to source and add to buffer
        var subscription = _source.Subscribe(new AnonymousObserver<T>(
            value => buffer.TryAdd(value),
            observer.OnError,
            observer.OnCompleted
        ));

        return new CompositeDisposable(
            subscription,
            new Subscription(() => cts.Cancel())
        );
    }
}
```

## Step 4: Implement the Sample Operator

Sample emits only the latest value at intervals:

```csharp
public class SampleObservable<T> : IObservable<T>
{
    private readonly IObservable<T> _source;
    private readonly TimeSpan _interval;

    public SampleObservable(IObservable<T> source, TimeSpan interval)
    {
        _source = source;
        _interval = interval;
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        T? latestValue = default;
        bool hasValue = false;
        var cts = new CancellationTokenSource();
        var lockObj = new object();

        // Timer task - emits sampled value
        var timerTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                await Task.Delay(_interval, cts.Token);

                lock (lockObj)
                {
                    if (hasValue)
                    {
                        observer.OnNext(latestValue!);
                        hasValue = false;
                    }
                }
            }
        });

        var subscription = _source.Subscribe(new AnonymousObserver<T>(
            value =>
            {
                lock (lockObj)
                {
                    latestValue = value;
                    hasValue = true;
                }
            },
            observer.OnError,
            observer.OnCompleted
        ));

        return new CompositeDisposable(
            subscription,
            new Subscription(() => cts.Cancel())
        );
    }
}
```

## Step 5: Implement the Throttle Operator

Throttle limits how often values can pass through:

```csharp
public class ThrottleObservable<T> : IObservable<T>
{
    private readonly IObservable<T> _source;
    private readonly TimeSpan _interval;

    public ThrottleObservable(IObservable<T> source, TimeSpan interval)
    {
        _source = source;
        _interval = interval;
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        DateTime lastEmit = DateTime.MinValue;
        var lockObj = new object();

        return _source.Subscribe(new AnonymousObserver<T>(
            value =>
            {
                lock (lockObj)
                {
                    var now = DateTime.UtcNow;
                    if (now - lastEmit >= _interval)
                    {
                        lastEmit = now;
                        observer.OnNext(value);
                    }
                    // Otherwise drop the value
                }
            },
            observer.OnError,
            observer.OnCompleted
        ));
    }
}
```

## Step 6: Add Extension Methods

```csharp
public static class BackpressureExtensions
{
    public static IObservable<T> WithBuffer<T>(
        this IObservable<T> source,
        int size,
        OverflowPolicy policy = OverflowPolicy.DropOldest)
        => new BufferedObservable<T>(source, size, policy);

    public static IObservable<T> Sample<T>(
        this IObservable<T> source,
        TimeSpan interval)
        => new SampleObservable<T>(source, interval);

    public static IObservable<T> Throttle<T>(
        this IObservable<T> source,
        TimeSpan interval)
        => new ThrottleObservable<T>(source, interval);
}
```

## Step 7: Test with High-Volume Stream

```csharp
// Fast producer - 100 msgs in 1 second
var fastSource = new FastProducerObservable(count: 100, delayMs: 10);

// Without backpressure - consumer overwhelmed
fastSource.Subscribe(x => {
    Thread.Sleep(50); // Slow processing
    Console.WriteLine($"Processed: {x}");
});

// With sampling - only process latest per 200ms
fastSource
    .Sample(TimeSpan.FromMilliseconds(200))
    .Subscribe(x => Console.WriteLine($"Sampled: {x}"));

// With throttle - at most 1 per 200ms
fastSource
    .Throttle(TimeSpan.FromMilliseconds(200))
    .Subscribe(x => Console.WriteLine($"Throttled: {x}"));
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. When would you use Sample vs Throttle?
2. What are the tradeoffs of different overflow policies?
3. How does backpressure affect message ordering?
4. When is data loss acceptable?

---

**Next:** [Lesson 12.5: Integration Patterns](../lesson-5/README.md)
