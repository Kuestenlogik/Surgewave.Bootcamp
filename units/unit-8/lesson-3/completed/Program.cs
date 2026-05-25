// Surgewave Bootcamp - Unit 12, Lesson 3: Rx Operators
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 12.3 - Rx Operators ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var port = runtime.Port;
Console.WriteLine($"Broker started on port {port}");
Console.WriteLine();

Console.WriteLine("--- Testing Operators with Numbers ---");

// Test Where operator
Console.WriteLine("Where (even numbers):");
var numbers = new EnumerableObservable<int>(Enumerable.Range(1, 10));
numbers
    .Where(x => x % 2 == 0)
    .Subscribe(x => Console.WriteLine($"  {x}"));

Console.WriteLine();

// Test Select operator
Console.WriteLine("Select (square):");
new EnumerableObservable<int>(Enumerable.Range(1, 5))
    .Select(x => x * x)
    .Subscribe(x => Console.WriteLine($"  {x}"));

Console.WriteLine();

// Test Take operator
Console.WriteLine("Take (first 3):");
new EnumerableObservable<int>(Enumerable.Range(1, 100))
    .Take(3)
    .Subscribe(
        x => Console.WriteLine($"  {x}"),
        onCompleted: () => Console.WriteLine("  Completed!")
    );

Console.WriteLine();

// Test Buffer operator
Console.WriteLine("Buffer (groups of 3):");
new EnumerableObservable<int>(Enumerable.Range(1, 10))
    .Buffer(3)
    .Subscribe(batch => Console.WriteLine($"  Batch: [{string.Join(", ", batch)}]"));

Console.WriteLine();

// Test operator chaining
Console.WriteLine("Chained: Where -> Select -> Take:");
new EnumerableObservable<int>(Enumerable.Range(1, 20))
    .Where(x => x % 2 == 0)       // 2, 4, 6, 8, 10, ...
    .Select(x => $"Value: {x}")  // "Value: 2", "Value: 4", ...
    .Take(3)                      // First 3
    .Subscribe(
        x => Console.WriteLine($"  {x}"),
        onCompleted: () => Console.WriteLine("  Pipeline completed!")
    );

Console.WriteLine();
Console.WriteLine("--- Producing Test Messages ---");

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{port}";
});

var products = new[]
{
    ("prod-1", "Laptop|999.99"),
    ("prod-2", "Mouse|29.99"),
    ("prod-3", "Keyboard|79.99"),
    ("prod-4", "Monitor|399.99"),
    ("prod-5", "Headset|149.99"),
    ("prod-6", "Webcam|89.99"),
    ("prod-7", "USB Hub|24.99"),
    ("prod-8", "Mousepad|19.99"),
};

foreach (var (key, value) in products)
{
    await producer.ProduceAsync("products-topic", key, value);
    Console.WriteLine($"  Produced: {key} -> {value}");
}

Console.WriteLine();
Console.WriteLine("--- Applying Operators to Surgewave Messages ---");

var surgewaveObservable = new SurgewaveObservable<string, string>(
    $"localhost:{port}",
    "products-topic",
    "operator-group"
);

var completed = new TaskCompletionSource();
var messageCount = 0;

using var subscription = surgewaveObservable
    .Where(msg => msg.Value != null)
    .Select(msg =>
    {
        var parts = msg.Value!.Split('|');
        return new { Name = parts[0], Price = decimal.Parse(parts[1]) };
    })
    .Where(product => product.Price > 50)  // Products over $50
    .Take(4)                                // First 4 matching
    .Subscribe(
        product =>
        {
            messageCount++;
            Console.WriteLine($"  High-value product: {product.Name} @ ${product.Price}");
        },
        ex => Console.WriteLine($"  Error: {ex.Message}"),
        () =>
        {
            Console.WriteLine($"  Pipeline completed after {messageCount} products!");
            completed.TrySetResult();
        }
    );

// Wait for either completion or timeout
var timeoutTask = Task.Delay(5000);
await Task.WhenAny(completed.Task, timeoutTask);

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Operators are composable - each returns IObservable<T>");
Console.WriteLine("2. Where filters, Select transforms, Take limits, Buffer batches");
Console.WriteLine("3. Extension methods enable fluent chaining");
Console.WriteLine("4. Operators propagate errors and completion automatically");
Console.WriteLine("5. Take operator completes the stream early");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// --- Type Definitions ---

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
    private readonly object?[] _disposables;
    private bool _disposed;

    public CompositeDisposable(params object?[] disposables) => _disposables = disposables;

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            foreach (var d in _disposables)
            {
                try
                {
                    if (d is IAsyncDisposable asyncDisposable)
                        asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    else if (d is IDisposable disposable)
                        disposable.Dispose();
                }
                catch { /* Ignore disposal errors */ }
            }
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
        _onError = onError ?? (_ => { });
        _onCompleted = onCompleted ?? (() => { });
    }

    public void OnNext(T value) => _onNext(value);
    public void OnError(Exception error) => _onError(error);
    public void OnCompleted() => _onCompleted();
}

// Where operator
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
                try
                {
                    if (_predicate(value))
                        observer.OnNext(value);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            },
            observer.OnError,
            observer.OnCompleted
        ));
    }
}

// Select operator
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
            value =>
            {
                try
                {
                    observer.OnNext(_selector(value));
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            },
            observer.OnError,
            observer.OnCompleted
        ));
    }
}

// Take operator
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

// Buffer operator
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

// Extension methods
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

// Simple observable from enumerable for testing
public class EnumerableObservable<T> : IObservable<T>
{
    private readonly IEnumerable<T> _source;

    public EnumerableObservable(IEnumerable<T> source) => _source = source;

    public IDisposable Subscribe(IObserver<T> observer)
    {
        try
        {
            foreach (var item in _source)
                observer.OnNext(item);
            observer.OnCompleted();
        }
        catch (Exception ex)
        {
            observer.OnError(ex);
        }
        return new Subscription(() => { });
    }
}

// Surgewave Observable wrapper
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
                        observer.OnNext(result);
                }
            }
            catch (OperationCanceledException) { }
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
