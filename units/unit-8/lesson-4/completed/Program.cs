// Surgewave Bootcamp - Unit 12, Lesson 4: Backpressure
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 12.4 - Backpressure ===");
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

Console.WriteLine("--- Demonstrating Backpressure Problem ---");
Console.WriteLine("Fast producer (50 msgs, 10ms each) vs slow consumer (50ms each):");

var received1 = 0;
var fastSource1 = new FastProducerObservable(count: 20, delayMs: 10);
var completed1 = new TaskCompletionSource();

using (var sub = fastSource1.Subscribe(
    x =>
    {
        received1++;
        Thread.Sleep(50); // Slow processing
        Console.WriteLine($"  Processed: {x}");
    },
    onCompleted: () => completed1.SetResult()))
{
    await Task.WhenAny(completed1.Task, Task.Delay(3000));
}
Console.WriteLine($"  Received {received1} messages (notice the delay accumulation)");

Console.WriteLine();
Console.WriteLine("--- Testing Sample Operator ---");
Console.WriteLine("Sample: emit only latest value every 100ms");

var sampledValues = new List<int>();
var fastSource2 = new FastProducerObservable(count: 50, delayMs: 10);
var completed2 = new TaskCompletionSource();

using (var sub = fastSource2
    .Sample(TimeSpan.FromMilliseconds(100))
    .Subscribe(
        x =>
        {
            sampledValues.Add(x);
            Console.WriteLine($"  Sampled: {x}");
        },
        onCompleted: () => completed2.SetResult()))
{
    await Task.WhenAny(completed2.Task, Task.Delay(2000));
}
Console.WriteLine($"  Sampled {sampledValues.Count} out of 50 messages");

Console.WriteLine();
Console.WriteLine("--- Testing Throttle Operator ---");
Console.WriteLine("Throttle: at most 1 value per 100ms");

var throttledValues = new List<int>();
var fastSource3 = new FastProducerObservable(count: 50, delayMs: 10);
var completed3 = new TaskCompletionSource();

using (var sub = fastSource3
    .Throttle(TimeSpan.FromMilliseconds(100))
    .Subscribe(
        x =>
        {
            throttledValues.Add(x);
            Console.WriteLine($"  Throttled: {x}");
        },
        onCompleted: () => completed3.SetResult()))
{
    await Task.WhenAny(completed3.Task, Task.Delay(2000));
}
Console.WriteLine($"  Throttled to {throttledValues.Count} out of 50 messages");

Console.WriteLine();
Console.WriteLine("--- Testing Bounded Buffer ---");
Console.WriteLine("Buffer size 5, DropOldest policy:");

var buffer = new BoundedBuffer<int>(5, OverflowPolicy.DropOldest);
for (int i = 1; i <= 10; i++)
{
    buffer.TryAdd(i);
    Console.WriteLine($"  Added {i}, buffer count: {buffer.Count}, dropped: {buffer.DroppedCount}");
}

Console.WriteLine("  Draining buffer:");
while (buffer.TryTake(out var item))
{
    Console.WriteLine($"  Got: {item}");
}

Console.WriteLine();
Console.WriteLine("--- Testing with Surgewave Messages ---");

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{port}";
});

// Produce many messages quickly
Console.WriteLine("Producing 20 messages...");
for (int i = 1; i <= 20; i++)
{
    await producer.ProduceAsync("backpressure-topic", $"key-{i}", $"value-{i}");
}

// Consume with throttling
var surgewaveObservable = new SurgewaveObservable<string, string>(
    $"localhost:{port}",
    "backpressure-topic",
    "backpressure-group"
);

Console.WriteLine("Consuming with throttle (200ms):");
var surgewaveCompleted = new TaskCompletionSource();
var surgewaveCount = 0;

