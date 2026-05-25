# Lesson 12.5: Instructions

Follow these step-by-step instructions to implement integration patterns.

## Step 1: Create the Merge Operator

Combine multiple observables into one:

```csharp
public class MergeObservable<T> : IObservable<T>
{
    private readonly IObservable<T>[] _sources;

    public MergeObservable(params IObservable<T>[] sources)
    {
        _sources = sources;
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        var completedCount = 0;
        var lockObj = new object();
        var subscriptions = new List<IDisposable>();

        foreach (var source in _sources)
        {
            var sub = source.Subscribe(new AnonymousObserver<T>(
                observer.OnNext,
                observer.OnError,
                () =>
                {
                    lock (lockObj)
                    {
                        completedCount++;
                        if (completedCount >= _sources.Length)
                            observer.OnCompleted();
                    }
                }
            ));
            subscriptions.Add(sub);
        }

        return new CompositeDisposable(subscriptions.ToArray());
    }
}
```

## Step 2: Create a Timer Observable

Generate values at intervals:

```csharp
public class IntervalObservable : IObservable<long>
{
    private readonly TimeSpan _interval;
    private readonly int? _count;

    public IntervalObservable(TimeSpan interval, int? count = null)
    {
        _interval = interval;
        _count = count;
    }

    public IDisposable Subscribe(IObserver<long> observer)
    {
        var cts = new CancellationTokenSource();
        long tick = 0;

        Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(_interval, cts.Token);
                    observer.OnNext(tick++);

                    if (_count.HasValue && tick >= _count.Value)
                    {
                        observer.OnCompleted();
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        });

        return new Subscription(() => cts.Cancel());
    }
}
```

## Step 3: Create SelectMany for Async Operations

Flatten and chain async operations:

```csharp
public class SelectManyObservable<TSource, TResult> : IObservable<TResult>
{
    private readonly IObservable<TSource> _source;
    private readonly Func<TSource, IObservable<TResult>> _selector;

    public SelectManyObservable(
        IObservable<TSource> source,
        Func<TSource, IObservable<TResult>> selector)
    {
        _source = source;
        _selector = selector;
    }

    public IDisposable Subscribe(IObserver<TResult> observer)
    {
        var innerSubscriptions = new List<IDisposable>();
        var lockObj = new object();
        var outerCompleted = false;
        var activeCount = 0;

        var outerSub = _source.Subscribe(new AnonymousObserver<TSource>(
            value =>
            {
                lock (lockObj) activeCount++;

                var inner = _selector(value);
                var innerSub = inner.Subscribe(new AnonymousObserver<TResult>(
                    observer.OnNext,
                    observer.OnError,
                    () =>
                    {
                        lock (lockObj)
                        {
                            activeCount--;
                            if (outerCompleted && activeCount == 0)
                                observer.OnCompleted();
                        }
                    }
                ));

                lock (lockObj) innerSubscriptions.Add(innerSub);
            },
            observer.OnError,
            () =>
            {
                lock (lockObj)
                {
                    outerCompleted = true;
                    if (activeCount == 0)
                        observer.OnCompleted();
                }
            }
        ));

        return new CompositeDisposable(
            outerSub,
            new Subscription(() =>
            {
                lock (lockObj)
                {
                    foreach (var sub in innerSubscriptions)
                        sub.Dispose();
                }
            })
        );
    }
}
```

## Step 4: Create a Reactive Pipeline Builder

```csharp
public class ReactivePipeline<TInput, TOutput>
{
    private readonly IObservable<TInput> _source;
    private readonly Func<IObservable<TInput>, IObservable<TOutput>> _transform;

    public ReactivePipeline(
        IObservable<TInput> source,
        Func<IObservable<TInput>, IObservable<TOutput>> transform)
    {
        _source = source;
        _transform = transform;
    }

    public IObservable<TOutput> Build() => _transform(_source);
}
```

## Step 5: Create FromAsync Helper

Convert async methods to observables:

```csharp
public class FromAsyncObservable<T> : IObservable<T>
{
    private readonly Func<CancellationToken, Task<T>> _factory;

    public FromAsyncObservable(Func<CancellationToken, Task<T>> factory)
    {
        _factory = factory;
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        var cts = new CancellationTokenSource();

        Task.Run(async () =>
        {
            try
            {
                var result = await _factory(cts.Token);
                if (!cts.Token.IsCancellationRequested)
                {
                    observer.OnNext(result);
                    observer.OnCompleted();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        });

        return new Subscription(() => cts.Cancel());
    }
}
```

## Step 6: Add Extension Methods

```csharp
public static class IntegrationExtensions
{
    public static IObservable<T> Merge<T>(params IObservable<T>[] sources)
        => new MergeObservable<T>(sources);

    public static IObservable<long> Interval(TimeSpan interval, int? count = null)
        => new IntervalObservable(interval, count);

    public static IObservable<TResult> SelectMany<TSource, TResult>(
        this IObservable<TSource> source,
        Func<TSource, IObservable<TResult>> selector)
        => new SelectManyObservable<TSource, TResult>(source, selector);

    public static IObservable<T> FromAsync<T>(Func<CancellationToken, Task<T>> factory)
        => new FromAsyncObservable<T>(factory);
}
```

## Step 7: Build a Complete Pipeline

```csharp
// Merge multiple Surgewave topics
var ordersStream = new SurgewaveObservable<string, string>(server, "orders", group);
var paymentsStream = new SurgewaveObservable<string, string>(server, "payments", group);

var mergedStream = Merge(ordersStream, paymentsStream);

// Process with enrichment
mergedStream
    .Where(msg => msg.Value != null)
    .Select(msg => ParseEvent(msg))
    .SelectMany(evt => EnrichWithUserData(evt))  // Async enrichment
    .Where(evt => evt.IsValid)
    .Subscribe(evt => ProcessEvent(evt));
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. When would you use Merge vs SelectMany?
2. How do you handle errors in merged streams?
3. What's the lifecycle of subscriptions in SelectMany?
4. How would you implement a timeout for async enrichment?

---

**Congratulations!** You've completed Unit 12: Reactive Integration!
