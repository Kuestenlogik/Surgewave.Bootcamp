# Lesson 12.1: Instructions

Follow these step-by-step instructions to learn reactive programming basics.

## Step 1: Understand IObserver<T>

First, implement a simple observer that handles incoming values:

```csharp
public class ConsoleObserver<T> : IObserver<T>
{
    private readonly string _name;

    public ConsoleObserver(string name) => _name = name;

    public void OnNext(T value)
    {
        Console.WriteLine($"[{_name}] Received: {value}");
    }

    public void OnError(Exception error)
    {
        Console.WriteLine($"[{_name}] Error: {error.Message}");
    }

    public void OnCompleted()
    {
        Console.WriteLine($"[{_name}] Completed");
    }
}
```

## Step 2: Implement IObservable<T>

Create a simple observable that emits a sequence of numbers:

```csharp
public class NumberSequence : IObservable<int>
{
    private readonly int _start;
    private readonly int _count;

    public NumberSequence(int start, int count)
    {
        _start = start;
        _count = count;
    }

    public IDisposable Subscribe(IObserver<int> observer)
    {
        for (int i = 0; i < _count; i++)
        {
            observer.OnNext(_start + i);
        }
        observer.OnCompleted();

        return new EmptyDisposable();
    }
}
```

## Step 3: Create a Proper Subscription

Implement a disposable subscription that supports cancellation:

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
```

## Step 4: Create an Async Observable

Build an observable that emits values asynchronously:

```csharp
public class AsyncNumberSequence : IObservable<int>
{
    private readonly int _count;
    private readonly int _delayMs;

    public AsyncNumberSequence(int count, int delayMs)
    {
        _count = count;
        _delayMs = delayMs;
    }

    public IDisposable Subscribe(IObserver<int> observer)
    {
        var cts = new CancellationTokenSource();

        Task.Run(async () =>
        {
            try
            {
                for (int i = 0; i < _count && !cts.Token.IsCancellationRequested; i++)
                {
                    await Task.Delay(_delayMs, cts.Token);
                    observer.OnNext(i);
                }
                if (!cts.Token.IsCancellationRequested)
                    observer.OnCompleted();
            }
            catch (OperationCanceledException)
            {
                // Subscription was disposed
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        });

        return new Subscription(() => cts.Cancel());
    }
}
```

## Step 5: Implement Subject<T>

Create a Subject that acts as both observable and observer:

```csharp
public class Subject<T> : IObservable<T>, IObserver<T>, IDisposable
{
    private readonly List<IObserver<T>> _observers = new();
    private bool _completed;
    private Exception? _error;

    public IDisposable Subscribe(IObserver<T> observer)
    {
        if (_error != null)
        {
            observer.OnError(_error);
            return new EmptyDisposable();
        }
        if (_completed)
        {
            observer.OnCompleted();
            return new EmptyDisposable();
        }

        _observers.Add(observer);
        return new Subscription(() => _observers.Remove(observer));
    }

    public void OnNext(T value)
    {
        foreach (var observer in _observers.ToArray())
            observer.OnNext(value);
    }

    public void OnError(Exception error)
    {
        _error = error;
        foreach (var observer in _observers.ToArray())
            observer.OnError(error);
        _observers.Clear();
    }

    public void OnCompleted()
    {
        _completed = true;
        foreach (var observer in _observers.ToArray())
            observer.OnCompleted();
        _observers.Clear();
    }

    public void Dispose() => _observers.Clear();
}
```

## Step 6: Test Your Implementation

```csharp
// Synchronous observable
var numbers = new NumberSequence(1, 5);
numbers.Subscribe(new ConsoleObserver<int>("Sync"));

// Async observable with early disposal
var asyncNumbers = new AsyncNumberSequence(10, 100);
var subscription = asyncNumbers.Subscribe(new ConsoleObserver<int>("Async"));
await Task.Delay(350);
subscription.Dispose(); // Cancel after ~3 values

// Subject for push model
var subject = new Subject<string>();
subject.Subscribe(new ConsoleObserver<string>("Sub1"));
subject.Subscribe(new ConsoleObserver<string>("Sub2"));
subject.OnNext("Hello");
subject.OnNext("World");
subject.OnCompleted();
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. What's the difference between OnCompleted() and Dispose()?
2. Why do we need to copy `_observers` to an array before iterating?
3. How does Subject<T> differ from a regular IObservable<T>?
4. What happens if OnError is called?

---

**Next:** [Lesson 12.2: Surgewave as Observable](../lesson-2/README.md)
