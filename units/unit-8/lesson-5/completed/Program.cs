// Surgewave Bootcamp - Unit 12, Lesson 5: Integration Patterns
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 12.5 - Integration Patterns ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var port = runtime.Port;
Console.WriteLine($"Broker started on port {port}");
Console.WriteLine();

Console.WriteLine("--- Testing Interval Observable ---");
var completed1 = new TaskCompletionSource();
var intervalValues = new List<long>();

using (var sub = IntegrationExtensions.Interval(TimeSpan.FromMilliseconds(100), count: 5)
    .Subscribe(
        tick =>
        {
            intervalValues.Add(tick);
            Console.WriteLine($"  Tick: {tick}");
        },
        onCompleted: () => completed1.SetResult()))
{
    await Task.WhenAny(completed1.Task, Task.Delay(2000));
}
Console.WriteLine($"  Received {intervalValues.Count} ticks");

Console.WriteLine();
Console.WriteLine("--- Testing Merge with Multiple Topics ---");

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{port}";
});

// Produce to orders topic
await producer.ProduceAsync("orders-topic", "order-1", "Order: Laptop");
await producer.ProduceAsync("orders-topic", "order-2", "Order: Mouse");
await producer.ProduceAsync("orders-topic", "order-3", "Order: Keyboard");

// Produce to payments topic
await producer.ProduceAsync("payments-topic", "pay-1", "Payment: $999");
await producer.ProduceAsync("payments-topic", "pay-2", "Payment: $29");
await producer.ProduceAsync("payments-topic", "pay-3", "Payment: $79");

Console.WriteLine("  Produced 3 orders and 3 payments");

var ordersStream = new SurgewaveObservable<string, string>(
    $"localhost:{port}", "orders-topic", "merge-group-1");
var paymentsStream = new SurgewaveObservable<string, string>(
    $"localhost:{port}", "payments-topic", "merge-group-2");

var mergedStream = IntegrationExtensions.Merge(ordersStream, paymentsStream);

Console.WriteLine("  Merged stream (showing first 4):");
var mergeCount = 0;
var mergeCompleted = new TaskCompletionSource();

using (var sub = mergedStream
    .Take(4)
    .Subscribe(
        msg =>
        {
            mergeCount++;
            Console.WriteLine($"  [{mergeCount}] {msg.Key}: {msg.Value}");
        },
        onCompleted: () => mergeCompleted.TrySetResult()))
{
    await Task.WhenAny(mergeCompleted.Task, Task.Delay(3000));
}

Console.WriteLine();
Console.WriteLine("--- Testing SelectMany for Async Enrichment ---");

// Simple observable with user IDs
var userIds = new[] { "user-1", "user-2", "user-3" };
var userIdObservable = new EnumerableObservable<string>(userIds);

Console.WriteLine("  Enriching user IDs with names:");
var enrichCompleted = new TaskCompletionSource();

using (var sub = userIdObservable
    .SelectMany(userId => IntegrationExtensions.FromAsync(async ct =>
    {
        var name = await LookupUserAsync(userId, ct);
        return new { UserId = userId, Name = name };
    }))
    .Subscribe(
        enriched => Console.WriteLine($"  {enriched.UserId} -> {enriched.Name}"),
        onCompleted: () => enrichCompleted.SetResult()))
{
    await Task.WhenAny(enrichCompleted.Task, Task.Delay(2000));
}

Console.WriteLine();
Console.WriteLine("--- Building Complete Pipeline ---");

// Produce some product events
await producer.ProduceAsync("products-topic", "prod-1", "Laptop|999|electronics");
await producer.ProduceAsync("products-topic", "prod-2", "Mouse|29|electronics");
await producer.ProduceAsync("products-topic", "prod-3", "Desk|199|furniture");
await producer.ProduceAsync("products-topic", "prod-4", "Chair|299|furniture");
await producer.ProduceAsync("products-topic", "prod-5", "Monitor|399|electronics");