using (var sub = surgewaveObservable
    .Throttle(TimeSpan.FromMilliseconds(200))
    .Subscribe(
        msg =>
        {
            surgewaveCount++;
            Console.WriteLine($"  [{surgewaveCount}] {msg.Key}: {msg.Value}");
            if (surgewaveCount >= 5) surgewaveCompleted.TrySetResult();
        }))
{
    await Task.WhenAny(surgewaveCompleted.Task, Task.Delay(3000));
}

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Backpressure occurs when producer > consumer speed");
Console.WriteLine("2. Sample takes latest value at intervals (lossy, time-based)");
Console.WriteLine("3. Throttle passes first value, blocks for interval (rate limiting)");
Console.WriteLine("4. Bounded buffer with policies controls memory usage");
Console.WriteLine("5. Choose strategy based on data freshness vs completeness needs");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// --- Type Definitions ---

public enum OverflowPolicy
{
    DropOldest,
    DropNewest,
    ThrowException
}

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

// Bounded buffer
public class BoundedBuffer<T>
{
    private readonly Queue<T> _queue = new();
    private readonly int _capacity;
    private readonly OverflowPolicy _policy;
    private readonly object _lock = new();
    private int _droppedCount;

    public BoundedBuffer(int capacity, OverflowPolicy policy = OverflowPolicy.DropOldest)
    {
        _capacity = capacity;
        _policy = policy;
    }

    public int Count
    {
        get { lock (_lock) return _queue.Count; }
    }

    public int DroppedCount
    {
        get { lock (_lock) return _droppedCount; }
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
                        _droppedCount++;
                        break;
                    case OverflowPolicy.DropNewest:
                        _droppedCount++;
                        return false;
                    case OverflowPolicy.ThrowException:
                        throw new InvalidOperationException($"Buffer overflow: capacity {_capacity} exceeded");
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

// Sample observable
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
        bool completed = false;
        var cts = new CancellationTokenSource();
        var lockObj = new object();

        Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested && !completed)
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
            }
            catch (OperationCanceledException) { }
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
            () =>
            {
                completed = true;
                observer.OnCompleted();
            }
        ));

        return new CompositeDisposable(
            subscription,
            new Subscription(() => cts.Cancel())
        );
    }
}

// Throttle observable
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
        int passedCount = 0;
        int droppedCount = 0;

        var subscription = _source.Subscribe(new AnonymousObserver<T>(
            value =>
            {
                bool shouldEmit;
                lock (lockObj)
                {
                    var now = DateTime.UtcNow;
                    shouldEmit = now - lastEmit >= _interval;
                    if (shouldEmit)
                    {
                        lastEmit = now;
                        passedCount++;
                    }
                    else
                    {
                        droppedCount++;
                    }
                }

                if (shouldEmit)
                    observer.OnNext(value);
            },
            observer.OnError,
            () =>
            {
                Console.WriteLine($"    Throttle stats: {passedCount} passed, {droppedCount} dropped");
                observer.OnCompleted();
            }
        ));

        return subscription;
    }
}

// Extension methods
public static class BackpressureExtensions
{
    public static IObservable<T> Sample<T>(
        this IObservable<T> source,
        TimeSpan interval)
        => new SampleObservable<T>(source, interval);

    public static IObservable<T> Throttle<T>(
        this IObservable<T> source,
        TimeSpan interval)
        => new ThrottleObservable<T>(source, interval);

    public static IDisposable Subscribe<T>(
        this IObservable<T> source,
        Action<T> onNext,
        Action<Exception>? onError = null,
        Action? onCompleted = null)
        => source.Subscribe(new AnonymousObserver<T>(onNext, onError, onCompleted));
}

// Fast producer for testing
public class FastProducerObservable : IObservable<int>
{
    private readonly int _count;
    private readonly int _delayMs;

    public FastProducerObservable(int count, int delayMs)
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
                for (int i = 1; i <= _count && !cts.Token.IsCancellationRequested; i++)
                {
                    observer.OnNext(i);
                    if (_delayMs > 0)
                        await Task.Delay(_delayMs, cts.Token);
                }
                observer.OnCompleted();
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
