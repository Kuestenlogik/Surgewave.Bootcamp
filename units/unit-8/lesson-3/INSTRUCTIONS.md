# Lesson 12.3: Instructions

Follow these step-by-step instructions to implement reactive operators.

## Step 1: Create the Operator Base Classes

First, set up helper classes for building operators:

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

public class AnonymousObserver<T> : IObserver<T>
{
    private readonly Action<T> _onNext;
    private readonly Action<Exception> _onError;
    private readonly Action _onCompleted;

    public AnonymousObserver(
        Action<T> onNext,
        Action<Exception>? onError = null,
        Action? onCompleted = null)
    {
        _onNext = onNext;
        _onError = onError ?? (ex => { });
        _onCompleted = onCompleted ?? (() => { });
    }

    public void OnNext(T value) => _onNext(value);
    public void OnError(Exception error) => _onError(error);
    public void OnCompleted() => _onCompleted();
}
```

## Step 2: Implement the Where Operator

Create a filter operator:

```csharp
public class WhereObservable<T> : IObservable<T>
{
    private readonly IObservable<T> _source;
    private readonly Func<T, bool> _predicate;

    public WhereObservable(IObservable<T> source, Func<T, bool> predicate)
    {
        _source = source;
        _predicate = predicate;
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        return _source.Subscribe(new AnonymousObserver<T>(
            value =>
            {
                if (_predicate(value))
                    observer.OnNext(value);
            },
            observer.OnError,
            observer.OnCompleted
        ));
    }
}
```

## Step 3: Implement the Select Operator

Create a transformation operator:

```csharp
public class SelectObservable<TSource, TResult> : IObservable<TResult>
{
    private readonly IObservable<TSource> _source;
    private readonly Func<TSource, TResult> _selector;

    public SelectObservable(IObservable<TSource> source, Func<TSource, TResult> selector)
    {
        _source = source;
        _selector = selector;
    }

    public IDisposable Subscribe(IObserver<TResult> observer)
    {
        return _source.Subscribe(new AnonymousObserver<TSource>(
            value => observer.OnNext(_selector(value)),
            observer.OnError,
            observer.OnCompleted
        ));
    }
}
```

## Step 4: Implement the Take Operator

Create an operator that completes after N elements:

```csharp
public class TakeObservable<T> : IObservable<T>
{
    private readonly IObservable<T> _source;
    private readonly int _count;

    public TakeObservable(IObservable<T> source, int count)
    {
        _source = source;
        _count = count;
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        int remaining = _count;
        IDisposable? subscription = null;

        subscription = _source.Subscribe(new AnonymousObserver<T>(
            value =>
            {
                if (remaining > 0)
                {
                    remaining--;
                    observer.OnNext(value);
                    if (remaining == 0)
                    {
                        observer.OnCompleted();
                        subscription?.Dispose();
                    }
                }
            },
            observer.OnError,
            observer.OnCompleted
        ));

        return subscription;
    }
}
```

## Step 5: Implement the Buffer Operator

Create a batching operator:

```csharp
public class BufferObservable<T> : IObservable<IList<T>>
{
    private readonly IObservable<T> _source;
    private readonly int _count;

    public BufferObservable(IObservable<T> source, int count)
    {
        _source = source;
        _count = count;
    }

    public IDisposable Subscribe(IObserver<IList<T>> observer)
    {
        var buffer = new List<T>();

        return _source.Subscribe(new AnonymousObserver<T>(
            value =>
            {
                buffer.Add(value);
                if (buffer.Count >= _count)
                {
                    observer.OnNext(buffer.ToList());
                    buffer.Clear();
                }
            },
            observer.OnError,
            () =>
            {
                if (buffer.Count > 0)
                    observer.OnNext(buffer.ToList());
                observer.OnCompleted();
            }
        ));
    }
}
```

## Step 6: Create Extension Methods

Build a fluent API:

```csharp
public static class ObservableExtensions
{
    public static IObservable<T> Where<T>(
        this IObservable<T> source,
        Func<T, bool> predicate)
        => new WhereObservable<T>(source, predicate);

    public static IObservable<TResult> Select<TSource, TResult>(
        this IObservable<TSource> source,
        Func<TSource, TResult> selector)
        => new SelectObservable<TSource, TResult>(source, selector);

    public static IObservable<T> Take<T>(
        this IObservable<T> source,
        int count)
        => new TakeObservable<T>(source, count);

    public static IObservable<IList<T>> Buffer<T>(
        this IObservable<T> source,
        int count)
        => new BufferObservable<T>(source, count);

    public static IDisposable Subscribe<T>(
        this IObservable<T> source,
        Action<T> onNext,
        Action<Exception>? onError = null,
        Action? onCompleted = null)
        => source.Subscribe(new AnonymousObserver<T>(onNext, onError, onCompleted));
}
```

## Step 7: Use with Surgewave Messages

```csharp
surgewaveObservable
    .Where(msg => !string.IsNullOrEmpty(msg.Value))
    .Select(msg => new { msg.Key, Value = msg.Value?.ToUpperInvariant() })
    .Take(5)
    .Subscribe(
        msg => Console.WriteLine($"Received: {msg.Key} -> {msg.Value}"),
        ex => Console.WriteLine($"Error: {ex.Message}"),
        () => Console.WriteLine("Completed!")
    );
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. Why do operators return new IObservable instances instead of modifying the source?
2. What happens when OnError is called in a pipeline?
3. How does Buffer handle completion when the buffer isn't full?
4. Why might you want a Skip operator in addition to Take?

---

**Next:** [Lesson 12.4: Backpressure](../lesson-4/README.md)
