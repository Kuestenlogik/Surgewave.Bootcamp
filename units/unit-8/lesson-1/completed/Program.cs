// Surgewave Bootcamp - Unit 12, Lesson 1: Reactive Basics
// COMPLETED SOLUTION

Console.WriteLine("=== Surgewave Bootcamp: Lesson 12.1 - Reactive Basics ===");
Console.WriteLine();

Console.WriteLine("--- Testing Synchronous Observable ---");
var numbers = new NumberSequence(1, 5);
numbers.Subscribe(new ConsoleObserver<int>("Sync"));

Console.WriteLine();
Console.WriteLine("--- Testing Async Observable with Cancellation ---");
var asyncNumbers = new AsyncNumberSequence(10, 100);
var subscription = asyncNumbers.Subscribe(new ConsoleObserver<int>("Async"));
await Task.Delay(350);
Console.WriteLine("  Disposing subscription after ~3 values...");
subscription.Dispose();
await Task.Delay(100); // Wait for cancellation message

Console.WriteLine();
Console.WriteLine("--- Testing Subject (Multiple Subscribers) ---");
var subject = new Subject<string>();
subject.Subscribe(new ConsoleObserver<string>("Sub1"));
subject.Subscribe(new ConsoleObserver<string>("Sub2"));
subject.OnNext("Hello");
subject.OnNext("World");
subject.OnCompleted();

Console.WriteLine();
Console.WriteLine("--- Testing Subject (Late Subscriber after Completion) ---");
var completedSubject = new Subject<int>();
completedSubject.OnNext(1);
completedSubject.OnCompleted();
// Late subscriber immediately receives OnCompleted
completedSubject.Subscribe(new ConsoleObserver<int>("Late"));

Console.WriteLine();
Console.WriteLine("--- Testing Subject (Error Handling) ---");
var errorSubject = new Subject<int>();
errorSubject.Subscribe(new ConsoleObserver<int>("Error"));
errorSubject.OnNext(1);
errorSubject.OnError(new InvalidOperationException("Something went wrong"));
// Further values are ignored after error
errorSubject.OnNext(2);

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. IObservable<T> is the producer (data source)");
Console.WriteLine("2. IObserver<T> is the consumer (receives data)");
Console.WriteLine("3. Subscribe returns IDisposable for cleanup");
Console.WriteLine("4. Subject<T> bridges imperative and reactive code");
Console.WriteLine("5. OnError/OnCompleted are terminal - no more values after");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// --- Type Definitions ---

// Step 1 - ConsoleObserver implementation
public class ConsoleObserver<T> : IObserver<T>
{
    private readonly string _name;

    public ConsoleObserver(string name) => _name = name;

    public void OnNext(T value)
    {
        Console.WriteLine($"  [{_name}] Received: {value}");
    }

    public void OnError(Exception error)
    {
        Console.WriteLine($"  [{_name}] Error: {error.Message}");
    }

    public void OnCompleted()
    {
        Console.WriteLine($"  [{_name}] Completed");
    }
}

// Step 2 - Empty disposable for synchronous observables
public class EmptyDisposable : IDisposable
{
    public static readonly EmptyDisposable Instance = new();
    public void Dispose() { }
}

// Step 3 - Synchronous number sequence
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
        try
        {
            for (int i = 0; i < _count; i++)
            {
                observer.OnNext(_start + i);
            }
            observer.OnCompleted();
        }
        catch (Exception ex)
        {
            observer.OnError(ex);
        }

        return EmptyDisposable.Instance;
    }
}

// Step 4 - Subscription with cancellation support
public class Subscription : IDisposable
{
    private readonly Action? _onDispose;
    private bool _disposed;

    public Subscription(Action? onDispose = null) => _onDispose = onDispose;

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _onDispose?.Invoke();
        }
    }
}

// Step 5 - Async observable with cancellation
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
                {
                    observer.OnCompleted();
                }
            }
            catch (OperationCanceledException)
            {
                // Subscription was disposed - this is expected
                Console.WriteLine("  [Async] Subscription cancelled");
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        });

        return new Subscription(() => cts.Cancel());
    }
}

// Step 6 - Subject implementation
public class Subject<T> : IObservable<T>, IObserver<T>, IDisposable
{
    private readonly List<IObserver<T>> _observers = new();
    private bool _completed;
    private Exception? _error;
    private readonly object _lock = new();

    public IDisposable Subscribe(IObserver<T> observer)
    {
        lock (_lock)
        {
            if (_error != null)
            {
                observer.OnError(_error);
                return EmptyDisposable.Instance;
            }

            if (_completed)
            {
                observer.OnCompleted();
                return EmptyDisposable.Instance;
            }

            _observers.Add(observer);
            return new Subscription(() =>
            {
                lock (_lock)
                {
                    _observers.Remove(observer);
                }
            });
        }
    }

    public void OnNext(T value)
    {
        IObserver<T>[] observers;
        lock (_lock)
        {
            if (_completed || _error != null) return;
            observers = _observers.ToArray();
        }

        foreach (var observer in observers)
        {
            observer.OnNext(value);
        }
    }

    public void OnError(Exception error)
    {
        IObserver<T>[] observers;
        lock (_lock)
        {
            if (_completed || _error != null) return;
            _error = error;
            observers = _observers.ToArray();
            _observers.Clear();
        }

        foreach (var observer in observers)
        {
            observer.OnError(error);
        }
    }

    public void OnCompleted()
    {
        IObserver<T>[] observers;
        lock (_lock)
        {
            if (_completed || _error != null) return;
            _completed = true;
            observers = _observers.ToArray();
            _observers.Clear();
        }

        foreach (var observer in observers)
        {
            observer.OnCompleted();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _observers.Clear();
        }
    }
}