Console.WriteLine("  Complete pipeline: Surgewave -> Parse -> Filter -> Enrich -> Output");

var productStream = new SurgewaveObservable<string, string>(
    $"localhost:{port}", "products-topic", "pipeline-group");

var pipelineCompleted = new TaskCompletionSource();
var processedCount = 0;

using (var sub = productStream
    .Where(msg => msg.Value != null)
    .Select(msg =>
    {
        var parts = msg.Value!.Split('|');
        return new
        {
            Id = msg.Key,
            Name = parts[0],
            Price = decimal.Parse(parts[1]),
            Category = parts[2]
        };
    })
    .Where(product => product.Price > 100) // Filter high-value items
    .SelectMany(product => IntegrationExtensions.FromAsync(async ct =>
    {
        // Simulated enrichment - add stock status
        await Task.Delay(20, ct);
        var inStock = product.Price < 500;
        return new
        {
            product.Id,
            product.Name,
            product.Price,
            product.Category,
            InStock = inStock
        };
    }))
    .Take(3)
    .Subscribe(
        enriched =>
        {
            processedCount++;
            var stockStatus = enriched.InStock ? "In Stock" : "Out of Stock";
            Console.WriteLine($"  [{processedCount}] {enriched.Name} (${enriched.Price}) - {enriched.Category} - {stockStatus}");
        },
        ex => Console.WriteLine($"  Error: {ex.Message}"),
        () => pipelineCompleted.TrySetResult()))
{
    await Task.WhenAny(pipelineCompleted.Task, Task.Delay(5000));
}

Console.WriteLine($"  Pipeline processed {processedCount} high-value products");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Merge combines multiple streams into one");
Console.WriteLine("2. Interval creates time-based observables");
Console.WriteLine("3. SelectMany enables async enrichment/expansion");
Console.WriteLine("4. FromAsync bridges Task<T> to IObservable<T>");
Console.WriteLine("5. Patterns compose into complete reactive pipelines");

Console.WriteLine();
Console.WriteLine("=== Unit 12 Complete! ===");
Console.WriteLine("You've mastered Reactive Integration with Surgewave!");

// Simulated async user lookup
static async Task<string> LookupUserAsync(string userId, CancellationToken ct)
{
    await Task.Delay(50, ct); // Simulate network delay
    return userId switch
    {
        "user-1" => "Alice",
        "user-2" => "Bob",
        "user-3" => "Charlie",
        _ => "Unknown"
    };
}

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

// Merge Observable
public class MergeObservable<T> : IObservable<T>
{
    private readonly IObservable<T>[] _sources;

    public MergeObservable(params IObservable<T>[] sources) => _sources = sources;

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

// Interval Observable
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

// SelectMany Observable
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

                try
                {
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
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
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

// FromAsync Observable
public class FromAsyncObservable<T> : IObservable<T>
{
    private readonly Func<CancellationToken, Task<T>> _factory;

    public FromAsyncObservable(Func<CancellationToken, Task<T>> factory) => _factory = factory;

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
            value => { if (_predicate(value)) observer.OnNext(value); },
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
            value => observer.OnNext(_selector(value)),
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

// Extension methods
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

    public static IObservable<T> Where<T>(this IObservable<T> source, Func<T, bool> predicate)
        => new WhereObservable<T>(source, predicate);

    public static IObservable<TResult> Select<TSource, TResult>(
        this IObservable<TSource> source,
        Func<TSource, TResult> selector)
        => new SelectObservable<TSource, TResult>(source, selector);

    public static IObservable<T> Take<T>(this IObservable<T> source, int count)
        => new TakeObservable<T>(source, count);

    public static IDisposable Subscribe<T>(
        this IObservable<T> source,
        Action<T> onNext,
        Action<Exception>? onError = null,
        Action? onCompleted = null)
        => source.Subscribe(new AnonymousObserver<T>(onNext, onError, onCompleted));
}

// EnumerableObservable
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
